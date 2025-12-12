// MainForm.RecordingManager.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void UpdateRecordingTime()
        {
            if (isRecording && recorder != null && lblStatus != null)
            {
                TimeSpan duration = recorder.RecordingDuration;
                string timeText = $"{duration:mm\\:ss\\.ff}";
                
                if (recorder.IsPaused)
                {
                    lblStatus.Text = $"⏸ ЗАПИСЬ ПРИОСТАНОВЛЕНА [{timeText}] (PAUSE ДЛЯ ПРОДОЛЖЕНИЯ)";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(244, 67, 54);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(255, 235, 238);
                }
                else
                {
                    lblStatus.Text = $"⚫ ИДЕТ ЗАПИСЬ МАКРОСА [{timeText}] (PAUSE ДЛЯ ПАУЗЫ)";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(244, 67, 54);
                    lblStatus.BackColor = System.Drawing.Color.FromArgb(255, 235, 238);
                }
            }
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            UpdateRecordingTime();
        }

        private void TogglePauseRecording()
        {
            if (!isRecording || recorder == null) return;

            // Мгновенное переключение без задержек
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
            
            // Не нужно вызывать UpdateUI() здесь, т.к. таймер сам обновляет статус
        }

        private async void StartRecording()
        {
            if (isRecording || isPlaying || recorder == null) return;

            isRecording = true;
            bool trackLang = chkTrackLanguage != null && chkTrackLanguage.Checked;
            AddLog("▶ Начата запись (F9)");
            recordingTimer?.Start();
            UpdateUI();
            
            await Task.Delay(500);
            recorder.StartRecording(trackLang);
        }

        private void StopRecording()
        {
            if (!isRecording || recorder == null) return;

            isRecording = false;
            recordingTimer?.Stop();
            recorder.StopRecording();
            
            // Если мы в режиме дозаписи в макрос
            if (isAppendingToMacro)
            {
                AppendRecordedEventsToMacro();
                isAppendingToMacro = false;
                UpdateUI();
                return;
            }
            
            // Если мы в режиме замены действия
            if (isReplacingAction && actionToReplaceIndex >= 0)
            {
                ReplaceActionInMacro();
                isReplacingAction = false;
                actionToReplaceIndex = -1;
                UpdateUI();
                return;
            }
            
            // Если мы в режиме замены движений мыши
            if (isReplacingMouseMoves && mouseMovesStartIndex >= 0)
            {
                ReplaceMouseMovesInMacro();
                isReplacingMouseMoves = false;
                mouseMovesStartIndex = -1;
                mouseMovesCountToReplace = 0;
                UpdateUI();
                return;
            }
            
            // Показываем диалог сохранения
            SaveRecordingDialog();
            
            UpdateUI();
        }
        
        private void ReplaceActionInMacro()
        {
            if (currentEditingEvents == null || recorder == null || actionToReplaceIndex < 0 || actionToReplaceIndex >= currentEditingEvents.Count) return;
            
            var recordedEvents = recorder.GetRecordedEvents();
            if (recordedEvents == null || recordedEvents.Count == 0)
            {
                AddLog("⚠️ Не записано ни одного действия для замены");
                return;
            }
            
            // Берем первое записанное событие и заменяем им старое
            var newEvent = new InputEvent
            {
                EventType = recordedEvents[0].EventType,
                Timestamp = currentEditingEvents[actionToReplaceIndex].Timestamp, // Сохраняем timestamp
                X = recordedEvents[0].X,
                Y = recordedEvents[0].Y,
                KeyCode = recordedEvents[0].KeyCode,
                MouseButton = recordedEvents[0].MouseButton,
                IsPressed = recordedEvents[0].IsPressed
            };
            
            currentEditingEvents[actionToReplaceIndex] = newEvent;
            
            // Сохраняем макрос
            if (currentEditingMacroFile != null)
            {
                string filePath = System.IO.Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                AddLog($"🔄 Действие заменено в макросе {currentEditingMacroFile}");
            }
            
            // Обновляем интерфейс
            RefreshMacroDetailsPanel();
            
            // Возвращаемся в настройки если они были открыты
            if (panelSettings != null && !panelSettings.Visible)
            {
                ShowSettingsPanel();
            }
        }
        
        private void ReplaceMouseMovesInMacro()
        {
            if (currentEditingEvents == null || recorder == null || mouseMovesStartIndex < 0) return;
            
            var recordedEvents = recorder.GetRecordedEvents();
            if (recordedEvents == null || recordedEvents.Count == 0)
            {
                AddLog("⚠️ Не записано ни одного движения мыши для замены");
                return;
            }
            
            // Фильтруем только MouseMove события
            var mouseMoves = recordedEvents.Where(e => e.EventType == "MouseMove").ToList();
            if (mouseMoves.Count == 0)
            {
                AddLog("⚠️ В записи нет движений мыши");
                return;
            }
            
            // Удаляем старые движения мыши
            int removed = 0;
            for (int i = mouseMovesStartIndex; i < currentEditingEvents.Count && removed < mouseMovesCountToReplace;)
            {
                if (currentEditingEvents[i].EventType == "MouseMove")
                {
                    currentEditingEvents.RemoveAt(i);
                    removed++;
                }
                else
                {
                    i++;
                }
            }
            
            // Вставляем новые движения мыши на то же место с правильным timestamp
            long baseTimestamp = mouseMovesStartIndex > 0 && mouseMovesStartIndex <= currentEditingEvents.Count 
                ? currentEditingEvents[Math.Max(0, mouseMovesStartIndex - 1)].Timestamp + 1000000 
                : 0;
            
            for (int i = 0; i < mouseMoves.Count; i++)
            {
                var newMove = new InputEvent
                {
                    EventType = "MouseMove",
                    Timestamp = baseTimestamp + (i * 100000), // Небольшие интервалы между движениями
                    X = mouseMoves[i].X,
                    Y = mouseMoves[i].Y
                };
                currentEditingEvents.Insert(mouseMovesStartIndex + i, newMove);
            }
            
            // Сохраняем макрос
            if (currentEditingMacroFile != null)
            {
                string filePath = System.IO.Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                AddLog($"🔄 Движения мыши заменены в макросе {currentEditingMacroFile}");
            }
            
            // Обновляем интерфейс
            RefreshMacroDetailsPanel();
            
            // Возвращаемся в настройки если они были открыты
            if (panelSettings != null && !panelSettings.Visible)
            {
                ShowSettingsPanel();
            }
        }
    }
}

