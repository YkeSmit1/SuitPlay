using System.Globalization;
using Calculator.Models;
using SuitPlay.Pages;
using SuitPlay.ViewModels;

namespace SuitPlay.Converters;

public class LineItemToLineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var lineItem = (LineItem)value;
        if (lineItem == null)
            return "";
        var viewModel = (Distributions2ViewModel)((Binding)parameter)?.Source;
        var developerMode = viewModel?.DeveloperMode == true;
        return developerMode ? lineItem!.Header : lineItem!.Line.MinBy(x => x.Count()).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}