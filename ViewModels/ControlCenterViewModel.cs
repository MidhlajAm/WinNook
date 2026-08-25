using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Services;
using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using Project_Nook.Models;
using Project_Nook.Widgets;
using System.ComponentModel;
using System.Windows.Data;

namespace Project_Nook.ViewModels
{
    public partial class ControlCenterViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private bool _isSyncingPins;

        [ObservableProperty]
        private string _greeting = "Welcome to WinNook Control Center!";

        public IslandViewModel Island { get; }

        /// <summary>Drives the Activity tab's widget list: toggles, ordering, and per-widget options.</summary>
        public WidgetRegistry Widgets { get; }

        [ObservableProperty]
        private bool _isStartupEnabled;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private int _selectedOptionsTabIndex = 0;

        public ObservableCollection<AppItem> DiveInApps { get; } = new ObservableCollection<AppItem>();

        /// <summary>
        /// Same scanned app list, filtered by the search box. Both the Dive In list and the Apps tab read
        /// from it, so the expensive registry plus Start Menu scan happens once.
        /// </summary>
        public ICollectionView AppsView { get; }

        [ObservableProperty]
        private string _appSearchText = string.Empty;

        partial void OnAppSearchTextChanged(string value) => AppsView?.Refresh();

        /// <summary>False once the Quick Launch rail is full, which disables the remaining pin toggles.</summary>
        public bool CanPinMore => QuickLaunch.CanPinMore;

        public string PinCountLabel => $"{QuickLaunch.Apps.Count} of 6 pinned";

        public QuickLaunchWidgetViewModel QuickLaunch { get; }

        /// <summary>Exposed so the Options tab can bind the screenshot destination toggle.</summary>
        public ScreenshotWidgetViewModel Screenshot { get; }

        /// <summary>Exposed so the Options tab can bind the Shelf storage budget.</summary>
        public ShelfViewModel Shelf { get; }

        public ControlCenterViewModel(
            IConfigurationService configurationService,
            IslandViewModel islandViewModel,
            WidgetRegistry widgets,
            QuickLaunchWidgetViewModel quickLaunch,
            ScreenshotWidgetViewModel screenshot,
            ShelfViewModel shelf)
        {
            _configurationService = configurationService;
            Island = islandViewModel;
            Widgets = widgets;
            QuickLaunch = quickLaunch;
            Screenshot = screenshot;
            Shelf = shelf;

            AppsView = CollectionViewSource.GetDefaultView(DiveInApps);
            AppsView.Filter = FilterApp;

            // The pinned list is the source of truth, and it can change from outside this window: the
            // notch's own unpin badge edits QuickLaunch.Apps directly. Without this the Apps tab toggle
            // kept showing an app as pinned after it had been removed from the rail.
            QuickLaunch.Apps.CollectionChanged += (s, e) =>
            {
                SyncPinnedToggles();
                OnPropertyChanged(nameof(CanPinMore));
                OnPropertyChanged(nameof(PinCountLabel));
            };

            IsStartupEnabled = _configurationService.IsStartupEnabled;
            LoadInstalledApps();
        }

        /// <summary>
        /// Pushes the pinned list back onto the app rows' toggles. Guarded, because setting IsPinned
        /// raises PropertyChanged, which would call straight back into Pin/Unpin and loop.
        /// </summary>
        private void SyncPinnedToggles()
        {
            if (_isSyncingPins) return;

            _isSyncingPins = true;
            try
            {
                foreach (var app in DiveInApps)
                {
                    app.IsPinned = QuickLaunch.IsPinnedPath(app.TargetPath);
                }
            }
            finally
            {
                _isSyncingPins = false;
            }
        }

