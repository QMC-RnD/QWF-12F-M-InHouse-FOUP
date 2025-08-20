using CommunityToolkit.Mvvm.Input;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Serializers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Fate.Wpf.MVVM;
using System.Windows;

namespace FOUPCtrl.HardwareManager.ViewModels.IOs
{
    public class IOPropertiesViewModel : ViewModelBase
    {
        private ControllerSerializer _controllerSerializer;
        private IEnumerable<IIOController> _ioControllers;

        private IIODriver _ioDriver;

        public IOPropertiesViewModel()
        {
            WireCommands();
        }

        public IMessageBoxService MessageService { get; set; } = new MessageBoxService();

        public ICollectionView IODriverCollection { get; set; }

        public IIODriver IODriver { get => _ioDriver; set => SetProperty(ref _ioDriver, value); }

        public IRelayCommand ClearDriverCommand { get; set; }

        public IRelayCommand SaveCommand { get; set; }

        public IRelayCommand CloseCommand { get; set; }

        public void Init(
            ControllerSerializer controllerSerializer,
            IEnumerable<IIOController> ioControllers,
            IEnumerable<IIODriver> ioDrivers)
        {
            _controllerSerializer = controllerSerializer;
            _ioControllers = ioControllers;

            IODriverCollection = CollectionViewSource.GetDefaultView(ioDrivers);
            IODriverCollection.Refresh();
        }

        private void WireCommands()
        {
            ClearDriverCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                () =>
                {
                    IODriver = null;
                });

            SaveCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                () =>
                {
                    try
                    {
                        bool? result = MessageService.Show(
                            $"Are you sure you want to save I/O Properties?",
                            "Save",
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Question);
                        if (result == true)
                        {
                            foreach (var controller in _ioControllers)
                            {
                                controller.SetDriver(IODriver);
                                _controllerSerializer.Serialize(controller);
                            }

                            MessageService.Show(
                                $"Successfully saved I/O Properties.",
                                "Save",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            Close(true);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageService.Show(e.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            CloseCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Close());
        }
    }
}
