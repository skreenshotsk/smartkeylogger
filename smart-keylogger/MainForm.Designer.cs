// MainForm.Designer.cs
namespace MacroRecorderGUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelRecording = new System.Windows.Forms.Panel();
            this.chkTrackLanguage = new System.Windows.Forms.CheckBox();
            this.btnPauseRecord = new System.Windows.Forms.Button();
            this.lblRecordingTitle = new System.Windows.Forms.Label();
            this.btnStartRecord = new System.Windows.Forms.Button();
            this.btnStopRecord = new System.Windows.Forms.Button();
            this.panelPlayback = new System.Windows.Forms.Panel();
            this.btnPausePlayback = new System.Windows.Forms.Button();
            this.lblPlaybackTitle = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPlayLoop = new System.Windows.Forms.Button();
            this.btnStopPlayback = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelHotkeys = new System.Windows.Forms.Panel();
            this.lblHotkeys = new System.Windows.Forms.Label();
            this.lblHotkeysTitle = new System.Windows.Forms.Label();
            this.panelLogs = new System.Windows.Forms.Panel();
            this.txtLogs = new System.Windows.Forms.RichTextBox();
            this.lblLogsTitle = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.panelRecording.SuspendLayout();
            this.panelPlayback.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.panelHotkeys.SuspendLayout();
            this.panelLogs.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            
            // panelHeader
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(700, 80);
            this.panelHeader.TabIndex = 0;
            
            // lblTitle
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(700, 80);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🕹 Macro Recorder";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // panelRecording
            this.panelRecording.BackColor = System.Drawing.Color.White;
            this.panelRecording.Controls.Add(this.chkTrackLanguage);
            this.panelRecording.Controls.Add(this.btnPauseRecord);
            this.panelRecording.Controls.Add(this.lblRecordingTitle);
            this.panelRecording.Controls.Add(this.btnStartRecord);
            this.panelRecording.Controls.Add(this.btnStopRecord);
            this.panelRecording.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelRecording.Location = new System.Drawing.Point(0, 80);
            this.panelRecording.MinimumSize = new System.Drawing.Size(0, 230);
            this.panelRecording.Name = "panelRecording";
            this.panelRecording.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.panelRecording.Size = new System.Drawing.Size(700, 230);
            this.panelRecording.TabIndex = 1;
            
            // chkTrackLanguage
            this.chkTrackLanguage.AutoSize = true;
            this.chkTrackLanguage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkTrackLanguage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.chkTrackLanguage.Location = new System.Drawing.Point(30, 180);
            this.chkTrackLanguage.Name = "chkTrackLanguage";
            this.chkTrackLanguage.Size = new System.Drawing.Size(350, 23);
            this.chkTrackLanguage.TabIndex = 4;
            this.chkTrackLanguage.Text = "✓ Отслеживать язык клавиатуры при записи";
            this.chkTrackLanguage.UseVisualStyleBackColor = true;
            
            // btnPauseRecord
            this.btnPauseRecord.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.btnPauseRecord.FlatAppearance.BorderSize = 0;
            this.btnPauseRecord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPauseRecord.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPauseRecord.ForeColor = System.Drawing.Color.White;
            this.btnPauseRecord.Location = new System.Drawing.Point(30, 120);
            this.btnPauseRecord.Name = "btnPauseRecord";
            this.btnPauseRecord.Size = new System.Drawing.Size(640, 50);
            this.btnPauseRecord.TabIndex = 3;
            this.btnPauseRecord.Text = "⏸ ПАУЗА ЗАПИСИ (PAUSE)";
            this.btnPauseRecord.UseVisualStyleBackColor = false;
            this.btnPauseRecord.Click += new System.EventHandler(this.btnPauseRecord_Click);
            
            // lblRecordingTitle
            this.lblRecordingTitle.AutoSize = true;
            this.lblRecordingTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblRecordingTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.lblRecordingTitle.Location = new System.Drawing.Point(27, 15);
            this.lblRecordingTitle.Name = "lblRecordingTitle";
            this.lblRecordingTitle.Size = new System.Drawing.Size(82, 25);
            this.lblRecordingTitle.TabIndex = 2;
            this.lblRecordingTitle.Text = "Запись:";
            
            // btnStartRecord
            this.btnStartRecord.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnStartRecord.FlatAppearance.BorderSize = 0;
            this.btnStartRecord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartRecord.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnStartRecord.ForeColor = System.Drawing.Color.White;
            this.btnStartRecord.Location = new System.Drawing.Point(30, 50);
            this.btnStartRecord.Name = "btnStartRecord";
            this.btnStartRecord.Size = new System.Drawing.Size(300, 50);
            this.btnStartRecord.TabIndex = 0;
            this.btnStartRecord.Text = "🖱️ НАЧАТЬ ЗАПИСЬ (F9)";
            this.btnStartRecord.UseVisualStyleBackColor = false;
            this.btnStartRecord.Click += new System.EventHandler(this.btnStartRecord_Click);
            
            // btnStopRecord
            this.btnStopRecord.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnStopRecord.FlatAppearance.BorderSize = 0;
            this.btnStopRecord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopRecord.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnStopRecord.ForeColor = System.Drawing.Color.White;
            this.btnStopRecord.Location = new System.Drawing.Point(350, 50);
            this.btnStopRecord.Name = "btnStopRecord";
            this.btnStopRecord.Size = new System.Drawing.Size(320, 50);
            this.btnStopRecord.TabIndex = 1;
            this.btnStopRecord.Text = "⏹ ОСТАНОВИТЬ ЗАПИСЬ (F10)";
            this.btnStopRecord.UseVisualStyleBackColor = false;
            this.btnStopRecord.Click += new System.EventHandler(this.btnStopRecord_Click);
            
            // panelPlayback
            this.panelPlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.panelPlayback.Controls.Add(this.btnPausePlayback);
            this.panelPlayback.Controls.Add(this.lblPlaybackTitle);
            this.panelPlayback.Controls.Add(this.btnPlay);
            this.panelPlayback.Controls.Add(this.btnPlayLoop);
            this.panelPlayback.Controls.Add(this.btnStopPlayback);
            this.panelPlayback.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPlayback.Location = new System.Drawing.Point(0, 310);
            this.panelPlayback.MinimumSize = new System.Drawing.Size(0, 220);
            this.panelPlayback.Name = "panelPlayback";
            this.panelPlayback.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.panelPlayback.Size = new System.Drawing.Size(700, 220);
            this.panelPlayback.TabIndex = 2;
            
            // btnPausePlayback
            this.btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.btnPausePlayback.FlatAppearance.BorderSize = 0;
            this.btnPausePlayback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPausePlayback.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPausePlayback.ForeColor = System.Drawing.Color.White;
            this.btnPausePlayback.Location = new System.Drawing.Point(30, 120);
            this.btnPausePlayback.Name = "btnPausePlayback";
            this.btnPausePlayback.Size = new System.Drawing.Size(640, 50);
            this.btnPausePlayback.TabIndex = 4;
            this.btnPausePlayback.Text = "⏸ ПАУЗА ВОСПРОИЗВЕДЕНИЯ (HOME)"; // ИЗМЕНЕНО: SCROLL -> HOME
            this.btnPausePlayback.UseVisualStyleBackColor = false;
            this.btnPausePlayback.Click += new System.EventHandler(this.btnPausePlayback_Click);
            
            // lblPlaybackTitle
            this.lblPlaybackTitle.AutoSize = true;
            this.lblPlaybackTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblPlaybackTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.lblPlaybackTitle.Location = new System.Drawing.Point(27, 15);
            this.lblPlaybackTitle.Name = "lblPlaybackTitle";
            this.lblPlaybackTitle.Size = new System.Drawing.Size(162, 25);
            this.lblPlaybackTitle.TabIndex = 3;
            this.lblPlaybackTitle.Text = "Воспроизведение:";
            
            // btnPlay
            this.btnPlay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPlay.ForeColor = System.Drawing.Color.White;
            this.btnPlay.Location = new System.Drawing.Point(30, 50);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(200, 50);
            this.btnPlay.TabIndex = 0;
            this.btnPlay.Text = "▶ ВОСПРОИЗВЕСТИ (F11)";
            this.btnPlay.UseVisualStyleBackColor = false;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            
            // btnPlayLoop
            this.btnPlayLoop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnPlayLoop.FlatAppearance.BorderSize = 0;
            this.btnPlayLoop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlayLoop.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPlayLoop.ForeColor = System.Drawing.Color.White;
            this.btnPlayLoop.Location = new System.Drawing.Point(250, 50);
            this.btnPlayLoop.Name = "btnPlayLoop";
            this.btnPlayLoop.Size = new System.Drawing.Size(200, 50);
            this.btnPlayLoop.TabIndex = 1;
            this.btnPlayLoop.Text = "🔄 ЦИКЛ (F12)";
            this.btnPlayLoop.UseVisualStyleBackColor = false;
            this.btnPlayLoop.Click += new System.EventHandler(this.btnPlayLoop_Click);
            
            // btnStopPlayback
            this.btnStopPlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnStopPlayback.FlatAppearance.BorderSize = 0;
            this.btnStopPlayback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopPlayback.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnStopPlayback.ForeColor = System.Drawing.Color.White;
            this.btnStopPlayback.Location = new System.Drawing.Point(470, 50);
            this.btnStopPlayback.Name = "btnStopPlayback";
            this.btnStopPlayback.Size = new System.Drawing.Size(200, 50);
            this.btnStopPlayback.TabIndex = 2;
            this.btnStopPlayback.Text = "⏹ ОСТАНОВИТЬ (`)";
            this.btnStopPlayback.UseVisualStyleBackColor = false;
            this.btnStopPlayback.Click += new System.EventHandler(this.btnStopPlay_Click);
            
            // panelStatus
            this.panelStatus.BackColor = System.Drawing.Color.White;
            this.panelStatus.Controls.Add(this.lblStatus);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelStatus.Location = new System.Drawing.Point(0, 530);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(700, 70);
            this.panelStatus.TabIndex = 3;
            
            // lblStatus
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(700, 70);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "● ГОТОВ К РАБОТЕ";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // panelHotkeys
            this.panelHotkeys.BackColor = System.Drawing.Color.White;
            this.panelHotkeys.Controls.Add(this.lblHotkeys);
            this.panelHotkeys.Controls.Add(this.lblHotkeysTitle);
            this.panelHotkeys.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelHotkeys.Location = new System.Drawing.Point(0, 600);
            this.panelHotkeys.Name = "panelHotkeys";
            this.panelHotkeys.Size = new System.Drawing.Size(700, 70);
            this.panelHotkeys.TabIndex = 4;
            
            // lblHotkeys
            this.lblHotkeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHotkeys.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHotkeys.Location = new System.Drawing.Point(0, 30);
            this.lblHotkeys.Name = "lblHotkeys";
            this.lblHotkeys.Padding = new System.Windows.Forms.Padding(30, 8, 30, 8);
            this.lblHotkeys.Size = new System.Drawing.Size(700, 40);
            this.lblHotkeys.TabIndex = 1;
            this.lblHotkeys.Text = "F9 - Запись | F10 - Стоп | PAUSE - Пауза записи | F11 - Воспроизвести | F12 - Цикл | HOME - Пауза воспр. | ` - Стоп";
            this.lblHotkeys.AutoSize = false;
            this.lblHotkeys.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // lblHotkeysTitle
            this.lblHotkeysTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblHotkeysTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHotkeysTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblHotkeysTitle.ForeColor = System.Drawing.Color.White;
            this.lblHotkeysTitle.Location = new System.Drawing.Point(0, 0);
            this.lblHotkeysTitle.Name = "lblHotkeysTitle";
            this.lblHotkeysTitle.Size = new System.Drawing.Size(700, 30);
            this.lblHotkeysTitle.TabIndex = 0;
            this.lblHotkeysTitle.Text = "Горячие клавиши:";
            this.lblHotkeysTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // panelLogs
            this.panelLogs.BackColor = System.Drawing.Color.White;
            this.panelLogs.Controls.Add(this.txtLogs);
            this.panelLogs.Controls.Add(this.lblLogsTitle);
            this.panelLogs.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelLogs.Location = new System.Drawing.Point(700, 0);
            this.panelLogs.Name = "panelLogs";
            this.panelLogs.Size = new System.Drawing.Size(400, 670);
            this.panelLogs.TabIndex = 5;
            
            // txtLogs
            this.txtLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.txtLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLogs.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLogs.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.txtLogs.Location = new System.Drawing.Point(0, 30);
            this.txtLogs.Name = "txtLogs";
            this.txtLogs.ReadOnly = true;
            this.txtLogs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLogs.Size = new System.Drawing.Size(400, 640);
            this.txtLogs.TabIndex = 1;
            this.txtLogs.Text = "";
            
            // lblLogsTitle
            this.lblLogsTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.lblLogsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLogsTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblLogsTitle.ForeColor = System.Drawing.Color.White;
            this.lblLogsTitle.Location = new System.Drawing.Point(0, 0);
            this.lblLogsTitle.Name = "lblLogsTitle";
            this.lblLogsTitle.Size = new System.Drawing.Size(400, 30);
            this.lblLogsTitle.TabIndex = 0;
            this.lblLogsTitle.Text = "ЛОГИ";
            this.lblLogsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // panelMain
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.panelHotkeys);
            this.panelMain.Controls.Add(this.panelStatus);
            this.panelMain.Controls.Add(this.panelPlayback);
            this.panelMain.Controls.Add(this.panelRecording);
            this.panelMain.Controls.Add(this.panelHeader);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(700, 670);
            this.panelMain.TabIndex = 6;
            
            // MainForm
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 670);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelLogs);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.MinimumSize = new System.Drawing.Size(1116, 689);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Macro Recorder";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panelHeader.ResumeLayout(false);
            this.panelRecording.ResumeLayout(false);
            this.panelRecording.PerformLayout();
            this.panelPlayback.ResumeLayout(false);
            this.panelPlayback.PerformLayout();
            this.panelStatus.ResumeLayout(false);
            this.panelHotkeys.ResumeLayout(false);
            this.panelLogs.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private Label lblTitle;
        private Panel panelRecording;
        private Button btnStartRecord;
        private Button btnStopRecord;
        private Panel panelPlayback;
        private Button btnPlay;
        private Button btnPlayLoop;
        private Button btnStopPlayback;
        private Panel panelStatus;
        private Label lblStatus;
        private Panel panelHotkeys;
        private Label lblHotkeys;
        private Label lblHotkeysTitle;
        private Label lblRecordingTitle;
        private Label lblPlaybackTitle;
        private Button btnPauseRecord;
        private Button btnPausePlayback;
        private Panel panelLogs;
        private RichTextBox txtLogs;
        private Label lblLogsTitle;
        private Panel panelMain;
        private CheckBox chkTrackLanguage;
    }
}