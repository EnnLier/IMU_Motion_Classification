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
        public String GetDataAsString()
        {
            String stringToSave = " " + SensorID.ToString();
            foreach(var calib in Calibration)
            {
                stringToSave += " " + calib.ToString();
            }
            foreach(var dat in Data)
            {
                stringToSave += " " + dat.ToString("0.0000");
            }
            Console.WriteLine(stringToSave);
            return stringToSave;

        }
        public int BatteryLevel;
        public float[] Data = new float[10];
        public int[] Calibration = new int[4];
        //public String StringToSave = String.Empty;
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
