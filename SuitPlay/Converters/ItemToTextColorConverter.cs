using System.Globalization;
using Calculator;

namespace SuitPlay.Converters;

public class ItemToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Calculate.Item2)value switch
        {
            { IsDifferent: true, IsSubstitute: true } => Colors.Purple,
            { IsDifferent: true } => Colors.Green,
            { IsSubstitute: true } => Colors.Red,
            _ => Colors.White
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}