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
using System.Diagnostics;
using Windows.Devices.Enumeration;
//using System.Web.UI.DataVisualization.Charting;
using System.Windows.Forms.DataVisualization.Charting;

namespace BLE_Drive_UI
{
    public partial class mw_form : Form
    {
        private BLEwatcher _BLEwatcher;
        private BLEdriver _BLEdriver;

        private static int _maxNumOfChartValues = 500;
        private int _currentChartValue = 0;

        private object m_chartLock = new object();

        public mw_form()
        {
            _BLEwatcher = new BLEwatcher();
            _BLEdriver = new BLEdriver();
            _BLEdriver.StatusChanged += BLEdriver_StatusChanged;
            _BLEdriver.ChangeLabel += Form_ChangeLabel;
            _BLEdriver.UpdateChart += update_accChart;
            InitializeComponent();
            
            cb_SaveToFile.Enabled = false;
            cb_StreamTCP.Enabled = false;
            cb_plotAcc.Enabled = false;
            b_recalibrate.Enabled = false;

            initialize_accChart();
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

        private void Form_ChangeLabel(object sender, changeLabelEventArgs e)
        {
            //Debug.WriteLine("Name: " + e.label + "    Value: " + e.value);
            foreach (Control x in this.Controls)
            {
                //var l = x.GetType();
                //Debug.WriteLine("Name: " + x.Name + "    Value: " + e.value);
                //Debug.WriteLine("Name: " + x.Name + "    label: " + e.label);
                if (x.Name == e.label)
                {
                    try
                    {
                        x.Text = e.value;
                    }
                    catch (System.InvalidOperationException)
                    {
                        x.Invoke((Action)delegate
                        {
                            x.Text = e.value;
                        });
                    }
                }
            }
        }

        private void BLEdriver_StatusChanged(object sender, statusChangedEventArgs e)
        {
            var timestamp = e.Timestamp.ToString("HH:mm:ss");
            
            try
            {
                this.l_Driver_Status.Text = timestamp + "      " + e.Status;
                this.cb_StreamTCP.Enabled = _BLEdriver.Connected == true ? true : false;
                this.cb_SaveToFile.Enabled = _BLEdriver.Connected == true ? true : false;
                this.cb_plotAcc.Enabled = _BLEdriver.Connected == true ? true : false;
                this.b_recalibrate.Enabled = _BLEdriver.Connected == true ? true : false;

            }
            catch(System.InvalidOperationException)
            {
                l_Driver_Status.Invoke((Action)delegate
                {
                    this.l_Driver_Status.Text = timestamp + "      " + e.Status;
                });
                cb_StreamTCP.Invoke((Action)delegate
                {
                    this.cb_StreamTCP.Enabled = _BLEdriver.Connected == true ? true : false;
                    this.cb_SaveToFile.Enabled = _BLEdriver.Connected == true ? true : false;
                    this.cb_plotAcc.Enabled = _BLEdriver.Connected == true ? true : false;
                    this.b_recalibrate.Enabled = _BLEdriver.Connected == true ? true : false;
                });
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

        private void cb_plotAcc_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_plotAcc.Checked)
            {
                _currentChartValue = 0;
                ch_AccPlot.Series[0].Points.Clear();
                ch_AccPlot.Series[1].Points.Clear();
                ch_AccPlot.Series[2].Points.Clear();
                _BLEdriver.isPlotting = true;
            }
            else
            {
                _BLEdriver.isPlotting = false;
            }
        }

        private void cb_SaveToFile_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_SaveToFile.Checked)
            {
                _BLEdriver.isSaving = true;
            }
            else
            {
                _BLEdriver.isSaving = false;
                _BLEdriver.flushBuffer();
            }
        }


        private void b_recalibrate_Click(object sender, EventArgs e)
        {
            _BLEdriver.recalibrate_imu();
        }

        private void mw_form_Load(object sender, EventArgs e)
        {

        }

