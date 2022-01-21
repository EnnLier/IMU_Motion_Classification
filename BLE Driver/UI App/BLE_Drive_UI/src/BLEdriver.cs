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
        public BLEdevice _deviceInformation { get; set; } 
        public bool isPlotting { get; set;}
        public bool isStreaming { get; set; }
        public bool isSaving; 

        public bool Connected;
        public bool _busy;
        public UInt16 BatteryLevel;

        private static UInt16 _packetsize = 22;
        private static UInt16 _datapoints = 6;              //to Plot
        private static UInt16 _writeBuffersize = 200;
        private static UInt16 _writeBufferRate = 10;

        private System.Timers.Timer UpdateMainwindowTimer = new System.Timers.Timer();

        public event EventHandler<statusChangedEventArgs> StatusChanged;
        public event EventHandler<changeLabelEventArgs> ChangeLabel;
        public event EventHandler<imuDataEventArgs> UpdateChart;
        public event EventHandler SelectedDeviceFound;

        private object m_DataLock = new object();

        private IPHostEntry _host;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;
        private Socket _sender;

        //private doubleBuffer _doubleBuffer;
        //public DoubleDataBufferAsync _doubleBuffer;
        public SyncDataSaver _dataSaver;

        private string[] calibration;
        private static string[] elements = new string[] {"sys", "gyr", "acc", "mag" };

        public float[] dataToPlot;
        public String stringToSave = String.Empty;

        public BLEdriver()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Connected = false;
            isSaving = false;
            isStreaming = false;
            _dataSaver = new SyncDataSaver(_writeBuffersize, _writeBufferRate);

            calibration = new string[] {"0","0","0","0"};
            dataToPlot = new float[_datapoints];

            UpdateMainwindowTimer.Elapsed += new ElapsedEventHandler(UpdateMainwindow);
            UpdateMainwindowTimer.Interval = 50;
            UpdateMainwindowTimer.Enabled = true;
            UpdateMainwindowTimer.Start();

        }

        ~BLEdriver()
        {
            if(isSaving)
                flushBuffer();
        }

        public float[] GetDataToPlot()
        {
            lock(m_DataLock)
            {
                return dataToPlot;
            }
        }

        public void startSaving()
        {
            if(!_dataSaver.Active)
            {
                isSaving = true;
                _dataSaver.start();
            }
        }

        public void stopSaving()
        {
            if (_dataSaver.Active)
            {
                _dataSaver.stop();
                isSaving = false;
            }
        }

        public void StartClient()
        {
            if(_sender != null) { return;}
            byte[] bytes = new byte[1024];
            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                _host = Dns.GetHostEntry("localhost");
                _ipAddress = _host.AddressList[0];
                _remoteEP = new IPEndPoint(_ipAddress, 11000);

                // Create a TCP/IP  socket.    
                _sender =new Socket(_ipAddress.AddressFamily,SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    _sender.Connect(_remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        _sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    //byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
                    //byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
                    // Send the data through the socket.    
                    //int bytesSent = _sender.Send(msg);
                    // Receive the response from the remote device.    
                    //int bytesRec = _sender.Receive(bytes);
                    //Console.WriteLine("Echoed test = {0}",Encoding.ASCII.GetString(bytes, 0, bytesRec));
                    OnStatusChanged("TCP Client Connected");

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    OnStatusChanged("TCP Client Connection Failed");
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    OnStatusChanged("TCP Client Connection Failed");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    OnStatusChanged("TCP Client Connection Failed");
                }

            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
                OnStatusChanged("TCP Client Connection Failed");
            }
        }

        public void CloseClient()
        {
            // Release the socket.    
            if (_sender != null)
            { 
                sendData(ToOutgoingPacket(new byte[]{ 0x04}, 1));
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
                _sender.Dispose();
                _sender = null;
                OnStatusChanged("TCP Client Connection Closed");
            }
        }

        public void recalibrate_imu()
        {
            WriteToBLEDevice("Recalibrate");
        }

        private void sendData(String data)
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

        private void sendData(Byte[] data)
        {
            try
            {
                if (_sender.Connected)
                {
                    int bytesSent = _sender.Send(data);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private String ToOutgoingPacket(String stringBuffer)
        {
            stringBuffer.Insert(0, Encoding.Default.GetString(new Byte[] { 0x55 }));
            //stringBuffer.Insert(11, Encoding.Default.GetString('\r'));
            stringBuffer += '\r';
            stringBuffer += '\n';
            return stringBuffer;
        }

        private Byte[] ToOutgoingPacket(Byte[] stringBuffer, UInt16 len)
        {
            var res = new Byte[len + 1];
            res[0] = 0x55;
            stringBuffer.CopyTo(res, 1);

            return res;
        }

        public async void ConnectDevice(BLEdevice deviceInformation)
        {
            _busy = true;
            _deviceInformation = deviceInformation;
            OnStatusChanged("Initializing");
            try
            {
                _BLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

                _BLEDevice.ConnectionStatusChanged += ConnectionStatusChangedEvent;
                _BLEDevice.GattServicesChanged += GattServicesChangedEvent;
                _BLEDevice.NameChanged += NameChangedEvent;

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
                                    _deviceInformation.BLEuartCharacteristic = characteristic;
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
                                _deviceInformation.BLEuartCharacteristic_write = characteristic;
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
                                    _deviceInformation.BatteryCharacteristic = characteristic;
                                }
                            }
                        }
                    }
                        
                }
                try
                {
                    if (_deviceInformation.BatteryCharacteristic != null && _deviceInformation.BLEuartCharacteristic != null)
                    {
                        _deviceInformation.BatteryCharacteristic.ValueChanged += Characteristic_ValueChanged;
                        _deviceInformation.BLEuartCharacteristic.ValueChanged += Characteristic_ValueChanged;
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

        }


        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            if (_deviceInformation.BatteryCharacteristic == null || _deviceInformation.BLEuartCharacteristic == null) { return; }
            if (sender.Service.AttributeHandle == _deviceInformation.BatteryCharacteristic.Service.AttributeHandle)
            {
                BatteryLevel = reader.ReadByte();
                Console.WriteLine(BatteryLevel);
            }
            if(sender.Service.AttributeHandle == _deviceInformation.BLEuartCharacteristic.Service.AttributeHandle)
            {
                var data = new Byte[_packetsize];                
                reader.ReadBytes(data);
                                
                var id = data[0];
                var calib = data[1];

                calibration[0] = ((calib >> 6) & 0x03).ToString();      //sys
                calibration[1] = ((calib >> 4) & 0x03).ToString();      //gyr
                calibration[2] = ((calib >> 2) & 0x03).ToString();      //acc
                calibration[3] = ((calib) & 0x03).ToString();           //mag

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


                lock(m_DataLock)
                {
                    dataToPlot = new float[] { x_a, y_a, z_a, gyrx, gyry, gyrz };
                }

                if (_sender != null)
                {
                    sendData(ToOutgoingPacket(data, _packetsize));
                }
                if (isSaving)
                {
                    stringToSave = id.ToString() + " " + calibration[0] + " " + calibration[1] + " " + calibration[2] + " " + calibration[3] + " " + quatW.ToString("0.0000") + " " + quatX.ToString("0.0000") + " " + quatY.ToString("0.0000") + " " + quatZ.ToString("0.0000") + " "
                    + x_a.ToString("0.0000") + " " + y_a.ToString("0.0000") + " " + z_a.ToString("0.0000") + " " + gyrx.ToString("0.0000") + " " + gyry.ToString("0.0000") + " " + gyrz.ToString("0.0000");
                    _dataSaver.addData(stringToSave);
                }
                //if (isPlotting)
                //{
                //    OnaccelerationData(x_a, y_a, z_a, gyrx, gyry, gyrz);
                //}

            }

        }


        public async void WriteToBLEDevice(Byte[] data)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            GattCommunicationStatus result = await _deviceInformation.BLEuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
            if (result == GattCommunicationStatus.Success)
            {
                Console.WriteLine(" Successfully wrote to device");
            }
        }
        public async void WriteToBLEDevice(String data)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            //Debug.WriteLine(_deviceInformation.BLEuartCharacteristic_write.CharacteristicProperties);
            
            GattCommunicationStatus result = await _deviceInformation.BLEuartCharacteristic_write.WriteValueAsync(writer.DetachBuffer());
            //var result = await _deviceInformation.BLEuartCharacteristic.
            //GattWriteResult result = await _deviceInformation.BLEuartCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());
            //Debug.WriteLine(result.ProtocolError.ToString()); Debug.WriteLine(result.Status.ToString());
            //if (result == GattWriteResult.)
            //{
            //    Console.WriteLine(" Successfully wrote to device");
            //}
        }

        private String ClearStringBuffer(String stringBuffer)
        {
            stringBuffer = String.Empty;
            return Encoding.Default.GetString(new Byte[] { 0x55 });
        }

        public bool flushBuffer()
        {
            try
            {
                _dataSaver.stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void NameChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Name changed: " + sender.Name);
        }

        private void GattServicesChangedEvent(BluetoothLEDevice sender, object args)
        {

        }

        private void ConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Connection Status changed: " + sender.ConnectionStatus);
            Connected = sender.ConnectionStatus.ToString() == "Connected" ? true : false;
            OnStatusChanged(sender.ConnectionStatus.ToString());
            if(!Connected)
            {
                for (int i = 0; i < 4; i++)
                    calibration[i] = "0";
            }
        }

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

        protected virtual void OnChangeLabel(String label, String value)
        {
            changeLabelEventArgs e = new changeLabelEventArgs();
            e.label = label;
            e.value = value;
            EventHandler<changeLabelEventArgs> handler = ChangeLabel;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnaccelerationData(float x, float y, float z, float gx, float gy, float gz)
        {
            imuDataEventArgs e = new imuDataEventArgs();
            e.Accx = x;
            e.Accy = y;
            e.Accz = z;
            e.Gyrx = gx;
            e.Gyry = gy;
            e.Gyrz = gz;
            EventHandler<imuDataEventArgs> handler = UpdateChart;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnSelectedDeviceFound()
        {
            EventHandler handler = SelectedDeviceFound;
            if(handler != null)
            {
                handler(this,EventArgs.Empty);
            }
        }


        private void UpdateMainwindow(object source, ElapsedEventArgs e)
        {
            //Debug.WriteLine("Update");
            for (int i = 0; i < 4 ; i++)
                OnChangeLabel("l_"+elements[i],calibration[i]);
        }


    }
    public class statusChangedEventArgs : EventArgs
    {
        public String Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
    

    public class imuDataEventArgs : EventArgs
    {
        public float Accx { get; set; }
        public float Accy { get; set; }
        public float Accz { get; set; }
        public float Gyrx { get; set; }
        public float Gyry { get; set; }
        public float Gyrz { get; set; }
    }

    public class changeLabelEventArgs : EventArgs
    {
        public String label { get; set; }
        public String value { get; set; }
    }

    public class StringBuffer
    {
        private static UInt16 _bufferLength;
        private static String[] SBuffer;

        public UInt16 Count { get; private set; }
        public bool Active { get; set; }

    public StringBuffer(UInt16 bufferlength)
        {
            _bufferLength = bufferlength;

            SBuffer = new String[_bufferLength];

            Count = 1;
        }

        public void Add(String data)
        {
            if(Count <= _bufferLength)
            {
                SBuffer[Count-1] = data;
                Count++;
            }
            else
            {
                throw new Exception("StringBuffer Overflow");
            }
        }

        public String[] Flush()
        {
            var tmp = new String[_bufferLength];
            SBuffer.CopyTo(tmp,0);
            //Console.WriteLine(tmp[0]);
            Clear();
            Count = 1;
            return tmp;
        }

        public void Clear()
        {
            for(int i = 0; i < Count; i++)
            {
                SBuffer[i] = String.Empty;
            }
        }
    }

    public class SyncDataSaver
    {
        private static String _path;
        private static UInt16 _bufferLength;

        private StringBuffer buffer1;
        private StringBuffer buffer2;

        private static object mThreadLock = new object();

        private Thread Buffer1PollingThread;
        private Thread Buffer2PollingThread;
        private Stopwatch Watch;

        private static double _rate;
        private double cummulativeRate;

        private String dataToSave = String.Empty;
        
        public bool isSaving = false;

        public bool Active = false;

        public SyncDataSaver(UInt16 bufferLength, double rate)
        {

            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            _bufferLength = bufferLength;
            _rate = rate;

            buffer1 = new StringBuffer(_bufferLength);
            buffer2 = new StringBuffer(_bufferLength);

            buffer1.Active = true;
            buffer2.Active = false;

            Watch = new Stopwatch();

        }

        public void start()
        {
            Console.WriteLine("Start Saving....");
            if(Buffer1PollingThread != null || Buffer2PollingThread != null)
            {
                while (Buffer1PollingThread.IsAlive || Buffer2PollingThread.IsAlive)
                {
                    Console.WriteLine(" Threads still alive....");
                    Thread.Sleep(200);
                }
            }
            dataToSave = String.Empty;
            Active = true;
            isSaving = true;
            buffer1.Active = true;
            buffer2.Active = false;

            cummulativeRate = 0;

            Buffer1PollingThread = new Thread(Buffer1Polling);
            Buffer2PollingThread = new Thread(Buffer2Polling);

            Buffer1PollingThread.Start();
            Buffer2PollingThread.Start();



                Console.WriteLine("PollingThread started....");
        }

        public void stop()
        {
            isSaving = false;
            //dataToSave = String.Empty;
            flush();
            Watch.Reset();
            while(Buffer1PollingThread.IsAlive || Buffer2PollingThread.IsAlive) {; }     //Wait for Threads to finish
            Active = false;
        }


        public void addData(String data)
        {
            lock(mThreadLock)
            {
                dataToSave = data;
            }
        }

        private void Buffer1Polling()
        {
            while (dataToSave == String.Empty) {Thread.Sleep(5); }
            Watch.Start();
            while (isSaving)
            {
                if (!buffer1.Active) { var n = _rate / 5; Thread.Sleep((int)n); continue; }
                double t = Watch.Elapsed.TotalMilliseconds;
                if (t >= cummulativeRate)
                {
                    cummulativeRate += _rate;
                    String timestamp = (t / 1000).ToString("0.0000");
                    lock (mThreadLock)
                    {
                        buffer1.Add(timestamp + dataToSave);
                    } 
                    if (buffer1.Count >= _bufferLength)
                    {
                        buffer1.Active = false;
                        buffer2.Active = true;
                        save(buffer1);
                    }
                }
            }
        }

        private void Buffer2Polling()
        {
            while (isSaving)
            {
                if (!buffer2.Active) { var n = _rate / 5; Thread.Sleep((int)n); continue; }
                double t = Watch.Elapsed.TotalMilliseconds;
                if (t >= cummulativeRate)
                {
                    cummulativeRate += _rate;
                    String timestamp = (t/1000).ToString("0.0000");
                    lock (mThreadLock)
                    {
                        buffer2.Add(timestamp + dataToSave);
                    }
                    if (buffer2.Count >= _bufferLength)
                    {
                        buffer1.Active = true;
                        buffer2.Active = false;
                        save(buffer2);
                    }
                }
            }
        }

        //private void dataPolling()
        //{
        //    Console.WriteLine("Start Polling");
        //    double rate = _rate;
        //    while (isSaving)
        //    {
        //        double t = Watch.Elapsed.TotalMilliseconds;
        //        if(t >= rate)
        //        {
        //            //Console.WriteLine("5 ms");
        //            String timestamp = t.ToString("0.0000");
        //            if(buffer1.Active)
        //            {
        //                lock (mThreadLock)
        //                {
        //                    buffer1.Add(timestamp + dataToSave);
        //                }
        //                if (buffer1.Count >= _bufferLength && !buffer1.Busy)
        //                {
        //                    SavingThread.Start();
        //                }
        //            }
        //            else if (buffer2.Active)
        //            {
        //                lock (mThreadLock)
        //                {
        //                    buffer2.Add(timestamp + dataToSave);
        //                }
        //                if (buffer2.Count >= _bufferLength && !buffer2.Busy)
        //                {
        //                    SavingThread.Start();
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("No Buffer active in data saving class polling function");
        //            }
        //            rate += _rate;
        //        }
                
        //    }
        //}

        private bool busy = false;
        private void save(StringBuffer buffer)
        {
            busy = true;
            String filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            File.WriteAllLines(_path + filename + ".txt", buffer.Flush());
            busy = false;
            //if (i == 1)
            //{
            //    File.WriteAllLines(_path + filename + ".txt", buffer1.Flush());
            //}
            //else if (i == 2)
            //{
            //    File.WriteAllLines(_path + filename + ".txt", buffer2.Flush());
            //}
            //else
            //{
            //    throw new Exception("No Buffer active in data saving Class Save function");
            //}
        }

        private void flush()
        {
            while (busy) { Thread.Sleep(20);}
            if(buffer1.Active)
            {
                save(buffer1);
            }
            else if (buffer2.Active)
            {
                save(buffer2);
            }
            //while (SavingThread.IsAlive) { }
            //SavingThread.Start();
        }
    }

    public class DoubleDataBufferAsync
    {
        //private float[][] fBuffer1;
        //private float[][] fBuffer2;

        //private static List<String> sBuffer1 = new List<String>();
        //private static List<String> sBuffer2 = new List<String>();
        private static bool _oneActive = true;
        private static bool _twoActive = false;

        private static String _path;
        private static UInt16 _bufferLength;
        //private static UInt16 _dataPoints;

        //private UInt16 _fBufC1;
        //private UInt16 _fBufC2;
        private static UInt16 _sBufC1;
        private static UInt16 _sBufC2;

        private static String[] sBuffer1;
        private static String[] sBuffer2;

        private static object mThreadLock = new object();

        public DoubleDataBufferAsync(UInt16 bufferLength)//, UInt16 dataPoints)
        {
            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            //_dataPoints = dataPoints;
            _bufferLength = bufferLength;
            _oneActive = true;
            _twoActive = false;

            //_fBufC1 = 0;
            //_fBufC2 = 0;

            _sBufC1 = 0;
            _sBufC2 = 0;

            //fBuffer1 = new float[_bufferLength][];
            //fBuffer2 = new float[_bufferLength][];

            sBuffer1 = new String[_bufferLength];
            sBuffer2 = new String[_bufferLength];
        }

        //public async void addData(float[] data)
        //{
        //    if (_oneActive) // Buffer 1 active
        //    {
        //        //Console.WriteLine(Buffer1.Count);
        //        fBuffer1[_fBufC1] = data;
        //        _fBufC1++;
        //        if (_fBufC1 >= _bufferLength)
        //        {
        //            await save();
        //            _twoActive = true;
        //            _oneActive = false;
        //            _fBufC1 = 0;
        //        }
        //    }
        //    else if (_twoActive)
        //    {
        //        fBuffer2[_fBufC2] = data;
        //      _fBufC2++;
        //        if (_fBufC2 >= _bufferLength)
        //        {
        //            await save();
        //            _oneActive = true;
        //            _twoActive = false;
        //            _fBufC2 = 0;
        //        }
        //    }
        //}

        public async void addData(String data)
        {
            var t = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff");
            String toSave = t + " " + data; // + "\n";

            if (_oneActive) // Buffer 1 active
            {
                //Console.WriteLine(Buffer1.Count);
                sBuffer1[_sBufC1] = toSave;
                _sBufC1++;
                if (_sBufC1 >= _bufferLength)
                {
                    _twoActive = true;
                    _oneActive = false;
                    _sBufC1 = 0;
                    await save(sBuffer1);
                }
            }
            else if (_twoActive)
            {
                sBuffer2[_sBufC2] = toSave;
                _sBufC2++;
                if (_sBufC2 >= _bufferLength)
                {
                    _oneActive = true;
                    _twoActive = false;
                    _sBufC2 = 0;
                    await save(sBuffer2);
                }
            }
        }

        static async Task save(String[] buffer)
        {
            //Console.WriteLine("Save from " + Thread.CurrentThread.ManagedThreadId);
            String name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                await Task.Run(() => {
                    File.WriteAllLines(_path + name + ".txt", buffer);
                });
            buffer = new String[]{ };
        }

        static async Task save()
        {
            if (_oneActive)
            {
                await save(sBuffer1);
            }
            else if (_twoActive)
            {
                await save(sBuffer2);
            }
            _oneActive = true;
            _twoActive = false;
            _sBufC2 = 0;
            _sBufC1 = 0;
        }


        public async void flush()
        {
            await save();
        }
    }

}
