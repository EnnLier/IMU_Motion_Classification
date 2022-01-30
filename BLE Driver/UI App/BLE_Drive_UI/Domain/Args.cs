using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLE_Drive_UI.Domain
{
    public class statusChangedEventArgs : EventArgs
    {
        public String Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class imuDataEventArgs : EventArgs
    {
        public float Accx { get; set; }
        public float Accy { get; set; }
        public float Accz { get; set; }
        public float Gyrx { get; set; }
        public float Gyry { get; set; }
        public float Gyrz { get; set; }
    }

    public class changeLabelEventArgs : EventArgs
    {
        public String label { get; set; }
        public String value { get; set; }
    }

    public class tcpConnectEventArgs : EventArgs
    {
        public bool connected { get; set; }
    }
}
