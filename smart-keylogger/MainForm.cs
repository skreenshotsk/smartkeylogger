// MainForm.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        private System.Windows.Forms.Timer? recordingTimer;
        private System.Windows.Forms.Timer? playbackTimer;
        private System.Windows.Forms.Timer? scheduleTimer; // Таймер для проверки запланированного выполнения
        
        // Планировщик выполнения
        private DateTime? scheduledDateTime = null;
        private string? scheduledMacroFile = null;
        private bool isWaitingForSchedule = false;
        
        // Управление файлами записей
        private List<string> savedMacroFiles = new List<string>(); // Список файлов в директории приложения
        private string currentMacroFile = ""; // Текущий выбранный файл для воспроизведения
        private string macrosDirectory = ""; // Директория для сохранения макросов
        
        // Сохраненные горячие клавиши
        private Dictionary<int, Keys> savedHotkeys = new Dictionary<int, Keys>();
        private Dictionary<int, uint> savedHotkeyModifiers = new Dictionary<int, uint>();
        
        // Клавиши, которые могут использоваться системой
        private HashSet<Keys> systemKeys = new HashSet<Keys>
        {
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8,
            Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Escape, Keys.CapsLock, Keys.NumLock, Keys.Scroll,
            Keys.PrintScreen, Keys.Insert, Keys.Delete,
            Keys.Alt, Keys.Control, Keys.Shift,
            Keys.LWin, Keys.RWin, Keys.Apps
            // Убрали Keys.Tab, чтобы Ctrl+Tab не считался системной клавишей
        };

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
            LoadHotkeysFromFile(); // Загружаем сохраненные горячие клавиши
            RegisterGlobalHotkeys();
            InitializeTimers(); // НОВЫЙ вызов
            
            // Подключаем обработчики для отслеживания клавиш при настройке горячих клавиш
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
        }

        private void InitializeTimers()
        {
            // Таймер записи
            recordingTimer = new System.Windows.Forms.Timer();
            recordingTimer.Interval = 100; // Обновление каждые 100мс
            recordingTimer.Tick += RecordingTimer_Tick!;

            // Таймер воспроизведения
            playbackTimer = new System.Windows.Forms.Timer();
            playbackTimer.Interval = 100;
            playbackTimer.Tick += PlaybackTimer_Tick!;

            // Таймер планировщика (проверка каждую секунду)
            scheduleTimer = new System.Windows.Forms.Timer();
            scheduleTimer.Interval = 1000;
            scheduleTimer.Tick += ScheduleTimer_Tick!;
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            UpdateRecordingTime();
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlaybackTime();
        }

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
                }
                else
                {
                    lblStatus.Text = $"▶ ИДЕТ ВОСПРОИЗВЕДЕНИЕ [{currentText} / {totalText}] (HOME ДЛЯ ПАУЗЫ, ` ДЛЯ ОСТАНОВКИ)";
                }
            }
        }

        private void InitializeComponents()
        {
            recorder = new MacroRecorder();
            player = new MacroPlayer();
            playbackCancellation = new CancellationTokenSource();
            
            // Инициализируем директорию для макросов
            macrosDirectory = Path.Combine(Application.StartupPath, "Macros");
            if (!Directory.Exists(macrosDirectory))
            {
                Directory.CreateDirectory(macrosDirectory);
                AddLog($"📁 Создана директория для макросов: {macrosDirectory}");
            }
            
            // Загружаем список существующих файлов
            LoadMacroFilesList();
            
            // Передаем ссылку на форму для логирования
            recorder.SetLogCallback(AddLog);
            player.SetLogCallback(AddLog);

            // Инициализируем ComboBox
            UpdateMacroFilesComboBox();

            UpdateUI();
        }
        
        private void LoadMacroFilesList()
        {
            try
            {
                savedMacroFiles.Clear();
                if (Directory.Exists(macrosDirectory))
                {
                    var files = Directory.GetFiles(macrosDirectory, "*.json");
                    foreach (var file in files)
                    {
                        savedMacroFiles.Add(Path.GetFileName(file));
                    }
                    savedMacroFiles.Sort();
                    if (savedMacroFiles.Count > 0)
                    {
                        AddLog($"📋 Загружено файлов макросов: {savedMacroFiles.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при загрузке списка файлов: {ex.Message}");
            }
        }
        
        private string GenerateMacroFileName()
        {
            int counter = 1;
            string baseName = "macro";
            string fileName;
            
            do
            {
                fileName = $"{baseName}_{counter}.json";
                counter++;
            } while (savedMacroFiles.Contains(fileName) || File.Exists(Path.Combine(macrosDirectory, fileName)));
            
            return fileName;
        }
        
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

        private void RegisterGlobalHotkeys()
    {
        UnregisterGlobalHotkeys();
        
            // Небольшая задержка после отмены всех горячих клавиш
            System.Threading.Thread.Sleep(50);
            
            // Используем сохраненные горячие клавиши или значения по умолчанию
            RegisterSingleHotkey(HOTKEY_START_RECORD, 
                savedHotkeys.ContainsKey(HOTKEY_START_RECORD) ? savedHotkeys[HOTKEY_START_RECORD] : Keys.F9,
                savedHotkeyModifiers.ContainsKey(HOTKEY_START_RECORD) ? savedHotkeyModifiers[HOTKEY_START_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_STOP_RECORD,
                savedHotkeys.ContainsKey(HOTKEY_STOP_RECORD) ? savedHotkeys[HOTKEY_STOP_RECORD] : Keys.F10,
                savedHotkeyModifiers.ContainsKey(HOTKEY_STOP_RECORD) ? savedHotkeyModifiers[HOTKEY_STOP_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PLAY,
                savedHotkeys.ContainsKey(HOTKEY_PLAY) ? savedHotkeys[HOTKEY_PLAY] : Keys.F11,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PLAY) ? savedHotkeyModifiers[HOTKEY_PLAY] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PLAY_LOOP,
                savedHotkeys.ContainsKey(HOTKEY_PLAY_LOOP) ? savedHotkeys[HOTKEY_PLAY_LOOP] : Keys.F12,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PLAY_LOOP) ? savedHotkeyModifiers[HOTKEY_PLAY_LOOP] : 0x0002); // MOD_CONTROL
            
            RegisterSingleHotkey(HOTKEY_STOP_PLAY,
                savedHotkeys.ContainsKey(HOTKEY_STOP_PLAY) ? savedHotkeys[HOTKEY_STOP_PLAY] : (Keys)0xC0,
                savedHotkeyModifiers.ContainsKey(HOTKEY_STOP_PLAY) ? savedHotkeyModifiers[HOTKEY_STOP_PLAY] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PAUSE_RECORD,
                savedHotkeys.ContainsKey(HOTKEY_PAUSE_RECORD) ? savedHotkeys[HOTKEY_PAUSE_RECORD] : Keys.Pause,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PAUSE_RECORD) ? savedHotkeyModifiers[HOTKEY_PAUSE_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PAUSE_PLAYBACK,
                savedHotkeys.ContainsKey(HOTKEY_PAUSE_PLAYBACK) ? savedHotkeys[HOTKEY_PAUSE_PLAYBACK] : Keys.Home,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PAUSE_PLAYBACK) ? savedHotkeyModifiers[HOTKEY_PAUSE_PLAYBACK] : MOD_NONE);
        }
        
        private void RegisterSingleHotkey(int hotkeyId, Keys key, uint modifier)
        {
            // Убеждаемся, что старая регистрация отменена (несколько раз для надежности)
            for (int i = 0; i < 3; i++)
            {
                UnregisterHotKey(this.Handle, hotkeyId);
            }
            
            // Небольшая задержка для гарантии, что система обработала отмену
            System.Threading.Thread.Sleep(50);
            
            // Используем KeysToVirtualKey для правильного преобразования
            Console.WriteLine($"DEBUG RegisterSingleHotkey: hotkeyId={hotkeyId}, key={key} (0x{(int)key:X} = {(int)key} decimal), modifier={modifier}");
            uint vk = KeysToVirtualKey(key);
            Console.WriteLine($"DEBUG RegisterSingleHotkey: после KeysToVirtualKey vk={vk:X} ({vk} decimal)");
            
            bool result = RegisterHotKey(this.Handle, hotkeyId, modifier, vk);
            Console.WriteLine($"Hotkey {hotkeyId} ({key}, mod={modifier}, vk={vk:X}) регистрация: {result}");
        if (!result)
        {
                int error = Marshal.GetLastWin32Error();
                AddLog($"⚠️ Не удалось зарегистрировать горячую клавишу {GetHotkeyActionName(hotkeyId)}: {key} (код ошибки: {error}, vk={vk:X}, mod={modifier})");
            }
            else
            {
                AddLog($"✅ Горячая клавиша {GetHotkeyActionName(hotkeyId)} успешно зарегистрирована: {key} с модификаторами {modifier} (vk={vk:X})");
            }
        }
        
        private uint KeysToVirtualKey(Keys key)
        {
            // Убираем модификаторы, оставляя только саму клавишу (нижние 16 бит)
            uint vk = (uint)((int)key & 0xFFFF);
            
            // Если vk == 0 после удаления модификаторов, значит были только модификаторы
            // Пробуем убрать модификаторы через GetKeyWithoutModifiers
            if (vk == 0)
            {
                Keys cleanKey = GetKeyWithoutModifiers(key);
                vk = (uint)((int)cleanKey & 0xFFFF);
            }
            
            // Специальные клавиши с явным указанием VK кода
            // Проверяем исходный key (нижние 16 бит)
            int keyCode = (int)key & 0xFFFF;
            
            if (keyCode == 0xC0 || key == Keys.Oemtilde) 
                vk = VK_BACKQUOTE; // 0xC0
            else if (keyCode == 0x13 || key == Keys.Pause) 
                vk = VK_PAUSE; // 0x13
            else if (keyCode == 0x24 || key == Keys.Home) 
                vk = VK_HOME; // 0x24
            else if (keyCode == 0x23 || key == Keys.End) 
                vk = 0x23; // VK_END
            else if (keyCode == 0x21 || key == Keys.PageUp || key == Keys.Prior) 
                vk = 0x21; // VK_PRIOR
            else if (keyCode == 0x22 || key == Keys.PageDown || key == Keys.Next) 
                vk = 0x22; // VK_NEXT
            else if (keyCode == 0x2D || key == Keys.Insert) 
                vk = 0x2D; // VK_INSERT
            else if (keyCode == 0x2E || key == Keys.Delete) 
                vk = 0x2E; // VK_DELETE
            else if (keyCode == 0x03 || key == Keys.Cancel)
                vk = 0x03; // VK_CANCEL (Ctrl+Break)
            // Для F-клавиш (F1-F12: 0x70-0x7B) и остальных клавиш vk уже установлен выше через нижние 16 бит
            
            // Если получили пробел (0x20) для F-клавиши, это ошибка - возвращаем исходный код
            if (vk == 0x20 && (keyCode >= 0x70 && keyCode <= 0x7B))
            {
                Console.WriteLine($"ERROR: KeysToVirtualKey получил пробел для F-клавиши! key={key} (0x{(int)key:X}), keyCode={keyCode:X}, используем keyCode как vk");
                vk = (uint)keyCode;
            }
            
            Console.WriteLine($"DEBUG KeysToVirtualKey: key={key} (0x{(int)key:X}), keyCode={keyCode:X}, vk={vk:X} ({vk} decimal)");
            return vk;
        }

        private void UnregisterGlobalHotkeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_START_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_STOP_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_PLAY);
                UnregisterHotKey(this.Handle, HOTKEY_PLAY_LOOP);
                UnregisterHotKey(this.Handle, HOTKEY_STOP_PLAY);
                UnregisterHotKey(this.Handle, HOTKEY_PAUSE_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_PAUSE_PLAYBACK);
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при отмене регистрации горячих клавиш: {ex.Message}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                // Игнорируем горячие клавиши если открыто меню настроек И мы не ожидаем настройку новой горячей клавиши
                if (panelSettings != null && panelSettings.Visible && (waitingForHotkeyButton == null || !waitingForHotkeyId.HasValue))
                {
                    base.WndProc(ref m);
                    return;
                }
                
                switch (m.WParam.ToInt32())
                {
                    case HOTKEY_START_RECORD:
                        BeginInvoke(new Action(StartRecording));
                        break;
                    case HOTKEY_STOP_RECORD:
                        if (isAppendingToMacro || isReplacingAction || isReplacingMouseMoves)
                        {
                            BeginInvoke(new Action(() => {
                                StopRecording(); // Это сохранит запись в макрос или заменит действие
                            }));
                        }
                        else
                        {
                        BeginInvoke(new Action(StopRecording));
                        }
                        break;
                    case HOTKEY_STOP_PLAY:
                        if (isAppendingToMacro || isReplacingAction || isReplacingMouseMoves)
                        {
                            // Отменяем дозапись/замену
                            BeginInvoke(new Action(() => {
                                CancelAppendingToMacro();
                            }));
                        }
                        else
                        {
                            BeginInvoke(new Action(StopPlayback));
                        }
                        break;
                    case HOTKEY_PLAY:
                        BeginInvoke(new Action(PlayMacro));
                        break;
                    case HOTKEY_PLAY_LOOP:
                        BeginInvoke(new Action(PlayMacroLoop));
                        break;
                    case HOTKEY_PAUSE_RECORD:
                        BeginInvoke(new Action(TogglePauseRecording));
                        break;
                    case HOTKEY_PAUSE_PLAYBACK:
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(TogglePausePlayback));
                        }
                        else
                        {
                            TogglePausePlayback();
                        }
                        break;
                }
                return;
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ВАЖНО: Обрабатываем горячую клавишу ТОЛЬКО если панель настроек видима И мы ожидаем нажатие клавиши
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                return false; // НЕ обрабатываем здесь, пусть KeyDown/KeyUp обработают
            }
            
            // Блокируем обработку клавиш только если открыто меню настроек И мы НЕ ожидаем настройку
            if (panelSettings != null && panelSettings.Visible)
            {
                return true; // Блокируем все клавиши в меню настроек (но не когда настраиваем горячую клавишу)
            }
            
            // Обработка прямых нажатий клавиш (не глобальные горячие клавиши, они обрабатываются в WndProc)
            if (isPlaying && (int)keyData == 0xC0)
            {
                StopPlayback();
                return true;
            }
            
            // F9 обрабатывается через глобальные горячие клавиши в WndProc, здесь не обрабатываем
            // чтобы избежать дублирования и конфликтов
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
            // ИЗМЕНЕНИЕ: Ctrl+F12 для цикла
            else if (keyData == (Keys.F12 | Keys.Control) && !isRecording && !isPlaying)
            {
                PlayMacroLoop();
                return true;
            }
            // Оставляем простой F12 для обратной совместимости (если окно активно)
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
            recordingTimer?.Start(); // НОВОЕ: запускаем таймер
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
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
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
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                AddLog($"🔄 Движения мыши заменены в макросе {currentEditingMacroFile}");
            }
            
            // Обновляем интерфейс
            RefreshMacroDetailsPanel();
        }
        
        private void RefreshMacroDetailsPanel()
        {
            // Всегда обновляем, даже если настройки не видны
            if (currentEditingMacroFile != null && currentMacroDetailsPanel != null)
            {
                // Обновляем панель напрямую через сохраненную ссылку
                ShowMacroDetails(currentEditingMacroFile, currentMacroDetailsPanel);
            }
            else if (panelSettings != null && panelSettings.Visible && currentEditingMacroFile != null)
            {
                Panel? rightPanel = null;
                foreach (Control ctrl in settingsContentPanel?.Controls ?? new ControlCollection(this))
                {
                    if (ctrl is Panel p && p.Dock == DockStyle.Fill && p.BackColor == System.Drawing.Color.White)
                    {
                        rightPanel = p;
                        break;
                    }
                }
                
                if (rightPanel != null)
                {
                    ShowMacroDetails(currentEditingMacroFile, rightPanel);
                }
                else
                {
                    ShowMacrosSettings();
                }
            }
        }
        
        private void CancelAppendingToMacro()
        {
            if ((!isAppendingToMacro && !isReplacingAction && !isReplacingMouseMoves) || recorder == null) return;
            
            isAppendingToMacro = false;
            isReplacingAction = false;
            isReplacingMouseMoves = false;
            actionToReplaceIndex = -1;
            mouseMovesStartIndex = -1;
            mouseMovesCountToReplace = 0;
            isRecording = false;
            recordingTimer?.Stop();
            recorder.StopRecording();
            
            AddLog("❌ Запись отменена");
            UpdateUI();
            
            // Возвращаемся в настройки если они были открыты
            if (panelSettings != null)
            {
                ShowSettingsPanel();
            }
        }
        
        private void AppendRecordedEventsToMacro()
        {
            if (currentEditingEvents == null || recorder == null) return;
            
            // Получаем записанные события из recorder
            var recordedEvents = recorder.GetRecordedEvents();
            if (recordedEvents == null || recordedEvents.Count == 0)
            {
                AddLog("⚠️ Не записано ни одного действия для добавления");
                isAppendingToMacro = false;
                return;
            }
            
            // Вычисляем максимальный timestamp в текущих событиях
            long maxTimestamp = currentEditingEvents.Count > 0 
                ? currentEditingEvents.Max(e => e.Timestamp) 
                : 0;
            
            // Добавляем новые события с корректным timestamp
            foreach (var evt in recordedEvents)
            {
                var newEvent = new InputEvent
                {
                    EventType = evt.EventType,
                    Timestamp = maxTimestamp + evt.Timestamp + 10000000, // +1 секунда после последнего
                    X = evt.X,
                    Y = evt.Y,
                    KeyCode = evt.KeyCode,
                    MouseButton = evt.MouseButton,
                    IsPressed = evt.IsPressed
                };
                currentEditingEvents.Add(newEvent);
            }
            
            // Сохраняем обновленный макрос
            if (currentEditingMacroFile != null)
            {
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                AddLog($"➕ Добавлено {recordedEvents.Count} действий в макрос {currentEditingMacroFile}");
            }
            
            isAppendingToMacro = false;
            
            // Возвращаемся в настройки и показываем обновленный макрос
            if (panelSettings != null && !panelSettings.Visible)
            {
                ShowSettingsPanel();
            }
            
            // Переключаемся на вкладку макросов и обновляем панель
            if (btnTabMacros != null && settingsContentPanel != null)
            {
                SwitchTab("macros");
                ShowMacrosSettings();
                
                // Убеждаемся, что выбран правильный макрос
                if (currentEditingMacroFile != null)
                {
                    // Находим правую панель и показываем детали макроса
                    Panel? rightPanel = null;
                    foreach (Control ctrl in settingsContentPanel.Controls)
                    {
                        if (ctrl is Panel p && p.Dock == DockStyle.Fill && p.BackColor == System.Drawing.Color.White)
                        {
                            rightPanel = p;
                            break;
                        }
                    }
                    
                    if (rightPanel != null)
                    {
                        ShowMacroDetails(currentEditingMacroFile, rightPanel);
                    }
                }
            }
            else
            {
                RefreshMacroDetailsPanel();
            }
        }
        
        private void SaveRecordingDialog()
        {
            if (recorder == null) return;
            
            // Создаем диалог выбора места сохранения
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                dialog.Title = "Сохранить запись макроса";
                dialog.FileName = GenerateMacroFileName();
                
                // Предлагаем два варианта через кнопки
                var result = MessageBox.Show(
                    "Выберите место сохранения:\n\n" +
                    "ДА - Сохранить в директорию приложения\n" +
                    "НЕТ - Выбрать место на диске/флешке\n" +
                    "ОТМЕНА - Не сохранять",
                    "Сохранение записи",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                
                string savedPath = "";
                
                if (result == DialogResult.Yes)
                {
                    // Запрашиваем имя файла у пользователя через простой диалог
                    string defaultName = Path.GetFileNameWithoutExtension(GenerateMacroFileName());
                    string? userFileName = ShowInputDialog("Введите имя файла (без расширения):", "Имя файла", defaultName);
                    
                    if (string.IsNullOrWhiteSpace(userFileName))
                    {
                        // Если пользователь не ввел имя, используем автоматическое
                        userFileName = defaultName;
                    }
                    else
                    {
                        // Очищаем имя от недопустимых символов
                        char[] invalidChars = Path.GetInvalidFileNameChars();
                        foreach (char c in invalidChars)
                        {
                            userFileName = userFileName.Replace(c.ToString(), "");
                        }
                        userFileName = userFileName.Trim();
                        if (string.IsNullOrWhiteSpace(userFileName))
                        {
                            userFileName = defaultName;
                        }
                    }
                    
                    // Проверяем, не существует ли уже файл с таким именем
                    string fileName = $"{userFileName}.json";
                    int counter = 1;
                    while (savedMacroFiles.Contains(fileName) || File.Exists(Path.Combine(macrosDirectory, fileName)))
                    {
                        fileName = $"{userFileName}_{counter}.json";
                        counter++;
                    }
                    
                    savedPath = Path.Combine(macrosDirectory, fileName);
                    
                    try
                    {
                        recorder.SaveToFile(savedPath);
                        savedMacroFiles.Add(fileName);
                        savedMacroFiles.Sort();
                        currentMacroFile = fileName;
                        AddLog($"💾 Запись сохранена в директорию приложения: {fileName}");
                        AddLog($"📁 Полный путь: {savedPath}");
                        
                        // Обновляем ComboBox если он существует
                        UpdateMacroFilesComboBox();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"❌ Ошибка при сохранении в директорию приложения: {ex.Message}");
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (result == DialogResult.No)
                {
                    // Пользователь выбирает место на диске
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    dialog.FileName = Path.GetFileNameWithoutExtension(GenerateMacroFileName());
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        savedPath = dialog.FileName;
                        
                        try
                        {
                            recorder.SaveToFile(savedPath);
                            string fileName = Path.GetFileName(savedPath);
                            AddLog($"💾 Запись сохранена на диск: {fileName}");
                            AddLog($"📁 Полный путь: {savedPath}");
                            
                            // Если файл сохранен в директории приложения, добавляем в список
                            if (savedPath.StartsWith(macrosDirectory, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!savedMacroFiles.Contains(fileName))
                                {
                                    savedMacroFiles.Add(fileName);
                                    savedMacroFiles.Sort();
                                    UpdateMacroFilesComboBox();
                                }
                                currentMacroFile = fileName;
                            }
                            else
                            {
                                currentMacroFile = savedPath; // Сохраняем полный путь для внешних файлов
                            }
                        }
                        catch (Exception ex)
                        {
                            AddLog($"❌ Ошибка при сохранении на диск: {ex.Message}");
                            MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        AddLog("⚠️ Сохранение отменено пользователем");
                    }
                }
                else
                {
                    AddLog("⚠️ Сохранение отменено пользователем");
                }
            }
        }
        
        private void UpdateMacroFilesComboBox()
        {
            if (cmbMacroFiles != null)
            {
                cmbMacroFiles.Items.Clear();
                cmbMacroFiles.Items.AddRange(savedMacroFiles.ToArray());
                
                if (!string.IsNullOrEmpty(currentMacroFile))
                {
                    if (savedMacroFiles.Contains(currentMacroFile))
                    {
                        cmbMacroFiles.SelectedItem = currentMacroFile;
                    }
                    else if (File.Exists(currentMacroFile))
                    {
                        // Внешний файл - добавляем в список с пометкой
                        cmbMacroFiles.Items.Add($"[Внешний] {Path.GetFileName(currentMacroFile)}");
                        cmbMacroFiles.SelectedIndex = cmbMacroFiles.Items.Count - 1;
                    }
                }
            }
        }
        
        private string GetSelectedMacroFile()
        {
            if (cmbMacroFiles != null && cmbMacroFiles.SelectedItem != null)
            {
                string? selected = cmbMacroFiles.SelectedItem.ToString();
                if (selected == null) return "";
                
                // Если это файл из директории приложения
                if (savedMacroFiles.Contains(selected))
                {
                    return Path.Combine(macrosDirectory, selected);
                }
                
                // Если это внешний файл
                if (selected.StartsWith("[Внешний] "))
                {
                    return currentMacroFile; // Возвращаем сохраненный полный путь
                }
            }
            
            // Если ничего не выбрано, возвращаем текущий файл
            if (!string.IsNullOrEmpty(currentMacroFile))
            {
                if (File.Exists(currentMacroFile))
                {
                    return currentMacroFile;
                }
            }
            
            return "";
        }
        
        private void LoadMacroFromFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                dialog.Title = "Загрузить файл макроса";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = dialog.FileName;
                    
                    try
                    {
                        // Проверяем валидность файла
                        if (!File.Exists(filePath))
                        {
                            AddLog($"❌ Файл не существует: {filePath}");
                            MessageBox.Show("Выбранный файл не существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Пытаемся загрузить файл для проверки используя основной recorder
                        if (recorder == null)
                        {
                            AddLog($"❌ Recorder не инициализирован");
                            MessageBox.Show("Ошибка: Recorder не инициализирован.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        var events = recorder.LoadFromFile(filePath);
                        
                        if (events == null || events.Count == 0)
                        {
                            AddLog($"❌ Файл не содержит записей или поврежден: {Path.GetFileName(filePath)}");
                            MessageBox.Show("Выбранный файл не содержит записей макроса или поврежден.", 
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        // Файл валиден
                        string fileName = Path.GetFileName(filePath);
                        
                        // Если файл в директории приложения, добавляем в список
                        if (filePath.StartsWith(macrosDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!savedMacroFiles.Contains(fileName))
                            {
                                savedMacroFiles.Add(fileName);
                                savedMacroFiles.Sort();
                            }
                            currentMacroFile = fileName;
                        }
                        else
                        {
                            // Внешний файл
                            currentMacroFile = filePath;
                        }
                        
                        AddLog($"✅ Файл успешно загружен: {fileName}");
                        AddLog($"📁 Полный путь: {filePath}");
                        if (events != null)
                        {
                            AddLog($"📊 Событий в файле: {events.Count}");
                        }
                        AddLog($"▶ Файл установлен для воспроизведения");
                        
                        // Обновляем ComboBox
                        UpdateMacroFilesComboBox();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"❌ Ошибка при загрузке файла: {ex.Message}");
                        MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", 
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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
                UpdateButtonStates();
                
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // События есть - начинаем воспроизведение
                isPlaying = true;
            isWaitingForSchedule = false; // Сбрасываем режим ожидания, начинается воспроизведение
                playbackCancellation = new CancellationTokenSource();
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
                UpdateButtonStates();
                
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // События есть - начинаем воспроизведение
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
                isWaitingForSchedule = false; // Сбрасываем режим ожидания, начинается воспроизведение
                playbackCancellation = new CancellationTokenSource();
                AddLog("🔄 Запущено циклическое воспроизведение (Ctrl+F12)");
                playbackTimer?.Start();
                UpdateUI();
                
                var keyboardLayout = recorder.GetRecordedKeyboardLayout();
                var durationTicks = recorder.GetRecordedDurationTicks();
                
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
            // Если ожидается запланированное выполнение, отменяем его
            if (isWaitingForSchedule && !isPlaying)
            {
                scheduleTimer?.Stop();
                isWaitingForSchedule = false;
                scheduledDateTime = null;
                scheduledMacroFile = null;
                AddLog("❌ Запланированное воспроизведение отменено");
                UpdateUI();
                UpdateButtonStates();
                return;
            }

            if (!isPlaying || player == null) return;

            playbackCancellation?.Cancel();
            player.Stop();
            isPlaying = false;
            AddLog("⏹ Воспроизведение остановлено (`)");
            UpdateUI();
            
            Console.WriteLine("Воспроизведение остановлено по запросу пользователя");
        }

        private void UpdateUI()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUI));
                return;
            }

            // Обновляем состояния кнопок
            UpdateButtonStates();

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
            
            // Запись
            btnStartRecord.Enabled = !isRecording && !isPlaying && !isWaitingMode;
            btnStopRecord.Enabled = isRecording && !isPlaying;
            btnPauseRecord.Enabled = isRecording && !isPlaying;

            // Воспроизведение
            btnPlay.Enabled = !isRecording && !isPlaying && !isWaitingMode;
            btnPlayLoop.Enabled = !isRecording && !isPlaying && !isWaitingMode;
            btnStopPlayback.Enabled = isPlaying && !isRecording || isWaitingMode;
            if (btnSchedulePlayback != null)
            {
                btnSchedulePlayback.Enabled = !isRecording && !isPlaying && !isWaitingMode;
            }
            // Кнопка паузы доступна только после начала реального воспроизведения
            btnPausePlayback.Enabled = isPlaying && !isRecording && (player?.PlaybackStarted ?? false);

            // Обновляем внешний вид кнопок (isWaitingMode уже определен выше)
            UpdateButtonAppearance(btnStartRecord, !isRecording && !isPlaying && !isWaitingMode);
            UpdateButtonAppearance(btnStopRecord, isRecording && !isPlaying);
            UpdateButtonAppearance(btnPauseRecord, isRecording && !isPlaying);
            UpdateButtonAppearance(btnPlay, !isRecording && !isPlaying && !isWaitingMode);
            UpdateButtonAppearance(btnPlayLoop, !isRecording && !isPlaying && !isWaitingMode);
            UpdateButtonAppearance(btnStopPlayback, isPlaying && !isRecording || isWaitingMode);
            if (btnSchedulePlayback != null)
            {
                UpdateButtonAppearance(btnSchedulePlayback, !isRecording && !isPlaying && !isWaitingMode);
            }
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

        private void btnSchedulePlayback_Click(object sender, EventArgs e)
        {
            // Проверяем, что файл выбран
            string fileToSchedule = GetSelectedMacroFile();
            if (string.IsNullOrEmpty(fileToSchedule))
            {
                AddLog("⚠️ Файл для воспроизведения не выбран");
                MessageBox.Show("Пожалуйста, выберите файл для воспроизведения или загрузите файл из проводника.", 
                    "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, нет ли уже запланированного выполнения
            if (isWaitingForSchedule && scheduledDateTime.HasValue)
            {
                MessageBox.Show($"Уже запланировано выполнение на {scheduledDateTime.Value:dd.MM.yyyy HH:mm}. " +
                    "Отмените текущее расписание перед созданием нового.", 
                    "Расписание уже существует", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Открываем форму выбора даты и времени
            ScheduleForm scheduleForm = new ScheduleForm();
            if (scheduleForm.ShowDialog() == DialogResult.OK && scheduleForm.Confirmed)
            {
                DateTime selectedDateTime = scheduleForm.SelectedDateTime;

                // Проверяем конфликты
                // (в будущем можно добавить проверку нескольких расписаний)
                
                // Сохраняем запланированное выполнение
                scheduledDateTime = selectedDateTime;
                scheduledMacroFile = fileToSchedule;
                isWaitingForSchedule = true;

                // Запускаем таймер планировщика
                scheduleTimer?.Start();

                // Обновляем UI
                UpdateUI();
                UpdateButtonStates();

                string dateStr = selectedDateTime.ToString("dd.MM.yyyy");
                string timeStr = selectedDateTime.ToString("HH:mm");
                AddLog($"⏰ Запланировано воспроизведение {Path.GetFileName(fileToSchedule)} на {dateStr} {timeStr}");
            }
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
            
            recordingTimer?.Stop();
            playbackTimer?.Stop();
            UnregisterGlobalHotkeys();
            playbackCancellation?.Dispose();
            recordingTimer?.Dispose();
            playbackTimer?.Dispose();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettingsPanel();
        }
        
        private void ShowSettingsPanel()
        {
            if (panelSettings == null) 
            {
                AddLog("❌ Ошибка: panelSettings не инициализирован");
                return;
            }
            
            // Инициализируем панель настроек при первом открытии
            if (panelSettings.Controls.Count == 0)
            {
                InitializeSettingsPanel();
            }
            
            // Убеждаемся, что панель настроек добавлена в форму
            if (!this.Controls.Contains(panelSettings))
            {
                this.Controls.Add(panelSettings);
                panelSettings.BringToFront();
            }
            
            // Переключаем видимость панелей
            panelMain.Visible = false;
            panelLogs.Visible = false; // Скрываем логи при открытии настроек
            panelSettings.Visible = true;
            panelSettings.BringToFront();
            
            AddLog("⚙️ Открыто меню настроек");
        }
        
        private void HideSettingsPanel()
        {
            if (panelSettings == null) return;
            
            panelMain.Visible = true;
            panelLogs.Visible = true; // Показываем логи обратно
            panelSettings.Visible = false;
            AddLog("◀ Возврат на главный экран");
        }
        
        private Button? btnTabHotkeys = null;
        private Button? btnTabMacros = null;
        private Panel? settingsContentPanel = null;
        
        private void InitializeSettingsPanel()
        {
            if (panelSettings == null) 
            {
                AddLog("❌ Ошибка: panelSettings равен null при инициализации");
                return;
            }
            
            panelSettings.Controls.Clear();
            panelSettings.BackColor = System.Drawing.Color.White;
            panelSettings.Dock = DockStyle.Fill;
            
            // КОНТЕЙНЕР ДЛЯ СОДЕРЖИМОГО - добавляем ПЕРВЫМ (будет заполнять все пространство, но под header и tabs)
            settingsContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                AutoScroll = true
            };
            
            // ПАНЕЛЬ С КНОПКОЙ "НАЗАД" И ВКЛАДКАМИ - добавляем ВТОРЫМ (будет под header)
            Panel tabsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(30, 10, 30, 10)
            };
            
            // Кнопка "Назад"
            Button btnBack = new Button
            {
                Text = "◀ НАЗАД",
                Size = new System.Drawing.Size(120, 40),
                Location = new System.Drawing.Point(10, 10),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(98, 98, 98),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnBack.Click += (s, e) => HideSettingsPanel();
            tabsPanel.Controls.Add(btnBack);
            
            // Вкладки - справа от кнопки "Назад" (ближе к кнопке)
            btnTabHotkeys = new Button
            {
                Text = "ГОРЯЧИЕ КЛАВИШИ",
                Size = new System.Drawing.Size(280, 40),
                Location = new System.Drawing.Point(140, 10),
                Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(33, 150, 243),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Tag = "hotkeys"
            };
            
            btnTabMacros = new Button
            {
                Text = "РЕДАКТИРОВАНИЕ МАКРОСОВ",
                Size = new System.Drawing.Size(280, 40),
                Location = new System.Drawing.Point(430, 10),
                Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(189, 189, 189),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Tag = "macros"
            };
            
            btnTabHotkeys.Click += (s, e) => {
                SwitchTab("hotkeys");
                ShowHotkeysSettings();
            };
            btnTabMacros.Click += (s, e) => {
                SwitchTab("macros");
                ShowMacrosSettings();
            };
            
            tabsPanel.Controls.Add(btnTabHotkeys);
            tabsPanel.Controls.Add(btnTabMacros);
            
            // HEADER - добавляем ПОСЛЕДНИМ (будет в самом верху, так как Dock.Top добавляется снизу вверх)
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = System.Drawing.Color.FromArgb(80, 80, 80)
            };
            
            Label lblSettingsTitle = new Label
            {
                Text = "⚙️ НАСТРОЙКИ",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblSettingsTitle);
            
            // Важно: добавляем в обратном порядке из-за Dock.Top
            // Сначала добавляем settingsContentPanel (Fill), потом tabsPanel (Top), потом headerPanel (Top)
            panelSettings.Controls.Add(settingsContentPanel);
            panelSettings.Controls.Add(tabsPanel);
            panelSettings.Controls.Add(headerPanel); // Будет вверху, так как добавлен последним
            
            // Показываем настройки горячих клавиш по умолчанию
            SwitchTab("hotkeys");
            ShowHotkeysSettings();
        }
        
        private void SwitchTab(string activeTab)
        {
            if (btnTabHotkeys == null || btnTabMacros == null) return;
            
            if (activeTab == "hotkeys")
            {
                btnTabHotkeys.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                btnTabMacros.BackColor = System.Drawing.Color.FromArgb(189, 189, 189);
            }
            else if (activeTab == "macros")
            {
                btnTabHotkeys.BackColor = System.Drawing.Color.FromArgb(189, 189, 189);
                btnTabMacros.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
            }
        }
        
        private void ShowHotkeysSettings()
        {
            if (settingsContentPanel == null) 
            {
                AddLog("❌ Ошибка: settingsContentPanel не инициализирован");
                return;
            }
            
            settingsContentPanel.Controls.Clear();
            settingsContentPanel.Padding = new Padding(30, 10, 30, 30); // Небольшой верхний отступ
            
            // Внутренняя панель для прокрутки - используем Dock.Fill чтобы правильно учитывать padding родителя
            Panel scrollablePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                AutoScroll = true
            };
            
            // Заголовок секции
            Label lblTitle = new Label
            {
                Text = "Настройка горячих клавиш",
                Font = new System.Drawing.Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, 0),
                Margin = new Padding(0, 0, 0, 0)
            };
            scrollablePanel.Controls.Add(lblTitle);
            
            // Список горячих клавиш для настройки
            int yPos = 50;
            var hotkeyActions = new[]
            {
                new { Name = "Начать запись", DefaultKey = Keys.F9, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_START_RECORD },
                new { Name = "Сохранить запись", DefaultKey = Keys.F10, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_STOP_RECORD },
                new { Name = "Воспроизвести", DefaultKey = Keys.F11, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PLAY },
                new { Name = "Воспроизвести в цикле", DefaultKey = Keys.F12, DefaultMod = (uint)0x0002, HotkeyId = HOTKEY_PLAY_LOOP }, // MOD_CONTROL
                new { Name = "Пауза записи", DefaultKey = Keys.Pause, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PAUSE_RECORD },
                new { Name = "Пауза воспроизведения", DefaultKey = Keys.Home, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PAUSE_PLAYBACK },
                new { Name = "Остановить воспроизведение", DefaultKey = (Keys)0xC0, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_STOP_PLAY } // Backquote
            };
            
            // Загружаем сохраненные горячие клавиши или используем значения по умолчанию
            foreach (var action in hotkeyActions)
            {
                if (!savedHotkeys.ContainsKey(action.HotkeyId))
                {
                    savedHotkeys[action.HotkeyId] = action.DefaultKey;
                    savedHotkeyModifiers[action.HotkeyId] = action.DefaultMod;
                }
            }
            
            foreach (var action in hotkeyActions)
            {
                // Получаем сохраненное значение или значение по умолчанию
                Keys currentKey = savedHotkeys.ContainsKey(action.HotkeyId) ? savedHotkeys[action.HotkeyId] : action.DefaultKey;
                uint currentMod = savedHotkeyModifiers.ContainsKey(action.HotkeyId) ? savedHotkeyModifiers[action.HotkeyId] : action.DefaultMod;
                string keyText = FormatKeysToString(currentKey, currentMod);
                
                // Панель для одной строки
                Panel rowPanel = new Panel
                {
                    Height = 50,
                    Width = scrollablePanel.ClientSize.Width,
                    Location = new System.Drawing.Point(0, yPos),
                    BackColor = System.Drawing.Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                
                Label lblAction = new Label
                {
                    Text = action.Name,
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    AutoSize = true,
                    Location = new System.Drawing.Point(0, 12)
                };
                rowPanel.Controls.Add(lblAction);
                
                // Кликабельное поле для горячей клавиши
                Button btnHotkey = new Button
                {
                    Text = keyText,
                    Size = new System.Drawing.Size(220, 40),
                    Location = new System.Drawing.Point(280, 5),
                    Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                    BackColor = System.Drawing.Color.White,
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { 
                        BorderSize = 1, 
                        BorderColor = System.Drawing.Color.FromArgb(189, 189, 189)
                    },
                    Cursor = Cursors.Hand,
                    Tag = new { HotkeyId = action.HotkeyId, Key = currentKey, Mod = currentMod }
                };
                btnHotkey.Click += (s, e) => ChangeHotkey(btnHotkey, action.HotkeyId);
                btnHotkey.MouseEnter += (s, e) => {
                    btnHotkey.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(33, 150, 243);
                };
                btnHotkey.MouseLeave += (s, e) => {
                    if (waitingForHotkeyButton != btnHotkey)
                        btnHotkey.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(189, 189, 189);
                };
                rowPanel.Controls.Add(btnHotkey);
                
                scrollablePanel.Controls.Add(rowPanel);
                yPos += 55;
            }
            
            // Кнопка "Вернуть настройки по умолчанию"
            Button btnReset = new Button
            {
                Text = "Вернуться к изначальным настройкам",
                Size = new System.Drawing.Size(320, 45),
                Location = new System.Drawing.Point(0, yPos + 30),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => ResetHotkeysToDefault();
            scrollablePanel.Controls.Add(btnReset);
            
            // Предупреждение
            Label lblWarning = new Label
            {
                Text = "⚠️ Предупреждение: некоторые клавиши (F1-F12 и др.) могут использоваться системой",
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.FromArgb(244, 67, 54),
                AutoSize = true,
                Location = new System.Drawing.Point(0, yPos + 90),
                MaximumSize = new System.Drawing.Size(scrollablePanel.ClientSize.Width - 20, 0)
            };
            scrollablePanel.Controls.Add(lblWarning);
            
            // Внутренний контейнер с фиксированной высотой для правильной прокрутки
            Panel contentContainer = new Panel
            {
                Height = yPos + 180,
                BackColor = System.Drawing.Color.Transparent,
                AutoSize = false
            };
            
            // Перемещаем все элементы из scrollablePanel в contentContainer
            var controlsToMove = scrollablePanel.Controls.Cast<Control>().ToList();
            scrollablePanel.Controls.Clear();
            
            foreach (var ctrl in controlsToMove)
            {
                contentContainer.Controls.Add(ctrl);
            }
            
            scrollablePanel.Controls.Add(contentContainer);
            
            // Обновляем ширину contentContainer и всех rowPanel при изменении размера
            void UpdateContentWidth()
            {
                if (contentContainer != null && scrollablePanel != null)
                {
                    int newWidth = scrollablePanel.ClientSize.Width;
                    if (newWidth > 0)
                    {
                        contentContainer.Width = newWidth;
                        // Обновляем ширину всех rowPanel
                        foreach (Control ctrl in contentContainer.Controls)
                        {
                            if (ctrl is Panel p && p.Anchor.HasFlag(AnchorStyles.Right))
                            {
                                p.Width = newWidth;
                            }
                        }
                    }
                }
            }
            
            scrollablePanel.Resize += (s, e) => UpdateContentWidth();
            settingsContentPanel.Resize += (s, e) => UpdateContentWidth();
            
            // Добавляем прокручиваемую панель в контейнер
            settingsContentPanel.Controls.Add(scrollablePanel);
            
            // Инициализируем ширину после добавления
            UpdateContentWidth();
        }
        
        private void ShowMacrosSettings()
        {
            if (settingsContentPanel == null) return;
            
            settingsContentPanel.Controls.Clear();
            settingsContentPanel.Padding = new Padding(0, 10, 0, 0); // Небольшой верхний отступ
            
            // Разделяем панель на две части: список макросов слева и детали справа
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                Padding = new Padding(15, 30, 15, 15), // Отступ сверху 20 (15 + 5 пикселей)
                Margin = new Padding(0, 5, 0, 0) // Дополнительный отступ сверху на 5 пикселей
            };
            
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(25),
                AutoScroll = true
            };
            
            settingsContentPanel.Controls.Add(rightPanel);
            settingsContentPanel.Controls.Add(leftPanel);
            
            // Заголовок списка макросов
            Label lblMacrosList = new Label
            {
                Text = "Записи:",
                Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(10, -5),
                Margin = new Padding(0, 0, 0, 10)
            };
            leftPanel.Controls.Add(lblMacrosList);
            
            // Список макросов (кликабельные элементы)
            Panel macrosListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(0, 30),
                AutoScroll = true
            };
            
            foreach (string macroFile in savedMacroFiles)
            {
                Button btnMacro = new Button
                {
                    Text = Path.GetFileNameWithoutExtension(macroFile),
                    Dock = DockStyle.Top,
                    Height = 45,
                    Font = new System.Drawing.Font("Segoe UI", 11F),
                    BackColor = System.Drawing.Color.White,
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = macroFile
                };
                btnMacro.Click += (s, e) => {
                    // Подсвечиваем выбранный макрос
                    foreach (Control ctrl in macrosListPanel.Controls)
                    {
                        if (ctrl is Button btn)
                        {
                            btn.BackColor = System.Drawing.Color.White;
                            btn.ForeColor = System.Drawing.Color.FromArgb(66, 66, 66);
                        }
                    }
                    btnMacro.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    btnMacro.ForeColor = System.Drawing.Color.White;
                    
                    ShowMacroDetails(macroFile, rightPanel);
                };
                btnMacro.MouseEnter += (s, e) => {
                    if (btnMacro.BackColor != System.Drawing.Color.FromArgb(33, 150, 243))
                        btnMacro.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
                };
                btnMacro.MouseLeave += (s, e) => {
                    if (btnMacro.BackColor != System.Drawing.Color.FromArgb(33, 150, 243))
                        btnMacro.BackColor = System.Drawing.Color.White;
                };
                macrosListPanel.Controls.Add(btnMacro);
                macrosListPanel.Controls.SetChildIndex(btnMacro, 0);
            }
            
            leftPanel.Controls.Add(macrosListPanel);
            
            // Выбираем первый макрос если есть
            if (savedMacroFiles.Count > 0)
            {
                var firstBtn = macrosListPanel.Controls[macrosListPanel.Controls.Count - 1] as Button;
                if (firstBtn != null)
                {
                    firstBtn.PerformClick();
                }
            }
            else
            {
                // Показываем сообщение, если макросов нет
                Label lblNoMacros = new Label
                {
                    Text = "Нет сохраненных макросов",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    ForeColor = System.Drawing.Color.FromArgb(158, 158, 158),
                    AutoSize = true,
                    Location = new System.Drawing.Point(25, 100)
                };
                rightPanel.Controls.Add(lblNoMacros);
            }
        }
        
        private List<InputEvent>? currentEditingEvents = null;
        private string? currentEditingMacroFile = null;
        private long? currentEditingKeyboardLayout = null;
        private long? currentEditingDurationTicks = null;
        private bool skipDeleteConfirmation = false; // Настройка "больше не спрашивать" для удаления
        private bool isAppendingToMacro = false; // Флаг режима дозаписи в макрос
        private int actionToReplaceIndex = -1; // Индекс действия для замены
        private int mouseMovesStartIndex = -1; // Начальный индекс группы движений мыши для замены
        private int mouseMovesCountToReplace = 0; // Количество движений мыши для замены
        private bool isReplacingAction = false; // Флаг режима замены действия
        private bool isReplacingMouseMoves = false; // Флаг режима замены движений мыши
        private Panel? currentMacroDetailsPanel = null; // Панель с деталями макроса для обновления
        
        private void ShowMacroDetails(string? macroFileName, Panel panel)
        {
            if (string.IsNullOrEmpty(macroFileName) || panel == null) return;
            
            panel.Controls.Clear();
            
            string filePath = Path.Combine(macrosDirectory, macroFileName);
            if (!File.Exists(filePath))
            {
                Label lblError = new Label
                {
                    Text = "Файл не найден",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    AutoSize = true
                };
                panel.Controls.Add(lblError);
                return;
            }
            
            // Загружаем макрос
            var events = recorder?.LoadFromFile(filePath);
            if (events == null)
            {
                Label lblError = new Label
                {
                    Text = "Ошибка загрузки файла",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    AutoSize = true
                };
                panel.Controls.Add(lblError);
                return;
            }
            
            // Сохраняем события для редактирования (создаем копию)
            currentEditingEvents = new List<InputEvent>(events);
            currentEditingMacroFile = macroFileName;
            
            // Загружаем дополнительные данные из файла
            LoadMacroMetadata(filePath);
            
            // Если событий нет, создаем пустой список
            if (currentEditingEvents.Count == 0)
            {
                currentEditingEvents = new List<InputEvent>();
            }
            
            // Название макроса (редактируемое)
            Label lblMacroNameLabel = new Label
            {
                Text = "Название макроса:",
                Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, 0),
            };
            panel.Controls.Add(lblMacroNameLabel);
            
            TextBox txtMacroName = new TextBox
            {
                Text = Path.GetFileNameWithoutExtension(macroFileName),
                Font = new System.Drawing.Font("Segoe UI", 12F),
                Location = new System.Drawing.Point(0, 25),
                Size = new System.Drawing.Size(450, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White,
            };
            panel.Controls.Add(txtMacroName);
            
            // Кнопка удаления макроса
            Button btnDeleteMacro = new Button
            {
                Text = "Удалить запись",
                Size = new System.Drawing.Size(180, 29),
                Location = new System.Drawing.Point(460, 25),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnDeleteMacro.Click += (s, e) => {
                DeleteMacro(macroFileName);
            };
            btnDeleteMacro.MouseEnter += (s, e) => {
                btnDeleteMacro.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
            };
            btnDeleteMacro.MouseLeave += (s, e) => {
                btnDeleteMacro.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            };
            panel.Controls.Add(btnDeleteMacro);
            
            // Список действий
            int yPos = 80;
            Label lblActions = new Label
            {
                Text = "Действия:",
                Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, yPos)
            };
            panel.Controls.Add(lblActions);
            
            // Кнопка "Добавить действие"
            Button btnAddAction = new Button
            {
                Text = "Добавить действие",
                Size = new System.Drawing.Size(180, 35),
                Location = new System.Drawing.Point(150, yPos - 2),
                Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnAddAction.Click += (s, e) => {
                StartAppendingToMacro(panel);
            };
            panel.Controls.Add(btnAddAction);
            
            yPos += 45;
            
            // Группируем движения мыши и нумеруем действия (используем currentEditingEvents)
            var groupedEvents = new List<object>();
            List<InputEvent> mouseMoveGroup = new List<InputEvent>();
            
            // Счетчики для нумерации каждого типа действия
            Dictionary<string, int> actionCounters = new Dictionary<string, int>();
            int mouseMoveGroupCounter = 0;
            
            for (int i = 0; i < currentEditingEvents.Count; i++)
            {
                if (currentEditingEvents[i].EventType == "MouseMove")
                {
                    mouseMoveGroup.Add(currentEditingEvents[i]);
                }
                else
                {
                    // Если накопились движения мыши, добавляем как группу
                    if (mouseMoveGroup.Count > 0)
                    {
                        mouseMoveGroupCounter++;
                        groupedEvents.Add(new { 
                            Event = mouseMoveGroup.ToList(), 
                            DisplayText = $"🖱️ Движения курсора №{mouseMoveGroupCounter}", 
                            Index = i - mouseMoveGroup.Count,
                            IsGroup = true
                        });
                        mouseMoveGroup.Clear();
                    }
                    
                    string displayText = FormatEventForDisplayWithNumber(currentEditingEvents[i], actionCounters);
                    groupedEvents.Add(new { 
                        Event = currentEditingEvents[i], 
                        DisplayText = displayText, 
                        Index = i,
                        IsGroup = false
                    });
                }
            }
            
            // Добавляем оставшиеся движения мыши
            if (mouseMoveGroup.Count > 0)
            {
                mouseMoveGroupCounter++;
                groupedEvents.Add(new { 
                    Event = mouseMoveGroup.ToList(), 
                    DisplayText = $"🖱️ Движения курсора №{mouseMoveGroupCounter}", 
                    Index = currentEditingEvents.Count - mouseMoveGroup.Count,
                    IsGroup = true
                });
            }
            
            // Панель для списка действий
            Panel actionsPanel = new Panel
            {
                Location = new System.Drawing.Point(0, yPos),
                Size = new System.Drawing.Size(750, 450),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(actionsPanel);
            
            int actionYPos = 0;
            
            // Отображаем действия
            foreach (var item in groupedEvents)
            {
                dynamic eventItem = item;
                
                // Контейнер для действия
                Panel actionContainer = new Panel
                {
                    Location = new System.Drawing.Point(0, actionYPos),
                    Size = new System.Drawing.Size(730, 45),
                    BackColor = System.Drawing.Color.White
                };
                
                Button btnAction = new Button
                {
                    Text = eventItem.DisplayText,
                    Font = new System.Drawing.Font("Segoe UI", 11F),
                    Location = new System.Drawing.Point(0, 0),
                    Size = new System.Drawing.Size(600, 45),
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    Padding = new Padding(15, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = eventItem,
                    Dock = DockStyle.Left
                };
                btnAction.Click += (s, e) => {
                    if (eventItem.IsGroup)
                    {
                        EditMacroMouseMoves(eventItem.Event, btnAction);
                    }
                    else
                    {
                        EditMacroAction(eventItem.Event, btnAction);
                    }
                };
                btnAction.MouseEnter += (s, e) => {
                    btnAction.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                };
                btnAction.MouseLeave += (s, e) => {
                    btnAction.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
                };
                
                // Кнопка удаления - сохраняем индекс из groupedEvents для надежного удаления
                int actionIndexInGroup = groupedEvents.IndexOf(item);
                Button btnDelete = new Button
                {
                    Text = "Удалить действие",
                    Font = new System.Drawing.Font("Segoe UI", 9F),
                    Location = new System.Drawing.Point(605, 5),
                    Size = new System.Drawing.Size(120, 35),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                    ForeColor = System.Drawing.Color.White,
                    FlatAppearance = { BorderSize = 0 },
                    Cursor = Cursors.Hand,
                    Tag = new { EventItem = eventItem, Index = actionIndexInGroup }
                };
                btnDelete.Click += (s, e) => {
                    var tagData = (dynamic)btnDelete.Tag;
                    DeleteMacroActionWithConfirmation(tagData.EventItem, panel);
                };
                btnDelete.MouseEnter += (s, e) => {
                    btnDelete.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
                };
                btnDelete.MouseLeave += (s, e) => {
                    btnDelete.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
                };
                
                actionContainer.Controls.Add(btnDelete);
                actionContainer.Controls.Add(btnAction);
                actionsPanel.Controls.Add(actionContainer);
                
                actionYPos += 50;
            }
            
            yPos += 460;
            
            // Кнопка сохранения
            Button btnSaveMacro = new Button
            {
                Text = "💾 Сохранить изменения",
                Size = new System.Drawing.Size(250, 40),
                Location = new System.Drawing.Point(0, yPos + 20),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnSaveMacro.Click += (s, e) => {
                SaveMacroChanges(macroFileName, txtMacroName.Text, panel);
            };
            panel.Controls.Add(btnSaveMacro);
        }
        
        private void SaveMacroChanges(string oldFileName, string newName, Panel panel)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя макроса не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Очищаем имя от недопустимых символов
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                newName = newName.Replace(c.ToString(), "");
            }
            newName = newName.Trim();
            
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя макроса содержит только недопустимые символы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string oldFilePath = Path.Combine(macrosDirectory, oldFileName);
            string newFileName = $"{newName}.json";
            string newFilePath = Path.Combine(macrosDirectory, newFileName);
            
            try
            {
                // Если имя не изменилось, ничего не делаем
                if (oldFileName == newFileName)
                {
                    AddLog("ℹ️ Имя макроса не изменилось");
                    return;
                }
                
                // Если файл с новым именем уже существует
                if (File.Exists(newFilePath) && oldFileName != newFileName)
                {
                    var result = MessageBox.Show(
                        $"Файл с именем '{newFileName}' уже существует. Перезаписать?",
                        "Файл уже существует",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }
                
                // Сохраняем изменения в текущем файле или переименовываем
                if (oldFileName != newFileName && File.Exists(oldFilePath))
                {
                    // Сохраняем в новый файл
                    SaveMacroEvents(newFilePath);
                    File.Delete(oldFilePath);
                    
                    // Обновляем список сохраненных файлов
                    if (savedMacroFiles.Contains(oldFileName))
                    {
                        savedMacroFiles.Remove(oldFileName);
                    }
                    
                    if (!savedMacroFiles.Contains(newFileName))
                    {
                        savedMacroFiles.Add(newFileName);
                        savedMacroFiles.Sort();
                    }
                    
                    // Обновляем текущий файл если он был выбран
                    if (currentMacroFile == oldFileName)
                    {
                        currentMacroFile = newFileName;
                    }
                    
                    // Обновляем ComboBox
                    UpdateMacroFilesComboBox();
                    
                    AddLog($"✏️ Макрос переименован: {oldFileName} → {newFileName}");
                }
                else
                {
                    // Просто сохраняем изменения в текущем файле
                    SaveMacroEvents(oldFilePath);
                    AddLog($"💾 Изменения макроса сохранены");
                }
                
                // Обновляем интерфейс настроек макросов
                ShowMacrosSettings();
                
                MessageBox.Show(oldFileName == newFileName ? "Изменения макроса сохранены" : "Макрос успешно переименован и сохранен", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при переименовании макроса: {ex.Message}");
                MessageBox.Show($"Ошибка при переименовании: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string FormatEventForDisplay(InputEvent evt)
        {
            switch (evt.EventType)
            {
                case "KeyPress":
                    string keyName = GetKeyDisplayName(evt.KeyCode);
                    return evt.IsPressed ? $"⌨️ Нажатие клавиши: {keyName}" : $"⌨️ Отпускание клавиши: {keyName}";
                case "MouseClick":
                    string button = evt.MouseButton == 1 ? "ЛКМ" : evt.MouseButton == 2 ? "ПКМ" : "СКМ";
                    return evt.IsPressed ? $"🖱️ Нажатие {button} ({evt.X}, {evt.Y})" : $"🖱️ Отпускание {button} ({evt.X}, {evt.Y})";
                case "MouseMove":
                    return $"🖱️ Движения курсора";
                default:
                    return evt.EventType;
            }
        }
        
        private string FormatEventForDisplayWithNumber(InputEvent evt, Dictionary<string, int> counters)
        {
            switch (evt.EventType)
            {
                case "KeyPress":
                    string keyName = GetKeyDisplayName(evt.KeyCode);
                    string keyCounterKey = $"KeyPress_{keyName}";
                    if (!counters.ContainsKey(keyCounterKey))
                        counters[keyCounterKey] = 0;
                    counters[keyCounterKey]++;
                    return $"⌨️ Нажатие клавиши {keyName} №{counters[keyCounterKey]}";
                case "MouseClick":
                    string button = evt.MouseButton == 1 ? "ЛКМ" : evt.MouseButton == 2 ? "ПКМ" : "СКМ";
                    string clickCounterKey = $"MouseClick_{button}";
                    if (!counters.ContainsKey(clickCounterKey))
                        counters[clickCounterKey] = 0;
                    counters[clickCounterKey]++;
                    return $"🖱️ Нажатие {button} №{counters[clickCounterKey]}";
                case "MouseMove":
                    // Этот случай обрабатывается отдельно для групп
                    return $"🖱️ Движения курсора";
                default:
                    return evt.EventType;
            }
        }
        
        private string GetKeyDisplayName(int keyCode)
        {
            // Полная функция для отображения имени клавиши
            if (keyCode >= 0x30 && keyCode <= 0x39) return ((char)keyCode).ToString(); // Цифры
            if (keyCode >= 0x41 && keyCode <= 0x5A) return ((char)keyCode).ToString(); // Буквы
            
            return keyCode switch
            {
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x20 => "Space",
                0x1B => "Escape",
                0x2E => "Delete",
                0x25 => "←",
                0x26 => "↑",
                0x27 => "→",
                0x28 => "↓",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x2C => "PrintScreen",
                0x2D => "Insert",
                0x14 => "CapsLock",
                0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                0xA0 => "Shift", 0xA1 => "Shift",
                0xA2 => "Ctrl", 0xA3 => "Ctrl",
                0xA4 => "Alt", 0xA5 => "Alt",
                0xBC => ",", 0xBD => "-", 0xBE => ".", 0xBF => "/",
                0xBA => ";", 0xBB => "=", 0xDB => "[", 0xDC => "\\",
                0xDD => "]", 0xDE => "'", 0xC0 => "`",
                0x13 => "Pause",
                _ => $"Key(0x{keyCode:X2})"
            };
        }
        
        private void EditMacroAction(InputEvent evt, Button btn)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            // Находим индекс действия в списке
            int actionIndex = -1;
            for (int i = 0; i < currentEditingEvents.Count; i++)
            {
                var currentEvt = currentEditingEvents[i];
                if (currentEvt.EventType == evt.EventType &&
                    currentEvt.Timestamp == evt.Timestamp &&
                    (evt.EventType == "KeyPress" ? currentEvt.KeyCode == evt.KeyCode : true) &&
                    (evt.EventType == "MouseClick" || evt.EventType == "MouseMove" 
                        ? currentEvt.X == evt.X && currentEvt.Y == evt.Y && currentEvt.MouseButton == evt.MouseButton 
                        : true))
                {
                    actionIndex = i;
                    break;
                }
            }
            
            if (actionIndex < 0)
            {
                MessageBox.Show("Не удалось найти действие для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Информационное окно о замене через запись
            Form infoDialog = new Form
            {
                Text = "Замена действия",
                Size = new System.Drawing.Size(550, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10);
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0);
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите новое действие для замены текущего.\n\n" +
                       $"Для отмены замены нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения нового действия нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(510, 130),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            // Таймер для автозакрытия через 3 секунды
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            // Устанавливаем флаг замены и индекс
            isReplacingAction = true;
            actionToReplaceIndex = actionIndex;
            
            AddLog($"🔄 Начата замена действия. Нажмите горячую клавишу для остановки.");
            
            // Запускаем запись
            StartRecording();
            
            // Закрываем панель настроек и возвращаемся на главный экран для записи
            HideSettingsPanel();
        }
        
        private void LoadMacroMetadata(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var macroData = System.Text.Json.JsonSerializer.Deserialize<MacroData>(json, new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                
                if (macroData != null)
                {
                    currentEditingKeyboardLayout = macroData.KeyboardLayout;
                    currentEditingDurationTicks = macroData.DurationTicks;
                }
            }
            catch
            {
                // Если не удалось загрузить метаданные, оставляем null
                currentEditingKeyboardLayout = null;
                currentEditingDurationTicks = null;
            }
        }
        
        private void DeleteMacro(string macroFileName)
        {
            if (string.IsNullOrEmpty(macroFileName)) return;
            
            // Подтверждение удаления
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить запись \"{Path.GetFileNameWithoutExtension(macroFileName)}\"?\n\nЗапись будет удалена безвозвратно.",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            
            if (result != DialogResult.Yes) return;
            
            try
            {
                string filePath = Path.Combine(macrosDirectory, macroFileName);
                
                // Удаляем файл
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    AddLog($"🗑️ Запись удалена: {macroFileName}");
                }
                
                // Удаляем из списка
                if (savedMacroFiles.Contains(macroFileName))
                {
                    savedMacroFiles.Remove(macroFileName);
                }
                
                // Если это текущий макрос - очищаем
                if (currentMacroFile == macroFileName)
                {
                    currentMacroFile = "";
                }
                
                if (currentEditingMacroFile == macroFileName)
                {
                    currentEditingMacroFile = null;
                    currentEditingEvents = null;
                    currentMacroDetailsPanel = null;
                }
                
                // Обновляем ComboBox
                UpdateMacroFilesComboBox();
                
                // Обновляем интерфейс настроек макросов
                ShowMacrosSettings();
                
                MessageBox.Show("Запись успешно удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при удалении записи: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SaveMacroEvents(string filePath)
        {
            if (currentEditingEvents == null) return;
            
            try
            {
                var macroData = new MacroData
                {
                    Events = currentEditingEvents,
                    KeyboardLayout = currentEditingKeyboardLayout,
                    DurationTicks = currentEditingDurationTicks
                };

                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(macroData, options);
                File.WriteAllText(filePath, json);
                
                AddLog($"💾 Макрос сохранен: {Path.GetFileName(filePath)} ({currentEditingEvents.Count} событий)");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при сохранении событий: {ex.Message}");
                throw;
            }
        }
        
        private void AddNewMacroAction(Panel panel)
        {
            // Диалог выбора типа действия
            Form actionDialog = new Form
            {
                Text = "Добавить действие",
                Size = new System.Drawing.Size(400, 250),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblType = new Label
            {
                Text = "Тип действия:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };
            actionDialog.Controls.Add(lblType);
            
            ComboBox cmbActionType = new ComboBox
            {
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbActionType.Items.AddRange(new[] { "Нажатие клавиши", "Нажатие мыши (ЛКМ)", "Нажатие мыши (ПКМ)", "Нажатие мыши (СКМ)", "Движение мыши" });
            cmbActionType.SelectedIndex = 0;
            actionDialog.Controls.Add(cmbActionType);
            
            Button btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(180, 150),
                Size = new System.Drawing.Size(80, 30)
            };
            actionDialog.Controls.Add(btnOK);
            
            Button btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(280, 150),
                Size = new System.Drawing.Size(80, 30)
            };
            actionDialog.Controls.Add(btnCancel);
            actionDialog.CancelButton = btnCancel;
            actionDialog.AcceptButton = btnOK;
            
            if (actionDialog.ShowDialog() == DialogResult.OK)
            {
                InputEvent? newEvent = null;
                
                switch (cmbActionType.SelectedIndex)
                {
                    case 0: // Нажатие клавиши
                        newEvent = CreateKeyPressEvent();
                        break;
                    case 1: // ЛКМ
                        newEvent = CreateMouseClickEvent(1);
                        break;
                    case 2: // ПКМ
                        newEvent = CreateMouseClickEvent(2);
                        break;
                    case 3: // СКМ
                        newEvent = CreateMouseClickEvent(3);
                        break;
                    case 4: // Движение мыши
                        newEvent = CreateMouseMoveEvent();
                        break;
                }
                
                if (newEvent != null && currentEditingEvents != null)
                {
                    // Добавляем событие в конец списка
                    long maxTimestamp = currentEditingEvents.Count > 0 
                        ? currentEditingEvents.Max(e => e.Timestamp) 
                        : 0;
                    newEvent.Timestamp = maxTimestamp + 10000000; // +1 секунда
                    currentEditingEvents.Add(newEvent);
                    
                    // Перерисовываем панель
                    ShowMacroDetails(currentEditingMacroFile, panel);
                    AddLog($"➕ Добавлено новое действие");
                }
            }
        }
        
        private InputEvent? CreateKeyPressEvent()
        {
            Form keyDialog = new Form
            {
                Text = "Выберите клавишу",
                Size = new System.Drawing.Size(350, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblInfo = new Label
            {
                Text = "Нажмите клавишу:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };
            keyDialog.Controls.Add(lblInfo);
            
            TextBox txtKey = new TextBox
            {
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(290, 25),
                ReadOnly = true,
                Text = "Нажмите клавишу..."
            };
            keyDialog.Controls.Add(txtKey);
            
            int? selectedKeyCode = null;
            
            keyDialog.KeyDown += (s, e) => {
                selectedKeyCode = (int)e.KeyCode;
                txtKey.Text = e.KeyCode.ToString();
                e.Handled = true;
            };
            
            Button btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(180, 75),
                Size = new System.Drawing.Size(60, 30)
            };
            keyDialog.Controls.Add(btnOK);
            keyDialog.AcceptButton = btnOK;
            
            if (keyDialog.ShowDialog() == DialogResult.OK && selectedKeyCode.HasValue)
            {
                return new InputEvent
                {
                    EventType = "KeyPress",
                    KeyCode = selectedKeyCode.Value,
                    IsPressed = true,
                    Timestamp = 0 // Будет установлено позже
                };
            }
            
            return null;
        }
        
        private InputEvent? CreateMouseClickEvent(int button)
        {
            Form posDialog = new Form
            {
                Text = "Координаты клика",
                Size = new System.Drawing.Size(350, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblX = new Label { Text = "X:", Location = new System.Drawing.Point(20, 20), AutoSize = true };
            TextBox txtX = new TextBox { Location = new System.Drawing.Point(50, 17), Size = new System.Drawing.Size(100, 25), Text = "0" };
            Label lblY = new Label { Text = "Y:", Location = new System.Drawing.Point(170, 20), AutoSize = true };
            TextBox txtY = new TextBox { Location = new System.Drawing.Point(200, 17), Size = new System.Drawing.Size(100, 25), Text = "0" };
            
            posDialog.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY });
            
            Button btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(180, 70),
                Size = new System.Drawing.Size(60, 30)
            };
            posDialog.Controls.Add(btnOK);
            posDialog.AcceptButton = btnOK;
            
            if (posDialog.ShowDialog() == DialogResult.OK &&
                int.TryParse(txtX.Text, out int x) &&
                int.TryParse(txtY.Text, out int y))
            {
                return new InputEvent
                {
                    EventType = "MouseClick",
                    MouseButton = button,
                    X = x,
                    Y = y,
                    IsPressed = true,
                    Timestamp = 0
                };
            }
            
            return null;
        }
        
        private InputEvent? CreateMouseMoveEvent()
        {
            Form posDialog = new Form
            {
                Text = "Координаты движения",
                Size = new System.Drawing.Size(350, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblX = new Label { Text = "X:", Location = new System.Drawing.Point(20, 20), AutoSize = true };
            TextBox txtX = new TextBox { Location = new System.Drawing.Point(50, 17), Size = new System.Drawing.Size(100, 25), Text = "0" };
            Label lblY = new Label { Text = "Y:", Location = new System.Drawing.Point(170, 20), AutoSize = true };
            TextBox txtY = new TextBox { Location = new System.Drawing.Point(200, 17), Size = new System.Drawing.Size(100, 25), Text = "0" };
            
            posDialog.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY });
            
            Button btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(180, 70),
                Size = new System.Drawing.Size(60, 30)
            };
            posDialog.Controls.Add(btnOK);
            posDialog.AcceptButton = btnOK;
            
            if (posDialog.ShowDialog() == DialogResult.OK &&
                int.TryParse(txtX.Text, out int x) &&
                int.TryParse(txtY.Text, out int y))
            {
                return new InputEvent
                {
                    EventType = "MouseMove",
                    X = x,
                    Y = y,
                    Timestamp = 0
                };
            }
            
            return null;
        }
        
        private void DeleteMacroActionWithConfirmation(dynamic eventItem, Panel panel)
        {
            if (currentEditingEvents == null) return;
            
            // Если пользователь выбрал "больше не спрашивать", пропускаем подтверждение
            if (skipDeleteConfirmation)
            {
                DeleteMacroAction(eventItem, panel);
                return;
            }
            
            // Создаем форму подтверждения
            Form confirmDialog = new Form
            {
                Text = "Подтверждение удаления",
                Size = new System.Drawing.Size(450, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblMessage = new Label
            {
                Text = "Вы уверены, что хотите удалить это действие из записи? Действие удалится безвозвратно.",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(400, 50),
                AutoSize = false
            };
            confirmDialog.Controls.Add(lblMessage);
            
            CheckBox chkDontAsk = new CheckBox
            {
                Text = "Больше не спрашивать",
                Location = new System.Drawing.Point(20, 80),
                AutoSize = true
            };
            confirmDialog.Controls.Add(chkDontAsk);
            
            Button btnYes = new Button
            {
                Text = "Да",
                DialogResult = DialogResult.Yes,
                Location = new System.Drawing.Point(250, 110),
                Size = new System.Drawing.Size(80, 30)
            };
            confirmDialog.Controls.Add(btnYes);
            
            Button btnNo = new Button
            {
                Text = "Нет",
                DialogResult = DialogResult.No,
                Location = new System.Drawing.Point(340, 110),
                Size = new System.Drawing.Size(80, 30)
            };
            confirmDialog.Controls.Add(btnNo);
            confirmDialog.CancelButton = btnNo;
            confirmDialog.AcceptButton = btnYes;
            
            var result = confirmDialog.ShowDialog();
            
            if (chkDontAsk.Checked && result == DialogResult.Yes)
            {
                skipDeleteConfirmation = true;
                SaveSettings();
            }
            
            if (result == DialogResult.Yes)
            {
                DeleteMacroAction(eventItem, panel);
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["SkipDeleteConfirmation"] = skipDeleteConfirmation
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                string settingsPath = Path.Combine(Application.StartupPath, "settings.json");
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Игнорируем ошибки сохранения настроек
            }
        }
        
        private void StartAppendingToMacro(Panel panel)
        {
            if (currentEditingMacroFile == null || currentEditingEvents == null)
            {
                MessageBox.Show("Не выбран макрос для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Получаем названия горячих клавиш
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0); // Backquote - для отмены
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10); // F10 - для сохранения
            
            // Создаем информационное окно
            Form infoDialog = new Form
            {
                Text = "Добавление действий",
                Size = new System.Drawing.Size(600, 220),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите нужные вам действия.\n\n" +
                       $"Для отмены записи нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения записи нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(550, 140),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            // Таймер для автозакрытия через 3 секунды
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            // Если окно закрылось (через таймер или кнопку OK), начинаем запись
            isAppendingToMacro = true;
            AddLog("➕ Начата дозапись в макрос. Нажмите горячую клавишу для остановки.");
            
            // Запускаем запись
            StartRecording();
            
            // Закрываем панель настроек и возвращаемся на главный экран для записи
            HideSettingsPanel();
        }
        
        private string GetHotkeyName(int hotkeyId, Keys defaultKey)
        {
            if (savedHotkeys.ContainsKey(hotkeyId))
            {
                uint mod = savedHotkeyModifiers.ContainsKey(hotkeyId) ? savedHotkeyModifiers[hotkeyId] : 0;
                return FormatKeysToString(savedHotkeys[hotkeyId], mod);
            }
            return FormatKeysToString(defaultKey, 0);
        }
        
        private void DeleteMacroAction(dynamic eventItem, Panel panel)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            try
            {
                // Используем более надежный способ удаления - находим событие по точному совпадению
                if (eventItem.IsGroup)
                {
                    // Удаляем группу движений мыши - находим по первому событию
                    var mouseMoves = (List<InputEvent>)eventItem.Event;
                    if (mouseMoves == null || mouseMoves.Count == 0) return;
                    
                    var firstMove = mouseMoves[0];
                    int startIndex = -1;
                    
                    // Находим начальный индекс группы движений мыши
                    for (int i = 0; i < currentEditingEvents.Count; i++)
                    {
                        var evt = currentEditingEvents[i];
                        if (evt.EventType == "MouseMove" &&
                            evt.X == firstMove.X &&
                            evt.Y == firstMove.Y &&
                            Math.Abs(evt.Timestamp - firstMove.Timestamp) < 1000000) // Допуск для timestamp
                        {
                            startIndex = i;
                            break;
                        }
                    }
                    
                    if (startIndex >= 0)
                    {
                        // Удаляем все MouseMove события подряд, начиная с startIndex
                        int removed = 0;
                        for (int i = startIndex; i < currentEditingEvents.Count && removed < mouseMoves.Count;)
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
                    }
                }
                else
                {
                    // Удаляем одно событие - находим его в списке с допуском для timestamp
                    var evt = (InputEvent)eventItem.Event;
                    for (int i = currentEditingEvents.Count - 1; i >= 0; i--)
                    {
                        var currentEvt = currentEditingEvents[i];
                        bool matches = false;
                        
                        if (currentEvt.EventType == evt.EventType)
                        {
                            if (evt.EventType == "KeyPress")
                            {
                                matches = currentEvt.KeyCode == evt.KeyCode;
                            }
                            else if (evt.EventType == "MouseClick")
                            {
                                matches = currentEvt.X == evt.X && 
                                         currentEvt.Y == evt.Y && 
                                         currentEvt.MouseButton == evt.MouseButton;
                            }
                            else if (evt.EventType == "MouseMove")
                            {
                                matches = currentEvt.X == evt.X && currentEvt.Y == evt.Y;
                            }
                        }
                        
                        if (matches)
                        {
                            currentEditingEvents.RemoveAt(i);
                            break;
                        }
                    }
                }
                
                AddLog($"🗑️ Действие удалено");
                
                // Сохраняем изменения
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                
                // Перерисовываем панель
                RefreshMacroDetailsPanel();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при удалении действия: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void EditMacroMouseMoves(List<InputEvent> mouseMoves, Button btn)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            // Находим начальный индекс группы движений мыши
            int startIndex = -1;
            if (mouseMoves.Count > 0)
            {
                var firstMove = mouseMoves[0];
                for (int i = 0; i < currentEditingEvents.Count; i++)
                {
                    var evt = currentEditingEvents[i];
                    if (evt.EventType == "MouseMove" &&
                        evt.X == firstMove.X &&
                        evt.Y == firstMove.Y &&
                        evt.Timestamp == firstMove.Timestamp)
                    {
                        startIndex = i;
                        break;
                    }
                }
            }
            
            if (startIndex < 0)
            {
                MessageBox.Show("Не удалось найти группу движений для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Информационное окно о замене через запись
            Form infoDialog = new Form
            {
                Text = "Замена движений мыши",
                Size = new System.Drawing.Size(550, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10);
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0);
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите новые движения мыши для замены текущих.\n\n" +
                       $"Для отмены замены нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения новых движений нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(510, 130),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            // Таймер для автозакрытия через 3 секунды
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            // Устанавливаем флаг замены и индекс
            isReplacingMouseMoves = true;
            mouseMovesStartIndex = startIndex;
            
            AddLog($"🔄 Начата замена движений мыши. Нажмите горячую клавишу для остановки.");
            
            // Запускаем запись
            StartRecording();
            
            // Закрываем панель настроек и возвращаемся на главный экран для записи
            HideSettingsPanel();
        }
        
        private void EditMacroMouseMoves_OLD(List<InputEvent> mouseMoves, Button btn)
        {
            // Диалог редактирования движений мыши
            Form editDialog = new Form
            {
                Text = "Редактирование движений курсора",
                Size = new System.Drawing.Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblInfo = new Label
            {
                Text = "Координаты движений курсора (по одному на строку, формат: X,Y):",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            editDialog.Controls.Add(lblInfo);
            
            TextBox txtCoords = new TextBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(540, 350),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 9F),
                Text = string.Join("\r\n", mouseMoves.Select(m => $"{m.X},{m.Y}"))
            };
            editDialog.Controls.Add(txtCoords);
            
            Button btnSave = new Button
            {
                Text = "Сохранить",
                Location = new System.Drawing.Point(410, 410),
                Size = new System.Drawing.Size(150, 35),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(33, 150, 243),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnSave.Click += (s, e) => {
                try
                {
                    var lines = txtCoords.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length != mouseMoves.Count)
                    {
                        MessageBox.Show($"Количество координат должно быть {mouseMoves.Count}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    for (int i = 0; i < lines.Length && i < mouseMoves.Count; i++)
                    {
                        var parts = lines[i].Split(',');
                        if (parts.Length == 2 && 
                            int.TryParse(parts[0].Trim(), out int x) && 
                            int.TryParse(parts[1].Trim(), out int y))
                        {
                            mouseMoves[i].X = x;
                            mouseMoves[i].Y = y;
                        }
                    }
                    
                    AddLog($"✏️ Изменены координаты движений курсора ({mouseMoves.Count} точек)");
                    editDialog.Close();
                }
                catch
                {
                    MessageBox.Show("Ошибка при обработке координат", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            editDialog.Controls.Add(btnSave);
            
            editDialog.ShowDialog();
        }
        
        private Button? waitingForHotkeyButton = null;
        private int? waitingForHotkeyId = null;
        private Keys currentPressedKeys = Keys.None; // Текущие нажатые клавиши при ожидании горячей клавиши
        private Keys maxPressedKeys = Keys.None; // Максимальная комбинация всех одновременно нажатых клавиш
        private HashSet<Keys> pressedKeysSet = new HashSet<Keys>(); // Множество нажатых клавиш
        private Keys lastNonModifierKey = Keys.None; // Последняя нажатая не-модификаторная клавиша
        
        private void ChangeHotkey(Button btn, int hotkeyId)
        {
            // ВАЖНО: Разрешаем изменение горячей клавиши ТОЛЬКО если панель настроек видима
            if (panelSettings == null || !panelSettings.Visible)
            {
                return; // Не выполняем изменение, если панель настроек не видима
            }
            
            btn.Text = "Нажмите клавишу...";
            btn.BackColor = System.Drawing.Color.FromArgb(255, 235, 59);
            
            // Устанавливаем флаг ожидания нажатия клавиши
            waitingForHotkeyButton = btn;
            waitingForHotkeyId = hotkeyId;
            currentPressedKeys = Keys.None;
            maxPressedKeys = Keys.None;
            lastNonModifierKey = Keys.None;
            pressedKeysSet.Clear();
            
            // Форма должна быть в фокусе для захвата клавиш
            this.KeyPreview = true;
            this.Focus();
            this.BringToFront();
            this.Activate();
            
            AddLog($"⌨️ Ожидание нажатия клавиши для: {GetHotkeyActionName(hotkeyId)}");
        }
        
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Обрабатываем только если ожидаем настройку горячей клавиши И панель настроек видима
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true; // Блокируем дальнейшую обработку
                
                // Извлекаем клавишу из события - используем именно ту клавишу, которая была нажата
                // e.KeyCode содержит код клавиши без модификаторов
                Keys keyCode = e.KeyCode;
                Keys modifierKeys = e.KeyData & Keys.Modifiers;
                
                // Добавляем не-модификаторную клавишу (используем KeyCode, а не KeyData)
                if (keyCode != Keys.None && keyCode != Keys.ControlKey && keyCode != Keys.Menu && keyCode != Keys.ShiftKey)
                {
                    pressedKeysSet.Add(keyCode);
                    // Сохраняем последнюю нажатую не-модификаторную клавишу
                    Keys excludedModifiers = Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.ControlKey | Keys.Menu | Keys.ShiftKey;
                    if ((keyCode & excludedModifiers) == 0)
                    {
                        lastNonModifierKey = keyCode;
                    }
                }
                
                // Добавляем модификаторы как отдельные клавиши (если они есть)
                if ((modifierKeys & Keys.Control) != 0)
                    pressedKeysSet.Add(Keys.Control);
                if ((modifierKeys & Keys.Alt) != 0)
                    pressedKeysSet.Add(Keys.Alt);
                if ((modifierKeys & Keys.Shift) != 0)
                    pressedKeysSet.Add(Keys.Shift);
                
                // Обновляем текущие нажатые клавиши
                currentPressedKeys = Keys.None;
                foreach (var k in pressedKeysSet)
                {
                    currentPressedKeys |= k;
                }
                
                // Сохраняем максимальную комбинацию всех одновременно нажатых клавиш
                if (currentPressedKeys != Keys.None)
                {
                    maxPressedKeys = currentPressedKeys; // Обновляем максимальную комбинацию
                    
                    // Обновляем текст кнопки в реальном времени
                    uint modifier = GetModifierFromKeys(currentPressedKeys);
                    string previewText = FormatKeysToString(currentPressedKeys, modifier);
                    waitingForHotkeyButton.Text = previewText;
                }
            }
        }
        
        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            // Обрабатываем только если ожидаем настройку горячей клавиши И панель настроек видима
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true; // Блокируем дальнейшую обработку
                
                // Удаляем клавишу из множества нажатых
                // Используем KeyCode для точного определения клавиши
                Keys keyCode = e.KeyCode;
                Keys releasedModifiers = e.KeyData & Keys.Modifiers;
                
                // Удаляем не-модификаторную клавишу
                if (keyCode != Keys.None && keyCode != Keys.ControlKey && keyCode != Keys.Menu && keyCode != Keys.ShiftKey)
                {
                    pressedKeysSet.Remove(keyCode);
                }
                
                // Удаляем модификаторы
                if ((releasedModifiers & Keys.Control) != 0)
                    pressedKeysSet.Remove(Keys.Control);
                if ((releasedModifiers & Keys.Alt) != 0)
                    pressedKeysSet.Remove(Keys.Alt);
                if ((releasedModifiers & Keys.Shift) != 0)
                    pressedKeysSet.Remove(Keys.Shift);
                
                // Если все клавиши отпущены - сохраняем горячую клавишу
                if (pressedKeysSet.Count == 0 && maxPressedKeys != Keys.None)
                {
                    // Используем максимальную комбинацию всех одновременно нажатых клавиш
                    Keys finalCombination = maxPressedKeys;
                    
                    // Извлекаем модификаторы
                    uint finalModifier = GetModifierFromKeys(finalCombination);
                    
                    // Используем последнюю нажатую не-модификаторную клавишу
                    // НО если это модификатор или None, используем текущий KeyCode из события KeyUp
                    Keys finalKey = lastNonModifierKey;
                    
                    // Если lastNonModifierKey не установлен или это модификатор, используем KeyCode из текущего события
                    Keys excludedModifiers = Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.ControlKey | Keys.Menu | Keys.ShiftKey | Keys.LMenu | Keys.RMenu | Keys.LShiftKey | Keys.RShiftKey | Keys.LControlKey | Keys.RControlKey;
                    if (finalKey == Keys.None || (finalKey & excludedModifiers) != 0)
                    {
                        // Используем KeyCode из события KeyUp (это реальная клавиша, которая была отпущена)
                        if (keyCode != Keys.None && (keyCode & excludedModifiers) == 0)
                        {
                            finalKey = keyCode;
                        }
                        else
                        {
                            // Если и KeyCode не подходит, извлекаем из комбинации
                            Keys keysWithoutMods = GetKeyWithoutModifiers(finalCombination);
                            finalKey = keysWithoutMods;
                            
                            // Убираем модификаторы из keysWithoutMods
                            finalKey &= ~excludedModifiers;
                            
                            // Если все еще не нашли, ищем вручную
                            if (finalKey == Keys.None)
                            {
                                for (int i = 255; i >= 1; i--)
                                {
                                    Keys testKey = (Keys)i;
                                    if ((keysWithoutMods & testKey) == testKey && (testKey & excludedModifiers) == 0)
                                    {
                                        finalKey = testKey;
                                        break;
                                    }
                                }
                            }
                            
                            // Если все еще None, используем keysWithoutMods как есть
                            if (finalKey == Keys.None)
                            {
                                finalKey = keysWithoutMods;
                            }
                        }
                    }
                    
                    // Отладочная информация
                    Console.WriteLine($"DEBUG: lastNonModifierKey = {lastNonModifierKey}, keyCode = {keyCode}, maxPressedKeys = {maxPressedKeys}, finalKey = {finalKey}, finalCombination = {finalCombination}");
                    
                    // Проверяем, является ли клавиша системной
                    bool isSystemKey = systemKeys.Contains(finalKey);
                    if (isSystemKey)
                    {
                        var result = MessageBox.Show(
                            "Предупреждение: эта клавиша может использоваться системой, в случае, если она не работает, попробуйте другую клавишу или сочетание клавиш.\n\nПродолжить?",
                            "Предупреждение",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        if (result == DialogResult.No)
                        {
                            // Восстанавливаем старое значение
                            string oldText = FormatKeysToString(
                                savedHotkeys.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeys[waitingForHotkeyId.Value] : Keys.None,
                                savedHotkeyModifiers.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeyModifiers[waitingForHotkeyId.Value] : 0);
                            waitingForHotkeyButton.Text = oldText;
                            waitingForHotkeyButton.BackColor = System.Drawing.Color.White;
                            waitingForHotkeyButton = null;
                            waitingForHotkeyId = null;
                            currentPressedKeys = Keys.None;
                            maxPressedKeys = Keys.None;
                            pressedKeysSet.Clear();
                            return;
                        }
                    }
                    
                    // Сохраняем новую горячую клавишу в память ПЕРЕД отменой старой
                    savedHotkeys[waitingForHotkeyId.Value] = finalKey;
                    savedHotkeyModifiers[waitingForHotkeyId.Value] = finalModifier;
                    
                    // Сохраняем в файл
                    SaveHotkeysToFile();
                    
                    // Регистрируем новую горячую клавишу через RegisterSingleHotkey
                    // (он сам отменит старую регистрацию перед регистрацией новой)
                    int hotkeyId = waitingForHotkeyId.Value;
                    
                    AddLog($"🔧 Попытка регистрации горячей клавиши {GetHotkeyActionName(hotkeyId)}: key={finalKey}, mod={finalModifier}");
                    
                    // Используем RegisterSingleHotkey для единообразной логики регистрации
                    RegisterSingleHotkey(hotkeyId, finalKey, finalModifier);
                    
                    // Преобразуем Keys в строку
                    string keyString = FormatKeysToString(finalCombination, finalModifier);
                    
                    // Обновляем текст кнопки
                    waitingForHotkeyButton.Text = keyString;
                    waitingForHotkeyButton.BackColor = System.Drawing.Color.White;
                    
                    // Обновляем Tag кнопки
                    waitingForHotkeyButton.Tag = new { HotkeyId = waitingForHotkeyId.Value, Key = finalKey, Mod = finalModifier };
                    
                    AddLog($"⌨️ Установлена горячая клавиша для {GetHotkeyActionName(waitingForHotkeyId.Value)}: {keyString}");
                    
                    // ВАЖНО: Сбрасываем состояние ПОСЛЕ сохранения, чтобы горячие клавиши не активировались на главном экране
                    waitingForHotkeyButton = null;
                    waitingForHotkeyId = null;
                    currentPressedKeys = Keys.None;
                    maxPressedKeys = Keys.None;
                    lastNonModifierKey = Keys.None;
                    pressedKeysSet.Clear();
                    
                    // Убеждаемся, что KeyPreview выключен после настройки
                    this.KeyPreview = false;
                }
                else if (pressedKeysSet.Count > 0)
                {
                    // Обновляем текущие нажатые клавиши (после удаления одной)
                    currentPressedKeys = Keys.None;
                    foreach (var k in pressedKeysSet)
                    {
                        currentPressedKeys |= k;
                    }
                    // Добавляем модификаторы из текущего состояния
                    if (e.KeyData != Keys.None)
                    {
                        currentPressedKeys |= (e.KeyData & Keys.Modifiers);
                    }
                }
            }
        }
        
        private string GetHotkeyActionName(int hotkeyId)
        {
            return hotkeyId switch
            {
                HOTKEY_START_RECORD => "Начать запись",
                HOTKEY_STOP_RECORD => "Сохранить запись",
                HOTKEY_PLAY => "Воспроизвести",
                HOTKEY_PLAY_LOOP => "Воспроизвести в цикле",
                HOTKEY_PAUSE_RECORD => "Пауза записи",
                HOTKEY_PAUSE_PLAYBACK => "Пауза воспроизведения",
                HOTKEY_STOP_PLAY => "Остановить воспроизведение",
                _ => "Неизвестное действие"
            };
        }
        
        
        private string FormatKeysToString(Keys keyData, uint modifier = 0)
        {
            string result = "";
            
            // Используем modifier если он указан, иначе извлекаем из keyData
            uint mods = modifier;
            if (mods == 0)
            {
                mods = GetModifierFromKeys(keyData);
            }
            
            // Проверяем модификаторы
            if ((mods & 0x0002) != 0) // MOD_CONTROL
                result += "Ctrl+";
            if ((mods & 0x0001) != 0) // MOD_ALT
                result += "Alt+";
            if ((mods & 0x0004) != 0) // MOD_SHIFT
                result += "Shift+";
            
            Keys key = keyData & ~Keys.Modifiers;
            
            // Преобразуем клавишу в строку
            if (key >= Keys.F1 && key <= Keys.F12)
            {
                result += key.ToString();
            }
            else if (key == Keys.Pause)
            {
                result += "Pause";
            }
            else if (key == Keys.Home)
            {
                result += "Home";
            }
            else if (key == Keys.Oemtilde || key == Keys.Oem3 || (int)key == 0xC0)
            {
                result += "`";
            }
            else
            {
                string keyStr = key.ToString();
                // Убираем префикс "D" для цифровых клавиш
                if (keyStr.StartsWith("D") && keyStr.Length == 2 && char.IsDigit(keyStr[1]))
                {
                    result += keyStr[1];
                }
                else
                {
                    result += keyStr;
                }
            }
            
            return result;
        }
        
        private uint GetModifierFromKeys(Keys keyData)
        {
            uint mod = MOD_NONE;
            // Поддерживаем комбинации модификаторов
            if ((keyData & Keys.Control) == Keys.Control)
                mod |= 0x0002; // MOD_CONTROL
            if ((keyData & Keys.Alt) == Keys.Alt)
                mod |= 0x0001; // MOD_ALT
            if ((keyData & Keys.Shift) == Keys.Shift)
                mod |= 0x0004; // MOD_SHIFT
            return mod;
        }
        
        private Keys GetKeyWithoutModifiers(Keys keyData)
        {
            // Keys.Modifiers включает Control, Alt, Shift, но может не включать все модификаторы
            // Убираем все возможные модификаторы, чтобы получить чистую клавишу
            return keyData & ~(Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.Modifiers);
        }
        
        private void SaveHotkeysToFile()
        {
            try
            {
                var hotkeyData = new Dictionary<string, object>();
                var keysDict = new Dictionary<string, int>();
                var modsDict = new Dictionary<string, uint>();
                
                foreach (var kvp in savedHotkeys)
                {
                    keysDict[kvp.Key.ToString()] = (int)kvp.Value;
                }
                
                foreach (var kvp in savedHotkeyModifiers)
                {
                    modsDict[kvp.Key.ToString()] = kvp.Value;
                }
                
                hotkeyData["Keys"] = keysDict;
                hotkeyData["Modifiers"] = modsDict;
                
                string json = System.Text.Json.JsonSerializer.Serialize(hotkeyData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                string hotkeysPath = Path.Combine(Application.StartupPath, "hotkeys.json");
                File.WriteAllText(hotkeysPath, json);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при сохранении горячих клавиш: {ex.Message}");
            }
        }
        
        private void LoadHotkeysFromFile()
        {
            try
            {
                string hotkeysPath = Path.Combine(Application.StartupPath, "hotkeys.json");
                if (!File.Exists(hotkeysPath))
                    return;
                
                string json = File.ReadAllText(hotkeysPath);
                var hotkeyData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                
                // Загружаем клавиши
                if (hotkeyData.TryGetProperty("Keys", out var keysElement))
                {
                    foreach (var prop in keysElement.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int hotkeyId) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            int keyValue = prop.Value.GetInt32();
                            Keys loadedKey = (Keys)keyValue;
                            
                            // Проверяем, что это не пробел (0x20 или Keys.Space) для HOTKEY_START_RECORD
                            if (hotkeyId == HOTKEY_START_RECORD && (loadedKey == Keys.Space || keyValue == 0x20))
                            {
                                // Игнорируем пробел, используем значение по умолчанию F9
                                AddLog($"⚠️ Обнаружен пробел для начала записи, заменяем на F9");
                                continue;
                            }
                            
                            // Логируем загруженное значение для отладки
                            Console.WriteLine($"DEBUG LoadHotkeysFromFile: hotkeyId={hotkeyId}, keyValue={keyValue} (0x{keyValue:X}), loadedKey={loadedKey}");
                            savedHotkeys[hotkeyId] = loadedKey;
                        }
                    }
                }
                
                // Загружаем модификаторы
                if (hotkeyData.TryGetProperty("Modifiers", out var modsElement))
                {
                    foreach (var prop in modsElement.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int hotkeyId) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            savedHotkeyModifiers[hotkeyId] = prop.Value.GetUInt32();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при загрузке горячих клавиш: {ex.Message}");
            }
        }
        
        private void ResetHotkeysToDefault()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите вернуть все горячие клавиши к настройкам по умолчанию?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // Очищаем сохраненные горячие клавиши
                savedHotkeys.Clear();
                savedHotkeyModifiers.Clear();
                
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
                AddLog("🔄 Горячие клавиши сброшены к настройкам по умолчанию");
                ShowHotkeysSettings(); // Обновляем UI
            }
        }
        
        private void btnLoadMacroFile_Click(object sender, EventArgs e)
        {
            LoadMacroFromFile();
        }
        
        private void cmbMacroFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbMacroFiles != null && cmbMacroFiles.SelectedItem != null)
            {
                string? selected = cmbMacroFiles.SelectedItem.ToString();
                if (selected == null) return;
                
                if (savedMacroFiles.Contains(selected))
                {
                    currentMacroFile = selected;
                    AddLog($"📋 Выбран файл для воспроизведения: {selected}");
                }
                else if (selected.StartsWith("[Внешний] "))
                {
                    // Внешний файл уже сохранен в currentMacroFile
                    AddLog($"📋 Выбран внешний файл для воспроизведения: {Path.GetFileName(currentMacroFile)}");
                }
            }
        }
        
        private string? ShowInputDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Text = text };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 230, Width = 70, Top = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Отмена", Left = 310, Width = 70, Top = 80, DialogResult = DialogResult.Cancel };
            
            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };
            
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;
            
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }
    }
}