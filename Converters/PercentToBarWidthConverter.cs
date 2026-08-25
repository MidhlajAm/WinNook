using System;
using System.Globalization;
using System.Windows.Data;

namespace Project_Nook.Converters
{
    /// <summary>
    /// Maps a 0-100 percentage onto the width of a fixed-length bar. The track is 40px wide, matching
    /// StatBarTrackStyle; pass a different length as the converter parameter to reuse it elsewhere.
    /// </summary>
    public class PercentToBarWidthConverter : IValueConverter
    {
        private const double DefaultTrackWidth = 40.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double track = DefaultTrackWidth;
            if (parameter is string raw &&
                double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) &&
                parsed > 0)
            {
                track = parsed;
            }

            double percent = value switch
            {
                int i => i,
                double d => d,
                _ => 0
            };

            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            return track * percent / 100.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