        private void initialize_accChart()
        {
            
            this.ch_AccPlot.Series.Clear();

            //ch_AccPlot.ChartType = SeriesChartType.Spline;

            //this.ch_AccPlot.Series.Add("AccX");
            //this.ch_AccPlot.Series.Add("AccY");
            //this.ch_AccPlot.Series.Add("AccZ");
            var chartAreaX = ch_AccPlot.ChartAreas.Add("X");
            var chartAreaY = ch_AccPlot.ChartAreas.Add("Y");
            var chartAreaZ = ch_AccPlot.ChartAreas.Add("Z");

            ch_AccPlot.Series[0] = this.ch_AccPlot.Series.Add("AccX");
            ch_AccPlot.Series[1] = this.ch_AccPlot.Series.Add("AccY");
            ch_AccPlot.Series[2] = this.ch_AccPlot.Series.Add("AccZ");

            ch_AccPlot.Series[0].ChartArea = chartAreaX.Name;
            ch_AccPlot.Series[1].ChartArea = chartAreaY.Name;
            ch_AccPlot.Series[2].ChartArea = chartAreaZ.Name;

            chartAreaX.AlignWithChartArea = chartAreaZ.Name;
            chartAreaY.AlignWithChartArea = chartAreaZ.Name;
            //chartAreaZ.AlignWithChartArea = chartAreaZ.Name;

            ch_AccPlot.Series[0].ChartType = SeriesChartType.FastLine;
            ch_AccPlot.Series[1].ChartType = SeriesChartType.FastLine;
            ch_AccPlot.Series[2].ChartType = SeriesChartType.FastLine;

            //ch_AccPlot.Series[0]
            //ch_AccPlot.Series[1].AxisLabel = "Y";
            //ch_AccPlot.Series[2].AxisLabel = "Z";


            chartAreaX.Position.Y = 0;
            chartAreaX.Position.Height = 33;
            chartAreaX.Position.Width = 100;
            chartAreaY.Position.Y = chartAreaX.Position.Bottom + 1;
            chartAreaY.Position.Height = chartAreaX.Position.Height;
            chartAreaY.Position.Width = chartAreaX.Position.Width;
            chartAreaZ.Position.Y = chartAreaY.Position.Bottom + 1;
            chartAreaZ.Position.Height = chartAreaY.Position.Height;
            chartAreaZ.Position.Width = chartAreaY.Position.Width;

            chartAreaX.AxisX.Maximum = 500;
            chartAreaX.AxisX.Minimum = 0;
            chartAreaY.AxisX.Maximum = 500;
            chartAreaY.AxisX.Minimum = 0;
            chartAreaZ.AxisX.Maximum = 500;
            chartAreaZ.AxisX.Minimum = 0;

            //chartAreaX.AxisY.Maximum = 20;
            //chartAreaX.AxisY.Minimum = -chartAreaX.AxisY.Maximum;
            //chartAreaY.AxisY.Maximum = chartAreaX.AxisY.Maximum;
            //chartAreaY.AxisY.Minimum = -chartAreaX.AxisY.Maximum;
            //chartAreaZ.AxisY.Maximum = chartAreaX.AxisY.Maximum;
            //chartAreaZ.AxisY.Minimum = -chartAreaX.AxisY.Maximum;

            chartAreaX.AxisX.MajorGrid.Enabled = false;
            chartAreaX.AxisY.MajorGrid.Enabled = false;
            chartAreaY.AxisX.MajorGrid.Enabled = false;
            chartAreaY.AxisY.MajorGrid.Enabled = false;
            chartAreaZ.AxisX.MajorGrid.Enabled = false;
            chartAreaZ.AxisY.MajorGrid.Enabled = false;

            chartAreaX.AxisY.Title = "AccX in m/s^2";
            chartAreaY.AxisY.Title = "AccY in m/s^2";
            chartAreaZ.AxisY.Title = "AccZ in m/s^2";

            ch_AccPlot.Series[0].BorderWidth = 2;
            ch_AccPlot.Series[1].BorderWidth = 2;
            ch_AccPlot.Series[2].BorderWidth = 2;
        }

        private void update_accChart(object sender, accelerationDataEventArgs e)
        {
            lock(m_chartLock)
            {
                ch_AccPlot.Invoke((Action)delegate
                {
                    ch_AccPlot.Series["AccX"].Points.AddXY(_currentChartValue, e.Accx);
                    ch_AccPlot.Series["AccY"].Points.AddXY(_currentChartValue, e.Accy);
                    ch_AccPlot.Series["AccZ"].Points.AddXY(_currentChartValue, e.Accz);
                    _currentChartValue++;
                    if (_currentChartValue >= _maxNumOfChartValues)
                    {
                        _currentChartValue = 0;
                        ch_AccPlot.Series[0].Points.Clear();
                        ch_AccPlot.Series[1].Points.Clear();
                        ch_AccPlot.Series[2].Points.Clear();
                    }
                });
            }
        }


    }
}
