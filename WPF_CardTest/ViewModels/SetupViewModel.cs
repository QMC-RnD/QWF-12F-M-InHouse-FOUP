using Fate.Wpf.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF_CardTest.ViewModels
{
    public class SetupViewModel : ViewModelBase
    {
        private string _comPort;
        public SetupViewModel()
        {
            _comPort = "COM3";
            SaveCommand = new RelayCommand(SaveComPort);
        }

        public string[] Ports { get; set; } = new string[] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6" };

        public string ComPort
        {
            get { return _comPort; }
            set => SetProperty(ref _comPort, value);
        }

        public ICommand SaveCommand { get; }
        public Action OnComPortSaved { get; set; }

        public void SaveComPort(object obj)
        {
            // Existing logic to save the COM port
            //File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comport.config"), ComPort);
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comport.config");
            File.WriteAllText(configPath, ComPort);
            // Notify the user which COM port is selected
            System.Windows.MessageBox.Show(
                $"COM port '{ComPort}' has been selected.",
                "COM Port Selected",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information
            );
            OnComPortSaved?.Invoke();
        }
    }
}