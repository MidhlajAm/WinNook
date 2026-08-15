using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace Project_Nook.Converters
{
    public class TypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type switch
                {
                    "Image" => SymbolRegular.Image24,
                    "Video" => SymbolRegular.Video24,
                    "Audio" => SymbolRegular.MusicNote124,
                    "Text" => SymbolRegular.DocumentText24,
                    "Archive" => SymbolRegular.FolderZip24,
                    "Code" => SymbolRegular.Code24,
                    _ => SymbolRegular.Document24
                };
            }
            return SymbolRegular.Document24;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
