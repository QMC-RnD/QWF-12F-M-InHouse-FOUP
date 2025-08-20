using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using FOUPCtrl.Communication; // Add this for CRC16 support
using FoupControl; // Add this for FOUP_Ctrl
using FOUPCtrl.Hardware;

namespace FOUPCtrl.TCPServer
{
    public class ConnectionManager : INotifyPropertyChanged
    {
        private static NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        private TCPServer tcpServer;

        // Add CRC16 support
        private FOUP_Ctrl _foupCtrl;

        //external binding variables
        private ObservableCollection<string> listMsgSent = new ObservableCollection<string>();
        private ObservableCollection<string> listMsgReceived = new ObservableCollection<string>();
        private bool tcpConnected;
        private string connectionInfoString = "No connection";
        private System.Net.IPAddress hostIP;
        private int hostPort;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> MessageReceivedEventHandler;

        public void NotifyPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public CommandProcessor GetCommandProcessor()
        {
            return tcpServer?.GetCommandProcessor();
        }

        public ConnectionManager(string strIP = "127.0.0.1", int portNum = 8002)
        {
            hostIP = System.Net.IPAddress.Parse(strIP);
            hostPort = portNum;

            // Initialize FOUP controller for CRC16 support
            _foupCtrl = FOUPCtrl.Hardware.HardwareManager.FoupCtrl;

            tcpServer = new TCPServer(hostIP, hostPort);

            // Set up CRC16 support
            tcpServer.SetFOUPController(_foupCtrl);

            var protocolSettings = new ProtocolSettings
            {
                EnableCRC16Protocol = true,
                StationCode = 0x30,
                Address = 0x30,
                EnableLogging = true
            };
            tcpServer.SetProtocolSettings(protocolSettings);

            // Subscribe to response event to update UI with all sent responses
            tcpServer.ResponseSent += (s, responseText) =>
            {
                Application.Current.Dispatcher.Invoke(() => ListMsgSent.Add(responseText));
            };

            NotifyPropertyChange("IPAddress");
            NotifyPropertyChange("PortNumber");
        }

        //Used for external Binding
        public bool ServerStarted
        {
            get
            {
                return tcpServer.ServerStarted;
            }
        }

        //Used for external Binding
        public bool ServerStopped
        {
            get
            {
                return !tcpServer.ServerStarted;
            }
        }

        //Used for external Binding
        public ObservableCollection<string> ListMsgSent
        {
            get { return listMsgSent; }
            set { listMsgSent = value; }
        }

        //Used for external Binding
        public ObservableCollection<string> ListMsgReceived
        {
            get { return listMsgReceived; }
            set { listMsgReceived = value; }
        }

        //Used for external Binding
        public bool TCPConnected
        {
            get { return tcpConnected; }
        }

        //Used for external Binding
        public string ConnectionInfoString
        {
            get { return connectionInfoString; }
        }

        public string IPAddress
        {
            get { return hostIP.ToString(); }
        }

        public string PortNumber
        {
            get { return hostPort.ToString(); }
        }

        public bool StartListen(string ip, string port, out string errMsg)
        {
            errMsg = "";

            if (!tcpServer.ServerStarted)
            {
                System.Net.IPAddress tmphostIP;
                int tmphostPort = 8002;

                if (System.Net.IPAddress.TryParse(ip, out tmphostIP))
                {
                    hostIP = tmphostIP;
                }
                else
                {
                    errMsg = "Invalid IP Address";
                    return false;
                }
                if (Int32.TryParse(port, out tmphostPort))
                {
                    if (tmphostPort < 0 || tmphostPort > 65535)
                    {
                        errMsg = "Port number out of range";
                        return false;
                    }
                    hostPort = tmphostPort;
                }
                else
                {
                    errMsg = "Invalid Port number";
                    return false;
                }

                // Create new TCP server with CRC16 support
                tcpServer = new TCPServer(hostIP, hostPort);

                // Set up CRC16 support
                tcpServer.SetFOUPController(_foupCtrl);

                var protocolSettings = new ProtocolSettings
                {
                    EnableCRC16Protocol = true,
                    StationCode = 0x30,
                    Address = 0x30,
                    EnableLogging = true
                };
                tcpServer.SetProtocolSettings(protocolSettings);

                tcpServer.MessageReceived += new EventHandler<string>(MessageReceivedEvent);
                tcpServer.ClientDisconnected += tcpServer_ClientDisconnected;
                tcpServer.ClientConnectedEventHandler += new EventHandler<string>(ClientConnectedEvent);

                // Subscribe to response event to update UI with all sent responses
                tcpServer.ResponseSent += (s, responseText) =>
                {
                    Application.Current.Dispatcher.Invoke(() => ListMsgSent.Add(responseText));
                };

                if (!tcpServer.StartServer())
                {
                    errMsg = "Failed to start server.";
                    NotifyPropertyChange("ServerStarted");
                    NotifyPropertyChange("ServerStopped");

                    string logMsg = string.Format("TCPIP Server <- {0}", "Failed to start.");
                    _logger.Info(logMsg);

                    return false;
                }

                // Log CRC16 support
                string logMsgCRC = string.Format("TCPIP Server with CRC16 support started on {0}:{1}", hostIP, hostPort);
                _logger.Info(logMsgCRC);
            }

            NotifyPropertyChange("ServerStarted");
            NotifyPropertyChange("ServerStopped");
            NotifyPropertyChange("IPAddress");
            NotifyPropertyChange("PortNumber");
            return true;
        }

