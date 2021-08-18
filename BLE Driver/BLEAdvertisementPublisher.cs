using System;
using System.Globalization;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Bluetooth_Driver
{
    class BLEAdvertisementPublisher
    {
        public BLEAdvertisementPublisher()
        {
        }
        private BluetoothLEAdvertisementPublisher BluetoothAdvertisementPublisher;
        private readonly object mThreadPublisherPayload = new object();
        private readonly object mThreadPublisherStatus = new object();

        
        public void StartPublishingPayloadEventHandler(string payload)
        {
            if (IsStarted)
            {
                StopPublishingPayloadEventHandler();
            }
            lock (mThreadPublisherPayload)
            {
                var bLEAdvertisement = PreparePayload(payload);
                BluetoothAdvertisementPublisher = new BluetoothLEAdvertisementPublisher(bLEAdvertisement);
                BluetoothAdvertisementPublisher.StatusChanged += this.PublisherStatusChangedEventHandler;
                BluetoothAdvertisementPublisher.Start();
            }
        }

        public void StopPublishingPayloadEventHandler()
        {
            if(IsStarted)
                BluetoothAdvertisementPublisher.Stop();
        }


        private BluetoothLEManufacturerData _createBluetoothLEManufacturerData(string data)
        {
            var dataWriter = new DataWriter();
            dataWriter.WriteInt32(data.Length);
            dataWriter.WriteString(data);

            return new BluetoothLEManufacturerData(0xFFFE, dataWriter.DetachBuffer());
        }


        private BluetoothLEAdvertisementDataSection _createBluetoothData(string data)
        {
            var dataWriter = new DataWriter();
            dataWriter.WriteString(data);

            return new BluetoothLEAdvertisementDataSection(0x66, dataWriter.DetachBuffer());
        }


        private BluetoothLEAdvertisement PreparePayload(string payload)
        {
            BluetoothLEAdvertisement bLEAdvertisement = new BluetoothLEAdvertisement();
            Console.WriteLine($"Payload is : {payload}");
            bLEAdvertisement.ManufacturerData.Add(_createBluetoothLEManufacturerData(""));
            bLEAdvertisement.DataSections.Add(_createBluetoothData(payload));
            return bLEAdvertisement;
        }


        private void PublisherStatusChangedEventHandler(BluetoothLEAdvertisementPublisher publisherObject, BluetoothLEAdvertisementPublisherStatusChangedEventArgs args)
        {
            lock(mThreadPublisherStatus)
            { 
                switch ((int)args.Status)
                {
                    case 0:
                        Console.WriteLine("Case 0: Created");
                        IsCreated = true;
                        IsWaiting = false;
                        IsStarted = false;
                        IsStopping = false;
                        IsStopped = false;
                        IsAborted = false;
                        break;
                    case 1:
                        Console.WriteLine("Case 1: Waiting");
                        IsCreated = false;
                        IsWaiting = true;
                        IsStarted = false;
                        IsStopping = false;
                        IsStopped = false;
                        IsAborted = false;
                        break;
                    case 2:
                        Console.WriteLine("Case 2: Started");
                        IsCreated = false;
                        IsWaiting = false;
                        IsStarted = true;
                        IsStopping = false;
                        IsStopped = false;
                        IsAborted = false;
                        break;
                    case 3:
                        Console.WriteLine("Case 3: Stopping");
                        IsCreated = false;
                        IsWaiting = false;
                        IsStarted = false;
                        IsStopping = true;
                        IsStopped = false;
                        IsAborted = false;
                        break;
                    case 4:
                        Console.WriteLine("Case 4: Stopped");
                        //Deleting Reference to publisher instance
                        BluetoothAdvertisementPublisher = null;

                        IsCreated = false;
                        IsWaiting = false;
                        IsStarted = false;
                        IsStopping = false;
                        IsStopped = true;
                        IsAborted = false;
                        break;
                    case 5:
                        Console.WriteLine("Case 5: Aborted");
                        IsCreated = false;
                        IsWaiting = false;
                        IsStarted = false;
                        IsStopping = false;
                        IsStopped = false;
                        IsAborted = true;
                        break;
                }
            }
        }
        public bool IsCreated { get; set; }
        public bool IsWaiting { get; set; }
        public bool IsStarted { get; set; }
        public bool IsStopping { get; set; }
        public bool IsStopped { get; set; }
        public bool IsAborted { get; set; }
    }
}