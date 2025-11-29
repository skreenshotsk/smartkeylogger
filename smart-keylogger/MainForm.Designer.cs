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
            this.btnPauseRecord = new System.Windows.Forms.Button(); // НОВАЯ кнопка
            this.lblRecordingTitle = new System.Windows.Forms.Label();
            this.btnStartRecord = new System.Windows.Forms.Button();
            this.btnStopRecord = new System.Windows.Forms.Button();
            this.panelPlayback = new System.Windows.Forms.Panel();
            this.lblPlaybackTitle = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPlayLoop = new System.Windows.Forms.Button();
            this.btnStopPlayback = new System.Windows.Forms.Button();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelHotkeys = new System.Windows.Forms.Panel();
            this.lblHotkeys = new System.Windows.Forms.Label();
            this.lblHotkeysTitle = new System.Windows.Forms.Label();
            this.panelHeader.SuspendLayout();
            this.panelRecording.SuspendLayout();
            this.panelPlayback.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.panelHotkeys.SuspendLayout();
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
            this.panelRecording.Controls.Add(this.btnPauseRecord); // НОВАЯ кнопка
            this.panelRecording.Controls.Add(this.lblRecordingTitle);
            this.panelRecording.Controls.Add(this.btnStartRecord);
            this.panelRecording.Controls.Add(this.btnStopRecord);
            this.panelRecording.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelRecording.Location = new System.Drawing.Point(0, 80);
            this.panelRecording.Name = "panelRecording";
            this.panelRecording.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.panelRecording.Size = new System.Drawing.Size(700, 200); // Увеличил высоту для новой кнопки
            this.panelRecording.TabIndex = 1;
            
            // btnPauseRecord - НОВАЯ кнопка
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
            this.panelPlayback.Controls.Add(this.lblPlaybackTitle);
            this.panelPlayback.Controls.Add(this.btnPlay);
            this.panelPlayback.Controls.Add(this.btnPlayLoop);
            this.panelPlayback.Controls.Add(this.btnStopPlayback);
            this.panelPlayback.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPlayback.Location = new System.Drawing.Point(0, 280); // Изменил позицию из-за увеличенной высоты panelRecording
            this.panelPlayback.Name = "panelPlayback";
            this.panelPlayback.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.panelPlayback.Size = new System.Drawing.Size(700, 160);
            this.panelPlayback.TabIndex = 2;
            
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
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.panelStatus.Controls.Add(this.lblStatus);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelStatus.Location = new System.Drawing.Point(0, 440); // Изменил позицию
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
            this.panelHotkeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelHotkeys.Location = new System.Drawing.Point(0, 510); // Изменил позицию
            this.panelHotkeys.Name = "panelHotkeys";
            this.panelHotkeys.Size = new System.Drawing.Size(700, 110); // Уменьшил высоту из-за увеличенной формы
            this.panelHotkeys.TabIndex = 4;
            
            // lblHotkeys
            this.lblHotkeys.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHotkeys.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblHotkeys.Location = new System.Drawing.Point(0, 30);
            this.lblHotkeys.Name = "lblHotkeys";
            this.lblHotkeys.Padding = new System.Windows.Forms.Padding(30, 15, 30, 15);
            this.lblHotkeys.Size = new System.Drawing.Size(700, 80);
            this.lblHotkeys.TabIndex = 1;
            this.lblHotkeys.Text = "F9 - Начать запись макроса\r\nF10 - Остановить запись\r\nPAUSE - Пауза/продолжение за" +
    "писи\r\nF11 - Воспроизвести макрос один раз\r\nF12 - Воспроизводить макрос в цикле\r\n" +
    "` - Остановить воспроизведение";
            
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
            
            // MainForm
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(700, 700);
            this.Controls.Add(this.panelHotkeys);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelPlayback);
            this.Controls.Add(this.panelRecording);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.MinimumSize = new System.Drawing.Size(716, 639);
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
        private Button btnPauseRecord; // НОВАЯ кнопка
    }
}