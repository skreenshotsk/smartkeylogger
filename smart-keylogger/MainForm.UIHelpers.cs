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
            if (!isRecording && !isPlaying && lblStatus != null)
            {
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
            ApplyDynamicHotkeyTexts();

            bool isWaitingMode = isWaitingForSchedule && !isPlaying && !isRecording;
            
            bool hasSelectedFile = !string.IsNullOrEmpty(GetSelectedMacroFile());
            
            bool canRecord = !isRecording && !isPlaying && !isWaitingMode;
            bool canStopRecord = isRecording && !isPlaying;
            bool canPauseRecord = isRecording && !isPlaying;

            bool canPlay = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            bool canPlayLoop = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            bool canStopPlayback = isPlaying && !isRecording || isWaitingMode;
            bool canSchedulePlayback = !isRecording && !isPlaying && !isWaitingMode && hasSelectedFile;
            bool canPausePlayback = isPlaying && !isRecording && player != null;

            UpdateButtonAppearance(btnStartRecord, canRecord);
            UpdateButtonAppearance(btnStopRecord, canStopRecord);
            UpdateButtonAppearance(btnPauseRecord, canPauseRecord);
            UpdateButtonAppearance(btnPlay, canPlay);
            UpdateButtonAppearance(btnPlayLoop, canPlayLoop);
            UpdateButtonAppearance(btnStopPlayback, canStopPlayback);
            UpdateButtonAppearance(btnPausePlayback, canPausePlayback);
            
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
                    btnSchedulePlayback.BackColor = System.Drawing.Color.FromArgb(156, 39, 176);
                    btnSchedulePlayback.ForeColor = System.Drawing.Color.White;
                }
            }

            string pauseRecordHotkey = GetHotkeyText(HOTKEY_PAUSE_RECORD, Keys.Pause, MOD_NONE);
            string pausePlaybackHotkey = GetHotkeyText(HOTKEY_PAUSE_PLAYBACK, Keys.Home, MOD_NONE);

            if (recorder != null && isRecording)
            {
                if (recorder.IsPaused)
                {
                    btnPauseRecord.Text = $"▶ ПРОДОЛЖИТЬ ЗАПИСЬ ({pauseRecordHotkey})";
                    btnPauseRecord.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
                }
                else
                {
                    btnPauseRecord.Text = $"⏸ ПАУЗА ЗАПИСИ ({pauseRecordHotkey})";
                    btnPauseRecord.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
                }
            }
            else
            {
                btnPauseRecord.Text = $"⏸ ПАУЗА ЗАПИСИ ({pauseRecordHotkey})";
            }

            if (player != null && isPlaying)
            {
                if (!player.PlaybackStarted)
                {
                    btnPausePlayback.Text = $"⏸ ПРОПУСТИТЬ ОТСЧЁТ / ПАУЗА ({pausePlaybackHotkey})";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
                }
                else if (player.IsPaused)
                {
                    btnPausePlayback.Text = $"▶ ПРОДОЛЖИТЬ ВОСПРОИЗВЕДЕНИЕ ({pausePlaybackHotkey})";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
                }
                else
                {
                    btnPausePlayback.Text = $"⏸ ПАУЗА ВОСПРОИЗВЕДЕНИЯ ({pausePlaybackHotkey})";
                    btnPausePlayback.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
                }
            }
            else
            {
                btnPausePlayback.Text = $"⏸ ПАУЗА ВОСПРОИЗВЕДЕНИЯ ({pausePlaybackHotkey})";
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

        private void ApplyDynamicHotkeyTexts()
        {
            string startRecordHotkey = GetHotkeyText(HOTKEY_START_RECORD, Keys.F9, MOD_NONE);
            string stopRecordHotkey = GetHotkeyText(HOTKEY_STOP_RECORD, Keys.F10, MOD_NONE);
            string playHotkey = GetHotkeyText(HOTKEY_PLAY, Keys.F11, MOD_NONE);
            string playLoopHotkey = GetHotkeyText(HOTKEY_PLAY_LOOP, Keys.F12, 0x0002);
            string stopPlaybackHotkey = GetHotkeyText(HOTKEY_STOP_PLAY, (Keys)0xC0, MOD_NONE);

            btnStartRecord.Text = $"🖱️ НАЧАТЬ ЗАПИСЬ ({startRecordHotkey})";
            btnStopRecord.Text = $"💾 СОХРАНИТЬ ЗАПИСЬ ({stopRecordHotkey})";
            btnPlay.Text = $"▶ ВОСПРОИЗВЕСТИ ({playHotkey})";
            btnPlayLoop.Text = $"🔄 ЦИКЛ ({playLoopHotkey})";
            btnStopPlayback.Text = $"⏹ ОСТАНОВИТЬ ({stopPlaybackHotkey})";
        }

        private string GetHotkeyText(int hotkeyId, Keys defaultKey, uint defaultMod)
        {
            Keys key = savedHotkeys.ContainsKey(hotkeyId) ? savedHotkeys[hotkeyId] : defaultKey;
            uint mod = savedHotkeyModifiers.ContainsKey(hotkeyId) ? savedHotkeyModifiers[hotkeyId] : defaultMod;
            return FormatKeysToString(key, mod);
        }
    }
}

