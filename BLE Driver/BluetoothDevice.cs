using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using Windows.Storage.Streams;

namespace Bluetooth_Driver
{
    class BluetoothDevice
    {
        public BluetoothDevice(ulong adress, short rssi, DateTimeOffset timestamp, BluetoothLEAdvertisement advertisement)
        {

            Advertisement = new BluetoothLEAdvertisement();
            BluetoothAdress = adress;
            RSSI = rssi;
            Timestamp = timestamp;
            Advertisement = advertisement;
        }
        private BluetoothLEDevice _device;
        public ulong BluetoothAdress {get; set;}
        public DateTimeOffset Timestamp {get;set; }
        public short RSSI { get;set; }
        private BluetoothLEAdvertisement _advertisement;
        public BluetoothLEAdvertisement Advertisement
        {
            get { return _advertisement;}
            set
            {
                _advertisement = value;
                if(_advertisement.LocalName.Length < 1)
                    DeviceName = "UNKNOWN";
                else
                    DeviceName = _advertisement.LocalName;

                if (_advertisement.ManufacturerData.Count > 0)
                    Company_ID = _advertisement.ManufacturerData[0].CompanyId;
                else
                    Company_ID = 0;
                if (_advertisement.DataSections.Count > 1)
                {
                    Data = _advertisement.DataSections[1].Data;
                    DataType = _advertisement.DataSections[1].DataType;
                }
            }
        }
        public string DeviceName { get; set; }
        public ushort Company_ID{ get; set; }
        public IBuffer Data { get; set; }
        public byte DataType {get; set; }

        private async void connect()
        {
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(BluetoothAdress);
        }
    }
}
