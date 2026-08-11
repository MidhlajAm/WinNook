using System;

namespace Project_Nook.Services
{
    public interface IConfigurationService
    {
        string GetSetting(string key);
        void SetSetting(string key, string value);
    }
}
