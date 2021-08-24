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

    Thread dataReceiveThread;

    private static uint _datasize = 12;

    public byte[] IncomingDataBuffer = null;
    public bool Connected = false;

    public object mThreadLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        Connected = false;
        _host = Dns.GetHostEntry("localhost");
        _ipAddress = _host.AddressList[0];
        _localEndPoint = new IPEndPoint(_ipAddress, 11000);
        

        Thread WaitingThread = new Thread(StartServer);
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
            dataReceiveThread = new Thread(new ThreadStart(ReceiveData));
            dataReceiveThread.IsBackground = true;
            dataReceiveThread.Start();
            Connected = true;
        }
        catch(Exception e)
        {
            print("Error in Startup: " + e);
        }
        
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                byte[] bytes = new byte[_datasize];
                int bytesRec = _handler.Receive(bytes);

                if (bytes[0] == 0x55)
                {
                    lock (mThreadLock)
                    {
                        IncomingDataBuffer = bytes;
                    }
                }
            }
            catch (Exception e)
            {
                print("ERROR in Update: " + e);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
