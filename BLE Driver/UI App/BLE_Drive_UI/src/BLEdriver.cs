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

/// <summary>
/// This class provides a hub for all connected BLE devices. It mainly manages incoming data in which it interprets, forwards or computes it any additional manner.
/// </summary>

namespace BLE_Drive_UI.src
{
    class BLEdriver
    {
        //List which contains all BLE devices which are connected to this driver
        public List<BLEDeviceInformation> ConnectedDeviceInformationList;

        //Boolean state variables
        public bool IsStreaming { get; private set; }
        public bool IsPlotting { get; private set; }
        public bool IsSaving { get; private set; }
        public bool Busy;

        //Not boolean state variable. On requesting Connection status, number of connected device will be returned
        public int Connected
        {
            get
            {
                return ConnectedDeviceInformationList.Count;
            }
        }
        
        //Static variable
        private static UInt16 _packetsize = 21;             //Number of Bytes in an incoming BLE packet
        //private static UInt16 _datapoints = 6;              //Number of Datapoints to Plot on GUI
        private static UInt16 _writeBuffersize = 200;       //Number of measurements saved in a .txt file
        private static UInt16 _writeBufferRate = 10;        //Rate in ms in which the synchronous datasaver saves the measurements

        //Events
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler<AddLogEntryEventArgs> WriteLogEntry;
        public event EventHandler SelectedDeviceFound;
        public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;

        //Mutex objects
        private object m_DataLock = new object();
        private object m_DataLockBatt = new object();
        private object m_DataLockCalib = new object();

        //Datasaver Object, which handles saving of incoming data to .txt files
        private SyncDataSaver _dataSaver;
        //TCPStreamer Object, which handles forwarding of the incoming data via tcp connection
        private TCPStreamer _tcpStreamer;

        public BLEdriver()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            //These Values are set by the user over the GUI, so initially both are false.
            IsSaving = false;
            IsStreaming = false;

            //TODO: Create this on GUI input like TCP streamer
            _dataSaver = new SyncDataSaver(_writeBuffersize, _writeBufferRate);

            // Create List for connected devices
            ConnectedDeviceInformationList = new List<BLEDeviceInformation>();
        }


        ~BLEdriver()
        {
            if (IsSaving)
                StopSaving();
            if (IsStreaming)
                StopStreaming();
        }

        /// <summary>
        /// This function creates the TCPStreamer and initializes a connection
        /// </summary>
        public void StartStreaming()
        {
            //Check if object already exists
            if (_tcpStreamer == null)
            {
                //If not, create it and register eventhandlers
                _tcpStreamer = new TCPStreamer();
                _tcpStreamer.ConnectedChanged += TCPConnectionStatusChangedEvent;
                _tcpStreamer.StatusChanged += StatusChanged;
            }
            //Start Client in new Thread so it does not freeze GUI while looking for a connection
            Thread InitTCPThread = new Thread(_tcpStreamer.StartTCPClient);
            InitTCPThread.Start();
        }

        /// <summary>
        /// This function stops streaming of data via TCP connection and removes the TCPStreamer Object
        /// </summary>
        public void StopStreaming()
        {
            //Check if object exists
            if (_tcpStreamer == null)
            {
                //Close client and deregister eventhandlers
                _tcpStreamer.CloseTCPClient();
                _tcpStreamer.ConnectedChanged -= TCPConnectionStatusChangedEvent;
                _tcpStreamer.StatusChanged -= StatusChanged;
                _tcpStreamer = null;
            }
            
        }

        /// <summary>
        /// This function Initializes the synchronous Datasaver
        /// </summary>
        public void StartSaving()
        {
            //Check if datasaver is currently active. TODO: Check if it exists and react accordignly, like the TCPStreamer
            if(!_dataSaver.Active)
            {   
                IsSaving = true;    //TODO: Set Bool flag asynchronously, like tcp streamer?
                //Start saving
                _dataSaver.start(); 
            }
        }

        /// <summary>
        /// This functions stops the synchronous Datasaver
        /// </summary>
        public void StopSaving()
        {
            if (_dataSaver.Active)
            {
                _dataSaver.stop();
                IsSaving = false;
            }
        }

