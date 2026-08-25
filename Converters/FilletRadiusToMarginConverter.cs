using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Nook.Converters
{
    public class FilletRadiusToMarginConverter : IValueConverter
    {
        /// <summary>
        /// How far each fillet is pulled back so it overlaps the notch instead of merely touching it.
        ///
        /// The fillets and the notch are separate visuals. Butted exactly edge to edge they land on a
        /// fractional device pixel and each anti-aliases against the desktop on its own, leaving a pale
        /// hairline seam along the join. Overlapping by a pixel removes the shared edge entirely; it is
        /// invisible because both shapes paint with the same brush instance.
        /// </summary>
        private const double Overlap = 1.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double r && parameter is string side)
            {
                double offset = Math.Max(0, r - Overlap);

                return side.ToLowerInvariant() == "left"
                    ? new Thickness(-offset, 0, 0, 0)
                    : new Thickness(0, 0, -offset, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
