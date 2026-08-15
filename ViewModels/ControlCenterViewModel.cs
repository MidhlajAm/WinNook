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

namespace Project_Nook.ViewModels
{
    public partial class ControlCenterViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;

        [ObservableProperty]
        private string _greeting = "Welcome to WinNook Control Center!";

        public IslandViewModel Island { get; }

        [ObservableProperty]
        private bool _isStartupEnabled;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private int _selectedOptionsTabIndex = 0;

        [ObservableProperty]
        private int _selectedDiveInTabIndex = 0;

        public ObservableCollection<AppItem> DiveInApps { get; } = new ObservableCollection<AppItem>();

        public ControlCenterViewModel(IConfigurationService configurationService, IslandViewModel islandViewModel)
        {
            _configurationService = configurationService;
            Island = islandViewModel;
            IsStartupEnabled = _configurationService.IsStartupEnabled;
            IsMusicWidgetEnabled = _configurationService.IsMusicWidgetEnabled;
            LoadInstalledApps();
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
                                    IconImage = iconImage,
                                    IsDiveInEnabled = savedApps.Contains(exeName, StringComparer.OrdinalIgnoreCase)
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
            }
        }

        partial void OnIsStartupEnabledChanged(bool value)
        {
            _configurationService.IsStartupEnabled = value;
        }

        [ObservableProperty]
        private bool _isMusicWidgetEnabled;

        partial void OnIsMusicWidgetEnabledChanged(bool value)
        {
            _configurationService.IsMusicWidgetEnabled = value;
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
