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

namespace BLE_Drive_UI.src
{
    class BLEdriver
    {
        private BluetoothLEDevice _BLEDevice;
        public BLEdevice _deviceInformation { get; set; } 
        public bool isSaving { get; set;}

        public bool Connected;
        public bool _busy;
        public UInt16 BatteryLevel;

        private static UInt16 _packetsize = 16;
        //private Byte[] _Buffer1 = new byte[20*_packetsize];
        //private Byte[] _Buffer2 = new byte[20 * _packetsize];
        private String _stringBuffer1 = String.Empty;
        private String _stringBuffer2 = String.Empty;



        public event EventHandler<statusChangedEventArgs> StatusChanged;
        public event EventHandler SelectedDeviceFound;


        private IPHostEntry _host;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;
        private Socket _sender;

        //private doubleBuffer _doubleBuffer;
        public doubleBuffer _doubleBuffer;

        public BLEdriver()
        {
            //ClearStringBuffer();
            Connected = false;
            isSaving = false;
            _doubleBuffer = new doubleBuffer();
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
                    byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
                    // Send the data through the socket.    
                    //int bytesSent = _sender.Send(msg);
                    // Receive the response from the remote device.    
                    //int bytesRec = _sender.Receive(bytes);
                    //Console.WriteLine("Echoed test = {0}",Encoding.ASCII.GetString(bytes, 0, bytesRec));

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private void CloseClient()
        {
            // Release the socket.    
            _sender.Shutdown(SocketShutdown.Both);
            _sender.Close();
            _sender.Dispose();
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
            catch(System.Net.Sockets.SocketException e)
            {
                CloseClient();
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
            catch(System.Net.Sockets.SocketException e)
            {
                CloseClient();
            }
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
                        if(serv.Uuid == new Guid(BLEUUID.BLEUART_BATTERY_SERVICE))
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
                var buff = new Byte[_packetsize];                
                reader.ReadBytes(buff);

                if (_sender != null)
                {
                    sendData(ToOutgoingPacket(buff, _packetsize));
                }
                if (isSaving)
                {
                    _doubleBuffer.addData(buff);
                }
               


                //sendData(ToOutgoingPacket(Encoding.Default.GetString(buff)));

                //_stringBuffer += Encoding.Default.GetString(buff);

                //if (_stringBuffer.Length >= 181)
                //{
                //    _stringBuffer += "<EOF>";
                //    //    Console.WriteLine(_stringBuffer.Length);
                //    //    Console.WriteLine(_stringBuffer);
                //    sendData();
                //    ClearStringBuffer();
                //}
                //Console.WriteLine("Buffer: " + Encoding.Default.GetString(buff));
                //Console.WriteLine(_incomingBuffer.Length);

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
            GattCommunicationStatus result = await _deviceInformation.BLEuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
            if (result == GattCommunicationStatus.Success)
            {
                Console.WriteLine(" Successfully wrote to device");
            }
        }

        private String ClearStringBuffer(String stringBuffer)
        {
            stringBuffer = String.Empty;
            return Encoding.Default.GetString(new Byte[] { 0x55 });
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
            var res = new Byte[len+1];
            res[0] = 0x55;
            stringBuffer.CopyTo(res, 1);
    
            return res;
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

        protected virtual void OnSelectedDeviceFound()
        {
            EventHandler handler = SelectedDeviceFound;
            if(handler != null)
            {
                handler(this,EventArgs.Empty);
            }
        }



    }
    public class statusChangedEventArgs : EventArgs
    {
        public String Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class doubleBuffer
    {
        //private String[][] _Buffer1 = new String[20];
        //private String[] Buffer1 = new String;
        //private String[] Buffer2 = String.Empty;

        private static List<String> Buffer1 = new List<String>();
        private static List<String> Buffer2 = new List<String>();
        private static bool _oneActive = true;
        private static bool _twoActive = false;

        private static String _path;
        private static int _numOfEntries;

        public doubleBuffer()
        {
           _path = @"C:\Users\Enno-LT\Desktop\IMU_Motion_Classification\BLE Driver\UI App\BLE_Drive_UI\Data\";
            _numOfEntries = 200;
            _oneActive = true;
            _twoActive = false;
    }

        public async void addData(byte[] data)
        {
            var scalingFactor = (1.00 / (1 << 14));
            var id = data[0]-48;
            var calib = data[1];
            //Console.WriteLine(calib);
            float quatW, quatX, quatY, quatZ;
            quatW = (float)scalingFactor * ((Int16)(data[2] | (data[3] << 8)));
            quatX = (float)scalingFactor * ((Int16)(data[4] | (data[5] << 8)));
            quatY = (float)scalingFactor * ((Int16)(data[6] | (data[7] << 8)));
            quatZ = (float)scalingFactor * ((Int16)(data[8] | (data[9] << 8)));

            var off = 10;
            scalingFactor = (1.00 / 100.0);// 1m/s^2 = 100 LSB 
            float x_a, y_a, z_a;
            x_a = (float)scalingFactor * ((Int16)(data[off + 0] | (data[off + 1] << 8)));
            y_a = (float)scalingFactor * ((Int16)(data[off + 2] | (data[off + 3] << 8)));
            z_a = (float)scalingFactor * ((Int16)(data[off + 4] | (data[off + 5] << 8)));





            //var str = System.Text.Encoding.Default.GetString(data);
            var str = id.ToString() + " " + calib.ToString() + " " + quatW.ToString("0.0000") + " " + quatX.ToString("0.0000") + " " + quatY.ToString("0.0000") + " " + quatZ.ToString("0.0000") + " "
                + x_a.ToString("0.0000") + " " + y_a.ToString("0.0000") + " " + z_a.ToString("0.0000");
          
            var t = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff");
            String toSave = t + " " + str; // + "\n";

            //Console.WriteLine("Add from " + Thread.CurrentThread.ManagedThreadId);

            if (_oneActive) // Buffer 1 active
            {
                //Console.WriteLine(Buffer1.Count);
                Buffer1.Add(toSave);
                if (Buffer1.Count >= _numOfEntries)
                {
                    await save();   
                    _twoActive = true;
                    _oneActive = false;
                }
            }
            else if (_twoActive)
            {
                Buffer2.Add(toSave);
                if (Buffer2.Count >= _numOfEntries)
                {
                    await save();
                    _oneActive = true;
                    _twoActive = false;
                }
            }
        }

        static async Task save()
        {
            Console.WriteLine("Save from " + Thread.CurrentThread.ManagedThreadId);
            String name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            if (_oneActive)
            {
                await Task.Run(() => {
                    var buf = Buffer1.ToArray();
                    Buffer1.Clear();
                    File.WriteAllLines(_path + name + ".txt", buf);
                });
                
            }
            else if (_twoActive)
            {
                await Task.Run(() => {
                    var buf = Buffer2.ToArray();
                    Buffer2.Clear();
                    File.WriteAllLines(_path + name + ".txt", buf);
                });
            }
        }
    }
}
