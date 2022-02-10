using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This class contains all eventargs used throughout the whole project
/// </summary>

namespace BLE_Drive_UI.Domain
{
    public class statusChangedEventArgs : EventArgs
    {
        public BLEDeviceInformation deviceInformation { get; set; }
        public String Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ConnectedChangedEventArgs : EventArgs
    {
        public BLEDeviceInformation deviceInformation { get; set; }
        public bool status { get; set; }
    }

    public class imuDataEventArgs : EventArgs
    {
        //public byte calib { get; set; }

        //public int id { get; set; }
        public float[] data { get; set;}
        //public float quatW { get; set; }
        //public float quatX { get; set; }
        //public float quatY { get; set; }
        //public float quatZ { get; set; }
        //public float Accx { get; set; }
        //public float Accy { get; set; }
        //public float Accz { get; set; }
        //public float Gyrx { get; set; }
        //public float Gyry { get; set; }
        //public float Gyrz { get; set; }
    }

    //public class changeLabelEventArgs : EventArgs
    //{
    //    public String label { get; set; }
    //    public String value { get; set; }
    //}

    public class tcpConnectEventArgs : EventArgs
    {
        public bool connected { get; set; }
    }
}
