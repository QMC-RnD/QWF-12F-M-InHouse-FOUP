using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Serializers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Fate.Wpf.MVVM;
using System.Windows;
using System.Threading;
using FOUPCtrl.HardwareManager.Views.IOs;

namespace FOUPCtrl.HardwareManager.ViewModels.IOs
{
    public class IOTabViewModel : ObservableObject, ITabItem
    {
        private ControllerSerializer _controllerSerializer;
        private List<IODetailsViewModel> _ioDetailsViewModels;
        private IEnumerable<IIODriver> _ioDrivers;
        private DispatcherTimer _filterTimer;
        private bool _config;

        private string _searchTerm;

        public IOTabViewModel()
        {
            _filterTimer = new DispatcherTimer();
            _filterTimer.Interval = TimeSpan.FromMilliseconds(800);
            _filterTimer.Tick += FilterTimer_Tick;
            WireCommands();
        }

        public string Header => "I/O";

        public IMessageBoxService MessageService { get; set; } = new MessageBoxService();

        public IWindowService WindowService { get; set; } = new WindowService();

        public ICollectionView IOControllerCollection { get; set; }

        public string SearchTerm { get => _searchTerm; set => SetFilterProperty(ref _searchTerm, value); }

        public IAsyncRelayCommand PollCommand { get; set; }

        public IAsyncRelayCommand PollCancelCommand { get; set; }

        public IRelayCommand PropertiesCommand { get; set; }

        public void Init(
            ControllerSerializer controllerSerializer,
            IEnumerable<IIOController> ioControllers,
            IEnumerable<IIODriver> ioDrivers,
            bool config)
        {
            _controllerSerializer = controllerSerializer;
            _ioDrivers = ioDrivers;
            _config = config;

            _ioDetailsViewModels = new List<IODetailsViewModel>();
            foreach (var controller in ioControllers)
            {
                var vm = new IODetailsViewModel();
                vm.Init(_controllerSerializer, controller, _ioDrivers, config);
                vm.IOParameterChanged += (s, e) => IOControllerCollection.Refresh();
                _ioDetailsViewModels.Add(vm);
            }

            IOControllerCollection = CollectionViewSource.GetDefaultView(_ioDetailsViewModels);
        }

        private void WireCommands()
        {
            PollCommand = new AsyncRelayCommand<object>(
                (param, token) =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            if (param is System.Collections.IList selectedItems)
                            {
                                var selectedVMs = selectedItems.Cast<IODetailsViewModel>().ToList();
                                selectedVMs.ForEach(vm => vm.Poll = true);
                            }

                            while (!token.IsCancellationRequested)
                            {
                                var vms = _ioDetailsViewModels.Where(vm => vm.Poll && vm.IOController.Mode == IOMode.Digital).ToList();
                                if (vms.Any())
                                {
                                    foreach (var vm in vms)
                                    {
                                        vm.State = vm.IOController.DigitalRead();
                                        Thread.Sleep(5);
                                    }
                                }
                                else
                                {
                                    Thread.Sleep(5);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageService.Dispatch(e.Message, "Poll", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            _ioDetailsViewModels.ForEach(vm => vm.Poll = false);
                        }
                    });
                });

            PollCancelCommand = new AsyncRelayCommand(
                () =>
                {
                    PollCommand.Cancel();
                    return PollCommand.ExecutionTask;
                });

            PropertiesCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(
                param =>
                {
                    if (param is System.Collections.IList selectedItems)
                    {
                        if (selectedItems.Count > 0 && _config)
                        {
                            try
                            {
                                IOPropertiesViewModel vm = new IOPropertiesViewModel();
                                var selectedIOItems = ((System.Collections.IList)param).Cast<IODetailsViewModel>();
                                vm.Init(_controllerSerializer, selectedIOItems.Select(x => x.IOController), _ioDrivers);
                                WindowService.ShowDialog<IOPropertiesView>(vm);
                                if (vm.WindowResult == true)
                                {
                                    IOControllerCollection.Refresh();
                                }
                            }
                            catch (Exception e)
                            {
                                MessageService.Show(e.Message, "Properties", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }, param => /*param is System.Collections.IList selectedItems && selectedItems.Count > 0 &&*/ _config);
        }

        private void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();

            IOControllerCollection.Filter = o =>
            {
                IODetailsViewModel vm = (IODetailsViewModel)o;
                return vm.IOController.Id.IndexOf(SearchTerm ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
            };
        }

        private void SetFilterProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            SetProperty(ref field, value, propertyName);
            _filterTimer.Stop();
            _filterTimer.Start();
        }
    }
}
