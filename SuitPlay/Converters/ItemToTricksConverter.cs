using System.Globalization;
using Calculator.Models;
using SuitPlay.ViewModels;

namespace SuitPlay.Converters;

public class ItemToTricksConverter : IValueConverter
{
    private bool DeveloperMode { get; } = Preferences.Get(Constants.DeveloperMode, true);
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var item = (Item2)value;
        if (item == null)
            return "";
        
        var tricksInSuitPlay = item.Tricks.Length > 1 ? $"({item.TricksInSuitPlay})" : "";
        return DeveloperMode ? $"{string.Join(',', item.Tricks)}" + tricksInSuitPlay : item.Tricks.Max().ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}