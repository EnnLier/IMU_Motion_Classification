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

        private SyncDataSaver _dataSaver;
        private TCPStreamer _tcpStreamer;

        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;
        public event EventHandler<statusChangedEventArgs> StatusChanged;

        //private static UInt16 _packetsize = 22;
        //private static UInt16 _datapoints = 6;              //to Plot
        private static UInt16 _writeBuffersize = 200;
        private static UInt16 _writeBufferRate = 10;

        public int[][] Calibration = new int[2][];
        public float[][] IMUdata = new float[2][];


        ~BLEhub()
        {
            if (IsSaving)
                StopSaving();
            if (IsStreaming)
                StopStreaming();
        }

        public float[][] GetDataToPlot()
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

        public void Disconnect()
        {
            foreach (var device in ConnectedDeviceList)
            {
                try
                {
                    device.Disconnect();
                    ConnectedDeviceList.Remove(device);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Retrying...");
                    Thread.Sleep(300);
                    device.Disconnect();
                }
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
            BLEDevice device = new BLEDevice();
            device.ConnectDevice(deviceInformation);
            ConnectedDeviceList.Add(device);
            device.sendData += OnIMUData;
            device.ConnectedChanged += ConnectedChanged;
            device.StatusChanged += StatusChanged;
            Busy = false;
        }


        //protected virtual void OnSelectedDeviceFound()
        //{
        //    EventHandler handler = SelectedDeviceFound;
        //    if (handler != null)
        //    {
        //        handler(this, EventArgs.Empty);
        //    }
        //}

        private void TCPConnectionStatusChangedEvent(object sender, tcpConnectEventArgs e)
        {
            IsStreaming = e.connected;
        }

        private void OnIMUData(object sender, imuDataEventArgs e)
        {
            //Console.WriteLine(e.id);
            lock(m_DataLock)
            {
                if(IMUdata != null)
                    IMUdata[e.id] = e.data;
                if(Calibration != null)
                {
                    Calibration[e.id] = new int[] { (e.calib >> 6) & 0x03, (e.calib >> 4) & 0x03, (e.calib >> 2) & 0x03, (e.calib) & 0x03 }; ;
                }

            }

        }
    }
}
