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

        private static int _maxNumOfChartValues = 200;
        private int _currentChartValue = 0;

        private object m_chartLock = new object();

        //private Thread DataPlotThread;
        private System.Windows.Forms.Timer PlotDataTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateBatteryTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateCalibrationLabelTimer = new System.Windows.Forms.Timer();
        //private System.Threading.Thread PlotDataThread;
        //private BackgroundWorker PlotDataBackgroundWorker;


        public mw_form()
        {
            _BLEwatcher = new BLEwatcher();
            _BLEdriver = new BLEdriver();
            _BLEdriver.StatusChanged += BLEdriver_StatusChanged;
            _BLEdriver.ConnectedChanged += BLEDriver_ConnectedChanged;
            //_BLEdriver.ChangeLabel += Form_ChangeLabel;
            InitializeComponent();

            cb_SaveToFile.Enabled = false;
            cb_StreamTCP.Enabled = false;
            cb_plotAcc.Enabled = false;
            b_recalibrate.Enabled = false;
            p_BatteryPanel.Enabled = false;

            PlotDataTimer.Tick += new EventHandler(update_dataChart);
            PlotDataTimer.Interval = 25;
            PlotDataTimer.Enabled = false;

            UpdateBatteryTimer.Tick += new EventHandler(update_battery);
            UpdateBatteryTimer.Interval = 500;
            //UpdateBatteryTimer.Enabled = false;

            UpdateCalibrationLabelTimer.Tick += new EventHandler(update_calibLabel);
            UpdateCalibrationLabelTimer.Interval = 500;
            //UpdateCalibrationLabelTimer.Enabled = false;

            initialize_dataChart();
        }


        private void BLEDriver_ConnectedChanged(object sender, bool connected)
        {   
            //Check if Controls need to be invoked to enable or disable them and act accordingly
            var i = b_recalibrate.InvokeRequired == true ? b_recalibrate.Invoke((Action)(() => this.b_recalibrate.Enabled = connected ? true : false)) : this.b_recalibrate.Enabled = connected ? true : false;
            i = cb_plotAcc.InvokeRequired == true ? cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Enabled = connected ? true : false)) : this.cb_plotAcc.Enabled = connected ? true : false;
            i = p_BatteryPanel.InvokeRequired == true ? p_BatteryPanel.Invoke((Action)(() => this.p_BatteryPanel.Enabled = connected ? true : false)) : this.p_BatteryPanel.Enabled = connected ? true : false;
            i = cb_SaveToFile.InvokeRequired == true ? cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Enabled = connected ? true : false)) : this.cb_SaveToFile.Enabled = connected ? true : false;
            i = cb_StreamTCP.InvokeRequired == true ? cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Enabled = connected ? true : false)) : this.cb_StreamTCP.Enabled = connected ? true : false;

            //if (b_recalibrate.InvokeRequired)
            //{
            //    //b_recalibrate.Invoke((Action)(() => this.b_recalibrate.Enabled = connected ? true : false));
            //    //cb_plotAcc.Invoke((Action)(() => this.cb_plotAcc.Enabled = connected ? true : false));
            //    //p_BatteryPanel.Invoke((Action)(() => this.p_BatteryPanel.Enabled = connected ? true : false));
            //    //cb_SaveToFile.Invoke((Action)(() => this.cb_SaveToFile.Enabled = connected ? true : false));
            //    //cb_StreamTCP.Invoke((Action)(() => this.cb_StreamTCP.Enabled = connected ? true : false));
            //}
            //else
            //{
            //    //this.b_recalibrate.Enabled = connected ? true : false;
            //    //this.cb_plotAcc.Enabled = connected ? true : false;
            //    //this.p_BatteryPanel.Enabled = connected ? true : false;
            //    //this.cb_SaveToFile.Enabled = connected ? true : false;
            //    this.cb_StreamTCP.Enabled = connected ? true : false;
            //}

            if (connected)
            {
                pb_connect.BeginInvoke((Action)(() => this.pb_connect.Text = "Disconnect"));
                p_BatteryPanel.BeginInvoke((Action)(() => update_battery(null, null)));
                p_BatteryPanel.BeginInvoke((Action)(() => this.UpdateBatteryTimer.Start()));
                l_sys.BeginInvoke((Action)(() => this.UpdateCalibrationLabelTimer.Start()));
            }
            else
            {
                pb_connect.BeginInvoke((Action)(() => this.pb_connect.Text = "Connect"));
                p_BatteryPanel.BeginInvoke((Action)(() => this.UpdateBatteryTimer.Stop()));
                l_sys.BeginInvoke((Action)(() => this.UpdateCalibrationLabelTimer.Stop()));
            }
        }


        private void update_battery(object sender, System.EventArgs e)
        {
            var level = _BLEdriver.BatteryLevel;
            l_batteryLabel.Text = level.ToString() + "%";
            p_BatteryPanel.Width = level;
            if (level <= 30 && level > 10)
            {
                p_BatteryPanel.BackColor = Color.FromArgb(255, 127, 0);
            }
            else if (level <= 10)
            {
                p_BatteryPanel.BackColor = Color.FromArgb(255, 0, 0);
            }
            else
            {
                p_BatteryPanel.BackColor = Color.FromArgb(0, 127, 0);
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
                if(_BLEdriver.Busy) return;
                if(pb_connect.Text == "Connect")
                {
                    var selectedDevice = (BLEdevice)this.lv_Device_List.SelectedItems[0].Tag;

                    if (_BLEdriver != null)
                    {
                        _BLEdriver.ConnectDevice(selectedDevice);
                    }
                }
                else if(pb_connect.Text == "Disconnect")
                {
                    _BLEdriver.Disconnect();
                }
                //pb_connect.Enabled = false;
            }
            catch(System.ArgumentOutOfRangeException)
            {
                Console.WriteLine("Nothing Highlighted");
            }
        }

        //private void Form_ChangeLabel(object sender, changeLabelEventArgs e)
        //{
        //    foreach (Control x in this.Controls)
        //    {
        //        if (x.Name == e.label)
        //        {
        //            if(x.InvokeRequired)
        //                x.Invoke((Action)delegate { x.Text = e.value; });
        //            else
        //                x.Text = e.value;
        //        }
        //    }
        //}

        private void update_calibLabel(object sender, EventArgs e)
        {
            var calib = _BLEdriver.Calibration;
            l_sys.Text = calib[0];
            l_gyr.Text = calib[1];
            l_acc.Text = calib[2];
            l_mag.Text = calib[3];
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
                _currentChartValue = 0;
                ch_dataPlot.Series[0].Points.Clear();
                ch_dataPlot.Series[1].Points.Clear();
                ch_dataPlot.Series[2].Points.Clear();
                ch_dataPlot.Series[3].Points.Clear();
                ch_dataPlot.Series[4].Points.Clear();
                ch_dataPlot.Series[5].Points.Clear();

                //_plotting = true;
                //PlotDataBackgroundWorker = new BackgroundWorker();
                //PlotDataBackgroundWorker.DoWork += PlotData;
                //PlotDataBackgroundWorker.ProgressChanged += PlotData_ProgressChanged;
                //PlotDataBackgroundWorker.WorkerReportsProgress = true;
                //PlotDataBackgroundWorker.WorkerSupportsCancellation = true;
                //PlotDataBackgroundWorker.RunWorkerAsync();

                PlotDataTimer.Enabled = true;
                this.PlotDataTimer.Start();
                //watch.Start();
            }
            else
            {
                //_plotting = false;
                //PlotDataBackgroundWorker.CancelAsync();
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
            var selectedDevice = (BLEdevice)this.lv_Device_List.SelectedItems[0].Tag;
            _BLEdriver.Recalibrate_imu(selectedDevice);
        }

        private void mw_form_Load(object sender, EventArgs e)
        {

        }

        private void initialize_dataChart()
        {
            
            this.ch_dataPlot.Series.Clear();

            //ch_AccPlot.ChartType = SeriesChartType.Spline;

            //this.ch_AccPlot.Series.Add("AccX");
            //this.ch_AccPlot.Series.Add("AccY");
            //this.ch_AccPlot.Series.Add("AccZ");
            var chartAreaAccX = ch_dataPlot.ChartAreas.Add("CaAccX");
            var chartAreaAccY = ch_dataPlot.ChartAreas.Add("CaAccY");
            var chartAreaAccZ = ch_dataPlot.ChartAreas.Add("CaAccZ");
            var chartAreaGyrX = ch_dataPlot.ChartAreas.Add("CaGyrX");
            var chartAreaGyrY = ch_dataPlot.ChartAreas.Add("CaGyrY");
            var chartAreaGyrZ = ch_dataPlot.ChartAreas.Add("CaGyrZ");

            ch_dataPlot.Series[0] = this.ch_dataPlot.Series.Add("AccX");
            ch_dataPlot.Series[1] = this.ch_dataPlot.Series.Add("AccY");
            ch_dataPlot.Series[2] = this.ch_dataPlot.Series.Add("AccZ");
            ch_dataPlot.Series[3] = this.ch_dataPlot.Series.Add("GyrX");
            ch_dataPlot.Series[4] = this.ch_dataPlot.Series.Add("GyrY");
            ch_dataPlot.Series[5] = this.ch_dataPlot.Series.Add("GyrZ");

            var leg1 = ch_dataPlot.Legends.Add("l1");
            var leg2 = ch_dataPlot.Legends.Add("l2");
            var leg3 = ch_dataPlot.Legends.Add("l3");
            var leg4 = ch_dataPlot.Legends.Add("l4");
            var leg5 = ch_dataPlot.Legends.Add("l5");
            var leg6 = ch_dataPlot.Legends.Add("l6");

            ch_dataPlot.Series[0].ChartArea = chartAreaAccX.Name;
            ch_dataPlot.Series[1].ChartArea = chartAreaAccY.Name;
            ch_dataPlot.Series[2].ChartArea = chartAreaAccZ.Name;
            ch_dataPlot.Series[3].ChartArea = chartAreaGyrX.Name;
            ch_dataPlot.Series[4].ChartArea = chartAreaGyrY.Name;
            ch_dataPlot.Series[5].ChartArea = chartAreaGyrZ.Name;

            ch_dataPlot.Series[0].Legend = leg1.Name;
            ch_dataPlot.Series[1].Legend = leg2.Name;
            ch_dataPlot.Series[2].Legend = leg3.Name;
            ch_dataPlot.Series[3].Legend = leg4.Name;
            ch_dataPlot.Series[4].Legend = leg5.Name;
            ch_dataPlot.Series[5].Legend = leg6.Name;

            chartAreaAccY.AlignWithChartArea = chartAreaAccX.Name;
            chartAreaAccZ.AlignWithChartArea = chartAreaAccX.Name;
            chartAreaGyrY.AlignWithChartArea = chartAreaGyrX.Name;
            chartAreaGyrZ.AlignWithChartArea = chartAreaGyrX.Name;
            //chartAreaZ.AlignWithChartArea = chartAreaZ.Name;

            ch_dataPlot.Series[0].ChartType = SeriesChartType.FastLine;
            ch_dataPlot.Series[1].ChartType = SeriesChartType.FastLine;
            ch_dataPlot.Series[2].ChartType = SeriesChartType.FastLine;
            ch_dataPlot.Series[3].ChartType = SeriesChartType.FastLine;
            ch_dataPlot.Series[4].ChartType = SeriesChartType.FastLine;
            ch_dataPlot.Series[5].ChartType = SeriesChartType.FastLine;
            //ch_AccPlot.Series[0]
            //ch_AccPlot.Series[1].AxisLabel = "Y";
            //ch_AccPlot.Series[2].AxisLabel = "Z";

            //Acc
            chartAreaAccX.Position.X = 0;
            chartAreaAccX.Position.Y = 0;
            chartAreaAccX.Position.Height = 33;
            chartAreaAccX.Position.Width = 49;
            chartAreaAccY.Position.Y = chartAreaAccX.Position.Bottom + 1;
            chartAreaAccY.Position.Height = chartAreaAccX.Position.Height;
            chartAreaAccY.Position.Width = chartAreaAccX.Position.Width;
            chartAreaAccZ.Position.Y = chartAreaAccY.Position.Bottom + 1;
            chartAreaAccZ.Position.Height = chartAreaAccY.Position.Height;
            chartAreaAccZ.Position.Width = chartAreaAccY.Position.Width;

            //Gyr
            chartAreaGyrX.Position.X = 51;
            chartAreaGyrX.Position.Y = 0;
            chartAreaGyrX.Position.Height = 33;
            chartAreaGyrX.Position.Width = 49;
            chartAreaGyrY.Position.Y = chartAreaGyrX.Position.Bottom + 1;
            chartAreaGyrY.Position.Height = chartAreaGyrX.Position.Height;
            chartAreaGyrY.Position.Width = chartAreaGyrX.Position.Width;
            chartAreaGyrZ.Position.Y = chartAreaGyrY.Position.Bottom + 1;
            chartAreaGyrZ.Position.Height = chartAreaGyrY.Position.Height;
            chartAreaGyrZ.Position.Width = chartAreaGyrY.Position.Width;

            leg1.Position.Auto = false;
            leg1.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccX.Position.Y, 10, 10);
            leg2.Position.Auto = false;
            leg2.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccY.Position.Y, 10, 10);
            leg3.Position.Auto = false;
            leg3.Position = new ElementPosition(chartAreaAccX.Position.X + 40, chartAreaAccZ.Position.Y, 10, 10);
            leg4.Position.Auto = false;
            leg4.Position = new ElementPosition(chartAreaGyrX.Position.X + 40, chartAreaGyrX.Position.Y, 10, 10);
            leg5.Position.Auto = false;
            leg5.Position = new ElementPosition(chartAreaGyrX.Position.X + 40, chartAreaGyrY.Position.Y, 10, 10);
            leg6.Position.Auto = false;
            leg6.Position = new ElementPosition(chartAreaGyrX.Position.X + 40, chartAreaGyrZ.Position.Y, 10, 10);

            chartAreaAccX.AxisX.Maximum = _maxNumOfChartValues;
            chartAreaAccX.AxisX.Minimum = 0;
            chartAreaAccY.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaAccY.AxisX.Minimum = 0;
            chartAreaAccZ.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaAccZ.AxisX.Minimum = 0;
            chartAreaGyrX.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaGyrX.AxisX.Minimum = 0;
            chartAreaGyrY.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaGyrY.AxisX.Minimum = 0;
            chartAreaGyrZ.AxisX.Maximum = chartAreaAccX.AxisX.Maximum;
            chartAreaGyrZ.AxisX.Minimum = 0;

            //chartAreaX.AxisY.Maximum = 20;
            //chartAreaX.AxisY.Minimum = -chartAreaX.AxisY.Maximum;
            //chartAreaY.AxisY.Maximum = chartAreaX.AxisY.Maximum;
            //chartAreaY.AxisY.Minimum = -chartAreaX.AxisY.Maximum;
            //chartAreaZ.AxisY.Maximum = chartAreaX.AxisY.Maximum;
            //chartAreaZ.AxisY.Minimum = -chartAreaX.AxisY.Maximum;

            chartAreaAccX.AxisX.MajorGrid.Enabled = false;
            chartAreaAccX.AxisY.MajorGrid.Enabled = false;
            chartAreaAccY.AxisX.MajorGrid.Enabled = false;
            chartAreaAccY.AxisY.MajorGrid.Enabled = false;
            chartAreaAccZ.AxisX.MajorGrid.Enabled = false;
            chartAreaAccZ.AxisY.MajorGrid.Enabled = false;
            chartAreaGyrX.AxisX.MajorGrid.Enabled = false;
            chartAreaGyrX.AxisY.MajorGrid.Enabled = false;
            chartAreaGyrY.AxisX.MajorGrid.Enabled = false;
            chartAreaGyrY.AxisY.MajorGrid.Enabled = false;
            chartAreaGyrZ.AxisX.MajorGrid.Enabled = false;
            chartAreaGyrZ.AxisY.MajorGrid.Enabled = false;

            //chartAreaAccX.AxisY.Title = "AccX in m/s^2";
            //chartAreaAccY.AxisY.Title = "AccY in m/s^2";
            //chartAreaAccZ.AxisY.Title = "AccZ in m/s^2";

            ch_dataPlot.Series[0].BorderWidth = 2;
            ch_dataPlot.Series[1].BorderWidth = 2;
            ch_dataPlot.Series[2].BorderWidth = 2;
            ch_dataPlot.Series[3].BorderWidth = 2;
            ch_dataPlot.Series[4].BorderWidth = 2;
            ch_dataPlot.Series[5].BorderWidth = 2;
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

            lock (m_chartLock)
            {
                //ch_dataPlot.Invoke((Action)delegate
                //{var data = _BLEdriver.GetDataToPlot();
                var data = _BLEdriver.GetDataToPlot();
                ch_dataPlot.Series[0].Points.AddXY(_currentChartValue, data[0]);
                ch_dataPlot.Series[1].Points.AddXY(_currentChartValue, data[1]);
                ch_dataPlot.Series[2].Points.AddXY(_currentChartValue, data[2]);
                ch_dataPlot.Series[3].Points.AddXY(_currentChartValue, data[3]);
                ch_dataPlot.Series[4].Points.AddXY(_currentChartValue, data[4]);
                ch_dataPlot.Series[5].Points.AddXY(_currentChartValue, data[5]);
                _currentChartValue++;
                if (_currentChartValue >= _maxNumOfChartValues)
                {
                    _currentChartValue = 0;
                    ch_dataPlot.Series[0].Points.Clear();
                    ch_dataPlot.Series[1].Points.Clear();
                    ch_dataPlot.Series[2].Points.Clear();
                    ch_dataPlot.Series[3].Points.Clear();
                    ch_dataPlot.Series[4].Points.Clear();
                    ch_dataPlot.Series[5].Points.Clear();
                }
                //});
            }
            //Console.WriteLine(watch.ElapsedMilliseconds);
            //watch.Restart();
        }


    }
}
