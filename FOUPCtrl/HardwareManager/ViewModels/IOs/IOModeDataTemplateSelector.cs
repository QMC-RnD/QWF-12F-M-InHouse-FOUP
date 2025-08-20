using FOUPCtrl.HardwareManager.Controllers.IOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FOUPCtrl.HardwareManager.ViewModels.IOs
{
    public class IOModeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is IOMode mode)
            {
                return element.FindResource(mode.ToString()) as DataTemplate;
            }

            return null;
        }
    }
}
