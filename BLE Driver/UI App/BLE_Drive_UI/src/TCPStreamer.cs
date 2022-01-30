using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BLE_Drive_UI.Domain;
using System.Threading.Tasks;

namespace BLE_Drive_UI.src
{
    class TCPStreamer
    {
        public event EventHandler<statusChangedEventArgs> StatusChanged;
        public event EventHandler<tcpConnectEventArgs> ConnectedChanged;

        public bool Connected { get; private set;}

        public IPHostEntry _host;
        public IPAddress _ipAddress;
        public IPEndPoint _remoteEP;
        public Socket _sender;

        public TCPStreamer(IPHostEntry host = null, IPAddress ipAddress = null, IPEndPoint remoteEP = null)
        {
            try
            {
                _host = host == null ? Dns.GetHostEntry("localhost") : host;
                _ipAddress = ipAddress == null ? _host.AddressList[0] : ipAddress;
                _remoteEP = remoteEP == null ? new IPEndPoint(_ipAddress, 11000) : remoteEP;

                // Create a TCP/IP  socket.   
                _sender = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                OnStatusChanged("TCP Client failed on creation");
                _sender.Dispose();
                _sender = null;
            }
        }

        public void StartTCPClient()
        {
            if (_sender != null) { return; }
            byte[] bytes = new byte[512];

            // Connect the socket to the remote endpoint. Catch any errors.    
            try
            {
                // Connect to Remote EndPoint  
                _sender.Connect(_remoteEP);

                Console.WriteLine("Socket connected to {0}",
                    _sender.RemoteEndPoint.ToString());

                OnStatusChanged("TCP Client Connected");
                OnConnectedChanged(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("TXP Client Exception: {0}", e.ToString());
                OnStatusChanged("TCP Client Connection Failed");
                _sender.Dispose();
                _sender = null;
                OnConnectedChanged(false);
            }
            
        }

        public void CloseTCPClient()
        {
            // Release the socket.    
            if (_sender != null && Connected == true)
            {
                try
                {
                    sendDataTCP(ToOutgoingPacket(new byte[] { 0x04 }, 1));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Sending 'Close' command via TCP connection not possible");
                    Console.WriteLine(e.Message);
                }
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
                _sender.Dispose();
                _sender = null;
                OnStatusChanged("TCP Client Connection Closed");
                OnConnectedChanged(false);
            }
        }

        public void sendDataTCP(String data)
        {
            try
            {
                if (_sender.Connected)
                {
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    int bytesSent = _sender.Send(msg);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void sendDataTCP(Byte[] data)
        {
            try
            {
                if (_sender.Connected)
                {
                    int bytesSent = _sender.Send(data);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private String ToOutgoingPacket(String stringBuffer)
        {
            stringBuffer.Insert(0, Encoding.Default.GetString(new Byte[] { 0x55 }));
            //stringBuffer.Insert(11, Encoding.Default.GetString('\r'));
            stringBuffer += '\r';
            stringBuffer += '\n';
            return stringBuffer;
        }

        private Byte[] ToOutgoingPacket(Byte[] stringBuffer, UInt16 len)
        {
            var res = new Byte[len + 1];
            res[0] = 0x55;
            stringBuffer.CopyTo(res, 1);

            return res;
        }

        protected virtual void OnStatusChanged(String status)
        {
            statusChangedEventArgs e = new statusChangedEventArgs();
            e.Status = status;
            e.Timestamp = DateTime.Now;

            EventHandler<statusChangedEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnConnectedChanged(bool connected)
        {
            Connected = connected;
            tcpConnectEventArgs e = new tcpConnectEventArgs();
            e.connected = connected;

            EventHandler<tcpConnectEventArgs> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
