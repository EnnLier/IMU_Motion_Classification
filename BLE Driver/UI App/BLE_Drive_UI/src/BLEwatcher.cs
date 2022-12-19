using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using BLE_Drive_UI.Domain;

/// <summary>
/// This Watcher class provides functions to look for and list BLE devices which are currently available and were available previously
/// </summary>

namespace BLE_Drive_UI.src
{
    class BLEwatcher
    {
        /// <summary>
        /// Create DeviceInformation Watcher object, add filters, register eventhandlers and start the Watcher
        /// </summary>
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

            // Start the watcher.
            deviceWatcher.Start();
        }

        //List which contains all available devices
        public List<BLEDeviceInformation> deviceList = new List<BLEDeviceInformation>();
        //DeviceWatcher object
        public DeviceWatcher deviceWatcher;

        //Mutex which handles list access. It is needed since the Mainwindow Thread occasionaly reads from the list
        public object mListLock = new object();

        /// <summary>
        /// Callback function which is called if a device is removed from the watcher. It removes the corresponding device from the devicelist
        /// </summary>
        /// <param name="sender">DeviceWatcher Object raising this event.</param>
        /// <param name="args">DeviceInformationUpdate Object linked to this event</param>
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //Lock deviceList while searching for the entry
            lock (mListLock)
            {
                foreach (BLEDeviceInformation dev in deviceList)
                {
                    if (args.Id.Equals(dev.BLEId))
                    {
                        deviceList.Remove(dev);
                        //Return after device was found and removed
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

        /// <summary>
        /// Callback function which is called if a device was found by the watcher and added to its list. It adds the corresponding device to the devicelist
        /// </summary>
        /// <param name="sender">DeviceWatcher Object raising this event.</param>
        /// <param name="args">DeviceInformation Object linked to this event</param>
        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            //Check if added device is actually present. This does not really work is seems to be an open issue for now
            if ((bool)args.Properties["System.Devices.Aep.IsPresent"] == false)
            {
                return;
            }
            BLEDeviceInformation device = new BLEDeviceInformation(args.Name, args.Id, args.Pairing.CanPair);
            //bool update = false;

            //lock devicelist 
            lock(mListLock)
            {
                // Check if entry already exists
                foreach(BLEDeviceInformation dev in deviceList)
                {
                    if (device.BLEId.Equals(dev.BLEId))
                    {
                        return;
                    }
                }
                //Add entry if its not alreay in List
                deviceList.Add(device);
            }
        }
    }
}
