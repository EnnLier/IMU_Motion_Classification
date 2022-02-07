using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using BLE_Drive_UI.Domain;
using System.Threading;
using Windows.Storage.Streams;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Globalization;
using System.Timers;
using System.Diagnostics;

namespace BLE_Drive_UI.src
{
    class BLEhub
    {
        public BLEhub()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            _dataSaver = new SyncDataSaver(_writeBuffersize,_writeBufferRate);
            _tcpStreamer = new TCPStreamer();

            ConnectedDeviceList = new List<BLEDevice>();
        }

        public List<BLEDevice> ConnectedDeviceList;
        private object m_DataLock = new object();

        public bool IsSaving { get; private set; }
        public bool IsStreaming { get; private set; }
        public bool Busy { get; private set; }
        public int Connected 
        {
            get
            {
                return ConnectedDeviceList.Count;
            }
        }

        private SyncDataSaver _dataSaver;
        private TCPStreamer _tcpStreamer;

        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;
        public event EventHandler<statusChangedEventArgs> StatusChanged;

        //private static UInt16 _packetsize = 22;
        //private static UInt16 _datapoints = 6;              //to Plot
        private static UInt16 _writeBuffersize = 200;
        private static UInt16 _writeBufferRate = 10;
        private static UInt16 _dataLength = 10;
        private static UInt16 _numOfDevices = 2;

        //public int[][] Calibration = new int[2][];
        public float[] IMUdata = new float[_numOfDevices * _dataLength];


        ~BLEhub()
        {
            if (IsSaving)
                StopSaving();
            if (IsStreaming)
                StopStreaming();
        }

        public float[] GetDataToPlot()
        {
            lock (m_DataLock)
            {
                return IMUdata;
            }
        }

        public void StartStreaming()
        {
            Thread InitTCPThread = new Thread(_tcpStreamer.StartTCPClient);
            InitTCPThread.Start();
            //_tcpStreamer.StartTCPClient();
        }

        public void StopStreaming()
        {
            _tcpStreamer.CloseTCPClient();
        }

        public void StartSaving()
        {
            if (!_dataSaver.Active)
            {
                IsSaving = true;
                _dataSaver.start();
            }
        }

        public void StopSaving()
        {
            if (_dataSaver.Active)
            {
                _dataSaver.stop();
                IsSaving = false;
            }
        }

        public void Disconnect(BLEDeviceInformation deviceInformation)
        {
            //Console.WriteLine("Disconnecting from ID: " + sensorID);
            foreach (BLEDevice device in ConnectedDeviceList)
            {
                //Console.WriteLine("Device ID: " + device.DeviceInformation.SensorID);
                if (device.DeviceInformation.SensorID == deviceInformation.SensorID)
                {
                    //Console.WriteLine("Device found ID: " + sensorID);
                    Disconnect(device);
                    return;
                }
            }
            
        }

        public void Disconnect(BLEDevice device)
        {
            try
            {
                Busy = true;
                device.Disconnect();
                //ConnectedDeviceList.Remove(device);
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                device.Disconnect();
            }
            finally
            {
                Busy = false;
            }
        }

        public void Recalibrate_imu(BLEDeviceInformation device)
        {
            try
            {
                //WriteToBLEDevice(device, "Recalibrate");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                Recalibrate_imu(device);
            }
        }

        //BLE Related functions
        public void ConnectDevice(BLEDeviceInformation deviceInformation)
        {
            Busy = true;
            foreach(var dev in ConnectedDeviceList)
            {
                Console.WriteLine(dev.DeviceInformation.SensorID);
                if (dev.DeviceInformation.BLEId == deviceInformation.BLEId)
                {
                    Console.WriteLine("Device Already connected");
                    Busy = false;
                    return;
                }
                
            }
            BLEDevice device = new BLEDevice();
            device.Connect(deviceInformation);
            device.ConnectedChanged += OnConnectionChanged;
        }


        private void TCPConnectionStatusChangedEvent(object sender, tcpConnectEventArgs e)
        {
            IsStreaming = e.connected;
        }

        protected virtual void OnConnectionChanged(object sender, ConnectedChangedEventArgs e)
        {
            var device = (BLEDevice)sender;
            var connected = e.status;

            if (connected)
            {
                if (!ConnectedDeviceList.Contains(device))
                {
                    ConnectedDeviceList.Add(device);
                    device.sendData += OnIMUData;
                    device.StatusChanged += StatusChanged;
                    Busy = false;
                }
                else
                {
                    Busy = false;
                    return;
                }
            }
            else
            {
                device.ConnectedChanged -= OnConnectionChanged;
                device.StatusChanged -= StatusChanged;
                device.sendData -= OnIMUData;
                ConnectedDeviceList.Remove(device);
                device = null;
            }
            EventHandler<ConnectedChangedEventArgs> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnIMUData(object sender, imuDataEventArgs e)
        {
            //Console.WriteLine(e.id);
            var device = (BLEDevice)sender;
            var id = device.DeviceInformation.SensorID;
            lock(m_DataLock)
            {
                //if(IMUdata != null)
                //{
                //IMUdata[id] = e.data;
                if (IsSaving)
                {
                    _dataSaver.addData(id, e.data);
                }
                //}
                
            }
        }
    }
}
