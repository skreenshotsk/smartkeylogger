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
            this.btnStartRecord = new System.Windows.Forms.Button();
            this.btnStopRecord = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPlayLoop = new System.Windows.Forms.Button();
            this.btnStopPlay = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblHotkeys = new System.Windows.Forms.Label();
            this.btnSettings = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnStartRecord
            // 
            this.btnStartRecord.Location = new System.Drawing.Point(20, 20);
            this.btnStartRecord.Name = "btnStartRecord";
            this.btnStartRecord.Size = new System.Drawing.Size(140, 45);
            this.btnStartRecord.TabIndex = 0;
            this.btnStartRecord.Text = "Старт записи (F9)";
            this.btnStartRecord.UseVisualStyleBackColor = true;
            this.btnStartRecord.Click += new System.EventHandler(this.btnStartRecord_Click);
            // 
            // btnStopRecord
            // 
            this.btnStopRecord.Location = new System.Drawing.Point(170, 20);
            this.btnStopRecord.Name = "btnStopRecord";
            this.btnStopRecord.Size = new System.Drawing.Size(140, 45);
            this.btnStopRecord.TabIndex = 1;
            this.btnStopRecord.Text = "Стоп записи (F10)";
            this.btnStopRecord.UseVisualStyleBackColor = true;
            this.btnStopRecord.Click += new System.EventHandler(this.btnStopRecord_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(20, 80);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(140, 45);
            this.btnPlay.TabIndex = 2;
            this.btnPlay.Text = "Воспроизвести (F11)";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnPlayLoop
            // 
            this.btnPlayLoop.Location = new System.Drawing.Point(170, 80);
            this.btnPlayLoop.Name = "btnPlayLoop";
            this.btnPlayLoop.Size = new System.Drawing.Size(140, 45);
            this.btnPlayLoop.TabIndex = 3;
            this.btnPlayLoop.Text = "Цикл (F12)";
            this.btnPlayLoop.UseVisualStyleBackColor = true;
            this.btnPlayLoop.Click += new System.EventHandler(this.btnPlayLoop_Click);
            // 
            // btnStopPlay
            // 
            this.btnStopPlay.Location = new System.Drawing.Point(320, 80);
            this.btnStopPlay.Name = "btnStopPlay";
            this.btnStopPlay.Size = new System.Drawing.Size(160, 45);
            this.btnStopPlay.TabIndex = 4;
            this.btnStopPlay.Text = "Стоп воспроизведения (Backquote)";
            this.btnStopPlay.UseVisualStyleBackColor = true;
            this.btnStopPlay.Click += new System.EventHandler(this.btnStopPlay_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblStatus.Location = new System.Drawing.Point(20, 140);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(65, 20);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Готов";
            // 
            // lblHotkeys
            // 
            this.lblHotkeys.AutoSize = true;
            this.lblHotkeys.Location = new System.Drawing.Point(20, 180);
            this.lblHotkeys.Name = "lblHotkeys";
            this.lblHotkeys.Size = new System.Drawing.Size(0, 13);
            this.lblHotkeys.TabIndex = 6;
            // 
            // btnSettings
            // 
            this.btnSettings.Location = new System.Drawing.Point(320, 20);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(160, 45);
            this.btnSettings.TabIndex = 7;
            this.btnSettings.Text = "Информация";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(500, 280);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.lblHotkeys);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnStopPlay);
            this.Controls.Add(this.btnPlayLoop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.btnStopRecord);
            this.Controls.Add(this.btnStartRecord);
            this.MinimumSize = new System.Drawing.Size(516, 319);
            this.Name = "MainForm";
            this.Text = "Macro Recorder GUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnStartRecord;
        private Button btnStopRecord;
        private Button btnPlay;
        private Button btnPlayLoop;
        private Button btnStopPlay;
        private Label lblStatus;
        private Label lblHotkeys;
        private Button btnSettings;
    }
}