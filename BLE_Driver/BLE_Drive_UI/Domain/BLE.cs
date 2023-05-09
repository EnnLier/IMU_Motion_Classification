using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using BLE_Drive_UI.Domain;

/// <summary>
/// BLE related classes and functions
/// </summary>
namespace BLE_Drive_UI.Domain
{
    /// <summary>
    /// This Class represents a BLE device and organizes necessary information
    /// </summary>
    public class BLEDeviceInformation : Object
    {
        /// <summary>
        /// This constructor initializes the object with minimum information
        /// </summary>
        /// <param name="name">Name of BLE device</param>
        /// <param name="id">BLE id of BLE device</param>
        /// <param name="canpair">Flag if device is pairable</param>
        public BLEDeviceInformation(String name, String id, bool canpair)
        {
            Name = name;
            BLEId = id;
            canPair = canpair;
        }
        //SensorID, ranges from 0 to number of connected devices
        public int SensorID { get; set;}

        public String Name { get; }
        public String BLEId { get; }
        public bool canPair { get; }

        //UUIDs for GATT protocol
        public GattCharacteristic BatteryCharacteristic { get; set; }
        public GattCharacteristic BLEuartCharacteristic { get; set; }
        public GattCharacteristic BLEuartCharacteristic_write { get; set; }

        //Batterylevel of this device
        public int BatteryLevel;

        //Last received Data of this device
        public float[] Data = new float[10];

        //Calibration values of this device
        public int[] Calibration = new int[4];

        /// <summary>
        /// Create a shallow copy of this object
        /// </summary>
        /// <returns>Shallow copy of this object</returns>
        public BLEDeviceInformation Clone()
        {
            return (BLEDeviceInformation)this.MemberwiseClone();
        }

        /// <summary>
        /// This function creates a String from the current measurements for saving purposes
        /// </summary>
        /// <returns>Last measurement as a string</returns>
        public String GetDataAsString()
        {
            //Append all data as strings
            String stringToSave = " " + SensorID.ToString();
            foreach(var calib in Calibration)
            {
                stringToSave += " " + calib.ToString();
            }
            foreach(var dat in Data)
            {
                stringToSave += " " + dat.ToString("0.0000");
            }
            return stringToSave;

        }
        
    }


    /// <summary>
    /// This static class contains UUIDs and functions helpful for this project
    /// </summary>
    public static class BLEUUID
    {
        /// <summary>
        /// This functions creates a MSDN GUID from a 128bit adafruit UUID, which is mainly used in this project
        /// </summary>
        /// <param name="adafruitUUID"></param>
        /// <returns>MSDN GUID</returns>
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

        /// <summary>
        /// This function rotates a bytearray
        /// </summary>
        /// <param name="arr">Array to rotate</param>
        /// <returns>Rotated Byte array</returns>
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

        //Collection of UUID for this project
        public static readonly Byte[] BLEUART_UUID_SERVICE = { 1, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_RXD = { 2, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_TXD = { 3, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_CUSTOM_UUID = { 8, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_BATTERY_SERVICE = { 4, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };


    }



}
