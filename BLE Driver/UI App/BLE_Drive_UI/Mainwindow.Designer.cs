
namespace BLE_Drive_UI
{
    partial class mw_form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.pb_Refresh_List = new System.Windows.Forms.Button();
            this.lv_Device_List = new System.Windows.Forms.ListView();
            this.name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.canConnect = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pb_frontConnect = new System.Windows.Forms.Button();
            this.l_Driver_Status = new System.Windows.Forms.Label();
            this.cb_SaveToFile = new System.Windows.Forms.CheckBox();
            this.cb_StreamTCP = new System.Windows.Forms.CheckBox();
            this.b_recalibrate = new System.Windows.Forms.Button();
            this.cb_plotAcc = new System.Windows.Forms.CheckBox();
            this.pb_backConnect = new System.Windows.Forms.Button();
            this.p_backCalibPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.labelGyrBack = new System.Windows.Forms.Label();
            this.labelAccBack = new System.Windows.Forms.Label();
            this.labelSysBack = new System.Windows.Forms.Label();
            this.labelMagBack = new System.Windows.Forms.Label();
            this.l_accBack = new System.Windows.Forms.Label();
            this.l_sysBack = new System.Windows.Forms.Label();
            this.l_gyrBack = new System.Windows.Forms.Label();
            this.l_magBack = new System.Windows.Forms.Label();
            this.p_frontCalibPanel = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.labelGyrFront = new System.Windows.Forms.Label();
            this.labelAccFront = new System.Windows.Forms.Label();
            this.labelSysFront = new System.Windows.Forms.Label();
            this.labelMagFront = new System.Windows.Forms.Label();
            this.l_accFront = new System.Windows.Forms.Label();
            this.l_sysFront = new System.Windows.Forms.Label();
            this.l_gyrFront = new System.Windows.Forms.Label();
            this.l_magFront = new System.Windows.Forms.Label();
            this.l_batteryLabelBack = new System.Windows.Forms.Label();
            this.p_backBatteryPanel = new System.Windows.Forms.Panel();
            this.l_batteryLabelFront = new System.Windows.Forms.Label();
            this.p_frontBatteryPanel = new System.Windows.Forms.Panel();
            this.ch_backDataPlot = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.ch_frontDataPlot = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.p_backCalibPanel.SuspendLayout();
            this.p_frontCalibPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ch_backDataPlot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ch_frontDataPlot)).BeginInit();
            this.SuspendLayout();
            // 
            // pb_Refresh_List
            // 
            this.pb_Refresh_List.Location = new System.Drawing.Point(21, 12);
            this.pb_Refresh_List.Name = "pb_Refresh_List";
            this.pb_Refresh_List.Size = new System.Drawing.Size(97, 29);
            this.pb_Refresh_List.TabIndex = 0;
            this.pb_Refresh_List.Text = "Refresh";
            this.pb_Refresh_List.UseVisualStyleBackColor = true;
            this.pb_Refresh_List.Click += new System.EventHandler(this.pb_Refresh_List_Click);
            // 
            // lv_Device_List
            // 
            this.lv_Device_List.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.name,
            this.ID,
            this.canConnect});
            this.lv_Device_List.FullRowSelect = true;
            this.lv_Device_List.GridLines = true;
            this.lv_Device_List.HideSelection = false;
            this.lv_Device_List.Location = new System.Drawing.Point(133, 30);
            this.lv_Device_List.Name = "lv_Device_List";
            this.lv_Device_List.Size = new System.Drawing.Size(318, 449);
            this.lv_Device_List.TabIndex = 1;
            this.lv_Device_List.UseCompatibleStateImageBehavior = false;
            this.lv_Device_List.View = System.Windows.Forms.View.Details;
            // 
            // name
            // 
            this.name.Text = "Name";
            this.name.Width = 160;
            // 
            // ID
            // 
            this.ID.Text = "ID";
            this.ID.Width = 336;
            // 
            // canConnect
            // 
            this.canConnect.Text = "Can Connect";
            this.canConnect.Width = 81;
            // 
            // pb_frontConnect
            // 
            this.pb_frontConnect.Location = new System.Drawing.Point(21, 70);
            this.pb_frontConnect.Name = "pb_frontConnect";
            this.pb_frontConnect.Size = new System.Drawing.Size(97, 30);
            this.pb_frontConnect.TabIndex = 2;
            this.pb_frontConnect.Text = "Connect Front";
            this.pb_frontConnect.UseVisualStyleBackColor = true;
            this.pb_frontConnect.Click += new System.EventHandler(this.pb_connect_Click);
            // 
            // l_Driver_Status
            // 
            this.l_Driver_Status.AutoSize = true;
            this.l_Driver_Status.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F);
            this.l_Driver_Status.Location = new System.Drawing.Point(465, 485);
            this.l_Driver_Status.Name = "l_Driver_Status";
            this.l_Driver_Status.Size = new System.Drawing.Size(25, 36);
            this.l_Driver_Status.TabIndex = 3;
            this.l_Driver_Status.Text = "`";
            // 
            // cb_SaveToFile
            // 
            this.cb_SaveToFile.AutoSize = true;
            this.cb_SaveToFile.Location = new System.Drawing.Point(21, 148);
            this.cb_SaveToFile.Name = "cb_SaveToFile";
            this.cb_SaveToFile.Size = new System.Drawing.Size(79, 17);
            this.cb_SaveToFile.TabIndex = 5;
            this.cb_SaveToFile.Text = "Save to file";
            this.cb_SaveToFile.UseVisualStyleBackColor = true;
            this.cb_SaveToFile.CheckedChanged += new System.EventHandler(this.cb_SaveToFile_CheckedChanged);
            // 
            // cb_StreamTCP
            // 
            this.cb_StreamTCP.AutoSize = true;
            this.cb_StreamTCP.Location = new System.Drawing.Point(21, 172);
            this.cb_StreamTCP.Name = "cb_StreamTCP";
            this.cb_StreamTCP.Size = new System.Drawing.Size(83, 17);
            this.cb_StreamTCP.TabIndex = 6;
            this.cb_StreamTCP.Text = "Stream TCP";
            this.cb_StreamTCP.UseVisualStyleBackColor = true;
            this.cb_StreamTCP.CheckedChanged += new System.EventHandler(this.cb_StreamTCP_CheckedChanged);
            // 
            // b_recalibrate
            // 
            this.b_recalibrate.Location = new System.Drawing.Point(1305, 369);
            this.b_recalibrate.Name = "b_recalibrate";
            this.b_recalibrate.Size = new System.Drawing.Size(76, 28);
            this.b_recalibrate.TabIndex = 15;
            this.b_recalibrate.Text = "Recalibrate";
            this.b_recalibrate.UseVisualStyleBackColor = true;
            this.b_recalibrate.Click += new System.EventHandler(this.b_recalibrate_Click);
            // 
            // cb_plotAcc
            // 
            this.cb_plotAcc.AutoSize = true;
            this.cb_plotAcc.Location = new System.Drawing.Point(21, 195);
            this.cb_plotAcc.Name = "cb_plotAcc";
            this.cb_plotAcc.Size = new System.Drawing.Size(70, 17);
            this.cb_plotAcc.TabIndex = 17;
            this.cb_plotAcc.Text = "Plot Data";
            this.cb_plotAcc.UseVisualStyleBackColor = true;
            this.cb_plotAcc.CheckedChanged += new System.EventHandler(this.cb_plotAcc_CheckedChanged);
            // 
            // pb_backConnect
            // 
            this.pb_backConnect.Location = new System.Drawing.Point(21, 106);
            this.pb_backConnect.Name = "pb_backConnect";
            this.pb_backConnect.Size = new System.Drawing.Size(97, 29);
            this.pb_backConnect.TabIndex = 21;
            this.pb_backConnect.Text = "Connect Back";
            this.pb_backConnect.UseVisualStyleBackColor = true;
            this.pb_backConnect.Click += new System.EventHandler(this.pb_connect_Click);
            // 
            // p_backCalibPanel
            // 
            this.p_backCalibPanel.Controls.Add(this.label2);
            this.p_backCalibPanel.Controls.Add(this.labelGyrBack);
            this.p_backCalibPanel.Controls.Add(this.labelAccBack);
            this.p_backCalibPanel.Controls.Add(this.labelSysBack);
            this.p_backCalibPanel.Controls.Add(this.labelMagBack);
            this.p_backCalibPanel.Controls.Add(this.l_accBack);
            this.p_backCalibPanel.Controls.Add(this.l_sysBack);
            this.p_backCalibPanel.Controls.Add(this.l_gyrBack);
            this.p_backCalibPanel.Controls.Add(this.l_magBack);
            this.p_backCalibPanel.Location = new System.Drawing.Point(1305, 204);
            this.p_backCalibPanel.Name = "p_backCalibPanel";
            this.p_backCalibPanel.Size = new System.Drawing.Size(90, 159);
            this.p_backCalibPanel.TabIndex = 26;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.label2.Location = new System.Drawing.Point(24, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 18);
            this.label2.TabIndex = 15;
            this.label2.Text = "Back";
            // 
            // labelGyrBack
            // 
            this.labelGyrBack.AutoSize = true;
            this.labelGyrBack.Location = new System.Drawing.Point(16, 73);
            this.labelGyrBack.Name = "labelGyrBack";
            this.labelGyrBack.Size = new System.Drawing.Size(26, 13);
            this.labelGyrBack.TabIndex = 9;
            this.labelGyrBack.Text = "Gyr:";
            // 
            // labelAccBack
            // 
            this.labelAccBack.AutoSize = true;
            this.labelAccBack.Location = new System.Drawing.Point(14, 106);
            this.labelAccBack.Name = "labelAccBack";
            this.labelAccBack.Size = new System.Drawing.Size(29, 13);
            this.labelAccBack.TabIndex = 7;
            this.labelAccBack.Text = "Acc:";
            // 
            // labelSysBack
            // 
            this.labelSysBack.AutoSize = true;
            this.labelSysBack.Location = new System.Drawing.Point(15, 40);
            this.labelSysBack.Name = "labelSysBack";
            this.labelSysBack.Size = new System.Drawing.Size(27, 13);
            this.labelSysBack.TabIndex = 8;
            this.labelSysBack.Text = "Sys:";
            // 
            // labelMagBack
            // 
            this.labelMagBack.AutoSize = true;
            this.labelMagBack.Location = new System.Drawing.Point(13, 139);
            this.labelMagBack.Name = "labelMagBack";
            this.labelMagBack.Size = new System.Drawing.Size(31, 13);
            this.labelMagBack.TabIndex = 10;
            this.labelMagBack.Text = "Mag:";
            // 
            // l_accBack
            // 
            this.l_accBack.AutoSize = true;
            this.l_accBack.Location = new System.Drawing.Point(61, 106);
            this.l_accBack.Name = "l_accBack";
            this.l_accBack.Size = new System.Drawing.Size(13, 13);
            this.l_accBack.TabIndex = 11;
            this.l_accBack.Text = "0";
            // 
            // l_sysBack
            // 
            this.l_sysBack.AutoSize = true;
            this.l_sysBack.Location = new System.Drawing.Point(61, 40);
            this.l_sysBack.Name = "l_sysBack";
            this.l_sysBack.Size = new System.Drawing.Size(13, 13);
            this.l_sysBack.TabIndex = 12;
            this.l_sysBack.Text = "0";
            // 
            // l_gyrBack
            // 
            this.l_gyrBack.AutoSize = true;
            this.l_gyrBack.Location = new System.Drawing.Point(61, 73);
            this.l_gyrBack.Name = "l_gyrBack";
            this.l_gyrBack.Size = new System.Drawing.Size(13, 13);
            this.l_gyrBack.TabIndex = 13;
            this.l_gyrBack.Text = "0";
            // 
            // l_magBack
            // 
            this.l_magBack.AutoSize = true;
            this.l_magBack.Location = new System.Drawing.Point(61, 139);
            this.l_magBack.Name = "l_magBack";
            this.l_magBack.Size = new System.Drawing.Size(13, 13);
            this.l_magBack.TabIndex = 14;
            this.l_magBack.Text = "0";
            // 
            // p_frontCalibPanel
            // 
            this.p_frontCalibPanel.Controls.Add(this.label1);
            this.p_frontCalibPanel.Controls.Add(this.labelGyrFront);
            this.p_frontCalibPanel.Controls.Add(this.labelAccFront);
            this.p_frontCalibPanel.Controls.Add(this.labelSysFront);
            this.p_frontCalibPanel.Controls.Add(this.labelMagFront);
            this.p_frontCalibPanel.Controls.Add(this.l_accFront);
            this.p_frontCalibPanel.Controls.Add(this.l_sysFront);
            this.p_frontCalibPanel.Controls.Add(this.l_gyrFront);
            this.p_frontCalibPanel.Controls.Add(this.l_magFront);
            this.p_frontCalibPanel.Location = new System.Drawing.Point(1305, 39);
            this.p_frontCalibPanel.Name = "p_frontCalibPanel";
            this.p_frontCalibPanel.Size = new System.Drawing.Size(90, 159);
            this.p_frontCalibPanel.TabIndex = 25;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.label1.Location = new System.Drawing.Point(24, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 18);
            this.label1.TabIndex = 15;
            this.label1.Text = "Front";
            // 
            // labelGyrFront
            // 
            this.labelGyrFront.AutoSize = true;
            this.labelGyrFront.Location = new System.Drawing.Point(16, 73);
            this.labelGyrFront.Name = "labelGyrFront";
            this.labelGyrFront.Size = new System.Drawing.Size(26, 13);
            this.labelGyrFront.TabIndex = 9;
            this.labelGyrFront.Text = "Gyr:";
            // 
            // labelAccFront
            // 
            this.labelAccFront.AutoSize = true;
            this.labelAccFront.Location = new System.Drawing.Point(14, 106);
            this.labelAccFront.Name = "labelAccFront";
            this.labelAccFront.Size = new System.Drawing.Size(29, 13);
            this.labelAccFront.TabIndex = 7;
            this.labelAccFront.Text = "Acc:";
            // 
            // labelSysFront
            // 
            this.labelSysFront.AutoSize = true;
            this.labelSysFront.Location = new System.Drawing.Point(15, 40);
            this.labelSysFront.Name = "labelSysFront";
            this.labelSysFront.Size = new System.Drawing.Size(27, 13);
            this.labelSysFront.TabIndex = 8;
            this.labelSysFront.Text = "Sys:";
            // 
            // labelMagFront
            // 
            this.labelMagFront.AutoSize = true;
            this.labelMagFront.Location = new System.Drawing.Point(13, 139);
            this.labelMagFront.Name = "labelMagFront";
            this.labelMagFront.Size = new System.Drawing.Size(31, 13);
            this.labelMagFront.TabIndex = 10;
            this.labelMagFront.Text = "Mag:";
            // 
            // l_accFront
            // 
            this.l_accFront.AutoSize = true;
            this.l_accFront.Location = new System.Drawing.Point(61, 106);
            this.l_accFront.Name = "l_accFront";
            this.l_accFront.Size = new System.Drawing.Size(13, 13);
            this.l_accFront.TabIndex = 11;
            this.l_accFront.Text = "0";
            // 
            // l_sysFront
            // 
            this.l_sysFront.AutoSize = true;
            this.l_sysFront.Location = new System.Drawing.Point(61, 40);
            this.l_sysFront.Name = "l_sysFront";
            this.l_sysFront.Size = new System.Drawing.Size(13, 13);
            this.l_sysFront.TabIndex = 12;
            this.l_sysFront.Text = "0";
            // 
            // l_gyrFront
            // 
            this.l_gyrFront.AutoSize = true;
            this.l_gyrFront.Location = new System.Drawing.Point(61, 73);
            this.l_gyrFront.Name = "l_gyrFront";
            this.l_gyrFront.Size = new System.Drawing.Size(13, 13);
            this.l_gyrFront.TabIndex = 13;
            this.l_gyrFront.Text = "0";
            // 
            // l_magFront
            // 
            this.l_magFront.AutoSize = true;
            this.l_magFront.Location = new System.Drawing.Point(61, 139);
            this.l_magFront.Name = "l_magFront";
            this.l_magFront.Size = new System.Drawing.Size(13, 13);
            this.l_magFront.TabIndex = 14;
            this.l_magFront.Text = "0";
            // 
            // l_batteryLabelBack
            // 
            this.l_batteryLabelBack.AutoSize = true;
            this.l_batteryLabelBack.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.l_batteryLabelBack.Location = new System.Drawing.Point(196, 532);
            this.l_batteryLabelBack.Name = "l_batteryLabelBack";
            this.l_batteryLabelBack.Size = new System.Drawing.Size(56, 19);
            this.l_batteryLabelBack.TabIndex = 26;
            this.l_batteryLabelBack.Text = "Battery";
            // 
            // p_backBatteryPanel
            // 
            this.p_backBatteryPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.p_backBatteryPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.p_backBatteryPanel.Location = new System.Drawing.Point(159, 485);
            this.p_backBatteryPanel.Name = "p_backBatteryPanel";
            this.p_backBatteryPanel.Size = new System.Drawing.Size(131, 44);
            this.p_backBatteryPanel.TabIndex = 25;
            // 
            // l_batteryLabelFront
            // 
            this.l_batteryLabelFront.AutoSize = true;
            this.l_batteryLabelFront.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.l_batteryLabelFront.Location = new System.Drawing.Point(45, 532);
            this.l_batteryLabelFront.Name = "l_batteryLabelFront";
            this.l_batteryLabelFront.Size = new System.Drawing.Size(56, 19);
            this.l_batteryLabelFront.TabIndex = 24;
            this.l_batteryLabelFront.Text = "Battery";
            // 
            // p_frontBatteryPanel
            // 
            this.p_frontBatteryPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.p_frontBatteryPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.p_frontBatteryPanel.Location = new System.Drawing.Point(12, 485);
            this.p_frontBatteryPanel.Name = "p_frontBatteryPanel";
            this.p_frontBatteryPanel.Size = new System.Drawing.Size(131, 44);
            this.p_frontBatteryPanel.TabIndex = 23;
            // 
            // ch_backDataPlot
            // 
            chartArea1.Name = "ChartArea1";
            this.ch_backDataPlot.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.ch_backDataPlot.Legends.Add(legend1);
            this.ch_backDataPlot.Location = new System.Drawing.Point(878, 29);
            this.ch_backDataPlot.Name = "ch_backDataPlot";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.ch_backDataPlot.Series.Add(series1);
            this.ch_backDataPlot.Size = new System.Drawing.Size(377, 450);
            this.ch_backDataPlot.TabIndex = 28;
            this.ch_backDataPlot.Text = "ch_back";
            // 
            // ch_frontDataPlot
            // 
            chartArea2.Name = "ChartArea1";
            this.ch_frontDataPlot.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.ch_frontDataPlot.Legends.Add(legend2);
            this.ch_frontDataPlot.Location = new System.Drawing.Point(477, 29);
            this.ch_frontDataPlot.Name = "ch_frontDataPlot";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.ch_frontDataPlot.Series.Add(series2);
            this.ch_frontDataPlot.Size = new System.Drawing.Size(377, 450);
            this.ch_frontDataPlot.TabIndex = 27;
            this.ch_frontDataPlot.Text = "ch_front";
            // 
            // mw_form
            // 
            this.ClientSize = new System.Drawing.Size(1416, 551);
            this.Controls.Add(this.ch_backDataPlot);
            this.Controls.Add(this.ch_frontDataPlot);
            this.Controls.Add(this.l_batteryLabelBack);
            this.Controls.Add(this.p_backCalibPanel);
            this.Controls.Add(this.p_backBatteryPanel);
            this.Controls.Add(this.p_frontCalibPanel);
            this.Controls.Add(this.l_batteryLabelFront);
            this.Controls.Add(this.pb_backConnect);
            this.Controls.Add(this.p_frontBatteryPanel);
            this.Controls.Add(this.cb_plotAcc);
            this.Controls.Add(this.b_recalibrate);
            this.Controls.Add(this.cb_StreamTCP);
            this.Controls.Add(this.cb_SaveToFile);
            this.Controls.Add(this.l_Driver_Status);
            this.Controls.Add(this.pb_frontConnect);
            this.Controls.Add(this.lv_Device_List);
            this.Controls.Add(this.pb_Refresh_List);
            this.Name = "mw_form";
            this.Text = "Mainwindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mw_form_FormClosing);
            this.Load += new System.EventHandler(this.mw_form_Load);
            this.p_backCalibPanel.ResumeLayout(false);
            this.p_backCalibPanel.PerformLayout();
            this.p_frontCalibPanel.ResumeLayout(false);
            this.p_frontCalibPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ch_backDataPlot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ch_frontDataPlot)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button pb_Refresh_List;
        private System.Windows.Forms.ListView lv_Device_List;
        private System.Windows.Forms.ColumnHeader name;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.ColumnHeader canConnect;
        private System.Windows.Forms.Button pb_frontConnect;
        private System.Windows.Forms.Label l_Driver_Status;
        private System.Windows.Forms.CheckBox cb_SaveToFile;
        private System.Windows.Forms.CheckBox cb_StreamTCP;
        private System.Windows.Forms.Button b_recalibrate;
        private System.Windows.Forms.CheckBox cb_plotAcc;
        private System.Windows.Forms.Button pb_backConnect;
        private System.Windows.Forms.Panel p_backCalibPanel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelGyrBack;
        private System.Windows.Forms.Label labelAccBack;
        private System.Windows.Forms.Label labelSysBack;
        private System.Windows.Forms.Label labelMagBack;
        private System.Windows.Forms.Label l_accBack;
        private System.Windows.Forms.Label l_sysBack;
        private System.Windows.Forms.Label l_gyrBack;
        private System.Windows.Forms.Label l_magBack;
        private System.Windows.Forms.Panel p_frontCalibPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelGyrFront;
        private System.Windows.Forms.Label labelAccFront;
        private System.Windows.Forms.Label labelSysFront;
        private System.Windows.Forms.Label labelMagFront;
        private System.Windows.Forms.Label l_accFront;
        private System.Windows.Forms.Label l_sysFront;
        private System.Windows.Forms.Label l_gyrFront;
        private System.Windows.Forms.Label l_magFront;
        private System.Windows.Forms.Label l_batteryLabelBack;
        private System.Windows.Forms.Panel p_backBatteryPanel;
        private System.Windows.Forms.Label l_batteryLabelFront;
        private System.Windows.Forms.Panel p_frontBatteryPanel;
        private System.Windows.Forms.DataVisualization.Charting.Chart ch_backDataPlot;
        private System.Windows.Forms.DataVisualization.Charting.Chart ch_frontDataPlot;
    }
}

