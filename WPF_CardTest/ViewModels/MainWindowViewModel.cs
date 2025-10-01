//#if x64
//using Creden.Hardware64.Cards;
//#else
//using Creden.Hardware.Cards;
//#endif
using Fate.Wpf.MVVM;
using FoupControl;
using FOUPCtrl;
using FOUPCtrl.Hardware;
using FOUPCtrl.Models;
using FOUPCtrl.Services;
using FOUPCtrl.TCPServer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static FoupControl.FOUP_Ctrl;
// Add alias to resolve CardStatus ambiguity
//using CredenCardStatus = Creden.Hardware.Cards.CardStatus;
//using FOUPCardStatus = FOUPCtrl.Hardware.CardStatus;

namespace WPF_CardTest.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Services
        private readonly FOUPTrainingService _trainingService;
        private readonly FOUPMappingService _mappingService;
        private readonly FOUPDeviceManager _deviceManager;
        private readonly FOUPCommunicationService _communicationService;
        #endregion

        #region Constants
        private const int EXPECTED_SLOTS = 25;
        #endregion

        #region Fields - Core System
        private FOUP_Ctrl _foupCtrl;
        private CancellationTokenSource _cts;
        private Task _statusPollingTask;
        #endregion

        #region Fields - Connection Status
        private string _status;
        private string _connectionIndication = "OFF-LINE";
        private ObservableCollection<string> _deviceIds;
        private string _selectedDeviceId1;
        private string _selectedDeviceId2;
        private ObservableCollection<string> _comPorts;
        private string _selectedComPort;
        private bool _isUsbSelected;
        private bool _isRs485Selected;
        private bool _isDisconnected = true;
        private bool _isConnected;
        private bool _serverStarted;
        private string _m_status;
        private string _motion;
        #endregion

        #region Fields - Device Status
        private string _deviceStatus;
        private string _deviceClampStatus;
        private string _deviceLatchStatus;
        private string _deviceElevatorStatus;
        private string _deviceDoorStatus;
        private string _deviceDockStatus;
        private string _deviceMappingStatus;
        private string _deviceVacuumStatus;
        #endregion

        #region Fields - Calibration Status
        private bool _type1CalibrationCompleted;
        private bool _type2CalibrationCompleted;
        private bool _type3CalibrationCompleted;
        private bool _type4CalibrationCompleted;
        private bool _type5CalibrationCompleted;

        private bool _type1CalibrationSelected;
        private bool _type2CalibrationSelected;
        private bool _type3CalibrationSelected;
        private bool _type4CalibrationSelected;
        private bool _type5CalibrationSelected;

        private bool _type1CalibrationStatus;
        private bool _type2CalibrationStatus;
        private bool _type3CalibrationStatus;
        private bool _type4CalibrationStatus;
        private bool _type5CalibrationStatus;
        #endregion

        #region Fields - Button States
        private bool _clampEnabled = true;
        private bool _unclampEnabled = true;
        private bool _latchEnabled = true;
        private bool _unlatchEnabled = true;
        private bool _elevatorUpEnabled = true;
        private bool _elevatorDownEnabled = true;
        private bool _doorForwardEnabled = true;
        private bool _doorBackwardEnabled = true;
        private bool _dockForwardEnabled = true;
        private bool _dockBackwardEnabled = true;
        private bool _mappingForwardEnabled = true;
        private bool _mappingBackwardEnabled = true;
        private bool _vacuumOnEnabled = true;
        private bool _vacuumOffEnabled = true;
        #endregion

        #region Fields - Mapping & Calibration
        private int _activeProfileIndex;
        private ObservableCollection<MappingResult> _waferMappingResult;
        private int _selectedTypeIndex = 0;
        #endregion

        #region Fields - Server Communication
        private ObservableCollection<string> _listMsgReceived;
        private ObservableCollection<string> _listMsgSent;
        private string _serverIP;
        private string _serverPort;
        private bool _canEditServerSettings = true;
        #endregion

        #region Fields - UI Components
        private string _currentComPort;
        private int _selectedFOUPTypeIndex;
        private int _positionTableIndexForFoupType;
        private int _mappingTableIndexForFoupType;
        private int _sequenceIndexForFoupType;
        private int _selectedPositionTableIndex;
        private int _selectedMappingTableIndex;
        private int _selectedSequenceIndex;
        private int _selectedCardIndex = 0;
        private ObservableCollection<FOUP_Ctrl.IOBitStatus> _inputBits = new ObservableCollection<FOUP_Ctrl.IOBitStatus>();
        private ObservableCollection<FOUP_Ctrl.IOBitStatus> _outputBits = new ObservableCollection<FOUP_Ctrl.IOBitStatus>();
        #endregion

        #region LED Selection Properties
        private bool _isPresenceSelected;
        private bool _isPlacementSelected;
        private bool _isStatus1Selected;
        private bool _isStatus2Selected;
        private bool _isStatus3Selected;
        private bool _isStatus4Selected;
        private bool _isLoadSelected;
        private bool _isUnloadSelected;
        private bool _isAlarmSelected;
        private bool _isManualSW1Selected;
        private bool _isManualSW2Selected;

        public bool IsPresenceSelected
        {
            get => _isPresenceSelected;
            set
            {
                if (SetProperty(ref _isPresenceSelected, value) && value)
                {
                    SelectedLedIndex = 0;
                    ClearOtherLedSelections(0);
                }
            }
        }

        public bool IsPlacementSelected
        {
            get => _isPlacementSelected;
            set
            {
                if (SetProperty(ref _isPlacementSelected, value) && value)
                {
                    SelectedLedIndex = 1;
                    ClearOtherLedSelections(1);
                }
            }
        }

        public bool IsStatus1Selected
        {
            get => _isStatus1Selected;
            set
            {
                if (SetProperty(ref _isStatus1Selected, value) && value)
                {
                    SelectedLedIndex = 2;
                    ClearOtherLedSelections(2);
                }
            }
        }

        public bool IsStatus2Selected
        {
            get => _isStatus2Selected;
            set
            {
                if (SetProperty(ref _isStatus2Selected, value) && value)
                {
                    SelectedLedIndex = 3;
                    ClearOtherLedSelections(3);
                }
            }
        }

        public bool IsStatus3Selected
        {
            get => _isStatus3Selected;
            set
            {
                if (SetProperty(ref _isStatus3Selected, value) && value)
                {
                    SelectedLedIndex = 4;
                    ClearOtherLedSelections(4);
                }
            }
        }

        public bool IsStatus4Selected
        {
            get => _isStatus4Selected;
            set
            {
                if (SetProperty(ref _isStatus4Selected, value) && value)
                {
                    SelectedLedIndex = 5;
                    ClearOtherLedSelections(5);
                }
            }
        }

        public bool IsLoadSelected
        {
            get => _isLoadSelected;
            set
            {
                if (SetProperty(ref _isLoadSelected, value) && value)
                {
                    SelectedLedIndex = 6;
                    ClearOtherLedSelections(6);
                }
            }
        }

        public bool IsUnloadSelected
        {
            get => _isUnloadSelected;
            set
            {
                if (SetProperty(ref _isUnloadSelected, value) && value)
                {
                    SelectedLedIndex = 7;
                    ClearOtherLedSelections(7);
                }
            }
        }

        public bool IsAlarmSelected
        {
            get => _isAlarmSelected;
            set
            {
                if (SetProperty(ref _isAlarmSelected, value) && value)
                {
                    SelectedLedIndex = 8;
                    ClearOtherLedSelections(8);
                }
            }
        }

        public bool IsManualSW1Selected
        {
            get => _isManualSW1Selected;
            set
            {
                if (SetProperty(ref _isManualSW1Selected, value) && value)
                {
                    SelectedLedIndex = 9;
                    ClearOtherLedSelections(9);
                }
            }
        }

        public bool IsManualSW2Selected
        {
            get => _isManualSW2Selected;
            set
            {
                if (SetProperty(ref _isManualSW2Selected, value) && value)
                {
                    SelectedLedIndex = 10;
                    ClearOtherLedSelections(10);
                }
            }
        }

        private void ClearOtherLedSelections(int exceptIndex)
        {
            if (exceptIndex != 0) _isPresenceSelected = false;
            if (exceptIndex != 1) _isPlacementSelected = false;
            if (exceptIndex != 2) _isStatus1Selected = false;
            if (exceptIndex != 3) _isStatus2Selected = false;
            if (exceptIndex != 4) _isStatus3Selected = false;
            if (exceptIndex != 5) _isStatus4Selected = false;
            if (exceptIndex != 6) _isLoadSelected = false;
            if (exceptIndex != 7) _isUnloadSelected = false;
            if (exceptIndex != 8) _isAlarmSelected = false;
            if (exceptIndex != 9) _isManualSW1Selected = false;
            if (exceptIndex != 10) _isManualSW2Selected = false;

            // Notify all properties
            OnPropertyChanged(nameof(IsPresenceSelected));
            OnPropertyChanged(nameof(IsPlacementSelected));
            OnPropertyChanged(nameof(IsStatus1Selected));
            OnPropertyChanged(nameof(IsStatus2Selected));
            OnPropertyChanged(nameof(IsStatus3Selected));
            OnPropertyChanged(nameof(IsStatus4Selected));
            OnPropertyChanged(nameof(IsLoadSelected));
            OnPropertyChanged(nameof(IsUnloadSelected));
            OnPropertyChanged(nameof(IsAlarmSelected));
            OnPropertyChanged(nameof(IsManualSW1Selected));
            OnPropertyChanged(nameof(IsManualSW2Selected));
        }
        #endregion

        #region LED Properties
        private bool _ledPresenceOn;
        private bool _ledPlacementOn;
        private bool _ledStatus1On;
        private bool _ledStatus2On;
        private bool _ledStatus3On;
        private bool _ledStatus4On;
        private bool _ledLoadOn;
        private bool _ledUnloadOn;
        private bool _ledAlarmOn;
        private bool _ledManualSW1On;
        private bool _ledManualSW2On;

        public bool LedPresenceOn { get => _ledPresenceOn; set => SetProperty(ref _ledPresenceOn, value); }
        public bool LedPlacementOn { get => _ledPlacementOn; set => SetProperty(ref _ledPlacementOn, value); }
        public bool LedStatus1On { get => _ledStatus1On; set => SetProperty(ref _ledStatus1On, value); }
        public bool LedStatus2On { get => _ledStatus2On; set => SetProperty(ref _ledStatus2On, value); }
        public bool LedStatus3On { get => _ledStatus3On; set => SetProperty(ref _ledStatus3On, value); }
        public bool LedStatus4On { get => _ledStatus4On; set => SetProperty(ref _ledStatus4On, value); }
        public bool LedLoadOn { get => _ledLoadOn; set => SetProperty(ref _ledLoadOn, value); }
        public bool LedUnloadOn { get => _ledUnloadOn; set => SetProperty(ref _ledUnloadOn, value); }
        public bool LedAlarmOn { get => _ledAlarmOn; set => SetProperty(ref _ledAlarmOn, value); }
        public bool LedManualSW1On { get => _ledManualSW1On; set => SetProperty(ref _ledManualSW1On, value); }
        public bool LedManualSW2On { get => _ledManualSW2On; set => SetProperty(ref _ledManualSW2On, value); }

        private int _selectedLedIndex = 0;
        public int SelectedLedIndex
        {
            get => _selectedLedIndex;
            set => SetProperty(ref _selectedLedIndex, value);
        }
        #endregion

        #region LED Commands
        public ICommand LedOnCommand { get; set; }
        public ICommand LedBlinkCommand { get; set; }
        public ICommand LedOffCommand { get; set; }
        #endregion

        #region Properties - Connection Status
        public bool ServerStarted { get => _serverStarted; set => SetProperty(ref _serverStarted, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public string ConnectionIndication { get => _connectionIndication; set => SetProperty(ref _connectionIndication, value); }
        public ObservableCollection<string> DeviceIds { get => _deviceIds; set => SetProperty(ref _deviceIds, value); }
        public string SelectedDeviceId1 { get => _selectedDeviceId1; set => SetProperty(ref _selectedDeviceId1, value); }
        public string SelectedDeviceId2 { get => _selectedDeviceId2; set => SetProperty(ref _selectedDeviceId2, value); }
        public ObservableCollection<string> ComPorts { get => _comPorts; set => SetProperty(ref _comPorts, value); }
        public string SelectedComPort { get => _selectedComPort; set => SetProperty(ref _selectedComPort, value); }
        public bool IsUsbSelected { get => _isUsbSelected; set => SetProperty(ref _isUsbSelected, value); }
        public bool IsRs485Selected { get => _isRs485Selected; set => SetProperty(ref _isRs485Selected, value); }
        public bool IsDisconnected { get => _isDisconnected; set => SetProperty(ref _isDisconnected, value); }
        public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }
        public string m_Status { get => _m_status; set => SetProperty(ref _m_status, value); }
        public string Motion { get => _motion; set => SetProperty(ref _motion, value); }
        #endregion

        #region Properties - Device Status
        public string DeviceStatus { get => _deviceStatus; set => SetProperty(ref _deviceStatus, value); }
        public string DeviceClampStatus { get => _deviceClampStatus; set => SetProperty(ref _deviceClampStatus, value); }
        public string DeviceLatchStatus { get => _deviceLatchStatus; set => SetProperty(ref _deviceLatchStatus, value); }
        public string DeviceElevatorStatus { get => _deviceElevatorStatus; set => SetProperty(ref _deviceElevatorStatus, value); }
        public string DeviceDoorStatus { get => _deviceDoorStatus; set => SetProperty(ref _deviceDoorStatus, value); }
        public string DeviceDockStatus { get => _deviceDockStatus; set => SetProperty(ref _deviceDockStatus, value); }
        public string DeviceMappingStatus { get => _deviceMappingStatus; set => SetProperty(ref _deviceMappingStatus, value); }
        public string DeviceVacuumStatus { get => _deviceVacuumStatus; set => SetProperty(ref _deviceVacuumStatus, value); }
        #endregion

        #region Properties - Button States
        public bool ClampEnabled { get => _clampEnabled; set => SetProperty(ref _clampEnabled, value); }
        public bool UnclampEnabled { get => _unclampEnabled; set => SetProperty(ref _unclampEnabled, value); }
        public bool LatchEnabled { get => _latchEnabled; set => SetProperty(ref _latchEnabled, value); }
        public bool UnlatchEnabled { get => _unlatchEnabled; set => SetProperty(ref _unlatchEnabled, value); }
        public bool ElevatorUpEnabled { get => _elevatorUpEnabled; set => SetProperty(ref _elevatorUpEnabled, value); }
        public bool ElevatorDownEnabled { get => _elevatorDownEnabled; set => SetProperty(ref _elevatorDownEnabled, value); }
        public bool DoorForwardEnabled { get => _doorForwardEnabled; set => SetProperty(ref _doorForwardEnabled, value); }
        public bool DoorBackwardEnabled { get => _doorBackwardEnabled; set => SetProperty(ref _doorBackwardEnabled, value); }
        public bool DockForwardEnabled { get => _dockForwardEnabled; set => SetProperty(ref _dockForwardEnabled, value); }
        public bool DockBackwardEnabled { get => _dockBackwardEnabled; set => SetProperty(ref _dockBackwardEnabled, value); }
        public bool MappingForwardEnabled { get => _mappingForwardEnabled; set => SetProperty(ref _mappingForwardEnabled, value); }
        public bool MappingBackwardEnabled { get => _mappingBackwardEnabled; set => SetProperty(ref _mappingBackwardEnabled, value); }
        public bool VacuumOnEnabled { get => _vacuumOnEnabled; set => SetProperty(ref _vacuumOnEnabled, value); }
        public bool VacuumOffEnabled { get => _vacuumOffEnabled; set => SetProperty(ref _vacuumOffEnabled, value); }
        #endregion

        #region Properties - Calibration
        public bool Type1CalibrationCompleted { get => _type1CalibrationCompleted; set => SetProperty(ref _type1CalibrationCompleted, value); }
        public bool Type2CalibrationCompleted { get => _type2CalibrationCompleted; set => SetProperty(ref _type2CalibrationCompleted, value); }
        public bool Type3CalibrationCompleted { get => _type3CalibrationCompleted; set => SetProperty(ref _type3CalibrationCompleted, value); }
        public bool Type4CalibrationCompleted { get => _type4CalibrationCompleted; set => SetProperty(ref _type4CalibrationCompleted, value); }
        public bool Type5CalibrationCompleted { get => _type5CalibrationCompleted; set => SetProperty(ref _type5CalibrationCompleted, value); }

        public bool Type1CalibrationSelected { get => _type1CalibrationSelected; set { if (SetProperty(ref _type1CalibrationSelected, value)) UpdateCalibrationStatus(); } }
        public bool Type2CalibrationSelected { get => _type2CalibrationSelected; set { if (SetProperty(ref _type2CalibrationSelected, value)) UpdateCalibrationStatus(); } }
        public bool Type3CalibrationSelected { get => _type3CalibrationSelected; set { if (SetProperty(ref _type3CalibrationSelected, value)) UpdateCalibrationStatus(); } }
        public bool Type4CalibrationSelected { get => _type4CalibrationSelected; set { if (SetProperty(ref _type4CalibrationSelected, value)) UpdateCalibrationStatus(); } }
        public bool Type5CalibrationSelected { get => _type5CalibrationSelected; set { if (SetProperty(ref _type5CalibrationSelected, value)) UpdateCalibrationStatus(); } }

        public bool Type1CalibrationStatus { get => _type1CalibrationStatus; set => SetProperty(ref _type1CalibrationStatus, value); }
        public bool Type2CalibrationStatus { get => _type2CalibrationStatus; set => SetProperty(ref _type2CalibrationStatus, value); }
        public bool Type3CalibrationStatus { get => _type3CalibrationStatus; set => SetProperty(ref _type3CalibrationStatus, value); }
        public bool Type4CalibrationStatus { get => _type4CalibrationStatus; set => SetProperty(ref _type4CalibrationStatus, value); }
        public bool Type5CalibrationStatus { get => _type5CalibrationStatus; set => SetProperty(ref _type5CalibrationStatus, value); }
        #endregion

        #region Properties - Mapping & Settings
        public ObservableCollection<MappingResult> WaferMappingResult { get => _waferMappingResult; set => SetProperty(ref _waferMappingResult, value); }

        public int ActiveProfileIndex
        {
            get => _activeProfileIndex;
            set
            {
                if (SetProperty(ref _activeProfileIndex, value))
                {
                    Settings.Instance.ActiveMappingType = value + 1;
                    OnPropertyChanged(nameof(IsType1Selected));
                    OnPropertyChanged(nameof(IsType2Selected));
                    OnPropertyChanged(nameof(IsType3Selected));
                    OnPropertyChanged(nameof(IsType4Selected));
                    OnPropertyChanged(nameof(IsType5Selected));
                    OnPropertyChanged(nameof(CurrentProfile));
                }
            }
        }

        public Settings Settings => Settings.Instance;
        public MappingTypeProfile CurrentProfile => Settings.Instance.CurrentProfile;
        public string[] MotionList { get; set; } = new string[] { "Load", "Unload", "Load (map)", "Unload (map)", "MAP ACAL" };

        public bool IsType1Selected => ActiveProfileIndex == 0;
        public bool IsType2Selected => ActiveProfileIndex == 1;
        public bool IsType3Selected => ActiveProfileIndex == 2;
        public bool IsType4Selected => ActiveProfileIndex == 3;
        public bool IsType5Selected => ActiveProfileIndex == 4;
        #endregion

        #region Properties - Server Communication
        public ObservableCollection<string> ListMsgReceived{ get => _communicationService?.ListMsgReceived ?? _listMsgReceived; set => SetProperty(ref _listMsgReceived, value); }
        public ObservableCollection<string> ListMsgSent{ get => _communicationService?.ListMsgSent ?? _listMsgSent; set => SetProperty(ref _listMsgSent, value); }
        public string ServerIP { get => _serverIP; set => SetProperty(ref _serverIP, value); }
        public string ServerPort { get => _serverPort; set => SetProperty(ref _serverPort, value); }
        public bool CanEditServerSettings { get => _canEditServerSettings; set => SetProperty(ref _canEditServerSettings, value); }
        #endregion

        #region Properties - UI Components
        public SetupViewModel SetupViewModel { get; set; }
        public IWindowService WindowService { get; set; } = new WindowService();
        public IMessageBoxService MessageService { get; set; } = new MessageBoxService();

        public string CurrentComPort { get => _currentComPort; set { _currentComPort = value; OnPropertyChanged(nameof(CurrentComPort)); } }

        public int SelectedTypeIndex { get => _selectedTypeIndex; set { if (SetProperty(ref _selectedTypeIndex, value) && value > 0) { Settings.Instance.ActiveMappingType = value; Settings.Instance.SaveToFile(); } } }

        public int SelectedCardIndex { get => _selectedCardIndex; set { if (SetProperty(ref _selectedCardIndex, value) && IsConnected) PollIOStatus(); } }
        public ObservableCollection<FOUP_Ctrl.IOBitStatus> InputBits { get => _inputBits; set => SetProperty(ref _inputBits, value); }
        public ObservableCollection<FOUP_Ctrl.IOBitStatus> OutputBits { get => _outputBits; set => SetProperty(ref _outputBits, value); }
        #endregion

        #region Properties - Type/Table Configuration
        public ObservableCollection<string> FOUPTypeOptions { get; } = new ObservableCollection<string> { "TYPE-1", "TYPE-2", "TYPE-3", "TYPE-4", "TYPE-5" };
        public ObservableCollection<string> PositionTableOptions { get; } = new ObservableCollection<string> { "No. 1", "No. 2", "No. 3", "No. 4", "No. 5" };
        public ObservableCollection<string> MappingTableOptions { get; } = new ObservableCollection<string> { "No. 1", "No. 2", "No. 3", "No. 4", "No. 5" };
        public ObservableCollection<string> SequenceOptions { get; } = new ObservableCollection<string> { "0-FOUP", "1-Adaptor", "3-FOSB", "5-N2PURGE" };

        public int SelectedFOUPTypeIndex
        {
            get => _selectedFOUPTypeIndex;
            set
            {
                if (_selectedFOUPTypeIndex != value)
                {
                    _selectedFOUPTypeIndex = value;
                    ActiveProfileIndex = value;
                    Settings.Instance.ActiveMappingType = value + 1;

                    _positionTableIndexForFoupType = Settings.Instance.CurrentProfile.PositionTableNo - 1;
                    _mappingTableIndexForFoupType = Settings.Instance.CurrentProfile.MappingTableNo - 1;
                    _sequenceIndexForFoupType = Settings.Instance.CurrentProfile.FOUPTypeIndex;

                    _selectedPositionTableIndex = _positionTableIndexForFoupType;
                    _selectedMappingTableIndex = _mappingTableIndexForFoupType;
                    _selectedSequenceIndex = _sequenceIndexForFoupType;

                    OnPropertyChanged(nameof(SelectedFOUPTypeIndex));
                    OnPropertyChanged(nameof(PositionTableIndexForFoupType));
                    OnPropertyChanged(nameof(MappingTableIndexForFoupType));
                    OnPropertyChanged(nameof(SequenceIndexForFoupType));
                    OnPropertyChanged(nameof(SelectedPositionTableIndex));
                    OnPropertyChanged(nameof(SelectedMappingTableIndex));
                    OnPropertyChanged(nameof(SelectedSequenceIndex));

                    UpdatePositionTableProperties(_selectedPositionTableIndex + 1);
                    UpdateMappingTableProperties(_selectedMappingTableIndex + 1);
                    SaveParameters();
                }
            }
        }

        public int PositionTableIndexForFoupType
        {
            get => _positionTableIndexForFoupType;
            set
            {
                if (_positionTableIndexForFoupType != value)
                {
                    _positionTableIndexForFoupType = value;
                    Settings.Instance.CurrentProfile.PositionTableNo = value + 1;
                    OnPropertyChanged(nameof(PositionTableIndexForFoupType));
                    SaveParameters();
                }
            }
        }

        public int MappingTableIndexForFoupType
        {
            get => _mappingTableIndexForFoupType;
            set
            {
                if (_mappingTableIndexForFoupType != value)
                {
                    _mappingTableIndexForFoupType = value;
                    Settings.Instance.CurrentProfile.MappingTableNo = value + 1;
                    OnPropertyChanged(nameof(MappingTableIndexForFoupType));
                    SaveParameters();
                }
            }
        }

        public int SequenceIndexForFoupType
        {
            get => _sequenceIndexForFoupType;
            set
            {
                if (_sequenceIndexForFoupType != value)
                {
                    _sequenceIndexForFoupType = value;
                    Settings.Instance.CurrentProfile.FOUPTypeIndex = value;
                    OnPropertyChanged(nameof(SequenceIndexForFoupType));
                    SaveParameters();
                }
            }
        }

        public int SelectedPositionTableIndex
        {
            get => _selectedPositionTableIndex;
            set
            {
                if (_selectedPositionTableIndex != value)
                {
                    _selectedPositionTableIndex = value;
                    OnPropertyChanged(nameof(SelectedPositionTableIndex));
                    UpdatePositionTableProperties(value + 1);
                }
            }
        }

        public int SelectedMappingTableIndex
        {
            get => _selectedMappingTableIndex;
            set
            {
                if (_selectedMappingTableIndex != value)
                {
                    _selectedMappingTableIndex = value;
                    OnPropertyChanged(nameof(SelectedMappingTableIndex));
                    UpdateMappingTableProperties(value + 1);
                }
            }
        }

        public int SelectedSequenceIndex
        {
            get => _selectedSequenceIndex;
            set
            {
                if (_selectedSequenceIndex != value)
                {
                    _selectedSequenceIndex = value;
                    OnPropertyChanged(nameof(SelectedSequenceIndex));
                }
            }
        }

        // Position and Mapping table properties
        public double MapStartPosition
        {
            get => Settings.Instance.GetPositionTableByNumber(_selectedPositionTableIndex + 1).MapStartPositionMm;
            set
            {
                var table = Settings.Instance.GetPositionTableByNumber(_selectedPositionTableIndex + 1);
                if (table.MapStartPositionMm != value)
                {
                    table.MapStartPositionMm = value;
                    OnPropertyChanged(nameof(MapStartPosition));
                    SaveParameters();
                }
            }
        }

        public double MapEndPosition
        {
            get => Settings.Instance.GetPositionTableByNumber(_selectedPositionTableIndex + 1).MapEndPositionMm;
            set
            {
                var table = Settings.Instance.GetPositionTableByNumber(_selectedPositionTableIndex + 1);
                if (table.MapEndPositionMm != value)
                {
                    table.MapEndPositionMm = value;
                    OnPropertyChanged(nameof(MapEndPosition));
                    SaveParameters();
                }
            }
        }

        public int SelectedSensorType
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).SensorType;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.SensorType != value)
                {
                    table.SensorType = value;
                    OnPropertyChanged(nameof(SelectedSensorType));
                    SaveParameters();
                }
            }
        }

        public int SlotCount
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).SlotCount;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.SlotCount != value)
                {
                    table.SlotCount = value;
                    Settings.Instance.SlotCount = value;
                    OnPropertyChanged(nameof(SlotCount));
                    SaveParameters();
                }
            }
        }

        public double SlotPitch
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).SlotPitchMm;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.SlotPitchMm != value)
                {
                    table.SlotPitchMm = value;
                    OnPropertyChanged(nameof(SlotPitch));
                    SaveParameters();
                }
            }
        }

        public double PositionRange
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).PositionRangeMm;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.PositionRangeMm != value)
                {
                    table.PositionRangeMm = value;
                    Settings.Instance.PositionRangeMm = value;
                    OnPropertyChanged(nameof(PositionRange));
                    SaveParameters();
                }
            }
        }

        public double PositionRangeUpper
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).PositionRangeUpperPercent;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.PositionRangeUpperPercent != value)
                {
                    table.PositionRangeUpperPercent = value;
                    Settings.Instance.PositionRangeUpperPercent = value;
                    OnPropertyChanged(nameof(PositionRangeUpper));
                    SaveParameters();
                }
            }
        }

        public double PositionRangeLower
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).PositionRangeLowerPercent;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.PositionRangeLowerPercent != value)
                {
                    table.PositionRangeLowerPercent = value;
                    Settings.Instance.PositionRangeLowerPercent = value;
                    OnPropertyChanged(nameof(PositionRangeLower));
                    SaveParameters();
                }
            }
        }

        public double WaferThickness
        {
            get => MmToUm(Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).WaferThicknessMm);
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                double valueMm = UmToMm(value);
                if (Math.Abs(table.WaferThicknessMm - valueMm) > 0.0001)
                {
                    table.WaferThicknessMm = valueMm;
                    OnPropertyChanged(nameof(WaferThickness));
                    SaveParameters();
                }
            }
        }

        public double ThicknessRange
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).ThicknessRangeMm;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.ThicknessRangeMm != value)
                {
                    table.ThicknessRangeMm = value;
                    Settings.Instance.ThicknessRangeMm = value;
                    OnPropertyChanged(nameof(ThicknessRange));
                    SaveParameters();
                }
            }
        }

        public double Offset
        {
            get => Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1).OffsetMm;
            set
            {
                var table = Settings.Instance.GetMappingTableByNumber(_selectedMappingTableIndex + 1);
                if (table.OffsetMm != value)
                {
                    table.OffsetMm = value;
                    Settings.Instance.OffsetMm = value;
                    OnPropertyChanged(nameof(Offset));
                    SaveParameters();
                }
            }
        }
        #endregion

        #region Commands
        public ICommand ClearCalibrationCommand => new RelayCommand(ClearCalibration);
        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public ICommand StartServerCommand { get; private set; }
        public ICommand StopServerCommand { get; private set; }
        public ICommand TestCRC16Command { get; private set; }
        public ICommand ClampCommand { get; set; }
        public ICommand UnclampCommand { get; set; }
        public ICommand LatchCommand { get; set; }
        public ICommand UnlatchCommand { get; set; }
        public ICommand ElevatorUpCommand { get; set; }
        public ICommand ElevatorDownCommand { get; set; }
        public ICommand DoorForwardCommand { get; set; }
        public ICommand DoorBackwardCommand { get; set; }
        public ICommand DockForwardCommand { get; set; }
        public ICommand DockBackwardCommand { get; set; }
        public ICommand MappingForwardCommand { get; set; }
        public ICommand MappingBackwardCommand { get; set; }
        public ICommand VacuumOnCommand { get; set; }
        public ICommand VacuumOffCommand { get; set; }
        public ICommand StartMotionCommand { get; set; }
        public ICommand OriginCommand { get; private set; }
        public ICommand UpdateStatusCommand { get; set; }
        public ICommand LoadParametersCommand { get; private set; }
        public ICommand SaveParametersCommand { get; private set; }
        public ICommand ResetParametersCommand { get; private set; }
        public ICommand ApplyParametersCommand { get; private set; }
        public ICommand Type1Command { get; set; }
        public ICommand Type2Command { get; set; }
        public ICommand Type3Command { get; set; }
        public ICommand Type4Command { get; set; }
        public ICommand Type5Command { get; set; }
        public ICommand TrainFromCsvCommand { get; set; }
        public ICommand TrainFromLiveDataCommand { get; set; }
        public ICommand MappingOfflineCommand { get; set; }
        public ICommand TopToBottomMappingCommand { get; private set; }
        public ICommand SetupCommand { get; set; }
        public ICommand PollIOCommand { get; private set; }
        public ICommand OnCommand { get; set; }
        public ICommand OffCommand { get; set; }
        public ICommand ElevatorMappingStartCommand { get; set; }
        public ICommand ElevatorMappingEndCommand { get; set; }
        public ICommand ResetErrorCommand { get; set; }
        #endregion

        #region Constructor & Initialization
        public MainViewModel()
        {
            SetupViewModel = new SetupViewModel();
            _foupCtrl = HardwareManager.FoupCtrl;

            // Initialize services
            _trainingService = new FOUPTrainingService(_foupCtrl);
            _mappingService = new FOUPMappingService(_foupCtrl, _trainingService);
            _deviceManager = new FOUPDeviceManager(_foupCtrl);
            _communicationService = new FOUPCommunicationService(_foupCtrl);

            // Subscribe to service events
            _deviceManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _deviceManager.DeviceStatusChanged += OnDeviceStatusChanged;
            _communicationService.ServerStatusChanged += OnServerStatusChanged;

            InitializeData();
            WaferMappingResult = new ObservableCollection<MappingResult>();
            LoadParameters();
            InitializeCommands();
            InitializeCalibrationStatus();

            // Initialize message collections - will be updated when server starts
            _listMsgSent = new ObservableCollection<string>();
            _listMsgReceived = new ObservableCollection<string>();

            RefreshComPort();
        }

        private void InitializeData()
        {
            DeviceIds = new ObservableCollection<string> { "1", "2", "3", "4", "5", "6", "7", "8" };
            SelectedDeviceId1 = "1";
            SelectedDeviceId2 = "2";

            ComPorts = new ObservableCollection<string>();
            foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
            {
                ComPorts.Add(port);
            }

            if (!ComPorts.Contains("COM1")) ComPorts.Add("COM1");
            if (!ComPorts.Contains("COM2")) ComPorts.Add("COM2");
            if (!ComPorts.Contains("COM3")) ComPorts.Add("COM3");
            if (!ComPorts.Contains("COM4")) ComPorts.Add("COM4");

            IsRs485Selected = true;
            _selectedFOUPTypeIndex = Math.Max(0, Math.Min(4, Settings.Instance.ActiveMappingType - 1));
            _activeProfileIndex = _selectedFOUPTypeIndex;

            _positionTableIndexForFoupType = Settings.Instance.CurrentProfile.PositionTableNo - 1;
            _mappingTableIndexForFoupType = Settings.Instance.CurrentProfile.MappingTableNo - 1;
            _sequenceIndexForFoupType = Settings.Instance.CurrentProfile.FOUPTypeIndex;

            _selectedPositionTableIndex = _positionTableIndexForFoupType;
            _selectedMappingTableIndex = _mappingTableIndexForFoupType;
            _selectedSequenceIndex = _sequenceIndexForFoupType;

            _selectedTypeIndex = Settings.Instance.ActiveMappingType;
            _cts = new CancellationTokenSource();
        }

        private void InitializeLedCommands()
        {
            LedOnCommand = new RelayCommand(param => SetSelectedLedState(true), param => IsConnected);
            LedBlinkCommand = new RelayCommand(param => StartSelectedLedBlink(), param => IsConnected);
            LedOffCommand = new RelayCommand(param => SetSelectedLedState(false), param => IsConnected);
        }

        // Update the existing InitializeCommands() method to include:
        private void InitializeCommands()
        {
            CancellationToken token = _cts.Token;

            InitializeConnectionCommands(token);
            InitializeDeviceOperationCommands(token);
            InitializeMappingCommands(token);
            InitializeSettingsCommands();
            InitializePIOCommands();
            InitializeLedCommands(); // Add this line
        }
        #endregion

        #region Service Event Handlers
        private void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = e.IsConnected;
                IsDisconnected = !e.IsConnected;
                ConnectionIndication = e.IsConnected ? "ON-LINE" : "OFF-LINE";
                Status = e.StatusMessage;
            });
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Status != null)
                {
                    DeviceClampStatus = e.Status.ClampStatus;
                    DeviceLatchStatus = e.Status.LatchStatus;
                    DeviceElevatorStatus = e.Status.ElevatorStatus;
                    DeviceDoorStatus = e.Status.DoorStatus;
                    DeviceDockStatus = e.Status.DockStatus;
                    DeviceMappingStatus = e.Status.MappingStatus;
                    DeviceVacuumStatus = e.Status.VacuumStatus;
                    DeviceStatus = e.Status.MachineStatus;

                    UpdateButtonStates(e.Status);
                }
            });
        }

        private void OnServerStatusChanged(object sender, ServerStatusChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ServerStarted = e.IsStarted;
                Status = e.StatusMessage;
                CanEditServerSettings = !e.IsStarted;
                OnPropertyChanged(nameof(ListMsgReceived));
                OnPropertyChanged(nameof(ListMsgSent));
            });
        }

        private void UpdateButtonStates(DeviceStatus status)
        {
            // Update button enabled states based on device status
            ClampEnabled = status.ClampStatus != "Clamped";
            UnclampEnabled = status.ClampStatus != "Unclamped";
            LatchEnabled = status.LatchStatus != "Latched";
            UnlatchEnabled = status.LatchStatus != "Unlatched";
            ElevatorUpEnabled = status.ElevatorStatus != "Up";
            ElevatorDownEnabled = status.ElevatorStatus != "Down";
            DoorForwardEnabled = status.DoorStatus != "Open";
            DoorBackwardEnabled = status.DoorStatus != "Closed";
            DockForwardEnabled = status.DockStatus != "Extended";
            DockBackwardEnabled = status.DockStatus != "Retracted";
            MappingForwardEnabled = status.MappingStatus != "Retracted";
            MappingBackwardEnabled = status.MappingStatus != "Extended";
            VacuumOnEnabled = status.VacuumStatus != "On";
            VacuumOffEnabled = status.VacuumStatus != "Off";
        }
        #endregion

        #region Override Methods
        public override Task OnLoadedAsync()
        {
            ServerIP = "127.0.0.1";
            ServerPort = "17000";
            Debug.WriteLine($"[MainViewModel] UI initialized with ServerIP: {ServerIP}, ServerPort: {ServerPort}");
            return Task.CompletedTask;
        }
        #endregion

        #region Command Initialization Methods
        private void InitializeConnectionCommands(CancellationToken token)
        {
            SetupCommand = new RelayCommand(param =>
            {
                var setupVM = new SetupViewModel();
                setupVM.ComPort = CurrentComPort;
                setupVM.OnComPortSaved = RefreshComPort;
                WindowService.Show<Views.SetupView>(setupVM);
            });

            ConnectCommand = new RelayCommand(async param => await ConnectDevice(), param => !IsConnected);
            DisconnectCommand = new RelayCommand(async param => await DisconnectDevice(), param => IsConnected);

            StartServerCommand = new RelayCommand(param => StartTCPServer(), param => !ServerStarted);
            StopServerCommand = new RelayCommand(param => StopTCPServer(), param => ServerStarted);
            TestCRC16Command = new RelayCommand(param => _communicationService.TestCRC16Protocol(), param => ServerStarted);
        }

        private void InitializeDeviceOperationCommands(CancellationToken token)
        {
            ClampCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.Clamp(_cts.Token), "Clamp"),
                param => CanExecuteOperations() && ClampEnabled);

            UnclampCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.Unclamp(_cts.Token), "Unclamp"),
                param => CanExecuteOperations() && UnclampEnabled);

            LatchCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.Latch(_cts.Token), "Latch"),
                param => CanExecuteOperations() && LatchEnabled);

            UnlatchCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.Unlatch(_cts.Token), "Unlatch"),
                param => CanExecuteOperations() && UnlatchEnabled);

            ElevatorUpCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.ElevatorUp(_cts.Token), "Elevator Up"),
                param => CanExecuteOperations() && ElevatorUpEnabled);

            ElevatorDownCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.ElevatorDown(_cts.Token), "Elevator Down"),
                param => CanExecuteOperations() && ElevatorDownEnabled);

            DoorForwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.DoorForward(_cts.Token), "Door Forward"),
                param => CanExecuteOperations() && DoorForwardEnabled);

            DoorBackwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.DoorBackward(_cts.Token), "Door Backward"),
                param => CanExecuteOperations() && DoorBackwardEnabled);

            DockForwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.DockForward(_cts.Token), "Dock Forward"),
                param => CanExecuteOperations() && DockForwardEnabled);

            DockBackwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.DockBackward(_cts.Token), "Dock Backward"),
                param => CanExecuteOperations() && DockBackwardEnabled);

            MappingForwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.MappingForward(_cts.Token), "Mapping Forward"),
                param => CanExecuteOperations() && MappingForwardEnabled);

            MappingBackwardCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.MappingBackward(_cts.Token), "Mapping Backward"),
                param => CanExecuteOperations() && MappingBackwardEnabled);

            VacuumOnCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.VacuumOn(_cts.Token), "Vacuum On"),
                param => CanExecuteOperations() && VacuumOnEnabled);

            VacuumOffCommand = new RelayCommand(
                param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.VacuumOff(_cts.Token), "Vacuum Off"),
                param => CanExecuteOperations() && VacuumOffEnabled);

            //ElevatorMappingStartCommand = new RelayCommand(
            //    param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.ElevatorMappingStartPosition(_cts.Token), "Elevator Mapping Start"),
            //    param => CanExecuteOperations());

            //ElevatorMappingEndCommand = new RelayCommand(
            //    param => ExecuteDeviceOperationWithRefresh(() => _foupCtrl.ElevatorMappingEndPosition(_cts.Token), "Elevator Mapping End"),
            //    param => CanExecuteOperations());

            StartMotionCommand = new RelayCommand(
                async param => await ExecuteStartMotion(),
                param => CanExecuteOperations() && !string.IsNullOrEmpty(Motion));

            UpdateStatusCommand = new RelayCommand(
                param => _deviceManager.UpdateDeviceStatus(),
                param => IsConnected);

            OriginCommand = new RelayCommand(
                async param => await ExecuteOriginCommand(),
                param => CanExecuteOperations());

            // Reset Error command should ALWAYS be available when connected (even with errors)
            ResetErrorCommand = new RelayCommand(
                param => ExecuteResetError(),
                param => IsConnected);
        }

        private void ExecuteDeviceOperationWithRefresh(Func<bool> operation, string operationName)
        {
            try
            {
                // Check for any errors that would block the operation
                if (_foupCtrl.IsErrorExist())
                {
                    // Get error details
                    string errorInfo = !string.IsNullOrEmpty(_foupCtrl.ErrorMessage) ? _foupCtrl.ErrorMessage : "Unknown error";
                    string errorCode = ""; // Get from FOUP_Ctrl if available

                    if (_foupCtrl.m_status[0] == (char)MachineStatus.RecoverableError)
                    {
                        string blockMessage = $"Cannot execute {operationName}: System has a recoverable error (A) that must be cleared first.\n\nError: {errorInfo}\n\nPlease use the 'Reset Error' button to clear the error before attempting any operations.";
                        Status = blockMessage;
                        MessageService.Show(blockMessage, "Operation Blocked - Recoverable Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else if (_foupCtrl.m_status[0] == (char)MachineStatus.UnrecoverableError)
                    {
                        string blockMessage = $"Cannot execute {operationName}: System has an unrecoverable error (E) that must be reset first.\n\nError: {errorInfo}\n\nPlease use the 'Reset Error' button to clear the error before attempting any operations.";
                        Status = blockMessage;
                        MessageService.Show(blockMessage, "Operation Blocked - Unrecoverable Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                bool result = operation();

                if (!result)
                {
                    // Operation failed - check if it was due to error blocking
                    if (_foupCtrl.IsErrorExist())
                    {
                        string errorInfo = !string.IsNullOrEmpty(_foupCtrl.ErrorMessage) ? _foupCtrl.ErrorMessage : "Operation was blocked due to system error";
                        Status = $"{operationName} blocked: {errorInfo}";

                        // Show appropriate message based on error type
                        if (_foupCtrl.m_status[0] == (char)MachineStatus.RecoverableError)
                        {
                            MessageService.Show($"{operationName} was blocked due to a recoverable error.\n\n{errorInfo}\n\nPlease reset the error and try again.", "Operation Blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (_foupCtrl.m_status[0] == (char)MachineStatus.UnrecoverableError)
                        {
                            MessageService.Show($"{operationName} was blocked due to an unrecoverable error.\n\n{errorInfo}\n\nPlease reset the error and try again.", "Operation Blocked", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        Status = $"{operationName} command failed.";
                    }
                }
                else
                {
                    Status = $"{operationName} command executed successfully.";

                    // Force immediate status update after successful operation
                    if (IsConnected)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            _foupCtrl.UpdateSensorStatus();
                            await Task.Delay(25);
                            _foupCtrl.UpdateSensorStatus();

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ForceUIRefresh();
                            });
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Status = $"{operationName} command error: {ex.Message}";
                MessageService.Show($"Error during {operationName} operation: {ex.Message}", $"{operationName} Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ForceUIRefresh()
        {
            // Update all device status properties
            DeviceClampStatus = GetClampStatusDirect();
            DeviceLatchStatus = GetLatchStatusDirect();
            DeviceElevatorStatus = GetElevatorStatusDirect();
            DeviceDoorStatus = GetDoorStatusDirect();
            DeviceDockStatus = GetDockStatusDirect();
            DeviceMappingStatus = GetMappingStatusDirect();
            DeviceVacuumStatus = GetVacuumStatusDirect();
            DeviceStatus = GetMachineStatusDirect();

            // Update button states directly based on status
            ClampEnabled = DeviceClampStatus != "Clamped";
            UnclampEnabled = DeviceClampStatus != "Unclamped";
            LatchEnabled = DeviceLatchStatus != "Latched";
            UnlatchEnabled = DeviceLatchStatus != "Unlatched";
            ElevatorUpEnabled = DeviceElevatorStatus != "Up";
            ElevatorDownEnabled = DeviceElevatorStatus != "Down";
            DoorForwardEnabled = DeviceDoorStatus != "Open";
            DoorBackwardEnabled = DeviceDoorStatus != "Closed";
            DockForwardEnabled = DeviceDockStatus != "Extended";
            DockBackwardEnabled = DeviceDockStatus != "Retracted";
            MappingForwardEnabled = DeviceMappingStatus != "Retracted";
            MappingBackwardEnabled = DeviceMappingStatus != "Extended";
            VacuumOnEnabled = DeviceVacuumStatus != "On";
            VacuumOffEnabled = DeviceVacuumStatus != "Off";

            // Force command refresh
            CommandManager.InvalidateRequerySuggested();

            Debug.WriteLine($"UI Refreshed - ClampStatus: {DeviceClampStatus}, ClampEnabled: {ClampEnabled}, UnclampEnabled: {UnclampEnabled}");
        }

        // Add these helper methods to get status directly from the FOUP controller
        private string GetClampStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusClamp == 1)
                return "Clamped";
            else if (_foupCtrl._sensorStatus.StatusUnclamp == 1)
                return "Unclamped";
            else
                return "Undefined";
        }

        private string GetLatchStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusLatch == 1)
                return "Latched";
            else if (_foupCtrl._sensorStatus.StatusUnlatch == 1)
                return "Unlatched";
            else
                return "Undefined";
        }

        private string GetElevatorStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusElevatorUp == 1)
                return "Up";
            else if (_foupCtrl._sensorStatus.StatusElevatorDown == 1)
                return "Down";
            else
                return "Undefined";
        }

        private string GetDoorStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusDoorForward == 1)
                return "Open";
            else if (_foupCtrl._sensorStatus.StatusDoorBackward == 1)
                return "Closed";
            else
                return "Undefined";
        }

        private string GetDockStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusDockForward == 1)
                return "Extended";
            else if (_foupCtrl._sensorStatus.StatusDockBackward == 1)
                return "Retracted";
            else
                return "Undefined";
        }

        private string GetMappingStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusMappingForward == 1)
                return "Retracted";
            else if (_foupCtrl._sensorStatus.StatusMappingBackward == 1)
                return "Extended";
            else
                return "Undefined";
        }

        private string GetVacuumStatusDirect()
        {
            if (_foupCtrl._sensorStatus.StatusVacuum == 1)
                return "On";
            else
                return "Off";
        }

        private string GetMachineStatusDirect()
        {
            if (_foupCtrl.m_status != null && _foupCtrl.m_status.Length > 0)
            {
                char machineStatusChar = _foupCtrl.m_status[0];
                return ((MachineStatus)machineStatusChar).ToString();
            }
            return "Unknown";
        }

        private void InitializeMappingCommands(CancellationToken token)
        {
            Type1Command = new RelayCommand(param => { Settings.Instance.ActiveMappingType = 1; ActiveProfileIndex = 0; SelectedTypeIndex = 1; SaveParameters(); Status = $"Switched to mapping profile: {Settings.Instance.CurrentProfile.Name}"; });
            Type2Command = new RelayCommand(param => { Settings.Instance.ActiveMappingType = 2; ActiveProfileIndex = 1; SelectedTypeIndex = 2; SaveParameters(); Status = $"Switched to mapping profile: {Settings.Instance.CurrentProfile.Name}"; });
            Type3Command = new RelayCommand(param => { Settings.Instance.ActiveMappingType = 3; ActiveProfileIndex = 2; SelectedTypeIndex = 3; SaveParameters(); Status = $"Switched to mapping profile: {Settings.Instance.CurrentProfile.Name}"; });
            Type4Command = new RelayCommand(param => { Settings.Instance.ActiveMappingType = 4; ActiveProfileIndex = 3; SelectedTypeIndex = 4; SaveParameters(); Status = $"Switched to mapping profile: {Settings.Instance.CurrentProfile.Name}"; });
            Type5Command = new RelayCommand(param => { Settings.Instance.ActiveMappingType = 5; ActiveProfileIndex = 4; SelectedTypeIndex = 5; SaveParameters(); Status = $"Switched to mapping profile: {Settings.Instance.CurrentProfile.Name}"; });

            TrainFromCsvCommand = new RelayCommand(param => TrainFromCsv(), param => true);
            TrainFromLiveDataCommand = new RelayCommand(async param => await TrainFromLiveData(), param => IsConnected && IsTypeSelected());
            MappingOfflineCommand = new RelayCommand(param => { if (IsTypeSelected()) MappingOffline(); }, param => _trainingService.IsMapTrained);
            TopToBottomMappingCommand = new RelayCommand(async param => await ExecuteTopToBottomMapping(), param => IsConnected && IsTypeSelected());
        }

        private void InitializeSettingsCommands()
        {
            LoadParametersCommand = new RelayCommand(param => LoadParameters());
            SaveParametersCommand = new RelayCommand(param => SaveParameters());
            ResetParametersCommand = new RelayCommand(param => ResetParameters());
            ApplyParametersCommand = new RelayCommand(param => ApplyParameters());
        }

        private void InitializePIOCommands()
        {
            PollIOCommand = new RelayCommand(param => PollIOStatus(), param => IsConnected);
            OnCommand = new RelayCommand(param => SetIOBit(param as FOUP_Ctrl.IOBitStatus, true), param => IsConnected && param is FOUP_Ctrl.IOBitStatus);
            OffCommand = new RelayCommand(param => SetIOBit(param as FOUP_Ctrl.IOBitStatus, false), param => IsConnected && param is FOUP_Ctrl.IOBitStatus);
        }
        #endregion

        #region Command Implementations
        private async Task ConnectDevice()
        {
            string comPort = "COM4";
            bool success = await _deviceManager.ConnectDevice(SelectedDeviceId1, SelectedDeviceId2, comPort);

            if (success)
            {
                PollIOStatus();
            }
            else
            {
                MessageBox.Show("Connection Failed");
            }
        }

        private async Task DisconnectDevice()
        {
            await _deviceManager.DisconnectDevice();
        }

        private void StartTCPServer()
        {
            string serverIP = ServerIP ?? "127.0.0.1";
            int serverPort = int.TryParse(ServerPort, out int port) ? port : 17000;

            bool started = _communicationService.StartTCPServer(serverIP, serverPort);

            if (started)
            {
                // Force UI update for message collections
                OnPropertyChanged(nameof(ListMsgReceived));
                OnPropertyChanged(nameof(ListMsgSent));
            }
        }

        private void StopTCPServer()
        {
            _communicationService.StopTCPServer();

            // Force UI update for message collections
            OnPropertyChanged(nameof(ListMsgReceived));
            OnPropertyChanged(nameof(ListMsgSent));
        }

        //private void ExecuteDeviceOperation(Func<bool> operation, string operationName)
        //{
        //    try
        //    {
        //        bool result = operation();
        //        Status = result ? $"{operationName} command executed successfully." : $"{operationName} command failed.";

        //        // IMPORTANT FIX: Force immediate status update after operation completion
        //        if (result && IsConnected)
        //        {
        //            // Force an immediate device status update to refresh button states
        //            var deviceStatus = _deviceManager.UpdateDeviceStatus();
        //            if (deviceStatus != null)
        //            {
        //                // Trigger the UI update immediately
        //                Application.Current.Dispatcher.Invoke(() =>
        //                {
        //                    UpdateButtonStates(deviceStatus);
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Status = $"{operationName} command error: {ex.Message}";
        //    }
        //}

        private async Task ExecuteStartMotion()
        {
            if (string.IsNullOrEmpty(Motion)) return;

            int sequenceType = Settings.Instance.CurrentProfile.FOUPTypeIndex;
            string sequenceName = SequenceOptions[sequenceType];

            Status = $"Executing {Motion} with {sequenceName} sequence...";

            try
            {
                // **NEW: Delegate to FOUP_Ctrl instead of handling locally**
                bool success = await _foupCtrl.ExecuteStartMotion(Motion, sequenceType, Settings.Instance, _cts.Token);

                if (success)
                {
                    Status = $"{Motion} operation completed successfully";
                }
                else
                {
                    Status = $"{Motion} operation failed: {_foupCtrl.ErrorMessage}";
                }
            }
            catch (OperationCanceledException)
            {
                Status = $"{Motion} operation was cancelled";
            }
            catch (Exception ex)
            {
                Status = $"Error during {Motion} operation: {ex.Message}";
                MessageBox.Show($"An error occurred during the {Motion} operation: {ex.Message}",
                               $"{Motion} Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteOriginCommand()
        {
            Status = "Moving elevator to origin position...";
            try
            {
                // **This is already delegating to FOUP_Ctrl - no change needed**
                bool success = await _foupCtrl.ExecuteOriginCommand(_cts.Token);
                if (success)
                {
                    Status = "Elevator moved to origin position successfully.";
                }
                else
                {
                    Status = _foupCtrl.ErrorMessage ?? "Failed to reach elevator origin position.";
                    MessageBox.Show(_foupCtrl.ErrorMessage ?? "Failed to reach elevator origin position.",
                                   "Origin Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Status = "Error during origin operation.";
                MessageBox.Show($"Error during origin operation: {ex.Message}",
                               "Origin Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrainFromCsv()
        {
            LogMessage("--- Starting Training from CSV File ---");
            Status = "Select CSV file for training...";

            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Mapping Training Data CSV File",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Status = "Processing CSV file...";
                    var result = _trainingService.TrainFromCsv(openFileDialog.FileName);

                    if (result.Success)
                    {
                        LogMessage("--- Training from CSV Successful ---");
                        Status = "Training from CSV completed successfully";
                        DisplayTrainingResults(result.FirstSlotRefPulses, result.AvgPitchPulses, result.SlotRefPulses, result.BoundaryPulses);
                    }
                    else
                    {
                        LogMessage("--- Training from CSV Failed ---");
                        Status = $"Training failed: {result.ErrorMessage}";
                        MessageBox.Show($"Training failed: {result.ErrorMessage}", "Training Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"An unexpected error occurred: {ex.Message}");
                    Status = "Training Error";
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                LogMessage("CSV file selection cancelled.");
                Status = "Training Cancelled";
            }
        }

        private async Task TrainFromLiveData()
        {
            LogMessage("--- Starting Training from Live Data ---");
            Status = "Preparing for live data training...";

            if (!IsConnected)
            {
                MessageBox.Show("Device must be connected to perform live training.", "Connection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                Status = "Training Failed: Device not connected";
                return;
            }

            try
            {
                var result = MessageBox.Show("Select data collection speed:\n\nYES = High-Speed Mode (sub-millisecond intervals, maximum data points)\nNO = Standard Mode (normal 7ms intervals)\nCANCEL = Cancel operation", "Data Collection Speed", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                TrainingResult trainingResult = null;

                if (result == MessageBoxResult.Yes)
                {
                    trainingResult = await _trainingService.TrainFromLiveDataHighSpeed(Settings.Instance, _cts.Token);
                }
                else if (result == MessageBoxResult.No)
                {
                    trainingResult = await _trainingService.TrainFromLiveData(Settings.Instance, _cts.Token);
                }

                if (trainingResult != null)
                {
                    if (trainingResult.Success)
                    {
                        LogMessage("--- Training from Live Data Successful ---");
                        Status = "Live Data Training Completed Successfully";
                        DisplayTrainingResults(trainingResult.FirstSlotRefPulses, trainingResult.AvgPitchPulses, trainingResult.SlotRefPulses, trainingResult.BoundaryPulses);

                        MessageBox.Show($"Live training completed successfully!\n\nData Points Collected: {trainingResult.DetectedWaferCount}\nCalibrated Slot 1 Position: {_trainingService.CalibratedSlot1PosMm:F3}mm\nCalibrated Average Pitch: {_trainingService.CalibratedAvgPitchMm:F3}mm\n\nTraining parameters have been calculated and stored for mapping operations.", "Live Training Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        LogMessage("--- Training from Live Data Failed ---");
                        Status = $"Live Data Training Failed: {trainingResult.ErrorMessage}";
                        MessageBox.Show($"Live training failed: {trainingResult.ErrorMessage}\n\nPlease check that:\n• Wafers are properly positioned\n• At least 2 wafers are detected\n• Mapping sensors are functioning correctly", "Training Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Live training operation was cancelled.");
                Status = "Live Training Cancelled";
                MessageBox.Show("Live training operation was cancelled by user.", "Operation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during live training: {ex.Message}", "Live Training Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Error during live training: {ex.Message}");
                Status = "Live Training Error";
            }
        }

        private void MappingOffline()
        {
            LogMessage("--- Starting Offline Mapping from CSV File ---");
            Status = "Select CSV file for offline mapping analysis...";

            var trainedParams = new TrainingParameters
            {
                CalibratedSlot1PosMm = _trainingService.CalibratedSlot1PosMm,
                CalibratedAvgPitchMm = _trainingService.CalibratedAvgPitchMm,
                IsMapTrained = _trainingService.IsMapTrained
            };

            if (!_mappingService.VerifyMappingPrerequisites(trainedParams))
            {
                MessageBox.Show("Mapping Training has not been performed or was unsuccessful.\nPlease run 'Train' first before running offline analysis.", "Training Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Operational Mapping Scan Data CSV File",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var result = _mappingService.PerformOfflineMappingAnalysis(openFileDialog.FileName, trainedParams, Settings.Instance.ActiveMappingType);

                if (result.Success)
                {
                    UpdateMappingResults(result.WaferMapAnalysisResult);
                    ShowDetailedMappingResults(result.WaferMapAnalysisResult);
                    Status = "Offline Mapping Complete & Displayed";
                }
                else
                {
                    Status = $"Offline mapping failed: {result.ErrorMessage}";
                    MessageBox.Show($"Offline mapping failed: {result.ErrorMessage}", "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                LogMessage("CSV file selection cancelled.");
                Status = "Offline Mapping Cancelled";
            }
        }

        private async Task ExecuteTopToBottomMapping()
        {
            if (!IsTypeSelected()) return;

            Status = $"Starting Top-to-Bottom Mapping Operation (Type {Settings.Instance.ActiveMappingType})...";
            try
            {
                WaferMappingResult.Clear();
                var analysisResult = await _mappingService.ExecuteTopToBottomMapping(_cts.Token, Settings.Instance);

                if (analysisResult != null)
                {
                    UpdateMappingResults(analysisResult);
                    Status = "Top-to-Bottom Mapping Operation completed successfully.";
                }
                else
                {
                    Status = "Top-to-Bottom Mapping Operation completed with no analysis results.";
                }
            }
            catch (OperationCanceledException)
            {
                Status = "Top-to-Bottom Mapping Operation was cancelled.";
                LogMessage("Mapping operation was cancelled by user or timeout.");
            }
            catch (Exception ex)
            {
                Status = "Top-to-Bottom Mapping Operation failed.";
                LogMessage($"Error during mapping operation: {ex.Message}");
                MessageBox.Show($"An error occurred during the mapping operation: {ex.Message}", "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PollIOStatus()
        {
            if (!IsConnected)
            {
                Status = "Device not connected. Cannot poll I/O status.";
                return;
            }

            try
            {
                // Create temporary lists to hold the results
                var inputBitsList = new List<FOUP_Ctrl.IOBitStatus>();
                var outputBitsList = new List<FOUP_Ctrl.IOBitStatus>();

                // Call the FOUPCtrl method
                bool success = _foupCtrl.PollIOStatus(SelectedCardIndex, inputBitsList, outputBitsList);

                if (success)
                {
                    // Update the observable collections
                    InputBits.Clear();
                    foreach (var bit in inputBitsList)
                    {
                        InputBits.Add(bit);
                    }

                    OutputBits.Clear();
                    foreach (var bit in outputBitsList)
                    {
                        OutputBits.Add(bit);
                    }

                    Status = $"Successfully polled IO Card {(SelectedCardIndex == 0 ? 1 : 2)}";
                    UpdateIOBitDisplay();
                }
                else
                {
                    Status = _foupCtrl.ErrorMessage ?? "Failed to poll I/O status";
                    MessageService.Show(_foupCtrl.ErrorMessage ?? "Failed to poll I/O status", "Poll Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Status = $"Error polling I/O status: {ex.Message}";
                MessageService.Show($"Error polling I/O status: {ex.Message}", "Poll Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SetIOBit(IOBitStatus bitStatus, bool value)
        {
            if (bitStatus == null) return;

            try
            {
                bool success = _foupCtrl.SetIOBit(SelectedCardIndex, bitStatus.Bit, value);

                if (success)
                {
                    bitStatus.IsOn = value;
                    Status = $"Successfully set bit {bitStatus.Bit} {(value ? "ON" : "OFF")}";
                    UpdateIOBitDisplay();
                }
                else
                {
                    Status = _foupCtrl.ErrorMessage ?? $"Failed to set bit {bitStatus.Bit}";
                    MessageService.Show(_foupCtrl.ErrorMessage ?? $"Failed to set bit {bitStatus.Bit}", "Hardware Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Status = $"Exception setting bit {bitStatus.Bit}: {ex.Message}";
                MessageService.Show($"Error setting output bit {bitStatus.Bit}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIOBitDisplay()
        {
            var temp = InputBits;
            InputBits = null;
            InputBits = temp;

            temp = OutputBits;
            OutputBits = null;
            OutputBits = temp;
        }

        private void ExecuteResetError()
        {
            try
            {
                // Check current error state
                bool hadRecoverableError = _foupCtrl.m_status[0] == (char)MachineStatus.RecoverableError;
                bool hadUnrecoverableError = _foupCtrl.m_status[0] == (char)MachineStatus.UnrecoverableError;

                if (!_foupCtrl.IsErrorExist())
                {
                    Status = "No errors to reset - System is already in normal state";
                    MessageService.Show("No errors detected. The system is already in normal operation state.", "No Errors", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string errorBeforeReset = _foupCtrl.ErrorMessage ?? "Unknown error";

                bool resetSuccess = _foupCtrl.ResetError();

                if (resetSuccess)
                {
                    if (hadRecoverableError)
                    {
                        Status = "Recoverable error successfully cleared - Normal operations can now proceed";
                        MessageService.Show($"Recoverable error has been successfully cleared.\n\nPrevious error: {errorBeforeReset}\n\nAll FOUP operations are now available.", "Error Reset Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (hadUnrecoverableError)
                    {
                        Status = "Unrecoverable error successfully reset - Normal operations can now proceed";
                        MessageService.Show($"Unrecoverable error has been successfully reset.\n\nPrevious error: {errorBeforeReset}\n\nAll FOUP operations are now available.", "Error Reset Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Status = "Error reset completed successfully";
                    }

                    // Force immediate status update after reset
                    Task.Run(async () =>
                    {
                        await Task.Delay(200);
                        _foupCtrl.UpdateSensorStatus();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ForceUIRefresh();
                            Status = "Error reset completed - Status refreshed";
                        });
                    });
                }
                else
                {
                    Status = $"Error reset failed: {_foupCtrl.ErrorMessage}";
                    MessageService.Show(
                        $"Error reset failed: {_foupCtrl.ErrorMessage}\n\nThe system may still be in an error state. Please check hardware connections and try again.",
                        "Reset Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Status = $"Exception during reset: {ex.Message}";
                MessageService.Show(
                    $"Exception during error reset: {ex.Message}",
                    "Reset Exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        #endregion

        #region Parameter Management
        private void LoadParameters()
        {
            try
            {
                Settings.Instance.LoadFromFile();

                _selectedFOUPTypeIndex = Math.Max(0, Math.Min(4, Settings.Instance.ActiveMappingType - 1));
                _activeProfileIndex = _selectedFOUPTypeIndex;

                _positionTableIndexForFoupType = Settings.Instance.CurrentProfile.PositionTableNo - 1;
                _mappingTableIndexForFoupType = Settings.Instance.CurrentProfile.MappingTableNo - 1;
                _sequenceIndexForFoupType = Settings.Instance.CurrentProfile.FOUPTypeIndex;

                _selectedPositionTableIndex = _positionTableIndexForFoupType;
                _selectedMappingTableIndex = _mappingTableIndexForFoupType;
                _selectedSequenceIndex = _sequenceIndexForFoupType;

                OnPropertyChanged(nameof(SelectedFOUPTypeIndex));
                OnPropertyChanged(nameof(ActiveProfileIndex));
                OnPropertyChanged(nameof(PositionTableIndexForFoupType));
                OnPropertyChanged(nameof(MappingTableIndexForFoupType));
                OnPropertyChanged(nameof(SequenceIndexForFoupType));
                OnPropertyChanged(nameof(SelectedPositionTableIndex));
                OnPropertyChanged(nameof(SelectedMappingTableIndex));
                OnPropertyChanged(nameof(SelectedSequenceIndex));

                OnPropertyChanged(nameof(IsType1Selected));
                OnPropertyChanged(nameof(IsType2Selected));
                OnPropertyChanged(nameof(IsType3Selected));
                OnPropertyChanged(nameof(IsType4Selected));
                OnPropertyChanged(nameof(IsType5Selected));
                OnPropertyChanged(nameof(CurrentProfile));

                OnPropertyChanged(nameof(MapStartPosition));
                OnPropertyChanged(nameof(MapEndPosition));
                OnPropertyChanged(nameof(SelectedSensorType));
                OnPropertyChanged(nameof(SlotCount));
                OnPropertyChanged(nameof(SlotPitch));
                OnPropertyChanged(nameof(PositionRange));
                OnPropertyChanged(nameof(PositionRangeUpper));
                OnPropertyChanged(nameof(PositionRangeLower));
                OnPropertyChanged(nameof(WaferThickness));
                OnPropertyChanged(nameof(ThicknessRange));
                OnPropertyChanged(nameof(Offset));

                Status = "Parameters loaded from application settings";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading parameters: {ex.Message}", "Parameter Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Error loading parameters: {ex.Message}");
            }
        }

        private void SaveParameters()
        {
            try
            {
                Settings.Instance.SaveToFile();
                string filePath = Settings.SettingsDirectory;
                LogMessage($"Settings saved to: {filePath}");
                Status = "Parameters saved to application settings";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving parameters: {ex.Message}", "Parameter Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Error saving parameters: {ex.Message}");
            }
        }

        private void ResetParameters()
        {
            var result = MessageBox.Show("Reset all parameters to default values?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Settings.Instance.InitializeDefaults();

                _activeProfileIndex = Math.Max(0, Math.Min(4, Settings.Instance.ActiveMappingType - 1));
                OnPropertyChanged(nameof(ActiveProfileIndex));
                OnPropertyChanged(nameof(IsType1Selected));
                OnPropertyChanged(nameof(IsType2Selected));
                OnPropertyChanged(nameof(IsType3Selected));
                OnPropertyChanged(nameof(IsType4Selected));
                OnPropertyChanged(nameof(IsType5Selected));
                OnPropertyChanged(nameof(CurrentProfile));

                Status = "Parameters reset to defaults";
            }
        }

        private void ApplyParameters()
        {
            try
            {
                Settings.Instance.SaveToFile();
                Status = "Parameters saved successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving parameters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Status = $"Error saving parameters: {ex.Message}";
            }
        }
        #endregion

        #region Calibration Methods
        private void ClearCalibration(object parameter)
        {
            Type1CalibrationCompleted = false;
            Type2CalibrationCompleted = false;
            Type3CalibrationCompleted = false;
            Type4CalibrationCompleted = false;
            Type5CalibrationCompleted = false;

            Type1CalibrationSelected = false;
            Type2CalibrationSelected = false;
            Type3CalibrationSelected = false;
            Type4CalibrationSelected = false;
            Type5CalibrationSelected = false;

            UpdateCalibrationStatus();
            Status = "Calibration data cleared";
        }

        private void UpdateCalibrationStatus()
        {
            Type1CalibrationStatus = Type1CalibrationSelected && Type1CalibrationCompleted;
            Type2CalibrationStatus = Type2CalibrationSelected && Type2CalibrationCompleted;
            Type3CalibrationStatus = Type3CalibrationSelected && Type3CalibrationCompleted;
            Type4CalibrationStatus = Type4CalibrationSelected && Type4CalibrationCompleted;
            Type5CalibrationStatus = Type5CalibrationSelected && Type5CalibrationCompleted;
        }

        private void InitializeCalibrationStatus()
        {
            Type1CalibrationCompleted = false;
            Type2CalibrationCompleted = false;
            Type3CalibrationCompleted = false;
            Type4CalibrationCompleted = false;
            Type5CalibrationCompleted = false;

            Type1CalibrationSelected = false;
            Type2CalibrationSelected = false;
            Type3CalibrationSelected = false;
            Type4CalibrationSelected = false;
            Type5CalibrationSelected = false;

            UpdateCalibrationStatus();
        }

        private void UpdateTypeSelection(int activeType)
        {
            Type1CalibrationSelected = (activeType == 1);
            Type2CalibrationSelected = (activeType == 2);
            Type3CalibrationSelected = (activeType == 3);
            Type4CalibrationSelected = (activeType == 4);
            Type5CalibrationSelected = (activeType == 5);
        }
        #endregion

        #region Utility Methods
        private double MmToUm(double mm) => mm * 1000;
        private double UmToMm(double um) => um / 1000;

        private void LogMessage(string message)
        {
            Debug.WriteLine(message);
        }

        private void UpdatePositionTableProperties(int tableNumber)
        {
            LogMessage($"Selected Position Table No. {tableNumber}");
            OnPropertyChanged(nameof(MapStartPosition));
            OnPropertyChanged(nameof(MapEndPosition));
        }

        private void UpdateMappingTableProperties(int tableNumber)
        {
            if (tableNumber < 1 || tableNumber > 5) return;

            MappingTable mapTable = Settings.Instance.GetMappingTableByNumber(tableNumber);

            OnPropertyChanged(nameof(SelectedSensorType));
            OnPropertyChanged(nameof(SlotCount));
            OnPropertyChanged(nameof(SlotPitch));
            OnPropertyChanged(nameof(PositionRange));
            OnPropertyChanged(nameof(PositionRangeUpper));
            OnPropertyChanged(nameof(PositionRangeLower));
            OnPropertyChanged(nameof(WaferThickness));
            OnPropertyChanged(nameof(ThicknessRange));
            OnPropertyChanged(nameof(Offset));

            LogMessage($"Loaded Mapping Table {tableNumber} values: SensorType={mapTable.SensorType}, SlotCount={mapTable.SlotCount}, SlotPitch={mapTable.SlotPitchMm}, PositionRange={mapTable.PositionRangeMm}, ThicknessRange={mapTable.ThicknessRangeMm}");
        }

        private bool IsTypeSelected()
        {
            if (SelectedTypeIndex == 0)
            {
                MessageBox.Show("Please select a FOUP Type before mapping", "Type Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public void RefreshComPort()
        {
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comport.config");
            if (System.IO.File.Exists(configPath))
                CurrentComPort = System.IO.File.ReadAllText(configPath).Trim();
            else
                CurrentComPort = "COM3";
        }

        private void DisplayTrainingResults(int firstWaferRefPulses, int avgPitchPulses, int[] slotRefPulses, int[] boundaryPulses)
        {
            try
            {
                LogMessage("Creating training results window...");
                LogMessage($"Storing: First Wafer Ref: {firstWaferRefPulses}, Avg Pitch: {avgPitchPulses}");

                var waferMapTeachingView = new Views.WaferMapTeachingView();
                waferMapTeachingView.LoadData(firstWaferRefPulses, avgPitchPulses, slotRefPulses, boundaryPulses);
                waferMapTeachingView.Show();
            }
            catch (Exception ex)
            {
                LogMessage($"Error displaying training results: {ex.Message}");
                MessageBox.Show($"Error displaying training results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMappingResults(FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult)
        {
            LogMessage("Updating ListView with mapping results...");
            WaferMappingResult.Clear();

            for (int i = 0; i < analysisResult.WaferStatus.Length; i++)
            {
                string statusText = GetWaferStatusText(analysisResult.WaferStatus[i]);
                double thickness = analysisResult.WaferThicknessMm[i];
                int count = analysisResult.WaferCount1Value[i];

                WaferMappingResult.Add(new MappingResult
                {
                    No = i + 1,
                    Status = statusText,
                    Thickness = (int)(thickness * 1000),
                    Count = count
                });
            }

            LogMessage($"Updated ListView with {WaferMappingResult.Count} wafer mapping results");
        }

        private void ShowDetailedMappingResults(FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult)
        {
            var result = MessageBox.Show("Do you want to show detailed wafer mapping results in a separate window?", "Show Details", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var waferMapView = new Views.WaferMapView();

                LogMessage($"Sending analysis results to view:");
                LogMessage($"FirstRefPos: {analysisResult.FirstWaferRefPosPulses}, AvgPitch: {analysisResult.AvgSlotPitchPulses}");
                LogMessage($"Detected wafers: {analysisResult.DetectedWaferCount}");

                waferMapView.FirstWaferRefPos = analysisResult.FirstWaferRefPosPulses;
                waferMapView.AvgSlotPitch = (uint)Math.Abs(analysisResult.AvgSlotPitchPulses);
                waferMapView.WaferNumber = analysisResult.DetectedWaferCount;

                waferMapView.WaferThicknessMap2 = analysisResult.WaferThicknessMm.ToArray();
                waferMapView.WaferCount1Map2 = analysisResult.WaferCount1Value.ToArray();
                waferMapView.WaferStatusMap = analysisResult.WaferStatus.ToArray();
                waferMapView.SlotRefPos = analysisResult.SlotRefPositionPulses.ToArray();
                waferMapView.SlotBoundary = analysisResult.SlotBoundaryPulses.ToArray();
                waferMapView.WaferBottomMap = analysisResult.WaferBottomEdgePulses.ToArray();
                waferMapView.WaferTopMap = analysisResult.WaferTopEdgePulses.ToArray();

                for (int i = 0; i < Math.Min(5, analysisResult.SlotRefPositionPulses.Length); i++)
                {
                    LogMessage($"Slot {i + 1}: Pos={analysisResult.SlotRefPositionPulses[i]}, Status={analysisResult.WaferStatus[i]}, Thickness={analysisResult.WaferThicknessMm[i]}");
                }

                waferMapView.UpdateDisplay();
                waferMapView.Show();
            }
        }

        private string GetWaferStatusText(int statusCode)
        {
            switch (statusCode)
            {
                case 0: return "Empty";
                case 1: return "Normal";
                case 2: return "Crossed";
                case 3: return "Thick";
                case 4: return "Thin";
                case 5: return "Position Error";
                case 6: return "Double";
                case 99: return "Error";
                default: return $"Unknown ({statusCode})";
            }
        }
        #endregion

        #region IO Bit Status Model
        //public class IOBitStatus
        //{
        //    public int ID { get; set; }
        //    public int Bit { get; set; }
        //    public string Command { get; set; }
        //    public bool IsOn { get; set; }
        //    public string Driver { get; set; }
        //    public int Port { get; set; }
        //    public int DelayMs { get; set; }
        //    public string Configuration { get; set; }
        //}
        #endregion

        #region LED Control Methods
        private void SetSelectedLedState(bool state)
        {
            if (!IsConnected) return;

            try
            {
                // Map the selected radio button index to the corresponding LED bit
                int ledBit = GetLedBitFromSelection();
                if (ledBit == -1)
                {
                    Status = "Invalid LED selection";
                    return;
                }

                // Set the specific LED bit on Card 2 (index 1)
                bool success = _foupCtrl.SetIOBit(1, ledBit, state);

                if (success)
                {
                    // Update the corresponding property
                    UpdateLedProperty(ledBit, state);
                    string ledName = GetLedNameFromBit(ledBit);
                    Status = $"{ledName} LED turned {(state ? "ON" : "OFF")}";
                }
                else
                {
                    Status = $"Failed to control LED: {_foupCtrl.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                Status = $"LED control error: {ex.Message}";
            }
        }

        private async void StartSelectedLedBlink()
        {
            if (!IsConnected) return;

            try
            {
                int ledBit = GetLedBitFromSelection();
                if (ledBit == -1)
                {
                    Status = "Invalid LED selection";
                    return;
                }

                string ledName = GetLedNameFromBit(ledBit);
                Status = $"{ledName} LED blinking...";

                // Blink pattern: on for 500ms, off for 500ms, repeat 5 times
                for (int i = 0; i < 5; i++)
                {
                    _foupCtrl.SetIOBit(1, ledBit, true);
                    UpdateLedProperty(ledBit, true);
                    await Task.Delay(500);

                    _foupCtrl.SetIOBit(1, ledBit, false);
                    UpdateLedProperty(ledBit, false);
                    await Task.Delay(500);
                }

                Status = $"{ledName} LED blink completed";
            }
            catch (Exception ex)
            {
                Status = $"LED blink error: {ex.Message}";
            }
        }

        private int GetLedBitFromSelection()
        {
            // Map radio button selection to Card 2 LED output bits (0-6 based on your FOUP_Ctrl)
            switch (SelectedLedIndex)
            {
                case 0: return 0;  // LED - PRESENCE
                case 1: return 1;  // LED - PLACEMENT  
                case 2: return 2;  // LED - STATUS 1
                case 3: return 3;  // LED - STATUS 2
                case 4: return 4;  // LED - STATUS 3 (if available)
                case 5: return 5;  // LED - STATUS 4 (if available)
                case 6: return 4;  // LED - LOAD (mapped to bit 4)
                case 7: return 5;  // LED - UNLOAD (mapped to bit 5)
                case 8: return 6;  // LED - ALARM
                case 9: return 7;  // Manual SW 1 (if available)
                case 10: return 8; // Manual SW 2 (if available)
                default: return -1;
            }
        }

        private void UpdateLedProperty(int ledBit, bool state)
        {
            // Update the corresponding LED property based on the bit
            switch (ledBit)
            {
                case 0: LedPresenceOn = state; break;
                case 1: LedPlacementOn = state; break;
                case 2: LedStatus1On = state; break;
                case 3: LedStatus2On = state; break;
                case 4: LedLoadOn = state; break;
                case 5: LedUnloadOn = state; break;
                case 6: LedAlarmOn = state; break;
                case 7: LedManualSW1On = state; break;
                case 8: LedManualSW2On = state; break;
            }
        }

        private string GetLedNameFromBit(int ledBit)
        {
            switch (ledBit)
            {
                case 0: return "Presence";
                case 1: return "Placement";
                case 2: return "Status 1";
                case 3: return "Status 2";
                case 4: return "Load";
                case 5: return "Unload";
                case 6: return "Alarm";
                case 7: return "Manual SW 1";
                case 8: return "Manual SW 2";
                default: return "Unknown";
            }
        }
        #endregion

        // Add a method to check if operations should be enabled based on error state
        private bool CanExecuteOperations()
        {
            // Only allow operations if no errors exist
            return IsConnected && !_foupCtrl.IsErrorExist();
        }
    }
}