        /// <summary>
        /// This awaitable function allows to disconnect from a Device. Since in BLE usually the Server(The participant who streams data) can cut a connection, a command word is sent to the device and the disconnect event is performed Server sided
        /// </summary>
        /// <param name="deviceInformation">Device to disconnect</param>
        public async void Disconnect(BLEDeviceInformation deviceInformation)
        {
            try
            {
                //Notify user
                OnStatusChanged("Disconnecting " + deviceInformation.Name + "...");
                //Try writing the command word "Disconnect" to the device
                await WriteToBLEDevice(deviceInformation, "Disconnect");
            }
            // In case something is occupying the necessary utilities
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                //Wait and call this function recursively
                Thread.Sleep(300);
                Disconnect(deviceInformation);
            }
        }

        /// <summary>
        /// This awaitable functions allows to save the currently used Calibration value to the IMU and overwrite its default values. They are saved locally on the device until owerwritten again
        /// </summary>
        /// <param name="device">Device to recalibrate</param>
        public async void Recalibrate_imu(BLEDeviceInformation deviceInformation)
        {
            try
            {
                //Notify user
                OnStatusChanged("Recalibrating " + deviceInformation.Name + "...");
                //Try writing "Recalibrate" command
                await WriteToBLEDevice(deviceInformation, "Recalibrate");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Retrying...");
                //wait and retry recursively if writing to device failed
                Thread.Sleep(300);
                Recalibrate_imu(deviceInformation);
            }
        }


        //BLE Related functions
        /// <summary>
        /// This functions allows connecting to a device corresponding to the passed deviceinformation
        /// </summary>
        /// <param name="deviceInformation">Device to connect</param>
        public async void ConnectDevice(BLEDeviceInformation deviceInformation)
        {
            //Check if device already Connected
            if (ConnectedDeviceInformationList.Contains(deviceInformation))
            {
                OnStatusChanged("Device " + deviceInformation.Name + " already connected");
                return;
            }
            //Set Busy flag to prevent abuse of connect button on GUI
            Busy = true;

            BluetoothLEDevice BLEDevice;
            OnStatusChanged("Initializing");
            try
            {
                //Try to initialize a BLEDevice object from passed deviceinformation and register "Connected" Event to handler
                BLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.BLEId);
                BLEDevice.ConnectionStatusChanged += BLEConnectionStatusChangedEvent;
            }
            catch(Exception e)
            {
                OnStatusChanged("Failed to connect");
                throw new Exception("Failed to Connect: ", e);
            }
            OnStatusChanged("Connecting...");

            //Read Provides Services from device
            GattDeviceServicesResult service_result = await BLEDevice.GetGattServicesAsync();

            //If Gatt protocol is provided
            if (service_result.Status == GattCommunicationStatus.Success)
            {
                //iterate through all services
                foreach (GattDeviceService serv in service_result.Services)
                {
                    //Read characteristics from their corresponding service
                    GattCharacteristicsResult char_result = await serv.GetCharacteristicsAsync();

                    //Iterate through the characteristics
                    foreach (GattCharacteristic characteristic in char_result.Characteristics)
                    {
                        //Read their Properties
                        var properties = characteristic.CharacteristicProperties;
                        //Service contains the custom BLEUART UUId
                        if (serv.Uuid == new Guid(BLEUUID.BLEUART_CUSTOM_UUID))
                        {
                            //Communication via Notification (Fastest - fire and forget) is possible
                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                //Save this characteristic in deviceInformation object
                                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    deviceInformation.BLEuartCharacteristic = characteristic;
                                }
                            }
                            //Service contains Write Flag
                            if (properties.HasFlag(GattCharacteristicProperties.Write))
                            {
                                //Save to deviceInformation object
                                deviceInformation.BLEuartCharacteristic_write = characteristic;
                            }
                        }
                        //Service contains the BatteryService UUID, which is needed to read the batteryvoltage
                        if (serv.Uuid == new Guid(BLEUUID.BLEUART_BATTERY_SERVICE))
                        {
                            //Communication via Notification (Fastest - fire and forget) is possible
                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                //Save this characteristic in deviceInformation object
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
                    //if all characteristics are found, register the notfication events to the eventhandler
                    if (deviceInformation.BatteryCharacteristic != null && deviceInformation.BLEuartCharacteristic != null)
                    {
                        deviceInformation.BatteryCharacteristic.ValueChanged += Characteristic_ValueChanged;
                        deviceInformation.BLEuartCharacteristic.ValueChanged += Characteristic_ValueChanged;
                    }
                    else
                    {
                        OnStatusChanged("Services not found. Try reconnecting");
                        Busy = false;
                        return;
                    }
                }
                catch (Exception e)
                {
                    OnStatusChanged("Services not found");
                    Console.WriteLine("Services not found: " + e.ToString());
                }
            }
            //If you made it, then the device is fully connected and you can add it to the list.
            ConnectedDeviceInformationList.Add(deviceInformation);

            //Reset Busy Flag
            Busy = false;
        }

        /// <summary>
        /// This callback function is raised every time, the driver receives a notification of one of the connected devices
        /// </summary>
        /// <param name="sender">Gattcharacteristics of device which raised event</param>
        /// <param name="args">Notification values/arguments</param>
        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            //Check which device from devicelist is calling this function
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var device = ConnectedDeviceInformationList.Find(i => string.Equals(i.BLEId, sender.Service.Session.DeviceId.Id));
            if(device == null)
            {
                Console.WriteLine("Unknown device");
                return;
            }

            //Battery value notification
            if (sender.Service.AttributeHandle == device.BatteryCharacteristic.Service.AttributeHandle)
            {
                
                lock (m_DataLockBatt)
                {
                    //Save batterylevel to corresponding object of the physical device
                    device.BatteryLevel = reader.ReadByte();
                }
            }
            //IMU data notification
            if (sender.Service.AttributeHandle == device.BLEuartCharacteristic.Service.AttributeHandle)
            {
                try
                { 
                    //Read exactly one packet
                    var packet = new Byte[_packetsize];
                    reader.ReadBytes(packet);

                    //Interpret Calibration values
                    int[] calib = new int[4];
                    calib[0] = ((packet[0] >> 6) & 0x03);      //sys
                    calib[1] = ((packet[0] >> 4) & 0x03);      //gyr
                    calib[2] = ((packet[0] >> 2) & 0x03);      //acc
                    calib[3] = ((packet[0]) & 0x03);           //mag

                    //Write Calibration value to corresponding object
                    device.Calibration = calib;
                    
                    //Read Quaternion data
                    var off = 1;
                    var scalingFactor = (1.00 / (1 << 14));
                    float quatW, quatX, quatY, quatZ;
                    quatW = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    quatX = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    quatY = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));
                    quatZ = (float)scalingFactor * ((Int16)(packet[off + 6] | (packet[off + 7] << 8)));

                    //Read Acceleration data
                    off = 9;
                    scalingFactor = (1.00 / 100.0);// 1m/s^2 = 100 LSB 
                    float x_a, y_a, z_a;
                    x_a = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    y_a = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    z_a = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));

                    //Read Gyroscope data
                    off = 15;
                    scalingFactor = (1.00 / 16.0);// 1dps = 16 LSB
                    float gyrx, gyry, gyrz;
                    gyrx = (float)scalingFactor * ((Int16)(packet[off + 0] | (packet[off + 1] << 8)));
                    gyry = (float)scalingFactor * ((Int16)(packet[off + 2] | (packet[off + 3] << 8)));
                    gyrz = (float)scalingFactor * ((Int16)(packet[off + 4] | (packet[off + 5] << 8)));

                    lock (m_DataLock)
                    {
                        //Write data to corresponding object
                        device.Data = new float[] { quatW, quatX, quatY, quatZ, x_a, y_a, z_a, gyrx, gyry, gyrz };
                    }

                    //Stream if flag was set by user
                    if (IsStreaming && _tcpStreamer != null)
                    {
                        _tcpStreamer.sendDataTCP(device.SensorID,device.Data);
                    }
                    //Save data to .txt file if flag was set by user
                    if (IsSaving && _dataSaver != null)
                    {
                        _dataSaver.addData(device.SensorID,device.GetDataAsString());
                    }
                }
                catch(System.Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        /// <summary>
        /// Write Byte array to BLE device. Outdated, needs rework
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data"></param>
        private async void WriteToBLEDevice(BLEDeviceInformation device, Byte[] data)
        {
            if (device == null || device.BLEuartCharacteristic_write == null)
            {
                return;
            }
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            GattCommunicationStatus result = await device.BLEuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
            if (result == GattCommunicationStatus.Success)
            {
                Console.WriteLine(" Successfully wrote to device");
            }
        }

        /// <summary>
        /// Write String to BLE device
        /// </summary>
        /// <param name="device">Device to write to</param>
        /// <param name="data">String to write</param>
        /// <returns></returns>
        private async Task WriteToBLEDevice(BLEDeviceInformation device, String data)
        {
            //Check if device exists and is writable
            if(device == null || device.BLEuartCharacteristic_write == null)
            {
                return;
            }
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            try
            {
                //Try writing buffer
                GattCommunicationStatus result = await device.BLEuartCharacteristic_write.WriteValueAsync(writer.DetachBuffer());
            }
            //Catch and throw exception if writing mechanism is occupied
            catch (System.UnauthorizedAccessException e)
            {
                Console.WriteLine("Error Writing to BLE Device: ");
                Console.WriteLine(e.Message);
                throw new System.UnauthorizedAccessException();
            }
        }

        int err_count = 0;
        /// <summary>
        /// Callback function which is called after a device was connected or disconnected
        /// </summary>
        /// <param name="sender">Device which called this function</param>
        /// <param name="args">Empty</param>
        private void BLEConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            BLEDeviceInformation deviceInformation = null;
            // Change this. Sometimes this event is raised before the connect function finishes. So initially the list is empty. Here we wait for it to fill.
            while (deviceInformation == null)
            {
                deviceInformation = ConnectedDeviceInformationList.Find(item => item.Name == sender.Name);
                Thread.Sleep(5);
                if (err_count > 100) return;
            }
            err_count = 0;
            // Check if this function was called due to connecting or disconnecting
            var connected = sender.ConnectionStatus.ToString() == "Connected" ? true : false;
            OnStatusChanged(sender.ConnectionStatus.ToString()+ " "+ sender.Name);

            //Prepare event to update GUI
            ConnectedChangedEventArgs e = new ConnectedChangedEventArgs();
            e.DeviceInformation = deviceInformation;
            e.Status = connected;

            if (connected)
            {

            }
            else
            {
                //Dispose BLE device object and remove it from devicelist
                sender.ConnectionStatusChanged -= BLEConnectionStatusChangedEvent;
                sender.Dispose();
                ConnectedDeviceInformationList.Remove(deviceInformation);
                _dataSaver?.addData(deviceInformation.SensorID,String.Empty);
                deviceInformation = null;
            }

            //Raise event to notify user via Mainwindow
            EventHandler<ConnectedChangedEventArgs> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, e);
            }

        }


        //Eventhandler
        /// <summary>
        /// Use this function to notify user via the GUI and to write messages to Logfile
        /// </summary>
        /// <param name="status">Notification string</param>
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

        //protected virtual void OnSelectedDeviceFound()
        //{
        //    EventHandler handler = SelectedDeviceFound;
        //    if(handler != null)
        //    {
        //        handler(this,EventArgs.Empty);
        //    }
        //}

        /// <summary>
        /// Use this function to write something to the logfile
        /// </summary>
        /// <param name="entry">Logfile entry</param>
        protected virtual void OnWriteLogEntry(String entry)
        {
            AddLogEntryEventArgs e = new AddLogEntryEventArgs();
            e.LogEntry = entry;
            EventHandler<AddLogEntryEventArgs> handler = WriteLogEntry;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Callback function which is raised if a TCP connection was succesfully established or closed.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Contians connection status</param>
        private void TCPConnectionStatusChangedEvent(object sender, TcpConnectEventArgs e)
        {
            IsStreaming = e.Connected;
        }


    }


 
}
