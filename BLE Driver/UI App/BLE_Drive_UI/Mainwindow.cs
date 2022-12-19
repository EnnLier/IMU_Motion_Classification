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
        //BLE related objects
        private BLEwatcher _BLEwatcher;
        private BLEdriver _BLEdriver;

        //Number of values allowed in plot
        private static int _maxNumOfChartValues = 200;
        //current value to write to plot
        private int _currentChartValue = 0;
        //Mutex for plot
        private object m_chartLock = new object();

        //Timer which update Plot, Battery and Calibrationvalues
        private System.Windows.Forms.Timer PlotDataTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateBatteryTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer UpdateCalibrationLabelTimer = new System.Windows.Forms.Timer();

        //Lists of Forms to ease enabling and disabling
        private List<Chart> _ChartList;
        private List<System.Windows.Forms.Label> _CalibLabelList;
        private List<System.Windows.Forms.Panel> _BatteryPanelList;
        private List<System.Windows.Forms.Label> _BatteryLabelList;
        private List<System.Windows.Forms.Button> _ButtonList;
        private List<Control> _FrontDeviceControlList;
        private List<Control> _BackDeviceControlList;
        private List<List<Control>> _DeviceControlList;

        //Logwriter
        private LogWriter _logWriter;

        /// <summary>
        /// Create all objects and initialize Mainwindow.
        /// </summary>
        public mw_form()
        {
            _logWriter = new LogWriter();
            _BLEwatcher = new BLEwatcher();
            _BLEdriver = new BLEdriver();

            //Register eventhandler
            _BLEdriver.StatusChanged += BLEdriver_StatusChanged;
            _BLEdriver.ConnectedChanged += BLEDriver_ConnectedChanged;
            _BLEdriver.WriteLogEntry += OnLogfileEntry;

            //Initialize Mainwindow
            InitializeComponent();

            //Initially disable most forms
            cb_SaveToFile.Enabled = false;
            cb_StreamTCP.Enabled = false;
            cb_plotAcc.Enabled = false;
            b_recalibrate.Enabled = false;
            p_frontBatteryPanel.Enabled = false;

            //Fill Form lists with forms
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
            //Initialize Timer
            PlotDataTimer.Tick += new EventHandler(update_dataChart);
            PlotDataTimer.Interval = 25;
            PlotDataTimer.Enabled = false;

            UpdateBatteryTimer.Tick += new EventHandler(update_battery);
            UpdateBatteryTimer.Interval = 500;

            UpdateCalibrationLabelTimer.Tick += new EventHandler(update_calibLabel);
            UpdateCalibrationLabelTimer.Interval = 500;

            //Create Chart for plotting
            initialize_dataChart(this.ch_frontDataPlot);
            initialize_dataChart(this.ch_backDataPlot);
        }

        ~mw_form()
        {
            _logWriter.Save();
        }

        /// <summary>
        /// This callbakc function is called if a BLE device Connects or Disconnects from the driver
        /// </summary>
        /// <param name="sender">Device Connecting/Disconnecting</param>
        /// <param name="args">Deviceinformation</param>
        private void BLEDriver_ConnectedChanged(object sender, ConnectedChangedEventArgs args)
        {
            //Current status - Connected/Disconnected
            var connected = args.Status;
            //Sensor ID
            var id = args.DeviceInformation.SensorID;

            //Enable or disable device relevant buttons according to connection status
            _ButtonList[id].Invoke((Action)delegate
            {
                foreach (var control in _DeviceControlList[id])
                {
                    control.Enabled = connected;
                    control.Visible = connected;
                }
            });

            //All devices disconnected. Stop all timer and disable all GUI elements
            if (_BLEdriver.Connected == 0) 
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
                _BatteryPanelList[id].BeginInvoke((Action)(() => this.UpdateBatteryTimer.Start()));
                _CalibLabelList[id].BeginInvoke((Action)(() => this.UpdateCalibrationLabelTimer.Start()));
            }
            else
            {
                _ButtonList[id].BeginInvoke((Action)(() => _ButtonList[id].Text = _ButtonList[id].Text.Replace("Disconnect", "Connect")));
            }
        }

        /// <summary>
        /// Callback function of Battery timer. Updates Batterylevel on GUI
        /// </summary>
        /// <param name="sender">Timer which is calling this function</param>
        /// <param name="e">Empty args</param>
        private void update_battery(object sender, System.EventArgs e)
        {
            //Update Form for each device
            foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                //Get Batterylevel and Sensorid of each device
                var level = device.BatteryLevel;
                var id = device.SensorID;

                //Change size and color of battery diagram and change displayed value
                _BatteryLabelList[id].Text = level.ToString() + "%";
                _BatteryPanelList[id].Width = level;
                //Orange
                if (level <= 30 && level > 10)
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(255, 127, 0);
                }
                //Red
                else if (level <= 10)
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(255, 0, 0);
                }
                //Green
                else
                {
                    _BatteryPanelList[id].BackColor = Color.FromArgb(0, 127, 0);
                }
            }
        }

        private void Mainwindow_Load(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Callback function of refresh button. Updates displayed list in GUI with updated list of BLEwatcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pb_Refresh_List_Click(object sender, EventArgs e)
        {
            //Clear current display
            this.lv_Device_List.Items.Clear();
            lock(_BLEwatcher.mListLock)
            {
                //Show only relevant information of each devices listed in devicelist
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

        /// <summary>
        /// Callback function of Connect button. Connect to and Disconnect from selected device. Acts as a callback function for to Buttons Connect Front and Connect Back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pb_connect_Click(object sender, EventArgs e)
        {
            try
            {
                //Return if Driver is currently connecting
                if (_BLEdriver.Busy) return;

                // id is 0 for front and 1 for back. This ID acts also as the sensorID 
                var id = _ButtonList.IndexOf((Button)sender);
                
                //Connect
                if (_ButtonList[id].Text.Contains("Connect "))
                {
                    //Get selected device from list
                    var selection = (BLEDeviceInformation)this.lv_Device_List.SelectedItems[0].Tag;

                    //Make shallow copy of object to not change device_list entries on connect
                    BLEDeviceInformation selectedDevice = selection.Clone();

                    //ID corresponding to front or Back sensor now defines sensoID
                    selectedDevice.SensorID = id;

                    //Connect to this device if driver exists
                    if (_BLEdriver != null)
                    {
                        _BLEdriver.ConnectDevice(selectedDevice);
                    }
                    //initialize_dataChart(_ChartList[id]);
                }
                //Disconnect
                else if (_ButtonList[id].Text.Contains( "Disconnect"))
                {
                    _BLEdriver.Disconnect(_BLEdriver.ConnectedDeviceInformationList.Find(item => item.SensorID == id));
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {
                Console.WriteLine("Nothing Highlighted");
            }
        }

        /// <summary>
        /// Callback function for Calib label timer. This function updates the displayed calirbation values 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void update_calibLabel(object sender, EventArgs e)
        {
            //Update each connected device
            foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                //Get calibration value
                var calib = device.Calibration;
                //Get sensor ID
                var id = device.SensorID;

                //Write Value to GUI
                _CalibLabelList[4 * id + 0].Text = calib[0].ToString();
                _CalibLabelList[4 * id + 1].Text = calib[1].ToString();
                _CalibLabelList[4 * id + 2].Text = calib[2].ToString();
                _CalibLabelList[4 * id + 3].Text = calib[3].ToString();
            }
        }

        /// <summary>
        /// This Callback function updates the information panel on the GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BLEdriver_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            //Get timestamp
            var timestamp = e.Timestamp.ToString("HH:mm:ss");

            //Write message to Logfile
            _logWriter.Write(e.Status);

            //Display Message on GUI
            if (l_Driver_Status.InvokeRequired)
                l_Driver_Status.Invoke((Action)delegate { this.l_Driver_Status.Text = timestamp + "      " + e.Status; });
            else
                this.l_Driver_Status.Text = timestamp + "      " + e.Status;
        }

        /// <summary>
        /// Callback function for Stream TCP checkbox. Enables and disables TCP streaming
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Callback function for Plot data checkbox. Enables and disable Plotting of incoming accelaration date of al connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cb_plotAcc_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cb_plotAcc.Checked)
            {
                //Plot data of all connected devices
                foreach (var device in _BLEdriver.ConnectedDeviceInformationList)
                {
                    //get sensor id
                    var id = device.SensorID;
                    //set current value to plot to zero
                    _currentChartValue = 0;
                    //Clear chart corresponding to sensorID
                    _ChartList[id].Series[0].Points.Clear();
                    _ChartList[id].Series[1].Points.Clear();
                    _ChartList[id].Series[2].Points.Clear();
                }
                //Plotting is done synchonously, so start the timer here
                PlotDataTimer.Enabled = true;
                this.PlotDataTimer.Start();
            }
            else
            {
                //stop timer if checkbox is unchecked
                PlotDataTimer.Enabled = false;
                this.PlotDataTimer.Stop();

            }
        }

        /// <summary>
        /// Callback function for Save data checkbox. Enables and disables data saving of all connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Callback function of Recalibrate Button. Allows to overwrite the currently locally save calibration data on each device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void b_recalibrate_Click(object sender, EventArgs e)
        {
            //get device from list
            var selectedDevice = (BLEDeviceInformation)this.lv_Device_List.SelectedItems[0].Tag;
            //write command to device
            _BLEdriver.Recalibrate_imu(selectedDevice);
        }

        private void mw_form_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Initialize Chart to Plot sensordata
        /// </summary>
        /// <param name="chart"></param>
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

        /// <summary>
        /// Callback function for Update Datachart Timer. Everytime this function is called a new datapoint is added to the chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void update_dataChart(object sender, System.EventArgs e)
        {
            //Plot data for each connected device
            foreach(var device in _BLEdriver.ConnectedDeviceInformationList)
            {
                lock (m_chartLock)
                {
                    //Get data and Corresponding sensorID
                    var data = device.Data;
                    var id = device.SensorID;
                    //Plot Accelerationdata - [0 until 3] Quaternion, [4 until 6] Acceleration, [7 until 9] Gyroscope
                    _ChartList[id].Series[0].Points.AddXY(_currentChartValue, data[4]);
                    _ChartList[id].Series[1].Points.AddXY(_currentChartValue, data[5]);
                    _ChartList[id].Series[2].Points.AddXY(_currentChartValue, data[6]);
                    //If maximum nunmber of values to plot is exceeded, clear all charts 
                    if (_currentChartValue >= _maxNumOfChartValues)
                    {
                        _ChartList[id].Series[0].Points.Clear();
                        _ChartList[id].Series[1].Points.Clear();
                        _ChartList[id].Series[2].Points.Clear();
                    }
                }
            }
            //...and start plotting from the beginning
            if (_currentChartValue >= _maxNumOfChartValues) _currentChartValue = 0;
            // Increment value
            _currentChartValue++;
        }

        /// <summary>
        /// Callback function for closing form. This function saves the logfile to a .txt file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mw_form_FormClosing(object sender, FormClosingEventArgs e)
        {
            _logWriter.Save();
        }

        /// <summary>
        /// This callback function adds an entry to the Logfile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnLogfileEntry(object sender, AddLogEntryEventArgs e)
        {
            _logWriter.Write(e.LogEntry);
        }
    }
}
