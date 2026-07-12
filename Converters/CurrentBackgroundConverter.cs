using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WuwaQuickSwapHelper.Converters;


public class CurrentBackgroundConverter : IValueConverter
{

    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {

        bool isCurrent = (bool)value;


        return isCurrent
            ? Brushes.DarkOrange
            : Brushes.DimGray;

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