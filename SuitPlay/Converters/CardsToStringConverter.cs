using System.Globalization;
using Calculator;

namespace SuitPlay.Converters;

public class CardsToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Utils.CardListToString((List<Face>)(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}