using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BLE_Drive_UI.Domain;
using System.Threading.Tasks;

/// <summary>
/// This class allows transmission of incoming IMU data from the driver directly via a TCP connection
/// </summary>

namespace BLE_Drive_UI.src
{
    class TCPStreamer
    {
        //Events
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler<TcpConnectEventArgs> ConnectedChanged;

        //State variables
        public bool Connected { get; private set;}

        //TCP objects
        private IPHostEntry _host;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;
        private Socket _sender;

        /// <summary>
        /// Set paramters for the TCP Host on Port 11000 (Make this dynamic). If no parameters are set, establish connection to localhost
        /// </summary>
        /// <param name="host">Host IP</param>
        /// <param name="ipAddress">Client/Remote IP</param>
        /// <param name="remoteEP"></param>
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

        /// <summary>
        /// Connect to remote Client
        /// </summary>
        public void StartTCPClient()
        {
            if (_sender == null) { return; }
            byte[] bytes = new byte[512];

            // Connect the socket to the remote endpoint. Catch any errors.    
            try
            {
                // Connect to Remote EndPoint  
                Console.WriteLine("Connecting TCP");
                _sender.Connect(_remoteEP);

                Console.WriteLine("Socket connected to {0}",_sender.RemoteEndPoint.ToString());

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

        /// <summary>
        /// Close client connection
        /// </summary>
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

        /// <summary>
        /// Send string data
        /// </summary>
        /// <param name="data">String data</param>
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

        /// <summary>
        /// Send computed IMU data as Bytes
        /// </summary>
        /// <param name="sensorID">ID corresponding to IMU Data </param>
        /// <param name="data">IMU data</param>
        internal void sendDataTCP(int sensorID, float[] data)
        {
            try
            {
                var byteArray = new Byte[1 + 4 + data.Length * 4];
                //set Overhead
                byteArray[0] = 0x55;
                //Copy SensorID to Bytearray
                Buffer.BlockCopy(BitConverter.GetBytes(sensorID), 0, byteArray, 1, BitConverter.GetBytes(sensorID).Length);
                //Copy data to Bytearray
                Buffer.BlockCopy(data, 0, byteArray, 5, data.Length * 4);
                //Send bytearray
                sendDataTCP(byteArray);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Send Bytearray via TCP connection
        /// </summary>
        /// <param name="data">Bytearray to send</param>
        public void sendDataTCP(Byte[] data)
        {
            try
            {
                if (_sender.Connected)
                {
                    //Send if connection exists
                    int bytesSent = _sender.Send(data);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Create interpretable bytearray from String
        /// </summary>
        /// <param name="stringBuffer">String to send</param>
        /// <returns></returns>
        private String ToOutgoingPacket(String stringBuffer)
        {
            //Add overhead
            stringBuffer.Insert(0, Encoding.Default.GetString(new Byte[] { 0x55 }));

            //Append Frame end
            stringBuffer += '\r';
            stringBuffer += '\n';
            return stringBuffer;
        }

        /// <summary>
        /// Create interpretable Bytearray from Bytearray
        /// </summary>
        /// <param name="buffer">Bytearray to send</param>
        /// <param name="len">length of packet</param>
        /// <returns></returns>
        private Byte[] ToOutgoingPacket(Byte[] buffer, UInt16 len)
        {
            var res = new Byte[len + 1];
            res[0] = 0x55;
            buffer.CopyTo(res, 1);

            return res;
        }

        /// <summary>
        /// This function notifies the user via the GUI
        /// </summary>
        /// <param name="status"></param>
        protected virtual void OnStatusChanged(String status)
        {
            StatusChangedEventArgs e = new StatusChangedEventArgs();
            e.Status = status;
            e.Timestamp = DateTime.Now;

            EventHandler<StatusChangedEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This function notifies the Driver if the TCP connection was succesfully established
        /// </summary>
        /// <param name="connected"></param>
        protected virtual void OnConnectedChanged(bool connected)
        {
            Connected = connected;
            TcpConnectEventArgs e = new TcpConnectEventArgs();
            e.Connected = connected;

            EventHandler<TcpConnectEventArgs> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

    }
}
