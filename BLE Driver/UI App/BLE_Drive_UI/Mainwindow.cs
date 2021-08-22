using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BLE_Drive_UI.src;
using BLE_Drive_UI.Domain;
using Windows.Devices.Enumeration;

namespace BLE_Drive_UI
{
    public partial class mw_form : Form
    {
        private BLEwatcher _BLEwatcher;
        private BLEdriver _BLEdriver;
        public mw_form()
        {
            _BLEwatcher = new BLEwatcher();
            _BLEdriver = new BLEdriver();
            _BLEdriver.StatusChanged += BLEdriver_StatusChanged;
            InitializeComponent();
            
        }



        private void Mainwindow_Load(object sender, EventArgs e)
        {

        }

        private void pb_Refresh_List_Click(object sender, EventArgs e)
        {
            this.lv_Device_List.Items.Clear();
            lock(_BLEwatcher.mListLock)
            {
                foreach (BLEdevice device in _BLEwatcher.deviceList)
                {
                    var row = new String[] { device.Name, device.Id, device.canPair.ToString() };
                    ListViewItem item = new ListViewItem(row);
                    item.Tag = device;
                    this.lv_Device_List.Items.Add(item);
                }
            }
            
        }

        private void pb_connect_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedDevice = (BLEdevice)this.lv_Device_List.SelectedItems[0].Tag;

                if(_BLEdriver != null)
                {
                    _BLEdriver.ConnectDevice(selectedDevice);
                }

            }
            catch(System.ArgumentOutOfRangeException)
            {
                Console.WriteLine("Nothing Highlighted");
            }
        }

        private void BLEdriver_StatusChanged(object sender, statusChangedEventArgs e)
        {
            this.l_Driver_Status.Text = e.Timestamp.TimeOfDay.Hours + ":" + e.Timestamp.TimeOfDay.Minutes + ":" + e.Timestamp.TimeOfDay.Seconds + "      " + e.Status;
        }
    }
}
