using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLE_Drive_UI.Domain
{

    public struct BLEdevice
    {
        public BLEdevice(String name, String id, bool canpair)
        {
            Name = name;
            Id = id;
            canPair = canpair;
        }
        public String Name { get; }
        public String Id { get; }
        public bool canPair { get; }

        public override string ToString() => $"(Name: {Name}, ID: {Id}, Can Pair: {canPair})";
    }

    public static class BLEUUID
    {
        public static Dictionary<String,Byte[]> uuids;

        

        static BLEUUID()
        { 
            uuids = new Dictionary<string, byte[]>
            {
                {"BLEUART_UUID_SERVICE", BLEUART_UUID_SERVICE},
                {"BLEUART_UUID_CHR_RXD", BLEUART_UUID_CHR_RXD},
                {"BLEUART_UUID_CHR_TXD", BLEUART_UUID_CHR_TXD}
            };
        }


        public static readonly Byte[] BLEUART_UUID_SERVICE = { 1, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_RXD = { 2, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };

        public static readonly Byte[] BLEUART_UUID_CHR_TXD = { 3, 0, 64, 110, 163, 181, 147, 243, 224, 169, 229, 14, 36, 220, 202, 158 };


    }



}
