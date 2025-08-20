using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_CardTest.Converters  // Make sure this matches the xmlns:converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Green : Brushes.Red;
            }

            // Handle integer values (0 and 1)
            if (value is int intValue)
            {
                return intValue == 1 ? Brushes.Green : Brushes.Red;
            }

            return Brushes.Red;  // Default to red for unknown values
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}