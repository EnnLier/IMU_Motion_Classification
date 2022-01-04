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
            
            pb_connect_TCP.Enabled = false;
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
                    if (String.IsNullOrEmpty(device.Name)) continue;
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
            try
            {
                this.l_Driver_Status.Text = e.Timestamp.TimeOfDay.Hours + ":" + e.Timestamp.TimeOfDay.Minutes + ":" + e.Timestamp.TimeOfDay.Seconds + "      " + e.Status;
                this.pb_connect_TCP.Enabled = _BLEdriver.Connected == true ? true : false;
            }
            catch(System.InvalidOperationException)
            {
                l_Driver_Status.Invoke((Action)delegate
                {
                    l_Driver_Status.Text = e.Status;
                });
                pb_connect_TCP.Invoke((Action)delegate
                {
                    this.pb_connect_TCP.Enabled = _BLEdriver.Connected == true ? true : false;
                });
            }
            
        }

        private void pb_connect_TCP_Click(object sender, EventArgs e)
        {
            _BLEdriver.StartClient();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(this.checkBox1.Checked)
                _BLEdriver.isSaving = true;
            else
            {
                _BLEdriver.isSaving = false;
            }
        }

        private void cb_StreamTCP_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_StreamTCP.Checked)
            { 
                _BLEdriver.StartClient();
                _BLEdriver.isStreaming = true;
            } 
            else
            {
                _BLEdriver.CloseClient();
                _BLEdriver.isStreaming = false;
            }
        }
    }
}
