namespace o_bergen.LiveResultManager
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            grpStatus = new GroupBox();
            lblStatusBadge = new Label();
            lblStatus = new Label();
            grpControls = new GroupBox();
            btnManageStretches = new Button();
            btnClearLog = new Button();
            btnStop = new Button();
            btnStart = new Button();
            grpConfiguration = new GroupBox();
            numInterval = new NumericUpDown();
            lblInterval = new Label();
            lblSupabaseStatus = new Label();
            btnBrowseDb = new Button();
            txtAccessDbPath = new TextBox();
            lblAccessDb = new Label();
            grpStatistics = new GroupBox();
            lblLastWritten = new Label();
            lblLastRead = new Label();
            lblSuccessRate = new Label();
            lblRecordsWritten = new Label();
            lblRecordsRead = new Label();
            grpLog = new GroupBox();
            txtLog = new TextBox();
            timerStatusBlink = new System.Windows.Forms.Timer(components);
            grpStatus.SuspendLayout();
            grpControls.SuspendLayout();
            grpConfiguration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
            grpStatistics.SuspendLayout();
            grpLog.SuspendLayout();
            SuspendLayout();
            // 
            // grpStatus
            // 
            grpStatus.Controls.Add(lblStatusBadge);
            grpStatus.Controls.Add(lblStatus);
            grpStatus.Location = new Point(12, 12);
            grpStatus.Name = "grpStatus";
            grpStatus.Size = new Size(960, 70);
            grpStatus.TabIndex = 0;
            grpStatus.TabStop = false;
            grpStatus.Text = "Status";
            // 
            // lblStatusBadge
            // 
            lblStatusBadge.BackColor = Color.Gray;
            lblStatusBadge.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblStatusBadge.ForeColor = Color.White;
            lblStatusBadge.Location = new Point(6, 25);
            lblStatusBadge.Name = "lblStatusBadge";
            lblStatusBadge.Size = new Size(120, 35);
            lblStatusBadge.TabIndex = 1;
            lblStatusBadge.Text = "● IDLE";
            lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 10F);
            lblStatus.Location = new Point(140, 33);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(150, 19);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Ready to start transfer";
            // 
            // grpControls
            // 
            grpControls.Controls.Add(btnManageStretches);
            grpControls.Controls.Add(btnClearLog);
            grpControls.Controls.Add(btnStop);
            grpControls.Controls.Add(btnStart);
            grpControls.Location = new Point(12, 88);
            grpControls.Name = "grpControls";
            grpControls.Size = new Size(470, 100);
            grpControls.TabIndex = 1;
            grpControls.TabStop = false;
            grpControls.Text = "Controls";
            // 
            // btnManageStretches
            // 
            btnManageStretches.Location = new Point(6, 60);
            btnManageStretches.Name = "btnManageStretches";
            btnManageStretches.Size = new Size(458, 35);
            btnManageStretches.TabIndex = 3;
            btnManageStretches.Text = "Manage Invalid Stretches";
            btnManageStretches.UseVisualStyleBackColor = true;
            btnManageStretches.Click += btnManageStretches_Click;
            // 
            // btnClearLog
            // 
            btnClearLog.Location = new Point(324, 22);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(140, 35);
            btnClearLog.TabIndex = 2;
            btnClearLog.Text = "Clear Log";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(160, 22);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(140, 35);
            btnStop.TabIndex = 1;
            btnStop.Text = "Stop Transfer";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(6, 22);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(140, 35);
            btnStart.TabIndex = 0;
            btnStart.Text = "Start Transfer";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // grpConfiguration
            // 
            grpConfiguration.Controls.Add(numInterval);
            grpConfiguration.Controls.Add(lblInterval);
            grpConfiguration.Controls.Add(lblSupabaseStatus);
            grpConfiguration.Controls.Add(btnBrowseDb);
            grpConfiguration.Controls.Add(txtAccessDbPath);
            grpConfiguration.Controls.Add(lblAccessDb);
            grpConfiguration.Location = new Point(12, 194);
            grpConfiguration.Name = "grpConfiguration";
            grpConfiguration.Size = new Size(470, 130);
            grpConfiguration.TabIndex = 2;
            grpConfiguration.TabStop = false;
            grpConfiguration.Text = "Configuration";
            // 
            // numInterval
            // 
            numInterval.Location = new Point(110, 95);
            numInterval.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numInterval.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numInterval.Name = "numInterval";
            numInterval.Size = new Size(80, 23);
            numInterval.TabIndex = 5;
            numInterval.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblInterval
            // 
            lblInterval.AutoSize = true;
            lblInterval.Location = new Point(6, 97);
            lblInterval.Name = "lblInterval";
            lblInterval.Size = new Size(98, 15);
            lblInterval.TabIndex = 4;
            lblInterval.Text = "Interval (seconds):";
            // 
            // lblSupabaseStatus
            // 
            lblSupabaseStatus.AutoSize = true;
            lblSupabaseStatus.Location = new Point(6, 68);
            lblSupabaseStatus.Name = "lblSupabaseStatus";
            lblSupabaseStatus.Size = new Size(154, 15);
            lblSupabaseStatus.TabIndex = 3;
            lblSupabaseStatus.Text = "Supabase: ● Not Connected";
            // 
            // btnBrowseDb
            // 
            btnBrowseDb.Location = new Point(379, 35);
            btnBrowseDb.Name = "btnBrowseDb";
            btnBrowseDb.Size = new Size(75, 23);
            btnBrowseDb.TabIndex = 2;
            btnBrowseDb.Text = "Browse...";
            btnBrowseDb.UseVisualStyleBackColor = true;
            btnBrowseDb.Click += btnBrowseDb_Click;
            // 
            // txtAccessDbPath
            // 
            txtAccessDbPath.Location = new Point(110, 35);
            txtAccessDbPath.Name = "txtAccessDbPath";
            txtAccessDbPath.Size = new Size(263, 23);
            txtAccessDbPath.TabIndex = 1;
            // 
            // lblAccessDb
            // 
            lblAccessDb.AutoSize = true;
            lblAccessDb.Location = new Point(6, 38);
            lblAccessDb.Name = "lblAccessDb";
            lblAccessDb.Size = new Size(69, 15);
            lblAccessDb.TabIndex = 0;
            lblAccessDb.Text = "Access DB: ";
            // 
            // grpStatistics
            // 
            grpStatistics.Controls.Add(lblLastWritten);
            grpStatistics.Controls.Add(lblLastRead);
            grpStatistics.Controls.Add(lblSuccessRate);
            grpStatistics.Controls.Add(lblRecordsWritten);
            grpStatistics.Controls.Add(lblRecordsRead);
            grpStatistics.Location = new Point(488, 88);
            grpStatistics.Name = "grpStatistics";
            grpStatistics.Size = new Size(484, 236);
            grpStatistics.TabIndex = 3;
            grpStatistics.TabStop = false;
            grpStatistics.Text = "Statistics";
            // 
            // lblLastWritten
            // 
            lblLastWritten.AutoSize = true;
            lblLastWritten.Location = new Point(6, 123);
            lblLastWritten.Name = "lblLastWritten";
            lblLastWritten.Size = new Size(120, 15);
            lblLastWritten.TabIndex = 4;
            lblLastWritten.Text = "Last written: 0 records";
            // 
            // lblLastRead
            // 
            lblLastRead.AutoSize = true;
            lblLastRead.Location = new Point(6, 97);
            lblLastRead.Name = "lblLastRead";
            lblLastRead.Size = new Size(104, 15);
            lblLastRead.TabIndex = 3;
            lblLastRead.Text = "Last read: 0 records";
            // 
            // lblSuccessRate
            // 
            lblSuccessRate.AutoSize = true;
            lblSuccessRate.Location = new Point(6, 71);
            lblSuccessRate.Name = "lblSuccessRate";
            lblSuccessRate.Size = new Size(111, 15);
            lblSuccessRate.TabIndex = 2;
            lblSuccessRate.Text = "Success Rate: 100%";
            // 
            // lblRecordsWritten
            // 
            lblRecordsWritten.AutoSize = true;
            lblRecordsWritten.Location = new Point(6, 45);
            lblRecordsWritten.Name = "lblRecordsWritten";
            lblRecordsWritten.Size = new Size(106, 15);
            lblRecordsWritten.TabIndex = 1;
            lblRecordsWritten.Text = "Records Written: 0";
            // 
            // lblRecordsRead
            // 
            lblRecordsRead.AutoSize = true;
            lblRecordsRead.Location = new Point(6, 19);
            lblRecordsRead.Name = "lblRecordsRead";
            lblRecordsRead.Size = new Size(94, 15);
            lblRecordsRead.TabIndex = 0;
            lblRecordsRead.Text = "Records Read: 0";
            // 
            // grpLog
            // 
            grpLog.Controls.Add(txtLog);
            grpLog.Location = new Point(12, 300);
            grpLog.Name = "grpLog";
            grpLog.Size = new Size(960, 338);
            grpLog.TabIndex = 4;
            grpLog.TabStop = false;
            grpLog.Text = "Log";
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.White;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(6, 22);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(948, 310);
            txtLog.TabIndex = 0;
            // 
            // timerStatusBlink
            // 
            timerStatusBlink.Interval = 1000;
            timerStatusBlink.Tick += timerStatusBlink_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 650);
            Controls.Add(grpLog);
            Controls.Add(grpStatistics);
            Controls.Add(grpConfiguration);
            Controls.Add(grpControls);
            Controls.Add(grpStatus);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "O-Bergen Live Result Manager";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            grpStatus.ResumeLayout(false);
            grpStatus.PerformLayout();
            grpControls.ResumeLayout(false);
            grpConfiguration.ResumeLayout(false);
            grpConfiguration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
            grpStatistics.ResumeLayout(false);
            grpStatistics.PerformLayout();
            grpLog.ResumeLayout(false);
            grpLog.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpStatus;
        private Label lblStatus;
        private Label lblStatusBadge;
        private GroupBox grpControls;
        private Button btnStart;
        private Button btnStop;
        private Button btnClearLog;
        private Button btnManageStretches;
        private GroupBox grpConfiguration;
        private Label lblAccessDb;
        private TextBox txtAccessDbPath;
        private Button btnBrowseDb;
        private Label lblSupabaseStatus;
        private Label lblInterval;
        private NumericUpDown numInterval;
        private GroupBox grpStatistics;
        private Label lblRecordsRead;
        private Label lblRecordsWritten;
        private Label lblSuccessRate;
        private Label lblLastRead;
        private Label lblLastWritten;
        private GroupBox grpLog;
        private TextBox txtLog;
        private System.Windows.Forms.Timer timerStatusBlink;
    }
}
