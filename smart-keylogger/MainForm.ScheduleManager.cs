// MainForm.ScheduleManager.cs
using System;
using System.IO;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void ScheduleTimer_Tick(object sender, EventArgs e)
        {
            if (scheduledDateTime.HasValue && scheduledMacroFile != null && isWaitingForSchedule)
            {
                if (DateTime.Now >= scheduledDateTime.Value)
                {
                    // Время наступило - запускаем воспроизведение
                    scheduleTimer?.Stop();
                    isWaitingForSchedule = false;
                    currentMacroFile = scheduledMacroFile;
                    scheduledDateTime = null;
                    scheduledMacroFile = null;
                    
                    AddLog($"⏰ Запуск запланированного воспроизведения: {Path.GetFileName(currentMacroFile)}");
                    PlayMacro();
                }
                else
                {
                    // Обновляем статус
                    if (lblStatus != null && !isRecording && !isPlaying)
                    {
                        string dateStr = scheduledDateTime.Value.ToString("dd.MM.yyyy");
                        string timeStr = scheduledDateTime.Value.ToString("HH:mm");
                        lblStatus.Text = $"⏳ Ожидание {dateStr} {timeStr} для воспроизведения";
                        lblStatus.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
                        lblStatus.BackColor = System.Drawing.Color.FromArgb(255, 243, 224);
                    }
                }
            }
        }
    }
}


