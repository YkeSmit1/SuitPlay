using System.Globalization;
using Calculator.Models;

namespace SuitPlay.Converters;

public class ItemToTricksConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var item = (Item2)value;
        var tricksInSuitPlay = item != null && item.Tricks.Length > 1 ? $"({item.TricksInSuitPlay})" : "";
        return item != null ? $"{string.Join(',', item.Tricks)}" + tricksInSuitPlay : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}