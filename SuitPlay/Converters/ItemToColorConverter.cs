using System.Globalization;
using Calculator;

namespace SuitPlay.Converters;

public class ItemToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Calculate.Item item)
            return item.Line switch
            {
                0 => Colors.Red,
                1 => Colors.DarkRed,
                2 => Colors.Green,
                3 => Colors.Aqua,
                4 => Colors.Yellow,
                5 => Colors.Orange,
                6 => Colors.Purple,
                7 => Colors.Brown,
                8 => Colors.DarkOrchid,
                _ => Colors.White
            };
        return Colors.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}