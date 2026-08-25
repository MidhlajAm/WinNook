using System;
using System.IO;
using System.Text.Json;
using Project_Nook.Services;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;

namespace Project_Nook.Infrastructure
{
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        /// <summary>
        /// How long to wait for more changes before writing. The appearance sliders update settings in
        /// real time, which without this meant a full serialize-and-write of the settings file on every
        /// frame of a drag.
        /// </summary>
        private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(400);

        /// <summary>Backoff after a failed write, so a locked file does not spin the timer.</summary>
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly string _configFilePath;
        private readonly object _gate = new();
        private readonly Timer _writeTimer;

        private Dictionary<string, string> _settings = new();
        private bool _isDirty;
        private bool _isDisposed;

        public ConfigurationService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "WinNook");
            Directory.CreateDirectory(folder);
            _configFilePath = Path.Combine(folder, "settings.json");

            _writeTimer = new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);

            LoadSettings();
        }

        private void LoadSettings()
        {
            if (!File.Exists(_configFilePath)) return;

            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    return;
                }
                catch
                {
                    retries--;
                    if (retries > 0) Thread.Sleep(200);
                }
            }
        }

        /// <summary>
        /// Writes pending changes now. Called on the debounce timer and again on shutdown, so quitting
        /// straight after a change cannot lose it.
        /// </summary>
        public void Flush()
        {
            string json;

            lock (_gate)
            {
                if (!_isDirty) return;
                _isDirty = false;
                json = JsonSerializer.Serialize(_settings);
            }

            try
            {
                // Write beside the real file and swap it in. A crash or power loss mid-write then leaves
                // the previous settings intact instead of a truncated file.
                //
                // File.Move with overwrite, not File.Replace: Replace also wants to shuffle the original
                // aside and fails outright on some setups, which silently cost every write.
                var tempPath = _configFilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _configFilePath, true);
            }
            catch (Exception ex)
            {
                // Leave the change marked dirty so the next write gets another go, and reschedule one:
                // without a retry a transient failure means settings stop persisting for the whole session.
                lock (_gate) _isDirty = true;

                LogWriteFailure(ex);

                try { _writeTimer.Change(RetryDelay, Timeout.InfiniteTimeSpan); }
                catch (ObjectDisposedException) { }
            }
        }

        /// <summary>
        /// Settings silently failing to save is the kind of bug that hides for months, so leave a trace.
        /// </summary>
        private static void LogWriteFailure(Exception ex)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinNook", "logs");
                Directory.CreateDirectory(dir);

                File.AppendAllText(
                    Path.Combine(dir, $"error-{DateTime.Now:yyyy-MM-dd}.log"),
                    $"[{DateTime.Now:HH:mm:ss}] settings write failed: {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { }
        }

        private void ScheduleWrite()
        {
            lock (_gate)
            {
                _isDirty = true;
                if (_isDisposed) return;
            }

            try { _writeTimer.Change(WriteDelay, Timeout.InfiniteTimeSpan); }
            catch (ObjectDisposedException) { }
        }

        public string GetSetting(string key)
        {
            lock (_gate)
            {
                return _settings.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }

        public bool HasSetting(string key)
        {
            lock (_gate)
            {
                return _settings.ContainsKey(key);
            }
        }

        public void SetSetting(string key, string value)
        {
            lock (_gate)
            {
                if (_settings.TryGetValue(key, out var existing) && existing == value) return;
                _settings[key] = value;
            }

            ScheduleWrite();
        }

        public bool IsStartupEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                    return key?.GetValue("WinNook") != null;
                }
                catch { return false; }
            }
            set
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    if (key == null) return;

                    if (value)
                    {
                        var exePath = Environment.ProcessPath;
                        if (string.IsNullOrEmpty(exePath)) return;
                        key.SetValue("WinNook", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("WinNook", false);
                    }
                }
                catch { }
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

        public void Dispose()
        {
            lock (_gate)
            {
                if (_isDisposed) return;
                _isDisposed = true;
            }

            _writeTimer.Dispose();
            Flush();
        }
    }
}
