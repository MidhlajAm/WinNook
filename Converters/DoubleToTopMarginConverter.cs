using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Nook.Converters
{
    /// <summary>
    /// Turns the top offset into a margin that pushes the whole notch away from the screen edge.
    /// Zero keeps it flush, which is the edge-attached look the fillets are designed for.
    /// </summary>
    public class DoubleToTopMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double top = value is double d && d > 0 ? d : 0;
            return new Thickness(0, top, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
