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
        public int[] Calibration = new int[] {0,0,0,0};

        public BLEDevice()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Connected = false;

            //Calibration = new string[] { "0", "0", "0", "0" };
            Data = new float[_datapoints];
        }

        private BluetoothLEDevice _bluetoothLEDevice;

        public bool Connected;
        public bool Busy;  

        public int BatteryLevel = 0;

        private static UInt16 _packetsize = 21;
        private static UInt16 _datapoints = 10;              

        public event EventHandler<statusChangedEventArgs> StatusChanged;
        public event EventHandler<imuDataEventArgs> sendData;
        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;

        private object m_DataLock = new object();
        private object m_DataLockBatt = new object();
        private object m_DataLockCalib = new object();

        public float[] Data;
        public String stringToSave = String.Empty;

        public float[] GetDataToPlot()
        {
            lock (m_DataLock)
            {
                return new float[] { Data[4], Data[5], Data[6] };
            }
        }

        public void Disconnect()
        {
            try
            {
                OnStatusChanged("Disconnecting " + DeviceInformation.Name + "...");
                WriteToBLEDevice("Disconnect");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                Disconnect();
            }
        }

        public void Recalibrate_imu()
        {
            try
            {
                WriteToBLEDevice("Recalibrate");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                Thread.Sleep(300);
                Recalibrate_imu();
            }
        }

        //BLE Related functions
        public async void Connect(BLEDeviceInformation deviceInformation)
        {
            //if (ConnectedDeviceInformationList.Contains(deviceInformation))
            //{
            //    OnStatusChanged("Device " + deviceInformation.Name + " already connected");
            //    return;
            //}
            //Busy = true;

            DeviceInformation = deviceInformation;

            OnStatusChanged("Initializing");
            try
            {
                _bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.BLEId);

                _bluetoothLEDevice.ConnectionStatusChanged += BLEConnectionStatusChangedEvent;
                //_BLEDevice.GattServicesChanged += GattServicesChangedEvent;
                //_BLEDevice.NameChanged += NameChangedEvent;

            }
            catch (Exception e)
            {

                OnStatusChanged("Failed to connect");
                //Busy = false;
                throw new Exception("Failed to Connect: ", e);
            }
            OnStatusChanged("Connecting...");


            GattDeviceServicesResult service_result = await _bluetoothLEDevice.GetGattServicesAsync();

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
                    //Busy = false;
                }
            }

            //ConnectedDeviceInformationList.Add(deviceInfomation);
            //return true;
            //Busy = false;
            Connected = true;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);

            if (sender.Service.AttributeHandle == DeviceInformation.BatteryCharacteristic.Service.AttributeHandle)
            {
                lock (m_DataLockBatt)
                {
                    BatteryLevel = reader.ReadByte();
                }
            }
            if (sender.Service.AttributeHandle == DeviceInformation.BLEuartCharacteristic.Service.AttributeHandle)
            {
                try
                {
                    var data = new Byte[_packetsize];
                    reader.ReadBytes(data);

                    byte calib = data[0];

                    lock (m_DataLockCalib)
                    {
                        Calibration = new int[] { (calib >> 6) & 0x03, (calib >> 4) & 0x03, (calib >> 2) & 0x03, (calib) & 0x03 };
                    }
                    lock (m_DataLock)
                    {
                        var off = 1;
                        var scalingFactor = (1.00 / (1 << 14));
                        //float quatW, quatX, quatY, quatZ;
                        Data[0] = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                        Data[1] = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                        Data[2] = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));
                        Data[3] = (float)scalingFactor * ((Int16)(data[off + 6] | (data[off + 7] << 8)));

                        off = 9;
                        scalingFactor = (1.00 / 100.0);// 1m/s^2 = 100 LSB 
                        //float x_a, y_a, z_a;
                        Data[4] = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                        Data[5] = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                        Data[6] = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));

                        off = 15;
                        scalingFactor = (1.00 / 16.0);// 1dps = 16 LSB
                        //float gyrx, gyry, gyrz;
                        Data[7] = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
                        Data[8] = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
                        Data[9] = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));

                        SendIMUData(Data);
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        private async void WriteToBLEDevice(String data)
        {
            if(DeviceInformation.BLEuartCharacteristic_write == null || !Connected)
            {
                return;
            }
            //Busy = true;
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            
            //Debug.WriteLine(_deviceInformation.BLEuartCharacteristic_write.CharacteristicProperties);
            try
            {
                GattCommunicationStatus result = await DeviceInformation.BLEuartCharacteristic_write.WriteValueAsync(writer.DetachBuffer());
                //Busy = false;
            }
            catch (Exception e)
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

        protected virtual void BLEConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Connection Status changed: " + sender.ConnectionStatus);
            var connectionStatus = sender.ConnectionStatus.ToString() == "Connected" ? true : false;
            OnStatusChanged(sender.ConnectionStatus.ToString() + ": " + sender.Name);


            if (!connectionStatus)
            {
                for (int i = 0; i < 4; i++)
                {
                    Calibration = new int[]{0,0,0,0};
                }
                _bluetoothLEDevice.ConnectionStatusChanged -= BLEConnectionStatusChangedEvent;
                DeviceInformation.BatteryCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                DeviceInformation.BLEuartCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                Connected = false;
            }

            ConnectedChangedEventArgs e = new ConnectedChangedEventArgs();
            e.deviceInformation = DeviceInformation;
            e.status = connectionStatus;
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

        protected virtual void SendIMUData(float[] data ) //float quatW, float quatX, float quatY, float quatZ, float x, float y, float z, float gx, float gy, float gz)
        {
            imuDataEventArgs e = new imuDataEventArgs();
            e.data = data;
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
