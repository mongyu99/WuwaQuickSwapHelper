using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Converters;

public class CurrentBackgroundConverter : IValueConverter
{
    private static SolidColorBrush Soft(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

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
            StepState.Current => Soft(0x9D, 0x8C, 0xFF),   // 스왑 키: 라벤더
            StepState.Success => Soft(0x3A, 0x3D, 0x52),
            StepState.Failed => Soft(0xF2, 0x8B, 0x9A),
            _ => Soft(0x44, 0x48, 0x62)                     // 일반 키
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