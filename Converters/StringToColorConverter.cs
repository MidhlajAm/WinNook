using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Project_Nook.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try { return ColorConverter.ConvertFromString(hex); } catch { }
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
