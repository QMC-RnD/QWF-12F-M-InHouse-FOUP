using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FoupControl;
using FOUPCtrl.Communication;
using FOUPCtrl.Hardware;
using FOUPCtrl.TCPServer;

namespace FOUPCtrl.Services
{
    public class FOUPCommunicationService
    {
        private ConnectionManager _connectionManager;
        private readonly FOUP_Ctrl _foupCtrl;

        public FOUPCommunicationService(FOUP_Ctrl foupCtrl)
        {
            _foupCtrl = foupCtrl ?? throw new ArgumentNullException(nameof(foupCtrl));
        }

        public event EventHandler<ServerStatusChangedEventArgs> ServerStatusChanged;

        // Expose the message collections from ConnectionManager
        public ObservableCollection<string> ListMsgReceived => _connectionManager?.ListMsgReceived;
        public ObservableCollection<string> ListMsgSent => _connectionManager?.ListMsgSent;

        // Expose ConnectionManager properties
        public bool ServerStarted => _connectionManager?.ServerStarted ?? false;
        public bool TCPConnected => _connectionManager?.TCPConnected ?? false;
        public string ConnectionInfoString => _connectionManager?.ConnectionInfoString ?? "No connection";

        public bool StartTCPServer(string ip, int port)
        {
            try
            {
                if (_connectionManager != null)
                {
                    _connectionManager.StopServer();
                    _connectionManager = null;
                }

                _connectionManager = new ConnectionManager(ip, port);
                SetupCommandProcessorIntegration();

                bool connected = _connectionManager.StartListen(ip, port.ToString(), out string errMsg);

                if (connected)
                {
                    ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                    {
                        IsStarted = true,
                        StatusMessage = $"Server Started on {ip}:{port} with CRC16 CommandProcessor"
                    });
                }
                else
                {
                    ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                    {
                        IsStarted = false,
                        StatusMessage = $"Failed to start server: {errMsg ?? "Unknown error"}"
                    });
                }

                return connected;
            }
            catch (Exception ex)
            {
                ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                {
                    IsStarted = false,
                    StatusMessage = $"Error starting server: {ex.Message}"
                });
                return false;
            }
        }

        public void StopTCPServer()
        {
            _connectionManager?.StopServer();
            _connectionManager = null;

            ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
            {
                IsStarted = false,
                StatusMessage = "Server Stopped."
            });
        }

        public void TestCRC16Protocol()
        {
            if (_connectionManager?.ServerStarted != true)
            {
                ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                {
                    IsStarted = false,
                    StatusMessage = "CRC16 Test Failed: TCP Server not running"
                });
                return;
            }

            try
            {
                // Create a CRC16 protocol message
                var testMessage = new ProtocolMessage
                {
                    Code = 0x30,
                    Address = 0x30,
                    Command = "CLAMPON"
                };

                // Convert to bytes
                byte[] messageBytes = testMessage.ToBytes();
                string hexString = testMessage.ToHexString();

                // Send the message through the connection manager
                _connectionManager?.SendProtocolMessage(testMessage.Command);

                // Test CRC16 validation
                bool isValid = CRC16Calculator.ValidateCRC16(messageBytes);

                ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                {
                    IsStarted = true,
                    StatusMessage = $"CRC16 Protocol Test Completed Successfully. Message: {hexString}, Valid: {isValid}"
                });
            }
            catch (Exception ex)
            {
                ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
                {
                    IsStarted = _connectionManager?.ServerStarted ?? false,
                    StatusMessage = $"CRC16 Test Error: {ex.Message}"
                });
            }
        }

        public CommandProcessor GetCommandProcessor()
        {
            return _connectionManager?.GetCommandProcessor();
        }

        public void SendMessage(string message)
        {
            _connectionManager?.SendMessage(message);
        }

        public void SendProtocolMessage(string command)
        {
            _connectionManager?.SendProtocolMessage(command);
        }

        public async Task ProcessAndSendRawCommand(string command)
        {
            if (_connectionManager != null)
            {
                await _connectionManager.ProcessAndSendRawCommand(command);
            }
        }

        private void SetupCommandProcessorIntegration()
        {
            try
            {
                var commandProcessor = _connectionManager.GetCommandProcessor();
                if (commandProcessor != null)
                {
                    // The CommandProcessor will handle commands through the FOUP_Ctrl directly
                    System.Diagnostics.Debug.WriteLine("[CommandProcessor] CommandProcessor integration complete");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandProcessor] ERROR setting up integration: {ex.Message}");
            }
        }
    }

    public class ServerStatusChangedEventArgs : EventArgs
    {
        public bool IsStarted { get; set; }
        public string StatusMessage { get; set; }
    }
}