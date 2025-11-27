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
        private MacroRecorder recorder;
        private MacroPlayer player;
        private bool isPlaying = false;
        private bool isRecording = false;
        private CancellationTokenSource playbackCancellation;
        
        // Константы для регистрации горячих клавиш
        private const int MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;
        
        // ID для горячих клавиш
        private const int HOTKEY_START_RECORD = 1;
        private const int HOTKEY_STOP_RECORD = 2;
        private const int HOTKEY_PLAY = 3;
        private const int HOTKEY_PLAY_LOOP = 4;
        private const int HOTKEY_STOP_PLAY = 5;

        // Код клавиши Backquote ( ` )
        private const uint VK_BACKQUOTE = 0xC0;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainForm()
        {
            InitializeComponent();
            InitializeComponents();
            RegisterGlobalHotkeys();
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
        }

        private void UnregisterGlobalHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_START_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_STOP_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY_LOOP);
            UnregisterHotKey(this.Handle, HOTKEY_STOP_PLAY);
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
                }
                return;
            }
            base.WndProc(ref m);
        }

        // Также оставляем обработку ProcessCmdKey для случая когда окно в фокусе
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Для Backquote нужно использовать числовое значение, так как его нет в Keys enum
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
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void StartRecording()
        {
            if (isRecording || isPlaying) return;

            isRecording = true;
            UpdateUI();
            
            // Задержка чтобы не записать саму горячую клавишу
            await Task.Delay(500);
            
            recorder.StartRecording();
        }

        private void StopRecording()
        {
            if (!isRecording) return;

            isRecording = false;
            recorder.StopRecording();
            recorder.SaveToFile();
            UpdateUI();
        }

        private async void PlayMacro()
        {
            if (isPlaying || isRecording) return;

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
            if (isPlaying || isRecording) return;

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
            if (!isPlaying) return;

            playbackCancellation.Cancel();
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

            btnStartRecord.Enabled = !isRecording && !isPlaying;
            btnStopRecord.Enabled = isRecording && !isPlaying;
            btnPlay.Enabled = !isRecording && !isPlaying;
            btnPlayLoop.Enabled = !isRecording && !isPlaying;
            btnStopPlay.Enabled = isPlaying && !isRecording;

            if (isRecording)
            {
                lblStatus.Text = "⚫ Запись...";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            else if (isPlaying)
            {
                lblStatus.Text = "▶ Воспроизведение... (` для остановки)";
                lblStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblStatus.Text = "● Готов";
                lblStatus.ForeColor = System.Drawing.Color.Blue;
            }

            lblHotkeys.Text = $"Горячие клавиши (работают всегда):\n" +
                             $"F9 - Старт записи\n" +
                             $"F10 - Стоп записи\n" +
                             $"F11 - Воспроизвести\n" +
                             $"F12 - Цикл\n" +
                             $"` (Backquote) - Стоп воспроизведения";
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

        private void btnStopPlay_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecording)
            {
                recorder.StopRecording();
            }
            
            if (isPlaying)
            {
                playbackCancellation.Cancel();
                player.Stop();
            }
            
            UnregisterGlobalHotkeys();
            playbackCancellation?.Dispose();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Горячие клавиши работают даже когда окно свернуто!\n\n" +
                           "Для остановки воспроизведения нажмите ` (Backquote) - клавиша над Tab\n" +
                           "Обычно находится слева от цифры 1 на стандартной клавиатуре", 
                           "Информация", 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Information);
        }
    }
}