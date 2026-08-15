using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Nook.Converters
{
    public class FilletRadiusToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double r && parameter is string side)
            {
                return side.ToLowerInvariant() == "left" 
                    ? new Thickness(-r, 0, 0, 0)
                    : new Thickness(0, 0, -r, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
