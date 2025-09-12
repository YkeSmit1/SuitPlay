using System.Globalization;
using Calculator;
using Calculator.Models;

namespace SuitPlay.Converters;

public class ItemToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Item2)value switch
        {
            { IsDifferent: true } => Colors.Red,
            { Tricks.Length: 1 } => Colors.White,
            _ => Colors.Green
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}