        private bool FilterApp(object item)
        {
            if (string.IsNullOrWhiteSpace(AppSearchText)) return true;
            if (item is not AppItem app) return false;

            return app.DisplayName?.IndexOf(AppSearchText, StringComparison.OrdinalIgnoreCase) >= 0
                || app.ExecutableName?.IndexOf(AppSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [RelayCommand]
        private void LoadInstalledApps()
        {
            var savedApps = _configurationService.GetDiveInApps();

            // Clear old items and unhook events
            foreach (var app in DiveInApps)
            {
                app.PropertyChanged -= AppItem_PropertyChanged;
            }
            DiveInApps.Clear();

            var appsList = new System.Collections.Generic.List<AppItem>();
            var uniqueExes = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var keys = new[] {
                Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            foreach (var key in keys)
            {
                if (key == null) continue;
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey == null) continue;
                    
                    var displayName = subKey.GetValue("DisplayName") as string;
                    var displayIcon = subKey.GetValue("DisplayIcon") as string;
                    
                    if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(displayIcon))
                    {
                        var iconPath = displayIcon.Split(',')[0].Trim('"', ' ');
                        if (iconPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            var exeName = System.IO.Path.GetFileName(iconPath);
                            if (exeName.Equals("Project Nook.exe", StringComparison.OrdinalIgnoreCase) || 
                                exeName.Equals("WinNook.exe", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (!uniqueExes.Contains(exeName))
                            {
                                uniqueExes.Add(exeName);
                                var iconImage = Project_Nook.Helpers.IconHelper.GetIcon(iconPath);
                                
                                appsList.Add(new AppItem
                                {
                                    DisplayName = displayName,
                                    ExecutableName = exeName,
                                    TargetPath = iconPath,
                                    IconImage = iconImage,
                                    IsDiveInEnabled = savedApps.Contains(exeName, StringComparer.OrdinalIgnoreCase),
                                    IsPinned = QuickLaunch.IsPinnedPath(iconPath)
                                });
                            }
                        }
                    }
                }
            }

            // Also scan Start Menu for apps that don't register nicely in Uninstall (like Chrome)
            var startMenuApps = Project_Nook.Helpers.StartMenuAppScanner.GetInstalledApps();
            foreach (var startApp in startMenuApps)
            {
                if (!uniqueExes.Contains(startApp.ExecutableName))
                {
                    uniqueExes.Add(startApp.ExecutableName);
                    startApp.IsDiveInEnabled = savedApps.Contains(startApp.ExecutableName, StringComparer.OrdinalIgnoreCase);
                    startApp.IsPinned = QuickLaunch.IsPinnedPath(startApp.TargetPath);
                    appsList.Add(startApp);
                }
            }

            var orderedApps = appsList.OrderBy(a => a.DisplayName).ToList();
            foreach (var item in orderedApps)
            {
                item.PropertyChanged += AppItem_PropertyChanged;
                DiveInApps.Add(item);
            }

            // Also ensure previously saved apps that might not be running right now are in the list
            foreach (var savedExe in savedApps)
            {
                if (!uniqueExes.Contains(savedExe))
                {
                    var item = new AppItem
                    {
                        DisplayName = savedExe,
                        ExecutableName = savedExe,
                        IsDiveInEnabled = true
                    };
                    item.PropertyChanged += AppItem_PropertyChanged;
                    DiveInApps.Add(item);
                }
            }
        }

        private void AppItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppItem.IsDiveInEnabled))
            {
                var enabledApps = DiveInApps.Where(a => a.IsDiveInEnabled).Select(a => a.ExecutableName).ToList();
                _configurationService.SaveDiveInApps(enabledApps);
                return;
            }

            if (e.PropertyName == nameof(AppItem.IsPinned) && sender is AppItem app)
            {
                // This change came from SyncPinnedToggles reflecting the rail, not from the user.
                if (_isSyncingPins) return;

                if (app.IsPinned)
                {
                    // The rail holds six. If it is full, put the toggle back rather than silently ignoring it.
                    if (!QuickLaunch.Pin(app.TargetPath, app.DisplayName)) app.IsPinned = false;
                }
                else
                {
                    QuickLaunch.UnpinPath(app.TargetPath);
                }

                OnPropertyChanged(nameof(CanPinMore));
                OnPropertyChanged(nameof(PinCountLabel));
            }
        }

        partial void OnIsStartupEnabledChanged(bool value)
        {
            _configurationService.IsStartupEnabled = value;
        }

        [RelayCommand]
        private void UpdateGreeting()
        {
            Greeting = "WinNook is ready!";
        }

        [RelayCommand]
        private void HoverIsland() => Island.HoverCommand.Execute(null);

        [RelayCommand]
        private void UnhoverIsland() => Island.UnhoverCommand.Execute(null);

        [RelayCommand]
        private void ExpandIsland() => Island.ExpandCommand.Execute(null);

        [RelayCommand]
        private void CollapseIsland() => Island.CollapseCommand.Execute(null);

        [RelayCommand]
        private void KillApp()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task DeleteApp()
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Self-Destruct WinNook",
                Content = "Are you sure you want to permanently delete WinNook, remove it from startup, and wipe all saved configurations?\n\nThis action cannot be undone.",
                PrimaryButtonText = "Delete Everything",
                CloseButtonText = "Cancel",
                PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger
            };

            var result = await uiMessageBox.ShowDialogAsync();
            if (result != Wpf.Ui.Controls.MessageBoxResult.Primary)
                return;

            // 1. Remove from startup
            _configurationService.IsStartupEnabled = false;

            // 2. Delete AppData Settings
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinNook");
            if (Directory.Exists(appDataFolder))
            {
                try
                {
                    Directory.Delete(appDataFolder, true);
                }
                catch { /* Ignore errors if file is locked */ }
            }

            // 3. Trigger Deletion Script for WinNook.exe
            string tempScript = Path.Combine(Path.GetTempPath(), "WinNook_Uninstall.cmd");
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(currentDir, "WinNook.exe");

            string scriptContent = $@"@echo off
timeout /t 2 /nobreak > NUL
del /f /q ""{exePath}""
del ""%~f0""
";
            File.WriteAllText(tempScript, scriptContent);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = tempScript,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);

            // 4. Exit the App
            Application.Current.Shutdown();
        }
    }
}
