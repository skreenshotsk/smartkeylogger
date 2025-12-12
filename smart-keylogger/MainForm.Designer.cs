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
            this.btnSettings = new System.Windows.Forms.Button();
            this.panelPlayback = new System.Windows.Forms.Panel();
            this.btnLoadMacroFile = new System.Windows.Forms.Button();
            this.cmbMacroFiles = new System.Windows.Forms.ComboBox();
            this.lblMacroFile = new System.Windows.Forms.Label();
            this.btnSchedulePlayback = new System.Windows.Forms.Button();
            this.btnPausePlayback = new System.Windows.Forms.Button();
            this.lblPlaybackTitle = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPlayLoop = new System.Windows.Forms.Button();
            this.btnStopPlayback = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelLogs = new System.Windows.Forms.Panel();
            this.txtLogs = new System.Windows.Forms.RichTextBox();
            this.lblLogsTitle = new System.Windows.Forms.Label();
            this.panelSettings = new System.Windows.Forms.Panel();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.panelRecording.SuspendLayout();
            this.panelPlayback.SuspendLayout();
            this.panelStatus.SuspendLayout();
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
            this.panelRecording.Controls.Add(this.btnSettings);
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
            
            // btnSettings
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(156)))), ((int)(((byte)(39)))), ((int)(((byte)(176)))));
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(450, 180);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(220, 35);
            this.btnSettings.TabIndex = 5;
            this.btnSettings.Text = "⚙️ НАСТРОЙКИ";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            
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
            this.btnStopRecord.Text = "💾 СОХРАНИТЬ ЗАПИСЬ (F10)";
            this.btnStopRecord.UseVisualStyleBackColor = false;
            this.btnStopRecord.Click += new System.EventHandler(this.btnStopRecord_Click);
            
            // btnSettings
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(156)))), ((int)(((byte)(39)))), ((int)(((byte)(176)))));
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(450, 180);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(220, 35);
            this.btnSettings.TabIndex = 5;
            this.btnSettings.Text = "⚙️ НАСТРОЙКИ";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            
            // panelPlayback
            this.panelPlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.panelPlayback.Controls.Add(this.btnSchedulePlayback);
            this.panelPlayback.Controls.Add(this.btnLoadMacroFile);
            this.panelPlayback.Controls.Add(this.cmbMacroFiles);
            this.panelPlayback.Controls.Add(this.lblMacroFile);
            this.panelPlayback.Controls.Add(this.btnPausePlayback);
            this.panelPlayback.Controls.Add(this.lblPlaybackTitle);
            this.panelPlayback.Controls.Add(this.btnPlay);
            this.panelPlayback.Controls.Add(this.btnPlayLoop);
            this.panelPlayback.Controls.Add(this.btnStopPlayback);
            this.panelPlayback.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPlayback.Location = new System.Drawing.Point(0, 310);
            this.panelPlayback.MinimumSize = new System.Drawing.Size(0, 320);
            this.panelPlayback.Name = "panelPlayback";
            this.panelPlayback.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.panelPlayback.Size = new System.Drawing.Size(700, 280);
            this.panelPlayback.TabIndex = 2;
            
            // btnPausePlayback
            this.btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.btnPausePlayback.FlatAppearance.BorderSize = 0;
            this.btnPausePlayback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPausePlayback.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPausePlayback.ForeColor = System.Drawing.Color.White;
            this.btnPausePlayback.Location = new System.Drawing.Point(30, 185);
            this.btnPausePlayback.Name = "btnPausePlayback";
            this.btnPausePlayback.Size = new System.Drawing.Size(640, 50);
            this.btnPausePlayback.TabIndex = 4;
            this.btnPausePlayback.Text = "⏸ ПАУЗА ВОСПРОИЗВЕДЕНИЯ (HOME)";
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
            
            // lblMacroFile
            this.lblMacroFile.AutoSize = true;
            this.lblMacroFile.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblMacroFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.lblMacroFile.Location = new System.Drawing.Point(27, 50);
            this.lblMacroFile.Name = "lblMacroFile";
            this.lblMacroFile.Size = new System.Drawing.Size(120, 19);
            this.lblMacroFile.TabIndex = 5;
            this.lblMacroFile.Text = "Файл макроса:";
            
            // cmbMacroFiles
            this.cmbMacroFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMacroFiles.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.cmbMacroFiles.FormattingEnabled = true;
            this.cmbMacroFiles.Location = new System.Drawing.Point(30, 75);
            this.cmbMacroFiles.Name = "cmbMacroFiles";
            this.cmbMacroFiles.Size = new System.Drawing.Size(420, 29);
            this.cmbMacroFiles.TabIndex = 6;
            this.cmbMacroFiles.SelectedIndexChanged += new System.EventHandler(this.cmbMacroFiles_SelectedIndexChanged);
            
            // btnLoadMacroFile
            this.btnLoadMacroFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.btnLoadMacroFile.FlatAppearance.BorderSize = 0;
            this.btnLoadMacroFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadMacroFile.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnLoadMacroFile.ForeColor = System.Drawing.Color.White;
            this.btnLoadMacroFile.Location = new System.Drawing.Point(470, 75);
            this.btnLoadMacroFile.Name = "btnLoadMacroFile";
            this.btnLoadMacroFile.Size = new System.Drawing.Size(200, 29);
            this.btnLoadMacroFile.TabIndex = 7;
            this.btnLoadMacroFile.Text = "📂 Загрузить из проводника";
            this.btnLoadMacroFile.UseVisualStyleBackColor = false;
            this.btnLoadMacroFile.Click += new System.EventHandler(this.btnLoadMacroFile_Click);
            
            // btnSchedulePlayback
            this.btnSchedulePlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(156)))), ((int)(((byte)(39)))), ((int)(((byte)(176)))));
            this.btnSchedulePlayback.FlatAppearance.BorderSize = 0;
            this.btnSchedulePlayback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSchedulePlayback.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSchedulePlayback.ForeColor = System.Drawing.Color.White;
            this.btnSchedulePlayback.Location = new System.Drawing.Point(470, 115);
            this.btnSchedulePlayback.Name = "btnSchedulePlayback";
            this.btnSchedulePlayback.Size = new System.Drawing.Size(200, 50);
            this.btnSchedulePlayback.TabIndex = 8;
            this.btnSchedulePlayback.Text = "⏰ ЗАДАТЬ ВРЕМЯ ВЫПОЛНЕНИЯ";
            this.btnSchedulePlayback.UseVisualStyleBackColor = false;
            this.btnSchedulePlayback.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSchedulePlayback.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(123)))), ((int)(((byte)(31)))), ((int)(((byte)(162)))));
            this.btnSchedulePlayback.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(20)))), ((int)(((byte)(140)))));
            this.btnSchedulePlayback.Click += new System.EventHandler(this.btnSchedulePlayback_Click);
            this.panelPlayback.Controls.Add(this.btnSchedulePlayback);
            
            // btnPlay
            this.btnPlay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPlay.ForeColor = System.Drawing.Color.White;
            this.btnPlay.Location = new System.Drawing.Point(30, 115);
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
            this.btnPlayLoop.Location = new System.Drawing.Point(250, 115);
            this.btnPlayLoop.Name = "btnPlayLoop";
            this.btnPlayLoop.Size = new System.Drawing.Size(200, 50);
            this.btnPlayLoop.TabIndex = 1;
            this.btnPlayLoop.Text = "🔄 ЦИКЛ (Ctrl+F12)";
            this.btnPlayLoop.UseVisualStyleBackColor = false;
            this.btnPlayLoop.Click += new System.EventHandler(this.btnPlayLoop_Click);
            
            // btnStopPlayback
            this.btnStopPlayback.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.btnStopPlayback.FlatAppearance.BorderSize = 0;
            this.btnStopPlayback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopPlayback.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnStopPlayback.ForeColor = System.Drawing.Color.White;
            this.btnStopPlayback.Location = new System.Drawing.Point(30, 250);
            this.btnStopPlayback.Name = "btnStopPlayback";
            this.btnStopPlayback.Size = new System.Drawing.Size(640, 50);
            this.btnStopPlayback.TabIndex = 2;
            this.btnStopPlayback.Text = "⏹ ОСТАНОВИТЬ (`)";
            this.btnStopPlayback.UseVisualStyleBackColor = false;
            this.btnStopPlayback.Click += new System.EventHandler(this.btnStopPlay_Click);
            
            // panelStatus
            this.panelStatus.BackColor = System.Drawing.Color.White;
            this.panelStatus.Controls.Add(this.lblStatus);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
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
            
            // panelLogs
            this.panelLogs.BackColor = System.Drawing.Color.White;
            this.panelLogs.Controls.Add(this.txtLogs);
            this.panelLogs.Controls.Add(this.lblLogsTitle);
            this.panelLogs.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelLogs.Location = new System.Drawing.Point(700, 0);
            this.panelLogs.Name = "panelLogs";
            this.panelLogs.Size = new System.Drawing.Size(400, 670);
            this.panelLogs.Visible = true; // Видимость управляется в коде
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
            
            // panelSettings
            this.panelSettings = new System.Windows.Forms.Panel();
            this.panelSettings.BackColor = System.Drawing.Color.White;
            this.panelSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSettings.Location = new System.Drawing.Point(0, 0);
            this.panelSettings.Name = "panelSettings";
            this.panelSettings.Size = new System.Drawing.Size(700, 670);
            this.panelSettings.Visible = false;
            this.panelSettings.TabIndex = 7;
            
            // panelMain
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.panelPlayback);
            this.panelMain.Controls.Add(this.panelRecording);
            this.panelMain.Controls.Add(this.panelStatus);
            this.panelMain.Controls.Add(this.panelHeader);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(700, 670);
            this.panelMain.TabIndex = 6;
            
            // MainForm
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 750);
            this.Controls.Add(this.panelSettings);
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
            this.panelLogs.ResumeLayout(false);
            this.panelSettings.ResumeLayout(false);
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
        private Label lblRecordingTitle;
        private Label lblPlaybackTitle;
        private Button btnPauseRecord;
        private Button btnPausePlayback;
        private Panel panelLogs;
        private RichTextBox txtLogs;
        private Label lblLogsTitle;
        private Panel panelMain;
        private CheckBox chkTrackLanguage;
        private ComboBox cmbMacroFiles;
        private Button btnLoadMacroFile;
        private Label lblMacroFile;
        private Button btnSettings;
        private Button btnSchedulePlayback;
        private Panel panelSettings;
    }
}