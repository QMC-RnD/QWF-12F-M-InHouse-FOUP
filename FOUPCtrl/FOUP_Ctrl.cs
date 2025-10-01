#if x64
using Creden.Hardware64.Cards;
#else
using Creden.Hardware.Cards;
#endif
using FOUPCtrl;
using FOUPCtrl.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FoupControl
{
    public class FOUP_Ctrl
    {
        #region Properties and Fields
        public string ErrorMessage { get { return _errorMessage; } }
        public bool ConnectionIOCard1 { get; private set; }
        public bool ConnectionIOCard2 { get; private set; }
        public byte IOID1 { get; set; }
        public byte IOID2 { get; set; }
        public string IOComPort1 { get; set; }
        public string IOComPort2 { get; set; }
        public bool ConnectionAxisCard { get; private set; }
        public byte AxisID { get; set; }
        public string AxisComPort { get; set; }

        #region Motion Timeout Settings
        private int clampTimeOver = 1500;      // Clamp/Unclamp timeout
        private int latchTimeOver = 3000;      // Latch/Unlatch timeout
        private int vacuumTimeOver = 1000;     // Vacuum On/Off timeout
        private int dockTimeOver = 2500;       // Dock Forward/Backward timeout
        private int doorTimeOver = 3000;       // Door Forward/Backward timeout
        private int mappingTimeOver = 1500;    // Mapping Forward/Backward timeout
        private int elevatorTimeOver = 8000;   // Elevator Up/Down timeout
        #endregion

        private int DelayBetweenTask = 1000;
        private List<DataPoint> _mappingData = new List<DataPoint>();
        public IO1616Card _credenIOCard1;
        public IO1616Card _credenIOCard2;
        public AX0040Card _credenAxisCard;
        public SensorList _sensorList = new SensorList();
        public SensorStatus _sensorStatus = new SensorStatus();
        public OutputList _outputList = new OutputList();
        public OutputStatus _outputStatus = new OutputStatus();
        private string _errorMessage = String.Empty;
        public char[] m_status = new char[20];
        protected string sErrorCode = "00";
        protected string sInterlockCode = "00";
        protected string sStatusCode = "00";
        Semaphore semReadPort = new Semaphore(1, 1);
        Semaphore semWritePort = new Semaphore(1, 1);
        private CancellationTokenSource _doorRetentionMonitoringCts;
        private CancellationTokenSource _airPressureMonitoringCts;
        private CancellationTokenSource _foupMountSensorMonitoringCts;
        private CancellationTokenSource _waferProtrusionMonitoringCts;
        private CancellationTokenSource _foupMountLoadMonitoringCts;
        private CancellationTokenSource _dockHandPinchMonitoringCts;
        private CancellationTokenSource _sequenceCancellationTokenSource;
        public string[] MotionList { get; } = new string[] { "Load", "Unload", "Load (map)", "Unload (map)", "MAP ACAL" };
        #endregion

        #region Constants and Configuration
        private int ClampSensor = 0;        // Bit 0 on port 0 - reads when clamp is activated
        private int UnclampSensor = 1;      // Bit 1 on port 0 - reads when unclamp is activated
        private int LatchSensor = 6;        // Bit 6 on port 0 - reads when latch is activated
        private int UnlatchSensor = 7;      // Bit 7 on port 0 - reads when unlatch is activated
        private int DockForwardLimit = 11;  // Read from card1
        private int DockBackwardLimit = 12; // Read from card1
        private int ElevatorUpperLimit = 6; // Read from card1
        private int ElevatorLowerLimit = 4; // Read from card2
        private int DoorForwardLimit = 10;  // Read from card2
        private int DoorBackwardLimit = 11; // Read from card2
        private int MappingForwardLimit = 12; // Read from card2
        private int MappingBackwardLimit = 13; // Read from card2
        private int VacuumSensorInputBit = 8; // Read from card1, bit 8 (port 1, bit 0)
        private int ProtrusionSensor = 7;   // Read from card1
        private int ClampOutput = 7;        // Bit 7 on port 2 - activates clamp
        private int UnclampOutput = 6;      // Bit 6 on port 2 - activates unclamp
        private int LatchOutput = 12;       // Bit 12 on port 2 - activates latch
        private int UnlatchOutput = 13;     // Bit 13 on port 2 - activates unlatch
        private int ElevatorUpOutput1 = 2;  // Bit 2 on port 2 - activates elevator up (1)
        private int ElevatorUpOutput2 = 5;  // Bit 5 on port 2 - activates elevator up (2)
        private int ElevatorDownOutput1 = 3; // Bit 3 on port 2 - activates elevator down (1)
        private int ElevatorDownOutput2 = 4; // Bit 4 on port 2 - activates elevator down (2)
        private int DoorForwardOutput = 11; // Bit 11 on port 2 - activates door forward
        private int DoorBackwardOutput = 10; // Bit 10 on port 2 - activates door backward
        private int DockForwardOutput = 9;  // Bit 9 on port 2 - activates dock forward
        private int DockBackwardOutput = 8; // Bit 8 on port 2 - activates dock backward
        private int MappingForwardOutput = 14; // Bit 14 on port 2 - activates mapping forward
        private int MappingBackwardOutput = 15; // Bit 15 on port 2 - activates mapping backward
        private int VacuumValve1A = 0;      // Bit 0 on port 2 - VACUUM VALVE 1A (release vacuum)
        private int VacuumValve1B = 1;      // Bit 1 on port 2 - VACUUM VALVE 1B (apply vacuum)
        private int DockHandPinchSensor = 4;
        #endregion

        #region Structures and Enums
        public struct SensorList
        {
            public int Clamp { get; set; }
            public int Unclamp { get; set; }
            public int Latch { get; set; }
            public int Unlatch { get; set; }
            public int DockForward { get; set; }
            public int DockBackward { get; set; }
            public int ElevatorUp { get; set; }
            public int ElevatorDown { get; set; }
            public int DoorForward { get; set; }
            public int DoorBackward { get; set; }
            public int MappingForward { get; set; }
            public int MappingBackward { get; set; }
            public int Vacuum { get; set; }
            public int Protrusion { get; set; }
        }
        public struct SensorStatus
        {
            public int StatusClamp { get; set; }
            public int StatusUnclamp { get; set; }
            public int StatusLatch { get; set; }
            public int StatusUnlatch { get; set; }
            public int StatusDockForward { get; set; }
            public int StatusDockBackward { get; set; }
            public int StatusElevatorUp { get; set; }
            public int StatusElevatorDown { get; set; }
            public int StatusDoorForward { get; set; }
            public int StatusDoorBackward { get; set; }
            public int StatusMappingForward { get; set; }
            public int StatusMappingBackward { get; set; }
            public int StatusVacuum { get; set; }
            public int StatusProtrusion { get; set; }
            public int StatusPresence1And2 { get; set; }
            public int StatusPresence3 { get; set; }
            public int StatusPresenceDiagonal1 { get; set; }
            public int StatusPresenceDiagonal2 { get; set; }
            public int StatusPressure { get; set; }
        }
        public struct OutputList
        {
            public int Clamp { get; set; }
            public int Unclamp { get; set; }
            public int Latch { get; set; }
            public int Unlatch { get; set; }
            public int ElevatorUp1 { get; set; }
            public int ElevatorUp2 { get; set; }
            public int ElevatorDown1 { get; set; }
            public int ElevatorDown2 { get; set; }
            public int DoorForward { get; set; }
            public int DoorBackward { get; set; }
            public int DockForward { get; set; }
            public int DockBackward { get; set; }
            public int MappingForward { get; set; }
            public int MappingBackward { get; set; }
            public int VacuumValve1A { get; set; }
            public int VacuumValve1B { get; set; }
        }
        public struct OutputStatus
        {
            public int StatusClamp { get; set; }
            public int StatusUnclamp { get; set; }
            public int StatusLatch { get; set; }
            public int StatusUnlatch { get; set; }
            public int StatusElevatorUp { get; set; }
            public int StatusElevatorDown { get; set; }
            public int StatusDoorForward { get; set; }
            public int StatusDoorBackward { get; set; }
            public int StatusDockForward { get; set; }
            public int StatusDockBackward { get; set; }
            public int StatusMappingForward { get; set; }
            public int StatusMappingBackward { get; set; }
            public int StatusVacuum { get; set; }
        }
        public class DataPoint
        {
            public long TimeMs { get; set; }
            public double Position { get; set; }
            public int SensorValue { get; set; }
            public double Velocity { get; set; }
        }
        public enum SequenceType
        {
            FOUP = 0,
            Adaptor = 1,
            FOSB = 3,
            N2Purge = 5
        }
        public enum OperationType
        {
            Load,
            Unload
        }
        public class SequenceStep
        {
            public string Name { get; set; }
            public Func<CancellationToken, bool> Operation { get; set; }
            public bool IsRequired { get; set; } = true;
        }
        #endregion

        #region Exceptions
        public class SensorErrorException : Exception
        {
            public string ErrorCode { get; }
            public string SensorName { get; }

            public SensorErrorException(string errorCode, string sensorName, string message)
                : base($"Sensor Error [{errorCode}] {sensorName}: {message}")
            {
                ErrorCode = errorCode;
                SensorName = sensorName;
            }

            public SensorErrorException(string errorCode, string sensorName, string message, Exception innerException)
                : base($"Sensor Error [{errorCode}] {sensorName}: {message}", innerException)
            {
                ErrorCode = errorCode;
                SensorName = sensorName;
            }
        }
        #endregion

        #region Constructor and Initialization
        public FOUP_Ctrl()
        {
            // Initialize sensor list
            _sensorList.Clamp = ClampSensor;
            _sensorList.Unclamp = UnclampSensor;
            _sensorList.Latch = LatchSensor;
            _sensorList.Unlatch = UnlatchSensor;
            _sensorList.DockForward = DockForwardLimit;
            _sensorList.DockBackward = DockBackwardLimit;
            _sensorList.ElevatorUp = ElevatorUpperLimit;
            _sensorList.ElevatorDown = ElevatorLowerLimit;
            _sensorList.DoorForward = DoorForwardLimit;
            _sensorList.DoorBackward = DoorBackwardLimit;
            _sensorList.MappingForward = MappingForwardLimit;
            _sensorList.MappingBackward = MappingBackwardLimit;
            _sensorList.Vacuum = VacuumSensorInputBit;
            _sensorList.Protrusion = ProtrusionSensor;

            // Initialize output list
            _outputList.Clamp = ClampOutput;
            _outputList.Unclamp = UnclampOutput;
            _outputList.Latch = LatchOutput;
            _outputList.Unlatch = UnlatchOutput;
            _outputList.ElevatorUp1 = ElevatorUpOutput1;
            _outputList.ElevatorUp2 = ElevatorUpOutput2;
            _outputList.ElevatorDown1 = ElevatorDownOutput1;
            _outputList.ElevatorDown2 = ElevatorDownOutput2;
            _outputList.DoorForward = DoorForwardOutput;
            _outputList.DoorBackward = DoorBackwardOutput;
            _outputList.DockForward = DockForwardOutput;
            _outputList.DockBackward = DockBackwardOutput;
            _outputList.MappingForward = MappingForwardOutput;
            _outputList.MappingBackward = MappingBackwardOutput;
            _outputList.VacuumValve1A = VacuumValve1A;
            _outputList.VacuumValve1B = VacuumValve1B;

            InitializeStatus();

#if x64
                    _credenIOCard1 = new Creden.Hardware64.Cards.IO1616Card();
                    _credenIOCard2 = new Creden.Hardware64.Cards.IO1616Card();
#else
            _credenIOCard1 = new Creden.Hardware.Cards.IO1616Card();
            _credenIOCard2 = new Creden.Hardware.Cards.IO1616Card();
#endif
        }


        ~FOUP_Ctrl()
        {
            Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void InitializeStatus()
        {
            m_status[0] = (char)MachineStatus.Normal;
            m_status[1] = (char)MachineMode.Online;
            m_status[2] = (char)LoadStatus.Indefinite;
            m_status[3] = (char)Operation.Stopping;
            m_status[4] = '0'; // Error code
            m_status[5] = '0'; // Error code 2
            m_status[6] = '?';
            m_status[7] = '?';
            m_status[8] = '?';
            m_status[9] = '0'; // reserve
            m_status[10] = '0'; // reserve
            m_status[11] = '?';
            m_status[12] = (char)ZAxisPosition.Indefinite;
            m_status[13] = '0'; // reserve
            m_status[14] = '0'; // reserve
            m_status[15] = '0'; // reserve
            m_status[16] = '0'; // reserve
            m_status[17] = (char)MappingStatus.Inexecution;
            m_status[18] = (char)PodType.Type1;
            m_status[19] = '0'; // reserve
        }
        #endregion

        #region Connection Management
        public bool Connect()
        {
            try
            {
                // Try to disconnect first, for reconnecting purposes
                try
                {
                    Disconnect();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Disconnect exception (non-critical): {ex.Message}");
                }

                // Always use COM4 for all connections
                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comport.config");
                string comPort = "COM3"; // default
                if (File.Exists(configFilePath))
                {
                    var port = File.ReadAllText(configFilePath).Trim();
                    if (!string.IsNullOrEmpty(port))
                        comPort = port;
                }
                Debug.WriteLine($"Connecting all devices using {comPort}");

                // Force creation of new card instances to ensure clean state
                _credenIOCard1 = new IO1616Card();
                _credenIOCard2 = new IO1616Card();
                _credenAxisCard = new AX0040Card();

                // IMPORTANT: Set the correct device IDs before connecting
                // For Axis Card, ID must be 3 (not 0) based on RS485 requirements
                IOID1 = 1;
                IOID2 = 2;
                AxisID = 3;  // CHANGED FROM 0 TO 3

                Debug.WriteLine($"Connecting IO Card 1 (ID:{IOID1}) on {comPort}");
                ConnectionIOCard1 = _credenIOCard1.ConnectRS485(IOID1, comPort);

                // Short delay between connections to avoid RS485 bus conflicts
                Thread.Sleep(100);

                Debug.WriteLine($"Connecting IO Card 2 (ID:{IOID2}) on {comPort}");
                ConnectionIOCard2 = _credenIOCard2.ConnectRS485(IOID2, comPort);

                // Short delay between connections
                Thread.Sleep(100);

                Debug.WriteLine($"Connecting Axis Card (ID:{AxisID}) on {comPort}");
                ConnectionAxisCard = _credenAxisCard.ConnectRS485(AxisID, comPort);

                // Check if all connections were successful
                if (!(ConnectionIOCard1 && ConnectionIOCard2 && ConnectionAxisCard))
                {
                    Debug.WriteLine("One or more connections failed");

                    // Output connection status for diagnosis
                    Debug.WriteLine($"Connection status - IO Card 1: {ConnectionIOCard1}, IO Card 2: {ConnectionIOCard2}, Axis Card: {ConnectionAxisCard}");

                    // Clean up any successful connections
                    Disconnect();
                    _errorMessage = "Connection failed for one or more cards";
                    return false;
                }

                // Configure axis card if connected successfully
                CardStatus status = _credenAxisCard.SetFeedbackPosSrc(3, 0);
                if (status != CardStatus.Successful)
                {
                    Debug.WriteLine($"Warning: Failed to set feedback position source: {status}");
                    // Continue despite this warning
                }

                Debug.WriteLine("All cards connected successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection error: {ex.Message}");
                _errorMessage = $"Connection error: {ex.Message}";
                return false;
            }
        }
        public void Disconnect()
        {
            if (ConnectionIOCard1)
            {
                _credenIOCard1?.Close();
                ConnectionIOCard1 = false;
            }
            if (ConnectionIOCard2)
            {
                _credenIOCard2?.Close();
                ConnectionIOCard2 = false;
            }
            if (ConnectionAxisCard)
            {
                _credenAxisCard?.Close();
                ConnectionAxisCard = false;
            }
        }
        public bool Reconnect(int millisecondsToWait, string lastOperation, int attempts = 10)
        {
            // Wait for process or reconnect attempt delay
            Thread.Sleep(millisecondsToWait);

            if (attempts <= 0)
            {
                // No more attempt to reconnect
                return false;
            }

            // Check connection
            if (Connect())
            {
                // If card still in connection or reconnected
                return true;
            }
            else
            {
                // Attempt to reconnect
                int SecondsForNextAttempt = 2;
                Reconnect(SecondsForNextAttempt * 1000, lastOperation, attempts - 1);
            }
            return false;
        }
        public bool IsErrorExist()
        {
            if (m_status[0] != (char)MachineStatus.Normal)
                return true;
            if (sErrorCode != "00")
                return true;

            return false;
        }
        #endregion

        #region IO Operations
        private void DigitalRead(IO1616Card card, int portId, ref byte value)
        {
            bool acquired = false;
            try
            {
                semReadPort.WaitOne();
                acquired = true;
                CardStatus status = card.ReadPort((byte)portId, ref value);
                if (status != CardStatus.Successful)
                {
                    throw new InvalidOperationException($"Failed to read port {portId}: {status}");
                }
            }
            finally
            {
                if (acquired)
                {
                    try { semReadPort.Release(); } catch (SemaphoreFullException) { }
                }
            }
        }

        private void DigitalWrite(IO1616Card card, int portId, byte value)
        {
            bool acquired = false;
            try
            {
                semWritePort.WaitOne();
                acquired = true;
                CardStatus status = card.WritePort((byte)portId, value);
                if (status != CardStatus.Successful)
                {
                    throw new InvalidOperationException($"Failed to write port {portId}: {status}");
                }
            }
            finally
            {
                if (acquired)
                {
                    try { semWritePort.Release(); } catch (SemaphoreFullException) { }
                }
            }
        }

        public byte SetBit(byte writeByte, int BitIndex)
        {
            if (BitIndex >= 8)
            {
                BitIndex = BitIndex - 8;
            }
            writeByte |= (byte)(1 << BitIndex);
            return writeByte;
        }

        public byte ClearBit(byte writeByte, int BitIndex)
        {
            if (BitIndex >= 8)
            {
                BitIndex = BitIndex - 8;
            }
            writeByte &= (byte)~((byte)1 << BitIndex);
            return writeByte;
        }

        public int ReadBit(IO1616Card card, int BitIndex)
        {
            int portId;
            byte readByte = 0;

            if (BitIndex < 8)
            {
                portId = 0;
            }
            else
            {
                portId = 1;
                BitIndex = BitIndex - 8;
            }

            DigitalRead(card, portId, ref readByte);

            UInt16 mask = (UInt16)((UInt16)1 << BitIndex);
            UInt16 result = (UInt16)(readByte & mask);

            int Value;
            if (result > 0)
                Value = 1;
            else
                Value = 0;

            return Value;
        }

        // Helper method for writing a bit
        private void WriteBit(IO1616Card card, int portId, int bitIndex, bool value)
        {
            byte currentValue = 0;
            DigitalRead(card, portId, ref currentValue);

            if (value)
                currentValue |= (byte)(1 << bitIndex);
            else
                currentValue &= (byte)~(1 << bitIndex);

            DigitalWrite(card, portId, currentValue);
        }
        #endregion

        #region Sensor Functions
        public void UpdateSensorStatus()
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return;

            // Get sensor statuses from both cards
            byte readByte = 0;

            try
            {
                // Read from card 1, port 0 (containing clamp, unclamp, elevator up sensors)
                DigitalRead(_credenIOCard1, 0, ref readByte);
                _sensorStatus.StatusClamp = (readByte & (1 << ClampSensor)) != 0 ? 1 : 0;
                _sensorStatus.StatusUnclamp = (readByte & (1 << UnclampSensor)) != 0 ? 1 : 0;
                _sensorStatus.StatusElevatorUp = (readByte & (1 << ElevatorUpperLimit)) != 0 ? 1 : 0;
                _sensorStatus.StatusProtrusion = (readByte & (1 << ProtrusionSensor)) != 0 ? 1 : 0;
                _sensorStatus.StatusPresence1And2 = (readByte & (1 << 2)) != 0 ? 1 : 0;
                _sensorStatus.StatusPresence3 = (readByte & (1 << 3)) != 0 ? 1 : 0;

                // Read from card 1, port 1 (next 8 inputs: 8-15)
                DigitalRead(_credenIOCard1, 1, ref readByte);
                _sensorStatus.StatusDockForward = (readByte & (1 << (DockForwardLimit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusDockBackward = (readByte & (1 << (DockBackwardLimit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusVacuum = (readByte & (1 << (VacuumSensorInputBit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusPresenceDiagonal1 = (readByte & (1 << (13 - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusPresenceDiagonal2 = (readByte & (1 << (14 - 8))) != 0 ? 1 : 0;

                // Read from card 2, port 0 (first 8 inputs: 0-7)
                DigitalRead(_credenIOCard2, 0, ref readByte);
                _sensorStatus.StatusLatch = (readByte & (1 << LatchSensor)) != 0 ? 1 : 0;
                _sensorStatus.StatusUnlatch = (readByte & (1 << UnlatchSensor)) != 0 ? 1 : 0;
                _sensorStatus.StatusElevatorDown = (readByte & (1 << ElevatorLowerLimit)) != 0 ? 1 : 0;
                _sensorStatus.StatusPressure = (readByte & (1 << 3)) != 0 ? 1 : 0;

                // Read from card 2, port 1 (next 8 inputs: 8-15)
                DigitalRead(_credenIOCard2, 1, ref readByte);
                _sensorStatus.StatusDoorForward = (readByte & (1 << (DoorForwardLimit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusDoorBackward = (readByte & (1 << (DoorBackwardLimit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusMappingForward = (readByte & (1 << (MappingForwardLimit - 8))) != 0 ? 1 : 0;
                _sensorStatus.StatusMappingBackward = (readByte & (1 << (MappingBackwardLimit - 8))) != 0 ? 1 : 0;

                // Use the new sensor validation methods
                CheckConflictingSensorStates(); // This will throw SensorErrorException if conflicts found

                // If no conflicts, ensure status is normal (if no other errors exist)
                if (sErrorCode == "00")
                {
                    m_status[0] = (char)MachineStatus.Normal;
                }

                // Position 4-5: Error Code
                m_status[4] = sErrorCode.Length > 0 ? sErrorCode[0] : '0';
                m_status[5] = sErrorCode.Length > 1 ? sErrorCode[1] : '0';

                // Position 6: Cassette Placement Status (placement quality) - ONLY StatusPresence1And2 and StatusPresence3
                if (_sensorStatus.StatusPresence1And2 == 1 && _sensorStatus.StatusPresence3 == 1)
                {
                    // Proper placement - all main presence sensors active (GREEN condition)
                    m_status[6] = (char)CassettePlacementStatus.Properly_Placed;
                }
                else
                {
                    // No cassette detected by main sensors
                    m_status[6] = (char)CassettePlacementStatus.No_Cassette;
                }

                // Position 7: FOUP Clamp Status
                if ((_sensorStatus.StatusClamp == 1) && (_sensorStatus.StatusUnclamp == 1))
                {
                    m_status[7] = (char)ClampStatus.Indefinite;
                }
                else if (_sensorStatus.StatusClamp == 1)
                {
                    m_status[7] = (char)ClampStatus.Close;
                }
                else if (_sensorStatus.StatusUnclamp == 1)
                {
                    m_status[7] = (char)ClampStatus.Open;
                }
                else
                {
                    m_status[7] = (char)ClampStatus.Indefinite;
                }

                // Position 8: Door Latch Status
                if ((_sensorStatus.StatusLatch == 1) && (_sensorStatus.StatusUnlatch == 1))
                {
                    m_status[8] = (char)LatchStatus.Indefinite;
                }
                else if (_sensorStatus.StatusLatch == 1)
                {
                    m_status[8] = (char)LatchStatus.Close;
                }
                else if (_sensorStatus.StatusUnlatch == 1)
                {
                    m_status[8] = (char)LatchStatus.Open;
                }
                else
                {
                    m_status[8] = (char)LatchStatus.Indefinite;
                }

                // Position 9: Vacuum Status
                m_status[9] = _sensorStatus.StatusVacuum == 1 ? (char)VacuumStatus.On : (char)VacuumStatus.Off;

                // Position 10: Door Position
                if (_sensorStatus.StatusDoorForward == 1)
                {
                    m_status[10] = (char)DoorPosition.Open;
                }
                else if (_sensorStatus.StatusDoorBackward == 1)
                {
                    m_status[10] = (char)DoorPosition.Close;
                }
                else
                {
                    m_status[10] = (char)DoorPosition.Indefinite;
                }

                // Position 11: Wafer Protrusion Sensor
                m_status[11] = _sensorStatus.StatusProtrusion == 1 ? (char)WaferProtrusionSensor.No_protrude : (char)WaferProtrusionSensor.Protrude;

                // Position 12: Elevator Axis Position
                if (_sensorStatus.StatusElevatorUp == 1)
                {
                    m_status[12] = (char)ZAxisPosition.Up_position;
                }
                else if (_sensorStatus.StatusElevatorDown == 1)
                {
                    m_status[12] = (char)ZAxisPosition.Down_position;
                }
                else
                {
                    m_status[12] = (char)ZAxisPosition.Indefinite;
                }

                // Position 13: Dock Position
                if (_sensorStatus.StatusDockForward == 1)
                {
                    m_status[13] = (char)DockPosition.Dock;
                }
                else if (_sensorStatus.StatusDockBackward == 1)
                {
                    m_status[13] = (char)DockPosition.Undock;
                }
                else
                {
                    m_status[13] = (char)DockPosition.Indefinite;
                }

                // Position 14: Cassette Presence Status (basic presence detection)
                if (_sensorStatus.StatusPresenceDiagonal1 == 0 || _sensorStatus.StatusPresenceDiagonal2 == 0)
                {
                    m_status[14] = (char)CassettePresenceStatus.Present;
                }
                else
                {
                    m_status[14] = (char)CassettePresenceStatus.None;
                }

                // Position 15: Mapping Position
                if (_sensorStatus.StatusMappingForward == 1)
                {
                    m_status[15] = (char)MappingPosition.Waiting_position;
                }
                else if (_sensorStatus.StatusMappingBackward == 1)
                {
                    m_status[15] = (char)MappingPosition.Measuring_position;
                }
                else
                {
                    m_status[15] = (char)MappingPosition.Indefinite;
                }

                // Position 16: Reserve (keep existing value or set to '0')
                m_status[16] = '0';

                // Position 17: Mapping Status (managed by mapping operations - keep current value)
                // m_status[17] is set by mapping operations

                // Position 18: Type (managed by mapping operations or configuration - keep current value)
                // m_status[18] is set by mapping type selection

                // Position 19: Reserve (keep existing value or set to '0')
                m_status[19] = '0';
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading sensor status: {ex.Message}");
                _errorMessage = $"Failed to read sensor status: {ex.Message}";
            }
        }
        #endregion

        #region Sensor Error Detection and Validation
        private void ValidateSensorStates(
            string operationName = "General",
            string specificSensorName = null,
            int? expectedSensorState = null,
            string operationErrorCode = null)
        {
            // 1. Update sensor status first (always get fresh data)
            UpdateSensorStatus();

            // 2. Smart operation-specific pre-condition validation (if specified)
            if (!string.IsNullOrEmpty(specificSensorName) && expectedSensorState.HasValue)
            {
                try
                {
                    // **SMART VALIDATION: Check if system is already in desired state**
                    bool shouldSkipValidation = ShouldSkipPreValidation(operationName, specificSensorName, expectedSensorState.Value);

                    if (!shouldSkipValidation)
                    {
                        ValidateSpecificSensor(specificSensorName, expectedSensorState.Value, operationErrorCode ?? ErrorCode.Error_Clamp_Sensor);
                        Debug.WriteLine($"Pre-operation validation passed for {operationName}: {specificSensorName} = {expectedSensorState.Value}");
                    }
                    else
                    {
                        Debug.WriteLine($"Pre-operation validation skipped for {operationName}: System already in desired state");
                    }
                }
                catch (SensorErrorException ex)
                {
                    Debug.WriteLine($"Pre-operation sensor validation failed for {operationName}: {ex.Message}");
                    throw;
                }
            }

            // 3. Check for conflicting sensor states (both 7x)
            CheckConflictingSensorStates();

            // 4. Check for individual sensor failures
            //CheckIndividualSensorFailures();

            Debug.WriteLine($"Comprehensive sensor validation passed for operation: {operationName}");
        }

        private bool ShouldSkipPreValidation(string operationName, string sensorName, int expectedState)
        {
            switch (operationName.ToUpper())
            {
                case "UNCLAMP":
                    // For unclamp operation, if we're already unclamped, we can proceed
                    if (sensorName.ToUpper() == "CLAMP" && expectedState == 1)
                    {
                        // Check if already unclamped - if so, skip validation
                        return _sensorStatus.StatusUnclamp == 1 && _sensorStatus.StatusClamp == 0;
                    }
                    break;

                case "CLAMP":
                    // For clamp operation, if we're already clamped, we can proceed
                    if (sensorName.ToUpper() == "UNCLAMP" && expectedState == 1)
                    {
                        // Check if already clamped - if so, skip validation
                        return _sensorStatus.StatusClamp == 1 && _sensorStatus.StatusUnclamp == 0;
                    }
                    break;

                case "UNLATCH":
                    // For unlatch operation, if we're already unlatched, we can proceed
                    if (sensorName.ToUpper() == "LATCH" && expectedState == 1)
                    {
                        // Check if already unlatched - if so, skip validation
                        return _sensorStatus.StatusUnlatch == 1 && _sensorStatus.StatusLatch == 0;
                    }
                    break;

                case "LATCH":
                    // For latch operation, if we're already latched, we can proceed
                    if (sensorName.ToUpper() == "UNLATCH" && expectedState == 1)
                    {
                        // Check if already latched - if so, skip validation
                        return _sensorStatus.StatusLatch == 1 && _sensorStatus.StatusUnlatch == 0;
                    }
                    break;

                case "ELEVATORUP":
                    // For elevator up operation, if we're already at top, we can proceed
                    if (sensorName.ToUpper() == "ELEVATOR_UP" && expectedState == 0)
                    {
                        // Check if already at top - if so, skip validation
                        return _sensorStatus.StatusElevatorUp == 1;
                    }
                    break;

                case "ELEVATORDOWN":
                    // For elevator down operation, if we're already at bottom, we can proceed
                    if (sensorName.ToUpper() == "ELEVATOR_DOWN" && expectedState == 0)
                    {
                        // Check if already at bottom - if so, skip validation
                        return _sensorStatus.StatusElevatorDown == 1;
                    }
                    break;

                case "DOCKFORWARD":
                    // For dock forward operation, if we're already extended, we can proceed
                    if (sensorName.ToUpper() == "DOCK_BACKWARD" && expectedState == 1)
                    {
                        // Check if already extended - if so, skip validation
                        return _sensorStatus.StatusDockForward == 1 && _sensorStatus.StatusDockBackward == 0;
                    }
                    break;

                case "DOCKBACKWARD":
                    // For dock backward operation, if we're already retracted, we can proceed
                    if (sensorName.ToUpper() == "DOCK_FORWARD" && expectedState == 1)
                    {
                        // Check if already retracted - if so, skip validation
                        return _sensorStatus.StatusDockBackward == 1 && _sensorStatus.StatusDockForward == 0;
                    }
                    break;

                case "DOORFORWARD":
                    // For door forward operation, if we're already open, we can proceed
                    if (sensorName.ToUpper() == "DOOR_BACKWARD" && expectedState == 1)
                    {
                        // Check if already open - if so, skip validation
                        return _sensorStatus.StatusDoorForward == 1 && _sensorStatus.StatusDoorBackward == 0;
                    }
                    break;

                case "DOORBACKWARD":
                    // For door backward operation, if we're already closed, we can proceed
                    if (sensorName.ToUpper() == "DOOR_FORWARD" && expectedState == 1)
                    {
                        // Check if already closed - if so, skip validation
                        return _sensorStatus.StatusDoorBackward == 1 && _sensorStatus.StatusDoorForward == 0;
                    }
                    break;

                case "MAPPINGFORWARD":
                    // For mapping forward operation, if we're already retracted, we can proceed
                    if (sensorName.ToUpper() == "MAPPING_BACKWARD" && expectedState == 1)
                    {
                        // Check if already retracted - if so, skip validation
                        return _sensorStatus.StatusMappingForward == 1 && _sensorStatus.StatusMappingBackward == 0;
                    }
                    break;

                case "MAPPINGBACKWARD":
                    // For mapping backward operation, if we're already extended, we can proceed
                    if (sensorName.ToUpper() == "MAPPING_FORWARD" && expectedState == 1)
                    {
                        // Check if already extended - if so, skip validation
                        return _sensorStatus.StatusMappingBackward == 1 && _sensorStatus.StatusMappingForward == 0;
                    }
                    break;

                // **SPECIAL CASE FOR ORIGIN/INITIALIZE OPERATIONS**
                case "ORIGIN":
                case "INITIALIZE":
                case "RESET ERROR":
                    // For origin/initialize operations, we should be more lenient
                    // Allow operations to proceed if system is in a reasonable state
                    Debug.WriteLine($"Origin/Initialize operation detected - applying lenient validation for {sensorName}");

                    // Skip validation for most sensors during origin, except for conflicting states
                    switch (sensorName.ToUpper())
                    {
                        case "CLAMP":
                        case "UNCLAMP":
                            // Allow origin to proceed regardless of clamp state
                            return true;
                        case "LATCH":
                        case "UNLATCH":
                            // Allow origin to proceed regardless of latch state
                            return true;
                        case "ELEVATOR_UP":
                        case "ELEVATOR_DOWN":
                            // Allow origin to proceed regardless of elevator position
                            return true;
                        case "DOCK_FORWARD":
                        case "DOCK_BACKWARD":
                            // Allow origin to proceed regardless of dock position
                            return true;
                        case "DOOR_FORWARD":
                        case "DOOR_BACKWARD":
                            // Allow origin to proceed regardless of door position
                            return true;
                        case "MAPPING_FORWARD":
                        case "MAPPING_BACKWARD":
                            // Allow origin to proceed regardless of mapping position
                            return true;
                    }
                    break;
            }

            return false; // Don't skip validation by default
        }
        private void CheckConflictingSensorStates(string operationName = null)
        {
            // If operation name is provided, only check relevant conflicts for that operation
            if (!string.IsNullOrEmpty(operationName))
            {
                switch (operationName.ToLower())
                {
                    case "clamp":
                    case "unclamp":
                        CheckClampConflicts();
                        break;
                    case "dockforward":
                    case "dockbackward":
                        CheckDockConflicts();
                        break;
                    case "latch":
                    case "unlatch":
                        CheckLatchConflicts();
                        break;
                    case "doorforward":
                    case "doorbackward":
                        CheckDoorConflicts();
                        break;
                    case "elevatorup":
                    case "elevatordown":
                    case "elevatormappingstartposition":
                    case "elevatormappingendposition":
                        CheckElevatorConflicts();
                        break;
                    case "mappingforward":
                    case "mappingbackward":
                        CheckMappingConflicts();
                        break;
                    default:
                        // For unknown operations or general checks, check all conflicts
                        CheckAllConflicts();
                        break;
                }
            }
            else
            {
                // No operation specified - check all conflicts (for UpdateSensorStatus)
                CheckAllConflicts();
            }
        }
        // Helper method for specific conflict checks
        private void CheckClampConflicts()
        {
            if (_sensorStatus.StatusClamp == 1 && _sensorStatus.StatusUnclamp == 1)
            {
                sErrorCode = ErrorCode.Error_Clamp_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_Clamp_Sensor,
                    "Clamp Sensors",
                    "Both clamp and unclamp sensors are detected simultaneously"
                );
            }
        }

        private void CheckDockConflicts()
        {
            if (_sensorStatus.StatusDockForward == 1 && _sensorStatus.StatusDockBackward == 1)
            {
                sErrorCode = ErrorCode.Error_Dock_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_Dock_Sensor,
                    "Dock Sensors",
                    "Both dock forward and backward sensors are detected simultaneously"
                );
            }
        }

        private void CheckLatchConflicts()
        {
            if (_sensorStatus.StatusLatch == 1 && _sensorStatus.StatusUnlatch == 1)
            {
                sErrorCode = ErrorCode.Error_Latch_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_Latch_Sensor,
                    "Latch Sensors",
                    "Both latch and unlatch sensors are detected simultaneously"
                );
            }
        }

        private void CheckDoorConflicts()
        {
            if (_sensorStatus.StatusDoorForward == 1 && _sensorStatus.StatusDoorBackward == 1)
            {
                sErrorCode = ErrorCode.Error_Door_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_Door_Sensor,
                    "Door Sensors",
                    "Both door open and close sensors are detected simultaneously"
                );
            }
        }

        private void CheckMappingConflicts()
        {
            if (_sensorStatus.StatusMappingForward == 1 && _sensorStatus.StatusMappingBackward == 1)
            {
                sErrorCode = ErrorCode.Error_Mapping_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_Mapping_Sensor,
                    "Mapping Sensors",
                    "Both mapping in and out sensors are detected simultaneously"
                );
            }
        }

        private void CheckElevatorConflicts()
        {
            if (_sensorStatus.StatusElevatorUp == 1 && _sensorStatus.StatusElevatorDown == 1)
            {
                sErrorCode = ErrorCode.Error_ElevatorAxis_Sensor;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    ErrorCode.Error_ElevatorAxis_Sensor,
                    "Elevator Axis Sensors",
                    "Both elevator up and down limit sensors are detected simultaneously"
                );
            }
        }

        // Helper method to check all conflicts (for general validation)
        private void CheckAllConflicts()
        {
            CheckClampConflicts();
            CheckDockConflicts();
            CheckLatchConflicts();
            CheckDoorConflicts();
            CheckMappingConflicts();
            CheckElevatorConflicts();
        }
        private bool IsAdapterType()
        {
            // Check if current FOUP type is Adapter based on status or settings

            // Option 1: Check based on m_status[18] (PodType)
            char currentPodType = m_status[18];

            // Option 2: Check based on current active mapping type or sequence type
            try
            {
                // Try to get the current sequence type from settings if available
                var settings = FOUPCtrl.Models.Settings.Instance;
                if (settings?.CurrentProfile?.Name?.ToUpper().Contains("ADAPTOR") == true ||
                    settings?.CurrentProfile?.Name?.ToUpper().Contains("ADAPTER") == true)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking adapter type from settings: {ex.Message}");
            }

            // Option 3: Check based on PodType enum values
            // Assuming Adapter corresponds to a specific PodType value
            // You may need to adjust this based on your actual PodType enum values
            if (currentPodType == (char)((int)PodType.Type1 + 1)) // Assuming Adapter is Type2
            {
                return true;
            }

            return false;
        }
        private void ValidateSpecificSensor(string sensorName, int expectedState, string errorCode)
        {
            UpdateSensorStatus();

            int actualState = -1;
            switch (sensorName.ToUpper())
            {
                case "CLAMP":
                    actualState = _sensorStatus.StatusClamp;
                    break;
                case "UNCLAMP":
                    actualState = _sensorStatus.StatusUnclamp;
                    break;
                case "LATCH":
                    actualState = _sensorStatus.StatusLatch;
                    break;
                case "UNLATCH":
                    actualState = _sensorStatus.StatusUnlatch;
                    break;
                case "ELEVATOR_UP":
                    actualState = _sensorStatus.StatusElevatorUp;
                    break;
                case "ELEVATOR_DOWN":
                    actualState = _sensorStatus.StatusElevatorDown;
                    break;
                case "DOOR_FORWARD":
                    actualState = _sensorStatus.StatusDoorForward;
                    break;
                case "DOOR_BACKWARD":
                    actualState = _sensorStatus.StatusDoorBackward;
                    break;
                case "DOCK_FORWARD":
                    actualState = _sensorStatus.StatusDockForward;
                    break;
                case "DOCK_BACKWARD":
                    actualState = _sensorStatus.StatusDockBackward;
                    break;
                case "MAPPING_FORWARD":
                    actualState = _sensorStatus.StatusMappingForward;
                    break;
                case "MAPPING_BACKWARD":
                    actualState = _sensorStatus.StatusMappingBackward;
                    break;
                case "VACUUM":
                    actualState = _sensorStatus.StatusVacuum;
                    break;
                case "PROTRUSION":
                    actualState = _sensorStatus.StatusProtrusion;
                    break;
                default:
                    throw new ArgumentException($"Unknown sensor name: {sensorName}");
            }

            if (actualState != expectedState)
            {
                sErrorCode = errorCode;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                throw new SensorErrorException(
                    errorCode,
                    sensorName,
                    $"Sensor state mismatch - Expected: {expectedState}, Actual: {actualState}"
                );
            }
        }
        #endregion

        #region Continuous Monitoring
        private void CheckDockHandPinchErrorContinuous()
        {
            try
            {
                // Read the dock head pinch sensor (bit 4 on card 1)
                int pinchSensorStatus = ReadBit(_credenIOCard1, DockHandPinchSensor);

                // Check if hand pinch is detected
                if (pinchSensorStatus == 1)
                {
                    sErrorCode = ErrorCode.Error_DockHandPinch;
                    m_status[0] = (char)MachineStatus.RecoverableError;
                    _errorMessage = "Dock hand pinch detected - Check for foreign matter or interfering object";

                    throw new SensorErrorException(
                        ErrorCode.Error_DockHandPinch,
                        "Dock Head Pinch Sensor",
                        "Hand pinch detected during dock operation - foreign matter or interfering object detected"
                    );
                }
            }
            catch (SensorErrorException)
            {
                throw; // Re-throw sensor exceptions
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous dock hand pinch monitoring: {ex.Message}");
            }
        }
        private void CheckDoorRetentionErrorContinuous()
        {
            try
            {
                // **SIMPLIFIED: Only check if vacuum is lost - no other conditions**
                bool vacuumIsLost = _sensorStatus.StatusVacuum == 0;
                bool doorIsUnlatched = _sensorStatus.StatusUnlatch == 1;

                if (vacuumIsLost && doorIsUnlatched)
                {
                    string detailedMessage = $"A0 Vacuum Lost Error Detected - " +
                                            $"Vacuum: Lost, " +
                                            //$"Door: {(_sensorStatus.StatusDoorForward == 1 ? "Open" : "Closed")}, " +
                                            $"Latch: {(_sensorStatus.StatusUnlatch == 1 ? "Unlatched" : "Latched")}";

                    Debug.WriteLine($"CONTINUOUS MONITORING: {detailedMessage}");

                    sErrorCode = ErrorCode.Error_WaferDrop;
                    m_status[0] = (char)MachineStatus.RecoverableError;
                    _errorMessage = "Vacuum lost during operation - Check vacuum system immediately";

                    throw new SensorErrorException(
                        ErrorCode.Error_WaferDrop,
                        "Vacuum Monitor (Continuous)",
                        detailedMessage
                    );
                }
            }
            catch (SensorErrorException)
            {
                throw; // Re-throw sensor exceptions
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous vacuum monitoring: {ex.Message}");
            }
        }
        private void CheckWaferProtrusionErrorContinuous()
        {
            try
            {
                if (_sensorStatus.StatusProtrusion == 0)
                {
                    sErrorCode = ErrorCode.Error_WaferProtruded;
                    m_status[0] = (char)MachineStatus.RecoverableError;
                    _errorMessage = "Wafer protrusion detected - Check wafer placement";

                    throw new SensorErrorException(
                        ErrorCode.Error_WaferProtruded,
                        "Wafer Protrusion (A1)",
                        "Wafer protrusion sensor indicates improper wafer placement. Check wafer positioning in cassette."
                    );
                }
            }
            catch (SensorErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous wafer protrusion monitoring: {ex.Message}");
            }
        }
        private void CheckFOUPMountSensorErrorContinuous()
        {
            try
            {
                bool presence1And2Active = _sensorStatus.StatusPresence1And2 == 1;
                bool presence3Active = _sensorStatus.StatusPresence3 == 1;

                // Check if main presence sensors are not detecting properly
                if (!presence1And2Active || !presence3Active)
                {
                    sErrorCode = ErrorCode.Error_FOUPMount_Sensor;
                    m_status[0] = (char)MachineStatus.RecoverableError;
                    _errorMessage = "FOUP mount sensor error - Main presence sensors not detecting";

                    throw new SensorErrorException(
                        ErrorCode.Error_FOUPMount_Sensor,
                        "FOUP Mount Sensors (A2)",
                        "Main presence sensor error detected during continuous monitoring."
                    );
                }
            }
            catch (SensorErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous FOUP mount sensor monitoring (A2): {ex.Message}");
            }
        }
        private void CheckFOUPMountLoadErrorContinuous()
        {
            try
            {
                // Only check diagonal sensors for Adapter type
                if (IsAdapterType())
                {
                    bool presenceDiag1Active = _sensorStatus.StatusPresenceDiagonal1 == 0;
                    bool presenceDiag2Active = _sensorStatus.StatusPresenceDiagonal2 == 0;

                    if (!presenceDiag1Active || !presenceDiag2Active)
                    {
                        sErrorCode = ErrorCode.Error_FOUPMount_Load;
                        m_status[0] = (char)MachineStatus.RecoverableError;
                        _errorMessage = "FOUP mount load error - Diagonal presence sensors not detecting";

                        throw new SensorErrorException(
                            ErrorCode.Error_FOUPMount_Load,
                            "FOUP Mount Load (A3)",
                            "FOUP mount load error detected during continuous monitoring for Adapter type."
                        );
                    }
                }
            }
            catch (SensorErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous FOUP mount load monitoring (A3): {ex.Message}");
            }
        }
        private void CheckAirPressureErrorContinuous()
        {
            try
            {
                int pressureSensorStatus = ReadBit(_credenIOCard2, 3);

                if (pressureSensorStatus == 0)
                {
                    sErrorCode = ErrorCode.Error_AirPressure;
                    m_status[0] = (char)MachineStatus.RecoverableError;
                    _errorMessage = "Air pressure sensor error - Insufficient air pressure";

                    throw new SensorErrorException(
                        ErrorCode.Error_AirPressure,
                        "Air Pressure (A5)",
                        "Air pressure sensor indicates insufficient pressure. Check air supply and pressure sensor."
                    );
                }
            }
            catch (SensorErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in continuous air pressure monitoring: {ex.Message}");
            }
        }
        private bool StartContinuousDoorRetentionMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "doorretention");
        }
        private bool StartContinuousWaferProtrusionMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "waferprotrusion");
        }
        private bool StartContinuousFOUPMountSensorMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "foupmountsensor");
        }
        private bool StartContinuousFOUPMountLoadMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "foupmountload");
        }
        private bool StartContinuousAirPressureMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "airpressure");
        }
        private bool StartContinuousDockHandPinchMonitoring(CancellationToken token)
        {
            return StartContinuousSensorMonitoring(token, "dockhandpinch");
        }
        private bool StartContinuousSensorMonitoring(CancellationToken token, string sensorType)
        {
            try
            {
                Debug.WriteLine($"Starting continuous sensor monitoring for: {sensorType}");

                // Get or create the appropriate cancellation token source for this sensor type
                CancellationTokenSource monitoringCts = GetMonitoringCts(sensorType);

                // Cancel existing monitoring for this specific sensor type only
                if (monitoringCts != null && !monitoringCts.Token.IsCancellationRequested)
                {
                    Debug.WriteLine($"Stopping existing {sensorType} monitoring");
                    monitoringCts.Cancel();
                    monitoringCts.Dispose();
                }

                // Create new cancellation token source for this sensor type
                monitoringCts = new CancellationTokenSource();
                SetMonitoringCts(sensorType, monitoringCts);

                // Start monitoring task for this specific sensor type
                Task.Run(async () => await ContinuousSensorMonitoringTask(monitoringCts.Token, sensorType),
                            monitoringCts.Token);

                // **CRITICAL FIX: Add a delay to allow sensor monitoring to detect errors before continuing**
                System.Threading.Thread.Sleep(200); // 200ms delay to allow monitoring to check sensors

                // **CRITICAL FIX: Check if sensor monitoring already detected an error**
                if (_sequenceCancellationTokenSource != null && _sequenceCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.WriteLine("Sensor monitoring detected error during startup - cancelling sequence");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start sensor monitoring: {ex.Message}");
                _errorMessage = $"Failed to start sensor monitoring: {ex.Message}";
                return false;
            }
        }
        private CancellationTokenSource GetMonitoringCts(string sensorType)
        {
            switch (sensorType.ToLower())
            {
                case "doorretention":
                case "vacuum":
                case "a0":
                    return _doorRetentionMonitoringCts;
                case "airpressure":
                case "pressure":
                case "a5":
                    return _airPressureMonitoringCts;
                case "foupmountsensor":
                case "a2":
                    return _foupMountSensorMonitoringCts;
                case "waferprotrusion":
                case "protrusion":
                case "a1":
                    return _waferProtrusionMonitoringCts;
                case "foupmountload":
                case "a3":
                    return _foupMountLoadMonitoringCts;
                case "dockhandpinch":
                case "handpinch":
                case "fe":
                    return _dockHandPinchMonitoringCts;
                default:
                    return _doorRetentionMonitoringCts; // fallback
            }
        }
        private void SetMonitoringCts(string sensorType, CancellationTokenSource cts)
        {
            switch (sensorType.ToLower())
            {
                case "doorretention":
                case "vacuum":
                case "a0":
                    _doorRetentionMonitoringCts = cts;
                    break;
                case "airpressure":
                case "pressure":
                case "a5":
                    _airPressureMonitoringCts = cts;
                    break;
                case "foupmountsensor":
                case "a2":
                    _foupMountSensorMonitoringCts = cts;
                    break;
                case "waferprotrusion":
                case "protrusion":
                case "a1":
                    _waferProtrusionMonitoringCts = cts;
                    break;
                case "fourmountload":
                case "a3":
                    _foupMountLoadMonitoringCts = cts;
                    break;
                case "dockhandpinch":
                case "handpinch":
                case "fe":
                    _dockHandPinchMonitoringCts = cts;
                    break;
            }
        }
        private async Task ContinuousSensorMonitoringTask(CancellationToken monitoringToken, string sensorType)
        {
            Debug.WriteLine($"Sensor monitoring task started for: {sensorType}");

            try
            {
                // **CRITICAL FIX: Do an immediate sensor check before starting continuous monitoring**
                try
                {
                    UpdateSensorStatus();
                    CheckSensorErrorContinuous(sensorType);
                    Debug.WriteLine($"Initial sensor check passed for: {sensorType}");
                }
                catch (SensorErrorException ex)
                {
                    Debug.WriteLine($"IMMEDIATE sensor error detected: {ex.ErrorCode} - {ex.Message}");
                    _errorMessage = ex.Message;
                    sErrorCode = ex.ErrorCode;
                    m_status[0] = (char)MachineStatus.RecoverableError;

                    // **CRITICAL FIX: Cancel the ongoing sequence immediately**
                    if (_sequenceCancellationTokenSource != null && !_sequenceCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Debug.WriteLine("EMERGENCY STOP: Cancelling ongoing sequence due to immediate sensor error");
                        _sequenceCancellationTokenSource.Cancel();
                    }

                    // **CRITICAL FIX: Stop all motor operations immediately**
                    await SafelyDisableAllOutputs();

                    // Show error dialog
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string title = $"Sensor Error ({ex.ErrorCode})";
                        string message = $"Error {ex.ErrorCode} Detected!\n\n{ex.Message}\n\nSequence has been stopped for safety.\n\nRecommended Actions:\n1. Check the affected sensor/system\n2. Reset error after fixing the issue";
                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                    });

                    return; // Stop monitoring after immediate error
                }

                // Continue with continuous monitoring if initial check passed
                while (!monitoringToken.IsCancellationRequested)
                {
                    try
                    {
                        UpdateSensorStatus();
                        CheckSensorErrorContinuous(sensorType);
                        await Task.Delay(100, monitoringToken);
                    }
                    catch (SensorErrorException ex)
                    {
                        Debug.WriteLine($"Sensor monitoring detected error: {ex.ErrorCode} - {ex.Message}");
                        _errorMessage = ex.Message;
                        sErrorCode = ex.ErrorCode;
                        m_status[0] = (char)MachineStatus.RecoverableError;

                        // **CRITICAL FIX: Cancel the ongoing sequence immediately**
                        if (_sequenceCancellationTokenSource != null && !_sequenceCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            Debug.WriteLine("EMERGENCY STOP: Cancelling ongoing sequence due to sensor error");
                            _sequenceCancellationTokenSource.Cancel();
                        }

                        // **CRITICAL FIX: Stop all motor operations immediately**
                        await SafelyDisableAllOutputs();

                        // Show error dialog
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            string title = $"Sensor Error ({ex.ErrorCode})";
                            string message = $"Error {ex.ErrorCode} Detected!\n\n{ex.Message}\n\nSequence has been stopped for safety.\n\nRecommended Actions:\n1. Check the affected sensor/system\n2. Reset error after fixing the issue";
                            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                        });

                        return; // Stop monitoring after error
                    }
                    catch (TaskCanceledException)
                    {
                        // **FIXED: Don't log TaskCanceledException as an error**
                        Debug.WriteLine($"Sensor monitoring task for {sensorType} was cancelled (normal operation)");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // **FIXED: Don't log OperationCanceledException as an error**
                        Debug.WriteLine($"Sensor monitoring task for {sensorType} was cancelled (normal operation)");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Sensor monitoring task error for {sensorType}: {ex.Message}");
                        await Task.Delay(1000, monitoringToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"Sensor monitoring task for {sensorType} was cancelled during startup");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Sensor monitoring task for {sensorType} was cancelled");
            }
        }
        private void CheckSensorErrorContinuous(string sensorType)
        {
            switch (sensorType.ToLower())
            {
                case "doorretention":
                case "vacuum":
                case "a0":
                    CheckDoorRetentionErrorContinuous();
                    break;
                case "waferprotrusion":
                case "protrusion":
                case "a1":
                    CheckWaferProtrusionErrorContinuous();
                    break;
                case "foupmountsensor":
                case "a2":
                    CheckFOUPMountSensorErrorContinuous();
                    break;
                case "foupmountload":
                case "a3":
                    CheckFOUPMountLoadErrorContinuous();
                    break;
                case "airpressure":
                case "pressure":
                case "a5":
                    CheckAirPressureErrorContinuous();
                    break;
                case "dockhandpinch":
                case "handpinch":
                case "fe":
                    CheckDockHandPinchErrorContinuous();
                    break;
                default:
                    Debug.WriteLine($"Unknown sensor type for continuous monitoring: {sensorType}");
                    break;
            }
        }
        public void StopContinuousDoorRetentionMonitoring()
        {
            try
            {
                Debug.WriteLine("Stopping all continuous sensor monitoring tasks");

                var monitoringTasks = new[]
                {
                    (_doorRetentionMonitoringCts, "Door Retention"),
                    (_airPressureMonitoringCts, "Air Pressure"),
                    (_foupMountSensorMonitoringCts, "FOUP Mount Sensor"),
                    (_waferProtrusionMonitoringCts, "Wafer Protrusion"),
                    (_foupMountLoadMonitoringCts, "FOUP Mount Load"),
                    (_dockHandPinchMonitoringCts, "Dock Hand Pinch")
                };

                foreach (var (cts, name) in monitoringTasks)
                {
                    if (cts != null && !cts.Token.IsCancellationRequested)
                    {
                        Debug.WriteLine($"Stopping {name} monitoring");
                        cts.Cancel();
                        cts.Dispose();
                    }
                }

                // Clear all references
                _doorRetentionMonitoringCts = null;
                _airPressureMonitoringCts = null;
                _foupMountSensorMonitoringCts = null;
                _waferProtrusionMonitoringCts = null;
                _foupMountLoadMonitoringCts = null;
                _dockHandPinchMonitoringCts = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping monitoring tasks: {ex.Message}");
            }
        }
        private bool StopVacuumMonitoringForOrigin(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("Stopping vacuum monitoring before vacuum off operation");

                if (_doorRetentionMonitoringCts != null && !_doorRetentionMonitoringCts.Token.IsCancellationRequested)
                {
                    Debug.WriteLine("Cancelling door retention monitoring for origin sequence");
                    _doorRetentionMonitoringCts.Cancel();
                    _doorRetentionMonitoringCts.Dispose();
                    _doorRetentionMonitoringCts = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping vacuum monitoring: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Basic Control Operations
        public bool Clamp(CancellationToken token)
        {
            if (!CanExecuteOperation("Clamp"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            if (_sensorStatus.StatusClamp == 0)
                writeByte = SetBit(writeByte, _outputList.Clamp);
            else
                writeByte = ClearBit(writeByte, _outputList.Clamp);

            int portId = _outputList.Clamp < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusClamp == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > clampTimeOver)
                    {
                        throw new TimeoutException("Clamp Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("clamp"); // ✅ Only check clamp-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Clamp_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during clamp operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Clamp operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during clamp operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool Unclamp(CancellationToken token)
        {
            if (!CanExecuteOperation("Unclamp"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            if (_sensorStatus.StatusUnclamp == 0)
                writeByte = SetBit(writeByte, _outputList.Unclamp);
            else
                writeByte = ClearBit(writeByte, _outputList.Unclamp);

            int portId = _outputList.Unclamp < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusUnclamp == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > clampTimeOver)
                    {
                        throw new TimeoutException("Unclamp Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("unclamp"); // ✅ Only check unclamp-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Unclamp_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during unclamp operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Unclamp operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during unclamp operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool Latch(CancellationToken token)
        {
            if (!CanExecuteOperation("Latch"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            byte writeByte = 0;
            if (_sensorStatus.StatusLatch == 0)
                writeByte = SetBit(writeByte, _outputList.Latch);
            else
                writeByte = ClearBit(writeByte, _outputList.Latch);

            int portId = _outputList.Latch < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusLatch == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > latchTimeOver)
                    {
                        throw new TimeoutException("Latch Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("latch"); // ✅ Only check latch-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Latch_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during latch operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Latch operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during latch operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool Unlatch(CancellationToken token)
        {
            if (!CanExecuteOperation("Unlatch"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            byte writeByte = 0;
            if (_sensorStatus.StatusUnlatch == 0)
                writeByte = SetBit(writeByte, _outputList.Unlatch);
            else
                writeByte = ClearBit(writeByte, _outputList.Unlatch);

            int portId = _outputList.Unlatch < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusUnlatch == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > latchTimeOver)
                    {
                        throw new TimeoutException("Unlatch Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("unlatch"); // ✅ Only check unlatch-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Unlatch_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during unlatch operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Unlatch operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during unlatch operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Elevator Up operation
        public bool ElevatorUp(CancellationToken token)
        {
            if (!CanExecuteOperation("ElevatorUp"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            writeByte = SetBit(writeByte, _outputList.ElevatorUp1);
            writeByte = SetBit(writeByte, _outputList.ElevatorUp2);

            int portId = _outputList.ElevatorUp1 < 8 ? 2 : 3;

            try
            {
                Debug.WriteLine($"Writing to port {portId} on card 1, setting Elevator Up bits to {writeByte}");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusElevatorUp == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > elevatorTimeOver)
                    {
                        throw new TimeoutException("Elevator Up Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("elevatorup"); // ✅ Only check elevator-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Elevator_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during elevator up operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Elevator up operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during elevator up operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Elevator Down operation
        public bool ElevatorDown(CancellationToken token)
        {
            if (!CanExecuteOperation("ElevatorDown"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            byte writeByte = 0;
            writeByte = SetBit(writeByte, _outputList.ElevatorDown1);
            writeByte = SetBit(writeByte, _outputList.ElevatorDown2);

            int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusElevatorDown == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > elevatorTimeOver)
                    {
                        throw new TimeoutException("Elevator Down Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("elevatordown"); // ✅ Only check elevator-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Elevator_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during elevator down operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Elevator down operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during elevator down operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Door Forward (Open) operation
        public bool DoorForward(CancellationToken token)
        {
            if (!CanExecuteOperation("DoorForward"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Ensure elevator is in up position
            if (_sensorStatus.StatusElevatorUp != 1)
            {
                _errorMessage = "Elevator must be in the up position.";
                return false;
            }

            byte writeByte = 0;
            if (_sensorStatus.StatusDoorForward == 0)
                writeByte = SetBit(writeByte, _outputList.DoorForward);
            else
                writeByte = ClearBit(writeByte, _outputList.DoorForward);

            int portId = _outputList.DoorForward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDoorForward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > doorTimeOver)
                    {
                        throw new TimeoutException("Door Forward Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("doorforward"); // ✅ Only check door-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Door_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during door forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Door forward operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during door forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Door Backward (Close) operation
        public bool DoorBackward(CancellationToken token)
        {
            if (!CanExecuteOperation("DoorBackward"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Ensure elevator is in up position
            if (_sensorStatus.StatusElevatorUp != 1)
            {
                _errorMessage = "Elevator must be in the up position.";
                return false;
            }

            byte writeByte = 0;
            // Set door backward output bit if the sensor is off; otherwise, clear it
            if (_sensorStatus.StatusDoorBackward == 0)
                writeByte = SetBit(writeByte, _outputList.DoorBackward);
            else
                writeByte = ClearBit(writeByte, _outputList.DoorBackward);

            int portId = _outputList.DoorBackward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDoorBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > doorTimeOver)
                    {
                        throw new TimeoutException("Door Backward Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("doorbackward"); // ✅ Only check door-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Door_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during door backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Door backward operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during door backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool DockForward(CancellationToken token)
        {
            if (!CanExecuteOperation("DockForward"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;

            if (_sensorStatus.StatusDockForward == 0)
                writeByte = SetBit(writeByte, _outputList.DockForward);
            else
                writeByte = ClearBit(writeByte, _outputList.DockForward);

            int portId = _outputList.DockForward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDockForward == 0)
                {
                    // **CRITICAL: Check for cancellation frequently during operation**
                    token.ThrowIfCancellationRequested();

                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > dockTimeOver)
                    {
                        throw new TimeoutException("Dock Forward Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("dockforward"); // ✅ Only check dock-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("DockForward operation was cancelled - stopping motor immediately");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                throw; // Re-throw to propagate cancellation
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Dock_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during dock forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during dock forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Dock Backward (Retract) operation
        public bool DockBackward(CancellationToken token)
        {
            if (!CanExecuteOperation("DockBackward"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;

            if (_sensorStatus.StatusDockBackward == 0)
                writeByte = SetBit(writeByte, _outputList.DockBackward);
            else
                writeByte = ClearBit(writeByte, _outputList.DockBackward);

            int portId = _outputList.DockBackward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDockBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > dockTimeOver)
                    {
                        throw new TimeoutException("Dock Backward Timeover");
                    }

                    UpdateSensorStatus();
                    CheckConflictingSensorStates("dockbackward"); // ✅ Only check dock-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Dock_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during dock backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Dock backward operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during dock backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }
        // Mapping Forward operation (actually retracts the mapping mechanism)
        public bool MappingForward(CancellationToken token)
        {
            if (!CanExecuteOperation("MappingForward"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            writeByte = SetBit(writeByte, _outputList.MappingForward);

            int portId = _outputList.MappingForward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusMappingForward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > mappingTimeOver)
                    {
                        throw new TimeoutException("Mapping Forward Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("mappingforward"); // ✅ Only check mapping-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Mapping_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during mapping forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Mapping forward operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during mapping forward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Mapping Backward operation (actually extends the mapping mechanism)
        public bool MappingBackward(CancellationToken token)
        {
            if (!CanExecuteOperation("MappingBackward"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            writeByte = SetBit(writeByte, _outputList.MappingBackward);

            int portId = _outputList.MappingBackward < 8 ? 2 : 3;

            try
            {
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusMappingBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > mappingTimeOver)
                    {
                        throw new TimeoutException("Mapping Backward Timeover");
                    }
                    UpdateSensorStatus();
                    CheckConflictingSensorStates("mappingbackward"); // ✅ Only check mapping-related conflicts
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Mapping_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during mapping backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Mapping backward operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during mapping backward operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Vacuum On operation
        public bool VacuumOn(CancellationToken token)
        {
            if (!CanExecuteOperation("VacuumOn"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            int portId = VacuumValve1B < 8 ? 2 : 3;

            try
            {
                // **NEW: Check if vacuum is already on before attempting operation**
                UpdateSensorStatus();
                if (_sensorStatus.StatusVacuum == 1)
                {
                    Debug.WriteLine("Vacuum is already ON - no operation needed");
                    m_status[9] = (char)VacuumStatus.On;
                    return true;
                }

                Debug.WriteLine("Vacuum is OFF - proceeding with vacuum on operation");

                byte writeByte = 0;
                writeByte = SetBit(writeByte, VacuumValve1B);

                Debug.WriteLine("Turning ON VACUUM VALVE 1B (Card 1, Bit 1)");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                // **ENHANCED: Dynamic timeout based on system conditions**
                int timeoutMs = vacuumTimeOver; // Base timeout

                // Check container type or system state for adjusted timeout
                if (m_status[18] == (char)PodType.Type2) // Adapter type example
                {
                    timeoutMs = vacuumTimeOver + 500; // Adapters might need more time
                }

                var stopwatch = Stopwatch.StartNew();

                while (_sensorStatus.StatusVacuum == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;

                    if (elapsedMS > timeoutMs)
                    {
                        // **ENHANCED: Try recovery before failing**
                        Debug.WriteLine($"Vacuum On: Timeout after {elapsedMS}ms - attempting recovery");

                        // Turn off valve 1B first
                        DigitalWrite(_credenIOCard1, portId, (byte)0);
                        Thread.Sleep(100);

                        // Check if vacuum sensor is now responding
                        UpdateSensorStatus();
                        if (_sensorStatus.StatusVacuum == 1)
                        {
                            Debug.WriteLine("Vacuum On: Recovery successful - vacuum detected");
                            m_status[9] = (char)VacuumStatus.On;
                            return true;
                        }

                        throw new TimeoutException("Vacuum sensor did not activate - Vacuum On Timeover");
                    }

                    // **ENHANCED: More responsive status checking**
                    Thread.Sleep(50); // Shorter delay for more responsive checking
                    UpdateSensorStatus();
                }

                Debug.WriteLine("Vacuum sensor activated, turning OFF VACUUM VALVE 1B (Card 1, Bit 1)");
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                // **NEW: Update status array to reflect actual state**
                m_status[9] = (char)VacuumStatus.On;

                Debug.WriteLine("Vacuum On operation completed successfully");
                return true;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Vacuum On timeout: {ex.Message} - activating VACUUM VALVE 1A (Card 1, Bit 0) to close vacuum");

                try
                {
                    int valve1APortId = VacuumValve1A < 8 ? 2 : 3;
                    byte valve1AWriteByte = 0;
                    valve1AWriteByte = SetBit(valve1AWriteByte, VacuumValve1A);

                    Debug.WriteLine("Turning ON VACUUM VALVE 1A (Card 1, Bit 0) to close vacuum");
                    DigitalWrite(_credenIOCard1, valve1APortId, valve1AWriteByte);

                    Thread.Sleep(100);

                    Debug.WriteLine("Immediately turning OFF VACUUM VALVE 1A (Card 1, Bit 0)");
                    DigitalWrite(_credenIOCard1, valve1APortId, (byte)0);
                }
                catch (Exception cleanupEx)
                {
                    Debug.WriteLine($"Error during timeout vacuum valve 1A operation: {cleanupEx.Message}");
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                sErrorCode = ErrorCode.Error_Vacuum_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                _errorMessage = "Vacuum sensor failed to activate within timeout period";
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during vacuum on operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Vacuum on operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during vacuum on operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Vacuum Off operation
        public bool VacuumOff(CancellationToken token)
        {
            if (!CanExecuteOperation("VacuumOff"))
            {
                return false;
            }

            if (!ConnectionIOCard1)
                return false;

            int portId = VacuumValve1A < 8 ? 2 : 3;

            try
            {
                // **NEW: Check if vacuum is already off before attempting operation**
                UpdateSensorStatus();
                if (_sensorStatus.StatusVacuum == 0)
                {
                    Debug.WriteLine("Vacuum is already OFF - no operation needed");
                    m_status[9] = (char)VacuumStatus.Off;
                    return true;
                }

                Debug.WriteLine("Vacuum is ON - proceeding with vacuum off operation");

                byte writeByte = 0;
                writeByte = SetBit(writeByte, VacuumValve1A);

                Debug.WriteLine("Turning ON VACUUM VALVE 1A (Card 1, Bit 0) to release vacuum");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                // **ENHANCED: Shorter initial delay but with status check**
                Thread.Sleep(100); // Reduced from 200ms

                // **ENHANCED: Dynamic timeout based on system state**
                int timeoutMs = vacuumTimeOver; // Base timeout

                // Check if we're in a sequence that might need more time
                if (m_status[3] == (char)Operation.Operating)
                {
                    timeoutMs = vacuumTimeOver + 500; // Extra time during operations
                }

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusVacuum == 1)
                {
                    token.ThrowIfCancellationRequested();

                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > timeoutMs)
                    {
                        // **ENHANCED: Graceful timeout handling**
                        Debug.WriteLine($"Vacuum Off: Timeout after {elapsedMS}ms - checking final status");

                        // Final status check before declaring failure
                        UpdateSensorStatus();
                        if (_sensorStatus.StatusVacuum == 0)
                        {
                            Debug.WriteLine("Vacuum Off: Final check shows vacuum is OFF - operation successful");
                            break;
                        }

                        throw new TimeoutException("Vacuum Off Timeover - Vacuum sensor did not deactivate");
                    }

                    // **ENHANCED: More frequent status updates during critical phase**
                    if (elapsedMS > 500) // After 500ms, check status more frequently
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }

                    UpdateSensorStatus();
                }

                // **ENHANCED: Ensure valve is turned off**
                Debug.WriteLine("Vacuum sensor deactivated, turning OFF VACUUM VALVE 1A (Card 1, Bit 0)");
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                // **NEW: Update status array to reflect actual state**
                m_status[9] = (char)VacuumStatus.Off;

                Debug.WriteLine("Vacuum Off operation completed successfully");
                return true;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Vacuum Off timeout: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                // **ENHANCED: For origin operations, be more lenient**
                string currentOperation = m_status[3] == (char)Operation.Operating ? "during operation" : "during origin";
                Debug.WriteLine($"Vacuum off timeout occurred {currentOperation}");

                // For origin operations, we might want to be less strict
                if (m_status[2] != (char)LoadStatus.LoadPosition)
                {
                    Debug.WriteLine("Not in load position - treating timeout as recoverable for origin");
                    m_status[9] = (char)VacuumStatus.Off; // Set status as off anyway
                    sErrorCode = ErrorCode.Error_VacuumRelease_Timeover; // Use vacuum release error instead
                    m_status[0] = (char)MachineStatus.RecoverableError; // Make it recoverable
                }
                else
                {
                    sErrorCode = ErrorCode.Error_VacuumRelease_Timeover;
                    m_status[0] = (char)MachineStatus.UnrecoverableError;
                }

                _errorMessage = "Vacuum sensor failed to deactivate within timeout period";
                return false;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"Sensor error during vacuum off operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Vacuum off operation was canceled");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during vacuum off operation: {ex.Message}");
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }
        #endregion

        #region Mapping Operations
        public async Task MappingOperation_UpToDown_HighSpeed(CancellationToken token, IMappingSettings settings)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                return;
            }

            // Validate settings early
            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null, cannot perform mapping.";
                Debug.WriteLine("Error: IMappingSettings object is null in MappingOperation_UpToDown_HighSpeed.");
                return;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return;
            }

            // ***** CHECK PROTRUSION SENSOR FIRST BEFORE ANY ELEVATOR MOVEMENT *****
            UpdateSensorStatus();
            if (_sensorStatus.StatusProtrusion != 1)
            {
                _errorMessage = "Wafers are not placed properly (Protrusion Sensor).";
                Debug.WriteLine("HIGH-SPEED: Protrusion sensor check failed before any elevator movement");
                return;
            }
            Debug.WriteLine("HIGH-SPEED: Protrusion sensor check passed before elevator movement.");

            // Default sensor type (0 = sensor A, 1 = sensor B)
            int sensorType = 0;

            // Get the sensor type if settings is a MappingTypeProfile
            if (settings is MappingTypeProfile mappingProfile)
            {
                m_status[18] = (char)((int)PodType.Type1 + mappingProfile.FOUPTypeIndex);
                Debug.WriteLine($"Using FOUP Type: {(PodType)m_status[18]} (index {mappingProfile.FOUPTypeIndex})");
                sensorType = mappingProfile.SensorType;
                Debug.WriteLine($"Using Sensor Type: {(sensorType == 0 ? "A (Input 14)" : "B (Input 15)")}");
            }
            else
            {
                Debug.WriteLine("Warning: Settings object is not a MappingTypeProfile - using default Sensor Type A (Input 14)");
            }

            // Calculate targets
            int initialDropMagnitude = (int)settings.MapStartPositionMm;
            int scanEndMagnitude = (int)settings.MapEndPositionMm;

            int initialDropTargetPulse = initialDropMagnitude < 0 ? initialDropMagnitude : -initialDropMagnitude;
            int scanEndTargetPulse = scanEndMagnitude < 0 ? scanEndMagnitude : -scanEndMagnitude;
            scanEndTargetPulse = Math.Min(scanEndTargetPulse, -1620);

            Debug.WriteLine($"HIGH-SPEED Mapping Settings: Initial Drop={initialDropTargetPulse}, Scan End={scanEndTargetPulse}, MmPerPulse={mmPerPulse}");

            // Pre-allocate much larger capacity for high-speed data collection
            _mappingData = new List<DataPoint>(10000);

            try
            {
                // **** SETUP PHASE - NOW ELEVATOR MOVEMENT AFTER PROTRUSION CHECK ****
                Debug.WriteLine("HIGH-SPEED: Moving elevator to top position...");
                bool elevatorUpSuccess = await Task.Run(() => ElevatorUp(token));
                if (!elevatorUpSuccess)
                {
                    _errorMessage = "Failed to home elevator to top position.";
                    return;
                }
                Debug.WriteLine("HIGH-SPEED: Elevator reached top position.");
                await Task.Delay(1000, token);

                Debug.WriteLine("HIGH-SPEED: Setting absolute position to 0...");
                CardStatus status = _credenAxisCard.SetAbsPosition(3, 0);
                if (status != CardStatus.Successful)
                {
                    _errorMessage = $"Failed to set absolute position to 0: {status}";
                    Debug.WriteLine(_errorMessage);
                    return;
                }
                Debug.WriteLine("HIGH-SPEED: Position successfully set to 0.");
                await Task.Delay(100, token);

                UpdateSensorStatus();
                if (_sensorStatus.StatusProtrusion != 1)
                {
                    _errorMessage = "Wafers are not placed properly (Protrusion Sensor).";
                    Debug.WriteLine("HIGH-SPEED: Protrusion sensor check failed before mapping");
                    return;
                }
                Debug.WriteLine("HIGH-SPEED: Protrusion sensor check passed.");

                // **** INITIAL DROP PHASE ****
                Debug.WriteLine("HIGH-SPEED: Starting initial elevator drop phase.");
                await Task.Run(async () =>
                {
                    int setupInitialPos = 0;
                    try
                    {
                        _credenAxisCard.GetAbsPosition(3, ref setupInitialPos);
                        Debug.WriteLine($"HIGH-SPEED: Position before initial drop: {setupInitialPos} pulses");

                        int targetPosition = initialDropTargetPulse;
                        Debug.WriteLine($"HIGH-SPEED: Executing initial drop to target position: {targetPosition} pulses");

                        if (setupInitialPos > targetPosition)
                        {
                            int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                            int initialDropDown1Bit = _outputList.ElevatorDown1 % 8;
                            int initialDropDown2Bit = _outputList.ElevatorDown2 % 8;

                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, true);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, true);

                            int currentPosition = setupInitialPos;
                            bool targetReached = false;
                            var dropStopwatch = Stopwatch.StartNew();

                            while (!targetReached && dropStopwatch.ElapsedMilliseconds < 5000)
                            {
                                token.ThrowIfCancellationRequested();
                                _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                                if (currentPosition <= targetPosition)
                                {
                                    targetReached = true;
                                    Debug.WriteLine($"HIGH-SPEED: Initial drop target reached: Current={currentPosition}, Target={targetPosition}");
                                }
                            }
                            dropStopwatch.Stop();

                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, false);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, false);

                            if (!targetReached)
                            {
                                Debug.WriteLine($"HIGH-SPEED: Warning: Initial drop to {targetPosition} timed out.");
                                throw new TimeoutException($"Failed to reach initial drop target {targetPosition} within timeout.");
                            }

                            await Task.Delay(250, token);
                            _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                            Debug.WriteLine($"HIGH-SPEED: Initial drop complete. Final Pos: {currentPosition} pulses ({currentPosition * mmPerPulse:F2}mm)");
                        }
                        else
                        {
                            Debug.WriteLine($"HIGH-SPEED: Already at or below target initial drop position ({targetPosition}). Current: {setupInitialPos}. Skipping drop.");
                        }

                        Debug.WriteLine("HIGH-SPEED: Extending mapping arm using MappingBackward method");
                        bool armExtended = await Task.Run(() => MappingBackward(token));

                        if (!armExtended)
                        {
                            Debug.WriteLine("HIGH-SPEED: WARNING: Mapping arm extension failed using MappingBackward method");
                            throw new Exception("Failed to extend mapping arm");
                        }

                        await Task.Delay(100, token);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"HIGH-SPEED: Error during initial setup phase: {ex.Message}");
                        await SafelyDisableAllOutputs();
                        throw;
                    }
                }, token);

                // **** ULTRA-HIGH-SPEED MAPPING SCAN ****
                Debug.WriteLine("HIGH-SPEED: Starting ULTRA-HIGH-SPEED mapping scan phase.");

                // Pre-allocate arrays for maximum speed
                var positions = new List<int>(10000);
                var sensorValues = new List<byte>(10000);
                var timestamps = new List<long>(10000);

                int mappingStartPos = 0;
                _credenAxisCard.GetAbsPosition(3, ref mappingStartPos);
                Debug.WriteLine($"HIGH-SPEED: Position before scan: {mappingStartPos} pulses");

                // Engage elevator down motors
                int motorPortId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                int scanElevatorDown1Bit = _outputList.ElevatorDown1 % 8;
                int scanElevatorDown2Bit = _outputList.ElevatorDown2 % 8;

                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, true);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, true);

                // CRITICAL HIGH-SPEED SECTION
                semReadPort.WaitOne();

                var mappingStopwatch = Stopwatch.StartNew();
                byte sensorReadByte = 0;
                int currentPos = mappingStartPos;
                int scanEndTarget = scanEndTargetPulse;

                // Pre-calculate sensor bit mask
                int bitPosition = sensorType == 0 ? 14 - 8 : 15 - 8;
                int sensorBitMask = 1 << bitPosition;

                Debug.WriteLine($"HIGH-SPEED: Starting ultra-fast data collection loop...");
                Debug.WriteLine($"HIGH-SPEED: Sensor bit mask: 0x{sensorBitMask:X2}, Target: {scanEndTarget}");

                int totalReads = 0;
                long lastLogTime = 0;

                // ULTRA-OPTIMIZED COLLECTION LOOP - MINIMIZE ALL OPERATIONS
                while (currentPos > scanEndTarget)
                {
                    token.ThrowIfCancellationRequested();

                    // 1. FASTEST POSSIBLE HARDWARE READS
                    _credenAxisCard.GetAbsPosition(3, ref currentPos);
                    _credenIOCard2.ReadPort(1, ref sensorReadByte);

                    // 2. STORE RAW DATA - NO CALCULATIONS
                    long currentTime = mappingStopwatch.ElapsedTicks; // Use Ticks for higher precision
                    positions.Add(currentPos);
                    sensorValues.Add(sensorReadByte);
                    timestamps.Add(currentTime);

                    totalReads++;

                    // Optional: Periodic logging every 1000 reads
                    if (totalReads % 1000 == 0)
                    {
                        long currentMs = mappingStopwatch.ElapsedMilliseconds;
                        if (currentMs - lastLogTime > 1000) // Log every second
                        {
                            Debug.WriteLine($"HIGH-SPEED: Collected {totalReads} points, Current pos: {currentPos}, Rate: ~{1000.0 / (currentMs - lastLogTime):F1} points/ms");
                            lastLogTime = currentMs;
                        }
                    }
                }

                // End critical section
                long totalScanTime = mappingStopwatch.ElapsedMilliseconds;
                double ticksPerMs = Stopwatch.Frequency / 1000.0;
                mappingStopwatch.Stop();
                semReadPort.Release();

                // Stop elevator motors
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, false);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, false);

                Debug.WriteLine($"HIGH-SPEED: Ultra-fast data collection complete!");
                Debug.WriteLine($"HIGH-SPEED: Total points collected: {totalReads}");
                Debug.WriteLine($"HIGH-SPEED: Total scan time: {totalScanTime}ms");
                Debug.WriteLine($"HIGH-SPEED: Average collection rate: {totalReads / (double)totalScanTime:F2} points/ms");
                Debug.WriteLine($"HIGH-SPEED: Average interval: {totalScanTime / (double)totalReads:F3}ms per point");

                // **** POST-PROCESS RAW DATA INTO DataPoint FORMAT ****
                Debug.WriteLine("HIGH-SPEED: Post-processing raw data into DataPoint format...");
                _mappingData.Clear();
                _mappingData.Capacity = totalReads;

                for (int i = 0; i < totalReads; i++)
                {
                    // Calculate sensor value from raw byte
                    bool selectedSensorActive = (sensorValues[i] & sensorBitMask) == 0;
                    int sensorValue = selectedSensorActive ? 1 : 0;

                    // Convert timestamp from ticks to milliseconds
                    long timeMs = (long)(timestamps[i] / ticksPerMs);

                    _mappingData.Add(new DataPoint
                    {
                        TimeMs = timeMs,
                        Position = positions[i] * mmPerPulse,
                        SensorValue = sensorValue,
                        Velocity = 0 // Skip velocity calculation for speed
                    });
                }

                Debug.WriteLine($"HIGH-SPEED: Post-processing complete. Final data count: {_mappingData.Count}");

                if (_mappingData.Count > 0)
                {
                    m_status[17] = (char)MappingStatus.Completed;
                    Debug.WriteLine($"HIGH-SPEED: Mapping data collection successful: {_mappingData.Count} points stored.");

                    // Calculate and display improved statistics
                    var activations = _mappingData.Count(d => d.SensorValue == 1);
                    Debug.WriteLine($"HIGH-SPEED: Sensor activations: {activations} ({activations * 100.0 / _mappingData.Count:F2}%)");

                    if (totalScanTime > 0)
                    {
                        Debug.WriteLine($"HIGH-SPEED: Data density: {_mappingData.Count / (double)totalScanTime:F2} points/ms");
                        Debug.WriteLine($"HIGH-SPEED: Time resolution: {totalScanTime / (double)_mappingData.Count:F3}ms/point");
                    }
                }
                else
                {
                    Debug.WriteLine("HIGH-SPEED: No mapping data was collected during the scan.");
                    m_status[17] = (char)MappingStatus.Inexecution;
                }

                // **** CLEANUP PHASE - SAME AS ORIGINAL ****
                Debug.WriteLine("HIGH-SPEED: Retracting mapping arm...");
                bool mappingForwardSuccess = await Task.Run(() => MappingForward(token));

                if (!mappingForwardSuccess)
                    Debug.WriteLine("HIGH-SPEED: Warning: Mapping arm retraction may not have completed properly.");
                else
                    Debug.WriteLine("HIGH-SPEED: Mapping arm successfully retracted.");

                Debug.WriteLine("HIGH-SPEED: Moving elevator to lowest position...");
                bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));

                if (!elevatorDownSuccess)
                    Debug.WriteLine("HIGH-SPEED: Warning: Full elevator descent may not have completed properly.");
                else
                    Debug.WriteLine("HIGH-SPEED: Elevator successfully reached lowest position.");

                // **** EXPORT DATA ****
                if (_mappingData.Count > 0)
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    //string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data_HighSpeed");
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data_HighSpeed_RepeatabilityTest");

                    Debug.WriteLine($"HIGH-SPEED: Exporting {_mappingData.Count} data points to: {savePath}");
                    bool exportSuccess = ExportMappingDataRaw(savePath);

                    if (exportSuccess)
                        Debug.WriteLine("HIGH-SPEED: Mapping data exported successfully.");
                    else
                        Debug.WriteLine($"HIGH-SPEED: Export failed: {_errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _errorMessage = "HIGH-SPEED: Mapping operation was canceled.";
                Debug.WriteLine(_errorMessage);
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            catch (Exception ex)
            {
                _errorMessage = $"HIGH-SPEED: Critical error in mapping sequence: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            finally
            {
                try { semReadPort?.Release(); } catch (SemaphoreFullException) { /* Already released */ }
                Debug.WriteLine("HIGH-SPEED: MappingOperation_UpToDown_HighSpeed finished.");
            }
        }
        public async Task MappingOperation_UpToDown(CancellationToken token, IMappingSettings settings)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                return;
            }

            // Validate settings early
            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null, cannot perform mapping.";
                Debug.WriteLine("Error: IMappingSettings object is null in MappingOperation_UpToDown.");
                return;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return;
            }

            // Default sensor type (0 = sensor A, 1 = sensor B)
            int sensorType = 0;

            // Get the sensor type if settings is a MappingTypeProfile
            if (settings is MappingTypeProfile mappingProfile)
            {
                // Set the FOUP type based on the profile
                m_status[18] = (char)((int)PodType.Type1 + mappingProfile.FOUPTypeIndex);
                Debug.WriteLine($"Using FOUP Type: {(PodType)m_status[18]} (index {mappingProfile.FOUPTypeIndex})");

                // Get the sensor type from the profile
                sensorType = mappingProfile.SensorType;
                Debug.WriteLine($"Using Sensor Type: {(sensorType == 0 ? "A (Input 14)" : "B (Input 15)")}");
            }
            else
            {
                Debug.WriteLine("Warning: Settings object is not a MappingTypeProfile - using default Sensor Type A (Input 14)");
            }

            // Assume settings values are pulse magnitudes; convert to targets (typically negative)
            int initialDropMagnitude = (int)settings.MapStartPositionMm;
            int scanEndMagnitude = (int)settings.MapEndPositionMm;

            Debug.WriteLine($"MapStartPositionMm: {settings.MapStartPositionMm}");
            Debug.WriteLine($"MapEndPositionMm: {settings.MapEndPositionMm}");
            Debug.WriteLine($"Mapping Amplifier Type: {settings.SensorType}");

            // Usually, targets are negative for downward movement from 0
            int initialDropTargetPulse = initialDropMagnitude < 0
                ? initialDropMagnitude  // already negative, use as is
                : -initialDropMagnitude; // positive, convert to negative

            int scanEndTargetPulse = scanEndMagnitude < 0
                ? scanEndMagnitude  // already negative, use as is
                : -scanEndMagnitude; // positive, convert to negative

            // Ensure the scan end target is at least -1620 for sufficient depth
            scanEndTargetPulse = Math.Min(scanEndTargetPulse, -1620); // remove if no need, use param 

            Debug.WriteLine($"Mapping Settings (Pulses): Initial Drop Target={initialDropTargetPulse}, Scan End Target={scanEndTargetPulse}, MmPerPulse={mmPerPulse}");

            // Pre-allocate memory for mapping data - higher initial capacity for more data points
            _mappingData = new List<DataPoint>(4000); //array

            try
            {
                // **** START HOMING SEQUENCE ****
                Debug.WriteLine("Moving elevator to top position...");
                bool elevatorUpSuccess = await Task.Run(() => ElevatorUp(token));
                if (!elevatorUpSuccess)
                {
                    _errorMessage = "Failed to home elevator to top position.";
                    return;
                }
                Debug.WriteLine("Elevator reached top position.");
                await Task.Delay(1000, token); // Stabilization delay

                // **** SET POSITION TO ZERO ****
                Debug.WriteLine("Setting absolute position to 0...");
                CardStatus status = _credenAxisCard.SetAbsPosition(3, 0);
                if (status != CardStatus.Successful)
                {
                    _errorMessage = $"Failed to set absolute position to 0: {status}";
                    Debug.WriteLine(_errorMessage);
                    return;
                }
                Debug.WriteLine("Position successfully set to 0.");
                await Task.Delay(100, token); // Allow time for setting to take effect

                // **** VERIFY SENSORS ****
                UpdateSensorStatus();
                if (_sensorStatus.StatusProtrusion != 1)
                {
                    _errorMessage = "Wafers are not placed properly (Protrusion Sensor).";
                    Debug.WriteLine("Protrusion sensor check failed before mapping");
                    return;
                }
                Debug.WriteLine("Protrusion sensor check passed.");

                // **** PHASE 1: INITIAL DROP **** (Using Control Operations)
                Debug.WriteLine("Starting initial elevator drop phase.");
                await Task.Run(async () =>
                {
                    int setupInitialPos = 0;
                    try
                    {
                        _credenAxisCard.GetAbsPosition(3, ref setupInitialPos);
                        Debug.WriteLine($"Position before initial drop: {setupInitialPos} pulses");

                        int targetPosition = initialDropTargetPulse;
                        Debug.WriteLine($"Executing initial drop to target position: {targetPosition} pulses");

                        if (setupInitialPos > targetPosition)
                        {
                            // Use defined control operations instead of hard-coded values
                            // Use output list to determine port ID for both elevator down outputs
                            int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                            int initialDropDown1Bit = _outputList.ElevatorDown1 % 8;
                            int initialDropDown2Bit = _outputList.ElevatorDown2 % 8;

                            // Turn on elevator down motors using WriteBit method
                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, true);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, true);

                            int currentPosition = setupInitialPos;
                            bool targetReached = false;
                            var dropStopwatch = Stopwatch.StartNew();

                            while (!targetReached && dropStopwatch.ElapsedMilliseconds < 5000)
                            {
                                token.ThrowIfCancellationRequested();
                                _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                                if (currentPosition <= targetPosition)
                                {
                                    targetReached = true;
                                    Debug.WriteLine($"Initial drop target reached: Current={currentPosition}, Target={targetPosition}");
                                }
                            }
                            dropStopwatch.Stop();

                            // Turn off elevator down motors using WriteBit method
                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, false);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, false);

                            if (!targetReached)
                            {
                                Debug.WriteLine($"Warning: Initial drop to {targetPosition} timed out.");
                                throw new TimeoutException($"Failed to reach initial drop target {targetPosition} within timeout.");
                            }

                            await Task.Delay(250, token);

                            _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                            Debug.WriteLine($"Initial drop complete. Final Pos: {currentPosition} pulses ({currentPosition * mmPerPulse:F2}mm)");
                        }
                        else
                        {
                            Debug.WriteLine($"Already at or below target initial drop position ({targetPosition}). Current: {setupInitialPos}. Skipping drop.");
                        }

                        // **** EXTEND MAPPING ARM **** (Using MappingBackward method)
                        Debug.WriteLine("Extending mapping arm using MappingBackward method");
                        bool armExtended = await Task.Run(() => MappingBackward(token));

                        if (!armExtended)
                        {
                            Debug.WriteLine("WARNING: Mapping arm extension failed using MappingBackward method");
                            throw new Exception("Failed to extend mapping arm");
                        }

                        await Task.Delay(100, token);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during initial setup phase: {ex.Message}");
                        await SafelyDisableAllOutputs();
                        throw;
                    }
                }, token);

                // **** PHASE 2: ULTRA-OPTIMIZED HIGH-SPEED MAPPING SCAN ****
                Debug.WriteLine("Starting high-speed mapping scan phase with optimized data collection.");

                // Direct collection list - no intermediary processing
                var rawData = new List<DataPoint>(4000);

                int mappingStartPos = 0;
                _credenAxisCard.GetAbsPosition(3, ref mappingStartPos);
                Debug.WriteLine($"Position before scan: {mappingStartPos} pulses");

                // Engage elevator down motors using defined control operations
                int motorPortId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                // Changed variable names to avoid conflict with the ones in the inner Task
                int scanElevatorDown1Bit = _outputList.ElevatorDown1 % 8;
                int scanElevatorDown2Bit = _outputList.ElevatorDown2 % 8;

                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, true);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, true);

                // CRITICAL SECTION - Optimize for speed
                semReadPort.WaitOne();

                var mappingStopwatch = Stopwatch.StartNew();
                byte sensorReadByte = 0;
                int currentPos = mappingStartPos;
                int scanEndTarget = scanEndTargetPulse;

                // Determine which bit to check based on sensor type
                int bitPosition = sensorType == 0 ? 14 - 8 : 15 - 8; // Adjust for port 1 (bits 8-15)
                int sensorBitMask = 1 << bitPosition;

                // Debug sensor configuration information
                byte initialSensorByte = 0;
                _credenIOCard2.ReadPort(1, ref initialSensorByte);
                Debug.WriteLine($"===== MAPPING SENSOR DIAGNOSTICS =====");
                Debug.WriteLine($"Sensor Type Selected: {sensorType} ({(sensorType == 0 ? "Input 14" : "Input 15")})");
                Debug.WriteLine($"Bit Position: {bitPosition + 8} (Port 1, bit {bitPosition})");
                Debug.WriteLine($"Sensor Bit Mask: 0x{sensorBitMask:X2}");
                Debug.WriteLine($"Initial Sensor Byte Value: 0x{initialSensorByte:X2} (Binary: {Convert.ToString(initialSensorByte, 2).PadLeft(8, '0')})");
                Debug.WriteLine($"Input 14 Status: {((initialSensorByte & 0x40) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Input 15 Status: {((initialSensorByte & 0x80) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Selected Sensor Status: {((initialSensorByte & sensorBitMask) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Using configured sensor mask: {((initialSensorByte & sensorBitMask) == 0 ? 1 : 0)}");
                Debug.WriteLine($"Using ONLY selected sensor (no OR logic)");
                Debug.WriteLine($"=======================================");

                // Initialize counters for sensor activity
                int totalReads = 0;
                int sensor14Activations = 0;
                int sensor15Activations = 0;
                int selectedSensorActivations = 0;
                int firstActivationPosition = 0;
                bool firstActivationRecorded = false;

                // ULTRA-FAST LOOP: Minimize operations, avoid any calculations
                while (currentPos > scanEndTarget)
                {
                    // 1. Read position directly - minimal overhead
                    _credenAxisCard.GetAbsPosition(3, ref currentPos);

                    // 2. Read sensor value directly - minimal overhead
                    _credenIOCard2.ReadPort(1, ref sensorReadByte);

                    // Test both individual sensors and the selected configuration (for diagnostics only)
                    bool sensor14Active = (sensorReadByte & 0x40) == 0;
                    bool sensor15Active = (sensorReadByte & 0x80) == 0;
                    bool selectedSensorActive = (sensorReadByte & sensorBitMask) == 0;

                    // Increment counters for diagnostics
                    totalReads++;
                    if (sensor14Active) sensor14Activations++;
                    if (sensor15Active) sensor15Activations++;
                    if (selectedSensorActive) selectedSensorActivations++;

                    // Record first activation position
                    if (selectedSensorActive && !firstActivationRecorded)
                    {
                        firstActivationPosition = currentPos;
                        firstActivationRecorded = true;
                    }

                    // Use ONLY the selected sensor bit (input 14 or 15 on card 2)
                    int sensorValue = selectedSensorActive ? 1 : 0;

                    // 3. Store minimal raw data - no calculations
                    rawData.Add(new DataPoint
                    {
                        TimeMs = mappingStopwatch.ElapsedMilliseconds,
                        Position = currentPos * mmPerPulse, // Only essential conversion
                        SensorValue = sensorValue,
                        Velocity = 0 // Skip velocity calculation entirely
                    });
                }

                // End critical section
                long scanTime = mappingStopwatch.ElapsedMilliseconds;
                mappingStopwatch.Stop();
                semReadPort.Release();

                // Stop elevator motors immediately using WriteBit
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, false);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, false);
                Debug.WriteLine("Elevator motors stopped after scan loop.");

                // Print sensor activity statistics
                Debug.WriteLine($"===== MAPPING SENSOR ACTIVITY STATISTICS =====");
                Debug.WriteLine($"Total reads: {totalReads}");
                Debug.WriteLine($"Input 14 activations: {sensor14Activations} ({(totalReads > 0 ? sensor14Activations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Input 15 activations: {sensor15Activations} ({(totalReads > 0 ? sensor15Activations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Selected sensor activations: {selectedSensorActivations} ({(totalReads > 0 ? selectedSensorActivations * 100.0 / totalReads : 0):F2}%)");
                if (firstActivationRecorded)
                {
                    Debug.WriteLine($"First sensor activation at position: {firstActivationPosition} pulses ({firstActivationPosition * mmPerPulse:F3}mm)");
                }
                else
                {
                    Debug.WriteLine("WARNING: No sensor activations recorded during the entire scan!");
                }
                Debug.WriteLine($"===========================================");

                // Copy collected data directly to main collection - no processing
                _mappingData = rawData;

                if (_mappingData.Count > 0)
                {
                    m_status[17] = (char)MappingStatus.Completed;
                    Debug.WriteLine($"Mapping data collection successful: {_mappingData.Count} points stored.");
                }
                else
                {
                    Debug.WriteLine("No mapping data was collected during the scan.");
                    m_status[17] = (char)MappingStatus.Inexecution;
                }

                // Final check of the sensor state
                byte finalSensorByte = 0;
                _credenIOCard2.ReadPort(1, ref finalSensorByte);
                Debug.WriteLine($"Final sensor byte: 0x{finalSensorByte:X2} (Binary: {Convert.ToString(finalSensorByte, 2).PadLeft(8, '0')})");
                Debug.WriteLine($"Final Input 14 status: {((finalSensorByte & 0x40) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Final Input 15 status: {((finalSensorByte & 0x80) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");

                // **** PHASE 4: CLEANUP - RETRACT ARM **** (Using MappingForward method)
                Debug.WriteLine("Retracting mapping arm using MappingForward method...");
                bool mappingForwardSuccess = await Task.Run(() => MappingForward(token));

                if (!mappingForwardSuccess)
                    Debug.WriteLine("Warning: Mapping arm retraction may not have completed properly.");
                else
                    Debug.WriteLine("Mapping arm successfully retracted.");

                // **** PHASE 5: FINAL ELEVATOR DESCENT ****
                Debug.WriteLine("Mapping arm retracted. Moving elevator to lowest position...");
                bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));

                if (!elevatorDownSuccess)
                    Debug.WriteLine("Warning: Full elevator descent may not have completed properly.");
                else
                    Debug.WriteLine("Elevator successfully reached lowest position.");

                // **** PHASE 6: EXPORT DATA ****
                if (_mappingData.Count > 0)
                {
                    // Export data to CSV using streamlined format
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data");

                    Debug.WriteLine($"Exporting {_mappingData.Count} data points to: {savePath}");
                    bool exportSuccess = ExportMappingDataRaw(savePath);

                    if (exportSuccess)
                        Debug.WriteLine("Mapping data exported successfully.");
                    else
                        Debug.WriteLine($"Export failed: {_errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _errorMessage = "Mapping operation was canceled.";
                Debug.WriteLine(_errorMessage);
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Critical error in mapping sequence: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            finally
            {
                try { semReadPort?.Release(); } catch (SemaphoreFullException) { /* Already released */ }
                Debug.WriteLine("MappingOperation_UpToDown finished.");
            }
        }
        public async Task MappingOperation_DownToUp(CancellationToken token, IMappingSettings settings)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine("Error: Not all cards are connected in MappingOperation_DownToUp");
                return;
            }

            // Validate settings early
            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null, cannot perform mapping.";
                Debug.WriteLine("Error: IMappingSettings object is null in MappingOperation_DownToUp.");
                return;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return;
            }

            // Default sensor type (0 = sensor A, 1 = sensor B)
            int sensorType = 0;

            // Get the sensor type if settings is a MappingTypeProfile
            if (settings is MappingTypeProfile mappingProfile)
            {
                // Set the FOUP type based on the profile
                m_status[18] = (char)((int)PodType.Type1 + mappingProfile.FOUPTypeIndex);
                Debug.WriteLine($"Using FOUP Type: {(PodType)m_status[18]} (index {mappingProfile.FOUPTypeIndex})");

                // Get the sensor type from the profile
                sensorType = mappingProfile.SensorType;
                Debug.WriteLine($"Using Sensor Type: {(sensorType == 0 ? "A (Input 14)" : "B (Input 15)")}");
            }
            else
            {
                Debug.WriteLine("Warning: Settings object is not a MappingTypeProfile - using default Sensor Type A (Input 14)");
            }

            // Convert positioning values (typically negative for down positions from 0)
            int scanStartMagnitude = (int)settings.MapEndPositionMm - 120;    // Note: Flipping start/end since we're going bottom-up
            int scanEndMagnitude = (int)settings.MapStartPositionMm - 280;    // Note: Flipping start/end since we're going bottom-up

            Debug.WriteLine($"DOWN-TO-UP Mapping - MapEndPositionMm: {settings.MapEndPositionMm} (Starting point)");
            Debug.WriteLine($"DOWN-TO-UP Mapping - MapStartPositionMm: {settings.MapStartPositionMm} (Ending point)");
            Debug.WriteLine($"Mapping Amplifier Type: {settings.SensorType}");

            // Convert positions to pulses (typically negative)
            int scanStartTargetPulse = scanStartMagnitude < 0
                ? scanStartMagnitude  // already negative, use as is
                : -scanStartMagnitude; // positive, convert to negative

            int scanEndTargetPulse = scanEndMagnitude < 0
                ? scanEndMagnitude    // already negative, use as is
                : -scanEndMagnitude;  // positive, convert to negative

            // Sanity check - ensure scanStartTargetPulse is more negative (deeper) than scanEndTargetPulse
            if (scanStartTargetPulse > scanEndTargetPulse)
            {
                _errorMessage = "Invalid mapping positions: End position must be closer to home (0) than start position for down-to-up mapping";
                Debug.WriteLine(_errorMessage);
                return;
            }

            Debug.WriteLine($"Mapping Settings (Pulses): Start Target={scanStartTargetPulse}, End Target={scanEndTargetPulse}, MmPerPulse={mmPerPulse}");

            // Pre-allocate memory for mapping data
            _mappingData = new List<DataPoint>(4000);

            try
            {
                // **** PHASE 1: ENSURE ELEVATOR IS AT BOTTOM POSITION ****
                Debug.WriteLine("Making sure the elevator is at the bottom position...");

                UpdateSensorStatus();
                if (_sensorStatus.StatusElevatorDown != 1)
                {
                    Debug.WriteLine("Elevator not at bottom, moving down...");
                    bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));
                    if (!elevatorDownSuccess)
                    {
                        _errorMessage = "Failed to move elevator to bottom position.";
                        Debug.WriteLine(_errorMessage);
                        return;
                    }
                    Debug.WriteLine("Elevator reached bottom position.");
                }
                else
                {
                    Debug.WriteLine("Elevator already at bottom position.");
                }

                //await Task.Delay(1000, token); // Stabilization delay

                // **** PHASE 2: GET POSITION READING ****
                int startPosition = 0;
                Debug.WriteLine("Reading current position...");
                CardStatus status = _credenAxisCard.GetAbsPosition(3, ref startPosition);
                if (status != CardStatus.Successful)
                {
                    _errorMessage = $"Failed to read absolute position: {status}";
                    Debug.WriteLine(_errorMessage);
                    return;
                }
                Debug.WriteLine($"Current position: {startPosition} pulses");

                // **** PHASE 3: MOVE ELEVATOR TO SCANNING START POSITION ****
                Debug.WriteLine($"Moving elevator to scanning start position (pulse={scanStartTargetPulse})...");

                // Determine which direction to move (most likely UP from bottom position)
                if (startPosition < scanStartTargetPulse)
                {
                    Debug.WriteLine("Need to move elevator UP to reach start position");

                    // Use defined control operations for elevator up
                    int portId = _outputList.ElevatorUp1 < 8 ? 2 : 3;
                    int elevatorUp1Bit = _outputList.ElevatorUp1 % 8;
                    int elevatorUp2Bit = _outputList.ElevatorUp2 % 8;

                    // Turn on elevator up motors
                    WriteBit(_credenIOCard1, portId, elevatorUp1Bit, true);
                    WriteBit(_credenIOCard1, portId, elevatorUp2Bit, true);

                    int currentPosition = startPosition;
                    bool targetReached = false;
                    var moveStopwatch = Stopwatch.StartNew();

                    while (!targetReached && !token.IsCancellationRequested && moveStopwatch.ElapsedMilliseconds < 10000)
                    {
                        _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                        if (currentPosition >= scanStartTargetPulse)
                        {
                            targetReached = true;
                            Debug.WriteLine($"Start position reached: {currentPosition} pulses");
                        }
                        //await Task.Delay(10, token);
                    }
                    moveStopwatch.Stop();

                    // Turn off elevator up motors
                    WriteBit(_credenIOCard1, portId, elevatorUp1Bit, false);
                    WriteBit(_credenIOCard1, portId, elevatorUp2Bit, false);

                    if (!targetReached)
                    {
                        Debug.WriteLine("Failed to reach start position within timeout");
                        throw new TimeoutException("Failed to reach scanning start position");
                    }
                }
                else if (startPosition > scanStartTargetPulse)
                {
                    Debug.WriteLine("Need to move elevator DOWN to reach start position");

                    // Use defined control operations for elevator down
                    int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                    int elevatorDown1Bit = _outputList.ElevatorDown1 % 8;
                    int elevatorDown2Bit = _outputList.ElevatorDown2 % 8;

                    // Turn on elevator down motors
                    WriteBit(_credenIOCard1, portId, elevatorDown1Bit, true);
                    WriteBit(_credenIOCard1, portId, elevatorDown2Bit, true);

                    int currentPosition = startPosition;
                    bool targetReached = false;
                    var moveStopwatch = Stopwatch.StartNew();

                    while (!targetReached && !token.IsCancellationRequested && moveStopwatch.ElapsedMilliseconds < 10000)
                    {
                        _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                        if (currentPosition <= scanStartTargetPulse)
                        {
                            targetReached = true;
                            Debug.WriteLine($"Start position reached: {currentPosition} pulses");
                        }
                        //await Task.Delay(10, token);
                    }
                    moveStopwatch.Stop();

                    // Turn off elevator down motors
                    WriteBit(_credenIOCard1, portId, elevatorDown1Bit, false);
                    WriteBit(_credenIOCard1, portId, elevatorDown2Bit, false);

                    if (!targetReached)
                    {
                        Debug.WriteLine("Failed to reach start position within timeout");
                        throw new TimeoutException("Failed to reach scanning start position");
                    }
                }

                //await Task.Delay(500, token); // Stabilization delay

                // **** PHASE 4: EXTEND MAPPING ARM ****
                Debug.WriteLine("Extending mapping arm...");
                bool armExtended = await Task.Run(() => MappingBackward(token));

                if (!armExtended)
                {
                    Debug.WriteLine("Failed to extend mapping arm");
                    throw new Exception("Failed to extend mapping arm");
                }

                Debug.WriteLine("Mapping arm extended successfully.");
                //await Task.Delay(500, token); // Stabilization delay

                // **** PHASE 5: PERFORM MAPPING SCAN UP ****
                Debug.WriteLine("Starting mapping data collection while moving UP...");

                // Get the current position for scan start reference
                int mappingStartPos = 0;
                _credenAxisCard.GetAbsPosition(3, ref mappingStartPos);
                Debug.WriteLine($"Scan starting position: {mappingStartPos} pulses");

                // Prepare the elevator up motors for scanning
                int motorPortId = _outputList.ElevatorUp1 < 8 ? 2 : 3;
                int scanElevatorUp1Bit = _outputList.ElevatorUp1 % 8;
                int scanElevatorUp2Bit = _outputList.ElevatorUp2 % 8;

                // Direct collection list - no intermediary processing
                var rawData = new List<DataPoint>(4000);

                // Determine which bit to check based on sensor type
                int bitPosition = sensorType == 0 ? 14 - 8 : 15 - 8; // Adjust for port 1 (bits 8-15)
                int sensorBitMask = 1 << bitPosition;

                // Start scanning motion
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, true);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, true);

                // CRITICAL SECTION - Optimize for speed
                semReadPort.WaitOne();

                var mappingStopwatch = Stopwatch.StartNew();
                byte sensorReadByte = 0;
                int currentPos = mappingStartPos;

                // Debug sensor configuration information
                byte initialSensorByte = 0;
                _credenIOCard2.ReadPort(1, ref initialSensorByte);
                Debug.WriteLine($"===== UP MAPPING SENSOR DIAGNOSTICS =====");
                Debug.WriteLine($"Sensor Type Selected: {sensorType} ({(sensorType == 0 ? "Input 14" : "Input 15")})");
                Debug.WriteLine($"Bit Position: {bitPosition + 8} (Port 1, bit {bitPosition})");
                Debug.WriteLine($"Sensor Bit Mask: 0x{sensorBitMask:X2}");
                Debug.WriteLine($"Initial Sensor Byte Value: 0x{initialSensorByte:X2} (Binary: {Convert.ToString(initialSensorByte, 2).PadLeft(8, '0')})");
                Debug.WriteLine($"Input 14 Status: {((initialSensorByte & 0x40) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Input 15 Status: {((initialSensorByte & 0x80) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Selected Sensor Status: {((initialSensorByte & sensorBitMask) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"=======================================");

                // Initialize counters for sensor activity statistics
                int totalReads = 0;
                int sensor14Activations = 0;
                int sensor15Activations = 0;
                int selectedSensorActivations = 0;

                // ULTRA-FAST LOOP: Minimize operations, avoid any calculations
                // Note: In UpToDown, we go while (currentPos > scanEndTarget)
                // In DownToUp, we go while (currentPos < scanEndTarget) because we're moving upward (less negative)
                while (currentPos < scanEndTargetPulse)
                {
                    // 1. Read position directly
                    _credenAxisCard.GetAbsPosition(3, ref currentPos);

                    // 2. Read sensor value directly
                    _credenIOCard2.ReadPort(1, ref sensorReadByte);

                    // Test both sensors and the selected one
                    bool sensor14Active = (sensorReadByte & 0x40) == 0;
                    bool sensor15Active = (sensorReadByte & 0x80) == 0;
                    bool selectedSensorActive = (sensorReadByte & sensorBitMask) == 0;

                    // Update statistics
                    totalReads++;
                    if (sensor14Active) sensor14Activations++;
                    if (sensor15Active) sensor15Activations++;
                    if (selectedSensorActive) selectedSensorActivations++;

                    // 3. Store data
                    rawData.Add(new DataPoint
                    {
                        TimeMs = mappingStopwatch.ElapsedMilliseconds,
                        Position = currentPos * mmPerPulse, // Only essential conversion
                        SensorValue = selectedSensorActive ? 1 : 0,
                        Velocity = 0 // Skip velocity calculation entirely
                    });
                }

                // End critical section
                long scanTime = mappingStopwatch.ElapsedMilliseconds;
                mappingStopwatch.Stop();
                semReadPort.Release();

                // Stop elevator motors immediately
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, false);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, false);
                Debug.WriteLine("Elevator motors stopped after scan loop.");

                // Copy collected data directly to main collection - no processing
                _mappingData = rawData;

                // Print sensor activity statistics
                Debug.WriteLine($"===== MAPPING SENSOR ACTIVITY STATISTICS =====");
                Debug.WriteLine($"Total reads: {totalReads}");
                Debug.WriteLine($"Input 14 activations: {sensor14Activations} ({(totalReads > 0 ? sensor14Activations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Input 15 activations: {sensor15Activations} ({(totalReads > 0 ? sensor15Activations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Selected sensor activations: {selectedSensorActivations} ({(totalReads > 0 ? selectedSensorActivations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Scan time: {scanTime}ms");
                Debug.WriteLine($"===========================================");

                if (_mappingData.Count > 0)
                {
                    m_status[17] = (char)MappingStatus.Completed;
                    Debug.WriteLine($"Mapping data collection successful: {_mappingData.Count} points stored.");
                }
                else
                {
                    Debug.WriteLine("No mapping data was collected during the scan.");
                    m_status[17] = (char)MappingStatus.Inexecution;
                }

                // **** PHASE 6: CLEANUP - RETRACT ARM ****
                Debug.WriteLine("Retracting mapping arm...");
                bool mappingForwardSuccess = await Task.Run(() => MappingForward(token));

                if (!mappingForwardSuccess)
                    Debug.WriteLine("Warning: Mapping arm retraction may not have completed properly.");
                else
                    Debug.WriteLine("Mapping arm successfully retracted.");

                // **** PHASE 7: FINAL ELEVATOR ASCENT TO HOME (0) ****
                Debug.WriteLine("Moving elevator to home position (0)...");

                // Since we may not be at the top yet, need to continue moving up
                if (currentPos < 0)
                {
                    Debug.WriteLine($"Current position {currentPos} pulses, moving to home (0)...");

                    // Turn on elevator up motors again
                    WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, true);
                    WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, true);

                    UpdateSensorStatus();
                    while (_sensorStatus.StatusElevatorUp == 0 && !token.IsCancellationRequested)
                    {
                        //await Task.Delay(50, token);
                        UpdateSensorStatus();
                    }

                    // Turn off motors once top is reached
                    WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, false);
                    WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, false);

                    if (_sensorStatus.StatusElevatorUp == 1)
                        Debug.WriteLine("Elevator reached home position successfully.");
                    else
                        Debug.WriteLine("Warning: May not have reached home position - sensor not triggered.");
                }
                else
                {
                    Debug.WriteLine("Elevator already at or above home position.");
                }

                // **** PHASE 8: EXPORT DATA ****
                if (_mappingData.Count > 0)
                {
                    // Export data to CSV using streamlined format
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data");

                    Debug.WriteLine($"Exporting {_mappingData.Count} data points to: {savePath}");
                    bool exportSuccess = ExportMappingDataRaw(savePath);

                    if (exportSuccess)
                        Debug.WriteLine("Mapping data exported successfully.");
                    else
                        Debug.WriteLine($"Export failed: {_errorMessage}");
                }

                Debug.WriteLine("MappingOperation_DownToUp completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _errorMessage = "Mapping operation was canceled.";
                Debug.WriteLine(_errorMessage);
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Critical error in mapping sequence: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
            }
            finally
            {
                try { semReadPort?.Release(); } catch (SemaphoreFullException) { /* Already released */ }
                Debug.WriteLine("MappingOperation_DownToUp finished.");
            }
        }
        public async Task<FOUPCtrl.WaferMap.MappingAnalysisResult> MappingOperation_UpToDown_WithAnalysis(CancellationToken token, IMappingSettings settings)
        {
            // Initialize result with error status in case of early return
            var errorResult = new FOUPCtrl.WaferMap.MappingAnalysisResult(25);
            for (int i = 0; i < 25; i++)
            {
                errorResult.WaferStatus[i] = 99; // Error status
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine("Error: Not all cards are connected in MappingOperation_UpToDown_WithAnalysis");
                return errorResult;
            }

            // Validate settings early
            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null, cannot perform mapping.";
                Debug.WriteLine("Error: IMappingSettings object is null in MappingOperation_UpToDown_WithAnalysis.");
                return errorResult;
            }

            // Validate mm per pulse setting
            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return errorResult;
            }

            bool acquired = false;

            // Get the sensor type
            int sensorType = settings.SensorType;

            // Cast to get access to additional properties if settings is dynamic
            dynamic settingsObj = settings;
            MappingTable mappingTable = null;
            int expectedSlots = 25; // Will be overridden by settings

            try
            {
                // Get the current mapping type (1-5)
                int activeType = settingsObj.ActiveMappingType;
                Debug.WriteLine($"Active mapping type: {activeType}");

                // Set FOUP type in status
                if (settings is MappingTypeProfile mappingProfile)
                {
                    m_status[18] = (char)((int)PodType.Type1 + mappingProfile.FOUPTypeIndex);
                    Debug.WriteLine($"Using FOUP Type: {(PodType)m_status[18]} (index {mappingProfile.FOUPTypeIndex})");
                }

                // Get the mapping table for the active type
                mappingTable = settingsObj.GetMappingTableByNumber(activeType);
                if (mappingTable == null)
                {
                    _errorMessage = $"Could not load mapping table for type {activeType}";
                    Debug.WriteLine(_errorMessage);
                    return errorResult;
                }

                // Load all required parameters from the mapping table
                expectedSlots = mappingTable.SlotCount;

                // *** IMPORTANT: Ensure SlotPitchMm is always negative for downward mapping ***
                double slotPitchMm = mappingTable.SlotPitchMm;
                // Make slot pitch negative if it's positive (for downward mapping)
                if (slotPitchMm > 0)
                {
                    slotPitchMm = -slotPitchMm;
                    Debug.WriteLine($"Automatically converted SlotPitchMm from positive to negative: {slotPitchMm}mm");
                }

                double firstWaferPosMm = mappingTable.FirstSlotPositionMm;
                double waferThicknessMm = mappingTable.WaferThicknessMm;
                double thicknessToleranceMm = mappingTable.ThicknessRangeMm;
                double positionToleranceMm = mappingTable.PositionRangeMm;
                string typeName = mappingTable.Name;

                Debug.WriteLine($"Loaded parameters from mapping table {activeType}:");
                Debug.WriteLine($"- Slot count: {expectedSlots}");
                Debug.WriteLine($"- Slot pitch: {slotPitchMm}mm (negative for downward mapping)");
                Debug.WriteLine($"- First wafer position: {firstWaferPosMm}mm");
                Debug.WriteLine($"- Wafer thickness: {waferThicknessMm}mm");
                Debug.WriteLine($"- Thickness tolerance: {thicknessToleranceMm}mm");
                Debug.WriteLine($"- Position tolerance: {positionToleranceMm}mm");

                // Ensure all parameters are valid (non-zero)
                if (expectedSlots <= 0 || Math.Abs(slotPitchMm) <= 0 || waferThicknessMm <= 0 ||
                    thicknessToleranceMm <= 0 || positionToleranceMm <= 0)
                {
                    _errorMessage = $"One or more required mapping parameters are invalid (zero or negative)";
                    Debug.WriteLine(_errorMessage);
                    Debug.WriteLine("Check the .ini file configuration for the selected mapping type");
                    return errorResult;
                }

                // Convert mapping positions to pulses
                int initialDropMagnitude = (int)settings.MapStartPositionMm;
                int scanEndMagnitude = (int)settings.MapEndPositionMm;

                Debug.WriteLine($"MapStartPositionMm: {settings.MapStartPositionMm}");
                Debug.WriteLine($"MapEndPositionMm: {settings.MapEndPositionMm}");

                // Convert to target pulses (usually negative for downward movement)
                int initialDropTargetPulse = initialDropMagnitude < 0
                    ? initialDropMagnitude  // already negative, use as is
                    : -initialDropMagnitude; // positive, convert to negative

                int scanEndTargetPulse = scanEndMagnitude < 0
                    ? scanEndMagnitude  // already negative, use as is
                    : -scanEndMagnitude; // positive, convert to negative

                // Ensure the scan end target is at least -1620 for sufficient depth
                scanEndTargetPulse = Math.Min(scanEndTargetPulse, -1620);

                Debug.WriteLine($"Mapping Settings (Pulses): Initial Drop Target={initialDropTargetPulse}, Scan End Target={scanEndTargetPulse}, MmPerPulse={mmPerPulse}");

                // Pre-allocate memory for mapping data
                _mappingData = new List<DataPoint>(4000);

                // Continue with the actual mapping operation (same as before)
                // **** START HOMING SEQUENCE ****
                Debug.WriteLine("Moving elevator to top position...");
                bool elevatorUpSuccess = await Task.Run(() => ElevatorUp(token));
                if (!elevatorUpSuccess)
                {
                    _errorMessage = "Failed to home elevator to top position.";
                    return errorResult;
                }
                Debug.WriteLine("Elevator reached top position.");
                await Task.Delay(1000, token); // Stabilization delay

                // **** SET POSITION TO ZERO ****
                Debug.WriteLine("Setting absolute position to 0...");
                CardStatus status = _credenAxisCard.SetAbsPosition(3, 0);
                if (status != CardStatus.Successful)
                {
                    _errorMessage = $"Failed to set absolute position to 0: {status}";
                    Debug.WriteLine(_errorMessage);
                    return errorResult;
                }
                Debug.WriteLine("Position successfully set to 0.");
                await Task.Delay(100, token); // Allow time for setting to take effect

                // **** VERIFY SENSORS ****
                UpdateSensorStatus();
                if (_sensorStatus.StatusProtrusion != 1)
                {
                    _errorMessage = "Wafers are not placed properly (Protrusion Sensor).";
                    Debug.WriteLine("Protrusion sensor check failed before mapping");
                    return errorResult;
                }
                Debug.WriteLine("Protrusion sensor check passed.");

                // **** PHASE 1: INITIAL DROP **** (Using Control Operations)
                Debug.WriteLine("Starting initial elevator drop phase.");
                await Task.Run(async () =>
                {
                    int setupInitialPos = 0;
                    try
                    {
                        _credenAxisCard.GetAbsPosition(3, ref setupInitialPos);
                        Debug.WriteLine($"Position before initial drop: {setupInitialPos} pulses");

                        int targetPosition = initialDropTargetPulse;
                        Debug.WriteLine($"Executing initial drop to target position: {targetPosition} pulses");

                        if (setupInitialPos > targetPosition)
                        {
                            // Use defined control operations instead of hard-coded values
                            int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                            int initialDropDown1Bit = _outputList.ElevatorDown1 % 8;
                            int initialDropDown2Bit = _outputList.ElevatorDown2 % 8;

                            // Turn on elevator down motors using WriteBit method
                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, true);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, true);

                            int currentPosition = setupInitialPos;
                            bool targetReached = false;
                            var dropStopwatch = Stopwatch.StartNew();

                            while (!targetReached && dropStopwatch.ElapsedMilliseconds < 5000)
                            {
                                token.ThrowIfCancellationRequested();
                                _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                                if (currentPosition <= targetPosition)
                                {
                                    targetReached = true;
                                    Debug.WriteLine($"Initial drop target reached: Current={currentPosition}, Target={targetPosition}");
                                }
                            }
                            dropStopwatch.Stop();

                            // Turn off elevator down motors using WriteBit method
                            WriteBit(_credenIOCard1, portId, initialDropDown1Bit, false);
                            WriteBit(_credenIOCard1, portId, initialDropDown2Bit, false);

                            if (!targetReached)
                            {
                                Debug.WriteLine($"Warning: Initial drop to {targetPosition} timed out.");
                                throw new TimeoutException($"Failed to reach initial drop target {targetPosition} within timeout.");
                            }

                            await Task.Delay(250, token);

                            _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                            Debug.WriteLine($"Initial drop complete. Final Pos: {currentPosition} pulses ({currentPosition * mmPerPulse:F2}mm)");
                        }
                        else
                        {
                            Debug.WriteLine($"Already at or below target initial drop position ({targetPosition}). Current: {setupInitialPos}. Skipping drop.");
                        }

                        // **** EXTEND MAPPING ARM ****
                        Debug.WriteLine("Extending mapping arm using MappingBackward method");
                        bool armExtended = await Task.Run(() => MappingBackward(token));

                        if (!armExtended)
                        {
                            Debug.WriteLine("WARNING: Mapping arm extension failed using MappingBackward method");
                            throw new Exception("Failed to extend mapping arm");
                        }

                        await Task.Delay(100, token);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during initial setup phase: {ex.Message}");
                        await SafelyDisableAllOutputs();
                        throw;
                    }
                }, token);

                // **** PHASE 2: HIGH-SPEED MAPPING SCAN WITH REAL-TIME ANALYSIS ****
                Debug.WriteLine("Starting high-speed mapping scan phase with real-time analysis and raw data collection.");

                // Direct collection list for analysis
                var rawData = new List<DataPoint>(4000);

                int mappingStartPos = 0;
                _credenAxisCard.GetAbsPosition(3, ref mappingStartPos);
                Debug.WriteLine($"Position before scan: {mappingStartPos} pulses");

                // Engage elevator down motors using defined control operations
                int motorPortId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                int scanElevatorDown1Bit = _outputList.ElevatorDown1 % 8;
                int scanElevatorDown2Bit = _outputList.ElevatorDown2 % 8;

                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, true);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, true);

                // CRITICAL SECTION - Optimize for speed
                semReadPort.WaitOne();
                acquired = true;

                var mappingStopwatch = Stopwatch.StartNew();
                byte sensorReadByte = 0;
                int currentPos = mappingStartPos;
                int scanEndTarget = scanEndTargetPulse;

                // Determine which bit to check based on sensor type
                int bitPosition = sensorType == 0 ? 14 - 8 : 15 - 8; // Adjust for port 1 (bits 8-15)
                int sensorBitMask = 1 << bitPosition;

                // Debug sensor configuration information
                byte initialSensorByte = 0;
                _credenIOCard2.ReadPort(1, ref initialSensorByte);
                Debug.WriteLine($"===== MAPPING SENSOR DIAGNOSTICS =====");
                Debug.WriteLine($"Sensor Type Selected: {sensorType} ({(sensorType == 0 ? "Input 14" : "Input 15")})");
                Debug.WriteLine($"Bit Position: {bitPosition + 8} (Port 1, bit {bitPosition})");
                Debug.WriteLine($"Sensor Bit Mask: 0x{sensorBitMask:X2}");
                Debug.WriteLine($"Initial Sensor Byte Value: 0x{initialSensorByte:X2} (Binary: {Convert.ToString(initialSensorByte, 2).PadLeft(8, '0')})");
                Debug.WriteLine($"Input 14 Status: {((initialSensorByte & 0x40) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Input 15 Status: {((initialSensorByte & 0x80) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"Selected Sensor Status: {((initialSensorByte & sensorBitMask) == 0 ? "ACTIVE(0)" : "INACTIVE(1)")}");
                Debug.WriteLine($"=======================================");

                // Initialize counters for sensor activity
                int totalReads = 0;
                int selectedSensorActivations = 0;

                // ULTRA-FAST LOOP: Minimize operations, collect data for analysis
                while (currentPos > scanEndTarget)
                {
                    // 1. Read position directly - minimal overhead
                    _credenAxisCard.GetAbsPosition(3, ref currentPos);

                    // 2. Read sensor value directly - minimal overhead
                    _credenIOCard2.ReadPort(1, ref sensorReadByte);

                    // Test selected sensor configuration
                    bool selectedSensorActive = (sensorReadByte & sensorBitMask) == 0;

                    // Increment counters for diagnostics
                    totalReads++;
                    if (selectedSensorActive) selectedSensorActivations++;

                    // Use ONLY the selected sensor bit (input 14 or 15 on card 2)
                    int sensorValue = selectedSensorActive ? 1 : 0;

                    // 3. Store minimal raw data for analysis
                    rawData.Add(new DataPoint
                    {
                        TimeMs = mappingStopwatch.ElapsedMilliseconds,
                        Position = currentPos * mmPerPulse, // Only essential conversion
                        SensorValue = sensorValue,
                        Velocity = 0 // Skip velocity calculation entirely
                    });
                }

                // End critical section
                long scanTime = mappingStopwatch.ElapsedMilliseconds;
                mappingStopwatch.Stop();
                if (acquired)
                {
                    semReadPort.Release();
                    acquired = false;
                }

                // Stop elevator motors immediately using WriteBit
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown1Bit, false);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorDown2Bit, false);
                Debug.WriteLine("Elevator motors stopped after scan loop.");

                // Print sensor activity statistics
                Debug.WriteLine($"===== MAPPING SENSOR ACTIVITY STATISTICS =====");
                Debug.WriteLine($"Total reads: {totalReads}");
                Debug.WriteLine($"Selected sensor activations: {selectedSensorActivations} ({(totalReads > 0 ? selectedSensorActivations * 100.0 / totalReads : 0):F2}%)");
                Debug.WriteLine($"Scan time: {scanTime}ms");
                Debug.WriteLine($"===========================================");

                // Copy collected data to main collection for compatibility
                _mappingData = rawData;

                // **** PHASE 3: REAL-TIME WAFER SLOT ANALYSIS ****
                Debug.WriteLine("Starting real-time wafer slot analysis...");

                // Perform wafer slot analysis using collected data with the parameters from the mapping table
                FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult = FOUPCtrl.WaferMap.PerformMappingAnalysisWithTypeParameters(
                    rawData,
                    firstWaferPosMm,        // First wafer position from mapping table
                    slotPitchMm,            // Slot pitch from mapping table (now guaranteed to be negative)
                    expectedSlots,          // Expected slots from mapping table
                    slotPitchMm,            // Type slot pitch (same as above, now negative)
                    positionToleranceMm,    // Position tolerance from mapping table
                    waferThicknessMm,       // Wafer thickness from mapping table
                    thicknessToleranceMm,   // Thickness tolerance from mapping table
                    expectedSlots,          // Type slot count (same as expected slots)
                    typeName,               // Type name from mapping table
                    (msg) => Debug.WriteLine($"Analysis: {msg}") // Logger
                );

                Debug.WriteLine($"Analysis complete: {analysisResult.DetectedWaferCount} wafers detected in {expectedSlots} slots");

                // Update mapping status
                if (rawData.Count > 0)
                {
                    m_status[17] = (char)MappingStatus.Completed;
                    Debug.WriteLine($"Mapping data collection and analysis successful: {rawData.Count} points analyzed.");
                }
                else
                {
                    Debug.WriteLine("No mapping data was collected during the scan.");
                    m_status[17] = (char)MappingStatus.Inexecution;
                }

                // **** PHASE 4: CLEANUP - RETRACT ARM ****
                Debug.WriteLine("Retracting mapping arm using MappingForward method...");
                bool mappingForwardSuccess = await Task.Run(() => MappingForward(token));

                if (!mappingForwardSuccess)
                    Debug.WriteLine("Warning: Mapping arm retraction may not have completed properly.");
                else
                    Debug.WriteLine("Mapping arm successfully retracted.");

                // **** PHASE 5: FINAL ELEVATOR DESCENT ****
                Debug.WriteLine("Mapping arm retracted. Moving elevator to lowest position...");
                bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));

                if (!elevatorDownSuccess)
                    Debug.WriteLine("Warning: Full elevator descent may not have completed properly.");
                else
                    Debug.WriteLine("Elevator successfully reached lowest position.");

                // **** PHASE 6: EXPORT RAW MAPPING DATA ****
                if (_mappingData.Count > 0)
                {
                    // Export data to CSV using streamlined format
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data");

                    Debug.WriteLine($"Exporting {_mappingData.Count} raw data points to: {savePath}");
                    bool exportSuccess = ExportMappingDataRaw(savePath);

                    if (exportSuccess)
                        Debug.WriteLine("Raw mapping data exported successfully.");
                    else
                        Debug.WriteLine($"Raw data export failed: {_errorMessage}");
                }

                // **** DISPLAY MAPPING RESULT IN DESIRED FORMAT ****
                string mappingResultString = GetMappingResultString(analysisResult);
                Debug.WriteLine($"=== MAPPING RESULT FORMAT ===");
                Debug.WriteLine($"Mapping result: {mappingResultString}");
                Debug.WriteLine($"Result length: {mappingResultString.Length}");
                Debug.WriteLine($"=== END MAPPING RESULT ===");

                Debug.WriteLine("MappingOperation_UpToDown_WithAnalysis completed successfully.");
                return analysisResult;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                _errorMessage = $"Failed to access required settings properties: {ex.Message}";
                Debug.WriteLine($"Error accessing settings properties: {ex.Message}");
                Debug.WriteLine("Make sure the settings object implements all required properties and methods.");
                return errorResult;
            }
            catch (OperationCanceledException)
            {
                _errorMessage = "Mapping operation was canceled.";
                Debug.WriteLine(_errorMessage);
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
                return errorResult;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Critical error in mapping sequence: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
                return errorResult;
            }
            finally
            {
                // Only release if not already released and was acquired
                if (acquired)
                {
                    try { semReadPort.Release(); } catch (SemaphoreFullException) { }
                }
                Debug.WriteLine("MappingOperation_UpToDown_WithAnalysis finished.");
            }
        }

        public async Task<FOUPCtrl.WaferMap.MappingAnalysisResult> MappingOperation_DownToUp_WithAnalysis(CancellationToken token, IMappingSettings settings)
        {
            // Initialize error result
            var errorResult = new FOUPCtrl.WaferMap.MappingAnalysisResult(25);
            for (int i = 0; i < 25; i++)
                errorResult.WaferStatus[i] = 99;

            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine("Error: Not all cards are connected in MappingOperation_DownToUp_WithAnalysis");
                return errorResult;
            }

            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null, cannot perform mapping.";
                Debug.WriteLine("Error: IMappingSettings object is null in MappingOperation_DownToUp_WithAnalysis.");
                return errorResult;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return errorResult;
            }

            int sensorType = settings.SensorType;
            dynamic settingsObj = settings;
            MappingTable mappingTable = null;
            int expectedSlots = 25;
            bool acquired = false;

            try
            {
                int activeType = settingsObj.ActiveMappingType;
                if (settings is MappingTypeProfile mappingProfile)
                    m_status[18] = (char)((int)PodType.Type1 + mappingProfile.FOUPTypeIndex);

                mappingTable = settingsObj.GetMappingTableByNumber(activeType);
                if (mappingTable == null)
                {
                    _errorMessage = $"Could not load mapping table for type {activeType}";
                    return errorResult;
                }

                expectedSlots = mappingTable.SlotCount;
                double slotPitchMm = mappingTable.SlotPitchMm;
                double firstWaferPosMm = mappingTable.FirstSlotPositionMm;
                double waferThicknessMm = mappingTable.WaferThicknessMm;
                double thicknessToleranceMm = mappingTable.ThicknessRangeMm;
                double positionToleranceMm = mappingTable.PositionRangeMm;
                string typeName = mappingTable.Name;

                // --- MODIFICATION: Ensure slotPitchMm is positive for upward mapping ---
                if (slotPitchMm < 0)
                {
                    slotPitchMm = -slotPitchMm;
                    Debug.WriteLine($"Automatically converted SlotPitchMm from negative to positive: {slotPitchMm}mm (for upward mapping)");
                }

                // Convert mapping positions to pulses (for up scan, start is more negative, end is closer to zero)
                int scanStartMagnitude = (int)settings.MapEndPositionMm - 120;
                int scanEndMagnitude = (int)settings.MapStartPositionMm - 280;

                int scanStartTargetPulse = scanStartMagnitude < 0 ? scanStartMagnitude : -scanStartMagnitude;
                int scanEndTargetPulse = scanEndMagnitude < 0 ? scanEndMagnitude : -scanEndMagnitude;

                if (scanStartTargetPulse > scanEndTargetPulse)
                {
                    _errorMessage = "Invalid mapping positions: End position must be closer to home (0) than start position for down-to-up mapping";
                    return errorResult;
                }

                _mappingData = new List<DataPoint>(4000);

                // PHASE 1: Move elevator to bottom if needed
                UpdateSensorStatus();
                if (_sensorStatus.StatusElevatorDown != 1)
                {
                    bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));
                    if (!elevatorDownSuccess)
                    {
                        _errorMessage = "Failed to move elevator to bottom position.";
                        return errorResult;
                    }
                }

                // PHASE 2: Move elevator to scan start position
                int startPosition = 0;
                CardStatus status = _credenAxisCard.GetAbsPosition(3, ref startPosition);
                if (status != CardStatus.Successful)
                {
                    _errorMessage = $"Failed to read absolute position: {status}";
                    return errorResult;
                }

                if (startPosition < scanStartTargetPulse)
                {
                    int portId = _outputList.ElevatorUp1 < 8 ? 2 : 3;
                    int elevatorUp1Bit = _outputList.ElevatorUp1 % 8;
                    int elevatorUp2Bit = _outputList.ElevatorUp2 % 8;
                    WriteBit(_credenIOCard1, portId, elevatorUp1Bit, true);
                    WriteBit(_credenIOCard1, portId, elevatorUp2Bit, true);
                    int currentPosition = startPosition;
                    bool targetReached = false;
                    var moveStopwatch = Stopwatch.StartNew();
                    while (!targetReached && !token.IsCancellationRequested && moveStopwatch.ElapsedMilliseconds < 10000)
                    {
                        _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                        if (currentPosition >= scanStartTargetPulse)
                            targetReached = true;
                    }
                    WriteBit(_credenIOCard1, portId, elevatorUp1Bit, false);
                    WriteBit(_credenIOCard1, portId, elevatorUp2Bit, false);
                    if (!targetReached)
                        throw new TimeoutException("Failed to reach scanning start position");
                }
                else if (startPosition > scanStartTargetPulse)
                {
                    int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                    int elevatorDown1Bit = _outputList.ElevatorDown1 % 8;
                    int elevatorDown2Bit = _outputList.ElevatorDown2 % 8;
                    WriteBit(_credenIOCard1, portId, elevatorDown1Bit, true);
                    WriteBit(_credenIOCard1, portId, elevatorDown2Bit, true);
                    int currentPosition = startPosition;
                    bool targetReached = false;
                    var moveStopwatch = Stopwatch.StartNew();
                    while (!targetReached && !token.IsCancellationRequested && moveStopwatch.ElapsedMilliseconds < 10000)
                    {
                        _credenAxisCard.GetAbsPosition(3, ref currentPosition);
                        if (currentPosition <= scanStartTargetPulse)
                            targetReached = true;
                    }
                    WriteBit(_credenIOCard1, portId, elevatorDown1Bit, false);
                    WriteBit(_credenIOCard1, portId, elevatorDown2Bit, false);
                    if (!targetReached)
                        throw new TimeoutException("Failed to reach scanning start position");
                }

                // PHASE 3: Extend mapping arm
                bool armExtended = await Task.Run(() => MappingBackward(token));
                if (!armExtended)
                    throw new Exception("Failed to extend mapping arm");

                // PHASE 4: Perform mapping scan up
                int mappingStartPos = 0;
                _credenAxisCard.GetAbsPosition(3, ref mappingStartPos);
                int motorPortId = _outputList.ElevatorUp1 < 8 ? 2 : 3;
                int scanElevatorUp1Bit = _outputList.ElevatorUp1 % 8;
                int scanElevatorUp2Bit = _outputList.ElevatorUp2 % 8;
                var rawData = new List<DataPoint>(4000);

                int bitPosition = sensorType == 0 ? 14 - 8 : 15 - 8;
                int sensorBitMask = 1 << bitPosition;

                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, true);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, true);

                semReadPort.WaitOne();
                acquired = true;
                var mappingStopwatch = Stopwatch.StartNew();
                byte sensorReadByte = 0;
                int currentPos = mappingStartPos;

                while (currentPos < scanEndTargetPulse)
                {
                    _credenAxisCard.GetAbsPosition(3, ref currentPos);
                    _credenIOCard2.ReadPort(1, ref sensorReadByte);
                    bool selectedSensorActive = (sensorReadByte & sensorBitMask) == 0;
                    int sensorValue = selectedSensorActive ? 1 : 0;
                    rawData.Add(new DataPoint
                    {
                        TimeMs = mappingStopwatch.ElapsedMilliseconds,
                        Position = currentPos * mmPerPulse,
                        SensorValue = sensorValue,
                        Velocity = 0
                    });
                }
                mappingStopwatch.Stop();
                semReadPort.Release();
                acquired = false;

                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp1Bit, false);
                WriteBit(_credenIOCard1, motorPortId, scanElevatorUp2Bit, false);

                _mappingData = rawData;

                // PHASE 5: Analysis (reverse slot order)
                // For up scan, the first slot is at the end of the scan, so reverse the slot order in the result
                var analysisResult = FOUPCtrl.WaferMap.PerformMappingAnalysisWithTypeParameters(
                    rawData,
                    firstWaferPosMm,
                    slotPitchMm,
                    expectedSlots,
                    slotPitchMm,
                    positionToleranceMm,
                    waferThicknessMm,
                    thicknessToleranceMm,
                    expectedSlots,
                    typeName,
                    (msg) => Debug.WriteLine($"Analysis: {msg}")
                );

                Array.Reverse(analysisResult.WaferStatus);
                Array.Reverse(analysisResult.WaferThicknessMm);
                Array.Reverse(analysisResult.SlotRefPositionPulses);
                Array.Reverse(analysisResult.WaferBottomEdgePulses);
                Array.Reverse(analysisResult.WaferTopEdgePulses);

                // PHASE 6: Cleanup
                bool mappingForwardSuccess = await Task.Run(() => MappingForward(token));
                await Task.Run(() => ElevatorUp(token)); // Move elevator to home (top) position

                // Export data if needed
                if (_mappingData.Count > 0)
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data_Up");
                    ExportMappingDataRaw(savePath);
                }

                return analysisResult;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Critical error in mapping sequence: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[17] = (char)MappingStatus.Inexecution;
                return errorResult;
            }
            finally
            {
                if (acquired)
                {
                    try { semReadPort.Release(); } catch (SemaphoreFullException) { }
                }
                Debug.WriteLine("MappingOperation_UpToDown finished.");
            }
        }
        /// <summary>
        /// Performs the Mapping Auto-Calibration sequence, calculating slot pitch
        /// and slot 1 position based on detected wafers in the FOUP.
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings that include start/end positions</param>
        /// <param name="callbackAction">Optional action to call for status updates</param>
        /// <returns>Tuple containing (success, avgPitch, slot1Pos, detectedWaferCount, avgThickness)</returns>
        public async Task<(bool Success, double AvgPitch, double Slot1Pos, int WaferCount, double AvgThickness)>
        MappingAutoCalibration(CancellationToken token, IMappingSettings settings, Action<string> callbackAction = null)
        {
            void Log(string message)
            {
                Debug.WriteLine(message);
                callbackAction?.Invoke(message);
            }

            try
            {
                Log("--- Starting Mapping Auto Calibration Sequence ---");

                // 1. Verify that settings are provided
                if (settings == null)
                {
                    Log("Auto Calibration Error: No settings provided");
                    return (false, 0, 0, 0, 0);
                }

                // 2. Verify start and end positions are set properly
                if (Math.Abs(settings.MapEndPositionMm) <= Math.Abs(settings.MapStartPositionMm))
                {
                    Log($"Auto Calibration Error: Invalid mapping range - Start: {settings.MapStartPositionMm}, End: {settings.MapEndPositionMm}");
                    return (false, 0, 0, 0, 0);
                }

                // 3. Initialize mapping data collection
                _mappingData.Clear();
                Log("Starting elevator movement for auto-calibration...");

                // 4. Ensure appropriate starting position - Elevator should be at bottom position
                UpdateSensorStatus();
                if (_sensorStatus.StatusElevatorDown != 1)
                {
                    Log("Moving elevator to down position...");
                    bool elevatorDownSuccess = await Task.Run(() => ElevatorDown(token));

                    // Brief pause to ensure elevator is stable
                    await Task.Delay(500, token);

                    // Verify down position reached
                    UpdateSensorStatus();
                    if (_sensorStatus.StatusElevatorDown != 1)
                    {
                        Log("Auto Calibration Error: Could not move elevator to down position");
                        return (false, 0, 0, 0, 0);
                    }
                }

                // 5. Safety check for software limits
                double softwareMin = -5;  // Minimum allowable position
                double softwareMax = -1650; // Maximum allowable position (adjust as needed)

                if (settings.MapStartPositionMm > softwareMin || settings.MapEndPositionMm < softwareMax)
                {
                    Log($"Auto Calibration Error: Positions exceed software limits - Start: {settings.MapStartPositionMm}, End: {settings.MapEndPositionMm}");
                    return (false, 0, 0, 0, 0);
                }

                if (settings.MapStartPositionMm > 0 || settings.MapEndPositionMm < -1650)
                {
                    Log($"Auto Calibration Error: Positions exceed software limits - Start: {settings.MapStartPositionMm}, End: {settings.MapEndPositionMm}");
                    Log($"Valid range is from 0 to -1650");
                    return (false, 0, 0, 0, 0);
                }

                // 6. Start the actual mapping operation
                Log($"Starting mapping scan from {settings.MapStartPositionMm}mm to {settings.MapEndPositionMm}mm");

                // Use the existing MappingOperation_UpToDown_HighSpeed method for better data quality
                await MappingOperation_UpToDown_HighSpeed(token, settings);

                // 7. Process the collected data
                Log("Processing mapping data...");
                var mappingData = GetMappingData();

                if (mappingData == null || mappingData.Count < 10)  // Arbitrary minimum data point threshold
                {
                    Log($"Auto Calibration Error: Not enough data points collected ({(mappingData?.Count ?? 0)})");
                    return (false, 0, 0, 0, 0);
                }

                // 8. Analyze the data to find wafers and calculate pitch
                Log($"Analyzing {mappingData.Count} data points to find wafer edges");

                // Find wafer edges in the collected data
                List<(double startPos, double endPos)> waferEdges = FindWaferEdges(mappingData);

                if (waferEdges.Count < 2)
                {
                    Log($"Auto Calibration Error: Not enough wafers detected ({waferEdges.Count})");
                    return (false, 0, 0, 0, 0);
                }

                // 9. Calculate wafer centers 
                List<double> waferCenters = new List<double>();
                foreach (var edge in waferEdges)
                {
                    double center = (edge.startPos + edge.endPos) / 2.0;
                    waferCenters.Add(center);
                }

                // Sort by position for initial ordering
                waferEdges = waferEdges.OrderBy(w => Math.Abs(w.startPos)).ToList();
                Log($"Found {waferEdges.Count} wafers in the mapping data (sorted by position).");

                // Recalculate wafer centers after sorting
                waferCenters.Clear();
                foreach (var edge in waferEdges)
                {
                    double center = (edge.startPos + edge.endPos) / 2.0;
                    waferCenters.Add(center);
                }

                // Determine if we're in a negative coordinate system (similar to ProcessTrainingData)
                bool isNegativeCoordinateSystem = waferCenters.Any() && waferCenters[0] < 0;
                Log($"Detected coordinate system: {(isNegativeCoordinateSystem ? "Negative" : "Positive")}");

                // For negative coordinate systems, the first slot is the one with highest value (closest to zero)
                // For positive coordinate systems, the first slot is the one with lowest value
                if (isNegativeCoordinateSystem)
                {
                    // Sort from highest to lowest (closest to zero first)
                    waferCenters = waferCenters.OrderByDescending(c => c).ToList();
                }
                else
                {
                    // Sort from lowest to highest
                    waferCenters = waferCenters.OrderBy(c => c).ToList();
                }

                double firstWaferCenterMm = waferCenters.First();
                double lastWaferCenterMm = waferCenters.Last();

                Log($"First wafer center: {firstWaferCenterMm:F3}mm");
                Log($"Last wafer center: {lastWaferCenterMm:F3}mm");

                // Get expected slot count from settings
                int expectedSlots = 25; // Default to 25 if we can't get it from settings

                // Try to get the slot count from MappingTable via IMappingSettings
                try
                {
                    var settingsWithTables = settings as dynamic;
                    if (settingsWithTables != null)
                    {
                        // Get MappingTableNo from settings if it's a MappingTypeProfile
                        int mappingTableNo = 1;
                        if (settings is MappingTypeProfile mappingProfile)
                        {
                            mappingTableNo = mappingProfile.MappingTableNo;
                            Log($"Using mapping table number {mappingTableNo} from profile");
                        }

                        // Get the mapping table and read its SlotCount
                        var mappingTable = settingsWithTables.GetMappingTableByNumber(mappingTableNo);
                        if (mappingTable != null && mappingTable.SlotCount > 0)
                        {
                            expectedSlots = mappingTable.SlotCount;
                            Log($"Using configured slot count from mapping table: {expectedSlots}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Could not get slot count from settings: {ex.Message}");
                    Log($"Using default slot count: {expectedSlots}");
                }

                // Calculate pitch - will be negative in negative coordinate system (same as ProcessTrainingData)
                double distance = lastWaferCenterMm - firstWaferCenterMm;
                int numberOfGaps = expectedSlots - 1;

                if (numberOfGaps <= 0)
                {
                    Log($"Auto Calibration Error: Invalid expected slots count ({expectedSlots}). Must be > 1.");
                    return (false, 0, 0, 0, 0);
                }

                // Calculate average pitch (maintains sign for negative coordinates)
                double avgPitch = distance / numberOfGaps;

                // First wafer center is our slot 1 position
                double slot1Pos = firstWaferCenterMm;

                Log($"Distance between first and last wafer: {distance:F3}mm");
                Log($"Average pitch calculation: {distance:F3}mm ÷ {numberOfGaps} gaps = {avgPitch:F3}mm/slot");
                Log($"Using first wafer center as slot 1 position: {slot1Pos:F3}mm");

                // 10. Calculate wafer thickness for reference
                double avgThickness = 0;
                foreach (var edge in waferEdges)
                {
                    avgThickness += Math.Abs(edge.endPos - edge.startPos);
                }
                avgThickness /= waferEdges.Count;

                // 11. Log the results
                Log("Auto calibration completed successfully");
                Log($"Detected average pitch: {avgPitch:F3} mm");
                Log($"First slot position (slot 1): {slot1Pos:F3} mm");
                Log($"Detected wafer count: {waferEdges.Count}");
                Log($"Detected average thickness: {avgThickness:F3} mm");

                // 12. Return the elevator to down position
                Log("Returning elevator to down position...");
                await Task.Run(() => ElevatorDown(token));

                // 13. Return calibration results
                Log("--- Mapping Auto Calibration Complete ---");

                return (true, avgPitch, slot1Pos, waferEdges.Count, avgThickness);
            }
            catch (OperationCanceledException)
            {
                Log("Mapping auto-calibration was cancelled");
                return (false, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Log($"Auto Calibration Error: {ex.Message}");
                return (false, 0, 0, 0, 0);
            }
        }
        private List<(double startPos, double endPos)> FindWaferEdges(List<FOUP_Ctrl.DataPoint> data)
        {
            List<(double startPos, double endPos)> edges = new List<(double startPos, double endPos)>();
            double? currentStart = null;

            for (int i = 1; i < data.Count; i++)
            {
                // Rising edge (start of wafer detection)
                if (data[i - 1].SensorValue == 0 && data[i].SensorValue == 1)
                {
                    currentStart = data[i].Position;
                }
                // Falling edge (end of wafer detection)
                else if (data[i - 1].SensorValue == 1 && data[i].SensorValue == 0 && currentStart.HasValue)
                {
                    edges.Add((currentStart.Value, data[i - 1].Position));
                    currentStart = null;
                }
            }

            // If we have a start without an end (e.g., scan ended while on a wafer)
            if (currentStart.HasValue && data.Count > 0)
            {
                // Use the last position as the end
                edges.Add((currentStart.Value, data[data.Count - 1].Position));
            }

            return edges;
        }
        public bool ExportMappingDataRaw(string savePath)
        {
            try
            {
                if (_mappingData == null || _mappingData.Count == 0)
                {
                    _errorMessage = "No mapping data available to export";
                    return false;
                }

                // Create directory if needed
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                // Create filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string csvPath = Path.Combine(savePath, $"MappingData_{timestamp}.csv");

                // Use high-performance StreamWriter with large buffer
                using (StreamWriter writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8, 65536))
                {
                    // Write simple header
                    writer.WriteLine("Time (ms),Position (mm),Sensor Value");

                    // Write data with minimal formatting
                    foreach (var point in _mappingData)
                    {
                        // Only write essential columns, avoid string formatting where possible
                        writer.Write(point.TimeMs);
                        writer.Write(',');
                        writer.Write(point.Position.ToString("F2"));
                        writer.Write(',');
                        writer.WriteLine(point.SensorValue);
                    }
                }

                Debug.WriteLine($"Successfully exported {_mappingData.Count} data points to: {csvPath}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error exporting data: {ex.Message}";
                Debug.WriteLine($"Error exporting data: {ex.Message}");
                return false;
            }
        }
        public List<DataPoint> GetMappingData()
        {
            return _mappingData;
        }
        public string GetMappingResultString(FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult)
        {
            if (analysisResult?.WaferStatus == null)
            {
                Debug.WriteLine("GetMappingResultString: Analysis result or wafer status is null");
                return "".PadLeft(25, '9'); // Return error status for all slots
            }

            var result = new System.Text.StringBuilder();

            for (int i = 0; i < analysisResult.WaferStatus.Length; i++)
            {
                // Convert wafer status to single character using traditional switch statement
                // 0 = Empty, 1 = Normal, 2 = Crossed, 3 = Thick, 4 = Thin, 5 = Position Error, 99 = Error
                char statusChar;
                switch (analysisResult.WaferStatus[i])
                {
                    case 0:
                        statusChar = '0';  // Empty
                        break;
                    case 1:
                        statusChar = '1';  // Normal
                        break;
                    case 2:
                        statusChar = '2';  // Crossed
                        break;
                    case 3:
                        statusChar = '3';  // Thick
                        break;
                    case 4:
                        statusChar = '4';  // Thin
                        break;
                    case 5:
                        statusChar = '5';  // Position Error
                        break;
                    case 99:
                        statusChar = '9';  // Error
                        break;
                    default:
                        statusChar = '9';  // Unknown - treat as error
                        break;
                }

                result.Append(statusChar);
            }

            string mappingResult = result.ToString();
            Debug.WriteLine($"Mapping result: {mappingResult}");
            Debug.WriteLine($"GetMappingResult returning: '{mappingResult}' (Length: {mappingResult.Length})");

            return mappingResult;
        }
        #endregion

        #region Sequence Operations
        public void Lock(CancellationToken token)
        {
            if (IsErrorExist())
            {
                return;
            }

            bool bMotionDone = false;

            m_status[3] = (char)Operation.Operating;
            bMotionDone = Clamp(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                return;
            }
            Thread.Sleep(DelayBetweenTask);

            bMotionDone = Unlatch(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                return;
            }

            m_status[3] = (char)Operation.Stopping;
        }
        public void Unlock(CancellationToken token)
        {
            if (IsErrorExist())
            {
                return;
            }

            bool bMotionDone = false;

            m_status[3] = (char)Operation.Operating;
            bMotionDone = Latch(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                return;
            }
            Thread.Sleep(DelayBetweenTask);

            bMotionDone = Unclamp(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                return;
            }

            m_status[3] = (char)Operation.Stopping;
        }
        public bool ExecuteFOUPLoadSequence(CancellationToken token)
        {
            if (IsErrorExist())
            {
                return false;
            }

            bool bMotionDone = false;

            m_status[3] = (char)Operation.Operating;

            // Step 1: Clamp
            Debug.WriteLine("Executing clamp operation...");
            bMotionDone = Clamp(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Clamp operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 2: Dock Forward
            Debug.WriteLine("Executing dock forward operation...");
            bMotionDone = DockForward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Dock forward operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 3: Latch
            Debug.WriteLine("Executing latch operation...");
            bMotionDone = Latch(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Latch operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 4: Vacuum On
            Debug.WriteLine("Executing vacuum on operation...");
            bMotionDone = VacuumOn(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Vacuum on operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 5: Door Forward (Open)
            Debug.WriteLine("Executing door open operation...");
            bMotionDone = DoorForward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Door forward operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 6: Elevator Down
            Debug.WriteLine("Executing elevator down operation...");
            bMotionDone = ElevatorDown(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Elevator down operation failed");
                return false;
            }

            m_status[2] = (char)LoadStatus.LoadPosition;
            m_status[3] = (char)Operation.Stopping;
            Debug.WriteLine("FOUP load sequence completed successfully");
            return true;
        }
        public bool ExecuteFOUPUnloadSequence(CancellationToken token)
        {
            if (IsErrorExist())
            {
                return false;
            }

            bool bMotionDone = false;

            m_status[3] = (char)Operation.Operating;

            // Step 1: Elevator Up
            Debug.WriteLine("Executing elevator up operation...");
            bMotionDone = ElevatorUp(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Elevator up operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 2: Door Backward (Close)
            Debug.WriteLine("Executing door close operation...");
            bMotionDone = DoorBackward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Door close operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 3: Unlatch
            Debug.WriteLine("Executing unlatch operation...");
            bMotionDone = Unlatch(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Unlatch operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 4: Vacuum Off
            Debug.WriteLine("Executing vacuum off operation...");
            bMotionDone = VacuumOff(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Vacuum off operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 5: Dock Backward (Retract)
            Debug.WriteLine("Executing dock backward operation...");
            bMotionDone = DockBackward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Dock backward operation failed");
                return false;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 6: Unclamp
            Debug.WriteLine("Executing unclamp operation...");
            bMotionDone = Unclamp(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Unclamp operation failed");
                return false;
            }

            m_status[2] = (char)LoadStatus.HomePosition;
            m_status[3] = (char)Operation.Stopping;
            Debug.WriteLine("FOUP unload sequence completed successfully");
            return true;
        }
        public async Task<bool> ExecuteFOUPLoadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing FOUP-specific load+mapping sequence");

            if (IsErrorExist())
            {
                Debug.WriteLine("Cannot execute operation due to existing errors");
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine("Error: Not all cards are connected");
                return false;
            }

            // Validate settings early
            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null.";
                Debug.WriteLine("Error: Settings object is null");
                return false;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return false;
            }

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // PHASE 1: INDIVIDUAL FOUP LOAD OPERATIONS
                Debug.WriteLine("Executing FOUP load operations...");

                // Step 1: Clamp
                Debug.WriteLine("Executing clamp operation...");
                bool bMotionDone = Clamp(token);
                if (!bMotionDone)
                {
                    m_status[3] = (char)Operation.Stopping;
                    Debug.WriteLine("Clamp operation failed");
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                // Step 2: Dock Forward
                Debug.WriteLine("Executing dock forward operation...");
                bMotionDone = DockForward(token);
                if (!bMotionDone)
                {
                    m_status[3] = (char)Operation.Stopping;
                    Debug.WriteLine("Dock forward operation failed");
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                //// Step 3: Latch
                //Debug.WriteLine("Executing latch operation...");
                //bMotionDone = Latch(token);
                //if (!bMotionDone)
                //{
                //    m_status[3] = (char)Operation.Stopping;
                //    Debug.WriteLine("Latch operation failed");
                //    return false;
                //}
                //await Task.Delay(DelayBetweenTask, token);

                //// Step 4: Vacuum On (Uncommented - usually required for FOUP operations)
                //Debug.WriteLine("Executing vacuum on operation...");
                //bMotionDone = VacuumOn(token);
                //if (!bMotionDone)
                //{
                //    m_status[3] = (char)Operation.Stopping;
                //    Debug.WriteLine("Vacuum on operation failed");
                //    return false;
                //}
                //await Task.Delay(DelayBetweenTask, token);

                //// Step 5: Door Forward (Open) (Uncommented - usually required to access wafers)
                //Debug.WriteLine("Executing door open operation...");
                //bMotionDone = DoorForward(token);
                //if (!bMotionDone)
                //{
                //    m_status[3] = (char)Operation.Stopping;
                //    Debug.WriteLine("Door forward operation failed");
                //    return false;
                //}
                //await Task.Delay(DelayBetweenTask, token);

                // PHASE 2: PERFORM MAPPING OPERATION WITH ANALYSIS
                Debug.WriteLine("Starting mapping operation with analysis...");
                var analysisResult = await MappingOperation_UpToDown_WithAnalysis(token, settings);

                // Check if analysis was successful
                bool analysisSuccessful = true;
                for (int i = 0; i < analysisResult.WaferStatus.Length; i++)
                {
                    if (analysisResult.WaferStatus[i] == 99) // Check if any slot has error status
                    {
                        analysisSuccessful = false;
                        break;
                    }
                }

                if (!analysisSuccessful)
                {
                    _errorMessage = "Mapping analysis failed - error status detected";
                    Debug.WriteLine("Mapping analysis failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Log analysis results
                Debug.WriteLine($"Mapping analysis completed: {analysisResult.DetectedWaferCount} wafers detected");
                for (int i = 0; i < Math.Min(analysisResult.ExpectedSlots, 5); i++) // Log first 5 slots as example
                {
                    string statusText = GetSlotStatusText(analysisResult.WaferStatus[i]);
                    Debug.WriteLine($"Slot {i + 1}: {statusText}, Thickness: {analysisResult.WaferThicknessMm[i]:F3}mm");
                }

                // Update status to indicate successful load and mapping
                m_status[2] = (char)LoadStatus.LoadPosition;
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("FOUP load+mapping sequence completed successfully");

                // Store the analysis result so it can be accessed by the UI
                _lastMappingAnalysisResult = analysisResult;

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("FOUP load+mapping sequence was canceled");
                await SafelyDisableAllOutputs();
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error during FOUP load+mapping: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return false;
            }
        }
        public async Task<bool> ExecuteFOUPUnloadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            if (IsErrorExist())
            {
                return false;
            }

            UpdateSensorStatus();

            //// Check for pod presence using protrusion sensor
            //if (_sensorStatus.StatusProtrusion != 1)
            //{
            //    _errorMessage = "No POD detected.";
            //    sStatusCode = FOUPInfo.InterlockExist;
            //    sInterlockCode = Interlock.NotUnlatched;
            //    Debug.WriteLine("Error: No POD detected (protrusion sensor)");
            //    return false;
            //}

            //// Check unlatch status
            //if (_sensorStatus.StatusUnlatch != 1)
            //{
            //    _errorMessage = "Pod is not unlatched.";
            //    sStatusCode = FOUPInfo.InterlockExist;
            //    sInterlockCode = Interlock.NotUnlatched;
            //    Debug.WriteLine("Error: Pod is not unlatched");
            //    return false;
            //}

            //// Check for clamping status
            //if (_sensorStatus.StatusClamp != 1)
            //{
            //    _errorMessage = "Pod is not clamped.";
            //    sStatusCode = FOUPInfo.InterlockExist;
            //    sInterlockCode = Interlock.NotUnlatched;
            //    Debug.WriteLine("Error: Pod is not clamped");
            //    return false;
            //}

            //// Check current load status
            //if (m_status[2] != (char)LoadStatus.LoadPosition)
            //{
            //    sStatusCode = FOUPInfo.InterlockExist;
            //    sInterlockCode = Interlock.NotUnlatched;
            //    _errorMessage = "Not in Load Position";
            //    Debug.WriteLine("Error: Not in Load Position");
            //    return false;
            //}

            bool bMotionDone = false;

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Execute the mapping operation - CHANGED to use DownToUp instead of UpToDown
                Debug.WriteLine("Starting mapping operation sequence using bottom-to-top scanning");
                await MappingOperation_DownToUp(token, settings);  // Changed to DownToUp

                // Give system time to stabilize after mapping
                await Task.Delay(DelayBetweenTask * 2, token);

                // Continue with latch operation
                //Debug.WriteLine("Starting latch operation");
                //bMotionDone = Latch(token);
                //if (!bMotionDone)
                //{
                //    m_status[3] = (char)Operation.Stopping;
                //    _errorMessage = "Latch operation failed";
                //    Debug.WriteLine("Error: Latch operation failed");
                //    return false;
                //}

                //await Task.Delay(DelayBetweenTask, token);

                // Unclamp operation
                Debug.WriteLine("Starting unclamp operation");
                bMotionDone = Unclamp(token);
                if (!bMotionDone)
                {
                    m_status[3] = (char)Operation.Stopping;
                    _errorMessage = "Unclamp operation failed";
                    Debug.WriteLine("Error: Unclamp operation failed");
                    return false;
                }

                // Operations complete - update status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = (char)LoadStatus.HomePosition;
                Debug.WriteLine("UnloadingMapping operation completed successfully (using bottom-to-top scanning)");
                return true;
            }
            catch (OperationCanceledException)
            {
                m_status[3] = (char)Operation.Stopping;
                _errorMessage = "Operation was canceled";
                Debug.WriteLine("Operation was canceled");
                return false;
            }
            catch (Exception ex)
            {
                m_status[3] = (char)Operation.Stopping;
                _errorMessage = $"Error during UnloadingMapping: {ex.Message}";
                Debug.WriteLine($"Error during UnloadingMapping: {ex.Message}");
                return false;
            }
        }
        public void Mapping(CancellationToken token)
        {
            if (IsErrorExist())
            {
                return;
            }

            bool bMotionDone = false;

            m_status[3] = (char)Operation.Operating;
            m_status[17] = (char)MappingStatus.InProcess;

            // Step 1: Extend the mapping arms
            bMotionDone = MappingBackward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 2: Move elevator down to map
            bMotionDone = ElevatorDown(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return;
            }
            Thread.Sleep(DelayBetweenTask);

            // Step 3: Retract the mapping arms
            bMotionDone = MappingForward(token);
            if (!bMotionDone)
            {
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return;
            }

            m_status[3] = (char)Operation.Stopping;
            m_status[17] = (char)MappingStatus.Completed;
        }
        #endregion

        #region Type-Specific Sequence Operations
        public bool ExecuteAdaptorLoadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing Adaptor-specific load sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Example custom sequence for Adaptor type
                bool success = DoorForward(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Load: Door forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                // Additional step for Adaptor type
                success = VacuumOn(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Load: Vacuum on failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockForward(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Load: Dock forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Clamp(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Load: Clamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unlatch(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.LoadPosition : m_status[2];

                Debug.WriteLine("Adaptor Load: " + (success ? "Completed successfully" : "Failed at unlatch step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Adaptor Load sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public bool ExecuteAdaptorUnloadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing Adaptor-specific unload sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Custom unload sequence for Adaptor type
                bool success = Latch(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Unload: Latch failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unclamp(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Unload: Unclamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockBackward(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Unload: Dock backward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                // Extra step for Adaptor
                success = VacuumOff(token);
                if (!success)
                {
                    Debug.WriteLine("Adaptor Unload: Vacuum off failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DoorBackward(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.HomePosition : m_status[2];

                Debug.WriteLine("Adaptor Unload: " + (success ? "Completed successfully" : "Failed at door backward step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Adaptor Unload sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public async Task<bool> ExecuteAdaptorLoadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing Adaptor-specific load+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First perform Adaptor-specific load
                bool loadSuccess = ExecuteAdaptorLoadSequence(token);
                if (!loadSuccess)
                {
                    Debug.WriteLine("Adaptor load+mapping: Load sequence failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false; // Return false on failure
                }

                // Wait for load to complete
                await Task.Delay(DelayBetweenTask, token);

                // Then perform mapping with Adaptor-specific parameters
                await MappingOperation_UpToDown(token, settings);

                // Set success status
                m_status[3] = (char)Operation.Stopping;

                Debug.WriteLine("Adaptor load+mapping sequence completed successfully");
                return true; // Return true on success
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Adaptor load+mapping sequence was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false; // Return false on cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Adaptor load+mapping sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false; // Return false on exception
            }
        }
        public async Task<bool> ExecuteAdaptorUnloadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing Adaptor-specific unload+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First perform mapping
                await MappingOperation_UpToDown(token, settings);

                // Wait for mapping to complete
                await Task.Delay(DelayBetweenTask, token);

                // Then Adaptor-specific unload
                bool unloadSuccess = ExecuteAdaptorUnloadSequence(token);
                if (!unloadSuccess)
                {
                    Debug.WriteLine("Adaptor unload+mapping: Unload sequence failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Set success status
                m_status[3] = (char)Operation.Stopping;

                Debug.WriteLine("Adaptor unload+mapping sequence completed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Adaptor unload+mapping sequence was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Adaptor unload+mapping sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public bool ExecuteFOSBLoadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing FOSB-specific load sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Example custom sequence for FOSB type
                bool success = DoorForward(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Load: Door forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                // FOSB might have a different step order
                success = Clamp(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Load: Clamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockForward(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Load: Dock forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unlatch(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.LoadPosition : m_status[2];

                Debug.WriteLine("FOSB Load: " + (success ? "Completed successfully" : "Failed at unlatch step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during FOSB Load sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public bool ExecuteFOSBUnloadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing FOSB-specific unload sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Custom unload sequence for FOSB type
                bool success = Latch(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Unload: Latch failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unclamp(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Unload: Unclamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockBackward(token);
                if (!success)
                {
                    Debug.WriteLine("FOSB Unload: Dock backward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DoorBackward(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.HomePosition : m_status[2];

                Debug.WriteLine("FOSB Unload: " + (success ? "Completed successfully" : "Failed at door backward step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during FOSB Unload sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public async Task<bool> ExecuteFOSBLoadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing FOSB-specific load+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First perform FOSB-specific load
                bool loadSuccess = ExecuteFOSBLoadSequence(token);
                if (!loadSuccess)
                {
                    Debug.WriteLine("FOSB load+mapping: Load sequence failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Wait for load to complete
                await Task.Delay(DelayBetweenTask, token);

                // Then perform mapping with FOSB-specific parameters
                await MappingOperation_UpToDown(token, settings);

                // Set success status
                m_status[3] = (char)Operation.Stopping;

                Debug.WriteLine("FOSB load+mapping sequence completed successfully");
                return true; // Return true on successful completion
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("FOSB load+mapping sequence was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false; // Return false on cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during FOSB load+mapping sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false; // Return false on exception
            }
        }
        public async Task<bool> ExecuteFOSBUnloadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing FOSB-specific unload+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First perform mapping
                await MappingOperation_UpToDown(token, settings);

                // Wait for mapping to complete
                await Task.Delay(DelayBetweenTask, token);

                // Then FOSB-specific unload
                bool unloadSuccess = ExecuteFOSBUnloadSequence(token);
                if (!unloadSuccess)
                {
                    Debug.WriteLine("FOSB unload+mapping: Unload sequence failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Set success status
                m_status[3] = (char)Operation.Stopping;

                Debug.WriteLine("FOSB unload+mapping sequence completed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("FOSB unload+mapping sequence was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during FOSB unload+mapping sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public bool ExecuteN2PurgeLoadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing N2PURGE-specific load sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // Example custom sequence for N2PURGE type
                bool success = DoorForward(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Load: Door forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockForward(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Load: Dock forward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                // N2PURGE needs vacuum before clamping
                success = VacuumOn(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Load: Vacuum on failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Clamp(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Load: Clamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unlatch(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.LoadPosition : m_status[2];

                Debug.WriteLine("N2PURGE Load: " + (success ? "Completed successfully" : "Failed at unlatch step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during N2PURGE Load sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public bool ExecuteN2PurgeUnloadSequence(CancellationToken token)
        {
            Debug.WriteLine("Executing N2PURGE-specific unload sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First turn off vacuum for N2PURGE
                bool success = VacuumOff(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Unload: Vacuum off failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Latch(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Unload: Latch failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = Unclamp(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Unload: Unclamp failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DockBackward(token);
                if (!success)
                {
                    Debug.WriteLine("N2PURGE Unload: Dock backward failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                Thread.Sleep(DelayBetweenTask);

                success = DoorBackward(token);

                // Set success status
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = success ? (char)LoadStatus.HomePosition : m_status[2];

                Debug.WriteLine("N2PURGE Unload: " + (success ? "Completed successfully" : "Failed at door backward step"));
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during N2PURGE Unload sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public async Task<bool> ExecuteN2PurgeLoadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            Debug.WriteLine("Executing N2PURGE-specific load+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // First perform N2PURGE-specific load
                bool loadSuccess = ExecuteN2PurgeLoadSequence(token);
                if (!loadSuccess)
                {
                    Debug.WriteLine("N2PURGE load+mapping: Load sequence failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Wait for load to complete
                await Task.Delay(DelayBetweenTask, token);

                // Then perform mapping with N2PURGE-specific parameters
                await MappingOperation_UpToDown(token, settings);

                // Set success status
                m_status[3] = (char)Operation.Stopping;

                Debug.WriteLine("N2PURGE load+mapping sequence completed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("N2PURGE load+mapping sequence was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during N2PURGE load+mapping sequence: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        public async Task<bool> ExecuteN2PurgeUnloadMappingSequence(CancellationToken token, IMappingSettings settings)
        {
            if (IsErrorExist())
            {
                Debug.WriteLine("N2PURGE Unload+Mapping Error: Existing errors prevent operation");
                return false;
            }

            // Log start of operation
            Debug.WriteLine("Executing N2PURGE-specific unload+mapping sequence");

            try
            {
                // Set operation status
                m_status[3] = (char)Operation.Operating;

                // 1. First perform the mapping operation
                Debug.WriteLine("Starting mapping operation for N2PURGE...");
                await MappingOperation_UpToDown(token, settings);

                // 2. Wait for system to stabilize after mapping
                await Task.Delay(DelayBetweenTask, token);

                // 3. Check pod presence using protrusion sensor
                UpdateSensorStatus();
                if (_sensorStatus.StatusProtrusion != 1)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: No POD detected (protrusion sensor)");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // 4. Check pod is clamped before unlatching
                if (_sensorStatus.StatusClamp != 1)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Pod is not clamped");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // 5. Check current load status
                if (m_status[2] != (char)LoadStatus.LoadPosition)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Not in Load Position");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // 6. N2PURGE specific: First turn off vacuum before other operations
                Debug.WriteLine("N2PURGE: Turning off vacuum first...");
                bool vacuumOffSuccess = VacuumOff(token);
                if (!vacuumOffSuccess)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Failed to turn off vacuum");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                // 7. Perform latch operation 
                Debug.WriteLine("N2PURGE: Performing latch operation...");
                bool latchSuccess = Latch(token);
                if (!latchSuccess)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Latch operation failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                // 8. Perform unclamp operation
                Debug.WriteLine("N2PURGE: Performing unclamp operation...");
                bool unclampSuccess = Unclamp(token);
                if (!unclampSuccess)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Unclamp operation failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                // 9. Retract dock
                Debug.WriteLine("N2PURGE: Retracting dock...");
                bool dockBackwardSuccess = DockBackward(token);
                if (!dockBackwardSuccess)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Dock retraction failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }
                await Task.Delay(DelayBetweenTask, token);

                // 10. Close door
                Debug.WriteLine("N2PURGE: Closing door...");
                bool doorBackwardSuccess = DoorBackward(token);
                if (!doorBackwardSuccess)
                {
                    Debug.WriteLine("N2PURGE Unload+Mapping Error: Door closing failed");
                    m_status[3] = (char)Operation.Stopping;
                    return false;
                }

                // Operations complete - update status
                Debug.WriteLine("N2PURGE unload with mapping sequence completed successfully");
                m_status[3] = (char)Operation.Stopping;
                m_status[2] = (char)LoadStatus.HomePosition;
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("N2PURGE Unload+Mapping: Operation was canceled");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during N2PURGE Unload+Mapping: {ex.Message}");
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
        }
        #endregion

        #region Unified Sequence Operations
        public async Task<bool> ExecuteUnifiedLoadMappingSequence(
            CancellationToken token,
            IMappingSettings settings,
            SequenceType sequenceType,
            OperationType operationType = OperationType.Load,
            IProgress<string> progress = null)
        {
            progress?.Report($"Starting {sequenceType} {operationType} sequence");

            if (!CanExecuteOperation($"{sequenceType} {operationType} Sequence"))
            {
                return false;
            }

            // **NEW: Check if already at load position**
            UpdateSensorStatus();
            char currentLoadStatus = m_status[2];

            if (currentLoadStatus == (char)LoadStatus.LoadPosition)
            {
                Debug.WriteLine("System is already at Load Position");
                _errorMessage = "System is already at Load Position. No action needed.";

                // Show acknowledgment to user via UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "The system is already at Load Position.\n\nNo loading sequence is required.",
                        "Already at Load Position",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                });

                return true; // Return success since we're already at the desired position
            }

            // **NEW: Create a linked cancellation token that can be cancelled by sensor monitoring**
            _sequenceCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var linkedToken = _sequenceCancellationTokenSource.Token;

            // Stop any existing monitoring
            StopContinuousDoorRetentionMonitoring();

            if (!ValidateSystemReady())
            {
                return false;
            }

            try
            {
                m_status[3] = (char)Operation.Operating;

                // Get and execute sequence steps
                var sequenceSteps = GetSequenceSteps(sequenceType, operationType);

                foreach (var step in sequenceSteps)
                {
                    if (step.IsRequired)
                    {
                        // **CRITICAL CHECK: Stop if sensor monitoring cancelled the sequence**
                        linkedToken.ThrowIfCancellationRequested();

                        progress?.Report($"Executing {step.Name}...");

                        if (!ValidateSystemReady(step.Name))
                        {
                            m_status[3] = (char)Operation.Stopping;
                            return false;
                        }

                        // **USE LINKED TOKEN: This will be cancelled if sensor monitoring detects an error**
                        bool success = step.Operation(linkedToken);
                        if (!success)
                        {
                            m_status[3] = (char)Operation.Stopping;
                            return false;
                        }

                        // **CRITICAL CHECK: Check again after each operation**
                        linkedToken.ThrowIfCancellationRequested();

                        //if (step.Name != "Door Retention Monitor")
                        //{
                        //    await Task.Delay(DelayBetweenTask, linkedToken);
                        //}
                    }
                }

                // Continue with mapping if needed...
                if (operationType == OperationType.Load)
                {
                    linkedToken.ThrowIfCancellationRequested();

                    if (!ValidateSystemReady("Mapping"))
                    {
                        m_status[3] = (char)Operation.Stopping;
                        return false;
                    }

                    progress?.Report("Performing mapping analysis...");
                    var analysisResult = await MappingOperation_UpToDown_WithAnalysis(linkedToken, settings);

                    if (!ValidateAnalysisResult(analysisResult))
                    {
                        m_status[3] = (char)Operation.Stopping;
                        return false;
                    }

                    LogAnalysisResults(analysisResult);
                    _lastMappingAnalysisResult = analysisResult;
                }

                // **NEW: Update status to Load Position after successful completion**
                m_status[2] = (char)LoadStatus.LoadPosition;
                m_status[3] = (char)Operation.Stopping;
                Debug.WriteLine("Unified Load Mapping sequence completed successfully - Status updated to Load Position");

                // Update sensor status to reflect the new state
                UpdateSensorStatus();

                progress?.Report($"{sequenceType} {operationType} sequence completed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Sequence was cancelled - likely due to sensor error");
                await SafelyDisableAllOutputs();
                StopContinuousDoorRetentionMonitoring();
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error during {sequenceType} {operationType}: {ex.Message}";
                await SafelyDisableAllOutputs();
                StopContinuousDoorRetentionMonitoring();
                m_status[3] = (char)Operation.Stopping;
                return false;
            }
            finally
            {
                // Clean up the cancellation token source
                _sequenceCancellationTokenSource?.Dispose();
                _sequenceCancellationTokenSource = null;
            }
        }
        public async Task<bool> ExecuteUnifiedUnloadMappingSequence(
            CancellationToken token,
            IMappingSettings settings,
            SequenceType sequenceType,
            OperationType operationType = OperationType.Unload,
            IProgress<string> progress = null)
        {
            progress?.Report($"Starting {sequenceType} {operationType} sequence (with mapping)");

            if (IsErrorExist())
            {
                Debug.WriteLine("Cannot execute operation due to existing errors");
                return false;
            }

            if (!CanExecuteOperation($"{sequenceType} {operationType} Sequence"))
            {
                return false;
            }

            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine("Error: Not all cards are connected");
                return false;
            }

            if (settings == null)
            {
                _errorMessage = "Settings object (IMappingSettings) is null.";
                Debug.WriteLine("Error: Settings object is null");
                return false;
            }

            double mmPerPulse = settings.MmPerPulse;
            if (mmPerPulse <= 0)
            {
                _errorMessage = "Invalid MmPerPulse setting (must be > 0).";
                Debug.WriteLine($"Error: Invalid MmPerPulse setting: {mmPerPulse}");
                return false;
            }

            try
            {
                m_status[3] = (char)Operation.Operating;


                // Get the sequence steps based on type and operation
                var sequenceSteps = GetSequenceSteps(sequenceType, operationType);

                // Execute the sequence steps
                foreach (var step in sequenceSteps)
                {
                    if (step.IsRequired)
                    {
                        progress?.Report($"Executing {step.Name}...");
                        Debug.WriteLine($"Executing {step.Name} operation...");

                        bool success = step.Operation(token);
                        if (!success)
                        {
                            m_status[3] = (char)Operation.Stopping;
                            Debug.WriteLine($"{step.Name} operation failed");
                            return false;
                        }

                        await Task.Delay(DelayBetweenTask, token);
                    }
                }

                // Always perform mapping operation during unload sequences (DownToUp)
                progress?.Report("Performing mapping analysis (DownToUp)...");
                Debug.WriteLine("Starting mapping operation (DownToUp)...");
                await MappingOperation_DownToUp_WithAnalysis(token, settings);

                // Optionally, you can analyze and store mapping results here if needed

                // Update status
                m_status[2] = (char)LoadStatus.HomePosition;
                m_status[3] = (char)Operation.Stopping;

                progress?.Report($"{sequenceType} {operationType} sequence (with mapping) completed successfully");
                Debug.WriteLine($"{sequenceType} {operationType} sequence (with mapping) completed successfully");

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"{sequenceType} {operationType} sequence was canceled");
                await SafelyDisableAllOutputs();
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error during {sequenceType} {operationType}: {ex.Message}";
                Debug.WriteLine($"{_errorMessage}\n{ex.StackTrace}");
                await SafelyDisableAllOutputs();
                m_status[3] = (char)Operation.Stopping;
                m_status[17] = (char)MappingStatus.Inexecution;
                return false;
            }
        }
        public async Task<FOUPCtrl.WaferMap.MappingAnalysisResult> ExecuteUnifiedMappingOperation(
            CancellationToken token,
            IMappingSettings settings,
            SequenceType sequenceType,
            OperationType operationType,
            IProgress<string> progress = null)
        {
            bool success;
            if (operationType == OperationType.Load)
            {
                success = await ExecuteUnifiedLoadMappingSequence(
                    token,
                    settings,
                    sequenceType,
                    operationType,
                    progress);
            }
            else // Unload
            {
                success = await ExecuteUnifiedUnloadMappingSequence(
                    token,
                    settings,
                    sequenceType,
                    operationType,
                    progress);
            }

            if (success)
            {
                if (operationType == OperationType.Load)
                {
                    var analysisResult = GetLastMappingAnalysisResult();
                    return analysisResult;
                }
                else
                {
                    // For Unload, mapping analysis may not be relevant
                    return null;
                }
            }
            else
            {
                // Optionally, you can throw or return a result with error info
                throw new InvalidOperationException(ErrorMessage);
            }
        }
        public List<SequenceStep> GetSequenceSteps(SequenceType sequenceType, OperationType operationType)
        {
            var steps = new List<SequenceStep>();

            switch (sequenceType)
            {
                case SequenceType.FOUP:
                    steps = operationType == OperationType.Load ? GetFOUPLoadSteps() : GetFOUPUnloadSteps();
                    break;
                case SequenceType.Adaptor:
                    steps = operationType == OperationType.Load ? GetAdaptorLoadSteps() : GetAdaptorUnloadSteps();
                    break;
                case SequenceType.FOSB:
                    steps = operationType == OperationType.Load ? GetFOSBLoadSteps() : GetFOSBUnloadSteps();
                    break;
                case SequenceType.N2Purge:
                    steps = operationType == OperationType.Load ? GetN2PurgeLoadSteps() : GetN2PurgeUnloadSteps();
                    break;
                default:
                    steps = operationType == OperationType.Load ? GetFOUPLoadSteps() : GetFOUPUnloadSteps();
                    break;
            }

            return steps;
        }
        private List<SequenceStep> GetFOUPLoadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "FOUP Mount Sensor Monitor", Operation = StartContinuousFOUPMountSensorMonitoring, IsRequired = true },
            //new SequenceStep { Name = "FOUP Mount Load Monitor", Operation = StartContinuousFOUPMountLoadMonitoring, IsRequired = true },
            //new SequenceStep { Name = "Air Pressure Monitor", Operation = StartContinuousAirPressureMonitoring, IsRequired = true },
            //new SequenceStep { Name = "FOUP Mount Sensor Monitor", Operation = StartContinuousFOUPMountSensorMonitoring, IsRequired = true },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            new SequenceStep { Name = "Dock Hand Pinch Monitor", Operation = StartContinuousDockHandPinchMonitoring, IsRequired = true },
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            //new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch },
            //new SequenceStep { Name = "FOUP Mount Sensor Monitor", Operation = StartContinuousFOUPMountSensorMonitoring, IsRequired = true },
            //new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Door Retention Monitor", Operation = StartContinuousDoorRetentionMonitoring, IsRequired = true },
            new SequenceStep { Name = "Wafer Protrusion Monitor", Operation = StartContinuousWaferProtrusionMonitoring, IsRequired = true },
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            //new SequenceStep { Name = "Wafer Protrusion Monitor", Operation = StartContinuousWaferProtrusionMonitoring, IsRequired = true },
            //Mapping sequence
            //new SequenceStep { Name = "Elevator Down", Operation = ElevatorDown }
        };
        private List<SequenceStep> GetFOUPUnloadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "Elevator Up", Operation = ElevatorUp },
            //new SequenceStep { Name = "Door Backward", Operation = DoorBackward },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch },
            //new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            //new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp }
        };
        private List<SequenceStep> GetAdaptorLoadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            //new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            //new SequenceStep { Name = "Clamp", Operation = Clamp },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch }
        };
        private List<SequenceStep> GetAdaptorUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };
        private List<SequenceStep> GetFOSBLoadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            //new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch }
        };
        private List<SequenceStep> GetFOSBUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };
        private List<SequenceStep> GetN2PurgeLoadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            new SequenceStep { Name = "Unlatch", Operation = Unlatch }
        };
        private List<SequenceStep> GetN2PurgeUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };
        private List<SequenceStep> GetOriginSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Door Retention Monitor", Operation = StartContinuousDoorRetentionMonitoring, IsRequired = true },
            new SequenceStep { Name = "Mapping Off", Operation = MappingForward },
            //new SequenceStep { Name = "Elevator Up", Operation = ElevatorUp },
            //new SequenceStep { Name = "Door Backward", Operation = DoorBackward },
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Stop Vacuum Monitoring", Operation = StopVacuumMonitoringForOrigin, IsRequired = true },
            new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp }
        };
        #endregion

        #region Cassette Status Pre-Checking
        private bool CheckCassetteStatusBeforeOrigin(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("=== Pre-checking cassette status before origin sequence ===");

                // Update sensor status first
                UpdateSensorStatus();

                // Check presence sensors (1,2,3)
                bool presence1And2 = _sensorStatus.StatusPresence1And2 == 1;
                bool presence3 = _sensorStatus.StatusPresence3 == 1;
                bool mainPresenceDetected = presence1And2 && presence3;

                // Check diagonal sensors
                bool diagonal1 = _sensorStatus.StatusPresenceDiagonal1 == 0; // 0 = detected
                bool diagonal2 = _sensorStatus.StatusPresenceDiagonal2 == 0; // 0 = detected
                bool diagonalPresenceDetected = diagonal1 || diagonal2;

                Debug.WriteLine($"Cassette Status Check:");
                Debug.WriteLine($"  - Main Presence (1,2,3): {(mainPresenceDetected ? "DETECTED" : "NOT DETECTED")}");
                Debug.WriteLine($"  - Diagonal Presence: {(diagonalPresenceDetected ? "DETECTED" : "NOT DETECTED")}");
                Debug.WriteLine($"  - Latch Status: {(_sensorStatus.StatusLatch == 1 ? "LATCHED" : "UNLATCHED")}");
                Debug.WriteLine($"  - Vacuum Status: {(_sensorStatus.StatusVacuum == 1 ? "ON" : "OFF")}");
                Debug.WriteLine($"  - Clamp Status: {(_sensorStatus.StatusClamp == 1 ? "CLAMPED" : "UNCLAMPED")}");

                // Determine cassette type and required actions
                if (mainPresenceDetected)
                {
                    Debug.WriteLine("Cassette detected - checking type and required actions...");

                    if (diagonalPresenceDetected)
                    {
                        Debug.WriteLine("12\" Cassette identified (diagonal sensors active)");

                        // For 12" cassette, check if latch/vacuum operations are needed
                        if (_sensorStatus.StatusUnlatch == 1 && _sensorStatus.StatusVacuum == 0)
                        {
                            Debug.WriteLine("12\" Cassette: Vacuum ON but not latched - will include latch in origin sequence");
                            return true; // Proceed with full origin sequence including latch
                        }
                        else if (_sensorStatus.StatusVacuum == 1)
                        {
                            Debug.WriteLine("12\" Cassette: Vacuum ON - will include vacuum off in origin sequence");
                            return true; // Proceed with origin sequence including vacuum off
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Smaller cassette identified (diagonal sensors inactive)");
                        Debug.WriteLine("Smaller cassette: May skip vacuum/latch operations if not required");
                        return true; // Proceed with origin sequence, but steps may be skipped based on sensor states
                    }
                }
                else
                {
                    Debug.WriteLine("No cassette detected - will focus on homing axes and basic origin steps");
                    return true; // Proceed with basic origin sequence (no cassette-specific steps needed)
                }

                Debug.WriteLine("Cassette status check completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cassette status pre-check: {ex.Message}");
                _errorMessage = $"Cassette status pre-check failed: {ex.Message}";
                return false;
            }
        }

        private bool PerformCassetteStatusPreCheck(CancellationToken token)
        {
            Debug.WriteLine("Performing cassette status pre-check before origin...");
            return CheckCassetteStatusBeforeOrigin(token);
        }
        #endregion

        #region Origin and System Operations
        public async Task<bool> ExecuteStartMotion(string motion, int sequenceType, IMappingSettings settings, CancellationToken token)
        {
            if (string.IsNullOrEmpty(motion))
            {
                _errorMessage = "Motion command is null or empty";
                return false;
            }

            if (!CanExecuteOperation($"ExecuteStartMotion: {motion}"))
            {
                return false;
            }

            Debug.WriteLine($"Executing {motion} with sequence type {sequenceType}");

            try
            {
                switch (motion)
                {
                    case "Load":
                        UpdateSensorStatus();
                        if (m_status[2] == (char)LoadStatus.LoadPosition)
                        {
                            Debug.WriteLine("System is already at Load Position");
                            _errorMessage = "System is already at Load Position. No action needed.";

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(
                                    "The system is already at Load Position.\n\nNo loading sequence is required.",
                                    "Already at Load Position",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information
                                );
                            });

                            return true;
                        }

                        bool loadSuccess = await ExecuteSequenceOperation(sequenceType, OperationType.Load, token);
                        if (loadSuccess)
                        {
                            // **ENHANCED: Verify we actually reached load position**
                            UpdateSensorStatus();
                            // Add your load position verification logic here if needed
                            m_status[2] = (char)LoadStatus.LoadPosition;
                            Debug.WriteLine("Load sequence completed successfully - Status updated to Load Position");
                            UpdateSensorStatus();
                        }
                        else
                        {
                            // **NEW: Set appropriate status on failure**
                            m_status[2] = (char)LoadStatus.Indefinite;
                            Debug.WriteLine("Load sequence failed - Status set to Indefinite");
                        }
                        return loadSuccess;

                    case "Unload":
                        UpdateSensorStatus();
                        if (m_status[2] == (char)LoadStatus.HomePosition)
                        {
                            Debug.WriteLine("System is already at Home Position");
                            _errorMessage = "System is already at Home Position. No action needed.";

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(
                                    "The system is already at Home Position.\n\nNo unloading sequence is required.",
                                    "Already at Home Position",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information
                                );
                            });

                            return true;
                        }

                        bool unloadSuccess = await ExecuteSequenceOperation(sequenceType, OperationType.Unload, token);
                        if (unloadSuccess)
                        {
                            // **ENHANCED: Verify we actually reached home position**
                            UpdateSensorStatus();
                            if (IsActuallyAtOriginPosition()) // Use the same check as origin
                            {
                                m_status[2] = (char)LoadStatus.HomePosition;
                                Debug.WriteLine("Unload sequence completed successfully - Status updated to Home Position");
                            }
                            else
                            {
                                m_status[2] = (char)LoadStatus.Indefinite;
                                Debug.WriteLine("Unload sequence completed but not at home position - Status set to Indefinite");
                            }
                            UpdateSensorStatus();
                        }
                        else
                        {
                            // **NEW: Set appropriate status on failure**
                            m_status[2] = (char)LoadStatus.Indefinite;
                            Debug.WriteLine("Unload sequence failed - Status set to Indefinite");
                        }
                        return unloadSuccess;

                    case "Load (map)":
                        bool loadMapSuccess = await ExecuteUnifiedLoadMappingSequence(
                            token,
                            settings,
                            (SequenceType)sequenceType,
                            OperationType.Load);
                        return loadMapSuccess;

                    case "Unload (map)":
                        bool unloadMapSuccess = await ExecuteUnifiedUnloadMappingSequence(
                            token,
                            settings,
                            (SequenceType)sequenceType,
                            OperationType.Unload);
                        return unloadMapSuccess;

                    case "MAP ACAL":
                        var result = await MappingAutoCalibration(token, settings);
                        return result.Success;

                    default:
                        _errorMessage = $"Unknown motion command: {motion}";
                        Debug.WriteLine(_errorMessage);
                        return false;
                }
            }
            catch (OperationCanceledException)
            {
                _errorMessage = $"{motion} operation was cancelled";
                m_status[2] = (char)LoadStatus.Indefinite; // **NEW: Clear status on cancellation**
                Debug.WriteLine(_errorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error during {motion} operation: {ex.Message}";
                m_status[2] = (char)LoadStatus.Indefinite; // **NEW: Clear status on error**
                Debug.WriteLine($"Error during {motion} operation: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExecuteSequenceOperation(int sequenceType, OperationType operationType, CancellationToken token)
        {
            var steps = GetSequenceSteps((SequenceType)sequenceType, operationType);

            foreach (var step in steps)
            {
                if (!step.Operation(token))
                {
                    _errorMessage = $"{operationType} sequence failed at step: {step.Name}";
                    return false;
                }

                // Add delay between steps if needed
                //await Task.Delay(DelayBetweenTask, token);
            }

            return true;
        }
        public async Task<bool> ExecuteOriginCommand(CancellationToken token)
        {
            Debug.WriteLine("Starting origin sequence execution - Fast mode (no delays)");

            try
            {
                // **FIXED: Check actual sensor positions instead of just status**
                UpdateSensorStatus();

                // Check if we're actually at origin position by examining sensors
                bool actuallyAtOrigin = IsActuallyAtOriginPosition();

                char currentLoadStatus = m_status[2];

                if (currentLoadStatus == (char)LoadStatus.HomePosition && actuallyAtOrigin)
                {
                    Debug.WriteLine("System is confirmed at Home Position (origin) - both status and sensors agree");
                    _errorMessage = "System is already at Home Position (origin). No action needed.";

                    // Show acknowledgment to user via UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "The system is already at Home Position (origin).\n\nNo movement is required.",
                            "Already at Origin",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    });

                    return true;
                }

                // **NEW: If status says origin but sensors disagree, correct the status**
                if (currentLoadStatus == (char)LoadStatus.HomePosition && !actuallyAtOrigin)
                {
                    Debug.WriteLine("Status shows origin but sensors indicate otherwise - correcting status and proceeding with origin");
                    m_status[2] = (char)LoadStatus.Indefinite; // Clear incorrect status
                }

                // Continue with origin sequence...
                var originSteps = GetOriginSteps();
                Debug.WriteLine($"Origin sequence contains {originSteps.Count} steps");

                foreach (var step in originSteps)
                {
                    token.ThrowIfCancellationRequested();

                    Debug.WriteLine($"Executing origin step: {step.Name}");

                    bool stepResult = step.Operation(token);

                    if (!stepResult)
                    {
                        _errorMessage = $"Origin sequence failed at step: {step.Name}";
                        Debug.WriteLine(_errorMessage);

                        // **NEW: Don't set HomePosition status if origin failed**
                        m_status[2] = (char)LoadStatus.Indefinite;
                        return false;
                    }

                    Debug.WriteLine($"Origin step '{step.Name}' completed successfully");
                }

                // **ENHANCED: Verify we actually reached origin before setting status**
                UpdateSensorStatus();
                if (IsActuallyAtOriginPosition())
                {
                    m_status[2] = (char)LoadStatus.HomePosition;
                    Debug.WriteLine("Origin sequence completed successfully - Status updated to Home Position");
                }
                else
                {
                    m_status[2] = (char)LoadStatus.Indefinite;
                    Debug.WriteLine("Origin sequence completed but not at expected position - Status set to Indefinite");
                }

                UpdateSensorStatus();

                Debug.WriteLine("Origin sequence completed successfully - all steps executed");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Origin sequence was cancelled");
                _errorMessage = "Origin operation was cancelled";
                m_status[2] = (char)LoadStatus.Indefinite; // **NEW: Clear status on failure**
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Origin sequence failed with exception: {ex.Message}");
                _errorMessage = $"Origin operation failed: {ex.Message}";
                m_status[2] = (char)LoadStatus.Indefinite; // **NEW: Clear status on failure**
                return false;
            }
        }
        private bool IsActuallyAtOriginPosition()
        {
            try
            {
                UpdateSensorStatus();

                // Define what "origin position" means based on your system
                // Adjust these criteria based on your actual origin position requirements

                bool elevatorAtTop = _sensorStatus.StatusElevatorUp == 1;
                bool dockRetracted = _sensorStatus.StatusDockBackward == 1;
                bool mappingRetracted = _sensorStatus.StatusMappingForward == 1;
                bool doorClosed = _sensorStatus.StatusDoorBackward == 1; // Adjust if needed
                bool unclamped = _sensorStatus.StatusUnclamp == 1;
                bool vacuumOff = _sensorStatus.StatusVacuum == 0;

                // **IMPORTANT: Adjust these conditions based on your actual origin requirements**
                bool atOrigin = elevatorAtTop && dockRetracted && mappingRetracted && unclamped && vacuumOff;

                Debug.WriteLine($"Origin position check:");
                Debug.WriteLine($"  - Elevator at top: {elevatorAtTop}");
                Debug.WriteLine($"  - Dock retracted: {dockRetracted}");
                Debug.WriteLine($"  - Mapping retracted: {mappingRetracted}");
                Debug.WriteLine($"  - Door closed: {doorClosed}");
                Debug.WriteLine($"  - Unclamped: {unclamped}");
                Debug.WriteLine($"  - Vacuum off: {vacuumOff}");
                Debug.WriteLine($"  - Overall at origin: {atOrigin}");

                return atOrigin;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking origin position: {ex.Message}");
                return false; // Conservative approach - if we can't check, assume not at origin
            }
        }
        public void ForceClose(CancellationTokenSource cts)
        {
            cts?.Cancel();

            if (ConnectionIOCard1)
            {
                // Turn off all outputs on card 1
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                DigitalWrite(_credenIOCard1, 3, (byte)0);
            }

            if (ConnectionIOCard2)
            {
                // Turn off all outputs on card 2
                DigitalWrite(_credenIOCard2, 2, (byte)0);
                DigitalWrite(_credenIOCard2, 3, (byte)0);
            }

            UpdateSensorStatus();
        }
        #endregion

        #region Status and Information
        public string GetStatus()
        {
            UpdateSensorStatus();
            return string.Concat(m_status);
        }
        public string GetStatus1()
        {
            UpdateSensorStatus();
            string temp = string.Concat(m_status);
            return temp.Substring(0, 10);
        }
        public string GetStatus2()
        {
            UpdateSensorStatus();
            string temp = string.Concat(m_status);
            return temp.Substring(10, 10);
        }
        public string GetStatusCode()
        {
            if (sErrorCode != "00")
                sStatusCode = "05";
            else
                sStatusCode = "00";
            return sStatusCode;
        }
        private FOUPCtrl.WaferMap.MappingAnalysisResult _lastMappingAnalysisResult;
        public FOUPCtrl.WaferMap.MappingAnalysisResult GetLastMappingAnalysisResult()
        {
            return _lastMappingAnalysisResult;
        }
        private string GetSlotStatusText(int status)
        {
            switch (status)
            {
                case 0: return "Empty";
                case 1: return "Normal";
                case 2: return "Crossed";
                case 3: return "Thick";
                case 4: return "Thin";
                case 5: return "Position Error";
                case 99: return "Error";
                default: return $"Unknown ({status})";
            }
        }
        #endregion

        #region Validation and Error Management
        private bool ValidateSystemReady(string operationName = "Operation")
        {
            // **CRITICAL: ONLY allow Reset operations when there are errors**
            if (IsErrorExist())
            {
                // Only allow Reset Error operation when errors exist
                if (operationName.ToUpper().Contains("RESET") || operationName.ToUpper().Contains("RSET"))
                {
                    Debug.WriteLine($"{operationName}: Allowing reset operation despite existing errors");

                    // For reset operations, only check hardware connections
                    if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
                    {
                        _errorMessage = "Not all cards are connected.";
                        Debug.WriteLine($"{operationName} blocked: Hardware not connected");
                        return false;
                    }
                    return true;
                }
                else
                {
                    // Block ALL other operations if any error exists
                    string errorInfo = !string.IsNullOrEmpty(_errorMessage) ? _errorMessage : $"Error Code: {sErrorCode}";
                    _errorMessage = $"Cannot execute {operationName}: System has errors that must be cleared first. {errorInfo}";
                    Debug.WriteLine($"OPERATION BLOCKED: {_errorMessage}");
                    return false;
                }
            }

            // Standard validation for when no errors exist
            if (!ConnectionIOCard1 || !ConnectionIOCard2 || !ConnectionAxisCard)
            {
                _errorMessage = "Not all cards are connected.";
                Debug.WriteLine($"{operationName} blocked: Hardware not connected");
                return false;
            }

            try
            {
                UpdateSensorStatus();
                CheckConflictingSensorStates();
                Debug.WriteLine($"{operationName}: System validation passed");
                return true;
            }
            catch (SensorErrorException ex)
            {
                Debug.WriteLine($"{operationName} blocked: {ex.Message}");
                _errorMessage = ex.Message;
                return false;
            }
        }
        private bool ValidateAnalysisResult(FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult)
        {
            if (analysisResult?.WaferStatus == null)
            {
                _errorMessage = "Analysis result or wafer status is null";
                return false;
            }

            for (int i = 0; i < analysisResult.WaferStatus.Length; i++)
            {
                if (analysisResult.WaferStatus[i] == 99) // Error status
                {
                    _errorMessage = $"Mapping analysis failed - error status detected in slot {i + 1}";
                    Debug.WriteLine($"Mapping analysis failed - error status detected in slot {i + 1}");
                    return false;
                }
            }

            return true;
        }
        private void LogAnalysisResults(FOUPCtrl.WaferMap.MappingAnalysisResult analysisResult)
        {
            Debug.WriteLine($"Mapping analysis completed: {analysisResult.DetectedWaferCount} wafers detected");

            int slotsToLog = Math.Min(analysisResult.ExpectedSlots, 5);
            for (int i = 0; i < slotsToLog; i++)
            {
                string statusText = GetSlotStatusText(analysisResult.WaferStatus[i]);
                Debug.WriteLine($"Slot {i + 1}: {statusText}, Thickness: {analysisResult.WaferThicknessMm[i]:F3}mm");
            }
        }
        public bool ResetError()
        {
            try
            {
                Debug.WriteLine("Resetting errors...");

                // Clear software flags
                sErrorCode = "00";
                sInterlockCode = "00";
                sStatusCode = "00";
                m_status[0] = (char)MachineStatus.Normal;
                m_status[4] = '0';
                m_status[5] = '0';

                // Clear hardware outputs if connected
                if (ConnectionIOCard1)
                {
                    DigitalWrite(_credenIOCard1, 2, (byte)0);
                    DigitalWrite(_credenIOCard1, 3, (byte)0);
                }
                if (ConnectionIOCard2)
                {
                    DigitalWrite(_credenIOCard2, 2, (byte)0);
                    DigitalWrite(_credenIOCard2, 3, (byte)0);
                }

                Thread.Sleep(100);

                // After clearing errors, validate system normally
                try
                {
                    UpdateSensorStatus();
                    CheckConflictingSensorStates();
                    //CheckASensorErrors();
                }
                catch (SensorErrorException ex)
                {
                    Debug.WriteLine($"Warning: Error detected immediately after reset: {ex.Message}");
                    // Don't fail the reset - let the error be detected on next operation
                }

                _errorMessage = string.Empty;
                Debug.WriteLine("Reset completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reset failed: {ex.Message}");
                _errorMessage = $"Reset failed: {ex.Message}";
                return false;
            }
        }
        private bool CanExecuteOperation(string operationName)
        {
            // Block ALL operations if there's any error (recoverable or unrecoverable)
            if (IsErrorExist())
            {
                string errorInfo = !string.IsNullOrEmpty(_errorMessage) ? _errorMessage : $"Error Code: {sErrorCode}";

                if (m_status[0] == (char)MachineStatus.RecoverableError)
                {
                    _errorMessage = $"Cannot execute {operationName}: System has a recoverable error that must be cleared first. Error: {errorInfo}";
                    Debug.WriteLine($"OPERATION BLOCKED: {_errorMessage}");
                    return false;
                }
                else if (m_status[0] == (char)MachineStatus.UnrecoverableError)
                {
                    _errorMessage = $"Cannot execute {operationName}: System has an unrecoverable error that must be reset first. Error: {errorInfo}";
                    Debug.WriteLine($"OPERATION BLOCKED: {_errorMessage}");
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region IO Monitoring and Control
        public bool PollIOStatus(int selectedCardIndex, List<IOBitStatus> inputBits, List<IOBitStatus> outputBits)
        {
            if (!ConnectionIOCard1 && !ConnectionIOCard2)
            {
                _errorMessage = "No IO cards are connected. Cannot poll I/O status.";
                return false;
            }

            try
            {
                byte cardId = (byte)(selectedCardIndex == 0 ? IOID1 : IOID2);
                IO1616Card selectedCard = selectedCardIndex == 0 ? _credenIOCard1 : _credenIOCard2;

                if (selectedCard == null)
                {
                    _errorMessage = $"Selected IO card {(selectedCardIndex == 0 ? 1 : 2)} is not initialized";
                    return false;
                }

                // Always clear input bits as they are refreshed completely
                inputBits.Clear();

                // Remove output bits only for the currently selected card to avoid duplicates
                var outputBitsToKeep = outputBits.Where(bit => bit.ID != cardId).ToList();
                outputBits.Clear();
                foreach (var bit in outputBitsToKeep)
                {
                    outputBits.Add(bit);
                }

                if (selectedCardIndex == 0) // Card 1
                {
                    PopulateCard1IOBits(cardId, selectedCard, inputBits, outputBits);
                }
                else // Card 2
                {
                    PopulateCard2IOBits(cardId, selectedCard, inputBits, outputBits);
                }

                Debug.WriteLine($"Successfully polled IO Card {(selectedCardIndex == 0 ? 1 : 2)} (ID: {cardId}) on COM port");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error polling I/O status for Card {(selectedCardIndex == 0 ? 1 : 2)}: {ex.Message}";
                Debug.WriteLine($"Error polling I/O status: {ex.Message}");
                return false;
            }
        }
        private void PopulateCard1IOBits(byte cardId, IO1616Card selectedCard, List<IOBitStatus> inputBits, List<IOBitStatus> outputBits)
        {
            string driver = $"CredenIODriver[{cardId}][{IOComPort1 ?? "COM4"}]";

            // Card 1 inputs - always read current state
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "CLAMP LIMIT SENSOR", IsOn = ReadBit(selectedCard, 0) == 1, Driver = driver, Port = 0 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "UNCLAMP LIMIT SENSOR", IsOn = ReadBit(selectedCard, 1) == 1, Driver = driver, Port = 1 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "PRESENCE SENSOR 1&2 (R&L)", IsOn = ReadBit(selectedCard, 2) == 1, Driver = driver, Port = 2 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "PRESENCE SENSOR 3", IsOn = ReadBit(selectedCard, 3) == 1, Driver = driver, Port = 3 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "DOCK HEAD PINCH SENSOR", IsOn = ReadBit(selectedCard, 4) == 1, Driver = driver, Port = 4 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "-", IsOn = ReadBit(selectedCard, 5) == 1, Driver = driver, Port = 5 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "ELEVATOR UPPER LIMIT", IsOn = ReadBit(selectedCard, 6) == 1, Driver = driver, Port = 6 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "PROTUSION PAIR SENSOR", IsOn = ReadBit(selectedCard, 7) == 1, Driver = driver, Port = 7 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "VACUMM SENSOR", IsOn = ReadBit(selectedCard, 8) == 1, Driver = driver, Port = 8 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "-", IsOn = ReadBit(selectedCard, 9) == 1, Driver = driver, Port = 9 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "-", IsOn = ReadBit(selectedCard, 10) == 1, Driver = driver, Port = 10 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "DOCK FORWARD LIMIT", IsOn = ReadBit(selectedCard, 11) == 1, Driver = driver, Port = 11 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "DOCK BACKWARD LIMIT", IsOn = ReadBit(selectedCard, 12) == 1, Driver = driver, Port = 12 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "PRESENCE DIAGONAL 1 (R)", IsOn = ReadBit(selectedCard, 13) == 1, Driver = driver, Port = 13 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "PRESENCE DIAGONAL 2 (L)", IsOn = ReadBit(selectedCard, 14) == 1, Driver = driver, Port = 14 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "-", IsOn = ReadBit(selectedCard, 15) == 1, Driver = driver, Port = 15 });

            // Card 1 outputs - read current hardware state
            try
            {
                byte outputPort2 = 0;
                byte outputPort3 = 0;
                selectedCard.ReadPort(2, ref outputPort2);  // Read output port 2 (bits 0-7)
                selectedCard.ReadPort(3, ref outputPort3);  // Read output port 3 (bits 8-15)

                // Add output bits with their current hardware state
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "VACUMM VALVE 1A", IsOn = (outputPort2 & 0x01) != 0, Driver = driver, Port = 0 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "VACUMM VALVE 1B", IsOn = (outputPort2 & 0x02) != 0, Driver = driver, Port = 1 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "VALVE 2A (EXHAUST UP)", IsOn = (outputPort2 & 0x04) != 0, Driver = driver, Port = 2 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "VALVE 2B (DOWN)", IsOn = (outputPort2 & 0x08) != 0, Driver = driver, Port = 3 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "VALVE 3A (EXHAUST DOWN)", IsOn = (outputPort2 & 0x10) != 0, Driver = driver, Port = 4 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "VALVE 3B (UP)", IsOn = (outputPort2 & 0x20) != 0, Driver = driver, Port = 5 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "UNCLAMP", IsOn = (outputPort2 & 0x40) != 0, Driver = driver, Port = 6 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "CLAMP", IsOn = (outputPort2 & 0x80) != 0, Driver = driver, Port = 7 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "DOCK SLIDE BACKWARD", IsOn = (outputPort3 & 0x01) != 0, Driver = driver, Port = 8 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "DOCK SLIDE FORWARD", IsOn = (outputPort3 & 0x02) != 0, Driver = driver, Port = 9 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "DOOR BACKWARD", IsOn = (outputPort3 & 0x04) != 0, Driver = driver, Port = 10 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "DOOR FORWARD", IsOn = (outputPort3 & 0x08) != 0, Driver = driver, Port = 11 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "LATCH", IsOn = (outputPort3 & 0x10) != 0, Driver = driver, Port = 12 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "UNLATCH", IsOn = (outputPort3 & 0x20) != 0, Driver = driver, Port = 13 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "MAPPING FORWARD", IsOn = (outputPort3 & 0x40) != 0, Driver = driver, Port = 14 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "MAPPING BACKWARD", IsOn = (outputPort3 & 0x80) != 0, Driver = driver, Port = 15 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read Card 1 output port states: {ex.Message}");
                AddDefaultCard1OutputBits(cardId, driver, outputBits);
            }
        }
        private void PopulateCard2IOBits(byte cardId, IO1616Card selectedCard, List<IOBitStatus> inputBits, List<IOBitStatus> outputBits)
        {
            string driver = $"CredenIODriver[{cardId}][{IOComPort2 ?? "COM4"}]";

            // Card 2 inputs - always read current state
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "E STOP 1", IsOn = ReadBit(selectedCard, 0) == 1, Driver = driver, Port = 0 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "E STOP 2", IsOn = ReadBit(selectedCard, 1) == 1, Driver = driver, Port = 1 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "MAINTENANCE MODE / SWITCH", IsOn = ReadBit(selectedCard, 2) == 1, Driver = driver, Port = 2 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "PRESSURE SENSOR", IsOn = ReadBit(selectedCard, 3) == 1, Driver = driver, Port = 3 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "ELEVATOR LOWER LIMIT", IsOn = ReadBit(selectedCard, 4) == 1, Driver = driver, Port = 4 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "-", IsOn = ReadBit(selectedCard, 5) == 1, Driver = driver, Port = 5 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "LATCH LIMIT", IsOn = ReadBit(selectedCard, 6) == 1, Driver = driver, Port = 6 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "UNLATCH LIMIT", IsOn = ReadBit(selectedCard, 7) == 1, Driver = driver, Port = 7 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "-", IsOn = ReadBit(selectedCard, 8) == 1, Driver = driver, Port = 8 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "-", IsOn = ReadBit(selectedCard, 9) == 1, Driver = driver, Port = 9 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "DOOR FORWARD LIMIT", IsOn = ReadBit(selectedCard, 10) == 1, Driver = driver, Port = 10 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "DOOR BACKWARD LIMIT", IsOn = ReadBit(selectedCard, 11) == 1, Driver = driver, Port = 11 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "MAPPING FORWARD LIMIT", IsOn = ReadBit(selectedCard, 12) == 1, Driver = driver, Port = 12 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "MAPPING BACKWARD LIMIT", IsOn = ReadBit(selectedCard, 13) == 1, Driver = driver, Port = 13 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "MAPPING AMPLIFIER 1", IsOn = ReadBit(selectedCard, 14) == 1, Driver = driver, Port = 14 });
            inputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "MAPPING AMPLIFIER 2", IsOn = ReadBit(selectedCard, 15) == 1, Driver = driver, Port = 15 });

            // Card 2 outputs - read current hardware state
            try
            {
                byte outputPort2 = 0;
                byte outputPort3 = 0;
                selectedCard.ReadPort(2, ref outputPort2);  // Read output port 2 (bits 0-7)
                selectedCard.ReadPort(3, ref outputPort3);  // Read output port 3 (bits 8-15)

                // Add output bits with their current hardware state
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "LED - PRESENCE", IsOn = (outputPort2 & 0x01) != 0, Driver = driver, Port = 0 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "LED - PLACEMENT", IsOn = (outputPort2 & 0x02) != 0, Driver = driver, Port = 1 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "LED - STATUS 1", IsOn = (outputPort2 & 0x04) != 0, Driver = driver, Port = 2 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "LED - STATUS 2", IsOn = (outputPort2 & 0x08) != 0, Driver = driver, Port = 3 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "LED - LOAD", IsOn = (outputPort2 & 0x10) != 0, Driver = driver, Port = 4 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "LED - UNLOAD", IsOn = (outputPort2 & 0x20) != 0, Driver = driver, Port = 5 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "LED - ALARM", IsOn = (outputPort2 & 0x40) != 0, Driver = driver, Port = 6 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "-", IsOn = (outputPort2 & 0x80) != 0, Driver = driver, Port = 7 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "-", IsOn = (outputPort3 & 0x01) != 0, Driver = driver, Port = 8 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "-", IsOn = (outputPort3 & 0x02) != 0, Driver = driver, Port = 9 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "-", IsOn = (outputPort3 & 0x04) != 0, Driver = driver, Port = 10 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "-", IsOn = (outputPort3 & 0x08) != 0, Driver = driver, Port = 11 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "-", IsOn = (outputPort3 & 0x10) != 0, Driver = driver, Port = 12 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "-", IsOn = (outputPort3 & 0x20) != 0, Driver = driver, Port = 13 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "-", IsOn = (outputPort3 & 0x40) != 0, Driver = driver, Port = 14 });
                outputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "-", IsOn = (outputPort3 & 0x80) != 0, Driver = driver, Port = 15 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read Card 2 output port states: {ex.Message}");
                AddDefaultCard2OutputBits(cardId, driver, outputBits);
            }
        }
        public bool SetIOBit(int selectedCardIndex, int bitIndex, bool value)
        {
            try
            {
                byte cardId = (byte)(selectedCardIndex == 0 ? IOID1 : IOID2);
                IO1616Card selectedCard = selectedCardIndex == 0 ? _credenIOCard1 : _credenIOCard2;

                if (selectedCard != null)
                {
                    Debug.WriteLine($"Setting bit {bitIndex} {(value ? "ON" : "OFF")} for card {(selectedCardIndex == 0 ? 1 : 2)} (ID: {cardId})");

                    int portId = bitIndex < 8 ? 2 : 3;
                    int bitIndexInPort = bitIndex % 8;

                    byte currentValue = 0;
                    CardStatus readStatus = selectedCard.ReadPort((byte)portId, ref currentValue);

                    if (readStatus == CardStatus.Successful)
                    {
                        if (value)
                            currentValue |= (byte)(1 << bitIndexInPort);
                        else
                            currentValue &= (byte)~(1 << bitIndexInPort);

                        CardStatus writeStatus = selectedCard.WritePort((byte)portId, currentValue);

                        if (writeStatus == CardStatus.Successful)
                        {
                            Debug.WriteLine($"Successfully set bit {bitIndex} {(value ? "ON" : "OFF")}");
                            return true;
                        }
                        else
                        {
                            _errorMessage = $"Failed to write bit {bitIndex}. Write Status: {writeStatus}";
                            Debug.WriteLine(_errorMessage);
                            return false;
                        }
                    }
                    else
                    {
                        _errorMessage = $"Failed to read current port value for bit {bitIndex}. Read Status: {readStatus}";
                        Debug.WriteLine(_errorMessage);
                        return false;
                    }
                }
                else
                {
                    _errorMessage = $"Error: Selected IO card {(selectedCardIndex == 0 ? 1 : 2)} is not initialized";
                    Debug.WriteLine(_errorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Exception setting bit {bitIndex}: {ex.Message}";
                Debug.WriteLine($"Exception setting bit {bitIndex}: {ex.Message}");
                return false;
            }
        }
        private void AddDefaultCard1OutputBits(byte cardId, string driver, List<IOBitStatus> outputBits)
        {
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "VACUMM VALVE 1A", IsOn = false, Driver = driver, Port = 0 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "VACUMM VALVE 1B", IsOn = false, Driver = driver, Port = 1 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "VALVE 2A (EXHAUST UP)", IsOn = false, Driver = driver, Port = 2 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "VALVE 2B (DOWN)", IsOn = false, Driver = driver, Port = 3 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "VALVE 3A (EXHAUST DOWN)", IsOn = false, Driver = driver, Port = 4 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "VALVE 3B (UP)", IsOn = false, Driver = driver, Port = 5 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "UNCLAMP", IsOn = false, Driver = driver, Port = 6 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "CLAMP", IsOn = false, Driver = driver, Port = 7 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "DOCK SLIDE BACKWARD", IsOn = false, Driver = driver, Port = 8 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "DOCK SLIDE FORWARD", IsOn = false, Driver = driver, Port = 9 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "DOOR BACKWARD", IsOn = false, Driver = driver, Port = 10 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "DOOR FORWARD", IsOn = false, Driver = driver, Port = 11 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "LATCH", IsOn = false, Driver = driver, Port = 12 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "UNLATCH", IsOn = false, Driver = driver, Port = 13 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "MAPPING FORWARD", IsOn = false, Driver = driver, Port = 14 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "MAPPING BACKWARD", IsOn = false, Driver = driver, Port = 15 });
        }
        private void AddDefaultCard2OutputBits(byte cardId, string driver, List<IOBitStatus> outputBits)
        {
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 0, Command = "LED - PRESENCE", IsOn = false, Driver = driver, Port = 0 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 1, Command = "LED - PLACEMENT", IsOn = false, Driver = driver, Port = 1 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 2, Command = "LED - STATUS 1", IsOn = false, Driver = driver, Port = 2 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 3, Command = "LED - STATUS 2", IsOn = false, Driver = driver, Port = 3 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 4, Command = "LED - LOAD", IsOn = false, Driver = driver, Port = 4 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 5, Command = "LED - UNLOAD", IsOn = false, Driver = driver, Port = 5 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 6, Command = "LED - ALARM", IsOn = false, Driver = driver, Port = 6 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 7, Command = "-", IsOn = false, Driver = driver, Port = 7 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 8, Command = "-", IsOn = false, Driver = driver, Port = 8 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 9, Command = "-", IsOn = false, Driver = driver, Port = 9 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 10, Command = "-", IsOn = false, Driver = driver, Port = 10 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 11, Command = "-", IsOn = false, Driver = driver, Port = 11 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 12, Command = "-", IsOn = false, Driver = driver, Port = 12 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 13, Command = "-", IsOn = false, Driver = driver, Port = 13 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 14, Command = "-", IsOn = false, Driver = driver, Port = 14 });
            outputBits.Add(new IOBitStatus { ID = cardId, Bit = 15, Command = "-", IsOn = false, Driver = driver, Port = 15 });
        }
        #endregion

        #region Utility and Helper Methods
        private async Task SafelyDisableAllOutputs()
        {
            if (ConnectionIOCard1)
            {
                try
                {
                    DigitalWrite(_credenIOCard1, 2, (byte)0); // Turn off all outputs on port 2
                    DigitalWrite(_credenIOCard1, 3, (byte)0); // Turn off all outputs on port 3
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error turning off outputs on card 1: {ex.Message}");
                }
            }

            if (ConnectionIOCard2)
            {
                try
                {
                    DigitalWrite(_credenIOCard2, 2, (byte)0); // Turn off all outputs on port 2
                    DigitalWrite(_credenIOCard2, 3, (byte)0); // Turn off all outputs on port 3
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error turning off outputs on card 2: {ex.Message}");
                }
            }
        }
        private async Task RetractMappingArmAsync(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("Retracting mapping arm...");
                await Task.Delay(300, token); // Small delay before retracting

                byte writeByte = 0;
                writeByte = SetBit(writeByte, _outputList.MappingForward);
                int portId = _outputList.MappingForward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                int retractRetries = 0;
                while (retractRetries < 15 && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);
                    UpdateSensorStatus();
                    if (_sensorStatus.StatusMappingForward == 1)
                    {
                        Debug.WriteLine("Mapping arm retracted successfully.");
                        break;
                    }
                    retractRetries++;
                }

                // Turn off output regardless of status
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                if (_sensorStatus.StatusMappingForward != 1)
                {
                    Debug.WriteLine("WARNING: Mapping arm may not be fully retracted (Sensor StatusMappingForward not detected).");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retracting mapping arm: {ex.Message}");
                // Still attempt to turn off outputs even if there was an error
                int portId = _outputList.MappingForward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, (byte)0);
            }
        }
        #endregion

        #region Nested Classes
        public class IOBitStatus
        {
            public int ID { get; set; }
            public int Bit { get; set; }
            public string Command { get; set; }
            public bool IsOn { get; set; }
            public string Driver { get; set; }
            public int Port { get; set; }
            public int DelayMs { get; set; }
            public string Configuration { get; set; }
        }
        #endregion
    }
}