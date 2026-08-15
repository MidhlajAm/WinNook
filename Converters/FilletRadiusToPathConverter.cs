using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Project_Nook.Converters
{
    public class FilletRadiusToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double r && parameter is string side)
            {
                // Format with invariant culture to ensure correct decimal separator ('.') for Geometry.Parse
                string rStr = r.ToString(CultureInfo.InvariantCulture);
                string pathData = side.ToLowerInvariant() == "left" 
                    ? $"M 0,0 L {rStr},0 L {rStr},{rStr} A {rStr},{rStr} 0 0 0 0,0 Z"
                    : $"M 0,0 L {rStr},0 A {rStr},{rStr} 0 0 0 0,{rStr} Z";
                    
                return Geometry.Parse(pathData);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
