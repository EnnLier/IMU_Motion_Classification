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
using Windows.Security.Cryptography;

namespace BLE_Drive_UI.src
{
    class BLEdriver
    {
        private BluetoothLEDevice _BLEDevice;
        public BLEdevice _deviceInformation { get; set; }


        public bool _busy;
        public UInt16 BatteryLevel;

        private static UInt16 _packetsize = 9;
        private Byte[] _incomingBuffer = new byte[20*_packetsize];
        private String _stringBuffer = String.Empty;


        
        public event EventHandler<statusChangedEventArgs> StatusChanged;
        public event EventHandler SelectedDeviceFound;

        private GattCharacteristic BleuartCharacteristic;
        private GattCharacteristic BatteryCharacteristic;


        public BLEdriver()
        {
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
            try
            {
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
                                        BleuartCharacteristic = characteristic;
                                        Console.WriteLine("Success0");
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
                                        BatteryCharacteristic = characteristic;
                                        Console.WriteLine("Success1");
                                    }
                                }
                            }
                            //if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            //{
                            //    try
                            //    {
                            //        GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            //            GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            //        if (status == GattCommunicationStatus.Success)
                            //        {

                            //            ////var bat_uuid = new Byte[] { 0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0, 0x93, 0xF3, 0xA3, 0xB5, 0x04, 0x00, 0x40, 0x6E };
                            //            ////Console.WriteLine(serv.Uuid);
                            //            ////Console.WriteLine(BLEUUID.toGuid(bat_uuid));
                            //            //characteristic.ValueChanged += Characteristic_ValueChanged;
                            //        }
                            //    }
                            //    catch(System.Exception e)
                            //    {
                            //        Console.WriteLine("Failed to notify enable: " + e);
                            //    }
                            //}
                        }
                        
                    }
                    try
                    {

                        if (BatteryCharacteristic != null)
                        {
                            BatteryCharacteristic.ValueChanged += Characteristic_ValueChanged;
                            Console.WriteLine("BLEBAS");
                        }
                        if (BleuartCharacteristic != null)
                        {
                            BleuartCharacteristic.ValueChanged += Characteristic_ValueChanged;
                            Console.WriteLine("BLEUART");
                        }
                        else
                        {
                            OnStatusChanged("Failed to Connect");
                        }
                    }
                    catch (Exception e)
                    {
                        OnStatusChanged("Services not found");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[ConnectDevice()]: " + e.ToString());
            }
            finally
            {
                _busy = false;
            }
        }


        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            if (BatteryCharacteristic == null || BleuartCharacteristic == null) { Console.WriteLine("isNull"); return; }
            if (sender.Service.AttributeHandle == BatteryCharacteristic.Service.AttributeHandle)
            {
                //var reader = DataReader.FromBuffer(args.CharacteristicValue);
                BatteryLevel = reader.ReadUInt16();
                Console.WriteLine("fdjlksjf");
            }
            if(sender.Service.AttributeHandle == BleuartCharacteristic.Service.AttributeHandle)
            {
                //var reader = DataReader.FromBuffer(args.CharacteristicValue);
                var buff = new Byte[9];
                var sBuff = String.Empty;
                reader.ReadBytes(buff);

                _stringBuffer += Encoding.Default.GetString(buff);

                if(_stringBuffer.Length >= 180)
                {
                    Console.WriteLine(_stringBuffer.Length);
                    Console.WriteLine(_stringBuffer);
                    _stringBuffer = String.Empty;
                }
                //Console.WriteLine("Buffer: " + Encoding.Default.GetString(buff));
                //Console.WriteLine(_incomingBuffer.Length);

            }

            //var reader = DataReader.FromBuffer(args.CharacteristicValue);

            //Console.WriteLine("LEngth: " + args.CharacteristicValue.Length);
            //String tmp = reader.ReadString(args.CharacteristicValue.Length);
            //Console.WriteLine();
            //Console.WriteLine(Encoding.Default.GetString(reader.));

        }

        public async void WriteToBLEDevice(Byte[] data)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            GattCommunicationStatus result = await BleuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
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
            GattCommunicationStatus result = await BleuartCharacteristic.WriteValueAsync(writer.DetachBuffer());
            if (result == GattCommunicationStatus.Success)
            {
                Console.WriteLine(" Successfully wrote to device");
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
            OnStatusChanged("sender.ConnectionStatus");
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
}
