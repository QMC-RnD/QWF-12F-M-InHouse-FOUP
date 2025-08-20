//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Data;
//using System.Windows.Media;

//namespace FOUPCtrl.HardwareManager.Converters
//{
//    public class CassetteCharToColorConverter : IValueConverter
//    {
//        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//        {
//            if (value is CassetteStatus status)
//            {
//                switch (status)
//                {
//                    case CassetteStatus.Empty: return Brushes.Transparent;
//                    case CassetteStatus.Presence: return Brushes.LawnGreen;
//                    case CassetteStatus.Thick: return Brushes.DarkMagenta;
//                    case CassetteStatus.Thin: return Brushes.Pink;
//                    case CassetteStatus.CrossSlot:
//                    case CassetteStatus.Double:
//                    case CassetteStatus.PositionError:
//                    default:
//                        return Brushes.Red;

//                }
//            }
//            else return Brushes.Transparent;
//        }

//        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
