using CommunityToolkit.Mvvm.Input;
using Fate.Wpf.MVVM;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Serializers;
using FOUPCtrl.HardwareManager.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;


namespace FOUPCtrl.HardwareManager.ViewModels.IOs
{
    public class IOConfigViewModel : ViewModelBase
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private ControllerSerializer _controllerSerializer;
        private IIOController _ioController;

        private IIODriver _ioDriver;

        public IOConfigViewModel()
        {
            WireCommands();
        }

        public IMessageBoxService MessageService { get; set; } = new MessageBoxService();

        public IIOController IOControllerParameter { get; set; }

        public ICollectionView IODriverCollection { get; set; }

        public IIODriver IODriver { get => _ioDriver; set => SetProperty(ref _ioDriver, value); }

        public IRelayCommand ClearDriverCommand { get; set; }

        public IRelayCommand SaveCommand { get; set; }

        public IRelayCommand CloseCommand { get; set; }

        private string _title;

        public string Title { get => _title; set => SetProperty(ref _title, value); }


        public void Init(
            ControllerSerializer controllerSerializer,
            IIOController ioController,
            IEnumerable<IIODriver> ioDrivers)
        {
            _controllerSerializer = controllerSerializer;
            _ioController = ioController;
            IODriver = _ioController.IODriver;

            IOControllerParameter = (IIOController)Activator.CreateInstance(_ioController.GetType(), new[] { _ioController.Id });
            _ioController.Copy(IOControllerParameter, typeof(DataMemberAttribute));

            IODriverCollection = CollectionViewSource.GetDefaultView(ioDrivers);
            IODriverCollection.Refresh();

            Title = "[" + IOControllerParameter.Id + "] " + FOUPCtrl.Properties.Resources.HWM_IOConfigView_Title;
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
                            $"Are you sure you want to save {IOControllerParameter.Id} configuration?",
                            "Save",
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Question);
                        if (result == true)
                        {
                            IOControllerParameter.Copy(_ioController, typeof(DataMemberAttribute));
                            _ioController.SetDriver(IODriver);
                            _controllerSerializer.Serialize(_ioController);

                            _logger?.Info("I/O configuration:{0} saved:{1}.", IOControllerParameter.Id, GetParameterList(IOControllerParameter));
                            MessageService.Show(
                                $"Successfully saved {IOControllerParameter.Id} configuration.",
                                "Save",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, "I/O configuration: {0}.", IOControllerParameter.Id);
                        MessageService.Show(e.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            CloseCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Close());
        }

        private string GetParameterList(IIOController ioController)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"\r\n\t{nameof(ioController.DriverId)}:{ioController.DriverId}");
            sb.Append($"\r\n\t{nameof(ioController.Mode)}:{ioController.Mode}");
            sb.Append($"\r\n\t{nameof(ioController.IsReadOnly)}:{ioController.IsReadOnly}");
            sb.Append($"\r\n\t{nameof(ioController.Port)}:{ioController.Port}");
            sb.Append($"\r\n\t{nameof(ioController.Bit)}:{ioController.Bit}");
            sb.Append($"\r\n\t{nameof(ioController.DelayAfterWrite)}:{ioController.DelayAfterWrite}");
            return sb.ToString();
        }
    }
}