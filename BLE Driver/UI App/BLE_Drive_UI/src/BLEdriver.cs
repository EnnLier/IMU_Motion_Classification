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
    class BLEdriver
    {
        private BluetoothLEDevice _BLEDevice;
        //public BLEdevice ConnectedDeviceInformation { get; set; }
        public List<BLEDeviceInformation> ConnectedDeviceInformationList;

        public bool IsStreaming { get; private set; }
        public bool IsPlotting { get; private set; }
        public bool IsSaving { get; private set; }

        public int Connected
        {
            get
            {
                return ConnectedDeviceInformationList.Count;
            }
        }
        public bool Busy;
        //public int BatteryLevel = 0;

        private static UInt16 _packetsize = 21;
        private static UInt16 _datapoints = 6;              //to Plot
        private static UInt16 _writeBuffersize = 200;
        private static UInt16 _writeBufferRate = 10;

        public event EventHandler<statusChangedEventArgs> StatusChanged;
        //public event EventHandler<changeLabelEventArgs> ChangeLabel;
        public event EventHandler<imuDataEventArgs> UpdateChart;
        public event EventHandler SelectedDeviceFound;
        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;
        //public event Action<int> UpdateBatteryInfo;

        private object m_DataLock = new object();
        private object m_DataLockBatt = new object();
        private object m_DataLockCalib = new object();

        //private doubleBuffer _doubleBuffer;
        //public DoubleDataBufferAsync _doubleBuffer;
        private SyncDataSaver _dataSaver;
        private TCPStreamer _tcpStreamer;

        //public string[] Calibration;

        public float[] dataToPlot;
        public String stringToSave = String.Empty;

        public BLEdriver()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            IsSaving = false;
            IsStreaming = false;
            _dataSaver = new SyncDataSaver(_writeBuffersize, _writeBufferRate);
            _tcpStreamer = new TCPStreamer();

            _tcpStreamer.ConnectedChanged += TCPConnectionStatusChangedEvent;

            //Calibration = new string[] {"0","0","0","0"};
            dataToPlot = new float[_datapoints];

            ConnectedDeviceInformationList = new List<BLEDeviceInformation>();

        }


        ~BLEdriver()
        {
            if (IsSaving)
                StopSaving();
            if (IsStreaming)
                StopStreaming();
        }

        public float[] GetDataToPlot()
        {
            lock(m_DataLock)
            {
                return dataToPlot;
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
            if(!_dataSaver.Active)
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

        public async void Disconnect(BLEDeviceInformation deviceInformation)
        {
            try
            {
                OnStatusChanged("Disconnecting " + deviceInformation.Name + "...");
                await WriteToBLEDevice(deviceInformation, "Disconnect");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                Disconnect(deviceInformation);
            }
        }

        public void Recalibrate_imu(BLEDeviceInformation device)
        {
            try
            {
                WriteToBLEDevice(device, "Recalibrate");
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
        public async void ConnectDevice(BLEDeviceInformation deviceInformation)
        {
            if (ConnectedDeviceInformationList.Contains(deviceInformation))
            {
                OnStatusChanged("Device " + deviceInformation.Name + " already connected");
                return;
            }
            Busy = true;
            //ConnectedDeviceInformation = deviceInformation;

            
            OnStatusChanged("Initializing");
            try
            {
                _BLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.BLEId);

                _BLEDevice.ConnectionStatusChanged += BLEConnectionStatusChangedEvent;
                //_BLEDevice.GattServicesChanged += GattServicesChangedEvent;
                //_BLEDevice.NameChanged += NameChangedEvent;

            }
            catch(Exception e)
            {

                OnStatusChanged("Failed to connect");
                throw new Exception("Failed to Connect: ", e);
            }
            OnStatusChanged("Connecting...");


            GattDeviceServicesResult service_result = await _BLEDevice.GetGattServicesAsync();

            //GattCharacteristicsResult char_result;

            if (service_result.Status == GattCommunicationStatus.Success)
            {
                foreach (GattDeviceService serv in service_result.Services)
                {

                    GattCharacteristicsResult char_result = await serv.GetCharacteristicsAsync();

                    foreach (GattCharacteristic characteristic in char_result.Characteristics)
                    {
                        var properties = characteristic.CharacteristicProperties;
                        if (serv.Uuid == new Guid(BLEUUID.BLEUART_CUSTOM_UUID))
                        {
                            //BleuartCharacteristic = characteristic;
                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    deviceInformation.BLEuartCharacteristic = characteristic;
                                }
                            }
                        }
                        if (properties.HasFlag(GattCharacteristicProperties.Write))
                        {
                            //BleuartCharacteristic = characteristic;
                            //GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            //    GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                            //if (status == GattCommunicationStatus.Success)
                            //{
                            deviceInformation.BLEuartCharacteristic_write = characteristic;
                            //}
                        }
                        if (serv.Uuid == new Guid(BLEUUID.BLEUART_BATTERY_SERVICE))
                        {
                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    deviceInformation.BatteryCharacteristic = characteristic;
                                }
                            }
                        }
                    }
                        
                }
                try
                {
                    if (deviceInformation.BatteryCharacteristic != null && deviceInformation.BLEuartCharacteristic != null)
                    {
                        deviceInformation.BatteryCharacteristic.ValueChanged += Characteristic_ValueChanged;
                        deviceInformation.BLEuartCharacteristic.ValueChanged += Characteristic_ValueChanged;
                    }
                    else
                    {
                        Console.WriteLine("Battery Service: + " );
                        OnStatusChanged("Services not found");
                    }
                }
                catch (Exception e)
                {
                    OnStatusChanged("Services not found");
                    Console.WriteLine("Services not found: " + e.ToString());
                }
            }
            ConnectedDeviceInformationList.Add(deviceInformation);

            Busy = false;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            //if (ConnectedDeviceInformation.BatteryCharacteristic == null || ConnectedDeviceInformation.BLEuartCharacteristic == null) { return; }
            //Console.WriteLine("AttributeHandle: " + sender.Service.AttributeHandle);
            //Console.WriteLine("UserDescription: " + sender.UserDescription);

            //Console.WriteLine("Uuid3: " + sender.Service.Session.DeviceId.Id);
            //Console.WriteLine("myUUID: " + ConnectedDeviceInformationList[0].BLEId);

            var device = ConnectedDeviceInformationList.Find(i => string.Equals(i.BLEId, sender.Service.Session.DeviceId.Id));
            if(device == null)
            {
                Console.WriteLine("Unknown device");
                return;
            }

            if (sender.Service.AttributeHandle == device.BatteryCharacteristic.Service.AttributeHandle)
            //if (sender.Service.AttributeHandle == ConnectedDeviceInformation.BatteryCharacteristic.Service.AttributeHandle)
            {
                
                lock (m_DataLockBatt)
                {
                    device.BatteryLevel = reader.ReadByte();
                }
                //UpdateBatteryInfo(BatteryLevel);
                //Console.WriteLine(BatteryLevel);
            }
            if (sender.Service.AttributeHandle == device.BLEuartCharacteristic.Service.AttributeHandle)
            //if (sender.Service.AttributeHandle == ConnectedDeviceInformation.BLEuartCharacteristic.Service.AttributeHandle)
            {
                try
                { 
                    var packet = new Byte[_packetsize];
                    reader.ReadBytes(packet);

                    //var id = data[0];

                    int[] calib = new int[4];
                    calib[0] = ((packet[0] >> 6) & 0x03);      //sys
                    calib[1] = ((packet[0] >> 4) & 0x03);      //gyr
                    calib[2] = ((packet[0] >> 2) & 0x03);      //acc
                    calib[3] = ((packet[0]) & 0x03);           //mag

                    device.Calibration = calib;
                
                    var off = 1;
                    var scalingFactor = (1.00 / (1 << 14));
                    float quatW, quatX, quatY, quatZ;
                    quatW = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    quatX = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    quatY = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));
                    quatZ = (float)scalingFactor * ((Int16)(packet[off + 6] | (packet[off + 7] << 8)));

                    off = 9;
                    scalingFactor = (1.00 / 100.0);// 1m/s^2 = 100 LSB 
                    float x_a, y_a, z_a;
                    x_a = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    y_a = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    z_a = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));

                    off = 15;
                    scalingFactor = (1.00 / 16.0);// 1dps = 16 LSB
                    float gyrx, gyry, gyrz;
                    gyrx = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    gyry = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    gyrz = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));

                    device.Data = new float[] { quatW, quatX, quatY, quatZ, x_a, y_a, z_a, gyrx, gyry, gyrz };

                    lock (m_DataLock)
                    {
                        dataToPlot[0] = x_a;
                        dataToPlot[1] = y_a;
                        dataToPlot[2] = z_a;
                    }

                    if (IsStreaming && _tcpStreamer != null)
                    {
                        _tcpStreamer.sendDataTCP(packet);
                    }
                    if (IsSaving && _dataSaver != null)
                    {
                        //stringToSave = device.SensorID.ToString() + " " + Calibration[0] + " " + Calibration[1] + " " + Calibration[2] + " " + Calibration[3] + " " + quatW.ToString("0.0000") + " " + quatX.ToString("0.0000") + " " + quatY.ToString("0.0000") + " " + quatZ.ToString("0.0000") + " "
                        //+ x_a.ToString("0.0000") + " " + y_a.ToString("0.0000") + " " + z_a.ToString("0.0000") + " " + gyrx.ToString("0.0000") + " " + gyry.ToString("0.0000") + " " + gyrz.ToString("0.0000");
                        _dataSaver.addData(device.SensorID,device.GetDataAsString());
                    }
                }
                catch(System.Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        private async void WriteToBLEDevice(BLEDeviceInformation device, Byte[] data)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            GattCommunicationStatus result = await device.BLEuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
            if (result == GattCommunicationStatus.Success)
            {
                Console.WriteLine(" Successfully wrote to device");
            }
        }

        private async Task WriteToBLEDevice(BLEDeviceInformation device, String data)
        {
            if(device == null || device.BLEuartCharacteristic_write == null)
            {
                return;
            }
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            //Debug.WriteLine(_deviceInformation.BLEuartCharacteristic_write.CharacteristicProperties);
            try
            {
                GattCommunicationStatus result = await device.BLEuartCharacteristic_write.WriteValueAsync(writer.DetachBuffer());
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine("Error Writing to BLE Device: ");
                Console.WriteLine(e.Message);
                throw new System.UnauthorizedAccessException();
            }
            //var result = await _deviceInformation.BLEua crtCharacteristic.
            //GattWriteResult result = await _deviceInformation.BLEuartCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());
            //Debug.WriteLine(result.ProtocolError.ToString()); Debug.WriteLine(result.Status.ToString());
            //if (result == GattWriteResult.)
            //{
            //    Console.WriteLine(" Successfully wrote to device");
            //}
        }

        int err_count = 0;
        private void BLEConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            BLEDeviceInformation deviceInformation = null;
            // Change this
            while (deviceInformation == null)
            {
                deviceInformation = ConnectedDeviceInformationList.Find(item => item.Name == sender.Name);
                Thread.Sleep(5);
                if (err_count > 100) return;
            }
            err_count = 0;
            
            //Console.WriteLine("Connection Status changed: " + sender.ConnectionStatus);
            var connected = sender.ConnectionStatus.ToString() == "Connected" ? true : false;
            OnStatusChanged(sender.ConnectionStatus.ToString()+ " "+ sender.Name);

            //Prepare event to update GUI
            ConnectedChangedEventArgs e = new ConnectedChangedEventArgs();
            e.deviceInformation = deviceInformation;
            e.status = connected;

            if (connected)
            {

            }
            else
            {
                sender.ConnectionStatusChanged -= BLEConnectionStatusChangedEvent;
                sender.Dispose();
                ConnectedDeviceInformationList.Remove(deviceInformation);
                _dataSaver?.addData(deviceInformation.SensorID,String.Empty);
                deviceInformation = null;
            }

            EventHandler<ConnectedChangedEventArgs> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, e);
            }

        }


        //Eventhandler
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

        //protected virtual void OnChangeLabel(String label, String value)
        //{
        //    changeLabelEventArgs e = new changeLabelEventArgs();
        //    e.label = label;
        //    e.value = value;
        //    EventHandler<changeLabelEventArgs> handler = ChangeLabel;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        //protected virtual void OnaccelerationData(float x, float y, float z, float gx, float gy, float gz)
        //{
        //    imuDataEventArgs e = new imuDataEventArgs();
        //    e.Accx = x;
        //    e.Accy = y;
        //    e.Accz = z;
        //    e.Gyrx = gx;
        //    e.Gyry = gy;
        //    e.Gyrz = gz;
        //    EventHandler<imuDataEventArgs> handler = UpdateChart;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        protected virtual void OnSelectedDeviceFound()
        {
            EventHandler handler = SelectedDeviceFound;
            if(handler != null)
            {
                handler(this,EventArgs.Empty);
            }
        }

        private void TCPConnectionStatusChangedEvent(object sender, tcpConnectEventArgs e)
        {
            IsStreaming = e.connected;
        }
    }


 
}
