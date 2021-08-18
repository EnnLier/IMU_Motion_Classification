using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Bluetooth_Driver
{
    class BLEAdvertisementWatcher
    {

        UInt32[] BLEUART_UUID_SERVICE =
        {
             0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0,
             0x93, 0xF3, 0xA3, 0xB5, 0x01, 0x00, 0x40, 0x6E
         };

        Byte[] BLEUART_UUID_CHR_RXD =
        {
             0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0,
             0x93, 0xF3, 0xA3, 0xB5, 0x02, 0x00, 0x40, 0x6E
         };

        UInt32[] BLEUART_UUID_CHR_TXD =
        {
             0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0,
             0x93, 0xF3, 0xA3, 0xB5, 0x03, 0x00, 0x40, 0x6E
         };

        private const short offset = 0;

        public BLEAdvertisementWatcher()
        {
            BLEWatcher.Received += this.SignalReceivedEventHandler;

            BluetoothLEAdvertisementBytePattern filtermask = new BluetoothLEAdvertisementBytePattern();
            
            filtermask.Offset = offset;
            filtermask.Data = BLEUART_UUID_CHR_RXD.AsBuffer();
            filtermask.DataType = 0x00;
            BLEFilter = new BluetoothLEAdvertisementFilter();
            BLEFilter.BytePatterns.Add(filtermask);

            BLEWatcher_filtered = new BluetoothLEAdvertisementWatcher(BLEFilter);

            BLEWatcher_filtered.Received += this.FilteredSignalReceivedEventHandler;

            BLEWatcher.Start();
        }

        public List<BluetoothDevice> DeviceList = new List<BluetoothDevice>();
        private BluetoothLEAdvertisementWatcher BLEWatcher;
        private BluetoothLEAdvertisementWatcher BLEWatcher_filtered;
        private BluetoothLEAdvertisementFilter BLEFilter;
        private readonly object mThreadWatcher = new object();

        private void FilteredSignalReceivedEventHandler(BluetoothLEAdvertisementWatcher bLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Console.WriteLine("Filtered Found: ");
            Console.WriteLine(args.Advertisement.LocalName);
        }

            private void SignalReceivedEventHandler(BluetoothLEAdvertisementWatcher bLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            lock (mThreadWatcher)
            {
                //Remove old Devices
                int timeout = 10;
                DeviceList.RemoveAll(device => Math.Abs((DateTime.UtcNow - device.Timestamp.UtcDateTime).TotalSeconds) > timeout); 
                DateTimeOffset deviceTimestamp = args.Timestamp;
                short rssi = args.RawSignalStrengthInDBm;
                ulong adress = args.BluetoothAddress;
                BluetoothLEAdvertisement advertisement = args.Advertisement;

                if (rssi < -90)
                    return;

                foreach (BluetoothDevice device in DeviceList)
                {
                    if (device.BluetoothAdress == adress)
                    {//Update Device
                        device.RSSI = rssi;
                        device.Timestamp = deviceTimestamp;
                        device.Advertisement = advertisement;
                        return;
                    }
                }

                BluetoothDevice bleDevice = new BluetoothDevice(adress, rssi, deviceTimestamp, advertisement);
                DeviceList.Add(bleDevice);
            }
        }

        public void ConnectToDevice(BluetoothDevice device)
        {

        }
    }
}