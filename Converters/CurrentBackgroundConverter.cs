using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Converters;

public class CurrentBackgroundConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not StepState state)
            return Brushes.Transparent;

        return state switch
        {
            StepState.Current => Brushes.Gold,
            StepState.Success => Brushes.DimGray,
            StepState.Failed => Brushes.IndianRed,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}