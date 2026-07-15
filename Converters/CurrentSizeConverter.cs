using System;
using System.Globalization;
using System.Windows.Data;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Converters;

public class CurrentSizeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not StepState state)
            return 22.0;

        return state == StepState.Current
            ? 32.0
            : 22.0;
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