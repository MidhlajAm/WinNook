using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Project_Nook.Converters
{
    public class PathToImageConverter : IValueConverter
    {
        /// <summary>Matches the 64px shelf tile, with headroom for high-DPI displays.</summary>
        private const int ThumbnailWidth = 128;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Crucial: don't lock the file
                    // These are 64px thumbnails. Without this a shelf of screenshots decodes every one at
                    // full resolution and holds it in memory: a 4K PNG costs ~33MB as a live bitmap.
                    bitmap.DecodePixelWidth = ThumbnailWidth;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze(); // Make it cross-thread safe and optimized
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
