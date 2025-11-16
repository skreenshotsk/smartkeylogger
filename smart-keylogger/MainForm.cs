// MainForm.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm : Form
    {
        private MacroRecorder recorder;
        private MacroPlayer player;
        private bool isPlaying = false;
        private bool isRecording = false;
        private CancellationTokenSource playbackCancellation;
        private Dictionary<Keys, Action> hotkeyActions;

        // Горячие клавиши
        private Keys startRecordHotkey = Keys.F9;
        private Keys stopRecordHotkey = Keys.F10;
        private Keys playHotkey = Keys.F11;
        private Keys loopPlayHotkey = Keys.F12;
        private Keys stopPlayHotkey = Keys.Escape;

        public MainForm()
        {
            InitializeComponent();
            InitializeHotkeys();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            recorder = new MacroRecorder();
            player = new MacroPlayer();
            playbackCancellation = new CancellationTokenSource();

            // Настройка формы для работы с горячими клавишами когда окно не в фокусе
            this.ShowInTaskbar = true;
            this.KeyPreview = true;

            UpdateUI();
        }

        private void InitializeHotkeys()
        {
            hotkeyActions = new Dictionary<Keys, Action>
            {
                { Keys.F9, StartRecording },
                { Keys.F10, StopRecording },
                { Keys.F11, PlayMacro },
                { Keys.F12, PlayMacroLoop },
                { Keys.Escape, StopPlayback }
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Обработка горячих клавиш даже когда окно не в фокусе
            if (hotkeyActions.ContainsKey(keyData))
            {
                hotkeyActions[keyData].Invoke();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void StartRecording()
        {
            if (isRecording) return;

            isRecording = true;
            recorder.StartRecording();
            UpdateUI();
            
            // Добавляем задержку чтобы не записать нажатие самой горячей клавиши
            await Task.Delay(100);
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
            isPlaying = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Обновляем UI в основном потоке
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
                lblStatus.Text = "▶ Воспроизведение...";
                lblStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblStatus.Text = "● Готов";
                lblStatus.ForeColor = System.Drawing.Color.Blue;
            }

            // Обновляем информацию о горячих клавишах
            lblHotkeys.Text = $"Горячие клавиши (работают всегда):\n" +
                             $"F9 - Старт записи\n" +
                             $"F10 - Стоп записи\n" +
                             $"F11 - Воспроизвести\n" +
                             $"F12 - Цикл\n" +
                             $"Esc - Стоп воспроизведения";
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
            }
            
            playbackCancellation?.Dispose();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Горячие клавиши работают даже когда окно свернуто!\n\n" +
                           "Для остановки воспроизведения нажмите Esc", 
                           "Информация", 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Information);
        }
    }
}