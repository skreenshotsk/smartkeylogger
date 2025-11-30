// MainForm.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MacroRecorderGUI
{
    public partial class MainForm : Form
    {
        private MacroRecorder? recorder;
        private MacroPlayer? player;
        private CancellationTokenSource? playbackCancellation;
        private bool isPlaying = false;
        private bool isRecording = false;
        private System.Windows.Forms.Timer recordingTimer;
        
        // Константы для регистрации горячих клавиш
        private const int MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;
        
        // ID для горячих клавиш
        private const int HOTKEY_START_RECORD = 1;
        private const int HOTKEY_STOP_RECORD = 2;
        private const int HOTKEY_PLAY = 3;
        private const int HOTKEY_PLAY_LOOP = 4;
        private const int HOTKEY_STOP_PLAY = 5;
        private const int HOTKEY_PAUSE_RECORD = 6;
        private const int HOTKEY_PAUSE_PLAYBACK = 7;

        // Код клавиши Backquote ( ` )
        private const uint VK_BACKQUOTE = 0xC0;
        private const uint VK_PAUSE = 0x13; // Код клавиши Pause/Break
        private const uint VK_HOME = 0x24;  // Код клавиши Home

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainForm()
        {
            InitializeComponent();
            InitializeComponents();
            RegisterGlobalHotkeys();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            recordingTimer = new System.Windows.Forms.Timer();
            recordingTimer.Interval = 100; // Обновление каждые 100мс
            recordingTimer.Tick += RecordingTimer_Tick;
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            UpdateRecordingTime();
        }

        private void UpdateRecordingTime()
        {
            if (isRecording && recorder != null && lblStatus != null)
            {
                TimeSpan duration = recorder.RecordingDuration;
                string timeText = $"{duration:mm\\:ss\\.ff}";
                
                if (recorder.IsPaused)
                {
                    lblStatus.Text = $"⏸ ЗАПИСЬ ПРИОСТАНОВЛЕНА [{timeText}] (PAUSE ДЛЯ ПРОДОЛЖЕНИЯ)";
                }
                else
                {
                    lblStatus.Text = $"⚫ ИДЕТ ЗАПИСЬ МАКРОСА [{timeText}] (PAUSE ДЛЯ ПАУЗЫ)";
                }
            }
        }

        private void InitializeComponents()
        {
            recorder = new MacroRecorder();
            player = new MacroPlayer();
            playbackCancellation = new CancellationTokenSource();

            UpdateUI();
        }

        private void RegisterGlobalHotkeys()
        {
            // Регистрируем глобальные горячие клавиши
            RegisterHotKey(this.Handle, HOTKEY_START_RECORD, MOD_NONE, (uint)Keys.F9);
            RegisterHotKey(this.Handle, HOTKEY_STOP_RECORD, MOD_NONE, (uint)Keys.F10);
            RegisterHotKey(this.Handle, HOTKEY_PLAY, MOD_NONE, (uint)Keys.F11);
            RegisterHotKey(this.Handle, HOTKEY_PLAY_LOOP, MOD_NONE, (uint)Keys.F12);
            RegisterHotKey(this.Handle, HOTKEY_STOP_PLAY, MOD_NONE, VK_BACKQUOTE);
            RegisterHotKey(this.Handle, HOTKEY_PAUSE_RECORD, MOD_NONE, VK_PAUSE);
            RegisterHotKey(this.Handle, HOTKEY_PAUSE_PLAYBACK, MOD_NONE, VK_HOME);
        }

        private void UnregisterGlobalHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_START_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_STOP_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY_LOOP);
            UnregisterHotKey(this.Handle, HOTKEY_STOP_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_PAUSE_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_PAUSE_PLAYBACK);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                switch (m.WParam.ToInt32())
                {
                    case HOTKEY_START_RECORD:
                        BeginInvoke(new Action(StartRecording));
                        break;
                    case HOTKEY_STOP_RECORD:
                        BeginInvoke(new Action(StopRecording));
                        break;
                    case HOTKEY_PLAY:
                        BeginInvoke(new Action(PlayMacro));
                        break;
                    case HOTKEY_PLAY_LOOP:
                        BeginInvoke(new Action(PlayMacroLoop));
                        break;
                    case HOTKEY_STOP_PLAY:
                        BeginInvoke(new Action(StopPlayback));
                        break;
                    case HOTKEY_PAUSE_RECORD:
                        BeginInvoke(new Action(TogglePauseRecording));
                        break;
                    case HOTKEY_PAUSE_PLAYBACK:
                        BeginInvoke(new Action(TogglePausePlayback));
                        break;
                }
                return;
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (isPlaying && (int)keyData == 0xC0)
            {
                StopPlayback();
                return true;
            }
            
            if (keyData == Keys.F9 && !isRecording && !isPlaying)
            {
                StartRecording();
                return true;
            }
            else if (keyData == Keys.F10 && isRecording)
            {
                StopRecording();
                return true;
            }
            else if (keyData == Keys.F11 && !isRecording && !isPlaying)
            {
                PlayMacro();
                return true;
            }
            else if (keyData == Keys.F12 && !isRecording && !isPlaying)
            {
                PlayMacroLoop();
                return true;
            }
            else if (keyData == Keys.Pause && isRecording && !isPlaying)
            {
                TogglePauseRecording();
                return true;
            }
            else if (keyData == Keys.Home && isPlaying && !isRecording)
            {
                TogglePausePlayback();
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void TogglePausePlayback()
        {
            if (!isPlaying || player == null) return;

            // Мгновенное переключение без задержек
            if (player.IsPaused)
            {
                player.ResumePlayback();
                Console.WriteLine("Воспроизведение возобновлено");
            }
            else
            {
                player.PausePlayback();
                Console.WriteLine("Воспроизведение приостановлено");
            }
            
            UpdateUI();
        }

        private void TogglePauseRecording()
        {
            if (!isRecording || recorder == null) return;

            if (recorder.IsPaused)
            {
                recorder.ResumeRecording();
                Console.WriteLine("Запись возобновлена");
            }
            else
            {
                recorder.PauseRecording();
                Console.WriteLine("Запись приостановлена");
            }
            
            UpdateUI();
        }

        private async void StartRecording()
        {
            if (isRecording || isPlaying || recorder == null) return;

            isRecording = true;
            recordingTimer.Start();
            UpdateUI();
            
            await Task.Delay(500);
            recorder.StartRecording();
        }

        private void StopRecording()
        {
            if (!isRecording || recorder == null) return;

            isRecording = false;
            recordingTimer.Stop();
            recorder.StopRecording();
            recorder.SaveToFile();
            UpdateUI();
        }

        private async void PlayMacro()
        {
            if (isPlaying || isRecording || recorder == null || player == null) return;

            var events = recorder.LoadFromFile();
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
                playbackCancellation = new CancellationTokenSource();
                UpdateUI();
                
                try
                {
                    await player.PlayEvents(events, false, playbackCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Воспроизведение остановлено пользователем");
                }
                
                isPlaying = false;
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void PlayMacroLoop()
        {
            if (isPlaying || isRecording || recorder == null || player == null) return;

            var events = recorder.LoadFromFile();
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
                playbackCancellation = new CancellationTokenSource();
                UpdateUI();
                
                try
                {
                    await player.PlayEvents(events, true, playbackCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Воспроизведение остановлено пользователем");
                }
                
                isPlaying = false;
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StopPlayback()
        {
            if (!isPlaying || player == null) return;

            playbackCancellation?.Cancel();
            player.Stop();
            isPlaying = false;
            UpdateUI();
            
            Console.WriteLine("Воспроизведение остановлено по запросу пользователя");
        }

        private void UpdateUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateUI));
                return;
            }

            // Обновляем состояния кнопок
            UpdateButtonStates();

            // Обновляем статус (если не в режиме записи)
            if (!isRecording && lblStatus != null)
            {
                if (isPlaying)
                {
                    if (player != null && player.IsPaused)
                    {
                        lblStatus.Text = "⏸ ВОСПРОИЗВЕДЕНИЕ ПРИОСТАНОВЛЕНО... (HOME ДЛЯ ПРОДОЛЖЕНИЯ)";
                        lblStatus.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
                        lblStatus.BackColor = System.Drawing.Color.FromArgb(255, 243, 224);
                    }
                    else
                    {
                        lblStatus.Text = "▶ ИДЕТ ВОСПРОИЗВЕДЕНИЕ... (HOME ДЛЯ ПАУЗЫ, ` ДЛЯ ОСТАНОВКИ)";
                        lblStatus.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                        lblStatus.BackColor = System.Drawing.Color.FromArgb(232, 245, 233);
                    }
                }
                else
                {
                    lblStatus.Text = "● ГОТОВ К РАБОТЕ";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(227, 242, 253);
                }
            }
        }

        private void UpdateButtonStates()
        {
            // Запись
            btnStartRecord.Enabled = !isRecording && !isPlaying;
            btnStopRecord.Enabled = isRecording && !isPlaying;
            btnPauseRecord.Enabled = isRecording && !isPlaying;

            // Воспроизведение
            btnPlay.Enabled = !isRecording && !isPlaying;
            btnPlayLoop.Enabled = !isRecording && !isPlaying;
            btnStopPlayback.Enabled = isPlaying && !isRecording;
            btnPausePlayback.Enabled = isPlaying && !isRecording;

            // Обновляем внешний вид кнопок
            UpdateButtonAppearance(btnStartRecord, !isRecording && !isPlaying);
            UpdateButtonAppearance(btnStopRecord, isRecording && !isPlaying);
            UpdateButtonAppearance(btnPauseRecord, isRecording && !isPlaying);
            UpdateButtonAppearance(btnPlay, !isRecording && !isPlaying);
            UpdateButtonAppearance(btnPlayLoop, !isRecording && !isPlaying);
            UpdateButtonAppearance(btnStopPlayback, isPlaying && !isRecording);
            UpdateButtonAppearance(btnPausePlayback, isPlaying && !isRecording);

            // Обновляем текст кнопки паузы записи в зависимости от состояния
            if (recorder != null && isRecording)
            {
                if (recorder.IsPaused)
                {
                    btnPauseRecord.Text = "▶ ПРОДОЛЖИТЬ ЗАПИСЬ (PAUSE)";
                    btnPauseRecord.BackColor = System.Drawing.Color.FromArgb(76, 175, 80); // Зеленый для продолжения
                }
                else
                {
                    btnPauseRecord.Text = "⏸ ПАУЗА ЗАПИСИ (PAUSE)";
                    btnPauseRecord.BackColor = System.Drawing.Color.FromArgb(255, 152, 0); // Оранжевый для паузы
                }
            }

            // Обновляем текст кнопки паузы воспроизведения в зависимости от состояния
            if (player != null && isPlaying)
            {
                if (player.IsPaused)
                {
                    btnPausePlayback.Text = "▶ ПРОДОЛЖИТЬ ВОСПРОИЗВЕДЕНИЕ (HOME)";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(76, 175, 80); // Зеленый для продолжения
                }
                else
                {
                    btnPausePlayback.Text = "⏸ ПАУЗА ВОСПРОИЗВЕДЕНИЯ (HOME)";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(255, 152, 0); // Оранжевый для паузы
                }
            }
        }

        private void UpdateButtonAppearance(Button button, bool enabled)
        {
            // Активный цвет - синий (#2196F3)
            System.Drawing.Color activeColor = System.Drawing.Color.FromArgb(33, 150, 243);
            
            // Неактивный цвет - серый (#BDBDBD)
            System.Drawing.Color inactiveColor = System.Drawing.Color.FromArgb(189, 189, 189);

            if (enabled)
            {
                button.BackColor = activeColor;
                button.Cursor = Cursors.Hand;
                button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(25, 118, 210); // Темнее при наведении
                button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(13, 71, 161); // Еще темнее при нажатии
            }
            else
            {
                button.BackColor = inactiveColor;
                button.Cursor = Cursors.Default;
                button.FlatAppearance.MouseOverBackColor = inactiveColor;
                button.FlatAppearance.MouseDownBackColor = inactiveColor;
            }
        }

        private void btnPausePlayback_Click(object sender, EventArgs e)
        {
            TogglePausePlayback();
        }

        private void btnPauseRecord_Click(object sender, EventArgs e)
        {
            TogglePauseRecording();
        }

        private void btnStopPlay_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void UpdateButtonAppearance()
        {
            // Функция для установки прозрачности неактивных кнопок
            void SetButtonState(Button button, bool enabled, System.Drawing.Color enabledColor)
            {
                if (enabled)
                {
                    button.BackColor = enabledColor;
                    button.Cursor = Cursors.Hand;
                }
                else
                {
                    // Полупрозрачная версия цвета
                    button.BackColor = System.Drawing.Color.FromArgb(150, enabledColor.R, enabledColor.G, enabledColor.B);
                    button.Cursor = Cursors.Default;
                }
            }

            SetButtonState(btnStartRecord, !isRecording && !isPlaying, System.Drawing.Color.FromArgb(76, 175, 80));
            SetButtonState(btnStopRecord, isRecording && !isPlaying, System.Drawing.Color.FromArgb(244, 67, 54));
            SetButtonState(btnPauseRecord, isRecording && !isPlaying, System.Drawing.Color.FromArgb(255, 152, 0));
            SetButtonState(btnPlay, !isRecording && !isPlaying, System.Drawing.Color.FromArgb(33, 150, 243));
            SetButtonState(btnPlayLoop, !isRecording && !isPlaying, System.Drawing.Color.FromArgb(156, 39, 176));
            SetButtonState(btnStopPlayback, isPlaying && !isRecording, System.Drawing.Color.FromArgb(255, 152, 0));
            SetButtonState(btnPausePlayback, isPlaying && !isRecording, System.Drawing.Color.FromArgb(255, 152, 0));
        }

        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            StartRecording();
        }

        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            StopRecording();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            PlayMacro();
        }

        private void btnPlayLoop_Click(object sender, EventArgs e)
        {
            PlayMacroLoop();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecording && recorder != null)
            {
                recorder.StopRecording();
            }
            
            if (isPlaying && player != null)
            {
                playbackCancellation?.Cancel();
                player.Stop();
            }
            
            recordingTimer?.Stop(); // НОВОЕ: останавливаем таймер при закрытии
            UnregisterGlobalHotkeys();
            playbackCancellation?.Dispose();
            recordingTimer?.Dispose(); // НОВОЕ: освобождаем ресурсы таймера
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Горячие клавиши работают даже когда окно свернуто!\n\n" +
                           "Для остановки воспроизведения нажмите ` (Backquote) - клавиша над Tab\n" +
                           "Для паузы записи нажмите Pause/Break\n" +
                           "Для паузы воспроизведения нажмите Home\n" +
                           "Обычно находится слева от цифры 1 на стандартной клавиатуре", 
                           "Информация", 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Information);
        }
    }
}