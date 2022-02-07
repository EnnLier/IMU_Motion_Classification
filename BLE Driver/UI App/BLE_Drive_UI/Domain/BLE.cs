using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using BLE_Drive_UI.Domain;

namespace BLE_Drive_UI.Domain
{

    public class BLEDeviceInformation : Object
    {
        public BLEDeviceInformation(String name, String id, bool canpair)
        {
            Name = name;
            BLEId = id;
            canPair = canpair;
        //Service = new Guid();
        //HandlesOfCharacteristicsOfService = new Dictionary<Guid,Dictionary<Guid, ushort>>();

    }
        //private String printDictionary()
        //{
        //    String toRet = String.Empty;
        //    String charHandles = String.Empty;
        //    int i = 0;
        //    foreach (KeyValuePair< Guid,Dictionary<Guid, ushort>> serviceCharacterHandles in HandlesOfCharacteristicsOfService)
        //    {
        //        int k = 0;
        //        foreach(KeyValuePair<Guid,ushort> characterHandles in serviceCharacterHandles.Value)
        //        {
        //            charHandles = "Characteristic " + k + ": " + characterHandles.Key.ToString() + " Handle: " + characterHandles.Value;
        //            k++;
        //        }
        //        toRet += "Service " + i + ": " + serviceCharacterHandles.Key.ToString() + "\t" + charHandles + "\t";
        //        //toRet = string.Format("Charachteristic {0} = {1}, Value = {2} \t",i, characterHandles.Key, handles);
        //        i++; 
        //    }
        //    return toRet;
        //}
        //public bool isFront { get; set }
        public int SensorID { get; set;}
        public String Name { get; }
        public String BLEId { get; }
        public bool canPair { get; }
        public GattCharacteristic BatteryCharacteristic { get; set; }
        public GattCharacteristic BLEuartCharacteristic { get; set; }
        public GattCharacteristic BLEuartCharacteristic_write { get; set; }

        public BLEDeviceInformation Clone()
        {
            return (BLEDeviceInformation)this.MemberwiseClone();
        }
        //public Guid Service { get;set;}
        //public Dictionary<Guid,Dictionary<Guid,ushort>> HandlesOfCharacteristicsOfService { get;set;}

        //public override string ToString() => $"(Name: {Name}, ID: {Id}, Can Pair: {canPair}), Service: {Service.ToString()}) , Characteristics and Handles: {printDictionary()})";
    }

    public static class BLEUUID
    {
        public static Guid toGuid(Byte[] adafruitUUID)
        {
            Byte[] part1 = rotate(adafruitUUID.Take(8).ToArray());
            adafruitUUID = adafruitUUID.Skip(8).ToArray();
            Byte[] part2 = adafruitUUID.Take(2).ToArray();
            adafruitUUID = adafruitUUID.Skip(2).ToArray();
            Byte[] part3 = adafruitUUID.Take(2).ToArray();
            adafruitUUID = adafruitUUID.Skip(2).ToArray();
            Byte[] part4 = adafruitUUID.Take(4).ToArray();
            Guid guid = new Guid(BitConverter.ToInt32(part4,0), BitConverter.ToInt16(part3, 0), BitConverter.ToInt16(part2, 0), part1);
            return guid;
        }

        private static Byte[] rotate(Byte[] arr)
        {
            Byte[] ret = new Byte[arr.Length];
            int k = 0;
            for(int i = arr.Length-1; i >= 0 ; i--)
            {
                ret[k] = arr[i];
                k++;
            }
            return ret;
        }

        static BLEUUID()
        { 
        }


        public static readonly Byte[] BLEUART_UUID_SERVICE = { 1, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_RXD = { 2, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_TXD = { 3, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_CUSTOM_UUID = { 8, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_BATTERY_SERVICE = { 4, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };


    }



}
