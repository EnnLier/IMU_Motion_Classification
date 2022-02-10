using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
        //private BLEhub _BLEdriver;
        private static int _maxNumOfChartValues = 200;
        private int _currentChartValue = 0;

        private object m_chartLock = new object();

        //private Thread DataPlotThread;
        private System.Windows.Forms.Timer PlotDataTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateBatteryTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateCalibrationLabelTimer = new System.Windows.Forms.Timer();

        private List<Chart> _ChartList;
        private List<System.Windows.Forms.Label> _CalibLabelList;
        private List<System.Windows.Forms.Panel> _BatteryPanelList;
        private List<System.Windows.Forms.Label> _BatteryLabelList;
        private List<System.Windows.Forms.Button> _ButtonList;

        private List<Control> _FrontDeviceControlList;
        private List<Control> _BackDeviceControlList;
        private List<List<Control>> _DeviceControlList;

        public mw_form()
        {
            _BLEwatcher = new BLEwatcher();
            _BLEdriver = new BLEdriver();
            //_BLEdriver = new BLEhub();
            _BLEdriver.StatusChanged += BLEdriver_StatusChanged;
            _BLEdriver.ConnectedChanged += BLEDriver_ConnectedChanged;
            //_BLEdriver.ChangeLabel += Form_ChangeLabel;
            InitializeComponent();

            cb_SaveToFile.Enabled = false;
            cb_StreamTCP.Enabled = false;
            cb_plotAcc.Enabled = false;
            b_recalibrate.Enabled = false;
            p_frontBatteryPanel.Enabled = false;
            //p_frontCalibPanel.Visible = false;
            //p_backCalibPanel.Visible = false;
            //ch_frontDataPlot.Visible = false;
            //ch_backDataPlot.Visible = false;
            //p_backBatteryPanel.Visible = false;
            //p_frontBatteryPanel.Visible = false;
            //l_batteryLabelBack.Visible = false;
            //l_batteryLabelFront.Visible = false;

            _ChartList = new List<Chart>() { ch_frontDataPlot, ch_backDataPlot };
            _CalibLabelList = new List<System.Windows.Forms.Label>() { l_sysFront, l_gyrFront, l_accFront, l_magFront, l_sysBack, l_gyrBack, l_accBack, l_magBack};
            _BatteryPanelList = new List<System.Windows.Forms.Panel>() { p_frontBatteryPanel, p_backBatteryPanel };
            _ButtonList = new List<System.Windows.Forms.Button>() { pb_frontConnect, pb_backConnect };
            _BatteryLabelList = new List<Label> {l_batteryLabelFront, l_batteryLabelBack };
            _FrontDeviceControlList = new List<Control>() { ch_frontDataPlot, p_frontCalibPanel, p_frontBatteryPanel, l_batteryLabelFront };
            _BackDeviceControlList = new List<Control>() { ch_backDataPlot, p_backCalibPanel, p_backBatteryPanel, l_batteryLabelBack };
            _DeviceControlList = new List<List<Control>>() { _FrontDeviceControlList, _BackDeviceControlList };

            foreach (var List in _DeviceControlList)
            {
                foreach ( var control in List)
                {
                    control.Enabled = false;
                    control.Visible = false;
                }
            }
            PlotDataTimer.Tick += new EventHandler(update_dataChart);
            PlotDataTimer.Interval = 25;
            PlotDataTimer.Enabled = false;

            UpdateBatteryTimer.Tick += new EventHandler(update_battery);
            UpdateBatteryTimer.Interval = 500;

            UpdateCalibrationLabelTimer.Tick += new EventHandler(update_calibLabel);
            UpdateCalibrationLabelTimer.Interval = 500;

            initialize_dataChart(this.ch_frontDataPlot);
            initialize_dataChart(this.ch_backDataPlot);
        }


        private void BLEDriver_ConnectedChanged(object sender, ConnectedChangedEventArgs args)
        {
            //pb_Refresh_List.BeginInvoke((Action)(() => pb_Refresh_List_Click(null,null)));
            var connected = args.status;
            var id = args.deviceInformation.SensorID;
            //Console.WriteLine(args.deviceInformation.SensorID);
            //Check if Controls need to be invoked to enable or disable them and act accordingly
            //var i = b_recalibrate.InvokeRequired == true ? b_recalibrate.Invoke((Action)(() => this.b_recalibrate.Enabled = connected ? true : false)) : this.b_recalibrate.Enabled = connected ? true : false;
            //i = cb_plotAcc.InvokeRequired == true ? cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Enabled = connected ? true : false)) : this.cb_plotAcc.Enabled = connected ? true : false;
            //i = p_frontBatteryPanel.InvokeRequired == true ? p_frontBatteryPanel.Invoke((Action)(() => this.p_frontBatteryPanel.Enabled = connected ? true : false)) : this.p_frontBatteryPanel.Enabled = connected ? true : false;
            //i = cb_SaveToFile.InvokeRequired == true ? cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Enabled = connected ? true : false)) : this.cb_SaveToFile.Enabled = connected ? true : false;
            //i = cb_StreamTCP.InvokeRequired == true ? cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Enabled = connected ? true : false)) : this.cb_StreamTCP.Enabled = connected ? true : false;
            Console.WriteLine("Connected devices: " + _BLEdriver.Connected);
            _ButtonList[id].Invoke((Action)delegate
            {
                foreach (var control in _DeviceControlList[id])
                {
                    control.Enabled = connected;
                    control.Visible = connected;
                }
            });
            if (_BLEdriver.Connected == 0) //All devices disconnected
            {
                _BatteryPanelList[id].BeginInvoke((Action)(() => this.UpdateBatteryTimer.Stop()));
                _CalibLabelList[id].BeginInvoke((Action)(() => this.UpdateCalibrationLabelTimer.Stop()));
                _ChartList[id].BeginInvoke((Action)(() => this.PlotDataTimer.Stop()));
                b_recalibrate.Invoke((Action)(() => this.b_recalibrate.Enabled = false));
                p_frontBatteryPanel.Invoke((Action)(() => this.p_frontBatteryPanel.Enabled = false));
                cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Enabled = false));
                cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Checked = false));
                cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Enabled = false));
                cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Checked = false));
                cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Enabled = false));
                cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Checked = false));
            }
            else
            {
                b_recalibrate.Invoke((Action)(() => this.b_recalibrate.Enabled = true));
                cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Enabled = true));
                cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Enabled = true));
                cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Enabled = true));
            }
            if (connected)
            {
                _ButtonList[id].BeginInvoke((Action)(() => _ButtonList[id].Text = _ButtonList[id].Text.Replace("Connect","Disconnect")));
                //_BatteryPanelList[deviceInformation.SensorID].BeginInvoke((Action)(() => update_battery(null, null)));
                _BatteryPanelList[id].BeginInvoke((Action)(() => this.UpdateBatteryTimer.Start()));
                _CalibLabelList[id].BeginInvoke((Action)(() => this.UpdateCalibrationLabelTimer.Start()));

            }
            else
            {
                _ButtonList[id].BeginInvoke((Action)(() => _ButtonList[id].Text = _ButtonList[id].Text.Replace("Disconnect", "Connect")));
            }
        }


        private void update_battery(object sender, System.EventArgs e)
        {
            foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                var level = device.BatteryLevel;
                var id = device.SensorID;

                _BatteryLabelList[id].Text = level.ToString() + "%";
                _BatteryPanelList[id].Width = level;
                if (level <= 30 && level > 10)
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(255, 127, 0);
                }
                else if (level <= 10)
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(255, 0, 0);
                }
                else
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(0, 127, 0);
                }
            }
            
            
        }

        private void Mainwindow_Load(object sender, EventArgs e)
        {
            
        }

        private void pb_Refresh_List_Click(object sender, EventArgs e)
        {
            this.lv_Device_List.Items.Clear();
            lock(_BLEwatcher.mListLock)
            {
                foreach (var device in _BLEwatcher.deviceList)
                {
                    if (String.IsNullOrEmpty(device.Name)) continue;
                    var row = new String[] { device.Name, device.BLEId, device.canPair.ToString() };
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
                if (_BLEdriver.Busy) return;
                //var button = (Button)sender;
                var id = _ButtonList.IndexOf((Button)sender);
                
                if (_ButtonList[id].Text.Contains("Connect "))
                {
                    var selection = (BLEDeviceInformation)this.lv_Device_List.SelectedItems[0].Tag;

                    //Make shallow copy of object to not change device_list entries on connect
                    BLEDeviceInformation selectedDevice = selection.Clone();

                    selectedDevice.SensorID = id;

                    if (_BLEdriver != null)
                    {
                        _BLEdriver.ConnectDevice(selectedDevice);
                    }
                    //initialize_dataChart(_ChartList[id]);
                }
                else if (_ButtonList[id].Text.Contains( "Disconnect"))
                {
                    _BLEdriver.Disconnect(_BLEdriver.ConnectedDeviceInformationList.Find(item => item.SensorID == id));
                }
                //pb_connect.Enabled = false;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                Console.WriteLine("Nothing Highlighted");
            }
        }


        private void update_calibLabel(object sender, EventArgs e)
        {
            foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                var calib = device.Calibration;
                
                var id = device.SensorID;

                _CalibLabelList[4 * id + 0].Text = calib[0].ToString();
                _CalibLabelList[4 * id + 1].Text = calib[1].ToString();
                _CalibLabelList[4 * id + 2].Text = calib[2].ToString();
                _CalibLabelList[4 * id + 3].Text = calib[3].ToString();
            }
            
        }

        private void BLEdriver_StatusChanged(object sender, statusChangedEventArgs e)
        {
            var timestamp = e.Timestamp.ToString("HH:mm:ss");
            //if(!_BLEdriver.Connected)
            //{
            //    cb_plotAcc.Checked = false;
            //}
            if (l_Driver_Status.InvokeRequired)
                l_Driver_Status.Invoke((Action)delegate { this.l_Driver_Status.Text = timestamp + "      " + e.Status; });
            else
                this.l_Driver_Status.Text = timestamp + "      " + e.Status;
            //catch(System.InvalidOperationException)
            //{
                
            //    //cb_StreamTCP.Invoke((Action)delegate
            //    //{
            //    //    this.cb_StreamTCP.Enabled = _BLEdriver.Connected == true ? true : false;
            //    //    this.cb_SaveToFile.Enabled = _BLEdriver.Connected == true ? true : false;
            //    //    this.cb_plotAcc.Enabled = _BLEdriver.Connected == true ? true : false;
            //    //    this.b_recalibrate.Enabled = _BLEdriver.Connected == true ? true : false;
            //    //    this.p_BatteryPanel.Enabled = _BLEdriver.Connected == true ? true : false;

            //    //});
            //}
            
        }


        private void cb_StreamTCP_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_StreamTCP.Checked)
            {
                _BLEdriver.StartStreaming();
            } 
            else
            {
                _BLEdriver.StopStreaming();
            }
        }

        private void cb_plotAcc_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_plotAcc.Checked)
            {
                foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
                {
                    var id = device.SensorID;
                    _currentChartValue = 0;
                    _ChartList[id].Series[0].Points.Clear();
                    _ChartList[id].Series[1].Points.Clear();
                    _ChartList[id].Series[2].Points.Clear();
                }
                PlotDataTimer.Enabled = true;
                this.PlotDataTimer.Start();
            }
            else
            {
                PlotDataTimer.Enabled = false;
                this.PlotDataTimer.Stop();

            }
        }


        private void cb_SaveToFile_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_SaveToFile.Checked)
            {
                _BLEdriver.StartSaving();
            }
            else
            {
                _BLEdriver.StopSaving();
            }
        }


        private void b_recalibrate_Click(object sender, EventArgs e)
        {
            var selectedDevice = (BLEDeviceInformation)this.lv_Device_List.SelectedItems[0].Tag;
            _BLEdriver.Recalibrate_imu(selectedDevice);
        }

        private void mw_form_Load(object sender, EventArgs e)
        {

        }

        private void initialize_dataChart(Chart chart)
        {
            chart.Series.Clear();

            var chartAreaAccX = chart.ChartAreas.Add("CaAccX");
            var chartAreaAccY = chart.ChartAreas.Add("CaAccY");
            var chartAreaAccZ = chart.ChartAreas.Add("CaAccZ");

            chart.Series[0] = chart.Series.Add("AccX");
            chart.Series[1] = chart.Series.Add("AccY");
            chart.Series[2] = chart.Series.Add("AccZ");

            var leg1 = chart.Legends.Add("l1");
            var leg2 = chart.Legends.Add("l2");
            var leg3 = chart.Legends.Add("l3");

            chart.Series[0].ChartArea = chartAreaAccX.Name;
            chart.Series[1].ChartArea = chartAreaAccY.Name;
            chart.Series[2].ChartArea = chartAreaAccZ.Name;

            chart.Series[0].Legend = leg1.Name;
            chart.Series[1].Legend = leg2.Name;
            chart.Series[2].Legend = leg3.Name;

            chartAreaAccY.AlignWithChartArea = chartAreaAccX.Name;
            chartAreaAccZ.AlignWithChartArea = chartAreaAccX.Name;

            chart.Series[0].ChartType = SeriesChartType.FastLine;
            chart.Series[1].ChartType = SeriesChartType.FastLine;
            chart.Series[2].ChartType = SeriesChartType.FastLine;

            //Acc
            chartAreaAccX.Position.X = 0;
            chartAreaAccX.Position.Y = 0;
            chartAreaAccX.Position.Height = 33;
            chartAreaAccX.Position.Width = 100;
            chartAreaAccY.Position.Y = chartAreaAccX.Position.Bottom + 1;
            chartAreaAccY.Position.Height = chartAreaAccX.Position.Height;
            chartAreaAccY.Position.Width = chartAreaAccX.Position.Width;
            chartAreaAccZ.Position.Y = chartAreaAccY.Position.Bottom + 1;
            chartAreaAccZ.Position.Height = chartAreaAccY.Position.Height;
            chartAreaAccZ.Position.Width = chartAreaAccY.Position.Width;

            leg1.Position.Auto = false;
            leg1.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccX.Position.Y, 10, 10);
            leg2.Position.Auto = false;
            leg2.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccY.Position.Y, 10, 10);
            leg3.Position.Auto = false;
            leg3.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccZ.Position.Y, 10, 10);

            chartAreaAccX.AxisX.Maximum = _maxNumOfChartValues;
            chartAreaAccX.AxisX.Minimum = 0;
            chartAreaAccY.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaAccY.AxisX.Minimum = 0;
            chartAreaAccZ.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaAccZ.AxisX.Minimum = 0;

            chartAreaAccX.AxisX.MajorGrid.Enabled = false;
            chartAreaAccX.AxisY.MajorGrid.Enabled = false;
            chartAreaAccY.AxisX.MajorGrid.Enabled = false;
            chartAreaAccY.AxisY.MajorGrid.Enabled = false;
            chartAreaAccZ.AxisX.MajorGrid.Enabled = false;
            chartAreaAccZ.AxisY.MajorGrid.Enabled = false;

            chart.Series[0].BorderWidth = 2;
            chart.Series[1].BorderWidth = 2;
            chart.Series[2].BorderWidth = 2;
        }

        //private void PlotData_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    var data = _BLEdriver.GetDataToPlot();
        //    ch_dataPlot.Series[0].Points.AddXY(_currentChartValue, data[0]);
        //    ch_dataPlot.Series[1].Points.AddXY(_currentChartValue, data[1]);
        //    ch_dataPlot.Series[2].Points.AddXY(_currentChartValue, data[2]);
        //    ch_dataPlot.Series[3].Points.AddXY(_currentChartValue, data[3]);
        //    ch_dataPlot.Series[4].Points.AddXY(_currentChartValue, data[4]);
        //    ch_dataPlot.Series[5].Points.AddXY(_currentChartValue, data[5]);
        //    _currentChartValue++;
        //    if (_currentChartValue >= _maxNumOfChartValues)
        //    {
        //        _currentChartValue = 0;
        //        ch_dataPlot.Series[0].Points.Clear();
        //        ch_dataPlot.Series[1].Points.Clear();
        //        ch_dataPlot.Series[2].Points.Clear();
        //        ch_dataPlot.Series[3].Points.Clear();
        //        ch_dataPlot.Series[4].Points.Clear();
        //        ch_dataPlot.Series[5].Points.Clear();
        //    }
        //}

        //private void PlotData(object sender, DoWorkEventArgs e)
        //{
        //    Console.WriteLine("Thread Started");
        //    Stopwatch PlotTickWatch = new Stopwatch();
        //    PlotTickWatch.Start();
        //    //this.ch_dataPlot.BeginInvoke((Action)delegate
        //    //{
        //        while (_plotting)
        //        {
        //            PlotDataBackgroundWorker.ReportProgress(0);
        //            while(PlotTickWatch.Elapsed.TotalMilliseconds <= 10)
        //            {
        //                Thread.Sleep(1);
        //            }
        //            PlotTickWatch.Restart();
        //        }
        //    //});
        //}

        //Stopwatch watch = new Stopwatch();
        private void update_dataChart(object sender, System.EventArgs e)
        {
            //Console.WriteLine(watch.ElapsedMilliseconds);
            //watch.Restart();
            //if(!_BLEdriver.Connected) return;

            foreach(var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                lock (m_chartLock)
                {
                    var data = device.Data;
                    var id = device.SensorID;
                    _ChartList[id].Series[0].Points.AddXY(_currentChartValue, data[4]);
                    _ChartList[id].Series[1].Points.AddXY(_currentChartValue, data[5]);
                    _ChartList[id].Series[2].Points.AddXY(_currentChartValue, data[6]);
                    if (_currentChartValue >= _maxNumOfChartValues)
                    {
                        _ChartList[id].Series[0].Points.Clear();
                        _ChartList[id].Series[1].Points.Clear();
                        _ChartList[id].Series[2].Points.Clear();
                    }
                }
            }
            if (_currentChartValue >= _maxNumOfChartValues) _currentChartValue = 0;
            _currentChartValue++;
        }

    }
}
