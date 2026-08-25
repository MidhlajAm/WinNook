using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Project_Nook.Helpers;
using Project_Nook.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace Project_Nook.ViewModels
{
    /// <summary>A pinned app. Only Path and Name persist; the icon is read back off disk at load.</summary>
    public class PinnedApp : ObservableObject
    {
        public string Path { get; set; }
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ImageSource Icon { get; set; }
    }

    public partial class QuickLaunchWidgetViewModel : ObservableObject
    {
        private const string SettingsKey = "PinnedApps";

        /// <summary>The rail is finite. Past this the strip stops being a glance and starts being a menu.</summary>
        private const int MaxPinned = 6;

        private readonly IConfigurationService _config;

        public ObservableCollection<PinnedApp> Apps { get; } = new();

        public bool CanPinMore => Apps.Count < MaxPinned;

        public QuickLaunchWidgetViewModel(IConfigurationService config)
        {
            _config = config;
            Load();
        }

        private void Load()
        {
            var json = _config.GetSetting(SettingsKey);
            if (string.IsNullOrEmpty(json)) return;

            List<PinnedApp> stored;
            try
            {
                stored = JsonSerializer.Deserialize<List<PinnedApp>>(json) ?? new List<PinnedApp>();
            }
            catch
            {
                return;
            }

            foreach (var app in stored)
            {
                // An app uninstalled since it was pinned should quietly drop off, not show a dead tile.
                if (string.IsNullOrEmpty(app.Path) || !File.Exists(app.Path)) continue;

                app.Icon = IconHelper.GetIcon(app.Path);
                Apps.Add(app);
            }

            OnPropertyChanged(nameof(CanPinMore));
        }

        private void Save()
        {
            _config.SetSetting(SettingsKey, JsonSerializer.Serialize(Apps.ToList()));
            OnPropertyChanged(nameof(CanPinMore));
        }

        /// <summary>Opens the Control Center's Apps tab. Wired by IslandViewModel.</summary>
        public Action OpenSettingsAction { get; set; }

        [RelayCommand]
        private void OpenSettings() => OpenSettingsAction?.Invoke();

        public bool IsPinnedPath(string path) =>
            !string.IsNullOrEmpty(path) &&
            Apps.Any(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase));

        public void UnpinPath(string path)
        {
            var match = Apps.FirstOrDefault(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase));
            if (match != null) Unpin(match);
        }

        [RelayCommand]
        private void Launch(PinnedApp app)
        {
            if (app?.Path == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = app.Path,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(app.Path) ?? string.Empty
                });
            }
            catch { }
        }

        /// <summary>
        /// Adds an app by full path. Shortcuts are resolved to their target first.
        /// Returns false when the rail is already full, so the caller can undo its own toggle.
        /// </summary>
        public bool Pin(string path, string displayName = null)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!CanPinMore) return false;

            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                path = ResolveShortcut(path) ?? path;
            }

            if (!File.Exists(path)) return false;
            if (Apps.Any(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase))) return true;

            Apps.Add(new PinnedApp
            {
                Path = path,
                Name = string.IsNullOrWhiteSpace(displayName)
                    ? System.IO.Path.GetFileNameWithoutExtension(path)
                    : displayName,
                Icon = IconHelper.GetIcon(path)
            });

            Save();
            return true;
        }

        [RelayCommand]
        private void Unpin(PinnedApp app)
        {
            if (app == null) return;

            Apps.Remove(app);
            Save();
        }

        /// <summary>
        /// Reads a .lnk's target through WScript.Shell, the same COM route StartMenuAppScanner uses.
        /// </summary>
        private static string ResolveShortcut(string lnkPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;

            object shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            try
            {
                object shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });

                return shortcut?.GetType().InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.GetProperty, null, shortcut, null) as string;
            }
            catch
            {
                return null;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }
    }
}
