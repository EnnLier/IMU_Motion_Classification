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

    private static int _numOfFloats = 11;
    public static int _datasize = 1 + 4 * _numOfFloats;

    public float[] IncomingDataBufferFront = null;
    public float[] IncomingDataBufferBack = null;
    public bool Connected = false;
    public bool justClosed = false;

    public object mThreadLock = new object();

    private Vector3 EulerAngles;

    private GameObject Front;
    private GameObject Back;

    // Start is called before the first frame update
    void Start()
    {
        Connected = false;
        _host = Dns.GetHostEntry("localhost");
        _ipAddress = _host.AddressList[0];
        _localEndPoint = new IPEndPoint(_ipAddress, 11000);

        //Front = GameObject.Find("skateboard/Axis_Front");
        Front = this.transform.GetChild(0).gameObject;
        //Back = GameObject.Find("skateboard/Axis_Back");
        Back = this.transform.GetChild(1).gameObject;

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
            _listener.Listen(40);

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

                //create a float array and copy the bytes into it...
                var floatArray = new float[_numOfFloats];
                Buffer.BlockCopy(bytes, 1, floatArray, 0, _datasize - 1);

                if (bytes[0] == 0x55)
                {
                    //print("rec");
                    lock (mThreadLock)
                    {
                        

                        if ((int)floatArray[0] == 0)
                        {
                            IncomingDataBufferFront = floatArray;
                            //Front.transform.rotation = BodyPose;

                        }
                        else if ((int)floatArray[0] == 1)
                        {
                            IncomingDataBufferBack = floatArray;
                            //Back.transform.rotation = BodyPose;
                        }
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
        if (Connected)
        {
            print(IncomingDataBufferFront.Length);
            if (IncomingDataBufferFront != null && IncomingDataBufferFront.Length == _numOfFloats)
            {
                
                float quatW = IncomingDataBufferFront[1];
                float quatX = IncomingDataBufferFront[2];
                float quatY = IncomingDataBufferFront[3];
                float quatZ = IncomingDataBufferFront[4];

                Quaternion BodyPose = new Quaternion(-quatZ, -quatX, -quatY, quatW);

                EulerAngles.x = -BodyPose.eulerAngles.x;
                EulerAngles.y = BodyPose.eulerAngles.z;
                EulerAngles.z = BodyPose.eulerAngles.y;

                Front.transform.rotation = BodyPose;
            }
            if (IncomingDataBufferBack != null && IncomingDataBufferBack.Length == _numOfFloats)
            {
                float quatW = IncomingDataBufferBack[1];
                float quatX = IncomingDataBufferBack[2];
                float quatY = IncomingDataBufferBack[3];
                float quatZ = IncomingDataBufferBack[4];

                Quaternion BodyPose = new Quaternion(-quatZ, -quatX, -quatY, quatW);

                EulerAngles.x = -BodyPose.eulerAngles.x;
                EulerAngles.y = BodyPose.eulerAngles.z;
                EulerAngles.z = BodyPose.eulerAngles.y;

                Back.transform.rotation = BodyPose;
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
