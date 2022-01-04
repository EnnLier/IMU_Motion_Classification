using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using UnityEngine;
using System;
using System.Threading;

public class TCPListener : MonoBehaviour
{

    // Get Host IP Address that is used to establish a connection  
    // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
    // If a host has multiple addresses, you will get a list of addresses  
    private static IPHostEntry _host;
    private static IPAddress _ipAddress;
    private static IPEndPoint _localEndPoint;

    private static Socket _listener;
    private static Socket _handler;

    Thread DataReceiveThread;
    Thread WaitingThread;

    public static uint _datasize = 19;

    public byte[] IncomingDataBuffer = null;
    public bool Connected = false;
    public bool justClosed = false;

    public object mThreadLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        Connected = false;
        _host = Dns.GetHostEntry("localhost");
        _ipAddress = _host.AddressList[0];
        _localEndPoint = new IPEndPoint(_ipAddress, 11000);
        

        WaitingThread = new Thread(StartServer);
        WaitingThread.Start();
    }

    public void StartServer()
    {
        try
        {
            // Create a Socket that will use Tcp protocol      
            _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method  
            _listener.Bind(_localEndPoint);
            // Specify how many requests a Socket can listen before it gives Server busy response.  
            // We will listen 10 requests at a time  
            _listener.Listen(10);

            print("Waiting for a connection...");
            _handler = _listener.Accept();
            DataReceiveThread = new Thread(new ThreadStart(ReceiveData));
            DataReceiveThread.IsBackground = true;
            DataReceiveThread.Start();
            Connected = true;
        }
        catch(Exception e)
        {
            print("Error in Startup: " + e);
        }
        
    }

    private void ReceiveData()
    {
        while (Connected)
        {
            try
            {
                if (_handler.Available == 0) continue;
                byte[] bytes = new byte[_datasize];
                int bytesRec = _handler.Receive(bytes);

                if (bytes[0] == 0x55)
                {
                    //print("rec");
                    lock (mThreadLock)
                    {
                        IncomingDataBuffer = bytes;
                    }
                }
                if (bytes[1] == 0x04)
                {
                    Connected = false;
                }
            }
            catch (ObjectDisposedException e)
            {
                print("ObjectDisposedException: " + e);
                Connected = false;
                //closeClient();
            }
            catch (SocketException e)
            {
                print("SocketException: " + e);
                Connected = false;
                //closeClient();
            }
            catch (Exception e)
            {
                print("ERROR in Update: " + e);
                //Connected = false;
                //closeClient();
            }
            //finally
            //{
            //    if (!_listener.)
            //    {
            //        print("Connection aborted");
            //        Connected = false;
            //    }
            //}
        }
    }

    private void closeClient()
    {
        if (_listener != null)
        {
            //_listener.Shutdown(SocketShutdown.Both);
            _listener.Close();
            _listener.Dispose();
            print("Client closed");

            _listener = null;
            DataReceiveThread = null;
            Start();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DataReceiveThread != null)
        {
            if (!DataReceiveThread.IsAlive)
            {
                print("Waiting for WaitingThread to close!");
                while (WaitingThread.IsAlive)
                {
                    
                }
                WaitingThread.Abort();
                closeClient();
                
            }
        }
    }

    bool SocketConnected(Socket s)
    {
        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);
        if (part1 && part2)
            return false;
        else
            return true;
    }
}
