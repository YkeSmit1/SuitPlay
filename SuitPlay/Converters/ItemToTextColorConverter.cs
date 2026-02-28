using System.Globalization;
using Calculator;
using Calculator.Models;
using SuitPlay.ViewModels;

namespace SuitPlay.Converters;

public class ItemToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var item = (Item2)value;
        if (item == null)
            return Colors.White;
        var viewModel = (Distributions2ViewModel)((Binding)parameter)?.Source;
        var developerMode = viewModel?.DeveloperMode == true;
        if (!developerMode)
            return Colors.White;
        return item switch
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