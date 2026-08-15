using System;
using System.IO;
using System.Text.Json;
using Project_Nook.Services;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Project_Nook.Infrastructure
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private Dictionary<string, string> _settings = new();

        public ConfigurationService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "WinNook");
            Directory.CreateDirectory(folder);
            _configFilePath = Path.Combine(folder, "settings.json");
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(_configFilePath))
            {
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        var json = File.ReadAllText(_configFilePath);
                        _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                        break;
                    }
                    catch
                    {
                        retries--;
                        if (retries > 0) System.Threading.Thread.Sleep(200);
                    }
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }
        }

        public string GetSetting(string key)
        {
            return _settings.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public bool HasSetting(string key)
        {
            return _settings.ContainsKey(key);
        }

        public void SetSetting(string key, string value)
        {
            _settings[key] = value;
            SaveSettings();
        }

        public bool IsStartupEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("WinNook") != null;
            }
            set
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (value)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    key.SetValue("WinNook", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("WinNook", false);
                }
            }
        }

        public List<string> GetDiveInApps()
        {
            var json = GetSetting("DiveInApps");
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void SaveDiveInApps(List<string> apps)
        {
            SetSetting("DiveInApps", JsonSerializer.Serialize(apps));
        }

        public bool IsMusicWidgetEnabled
        {
            get => bool.TryParse(GetSetting(nameof(IsMusicWidgetEnabled)), out var result) ? result : true;
            set => SetSetting(nameof(IsMusicWidgetEnabled), value.ToString());
        }
    }
}
