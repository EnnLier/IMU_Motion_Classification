
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
            this.pb_connect_TCP = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.cb_StreamTCP = new System.Windows.Forms.CheckBox();
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
            // pb_connect_TCP
            // 
            this.pb_connect_TCP.Location = new System.Drawing.Point(21, 154);
            this.pb_connect_TCP.Name = "pb_connect_TCP";
            this.pb_connect_TCP.Size = new System.Drawing.Size(97, 28);
            this.pb_connect_TCP.TabIndex = 4;
            this.pb_connect_TCP.Text = "Connect TCP";
            this.pb_connect_TCP.UseVisualStyleBackColor = true;
            this.pb_connect_TCP.Click += new System.EventHandler(this.pb_connect_TCP_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(21, 106);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(79, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Save to file";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
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
            // mw_form
            // 
            this.ClientSize = new System.Drawing.Size(923, 551);
            this.Controls.Add(this.cb_StreamTCP);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.pb_connect_TCP);
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
        private System.Windows.Forms.Button pb_connect_TCP;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox cb_StreamTCP;
    }
}

