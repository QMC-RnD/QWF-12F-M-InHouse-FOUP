using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FOUPCtrl.HardwareManager.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // Check if the string is empty (or only contains whitespace)
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return Visibility.Visible; // Show the element
                }
            }

            // If the string is not empty, or if it's not a string, hide the element
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
