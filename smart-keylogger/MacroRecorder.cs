// Modified MacroRecorder.cs - с фильтрацией горячих клавиш и поддержкой паузы
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MacroRecorder
{
    private TaskPoolGlobalHook? hook;
    private readonly object _lock = new object();
    private List<InputEvent> recordedEvents = new List<InputEvent>();
    private bool isRecording = false;
    private bool isPaused = false; // НОВОЕ поле для состояния паузы
    private DateTime recordingStartTime;
    private DateTime pauseStartTime;
    private long totalPauseDuration = 0; // Общее время паузы в тиках
    private long? recordedKeyboardLayout = null;
    private bool trackLanguage = false;
    private long? loadedDurationTicks = null; // Длительность загруженного макроса

    // НОВЫЕ свойства для таймера записи
    public TimeSpan RecordingDuration 
    { 
        get 
        {
            if (!isRecording) return TimeSpan.Zero;
            
            TimeSpan elapsed = DateTime.Now - recordingStartTime;
            TimeSpan pauseTime = TimeSpan.FromTicks(totalPauseDuration);
            
            // Если сейчас на паузе, добавляем текущее время паузы
            if (isPaused)
            {
                pauseTime += (DateTime.Now - pauseStartTime);
            }
            
            return elapsed - pauseTime;
        } 
    }

    public bool IsRecording => isRecording;

    // Список кодов горячих клавиш которые нужно игнорировать
    // В списке игнорируемых кодов клавиш добавить:
    private HashSet<int> ignoredKeyCodes = new HashSet<int>
    {
        0x78, // F9
        0x79, // F10  
        0x7A, // F11
        0x7B, // F12
        0xC0, // ` (~)
        0x13, // Pause/Break
        0x24  // Home - НОВАЯ игнорируемая клавиша для паузы воспроизведения
    };

    // НОВОЕ свойство для проверки состояния паузы
    public bool IsPaused => isPaused;
    
    // Callback для логирования
    private Action<string>? logCallback;
    
    public void SetLogCallback(Action<string>? callback)
    {
        logCallback = callback;
    }
    
    private void Log(string message)
    {
        logCallback?.Invoke(message);
    }

    // Конвертер кодов прямо внутри класса
    private int SharpHookToVirtualKey(KeyCode sharpHookKeyCode)
    {
        return sharpHookKeyCode switch
        {
            // Цифры
            KeyCode.Vc0 => 0x30, KeyCode.Vc1 => 0x31, KeyCode.Vc2 => 0x32,
            KeyCode.Vc3 => 0x33, KeyCode.Vc4 => 0x34, KeyCode.Vc5 => 0x35,
            KeyCode.Vc6 => 0x36, KeyCode.Vc7 => 0x37, KeyCode.Vc8 => 0x38,
            KeyCode.Vc9 => 0x39,

            // Буквы
            KeyCode.VcA => 0x41, KeyCode.VcB => 0x42, KeyCode.VcC => 0x43,
            KeyCode.VcD => 0x44, KeyCode.VcE => 0x45, KeyCode.VcF => 0x46,
            KeyCode.VcG => 0x47, KeyCode.VcH => 0x48, KeyCode.VcI => 0x49,
            KeyCode.VcJ => 0x4A, KeyCode.VcK => 0x4B, KeyCode.VcL => 0x4C,
            KeyCode.VcM => 0x4D, KeyCode.VcN => 0x4E, KeyCode.VcO => 0x4F,
            KeyCode.VcP => 0x50, KeyCode.VcQ => 0x51, KeyCode.VcR => 0x52,
            KeyCode.VcS => 0x53, KeyCode.VcT => 0x54, KeyCode.VcU => 0x55,
            KeyCode.VcV => 0x56, KeyCode.VcW => 0x57, KeyCode.VcX => 0x58,
            KeyCode.VcY => 0x59, KeyCode.VcZ => 0x5A,

            // Функциональные клавиши
            KeyCode.VcF1 => 0x70, KeyCode.VcF2 => 0x71, KeyCode.VcF3 => 0x72,
            KeyCode.VcF4 => 0x73, KeyCode.VcF5 => 0x74, KeyCode.VcF6 => 0x75,
            KeyCode.VcF7 => 0x76, KeyCode.VcF8 => 0x77, KeyCode.VcF9 => 0x78,
            KeyCode.VcF10 => 0x79, KeyCode.VcF11 => 0x7A, KeyCode.VcF12 => 0x7B,

            // Специальные клавиши
            KeyCode.VcEscape => 0x1B, KeyCode.VcEnter => 0x0D, KeyCode.VcSpace => 0x20,
            KeyCode.VcBackspace => 0x08, KeyCode.VcTab => 0x09, KeyCode.VcCapsLock => 0x14,

            // Модификаторы
            KeyCode.VcLeftShift => 0xA0, KeyCode.VcRightShift => 0xA1,
            KeyCode.VcLeftControl => 0xA2, KeyCode.VcRightControl => 0xA3,
            KeyCode.VcLeftAlt => 0xA4, KeyCode.VcRightAlt => 0xA5,

            // Навигация
            KeyCode.VcInsert => 0x2D, KeyCode.VcDelete => 0x2E, KeyCode.VcHome => 0x24,
            KeyCode.VcEnd => 0x23, KeyCode.VcPageUp => 0x21, KeyCode.VcPageDown => 0x22,

            // Стрелки
            KeyCode.VcUp => 0x26, KeyCode.VcDown => 0x28,
            KeyCode.VcLeft => 0x25, KeyCode.VcRight => 0x27,
            // Знаки препинания
            KeyCode.VcComma => 0xBC,    // , (188)
            KeyCode.VcPeriod => 0xBE,   // . (190)
            KeyCode.VcSlash => 0xBF,    // / (191)
            KeyCode.VcBackQuote => 0xC0, // `
            KeyCode.VcMinus => 0xBD,    // -
            KeyCode.VcEquals => 0xBB,   // =
            KeyCode.VcBackslash => 0xDC, // \
            KeyCode.VcOpenBracket => 0xDB, // [
            KeyCode.VcCloseBracket => 0xDD, // ]
            KeyCode.VcSemicolon => 0xBA, // ;
            KeyCode.VcQuote => 0xDE,    // '
            // Другие
            KeyCode.VcPrintScreen => 0x2C, // Print Screen
            KeyCode.VcLeftMeta => 0x5B,
            KeyCode.VcRightMeta => 0x5C,
            KeyCode.VcPause => 0x13,    // Pause/Break
            // По умолчанию оставляем как есть
            _ => (int)sharpHookKeyCode
        };
    }

    public void StartRecording(bool trackLang = false)
    {
        recordedEvents.Clear();
        isRecording = true;
        isPaused = false;
        totalPauseDuration = 0;
        recordingStartTime = DateTime.Now;
        trackLanguage = trackLang;
        recordedKeyboardLayout = null;
        savedDurationTicks = null;
        loadedDurationTicks = null;
        
        if (trackLanguage)
        {
            IntPtr hwnd = WinAPI.GetForegroundWindow();
            uint threadId = WinAPI.GetWindowThreadProcessId(hwnd, out _);
            IntPtr layout = WinAPI.GetKeyboardLayout(threadId);
            recordedKeyboardLayout = layout.ToInt64();
            string langName = GetLanguageName(layout);
            Log($"🌐 Язык клавиатуры сохранен: {langName} (0x{layout.ToInt64():X8})");
        }
        
        hook = new TaskPoolGlobalHook();
        
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;
        hook.MouseMoved += OnMouseMoved;
        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        
        Log("▶ Начата запись макроса");
        Console.WriteLine("Запись начата! Нажмите Enter в этом окне чтобы остановить и сохранить...");
        hook.RunAsync();
    }

    public void PauseRecording()
    {
        if (!isRecording || isPaused) return;
        
        isPaused = true;
        pauseStartTime = DateTime.Now;
        
        // получаем текущую длительность записи
        TimeSpan currentDuration = RecordingDuration;
        string timeText = $"{currentDuration:mm\\:ss\\.ff}";
        
        Log($"⏸ Запись приостановлена на {timeText}");
        Console.WriteLine($"Запись приостановлена на {timeText}");
    }

    public void ResumeRecording()
    {
        if (!isRecording || !isPaused) return;
        
        isPaused = false;
        totalPauseDuration += (DateTime.Now - pauseStartTime).Ticks;
        Log($"▶ Запись возобновлена");
        Console.WriteLine("Запись возобновлена");
    }

    private long? savedDurationTicks = null; // Сохраняем длительность для использования при сохранении

    public void StopRecording()
    {
        // получаем длительность записи
        TimeSpan recordingDuration = RecordingDuration;
        string durationText = $"{recordingDuration:mm\\:ss\\.ff}";
        
        // Сохраняем длительность в тиках для использования при сохранении
        savedDurationTicks = recordingDuration.Ticks;

        isRecording = false;
        isPaused = false;
        hook?.Dispose();
        hook = null;
        
        Log($"⏹ Запись остановлена. Длительность записи: {durationText}");
        Console.WriteLine($"Запись остановлена. Длительность: {durationText}");
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording || isPaused) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        long adjustedTicks = elapsed.Ticks - totalPauseDuration;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseMove",
            X = e.Data.X,
            Y = e.Data.Y,
            Timestamp = adjustedTicks
        });
        // Движения мыши не логируем
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording || isPaused) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        long adjustedTicks = elapsed.Ticks - totalPauseDuration;
        
        string buttonName = e.Data.Button switch
        {
            MouseButton.Button1 => "ЛКМ",
            MouseButton.Button2 => "ПКМ",
            MouseButton.Button3 => "СКМ",
            _ => "Кнопка"
        };
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = true,
            Timestamp = adjustedTicks
        });
        
        Log($"🖱️ {buttonName} нажата ({e.Data.X}, {e.Data.Y})");
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording || isPaused) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        long adjustedTicks = elapsed.Ticks - totalPauseDuration;
        
        string buttonName = e.Data.Button switch
        {
            MouseButton.Button1 => "ЛКМ",
            MouseButton.Button2 => "ПКМ",
            MouseButton.Button3 => "СКМ",
            _ => "Кнопка"
        };
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = false,
            Timestamp = adjustedTicks
        });
        
        Log($"🖱️ {buttonName} отпущена ({e.Data.X}, {e.Data.Y})");
    }

    public bool IsIgnoredKey(int keyCode)
    {
        return ignoredKeyCodes.Contains(keyCode);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording || isPaused) return;
        
        int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
        
        // Пропускаем горячие клавиши
        if (IsIgnoredKey(virtualKeyCode))
        {
            Console.WriteLine($"Пропущена горячая клавиша: VK=0x{virtualKeyCode:X2}");
            return;
        }
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        long adjustedTicks = elapsed.Ticks - totalPauseDuration;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = virtualKeyCode,
            IsPressed = true,
            Timestamp = adjustedTicks
        });
        
        string keyName = GetKeyName(virtualKeyCode);
        Log($"⌨️ {keyName}");
        Console.WriteLine($"Записана клавиша: VK=0x{virtualKeyCode:X2}, SharpHook={e.Data.KeyCode}");
    }
    
    private string GetKeyName(int keyCode)
    {
        // Преобразуем код клавиши в читаемое имя (английские буквы)
        if (keyCode >= 0x30 && keyCode <= 0x39) return ((char)keyCode).ToString(); // Цифры
        if (keyCode >= 0x41 && keyCode <= 0x5A) return ((char)keyCode).ToString(); // Буквы (английские)
        
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
            0xA0 => "Shift",
            0xA1 => "Shift",
            0xA2 => "Ctrl",
            0xA3 => "Ctrl",
            0xA4 => "Alt",
            0xA5 => "Alt",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xBA => ";",
            0xBB => "=",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => $"Key(0x{keyCode:X2})"
        };
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording || isPaused) return;
        
        int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
        
        // Пропускаем горячие клавиши
        if (IsIgnoredKey(virtualKeyCode))
        {
            Console.WriteLine($"Пропущена горячая клавиша: VK=0x{virtualKeyCode:X2}");
            return;
        }
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        long adjustedTicks = elapsed.Ticks - totalPauseDuration;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = virtualKeyCode,
            IsPressed = false,
            Timestamp = adjustedTicks
        });
        // Отпускание клавиш не логируем отдельно
    }

    public void SaveToFile(string filename = "macro.json")
    {
        try
        {
            // Фильтруем события горячих клавиш на всякий случай
            var filteredEvents = recordedEvents.Where(e => 
                e.EventType != "KeyPress" || !ignoredKeyCodes.Contains(e.KeyCode)).ToList();

            var macroData = new MacroData
            {
                Events = filteredEvents,
                KeyboardLayout = recordedKeyboardLayout,
                DurationTicks = savedDurationTicks
            };

            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(macroData, options);
            File.WriteAllText(filename, json);
            Console.WriteLine($"Макрос сохранен в файл: {filename} ({filteredEvents.Count} событий)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
        }
    }

    public List<InputEvent>? LoadFromFile(string filename = "macro.json")
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Файл {filename} не существует!");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filename);
            
            // Пытаемся загрузить как MacroData (новый формат)
            try
            {
                var macroData = System.Text.Json.JsonSerializer.Deserialize<MacroData>(json, new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                if (macroData != null && macroData.Events != null)
                {
                    recordedKeyboardLayout = macroData.KeyboardLayout;
                    loadedDurationTicks = macroData.DurationTicks; // Сохраняем длительность
                    if (recordedKeyboardLayout.HasValue)
                    {
                        IntPtr layout = new IntPtr(recordedKeyboardLayout.Value);
                        string langName = GetLanguageName(layout);
                        Log($"🌐 Загружен сохраненный язык: {langName}");
                    }
                    return macroData.Events;
                }
            }
            catch
            {
                // Если не получилось, пробуем старый формат
            }
            
            // Старый формат - просто список событий
            return System.Text.Json.JsonSerializer.Deserialize<List<InputEvent>>(json, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            return null;
        }
    }
    
    public IntPtr? GetRecordedKeyboardLayout()
    {
        if (recordedKeyboardLayout.HasValue)
        {
            return new IntPtr(recordedKeyboardLayout.Value);
        }
        return null;
    }
    
    private string GetLanguageName(IntPtr layout)
    {
        long layoutId = layout.ToInt64() & 0xFFFF;
        // Основные коды языков
        return layoutId switch
        {
            0x0419 => "Русский", // ru-RU
            0x0409 => "Английский (US)", // en-US
            0x0407 => "Немецкий", // de-DE
            0x040C => "Французский", // fr-FR
            0x0410 => "Итальянский", // it-IT
            0x0405 => "Чешский", // cs-CZ
            0x0415 => "Польский", // pl-PL
            0x0412 => "Корейский", // ko-KR
            0x0411 => "Японский", // ja-JP
            0x0804 => "Китайский (упрощенный)", // zh-CN
            0x0404 => "Китайский (традиционный)", // zh-TW
            _ => $"Язык (0x{layoutId:X4})"
        };
    }

    // Метод для добавления игнорируемых клавиш
    public void AddIgnoredKey(int keyCode)
    {
        ignoredKeyCodes.Add(keyCode);
    }

    public int GetRecordedEventsCount()
    {
        return recordedEvents.Count;
    }
    
    public long? GetRecordedDurationTicks()
    {
        return loadedDurationTicks;
    }
}