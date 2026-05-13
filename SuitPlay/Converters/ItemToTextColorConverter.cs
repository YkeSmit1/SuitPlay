using System.Globalization;
using Calculator;
using Calculator.Models;
using SuitPlay.ViewModels;

namespace SuitPlay.Converters;

public class ItemToTextColorConverter : IValueConverter
{
    private bool DeveloperMode { get; } = Preferences.Get(Constants.DeveloperMode, true);
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var item = (Item2)value;
        if (item == null)
            return null;
        if (!DeveloperMode)
            return null;
        return item switch
        {
            { IsDifferent: true } => Colors.Red,
            { Tricks.Length: 1 } => null,
            _ => Colors.Green
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}