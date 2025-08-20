using Creden.Hardware.Cards;
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
        public string ErrorMessage
        {
            get { return _errorMessage; }
        }
        #region Constants and Fields
        private int clampTimeOver = 700;
        private int latchTimeOver = 1500;
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

        public bool ConnectionIOCard1 { get; private set; }
        public bool ConnectionIOCard2 { get; private set; }
        public byte IOID1 { get; set; }
        public byte IOID2 { get; set; }
        public string IOComPort1 { get; set; }
        public string IOComPort2 { get; set; }
        public bool ConnectionAxisCard { get; private set; }
        public byte AxisID { get; set; }
        public string AxisComPort { get; set; }

        // Sensor input bit positions
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
        private int VacuumSensorInputBit = 8; // Read from card2
        private int ProtrusionSensor = 7;   // Read from card1

        // Control output bit positions
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
        private int VacuumOutput = 0;       // Bit 0 on port 2 - activates vacuum

        #endregion

        #region Structures
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
            public int Vacuum { get; set; }
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

        // DataPoint class if not already defined elsewhere
        public class DataPoint
        {
            public long TimeMs { get; set; }
            public double Position { get; set; }
            public int SensorValue { get; set; }
            public double Velocity { get; set; }
        }

        #endregion

        #region Constructor and Destructor
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
            _outputList.Vacuum = VacuumOutput;

            InitializeStatus();

            _credenIOCard1 = new IO1616Card();
            _credenIOCard2 = new IO1616Card();
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

        #region Connection Methods
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

        // Modified Disconnect method to include axis card
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

        public bool IsErrorExist()
        {
            if (m_status[0] != (char)MachineStatus.Normal)
                return true;
            if (sErrorCode != "00")
                return true;

            return false;
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
        #endregion

        #region IO Functions
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

            // Read from card 1, port 0 (containing clamp, unclamp, elevator up sensors)
            DigitalRead(_credenIOCard1, 0, ref readByte);
            _sensorStatus.StatusClamp = (readByte & (1 << ClampSensor)) != 0 ? 1 : 0;
            _sensorStatus.StatusUnclamp = (readByte & (1 << UnclampSensor)) != 0 ? 1 : 0;
            _sensorStatus.StatusElevatorUp = (readByte & (1 << ElevatorUpperLimit)) != 0 ? 1 : 0;
            _sensorStatus.StatusProtrusion = (readByte & (1 << ProtrusionSensor)) != 0 ? 1 : 0;
            _sensorStatus.StatusVacuum = (readByte & (1 << VacuumSensorInputBit)) != 0 ? 1 : 0;

            // Read from card 1, port 1 (next 8 inputs: 8-15)
            DigitalRead(_credenIOCard1, 1, ref readByte);
            _sensorStatus.StatusDockForward = (readByte & (1 << (DockForwardLimit - 8))) != 0 ? 1 : 0;
            _sensorStatus.StatusDockBackward = (readByte & (1 << (DockBackwardLimit - 8))) != 0 ? 1 : 0;

            // Read from card 2, port 0 (first 8 inputs: 0-7)
            DigitalRead(_credenIOCard2, 0, ref readByte);
            _sensorStatus.StatusLatch = (readByte & (1 << LatchSensor)) != 0 ? 1 : 0;
            _sensorStatus.StatusUnlatch = (readByte & (1 << UnlatchSensor)) != 0 ? 1 : 0;
            _sensorStatus.StatusElevatorDown = (readByte & (1 << ElevatorLowerLimit)) != 0 ? 1 : 0;

            // Read from card 2, port 1 (next 8 inputs: 8-15)
            DigitalRead(_credenIOCard2, 1, ref readByte);
            _sensorStatus.StatusDoorForward = (readByte & (1 << (DoorForwardLimit - 8))) != 0 ? 1 : 0;
            _sensorStatus.StatusDoorBackward = (readByte & (1 << (DoorBackwardLimit - 8))) != 0 ? 1 : 0;
            _sensorStatus.StatusMappingForward = (readByte & (1 << (MappingForwardLimit - 8))) != 0 ? 1 : 0;
            _sensorStatus.StatusMappingBackward = (readByte & (1 << (MappingBackwardLimit - 8))) != 0 ? 1 : 0;

            // Debug output
            //Debug.WriteLine($"Clamp: {_sensorStatus.StatusClamp}, Unclamp: {_sensorStatus.StatusUnclamp}, Latch: {_sensorStatus.StatusLatch}, Unlatch: {_sensorStatus.StatusUnlatch}");

            // Update status arrays based on sensor readings
            if ((_sensorStatus.StatusClamp == 1) && (_sensorStatus.StatusUnclamp == 1))
            {
                m_status[7] = (char)ClampStatus.Indefinite;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                sErrorCode = ErrorCode.Error_Clamp_Sensor;
            }
            else
            {
                if (_sensorStatus.StatusClamp == 1)
                    m_status[7] = (char)ClampStatus.Close;
                else if (_sensorStatus.StatusUnclamp == 1)
                    m_status[7] = (char)ClampStatus.Open;
                else
                    m_status[7] = (char)ClampStatus.Indefinite;
            }

            if ((_sensorStatus.StatusLatch == 1) && (_sensorStatus.StatusUnlatch == 1))
            {
                m_status[8] = (char)LatchStatus.Indefinite;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                sErrorCode = ErrorCode.Error_Latch_Sensor;
            }
            else
            {
                if (_sensorStatus.StatusLatch == 1)
                    m_status[8] = (char)LatchStatus.Close;
                else if (_sensorStatus.StatusUnlatch == 1)
                    m_status[8] = (char)LatchStatus.Open;
                else
                    m_status[8] = (char)LatchStatus.Indefinite;
            }
        }

        #endregion

        // Enums for sequence and operation types
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

        // Sequence step definition
        public class SequenceStep
        {
            public string Name { get; set; }
            public Func<CancellationToken, bool> Operation { get; set; }
            public bool IsRequired { get; set; } = true;
        }

        #region Control Operations
        public bool Clamp(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            UpdateSensorStatus();

            if (_sensorStatus.StatusClamp == 0)
                writeByte = SetBit(writeByte, _outputList.Clamp);
            else
                writeByte = ClearBit(writeByte, _outputList.Clamp);

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.Clamp < 8 ? 2 : 3;

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
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                sErrorCode = ErrorCode.Error_Clamp_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool Unclamp(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            UpdateSensorStatus();

            if (_sensorStatus.StatusUnclamp == 0)
                writeByte = SetBit(writeByte, _outputList.Unclamp);
            else
                writeByte = ClearBit(writeByte, _outputList.Unclamp);

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.Unclamp < 8 ? 2 : 3;

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
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                sErrorCode = ErrorCode.Error_Unclamp_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool Latch(CancellationToken token)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Check for connection and get initial status
            UpdateSensorStatus();

            // First check if already at target position
            if (_sensorStatus.StatusLatch == 1)
            {
                Debug.WriteLine("Latch operation skipped - already latched");
                return true;  // Already latched, nothing to do
            }

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.Latch < 8 ? 2 : 3;
                byte writeByte = 0;

                // Set the latch bit
                writeByte = SetBit(writeByte, _outputList.Latch);

                // Turn on the actuator
                Debug.WriteLine($"Starting Latch operation - activating output on port {portId}");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                bool sensorActivated = false;

                // Begin monitoring loop with MORE FREQUENT sensor checking
                while (!sensorActivated && !token.IsCancellationRequested)
                {
                    // Check for timeout
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > latchTimeOver)
                    {
                        throw new TimeoutException("Latch Timeover");
                    }

                    // Update sensor readings MORE FREQUENTLY
                    UpdateSensorStatus();

                    // Check if sensor activated
                    if (_sensorStatus.StatusLatch == 1)
                    {
                        sensorActivated = true;
                        Debug.WriteLine($"Latch sensor activated at {elapsedMS}ms");
                        break;
                    }

                    // Small delay to prevent CPU thrashing while still checking frequently
                    Thread.Sleep(5);
                }

                // We got out of the loop - immediately turn off the output
                Debug.WriteLine("Immediately turning off latch output");
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                // Verify sensor is still active after turning off the output
                UpdateSensorStatus();
                if (_sensorStatus.StatusLatch != 1)
                {
                    Debug.WriteLine("WARNING: Latch sensor deactivated after turning off output!");
                }

                return sensorActivated;
            }
            catch (TimeoutException)
            {
                // Ensure outputs are disabled on timeout
                int portId = _outputList.Latch < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                sErrorCode = ErrorCode.Error_Latch_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                Debug.WriteLine("Latch operation timed out");
                return false;
            }
            catch (Exception ex)
            {
                // Ensure outputs are disabled on any exception
                int portId = _outputList.Latch < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                _errorMessage = ex.Message;
                Debug.WriteLine($"Latch operation failed: {ex.Message}");
                return false;
            }
        }

        public bool Unlatch(CancellationToken token)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Check for connection and get initial status
            UpdateSensorStatus();

            // First check if already at target position
            if (_sensorStatus.StatusUnlatch == 1)
            {
                Debug.WriteLine("Unlatch operation skipped - already unlatched");
                return true;  // Already unlatched, nothing to do
            }

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.Unlatch < 8 ? 2 : 3;
                byte writeByte = 0;

                // Set the unlatch bit
                writeByte = SetBit(writeByte, _outputList.Unlatch);

                // Turn on the actuator
                Debug.WriteLine($"Starting Unlatch operation - activating output on port {portId}");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                bool sensorActivated = false;

                // Begin monitoring loop with MORE FREQUENT sensor checking
                while (!sensorActivated && !token.IsCancellationRequested)
                {
                    // Check for timeout
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > latchTimeOver)
                    {
                        throw new TimeoutException("Unlatch Timeover");
                    }

                    // Update sensor readings MORE FREQUENTLY
                    UpdateSensorStatus();

                    // Check if sensor activated
                    if (_sensorStatus.StatusUnlatch == 1)
                    {
                        sensorActivated = true;
                        Debug.WriteLine($"Unlatch sensor activated at {elapsedMS}ms");
                        break;
                    }

                    // Small delay to prevent CPU thrashing while still checking frequently
                    Thread.Sleep(5);
                }

                // We got out of the loop - immediately turn off the output
                Debug.WriteLine("Immediately turning off unlatch output");
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                // Verify sensor is still active after turning off the output
                UpdateSensorStatus();
                if (_sensorStatus.StatusUnlatch != 1)
                {
                    Debug.WriteLine("WARNING: Unlatch sensor deactivated after turning off output!");
                }

                return sensorActivated;
            }
            catch (TimeoutException)
            {
                // Ensure outputs are disabled on timeout
                int portId = _outputList.Unlatch < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                sErrorCode = ErrorCode.Error_Unlatch_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                Debug.WriteLine("Unlatch operation timed out");
                return false;
            }
            catch (Exception ex)
            {
                // Ensure outputs are disabled on any exception
                int portId = _outputList.Unlatch < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, (byte)0);

                _errorMessage = ex.Message;
                Debug.WriteLine($"Unlatch operation failed: {ex.Message}");
                return false;
            }
        }



        // Elevator Up operation
        public bool ElevatorUp(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            UpdateSensorStatus();

            // Set the elevator up output bits using the helper method.
            writeByte = SetBit(writeByte, _outputList.ElevatorUp1);
            writeByte = SetBit(writeByte, _outputList.ElevatorUp2);

            try
            {
                // Determine port ID based on output bit (assumed both outputs are on the same port)
                int portId = _outputList.ElevatorUp1 < 8 ? 2 : 3;
                Debug.WriteLine($"Writing to port {portId} on card 1, setting Elevator Up bits to {writeByte}");
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusElevatorUp == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 10000)
                    {
                        throw new TimeoutException("Elevator Up Timeover");
                    }
                    UpdateSensorStatus();
                    //Debug.WriteLine($"ElevatorUp sensor status: {_sensorStatus.StatusElevatorUp}");
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.ElevatorUp1 < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Elevator_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.ElevatorUp1 < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }


        // Elevator Down operation
        public bool ElevatorDown(CancellationToken token)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Ensure mapping arm is in forward position
            UpdateSensorStatus();
            //if (_sensorStatus.StatusMappingForward != 1)
            //{
            //    _errorMessage = "Mapping arm not in forward position.";
            //    return false;
            //}

            byte writeByte = 0;
            // Set elevator down output bits using the helper method
            writeByte = SetBit(writeByte, _outputList.ElevatorDown1);
            writeByte = SetBit(writeByte, _outputList.ElevatorDown2);

            try
            {
                // Determine port ID based on first output bit
                int portId = _outputList.ElevatorDown1 < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusElevatorDown == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 6000)
                    {
                        throw new TimeoutException("Elevator Down Timeover");
                    }
                    UpdateSensorStatus();
                }

                // Turn off outputs
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.ElevatorDown1 < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Elevator_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.ElevatorDown1 < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }


        // Door Forward (Open) operation
        public bool DoorForward(CancellationToken token)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Ensure elevator is in up position
            UpdateSensorStatus();
            if (_sensorStatus.StatusElevatorUp != 1)
            {
                _errorMessage = "Elevator must be in the up position.";
                return false;
            }

            byte writeByte = 0;
            // Set door forward output bit if sensor is off
            if (_sensorStatus.StatusDoorForward == 0)
                writeByte = SetBit(writeByte, _outputList.DoorForward);
            else
                writeByte = ClearBit(writeByte, _outputList.DoorForward);

            try
            {
                // Determine port ID based on output bit value
                int portId = _outputList.DoorForward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDoorForward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 2000)
                    {
                        throw new TimeoutException("Door Forward Timeover");
                    }
                    UpdateSensorStatus();
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.DoorForward < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Door_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.DoorForward < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }


        // Door Backward (Close) operation
        public bool DoorBackward(CancellationToken token)
        {
            if (!ConnectionIOCard1 || !ConnectionIOCard2)
                return false;

            // Ensure elevator is in up position
            UpdateSensorStatus();
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

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.DoorBackward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDoorBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 2000)
                    {
                        throw new TimeoutException("Door Backward Timeover");
                    }
                    UpdateSensorStatus();
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.DoorBackward < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Door_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.DoorBackward < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        public bool DockForward(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            UpdateSensorStatus();

            if (_sensorStatus.StatusDockForward == 0)
                writeByte = SetBit(writeByte, _outputList.DockForward);
            else
                writeByte = ClearBit(writeByte, _outputList.DockForward);

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.DockForward < 8 ? 2 : 3;

                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDockForward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 2000)
                    {
                        throw new TimeoutException("Dock Forward Timeover");
                    }

                    UpdateSensorStatus();
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                sErrorCode = ErrorCode.Error_Dock_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Dock Backward (Retract) operation
        public bool DockBackward(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            UpdateSensorStatus();

            if (_sensorStatus.StatusDockBackward == 0)
                writeByte = SetBit(writeByte, _outputList.DockBackward);
            else
                writeByte = ClearBit(writeByte, _outputList.DockBackward);

            try
            {
                // Determine port ID based on output bit
                int portId = _outputList.DockBackward < 8 ? 2 : 3;

                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusDockBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 2000)
                    {
                        throw new TimeoutException("Dock Backward Timeover");
                    }

                    UpdateSensorStatus();
                }

                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                sErrorCode = ErrorCode.Error_Dock_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, 2, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Ultra-high-speed mapping operation optimized for maximum data collection rate
        /// Achieves sub-millisecond intervals by minimizing hardware calls and computations
        /// </summary>
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
                    string savePath = Path.Combine(documentsPath, "FOUP_Mapping_Data_HighSpeed");

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

        /// <summary>
        /// Performs a mapping operation by moving the elevator from the bottom position upward
        /// while collecting sensor data
        /// </summary>
        /// <param name="token">Cancellation token for the operation</param>
        /// <param name="settings">Mapping settings containing start/end positions and other parameters</param>
        /// <returns>Task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Optimized method for exporting raw mapping data to CSV with minimal processing
        /// </summary>
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


        /// <summary>
        /// Gets the current mapping data collection
        /// </summary>
        /// <returns>List of mapping data points</returns>
        public List<DataPoint> GetMappingData()
        {
            return _mappingData;
        }

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

        // Helper method to retract mapping arm after operation
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


        // Mapping Forward operation (actually retracts the mapping mechanism)
        public bool MappingForward(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            // Set the mapping forward bit into the writeByte.
            writeByte = SetBit(writeByte, _outputList.MappingForward);

            try
            {
                // Determine port ID based on the output bit number.
                int portId = _outputList.MappingForward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusMappingForward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 500)
                    {
                        throw new TimeoutException("Mapping Forward Timeover");
                    }
                    UpdateSensorStatus();
                }

                // Turn off mapping forward output
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.MappingForward < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Mapping_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.MappingForward < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }


        // Mapping Backward operation (actually extends the mapping mechanism)
        public bool MappingBackward(CancellationToken token)
        {
            if (!ConnectionIOCard1)
                return false;

            byte writeByte = 0;
            // Set the mapping backward bit into the writeByte.
            writeByte = SetBit(writeByte, _outputList.MappingBackward);

            try
            {
                // Determine port ID based on the output bit.
                int portId = _outputList.MappingBackward < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard1, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusMappingBackward == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 500)
                    {
                        throw new TimeoutException("Mapping Backward Timeover");
                    }
                    UpdateSensorStatus();
                }

                // Turn off mapping backward output
                DigitalWrite(_credenIOCard1, portId, (byte)0);
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard1, _outputList.MappingBackward < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Mapping_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard1, _outputList.MappingBackward < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }

        // Vacuum On operation
        public bool VacuumOn(CancellationToken token)
        {
            if (!ConnectionIOCard2)
                return false;

            byte writeByte = 0;
            // Set the vacuum output bit using the helper method.
            writeByte = SetBit(writeByte, _outputList.Vacuum);

            try
            {
                // Determine port ID based on the output bit.
                int portId = _outputList.Vacuum < 8 ? 2 : 3;
                DigitalWrite(_credenIOCard2, portId, writeByte);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusVacuum == 0)
                {
                    token.ThrowIfCancellationRequested();
                    long elapsedMS = stopwatch.ElapsedMilliseconds;
                    if (elapsedMS > 1500)
                    {
                        throw new TimeoutException("Vacuum On Timeover");
                    }
                    UpdateSensorStatus();
                }

                // For vacuum, we leave the output on after it is engaged.
                return true;
            }
            catch (TimeoutException)
            {
                DigitalWrite(_credenIOCard2, _outputList.Vacuum < 8 ? 2 : 3, (byte)0);
                sErrorCode = ErrorCode.Error_Vacuum_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                DigitalWrite(_credenIOCard2, _outputList.Vacuum < 8 ? 2 : 3, (byte)0);
                _errorMessage = ex.Message;
                return false;
            }
        }


        // Vacuum Off operation
        public bool VacuumOff(CancellationToken token)
        {
            if (!ConnectionIOCard2)
                return false;

            try
            {
                // Determine port ID based on the vacuum output bit.
                int portId = _outputList.Vacuum < 8 ? 2 : 3;
                // Clear the vacuum output.
                DigitalWrite(_credenIOCard2, portId, (byte)0);

                var stopwatch = Stopwatch.StartNew();
                while (_sensorStatus.StatusVacuum == 1)
                {
                    token.ThrowIfCancellationRequested();
                    UpdateSensorStatus();

                    if (stopwatch.ElapsedMilliseconds > 1500)
                    {
                        throw new TimeoutException("Vacuum Off Timeover");
                    }
                }

                return true;
            }
            catch (TimeoutException)
            {
                sErrorCode = ErrorCode.Error_Vacuum_Timeover;
                m_status[0] = (char)MachineStatus.UnrecoverableError;
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return false;
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

        #region Sequence Operations
        // LOCK
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

        // UNLK
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

        // Load sequence
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

        // Unified sequence executor
        public async Task<bool> ExecuteUnifiedLoadMappingSequence(
            CancellationToken token,
            IMappingSettings settings,
            SequenceType sequenceType,
            OperationType operationType = OperationType.Load,
            IProgress<string> progress = null)
        {
            progress?.Report($"Starting {sequenceType} {operationType} sequence");

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

                // Only perform mapping operation during load sequences
                if (operationType == OperationType.Load)
                {
                    progress?.Report("Performing mapping analysis...");
                    Debug.WriteLine("Starting mapping operation with analysis...");
                    var analysisResult = await MappingOperation_UpToDown_WithAnalysis(token, settings);

                    if (!ValidateAnalysisResult(analysisResult))
                    {
                        m_status[3] = (char)Operation.Stopping;
                        return false;
                    }

                    LogAnalysisResults(analysisResult);
                    _lastMappingAnalysisResult = analysisResult;
                }

                // Update status
                m_status[2] = (char)LoadStatus.LoadPosition;
                m_status[3] = (char)Operation.Stopping;

                progress?.Report($"{sequenceType} {operationType} sequence completed successfully");
                Debug.WriteLine($"{sequenceType} {operationType} sequence completed successfully");

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

        // Helper: Get sequence steps for each type/operation
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

        // Define the step lists for each sequence and operation
        private List<SequenceStep> GetFOUPLoadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            //new SequenceStep { Name = "Latch", Operation = Latch },
            //new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            //new SequenceStep { Name = "Elevator Down", Operation = ElevatorDown }
        };

        private List<SequenceStep> GetAdaptorLoadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            //new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            //new SequenceStep { Name = "Clamp", Operation = Clamp },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch }
        };

        private List<SequenceStep> GetFOSBLoadSteps() => new List<SequenceStep>
        {
            //new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            //new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            //new SequenceStep { Name = "Unlatch", Operation = Unlatch }
        };

        private List<SequenceStep> GetN2PurgeLoadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Door Forward", Operation = DoorForward },
            new SequenceStep { Name = "Dock Forward", Operation = DockForward },
            new SequenceStep { Name = "Vacuum On", Operation = VacuumOn },
            new SequenceStep { Name = "Clamp", Operation = Clamp },
            new SequenceStep { Name = "Unlatch", Operation = Unlatch }
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

        private List<SequenceStep> GetAdaptorUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };

        private List<SequenceStep> GetFOSBUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };

        private List<SequenceStep> GetN2PurgeUnloadSteps() => new List<SequenceStep>
        {
            new SequenceStep { Name = "Vacuum Off", Operation = VacuumOff },
            new SequenceStep { Name = "Latch", Operation = Latch },
            new SequenceStep { Name = "Unclamp", Operation = Unclamp },
            new SequenceStep { Name = "Dock Backward", Operation = DockBackward },
            new SequenceStep { Name = "Door Backward", Operation = DoorBackward }
        };

        // Helper: Validate mapping analysis result
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

        // Helper: Log mapping analysis results
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

        // Unload sequence
        // Unload sequence with revised order: elevator up, door close, unlatch, vacuum off, dock backward, unclamp
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


        // Helper method to find wafer edges in the mapping data
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

        // Mapping sequence
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

        #region Status Methods
        // STAS
        public string GetStatus()
        {
            UpdateSensorStatus();
            return string.Concat(m_status);
        }

        // STA1
        public string GetStatus1()
        {
            UpdateSensorStatus();
            string temp = string.Concat(m_status);
            return temp.Substring(0, 10);
        }

        // STA2
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

        // RSET
        public void ResetError()
        {
            sErrorCode = "00";
            sInterlockCode = "00";
            m_status[0] = (char)MachineStatus.Normal;
        }
        #endregion

        /// <summary>
        /// Executes the Adaptor-specific load sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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

        /// <summary>
        /// Executes the FOSB-specific load sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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

        /// <summary>
        /// Executes the N2PURGE-specific load sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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

        /// <summary>
        /// Executes the Adaptor-specific unload sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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

        /// <summary>
        /// Executes the FOSB-specific unload sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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

        /// <summary>
        /// Executes the N2PURGE-specific unload sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <returns>True if the sequence completed successfully, false otherwise</returns>
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



        /// <summary>
        /// Executes the FOUP-specific load with mapping sequence, performing load operations followed by mapping analysis
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task<bool> indicating success or failure of the operation</returns>
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

        private FOUPCtrl.WaferMap.MappingAnalysisResult _lastMappingAnalysisResult;

        /// <summary>
        /// Gets the last mapping analysis result from the most recent mapping operation
        /// </summary>
        /// <returns>The last mapping analysis result, or null if no mapping has been performed</returns>
        public FOUPCtrl.WaferMap.MappingAnalysisResult GetLastMappingAnalysisResult()
        {
            return _lastMappingAnalysisResult;
        }

        // Helper method to convert status codes to readable text
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

        /// <summary>
        /// Executes the Adaptor-specific load with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Executes the FOSB-specific load with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Executes the N2PURGE-specific load with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Executes the Adaptor-specific unload with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task<bool> indicating success or failure of the operation</returns>
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

        /// <summary>
        /// Executes the FOSB-specific unload with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task<bool> indicating success or failure of the operation</returns>
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

        /// <summary>
        /// Executes the N2PURGE-specific unload with mapping sequence
        /// </summary>
        /// <param name="token">Cancellation token for async operations</param>
        /// <param name="settings">Mapping settings to use</param>
        /// <returns>Task<bool> indicating success or failure of the operation</returns>
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

        /// <summary>
        /// Performs a mapping operation with real-time wafer slot analysis, calculating which slots have wafers
        /// and their status AND exports raw mapping data. This function performs the same mechanical sequence 
        /// as MappingOperation_UpToDown but analyzes data in real-time for monitor display and exports raw data.
        /// </summary>
        /// <param name="token">Cancellation token for the operation</param>
        /// <param name="settings">Mapping settings containing start/end positions and other parameters</param>
        /// <returns>MappingAnalysisResult containing calculated slot statuses and wafer information</returns>
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
    }
}