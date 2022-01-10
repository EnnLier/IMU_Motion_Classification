
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
            this.pb_Refresh_List = new System.Windows.Forms.Button();
            this.lv_Device_List = new System.Windows.Forms.ListView();
            this.name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.canConnect = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pb_connect = new System.Windows.Forms.Button();
            this.l_Driver_Status = new System.Windows.Forms.Label();
            this.cb_SaveToFile = new System.Windows.Forms.CheckBox();
            this.cb_StreamTCP = new System.Windows.Forms.CheckBox();
            this.labelAcc = new System.Windows.Forms.Label();
            this.labelSys = new System.Windows.Forms.Label();
            this.labelGyr = new System.Windows.Forms.Label();
            this.labelMag = new System.Windows.Forms.Label();
            this.l_mag = new System.Windows.Forms.Label();
            this.l_gyr = new System.Windows.Forms.Label();
            this.l_sys = new System.Windows.Forms.Label();
            this.l_acc = new System.Windows.Forms.Label();
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
            this.lv_Device_List.Size = new System.Drawing.Size(682, 420);
            this.lv_Device_List.TabIndex = 1;
            this.lv_Device_List.UseCompatibleStateImageBehavior = false;
            this.lv_Device_List.View = System.Windows.Forms.View.Details;
            // 
            // name
            // 
            this.name.Text = "Name";
            this.name.Width = 248;
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
            // pb_connect
            // 
            this.pb_connect.Location = new System.Drawing.Point(21, 70);
            this.pb_connect.Name = "pb_connect";
            this.pb_connect.Size = new System.Drawing.Size(97, 30);
            this.pb_connect.TabIndex = 2;
            this.pb_connect.Text = "Connect BT";
            this.pb_connect.UseVisualStyleBackColor = true;
            this.pb_connect.Click += new System.EventHandler(this.pb_connect_Click);
            // 
            // l_Driver_Status
            // 
            this.l_Driver_Status.AutoSize = true;
            this.l_Driver_Status.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F);
            this.l_Driver_Status.Location = new System.Drawing.Point(193, 489);
            this.l_Driver_Status.Name = "l_Driver_Status";
            this.l_Driver_Status.Size = new System.Drawing.Size(21, 29);
            this.l_Driver_Status.TabIndex = 3;
            this.l_Driver_Status.Text = "`";
            // 
            // cb_SaveToFile
            // 
            this.cb_SaveToFile.AutoSize = true;
            this.cb_SaveToFile.Location = new System.Drawing.Point(21, 106);
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
            this.cb_StreamTCP.Location = new System.Drawing.Point(21, 130);
            this.cb_StreamTCP.Name = "cb_StreamTCP";
            this.cb_StreamTCP.Size = new System.Drawing.Size(83, 17);
            this.cb_StreamTCP.TabIndex = 6;
            this.cb_StreamTCP.Text = "Stream TCP";
            this.cb_StreamTCP.UseVisualStyleBackColor = true;
            this.cb_StreamTCP.CheckedChanged += new System.EventHandler(this.cb_StreamTCP_CheckedChanged);
            // 
            // labelAcc
            // 
            this.labelAcc.AutoSize = true;
            this.labelAcc.Location = new System.Drawing.Point(892, 124);
            this.labelAcc.Name = "labelAcc";
            this.labelAcc.Size = new System.Drawing.Size(29, 13);
            this.labelAcc.TabIndex = 7;
            this.labelAcc.Text = "Acc:";
            // 
            // labelSys
            // 
            this.labelSys.AutoSize = true;
            this.labelSys.Location = new System.Drawing.Point(893, 58);
            this.labelSys.Name = "labelSys";
            this.labelSys.Size = new System.Drawing.Size(27, 13);
            this.labelSys.TabIndex = 8;
            this.labelSys.Text = "Sys:";
            // 
            // labelGyr
            // 
            this.labelGyr.AutoSize = true;
            this.labelGyr.Location = new System.Drawing.Point(894, 91);
            this.labelGyr.Name = "labelGyr";
            this.labelGyr.Size = new System.Drawing.Size(26, 13);
            this.labelGyr.TabIndex = 9;
            this.labelGyr.Text = "Gyr:";
            // 
            // labelMag
            // 
            this.labelMag.AutoSize = true;
            this.labelMag.Location = new System.Drawing.Point(891, 157);
            this.labelMag.Name = "labelMag";
            this.labelMag.Size = new System.Drawing.Size(31, 13);
            this.labelMag.TabIndex = 10;
            this.labelMag.Text = "Mag:";
            // 
            // l_mag
            // 
            this.l_mag.AutoSize = true;
            this.l_mag.Location = new System.Drawing.Point(939, 157);
            this.l_mag.Name = "l_mag";
            this.l_mag.Size = new System.Drawing.Size(13, 13);
            this.l_mag.TabIndex = 14;
            this.l_mag.Text = "0";
            // 
            // l_gyr
            // 
            this.l_gyr.AutoSize = true;
            this.l_gyr.Location = new System.Drawing.Point(939, 91);
            this.l_gyr.Name = "l_gyr";
            this.l_gyr.Size = new System.Drawing.Size(13, 13);
            this.l_gyr.TabIndex = 13;
            this.l_gyr.Text = "0";
            // 
            // l_sys
            // 
            this.l_sys.AutoSize = true;
            this.l_sys.Location = new System.Drawing.Point(939, 58);
            this.l_sys.Name = "l_sys";
            this.l_sys.Size = new System.Drawing.Size(13, 13);
            this.l_sys.TabIndex = 12;
            this.l_sys.Text = "0";
            // 
            // l_acc
            // 
            this.l_acc.AutoSize = true;
            this.l_acc.Location = new System.Drawing.Point(939, 124);
            this.l_acc.Name = "l_acc";
            this.l_acc.Size = new System.Drawing.Size(13, 13);
            this.l_acc.TabIndex = 11;
            this.l_acc.Text = "0";
            // 
            // mw_form
            // 
            this.ClientSize = new System.Drawing.Size(1147, 551);
            this.Controls.Add(this.l_mag);
            this.Controls.Add(this.l_gyr);
            this.Controls.Add(this.l_sys);
            this.Controls.Add(this.l_acc);
            this.Controls.Add(this.labelMag);
            this.Controls.Add(this.labelGyr);
            this.Controls.Add(this.labelSys);
            this.Controls.Add(this.labelAcc);
            this.Controls.Add(this.cb_StreamTCP);
            this.Controls.Add(this.cb_SaveToFile);
            this.Controls.Add(this.l_Driver_Status);
            this.Controls.Add(this.pb_connect);
            this.Controls.Add(this.lv_Device_List);
            this.Controls.Add(this.pb_Refresh_List);
            this.Name = "mw_form";
            this.Text = "Mainwindow";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button pb_Refresh_List;
        private System.Windows.Forms.ListView lv_Device_List;
        private System.Windows.Forms.ColumnHeader name;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.ColumnHeader canConnect;
        private System.Windows.Forms.Button pb_connect;
        private System.Windows.Forms.Label l_Driver_Status;
        private System.Windows.Forms.CheckBox cb_SaveToFile;
        private System.Windows.Forms.CheckBox cb_StreamTCP;
        private System.Windows.Forms.Label labelAcc;
        private System.Windows.Forms.Label labelSys;
        private System.Windows.Forms.Label labelGyr;
        private System.Windows.Forms.Label labelMag;
        private System.Windows.Forms.Label l_mag;
        private System.Windows.Forms.Label l_gyr;
        private System.Windows.Forms.Label l_sys;
        private System.Windows.Forms.Label l_acc;
    }
}

