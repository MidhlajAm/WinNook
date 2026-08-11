using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Nook.Converters
{
    public class DoubleToBottomCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double radius)
            {
                // Notch only rounds the bottom corners!
                return new CornerRadius(0, 0, radius, radius);
            }
            return new CornerRadius(0, 0, 24, 24);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