        private void tcpServer_ClientDisconnected(object sender, EventArgs e)
        {
            tcpConnected = false;
            NotifyPropertyChange("TCPConnected");

            connectionInfoString = "Disconnected";
            NotifyPropertyChange("ConnectionInfoString");

            string info = string.Format("{0}<<Client Disconnected", DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss.fff"));
            Application.Current.Dispatcher.Invoke(new Action(() => { ListMsgReceived.Add(info); }));

            Global.IsToolConnected = false;

            string logMsg = string.Format("TCPIP Server <- {0}", "Client disconnected.");
            _logger.Info(logMsg);
        }

        public bool StopServer()
        {
            tcpServer.StopServer();
            NotifyPropertyChange("ServerStarted");
            NotifyPropertyChange("ServerStopped");

            string logMsg = string.Format("TCPIP Server <- {0}", "Stopped.");
            _logger.Info(logMsg);

            return true;
        }

        public void SendMessage(string msgSent)
        {
            tcpServer.SendToClient(msgSent);

            string info = string.Format("{0}>>{1}", DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss.fff"), msgSent.Trim(new char[] { '\r', '\n' }));
            Application.Current.Dispatcher.Invoke(new Action(() => { ListMsgSent.Add(info); }));

            string logMsg = string.Format("TCPIP -> {0}", msgSent.Trim());
            _logger.Info(logMsg);
        }

        // Add method to send CRC16 protocol messages
        public void SendProtocolMessage(string command)
        {
            if (tcpServer.SendProtocolMessage(command))
            {
                string info = string.Format("{0}>>[CRC16] {1}", DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss.fff"), command);
                Application.Current.Dispatcher.Invoke(new Action(() => { ListMsgSent.Add(info); }));

                string logMsg = string.Format("TCPIP -> [CRC16] {0}", command);
                _logger.Info(logMsg);
            }
        }

        /// <summary>
        /// Processes a command using the CommandProcessor, sends the raw response via TCP, and logs the response text.
        /// </summary>
        /// <param name="command">The command to process and send.</param>
        public async Task ProcessAndSendRawCommand(string command)
        {
            var commandProcessor = GetCommandProcessor();
            if (commandProcessor == null)
            {
                _logger.Error("CommandProcessor is not available.");
                return;
            }

            try
            {
                byte[] responseBytes = await commandProcessor.ProcessCommand(command);
                tcpServer.SendRaw(responseBytes);

                string responseText = responseBytes != null && responseBytes.Length > 3
                    ? Encoding.ASCII.GetString(responseBytes, 0, responseBytes.Length - 3)
                    : "(no response)";

                Application.Current.Dispatcher.Invoke(() => ListMsgSent?.Add(responseText));
                _logger.Info($"Processed and sent raw command. Command: {command}, Response: {responseText}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error processing and sending raw command: {command}");
            }
        }

        private void ClientConnectedEvent(object obj, string Address)
        {
            tcpConnected = true;
            NotifyPropertyChange("TCPConnected");

            connectionInfoString = string.Format("Connected=>{0} (CRC16 Ready)", Address);
            NotifyPropertyChange("ConnectionInfoString");

            Global.IsToolConnected = true;

            Application.Current.Dispatcher.Invoke(new Action(() => { ListMsgReceived.Add(string.Format("{0}<<Client {1} connected (CRC16 Protocol Ready)", DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss.fff"), Address)); }));

            string logMsg = string.Format("TCPIP Server <- {0} (with CRC16 support)", "Client connected.");
            _logger.Info(logMsg);
        }

        private void MessageReceivedEvent(object obj, string msgReceived)
        {
            MessageReceivedEventHandler?.Invoke(null, msgReceived);

            string info = string.Format("{0}<<{1}", DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss.fff"), msgReceived.Trim(new char[] { '\r', '\n' }));
            Application.Current.Dispatcher.Invoke(new Action(() => { ListMsgReceived.Add(info); }));

            string logMsg = string.Format("TCPIP <- {0}", msgReceived.Trim());
            _logger.Info(logMsg);
        }
    }
}