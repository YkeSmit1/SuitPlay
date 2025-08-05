using System.Globalization;
using Calculator;

namespace SuitPlay.Converters;

public class ItemToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Item item ? $"{Utils.CardsToString(item.Play.Take(7))}:{item.Tricks}" : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}