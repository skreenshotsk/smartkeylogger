// MainForm.PlaybackManager.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void TogglePausePlayback()
        {
            TogglePausePlayback(true);
        }

        private void UpdatePlaybackTime()
        {
            if (isPlaying && player != null && lblStatus != null && player.PlaybackStarted)
            {
                TimeSpan currentTime = player.CurrentPlaybackTime;
                TimeSpan totalTime = player.TotalPlaybackTime;
                
                string currentText = $"{currentTime:mm\\:ss\\.ff}";
                string totalText = $"{totalTime:mm\\:ss\\.ff}";
                
                if (player.IsPaused)
                {
                    lblStatus.Text = $"⏸ ВОСПРОИЗВЕДЕНИЕ ПРИОСТАНОВЛЕНО [{currentText} / {totalText}] (HOME ДЛЯ ПРОДОЛЖЕНИЯ)";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(232, 245, 233);
                }
                else
                {
                    lblStatus.Text = $"▶ ИДЕТ ВОСПРОИЗВЕДЕНИЕ [{currentText} / {totalText}] (HOME ДЛЯ ПАУЗЫ, ` ДЛЯ ОСТАНОВКИ)";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(232, 245, 233);
                }
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlaybackTime();
        }

        private void TogglePausePlayback(bool propagateToRemotes)
        {
            if (!isPlaying || player == null) return;
            
            // Не позволяем паузу во время технической паузы
            if (!player.PlaybackStarted)
            {
                return;
            }

            // Мгновенное переключение без задержек
            if (player.IsPaused)
            {
                player.ResumePlayback();
            }
            else
            {
                player.PausePlayback();
            }
            
            // Обновляем UI асинхронно чтобы не блокировать
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateUI));
            }
            else
            {
                UpdateUI();
            }

            if (propagateToRemotes)
            {
                _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "toggle_pause" });
            }
        }

        private async void PlayMacro()
        {
            if (isPlaying || isRecording || recorder == null || player == null) return;

            // Определяем какой файл загружать
            string fileToLoad = GetSelectedMacroFile();
            if (string.IsNullOrEmpty(fileToLoad))
            {
                AddLog("⚠️ Файл для воспроизведения не выбран");
                MessageBox.Show("Пожалуйста, выберите файл для воспроизведения или загрузите файл из проводника.", 
                    "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddLog($"📂 Загрузка файла для воспроизведения: {Path.GetFileName(fileToLoad)}");
            var events = recorder.LoadFromFile(fileToLoad);
            
            // Проверяем наличие событий до установки состояния воспроизведения
            if (events == null || events.Count == 0)
            {
                // Сбрасываем состояние, если оно было установлено (например, из планировщика)
                isPlaying = false;
                isWaitingForSchedule = false;
                scheduleTimer?.Stop();
                scheduledDateTime = null;
                scheduledMacroFile = null;
                playbackCancellation?.Cancel();
                playbackTimer?.Stop();
                UpdateUI();
                
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // События есть - начинаем воспроизведение
            isPlaying = true;
            isWaitingForSchedule = false; // Сбрасываем режим ожидания, начинается воспроизведение
            playbackCancellation = new System.Threading.CancellationTokenSource();
            AddLog("▶ Запущено воспроизведение (F11)");
            playbackTimer?.Start();
            UpdateUI();
            
            // Запускаем задачу для периодического обновления UI во время технической паузы
            var uiUpdateTask = Task.Run(async () =>
            {
                while (isPlaying && player != null && !player.PlaybackStarted && !playbackCancellation.Token.IsCancellationRequested)
                {
                    await Task.Delay(100);
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(UpdateUI));
                    }
                }
            });
            
            var keyboardLayout = recorder.GetRecordedKeyboardLayout();
            var durationTicks = recorder.GetRecordedDurationTicks();
            var macroData = new MacroData
            {
                Events = events,
                KeyboardLayout = keyboardLayout?.ToInt64(),
                DurationTicks = durationTicks
            };
            bool runLocal = IsSelfSelected();
            _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "play", Loop = false, MacroData = macroData });

            if (!runLocal)
            {
                AddLog("🌐 Воспроизведение отправлено на выбранные ПК (текущее устройство не выбрано)");
                isPlaying = false;
                playbackTimer?.Stop();
                UpdateUI();
                return;
            }
            
            try
            {
                await player.PlayEvents(events, false, playbackCancellation.Token, keyboardLayout, durationTicks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Воспроизведение остановлено пользователем");
            }
            
            isPlaying = false;
            UpdateUI();
        }

        private async void PlayMacroLoop()
        {
            if (isPlaying || isRecording || recorder == null || player == null) return;

            // Определяем какой файл загружать
            string fileToLoad = GetSelectedMacroFile();
            if (string.IsNullOrEmpty(fileToLoad))
            {
                AddLog("⚠️ Файл для воспроизведения не выбран");
                MessageBox.Show("Пожалуйста, выберите файл для воспроизведения или загрузите файл из проводника.", 
                    "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddLog($"📂 Загрузка файла для циклического воспроизведения: {Path.GetFileName(fileToLoad)}");
            var events = recorder.LoadFromFile(fileToLoad);
            
            // Проверяем наличие событий до установки состояния воспроизведения
            if (events == null || events.Count == 0)
            {
                // Сбрасываем состояние
                isPlaying = false;
                isWaitingForSchedule = false;
                scheduleTimer?.Stop();
                scheduledDateTime = null;
                scheduledMacroFile = null;
                playbackCancellation?.Cancel();
                playbackTimer?.Stop();
                UpdateUI();
                
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // События есть - начинаем воспроизведение
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
                isWaitingForSchedule = false; // Сбрасываем режим ожидания, начинается воспроизведение
                playbackCancellation = new System.Threading.CancellationTokenSource();
                AddLog("🔄 Запущено циклическое воспроизведение (Ctrl+F12)");
                playbackTimer?.Start();
                UpdateUI();
                
                var keyboardLayout = recorder.GetRecordedKeyboardLayout();
                var durationTicks = recorder.GetRecordedDurationTicks();
                var macroData = new MacroData
                {
                    Events = events,
                    KeyboardLayout = keyboardLayout?.ToInt64(),
                    DurationTicks = durationTicks
                };
                bool runLocal = IsSelfSelected();
                _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "play", Loop = true, MacroData = macroData });
                if (!runLocal)
                {
                    AddLog("🌐 Циклическое воспроизведение отправлено на выбранные ПК (текущее устройство не выбрано)");
                    isPlaying = false;
                    playbackTimer?.Stop();
                    UpdateUI();
                    return;
                }
                
                // Запускаем задачу для периодического обновления UI во время технической паузы
                var uiUpdateTask = Task.Run(async () =>
                {
                    while (isPlaying && player != null && !player.PlaybackStarted && !playbackCancellation.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(UpdateUI));
                        }
                    }
                });
                
                try
                {
                    await player.PlayEvents(events, true, playbackCancellation.Token, keyboardLayout, durationTicks);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Воспроизведение остановлено пользователем");
                }
                
                isPlaying = false;
                UpdateUI();
            }
        }

        private void StopPlayback()
        {
            StopPlayback(true);
        }

        private void StopPlayback(bool propagateToRemotes)
        {
            // Если ожидается запланированное выполнение, отменяем его
            if (isWaitingForSchedule && !isPlaying)
            {
                scheduleTimer?.Stop();
                isWaitingForSchedule = false;
                scheduledDateTime = null;
                scheduledMacroFile = null;
                AddLog("❌ Запланированное воспроизведение отменено");
                UpdateUI();
                if (propagateToRemotes)
                {
                    _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "stop" });
                }
                return;
            }

            if (!isPlaying || player == null)
            {
                if (propagateToRemotes)
                {
                    _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "stop" });
                }
                return;
            }

            playbackCancellation?.Cancel();
            player.Stop();
            isPlaying = false;
            AddLog("⏹ Воспроизведение остановлено (`)");
            UpdateUI();

            if (propagateToRemotes)
            {
                _ = BroadcastRemoteCommandAsync(new RemoteCommandMessage { Command = "stop" });
            }
            
            Console.WriteLine("Воспроизведение остановлено по запросу пользователя");
        }

        private async void StartRemotePlayback(MacroData macroData, bool loop)
        {
            if (isPlaying || isRecording || player == null) return;
            if (macroData.Events == null || macroData.Events.Count == 0) return;

            isPlaying = true;
            isWaitingForSchedule = false;
            playbackCancellation = new System.Threading.CancellationTokenSource();
            playbackTimer?.Start();
            AddLog(loop ? "🌐 Получена команда: циклическое воспроизведение" : "🌐 Получена команда: воспроизведение");
            UpdateUI();

            IntPtr? keyboardLayout = macroData.KeyboardLayout.HasValue ? new IntPtr(macroData.KeyboardLayout.Value) : null;
            try
            {
                await player.PlayEvents(macroData.Events, loop, playbackCancellation.Token, keyboardLayout, macroData.DurationTicks);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            isPlaying = false;
            UpdateUI();
        }
    }
}

