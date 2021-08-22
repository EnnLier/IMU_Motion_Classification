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

namespace BLE_Drive_UI.src
{
    class BLEdriver
    {
        private BluetoothLEDevice _BLEDevice;
        public BLEdevice _deviceInformation { get; set; }


        public bool _busy;

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
                OnStatusChanged("Connected");


                GattDeviceServicesResult service_result = await _BLEDevice.GetGattServicesAsync();

                //GattCharacteristicsResult char_result;

                if (service_result.Status == GattCommunicationStatus.Success)
                {
                    foreach (GattDeviceService serv in service_result.Services)
                    {

                        GattCharacteristicsResult char_result = await serv.GetCharacteristicsAsync();

                        foreach (GattCharacteristic characteristic in char_result.Characteristics)
                        {
                            if (serv.Uuid == new Guid(BLEUUID.BLEUART_CUSTOM_UUID))
                            {
                                BleuartCharacteristic = characteristic;
                            }   
                            else if(serv.Uuid == new Guid(BLEUUID.BLEUART_BATTERY_SERVICE))
                            {
                                BatteryCharacteristic = characteristic;
                            }
                            else
                            {
                                //Console.WriteLine("Unknown Charakteristic: " + serv.Uuid);
                            }
                                var properties = characteristic.CharacteristicProperties;

                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                try
                                {
                                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    if (status == GattCommunicationStatus.Success)
                                    {
                                        characteristic.ValueChanged += Characteristic_ValueChanged;
                                        OnStatusChanged("Services Found");
                                    }
                                }
                                catch(System.Exception e)
                                {
                                    Console.WriteLine("Failed to notify enable: " + e);
                                }
                                
                            }
                        }

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
            Console.WriteLine("Attribute :" +sender.Service.AttributeHandle);

            Console.WriteLine(reader.ReadString(args.CharacteristicValue.Length));
            
        }

        private async void WriteToBLEDevice(Byte[] data)
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

        private void NameChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Name changed: " + sender.Name);
        }

        private void GattServicesChangedEvent(BluetoothLEDevice sender, object args)
        {
            //Console.WriteLine("GattServices changed: ");
            //GattDeviceServicesResult result = await _BLEDevice.GetGattServicesAsync();
            //Guid filteruuid = new Guid(BLEUUID.BLEUART_UUID_SERVICE);

            //if (result.Status == GattCommunicationStatus.Success)
            //{
            //    foreach (GattDeviceService service in result.Services)
            //    {
            //        //if (BLEUUID)
            //        //{
            //        //    OnStatusChanged("UUID Found!");
            //        //}
            //        Console.WriteLine("GattServices changed: " + service.Uuid);
            //    }
            //}
        }

        private void ConnectionStatusChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Connection Status changed: " + sender.ConnectionStatus);
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
