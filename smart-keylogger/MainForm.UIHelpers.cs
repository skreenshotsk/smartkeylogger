// MainForm.UIHelpers.cs
using System;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void AddLog(string message)
        {
            if (txtLogs == null) return;
            
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AddLog), message);
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText($"[{timestamp}] {message}\n");
            txtLogs.SelectionStart = txtLogs.Text.Length;
            txtLogs.ScrollToCaret();
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateUI));
                return;
            }

            UpdateButtonStates();
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            // Обновляем статус (если не в режиме записи)
            if (!isRecording && !isPlaying && lblStatus != null)
            {
                // Если ожидается запланированное выполнение, статус уже обновляется в ScheduleTimer_Tick
                if (!isWaitingForSchedule)
                {
                    lblStatus.Text = "● ГОТОВ К РАБОТЕ";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(227, 242, 253);
                }
            }
        }

        private void UpdateButtonStates()
        {
            // В режиме ожидания все кнопки недоступны кроме остановки
            bool isWaitingMode = isWaitingForSchedule && !isPlaying && !isRecording;
            
            // Проверяем наличие выбранного файла через GetSelectedMacroFile
            bool hasSelectedFile = !string.IsNullOrEmpty(GetSelectedMacroFile());
            
            // Запись
            bool canRecord = !isRecording && !isPlaying && !isWaitingMode;
            bool canStopRecord = isRecording && !isPlaying;
            bool canPauseRecord = isRecording && !isPlaying;

            // Воспроизведение
            bool canPlay = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            bool canPlayLoop = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            bool canStopPlayback = isPlaying && !isRecording || isWaitingMode;
            bool canSchedulePlayback = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            // Кнопка паузы доступна только после начала реального воспроизведения
            bool canPausePlayback = isPlaying && !isRecording && (player?.PlaybackStarted ?? false);

            UpdateButtonAppearance(btnStartRecord, canRecord);
            UpdateButtonAppearance(btnStopRecord, canStopRecord);
            UpdateButtonAppearance(btnPauseRecord, canPauseRecord);
            UpdateButtonAppearance(btnPlay, canPlay);
            UpdateButtonAppearance(btnPlayLoop, canPlayLoop);
            UpdateButtonAppearance(btnStopPlayback, canStopPlayback);
            UpdateButtonAppearance(btnPausePlayback, canPausePlayback);
            
            // Кнопка планировщика - фиолетовая, не меняем цвет
            if (btnSchedulePlayback != null)
            {
                btnSchedulePlayback.Enabled = canSchedulePlayback;
                if (!canSchedulePlayback)
                {
                    btnSchedulePlayback.BackColor = System.Drawing.Color.FromArgb(189, 189, 189);
                    btnSchedulePlayback.ForeColor = System.Drawing.Color.Gray;
                }
                else
                {
                    // Возвращаем оригинальный фиолетовый цвет
                    btnSchedulePlayback.BackColor = System.Drawing.Color.FromArgb(156, 39, 176);
                    btnSchedulePlayback.ForeColor = System.Drawing.Color.White;
                }
            }

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
                if (!player.PlaybackStarted)
                {
                    btnPausePlayback.Text = "⏸ ПАУЗА (ОЖИДАНИЕ НАЧАЛА...)";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(189, 189, 189); // Серый - недоступна
                }
                else if (player.IsPaused)
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
            if (button == null) return;

            button.Enabled = enabled;
            if (enabled)
            {
                button.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                button.ForeColor = System.Drawing.Color.White;
            }
            else
            {
                button.BackColor = System.Drawing.Color.FromArgb(189, 189, 189);
                button.ForeColor = System.Drawing.Color.Gray;
            }
        }
    }
}

