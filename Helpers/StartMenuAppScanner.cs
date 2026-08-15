using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Project_Nook.Models;

namespace Project_Nook.Helpers
{
    public static class StartMenuAppScanner
    {
        public static List<AppItem> GetInstalledApps()
        {
            var appsList = new List<AppItem>();
            var uniqueExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var startMenuPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
            };

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return appsList;

            object? shell = Activator.CreateInstance(shellType);
            if (shell == null) return appsList;

            try
            {
                foreach (var startMenuPath in startMenuPaths)
                {
                    if (!Directory.Exists(startMenuPath)) continue;

                    var lnkFiles = Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories);
                    foreach (var lnk in lnkFiles)
                    {
                        try
                        {
                            object[] args = { lnk };
                            object? shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, args);
                            if (shortcut != null)
                            {
                                string? targetPath = shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null) as string;
                                
                                if (!string.IsNullOrEmpty(targetPath) && targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
                                {
                                    var exeName = Path.GetFileName(targetPath);
                                    
                                    // Exclude installers, uninstallers, and WinNook itself
                                    if (exeName.Contains("uninstall", StringComparison.OrdinalIgnoreCase) ||
                                        exeName.Contains("setup", StringComparison.OrdinalIgnoreCase) ||
                                        exeName.Equals("Project Nook.exe", StringComparison.OrdinalIgnoreCase) ||
                                        exeName.Equals("WinNook.exe", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if (!uniqueExes.Contains(exeName))
                                    {
                                        uniqueExes.Add(exeName);
                                        
                                        var displayName = Path.GetFileNameWithoutExtension(lnk);
                                        var iconImage = IconHelper.GetIcon(targetPath);

                                        appsList.Add(new AppItem
                                        {
                                            DisplayName = displayName,
                                            ExecutableName = exeName,
                                            IconImage = iconImage
                                        });
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore shortcuts that fail to parse
                        }
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }

            return appsList;
        }
    }
}
