// MainForm.cs
using System;
using System.Collections.Generic;
using System.IO;
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
        
        // Управление файлами записей
        private List<string> savedMacroFiles = new List<string>(); // Список файлов в директории приложения
        private string currentMacroFile = ""; // Текущий выбранный файл для воспроизведения
        private string macrosDirectory = ""; // Директория для сохранения макросов

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
            InitializeTimers(); // НОВЫЙ вызов
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
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            UpdateRecordingTime();
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlaybackTime();
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
        
        // Добавляем константы для модификаторов
        const int MOD_CONTROL = 0x0002;
        const int MOD_ALT = 0x0001;
        
        // Регистрируем глобальные горячие клавиши
        bool result;
        
        result = RegisterHotKey(this.Handle, HOTKEY_START_RECORD, MOD_NONE, (uint)Keys.F9);
        Console.WriteLine($"F9 регистрация: {result}");
        
        result = RegisterHotKey(this.Handle, HOTKEY_STOP_RECORD, MOD_NONE, (uint)Keys.F10);
        Console.WriteLine($"F10 регистрация: {result}");
        
        result = RegisterHotKey(this.Handle, HOTKEY_PLAY, MOD_NONE, (uint)Keys.F11);
        Console.WriteLine($"F11 регистрация: {result}");
        
        // ИЗМЕНЕНИЕ: Ctrl+F12 для цикла вместо F12
        result = RegisterHotKey(this.Handle, HOTKEY_PLAY_LOOP, MOD_CONTROL, (uint)Keys.F12);
        Console.WriteLine($"Ctrl+F12 регистрация: {result}");
        
        // Если не сработало Ctrl+F12, пробуем Alt+F12
        if (!result)
        {
            result = RegisterHotKey(this.Handle, HOTKEY_PLAY_LOOP, MOD_ALT, (uint)Keys.F12);
            Console.WriteLine($"Alt+F12 регистрация: {result}");
        }
        
        result = RegisterHotKey(this.Handle, HOTKEY_STOP_PLAY, MOD_NONE, VK_BACKQUOTE);
        Console.WriteLine($"Backquote регистрация: {result}");
        
        result = RegisterHotKey(this.Handle, HOTKEY_PAUSE_RECORD, MOD_NONE, VK_PAUSE);
        Console.WriteLine($"Pause регистрация: {result}");
        
        result = RegisterHotKey(this.Handle, HOTKEY_PAUSE_PLAYBACK, MOD_NONE, VK_HOME);
        Console.WriteLine($"Home регистрация: {result}");
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
            
            // Показываем диалог сохранения
            SaveRecordingDialog();
            
            UpdateUI();
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
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
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
            else
            {
                MessageBox.Show("Нет записанных событий для воспроизведения!", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
            if (events != null && events.Count > 0)
            {
                isPlaying = true;
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
                lblStatus.Text = "● ГОТОВ К РАБОТЕ";
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
                lblStatus.BackColor = System.Drawing.Color.FromArgb(227, 242, 253);
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
            // Кнопка паузы доступна только после начала реального воспроизведения
            btnPausePlayback.Enabled = isPlaying && !isRecording && (player?.PlaybackStarted ?? false);

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
            MessageBox.Show("Горячие клавиши работают даже когда окно свернуто!\n\n" +
                           "Для остановки воспроизведения нажмите ` (Backquote) - клавиша над Tab\n" +
                           "Для паузы записи нажмите Pause/Break\n" +
                           "Для паузы воспроизведения нажмите Home\n" +
                           "Обычно находится слева от цифры 1 на стандартной клавиатуре", 
                           "Информация", 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Information);
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