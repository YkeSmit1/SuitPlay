using System.Globalization;
using Calculator.Models;
using SuitPlay.Pages;
using SuitPlay.ViewModels;

namespace SuitPlay.Converters;

public class LineItemToLineConverter : IValueConverter
{
    private bool DeveloperMode { get; } = Preferences.Get(Constants.DeveloperMode, true);
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LineItem lineItem)
            return "";
        return DeveloperMode ? lineItem!.Header : lineItem!.Line.MaxBy(x => x.Count()).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}