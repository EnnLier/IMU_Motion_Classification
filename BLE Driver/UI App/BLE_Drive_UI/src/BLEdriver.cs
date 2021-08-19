using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using BLE_Drive_UI.Domain;
using System.Threading;

namespace BLE_Drive_UI.src
{
    class BLEdriver
    {
        private BluetoothLEDevice _BLEDevice;
        //public BLEdevice _deviceInformation { get;set;}

        public bool _busy;
        private bool _deviceCompatible;

        public event EventHandler<statusChangedEventArgs> StatusChanged;

        public BLEdriver()
        {
        }

        public async void ConnectDevice(BLEdevice deviceInformation)
        {
            _busy = true;
            OnStatusChanged("Initializing");
            // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
            try
            {
                _BLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);
                while (_BLEDevice == null)
                {
                    Console.WriteLine("Waiting...");
                    Thread.Sleep(500);
                }

                _BLEDevice.ConnectionStatusChanged += ConnectionStatusChangedEvent;
                _BLEDevice.GattServicesChanged += GattServicesChangedEvent;
                _BLEDevice.NameChanged += NameChangedEvent;

                //Console.WriteLine("Service 0 + Handle:" + _BLEDevice.GattServices[0].AttributeHandle);
                GattDeviceServicesResult service_result = await _BLEDevice.GetGattServicesAsync();

                Guid filter_uuid = new Guid(BLEUUID.BLEUART_UUID_SERVICE);

                //IRGENDWIE GATTDEVICESERVICE SPEICHERN UND DAMIT WEITERARBEITEN

                if (service_result.Status == GattCommunicationStatus.Success)
                {
                    foreach(GattDeviceService serv in service_result.Services)
                    {
                        //if (service.Uuid == filteruuid)
                        Console.WriteLine("Handle: " + serv.AttributeHandle);
                        if(serv.Uuid == filter_uuid)
                        {
                            OnStatusChanged("UUID Found!");
                            
                        }
                    }
                }

                //GattCharacteristicsResult char_result = await service.GetCharacteristicsAsync();

                //if (char_result.Status == GattCommunicationStatus.Success)
                //{
                //    var characteristics = char_result.Characteristics;
                //    // ...
                //}

            }
            catch(Exception e)
            {
                OnStatusChanged("Failed to connect");
                Console.WriteLine("[ConnectDevice()]: " + e.ToString());
            }
            finally
            {
                _busy = false;
                _deviceCompatible = false;
            }
        }

        private void NameChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Name changed: " + sender.Name);
        }

        private async void GattServicesChangedEvent(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("GattServices changed: ");
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
        
    }
    public class statusChangedEventArgs : EventArgs
    {
        public String Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
