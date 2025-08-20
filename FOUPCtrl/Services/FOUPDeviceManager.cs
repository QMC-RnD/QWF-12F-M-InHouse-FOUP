using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FoupControl;
using FOUPCtrl.Hardware;

namespace FOUPCtrl.Services
{
    public class FOUPDeviceManager
    {
        private readonly FOUP_Ctrl _foupCtrl;
        private Task _statusPollingTask;
        private CancellationTokenSource _pollingCts;

        public FOUPDeviceManager(FOUP_Ctrl foupCtrl)
        {
            _foupCtrl = foupCtrl ?? throw new ArgumentNullException(nameof(foupCtrl));
            // FIX: Initialize _pollingCts in the constructor
            _pollingCts = new CancellationTokenSource();
        }

        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public async Task<bool> ConnectDevice(string deviceId1, string deviceId2, string comPort)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId1) || string.IsNullOrEmpty(deviceId2))
                {
                    return false;
                }

                // Cancel and await previous polling task if it exists
                if (_statusPollingTask != null)
                {
                    _pollingCts?.Cancel();
                    try
                    {
                        await _statusPollingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected, ignore
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DisconnectDevice] Exception while awaiting polling task: {ex.Message}");
                    }
                    _statusPollingTask = null;
                    _pollingCts?.Dispose();
                    // FIX: Always ensure _pollingCts is properly initialized
                    _pollingCts = new CancellationTokenSource();
                }
                else
                {
                    // FIX: Ensure _pollingCts is initialized even when there's no previous task
                    _pollingCts?.Dispose();
                    _pollingCts = new CancellationTokenSource();
                }

                // Set COM port and device IDs
                _foupCtrl.IOComPort1 = comPort;
                _foupCtrl.IOComPort2 = comPort;
                _foupCtrl.IOID1 = byte.Parse(deviceId1);
                _foupCtrl.IOID2 = byte.Parse(deviceId2);

                System.Diagnostics.Debug.WriteLine($"[Connect] Set COM ports to {comPort}");
                System.Diagnostics.Debug.WriteLine($"[Connect] Set Card IDs: Card 1 = {deviceId1}, Card 2 = {deviceId2}");

                bool connected = _foupCtrl.Connect();
                System.Diagnostics.Debug.WriteLine($"[Connect] Connection result: {(connected ? "SUCCESS" : "FAILED")}");

                if (connected)
                {
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                    {
                        IsConnected = true,
                        StatusMessage = "Connected successfully to devices."
                    });

                    // Start polling status - now _pollingCts is guaranteed to be non-null
                    System.Diagnostics.Debug.WriteLine("[Connect] Starting status polling...");
                    StartStatusPolling(_pollingCts.Token);
                    System.Diagnostics.Debug.WriteLine("[Connect] Connection sequence completed successfully.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Connect] Connection failed");
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                    {
                        IsConnected = false,
                        StatusMessage = "Connection failed."
                    });
                }

                return connected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Connect] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Connect] Stack trace: {ex.StackTrace}");
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                {
                    IsConnected = false,
                    StatusMessage = $"Error during connection: {ex.Message}"
                });
                return false;
            }
        }

        public async Task DisconnectDevice()
        {
            try
            {
                // Stop polling
                _pollingCts?.Cancel();

                // Wait for the polling task to finish
                if (_statusPollingTask != null)
                {
                    try
                    {
                        await _statusPollingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected, ignore
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Disconnect] Exception while awaiting polling task: {ex.Message}");
                    }
                    _statusPollingTask = null;
                }

                // Dispose and recreate the token source
                _pollingCts?.Dispose();
                _pollingCts = new CancellationTokenSource();

                // Disconnect hardware
                _foupCtrl?.Disconnect();

                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                {
                    IsConnected = false,
                    StatusMessage = "Disconnected."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Disconnect] EXCEPTION: {ex.Message}");
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                {
                    IsConnected = false,
                    StatusMessage = $"Disconnect error: {ex.Message}"
                });
            }
        }

        public void StartStatusPolling(CancellationToken token)
        {
            _statusPollingTask = Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var status = UpdateDeviceStatus();
                            if (status != null)
                            {
                                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs { Status = status });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error polling status: {ex.Message}");
                        }

                        try
                        {
                            Task.Delay(100, token).Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[StatusPolling] Exception: {ex.Message}");
                }
            }, token);
        }

        public DeviceStatus UpdateDeviceStatus()
        {
            if (_foupCtrl == null)
                return null;

            try
            {
                _foupCtrl.UpdateSensorStatus();

                var status = new DeviceStatus
                {
                    ClampStatus = GetClampStatus(),
                    LatchStatus = GetLatchStatus(),
                    ElevatorStatus = GetElevatorStatus(),
                    DoorStatus = GetDoorStatus(),
                    DockStatus = GetDockStatus(),
                    MappingStatus = GetMappingStatus(),
                    VacuumStatus = GetVacuumStatus(),
                    MachineStatus = GetMachineStatus()
                };

                return status;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating device status: {ex.Message}");
                return null;
            }
        }

        private string GetClampStatus()
        {
            if (_foupCtrl._sensorStatus.StatusClamp == 1)
                return "Clamped";
            else if (_foupCtrl._sensorStatus.StatusUnclamp == 1)
                return "Unclamped";
            else
                return "Undefined";
        }

        private string GetLatchStatus()
        {
            if (_foupCtrl._sensorStatus.StatusLatch == 1)
                return "Latched";
            else if (_foupCtrl._sensorStatus.StatusUnlatch == 1)
                return "Unlatched";
            else
                return "Undefined";
        }

        private string GetElevatorStatus()
        {
            if (_foupCtrl._sensorStatus.StatusElevatorUp == 1)
                return "Up";
            else if (_foupCtrl._sensorStatus.StatusElevatorDown == 1)
                return "Down";
            else
                return "Undefined";
        }

        private string GetDoorStatus()
        {
            if (_foupCtrl._sensorStatus.StatusDoorForward == 1)
                return "Open";
            else if (_foupCtrl._sensorStatus.StatusDoorBackward == 1)
                return "Closed";
            else
                return "Undefined";
        }

        private string GetDockStatus()
        {
            if (_foupCtrl._sensorStatus.StatusDockForward == 1)
                return "Extended";
            else if (_foupCtrl._sensorStatus.StatusDockBackward == 1)
                return "Retracted";
            else
                return "Undefined";
        }

        private string GetMappingStatus()
        {
            if (_foupCtrl._sensorStatus.StatusMappingForward == 1)
                return "Retracted";
            else if (_foupCtrl._sensorStatus.StatusMappingBackward == 1)
                return "Extended";
            else
                return "Undefined";
        }

        private string GetVacuumStatus()
        {
            if (_foupCtrl._sensorStatus.StatusVacuum == 1)
                return "On";
            else
                return "Off";
        }

        private string GetMachineStatus()
        {
            if (_foupCtrl.m_status != null && _foupCtrl.m_status.Length > 0)
            {
                char machineStatusChar = _foupCtrl.m_status[0];
                return ((MachineStatus)machineStatusChar).ToString();
            }
            return "Unknown";
        }
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string StatusMessage { get; set; }
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceStatus Status { get; set; }
    }

    public class DeviceStatus
    {
        public string ClampStatus { get; set; }
        public string LatchStatus { get; set; }
        public string ElevatorStatus { get; set; }
        public string DoorStatus { get; set; }
        public string DockStatus { get; set; }
        public string MappingStatus { get; set; }
        public string VacuumStatus { get; set; }
        public string MachineStatus { get; set; }
    }
}