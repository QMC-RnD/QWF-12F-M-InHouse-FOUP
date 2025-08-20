using FoupControl;
using FOUPCtrl.Communication;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FOUPCtrl.TCPServer
{
    public class TCPServer
    {
        public event EventHandler<string> ClientConnectedEventHandler;
        public event EventHandler<string> MessageReceived;
        public event EventHandler ClientDisconnected;
        public event EventHandler<string> ResponseSent;

        //Socket class
        private TcpListener TcpListenerObj;
        private TcpClient TcpClientObj;

        private Thread TcpListenerThread;

        private ConnectedClient ConnectedClientObj;

        private bool stopServerFlag;

        // Add CRC16 support fields
        private CommandProcessor _commandProcessor;
        private FOUP_Ctrl _foupCtrl; // Use simple name since FOUP_Ctrl is in parent namespace
        private ProtocolSettings _protocolSettings;

        public bool ServerStarted { get; private set; }

        public bool Connected
        {
            get
            {
                if (ConnectedClientObj == null)
                    return false;

                return ConnectedClientObj.StillConnected;
            }
        }

        private string ErrorMessage = "";

        public TCPServer(System.Net.IPAddress IpAddress, int Port)
        {
            TcpListenerObj = new TcpListener(IpAddress, Port);
            _protocolSettings = ProtocolSettings.Default;
        }

        // Add method to set FOUP controller
        public void SetFOUPController(FOUP_Ctrl foupCtrl) // Use simple name
        {
            _foupCtrl = foupCtrl;
            _commandProcessor = new CommandProcessor(_foupCtrl);

            // Subscribe to acknowledgment events
            _commandProcessor.AckowledgmentSent += (sender, ackMessage) =>
            {
                // Send acknowledgment to UI through ResponseSent event
                ResponseSent?.Invoke(this, ackMessage);
            };
        }

        // Add method to configure protocol settings
        public void SetProtocolSettings(ProtocolSettings settings)
        {
            _protocolSettings = settings ?? ProtocolSettings.Default;
        }

        public bool StartServer()
        {
            if (ServerStarted)
            {
                ErrorMessage = "TCP Server already started.";
                return false;
            }

            try
            {
                TcpListenerObj.Start();
                TcpListenerThread = new Thread(new ThreadStart(ListenForClient));
                TcpListenerThread.Start();
                ServerStarted = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public void StopServer()
        {
            //jst in case TCP not listen
            if (!ServerStarted)
                return;

            stopServerFlag = true;

            //Disconnect client if any
            if (ConnectedClientObj != null)
            {
                if (ConnectedClientObj.StillConnected)
                    ConnectedClientObj.Disconnect();
            }
            //close the TCP listener
            TcpListenerObj.Server.Close();
            ServerStarted = false;
        }

        private void ListenForClient()
        {
            while (!stopServerFlag)
            {
                try
                {
                    while (!stopServerFlag)
                    {
                        if (TcpListenerObj.Pending())//return true if have new connection
                            break;

                        System.Threading.Thread.Sleep(50);
                    }

                    if (stopServerFlag)
                        return;

                    if (ConnectedClientObj != null)//Skip if already have at least 1 client connected
                    {
                        if (ConnectedClientObj.StillConnected)
                        {
                            if (ConnectedClientObj.CheckAlive())
                                continue;
                        }
                    }

                    if (stopServerFlag)
                        return;

                    TcpClientObj = TcpListenerObj.AcceptTcpClient();
                }
                catch
                {
                    break;
                }

                if (stopServerFlag)
                    return;

                IPEndPoint endPoint = (IPEndPoint)TcpClientObj.Client.RemoteEndPoint;

                ClientConnectedEventHandler?.Invoke(null, endPoint.Address.ToString());
                ConnectedClientObj = new ConnectedClient(TcpClientObj);
                ConnectedClientObj.ClientDisconnectedEvent += new ConnectedClient.ClientDisconnectedHandler(ConnectedClient_ClientDisconnected);
                ConnectedClientObj.MessageReceivedEvent += new ConnectedClient.MessageReceivedHandler(ConnectedClient_MessageReceived);
                ConnectedClientObj.RawDataReceivedEvent += new ConnectedClient.RawDataReceivedHandler(ConnectedClient_RawDataReceived);
            }
        }

        // Changed from async void to async Task and handle it properly
        private void ConnectedClient_MessageReceived(string MsgReceived)
        {
            Task.Run(async () =>
            {
                if (_protocolSettings?.EnableLogging == true)
                {
                    System.Diagnostics.Debug.WriteLine($"Received text message: {MsgReceived}");
                }

                if (_commandProcessor != null)
                {
                    byte[] response = await _commandProcessor.ProcessCommand(MsgReceived);
                    ConnectedClientObj.SendRaw(response);

                    // Extract response text (excluding CRC and terminator)
                    string responseText = response != null && response.Length > 3
                        ? Encoding.ASCII.GetString(response, 0, response.Length - 3)
                        : "(no response)";
                    ResponseSent?.Invoke(this, responseText);
                }

                MessageReceived?.Invoke(null, MsgReceived);
            });
        }

        // Changed from async void to proper Task handling
        // Raw data → Protocol detection → Command processing → CRC16 response
        private void ConnectedClient_RawDataReceived(byte[] rawData)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_protocolSettings?.EnableLogging == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"Received raw data: {ProtocolMessage.BytesToHexString(rawData)}");
                    }

                    var protocolMessage = ProtocolMessage.Parse(rawData);

                    if (protocolMessage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Parsed protocol command: {protocolMessage.Command}");

                        if (_commandProcessor != null)
                        {
                            // CRC16 protocol path
                            byte[] response = await _commandProcessor.ProcessCommand(protocolMessage.Command);
                            ConnectedClientObj.SendRaw(response);

                            // Extract response text (excluding CRC and terminator)
                            string responseText = response != null && response.Length > 3
                                ? Encoding.ASCII.GetString(response, 0, response.Length - 3)
                                : "(no response)";
                            ResponseSent?.Invoke(this, responseText);
                        }

                        MessageReceived?.Invoke(null, protocolMessage.Command);
                    }
                    else
                    {
                        // Plain text fallback
                        string textMessage = Encoding.ASCII.GetString(rawData).Trim('\0');

                        if (_commandProcessor != null)
                        {
                            byte[] response = await _commandProcessor.ProcessCommand(textMessage);
                            ConnectedClientObj.SendRaw(response);

                            // Extract response text (excluding CRC and terminator)
                            string responseText = response != null && response.Length > 3
                                ? Encoding.ASCII.GetString(response, 0, response.Length - 3)
                                : "(no response)";
                            ResponseSent?.Invoke(this, responseText);
                        }

                        MessageReceived?.Invoke(null, textMessage);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing raw data: {ex.Message}");
                }
            });
        }

        private void ConnectedClient_ClientDisconnected()
        {
            ClientDisconnected?.Invoke(null, null);
            if (ConnectedClientObj != null)
            {
                ConnectedClientObj.ClientDisconnectedEvent -= ConnectedClient_ClientDisconnected;
                ConnectedClientObj.MessageReceivedEvent -= ConnectedClient_MessageReceived;
                ConnectedClientObj.RawDataReceivedEvent -= ConnectedClient_RawDataReceived;
            }
        }

        public bool SendToClient(string MessageToSend)
        {
            if (ConnectedClientObj?.StillConnected != true) return false;
            return ConnectedClientObj.Send(MessageToSend);
        }

        // New method to send protocol messages with CRC16
        public bool SendProtocolMessage(string command)
        {
            if (ConnectedClientObj?.StillConnected != true) return false;

            var message = new ProtocolMessage
            {
                Code = _protocolSettings?.StationCode ?? 0x30,
                Address = _protocolSettings?.Address ?? 0x30,
                Command = command
            };

            byte[] messageBytes = message.ToBytes();

            if (_protocolSettings?.EnableLogging == true)
            {
                System.Diagnostics.Debug.WriteLine($"Sending protocol message: {message.ToHexString()}");
            }

            return ConnectedClientObj.SendRaw(messageBytes);
        }

        public void DisconnectClient(bool stopServer)
        {
            if (ConnectedClientObj != null)
                ConnectedClientObj.Disconnect();
        }

        private class ConnectedClient
        {
            public delegate void MessageReceivedHandler(string MsgReceived);
            public event MessageReceivedHandler MessageReceivedEvent;

            public delegate void RawDataReceivedHandler(byte[] RawData);
            public event RawDataReceivedHandler RawDataReceivedEvent;

            public delegate void ClientDisconnectedHandler();
            public event ClientDisconnectedHandler ClientDisconnectedEvent;

            private TcpClient TcpClientObj;
            private NetworkStream ClientNetworkStream;
            public bool StillConnected;
            private Thread ReadDataThread;

            public ConnectedClient(TcpClient Client)
            {
                TcpClientObj = Client;
                TcpClientObj.NoDelay = true;
                StillConnected = true;

                ReadDataThread = new Thread(new ThreadStart(ReadData));
                ReadDataThread.Start();
            }

            private void ReadData()
            {
                ClientNetworkStream = TcpClientObj.GetStream();

                byte[] data;
                int bytesRead;

                while (StillConnected)
                {
                    try
                    {
                        data = new byte[1024];
                        bytesRead = ClientNetworkStream.Read(data, 0, 1024);
                    }
                    catch
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Trim the data to actual bytes received
                    byte[] actualData = new byte[bytesRead];
                    Array.Copy(data, 0, actualData, 0, bytesRead);

                    // First, try to parse as CRC16 protocol message
                    var protocolMessage = ProtocolMessage.Parse(actualData);

                    if (protocolMessage != null)
                    {
                        // Valid CRC16 protocol message - only fire raw data event
                        RawDataReceivedEvent?.Invoke(actualData);
                    }
                    else
                    {
                        // Not a valid protocol message, treat as text - only fire text event
                        string msgReceived = System.Text.Encoding.ASCII.GetString(actualData).Trim('\0');
                        MessageReceivedEvent?.Invoke(msgReceived);
                    }
                }

                Disconnect();
            }

            public bool CheckAlive()
            {
                Send("");
                return StillConnected;
            }

            public bool Send(string Message)
            {
                if (!StillConnected) return false;

                try
                {
                    byte[] data = Encoding.ASCII.GetBytes(Message);

                    ClientNetworkStream.Write(data, 0, data.Length);
                    ClientNetworkStream.Flush();

                    return true;
                }
                catch
                {
                    Disconnect();
                    return false;
                }
            }

            // New method to send raw bytes
            public bool SendRaw(byte[] data)
            {
                if (!StillConnected) return false;

                try
                {
                    ClientNetworkStream.Write(data, 0, data.Length);
                    ClientNetworkStream.Flush();

                    return true;
                }
                catch
                {
                    Disconnect();
                    return false;
                }
            }

            public void Disconnect()
            {
                if (!StillConnected)
                    return;

                ClientNetworkStream?.Close();
                TcpClientObj?.Close();
                ClientDisconnectedEvent?.Invoke();
                StillConnected = false;
            }
        }

        public CommandProcessor GetCommandProcessor()
        {
            return _commandProcessor;
        }

        public void SendRaw(byte[] data)
        {
            if (ConnectedClientObj?.StillConnected == true && data != null && data.Length > 0)
            {
                ConnectedClientObj.SendRaw(data);
            }
        }

    }
}