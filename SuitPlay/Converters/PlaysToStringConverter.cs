﻿using System.Globalization;
using Calculator;

namespace SuitPlay.Converters;

public class PlaysToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var play = (List<Face>)value;
        return play == null ? "no play" : $"{Utils.CardsToString(play)}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}