using Project_Nook.Services;
using System.Collections.Generic;

namespace Project_Nook.Infrastructure
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, string> _settings = new();

        public string GetSetting(string key)
        {
            return _settings.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public void SetSetting(string key, string value)
        {
            _settings[key] = value;
        }
    }
}
