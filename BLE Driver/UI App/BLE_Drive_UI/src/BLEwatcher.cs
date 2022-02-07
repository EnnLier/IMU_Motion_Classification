using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using BLE_Drive_UI.Domain;

namespace BLE_Drive_UI.src
{
    class BLEwatcher
    {
        public BLEwatcher()
        {
            // Query for extra properties you want returned
            //string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            string[] requestedProperties = {"System.Devices.Aep.Bluetooth.Le.IsConnectable","System.Devices.Aep.IsPresent" };


            deviceWatcher =
                        DeviceInformation.CreateWatcher(
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Start the watcher.
            deviceWatcher.Start();
        }

        public List<BLEDeviceInformation> deviceList = new List<BLEDeviceInformation>();
        public DeviceWatcher deviceWatcher;

        public object mListLock = new object();


        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Console.WriteLine("DeviceWatcher_Stopped");
            //throw new NotImplementedException("DeviceWatcher_Stopped");
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Console.WriteLine("DeviceWatcher_EnumerationCompleted");
            //throw new NotImplementedException("DeviceWatcher_EnumerationCompleted");
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //Console.WriteLine("DeviceWatcher_Removed");
            //Console.WriteLine(args.Id);
            lock (mListLock)
            {
                foreach (BLEDeviceInformation dev in deviceList)
                {
                    if (args.Id.Equals(dev.BLEId))
                    {
                        deviceList.Remove(dev);
                        return;
                    }
                }
            }
            //throw new NotImplementedException("DeviceWatcher_Removed");
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            
            //Console.WriteLine("Updated...");
            //Console.WriteLine(args.Id);
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if ((bool)args.Properties["System.Devices.Aep.IsPresent"] == false)
            {
                return;
            }
            BLEDeviceInformation device = new BLEDeviceInformation(args.Name, args.Id, args.Pairing.CanPair);
            //bool update = false;
            lock(mListLock)
            {
                foreach(BLEDeviceInformation dev in deviceList)
                {
                    if (device.BLEId.Equals(dev.BLEId))
                    {
                        return;
                    }
                }
                deviceList.Add(device);
            }
        }
    }
}
