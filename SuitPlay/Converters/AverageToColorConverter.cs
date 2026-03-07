using System.Globalization;
using Calculator.Models;

namespace SuitPlay.Converters;

public class AverageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isBestValue = value != null && (bool)value;
        return isBestValue ? Colors.Green : Colors.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}