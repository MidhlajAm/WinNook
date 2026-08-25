using System;

namespace Project_Nook.Services
{
    public interface IConfigurationService
    {
        string GetSetting(string key);
        bool HasSetting(string key);

        /// <summary>Writes any pending changes immediately, bypassing the debounce.</summary>
        void Flush();
        void SetSetting(string key, string value);
        bool IsStartupEnabled { get; set; }
        System.Collections.Generic.List<string> GetDiveInApps();
        void SaveDiveInApps(System.Collections.Generic.List<string> apps);
    }
}
