using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BLE_Drive_UI.Domain;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLE_Drive_UI.src
{
    class BLEDevice
    {
        public BLEDeviceInformation DeviceInformation { get; set;}

        public BLEDevice()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Connected = false;

            //Calibration = new string[] { "0", "0", "0", "0" };
            dataToPlot = new float[_datapoints];
        }

        private BluetoothLEDevice _BLEDevice;

        public bool Connected;
        public bool Busy;
        public int BatteryLevel = 0;

        private static UInt16 _packetsize = 22;
        private static UInt16 _datapoints = 10;              //to Plot
        //private static UInt16 _writeBuffersize = 200;
        //private static UInt16 _writeBufferRate = 10;

        public event EventHandler<statusChangedEventArgs> StatusChanged;
        //public event EventHandler<changeLabelEventArgs> ChangeLabel;
        public event EventHandler<imuDataEventArgs> sendData;
        //public event EventHandler SelectedDeviceFound;
        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;
        //public event Action<int> UpdateBatteryInfo;

        private object m_DataLock = new object();
        private object m_DataLockBatt = new object();
        private object m_DataLockCalib = new object();

        //public string[] Calibration;

        public float[] dataToPlot;
        public String stringToSave = String.Empty;

        public float[] GetDataToPlot()
        {
            lock (m_DataLock)
            {
                return dataToPlot;
            }
        }

        public void Disconnect()
        {
            try
            {
                OnStatusChanged("Disconnecting " + DeviceInformation.Name + "...");
                WriteToBLEDevice(DeviceInformation, "Disconnect");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                Disconnect();
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
            //if (ConnectedDeviceInformationList.Contains(deviceInformation))
            //{
            //    OnStatusChanged("Device " + deviceInformation.Name + " already connected");
            //    return;
            //}
            Busy = true;
            //DeviceInformation = deviceInformation;

            OnStatusChanged("Initializing");
            try
            {
                _BLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

                _BLEDevice.ConnectionStatusChanged += BLEConnectionStatusChangedEvent;
                //_BLEDevice.GattServicesChanged += GattServicesChangedEvent;
                //_BLEDevice.NameChanged += NameChangedEvent;

            }
            catch (Exception e)
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
                        Console.WriteLine("Battery Service: + ");
                        OnStatusChanged("Services not found");
                    }
                }
                catch (Exception e)
                {
                    OnStatusChanged("Services not found");
                    Console.WriteLine("Services not found: " + e.ToString());
                }
            }

            //ConnectedDeviceInformationList.Add(deviceInfomation);
            DeviceInformation = deviceInformation;
            Busy = false;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            //if (ConnectedDeviceInformation.BatteryCharacteristic == null || ConnectedDeviceInformation.BLEuartCharacteristic == null) { return; }

            if (sender.Service.AttributeHandle == DeviceInformation.BatteryCharacteristic.Service.AttributeHandle)
            //if (sender.Service.AttributeHandle == ConnectedDeviceInformation.BatteryCharacteristic.Service.AttributeHandle)
            {

                lock (m_DataLockBatt)
                {
                    BatteryLevel = reader.ReadByte();
                }
                //UpdateBatteryInfo(BatteryLevel);
                //Console.WriteLine(BatteryLevel);
            }
            if (sender.Service.AttributeHandle == DeviceInformation.BLEuartCharacteristic.Service.AttributeHandle)
            //if (sender.Service.AttributeHandle == ConnectedDeviceInformation.BLEuartCharacteristic.Service.AttributeHandle)
            {
                try
                {
                    var data = new Byte[_packetsize];
                    reader.ReadBytes(data);

                    //var id = data[0];
                    int id = this.DeviceInformation.isFront ? 0 : 1;
                    byte calib = data[1];

                    //lock (m_DataLockCalib)
                    //{
                    //    Calibration[0] = ((calib >> 6) & 0x03).ToString();      //sys
                    //    Calibration[1] = ((calib >> 4) & 0x03).ToString();      //gyr
                    //    Calibration[2] = ((calib >> 2) & 0x03).ToString();      //acc
                    //    Calibration[3] = ((calib) & 0x03).ToString();           //mag
                    //}

                    var off = 2;
                    var scalingFactor = (1.00 / (1 << 14));
                    float quatW, quatX, quatY, quatZ;
                    quatW = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                    quatX = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                    quatY = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));
                    quatZ = (float)scalingFactor * ((Int16)(data[off + 6] | (data[off + 7] << 8)));

                    off = 10;
                    scalingFactor = (1.00 / 100.0);// 1m/s^2 = 100 LSB 
                    float x_a, y_a, z_a;
                    x_a = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                    y_a = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                    z_a = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));

                    off = 16;
                    scalingFactor = (1.00 / 16.0);// 1dps = 16 LSB
                    float gyrx, gyry, gyrz;
                    gyrx = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                    gyry = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                    gyrz = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));


                    lock (m_DataLock)
                    {
                        //dataToPlot[id * 3 + 0] = x_a;
                        //dataToPlot[id * 3 + 1] = y_a;
                        //dataToPlot[id * 3 + 2] = z_a;
                        //dataToPlot = new float[] { quatW, quatX, quatY, quatZ, x_a, y_a, z_a, gyrx, gyry, gyrz };
                        //SendIMUData(id, quatW, quatX, quatY, quatZ, x_a, y_a, z_a, gyrx, gyry, gyrz );
                        SendIMUData(id, calib, new float[] { quatW, quatX, quatY, quatZ, x_a, y_a, z_a, gyrx, gyry, gyrz });
                    }
                    //stringToSave = id.ToString() + " " + Calibration[0] + " " + Calibration[1] + " " + Calibration[2] + " " + Calibration[3] + " " + quatW.ToString("0.0000") + " " + quatX.ToString("0.0000") + " " + quatY.ToString("0.0000") + " " + quatZ.ToString("0.0000") + " "
                    //    + x_a.ToString("0.0000") + " " + y_a.ToString("0.0000") + " " + z_a.ToString("0.0000") + " " + gyrx.ToString("0.0000") + " " + gyry.ToString("0.0000") + " " + gyrz.ToString("0.0000");

                    //if (IsStreaming && _tcpStreamer != null)
                    //{
                    //    _tcpStreamer.sendDataTCP(data);
                    //}
                    //if (IsSaving && _dataSaver != null)
                    //{
                    //    stringToSave = id.ToString() + " " + Calibration[0] + " " + Calibration[1] + " " + Calibration[2] + " " + Calibration[3] + " " + quatW.ToString("0.0000") + " " + quatX.ToString("0.0000") + " " + quatY.ToString("0.0000") + " " + quatZ.ToString("0.0000") + " "
                    //    + x_a.ToString("0.0000") + " " + y_a.ToString("0.0000") + " " + z_a.ToString("0.0000") + " " + gyrx.ToString("0.0000") + " " + gyry.ToString("0.0000") + " " + gyrz.ToString("0.0000");
                    //    _dataSaver.addData(stringToSave);
                    //}
                    //if (isPlotting)
                    //{
                    //    OnaccelerationData(x_a, y_a, z_a, gyrx, gyry, gyrz);
                    //}
                }
                catch (System.Exception e)
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

        private async void WriteToBLEDevice(BLEDeviceInformation device, String data)
        {
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
            //var result = await _deviceInformation.BLEuartCharacteristic.
            //GattWriteResult result = await _deviceInformation.BLEuartCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());
            //Debug.WriteLine(result.ProtocolError.ToString()); Debug.WriteLine(result.Status.ToString());
            //if (result == GattWriteResult.)
            //{
            //    Console.WriteLine(" Successfully wrote to device");
            //}
        }

        private void BLEConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            //BLEDeviceInformation device = null;
            //foreach (var dev in ConnectedDeviceInformationList)
            //{
            //    if (dev.Name.Equals(sender.Name))
            //    {
            //        device = dev;
            //        break;
            //    }
            //}

            //Console.WriteLine("Connection Status changed: " + sender.ConnectionStatus);
            var connected = sender.ConnectionStatus.ToString() == "Connected" ? true : false;
            OnStatusChanged(sender.ConnectionStatus.ToString() + " to " + sender.Name);


            if (connected)
            {

            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    //Calibration[i] = "0";
                }
            }

            ConnectedChangedEventArgs e = new ConnectedChangedEventArgs();
            e.deviceInformation = DeviceInformation;
            e.status = connected;
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
            e.deviceInformation = DeviceInformation;
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

        protected virtual void SendIMUData(int id, byte calib, float[] data ) //float quatW, float quatX, float quatY, float quatZ, float x, float y, float z, float gx, float gy, float gz)
        {
            imuDataEventArgs e = new imuDataEventArgs();
            e.id = id;
            e.data = data;
            e.calib = calib;
            //e.quatW = quatW;
            //e.quatX = quatX;
            //e.quatY = quatY;
            //e.quatZ = quatZ;
            //e.Accx = x;
            //e.Accy = y;
            //e.Accz = z;
            //e.Gyrx = gx;
            //e.Gyry = gy;
            //e.Gyrz = gz;
            EventHandler<imuDataEventArgs> handler = sendData;
            if (handler != null)
            {
                handler(this, e);
            }
        }

    }
}
