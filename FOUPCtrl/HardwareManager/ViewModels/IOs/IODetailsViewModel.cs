using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fate.Wpf.MVVM;
using FOUPCtrl.HardwareManager.Utilities;
using System.Windows;
using FOUPCtrl.HardwareManager.Views.IOs;

namespace FOUPCtrl.HardwareManager.ViewModels.IOs
{
    public class IODetailsViewModel : ObservableObject
    {
        private ControllerSerializer _controllerSerializer;
        private IEnumerable<IIODriver> _ioDrivers;
        private bool _config;

        private bool _poll;
        private bool _state;
        private int _analogValue;

        public IODetailsViewModel()
        {
            WireCommands();
        }

        public event EventHandler IOParameterChanged;

        public IMessageBoxService MessageService { get; set; } = new MessageBoxService();

        public IWindowService WindowService { get; set; } = new WindowService();

        public IIOController IOController { get; private set; }

        public bool Poll { get => _poll; set => SetProperty(ref _poll, value); }

        public bool IsWriteEnabled { get => !IOController?.IsReadOnly ?? false; }

        public bool State { get => _state; set => SetProperty(ref _state, value); }

        public int AnalogValue { get => _analogValue; set => SetProperty(ref _analogValue, value); }

        public IRelayCommand OnCommand { get; set; }

        public IRelayCommand OffCommand { get; set; }

        public IRelayCommand AnalogReadCommand { get; set; }

        public IRelayCommand AnalogWriteCommand { get; set; }

        public IAsyncRelayCommand ConfigCommand { get; set; }

        public void Init(
            ControllerSerializer controllerSerializer,
            IIOController ioController,
            IEnumerable<IIODriver> ioDrivers,
            bool config)
        {
            _controllerSerializer = controllerSerializer;
            IOController = ioController;
            _ioDrivers = ioDrivers;
            _config = config;
        }

        private void WireCommands()
        {
            OnCommand = new AsyncRelayCommand(
                async token =>
                {
                    try
                    {
                        await IOController.DigitalWriteAsync(true, token);
                        State = true;
                    }
                    catch (Exception e)
                    {
                        MessageService.Show(e.Message, "ON", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            OffCommand = new AsyncRelayCommand(
                async token =>
                {
                    try
                    {
                        await IOController.DigitalWriteAsync(false, token);
                        State = false;
                    }
                    catch (Exception e)
                    {
                        MessageService.Show(e.Message, "OFF", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            AnalogReadCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                () =>
                {
                    try
                    {
                        AnalogValue = IOController.AnalogRead();
                    }
                    catch (Exception e)
                    {
                        MessageService.Show(e.Message, "Read", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            AnalogWriteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                () =>
                {
                    try
                    {
                        IOController.AnalogWrite(AnalogValue);
                    }
                    catch (Exception e)
                    {
                        MessageService.Show(e.Message, "Write", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            ConfigCommand = new AsyncRelayCommand<object>(
                async (param, token) =>
                {
                    if (_config)
                    {
                        try
                        {
                            IOConfigViewModel vm = new IOConfigViewModel();
                            vm.Init(_controllerSerializer, IOController, _ioDrivers);
                            await WindowService.ShowWindow<IOConfigView>(vm, token);
                            IOParameterChanged?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception e)
                        {
                            MessageService.Show(e.Message, "Config", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }, param => _config);
        }
    }
}
