using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Storage.Streams;

namespace Bluetooth_Driver
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            BluetoothLEPlatform BLEManager = new BluetoothLEPlatform();
            while (true)
            {
                Console.WriteLine("Enter [space] to reload devices \nEnter 'publish' to publish Payload\nEnter 'stop' to Stop Broadcasting Payload");
                BLEManager.PrintList();
                string input = Console.ReadLine();
                if (input == " ")
                { 
                    Console.Clear();
                    continue;
                }
                else if(input == "publish")
                {
                    Console.WriteLine("Enter Payload");
                    string s = Console.ReadLine();
                    BLEManager.SendPayload(s);
                }
                else if(input == "stop")
                    BLEManager.StopPublishing();
                else
                {
                    Console.Clear();
                }
            }
        }
    }

    class BluetoothLEPlatform
    {
    //Constructor
        public BluetoothLEPlatform()
        {
            SendPayloadEvent += BLEPublisher.StartPublishingPayloadEventHandler;
            StopPublishingEvent += BLEPublisher.StopPublishingPayloadEventHandler;
        }
        private BLEAdvertisementPublisher BLEPublisher = new BLEAdvertisementPublisher();
        private BLEAdvertisementWatcher BLEWatcher = new BLEAdvertisementWatcher();

        public void SendPayload(string payload)
        {
            if(SendPayloadEvent != null)
                SendPayloadEvent(payload);
        }
        public void StopPublishing()
        {
            if(StopPublishingEvent != null)
            {
                StopPublishingEvent();
            }
        }

    //Events
        private event Action<string> SendPayloadEvent;
        private event Action StopPublishingEvent;
        public void PrintList()
        {
            BLEWatcher.DeviceList.Sort((x, y) => y.RSSI.CompareTo(x.RSSI));
            foreach (BluetoothDevice device in BLEWatcher.DeviceList)
            {
                //Console.WriteLine("device.Company_ID: " + device.Company_ID);
                if (device.Company_ID != 0xFFFE)
                {
                    Console.WriteLine($"Adress: {device.BluetoothAdress}   Signal Strength: {device.RSSI}DB   Payload: UNKNOWN   Name: {device.DeviceName}   Company_ID: {device.Company_ID}");
                }
                else
                {
                    var data = device.Advertisement.DataSections[1].Data;
                    var dataReader = DataReader.FromBuffer(data);
                    var output = dataReader.ReadString(data.Length);
                    Console.WriteLine($"Adress: {device.BluetoothAdress}   Signal Strength: {device.RSSI}DB   Payload: {output}   Name: {device.DeviceName}   Company_ID: {device.Company_ID}");
                }
            }
        }
    }
}