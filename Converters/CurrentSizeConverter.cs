using System;
using System.Globalization;
using System.Windows.Data;

namespace WuwaQuickSwapHelper.Converters
{
	public class CurrentSizeConverter : IValueConverter
	{
		public object Convert(
			object value,
			Type targetType,
			object parameter,
			CultureInfo culture)
		{
			bool isCurrent = (bool)value;

			return isCurrent ? 32 : 22;
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
}