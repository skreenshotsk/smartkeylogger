// MainForm.cs - Основной файл с инициализацией, обработчиками событий и методами, которые еще не вынесены
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm : Form
    {
        // Основные компоненты
        private MacroRecorder? recorder;
        private MacroPlayer? player;
        private CancellationTokenSource? playbackCancellation;
        private bool isPlaying = false;
        private bool isRecording = false;
        
        // Таймеры
        private System.Windows.Forms.Timer? recordingTimer;
        private System.Windows.Forms.Timer? playbackTimer;
        private System.Windows.Forms.Timer? scheduleTimer;
        
        // Планировщик выполнения
        private DateTime? scheduledDateTime = null;
        private string? scheduledMacroFile = null;
        private bool isWaitingForSchedule = false;
        
        // Управление файлами записей
        private List<string> savedMacroFiles = new List<string>();
        private string currentMacroFile = "";
        private string macrosDirectory = "";
        
        // Состояние редактирования макросов
        private List<InputEvent>? currentEditingEvents = null;
        private string? currentEditingMacroFile = null;
        private long? currentEditingKeyboardLayout = null;
        private long? currentEditingDurationTicks = null;
        private bool skipDeleteConfirmation = false;
        private bool isAppendingToMacro = false;
        private int actionToReplaceIndex = -1;
        private int mouseMovesStartIndex = -1;
        private int mouseMovesCountToReplace = 0;
        private bool isReplacingAction = false;
        private bool isReplacingMouseMoves = false;
        private Panel? currentMacroDetailsPanel = null;
        
        // UI компоненты настроек
        private Button? btnTabHotkeys = null;
        private Button? btnTabMacros = null;
        private Panel? settingsContentPanel = null;

        public MainForm()
        {
            InitializeComponent();
            InitializeComponents();
            LoadHotkeysFromFile();
            RegisterGlobalHotkeys();
            InitializeTimers();
            
            // Подключаем обработчики для отслеживания клавиш при настройке горячих клавиш
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
        }

        private void InitializeTimers()
        {
            // Таймер записи
            recordingTimer = new System.Windows.Forms.Timer();
            recordingTimer.Interval = 100;
            recordingTimer.Tick += RecordingTimer_Tick!;

            // Таймер воспроизведения
            playbackTimer = new System.Windows.Forms.Timer();
            playbackTimer.Interval = 100;
            playbackTimer.Tick += PlaybackTimer_Tick!;

            // Таймер планировщика
            scheduleTimer = new System.Windows.Forms.Timer();
            scheduleTimer.Interval = 1000;
            scheduleTimer.Tick += ScheduleTimer_Tick!;
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
                                StopRecording();
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
            else if (keyData == (Keys.F12 | Keys.Control) && !isRecording && !isPlaying)
            {
                PlayMacroLoop();
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

        // Обработчики событий UI
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
                
                // Сохраняем запланированное выполнение
                scheduledDateTime = selectedDateTime;
                scheduledMacroFile = fileToSchedule;
                isWaitingForSchedule = true;

                // Запускаем таймер планировщика
                scheduleTimer?.Start();

                // Обновляем UI
                UpdateUI();

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
            panelLogs.Visible = false;
            panelSettings.Visible = true;
            panelSettings.BringToFront();
            
            AddLog("⚙️ Открыто меню настроек");
        }
        
        private void HideSettingsPanel()
        {
            if (panelSettings == null) return;
            
            panelMain.Visible = true;
            panelLogs.Visible = true;
            panelSettings.Visible = false;
            AddLog("◀ Возврат на главный экран");
        }
        
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
            
            // Контейнер для содержимого
            settingsContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                AutoScroll = true
            };
            
            // Панель с кнопкой "Назад" и вкладками
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
            
            // Вкладки
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
            
            // Header
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
            
            // Добавляем в обратном порядке из-за Dock.Top
            panelSettings.Controls.Add(settingsContentPanel);
            panelSettings.Controls.Add(tabsPanel);
            panelSettings.Controls.Add(headerPanel);
            
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
        
        private void RefreshMacroDetailsPanel()
        {
            // Всегда обновляем, даже если настройки не видны
            if (currentEditingMacroFile != null && currentMacroDetailsPanel != null)
            {
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
            
            var recordedEvents = recorder.GetRecordedEvents();
            if (recordedEvents == null || recordedEvents.Count == 0)
            {
                AddLog("⚠️ Не записано ни одного действия для добавления");
                isAppendingToMacro = false;
                return;
            }
            
            long maxTimestamp = currentEditingEvents.Count > 0 
                ? currentEditingEvents.Max(e => e.Timestamp) 
                : 0;
            
            foreach (var evt in recordedEvents)
            {
                var newEvent = new InputEvent
                {
                    EventType = evt.EventType,
                    Timestamp = maxTimestamp + evt.Timestamp + 10000000,
                    X = evt.X,
                    Y = evt.Y,
                    KeyCode = evt.KeyCode,
                    MouseButton = evt.MouseButton,
                    IsPressed = evt.IsPressed
                };
                currentEditingEvents.Add(newEvent);
            }
            
            if (currentEditingMacroFile != null)
            {
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                AddLog($"➕ Добавлено {recordedEvents.Count} действий в макрос {currentEditingMacroFile}");
            }
            
            isAppendingToMacro = false;
            
            if (panelSettings != null && !panelSettings.Visible)
            {
                ShowSettingsPanel();
            }
            
            if (btnTabMacros != null && settingsContentPanel != null)
            {
                SwitchTab("macros");
                ShowMacrosSettings();
                
                if (currentEditingMacroFile != null)
                {
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
            
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                dialog.Title = "Сохранить запись макроса";
                dialog.FileName = GenerateMacroFileName();
                
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
                    string defaultName = Path.GetFileNameWithoutExtension(GenerateMacroFileName());
                    string? userFileName = ShowInputDialog("Введите имя файла (без расширения):", "Имя файла", defaultName);
                    
                    if (string.IsNullOrWhiteSpace(userFileName))
                    {
                        userFileName = defaultName;
                    }
                    else
                    {
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
                                currentMacroFile = savedPath;
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
                
                if (savedMacroFiles.Contains(selected))
                {
                    return Path.Combine(macrosDirectory, selected);
                }
                
                if (selected.StartsWith("[Внешний] "))
                {
                    return currentMacroFile;
                }
            }
            
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
                        if (!File.Exists(filePath))
                        {
                            AddLog($"❌ Файл не существует: {filePath}");
                            MessageBox.Show("Выбранный файл не существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
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
                        
                        string fileName = Path.GetFileName(filePath);
                        
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
                            currentMacroFile = filePath;
                        }
                        
                        AddLog($"✅ Файл успешно загружен: {fileName}");
                        AddLog($"📁 Полный путь: {filePath}");
                        if (events != null)
                        {
                            AddLog($"📊 Событий в файле: {events.Count}");
                        }
                        AddLog($"▶ Файл установлен для воспроизведения");
                        
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
                    AddLog($"📋 Выбран внешний файл для воспроизведения: {Path.GetFileName(currentMacroFile)}");
                }
                
                // Обновляем состояние кнопок после выбора файла
                UpdateUI();
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

        // Вспомогательные методы, которые используются в других модулях
        // Эти методы будут реализованы в других partial файлах
    }
}
