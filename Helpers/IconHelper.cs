using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Project_Nook.Helpers
{
    public static class IconHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource? GetIcon(string filePath)
        {
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    // Freeze for cross-thread access if needed
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
            }
            catch
            {
                // Ignore exceptions during icon extraction
            }
            return null;
        }
    }
}
