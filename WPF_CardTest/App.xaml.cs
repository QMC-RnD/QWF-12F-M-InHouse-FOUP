using Fate.Wpf.MVVM;
using FOUPCtrl.Communication; // Add this using statement
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPF_CardTest.ViewModels;

namespace WPF_CardTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Log architecture information
            string archInfo = FOUPCtrl.Hardware.HardwareManager.GetArchitectureInfo();
            System.Diagnostics.Debug.WriteLine($"Application startup: {archInfo}");

            // Validate hardware compatibility
            FOUPCtrl.Hardware.HardwareManager.ValidateHardwareCompatibility(out string validationMessage);
            System.Diagnostics.Debug.WriteLine($"Hardware validation: {validationMessage}");

            // Run CRC16 verification tests on startup
#if DEBUG
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Running CRC16 Verification Tests ===");

                // Test CRC16 pattern
                CommandProcessor.VerifyCRC16Pattern();

                // Run comprehensive CRC16 tests
                CRC16Test.RunAllTests();

                System.Diagnostics.Debug.WriteLine("=== CRC16 Tests Completed ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRC16 Test Error: {ex.Message}");
            }
#endif

            var windowService = new WindowService();
            ViewModels.MainViewModel vm = new ViewModels.MainViewModel();
            windowService.Show<MainWindow>(vm);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("startup_error.txt", ex.ToString());
                throw;
            }
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOn && isOn)
            {
                return Colors.Green;
            }
            return Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}