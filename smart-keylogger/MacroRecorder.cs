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
    private bool isPaused = false;
    private DateTime recordingStartTime;
    private DateTime pauseStartTime;
    private long totalPauseDuration = 0;
    private long? recordedKeyboardLayout = null;
    private bool trackLanguage = false;
    private long? loadedDurationTicks = null;
    private long? savedDurationTicks = null;

    public TimeSpan RecordingDuration
    {
        get
        {
            lock (_lock)
            {
                if (!isRecording) return TimeSpan.Zero;

                TimeSpan elapsed = DateTime.Now - recordingStartTime;
                TimeSpan pauseTime = TimeSpan.FromTicks(totalPauseDuration);

                if (isPaused)
                {
                    pauseTime += (DateTime.Now - pauseStartTime);
                }

                return elapsed - pauseTime;
            }
        }
    }

    public bool IsRecording
    {
        get
        {
            lock (_lock)
            {
                return isRecording;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                return isPaused;
            }
        }
    }

    //коды горячих клавиш приложения — не попадают в макрос
    private HashSet<int> ignoredKeyCodes = new HashSet<int>
    {
        0x78, 0x79, 0x7A, 0x7B, 0xC0, 0x13, 0x24
    };

    private Action<string>? logCallback;

    public void SetLogCallback(Action<string>? callback)
    {
        logCallback = callback;
    }

    private void Log(string message)
    {
        logCallback?.Invoke(message);
    }

    private int SharpHookToVirtualKey(KeyCode sharpHookKeyCode)
    {
        return sharpHookKeyCode switch
        {
            KeyCode.Vc0 => 0x30, KeyCode.Vc1 => 0x31, KeyCode.Vc2 => 0x32,
            KeyCode.Vc3 => 0x33, KeyCode.Vc4 => 0x34, KeyCode.Vc5 => 0x35,
            KeyCode.Vc6 => 0x36, KeyCode.Vc7 => 0x37, KeyCode.Vc8 => 0x38,
            KeyCode.Vc9 => 0x39,
            KeyCode.VcA => 0x41, KeyCode.VcB => 0x42, KeyCode.VcC => 0x43,
            KeyCode.VcD => 0x44, KeyCode.VcE => 0x45, KeyCode.VcF => 0x46,
            KeyCode.VcG => 0x47, KeyCode.VcH => 0x48, KeyCode.VcI => 0x49,
            KeyCode.VcJ => 0x4A, KeyCode.VcK => 0x4B, KeyCode.VcL => 0x4C,
            KeyCode.VcM => 0x4D, KeyCode.VcN => 0x4E, KeyCode.VcO => 0x4F,
            KeyCode.VcP => 0x50, KeyCode.VcQ => 0x51, KeyCode.VcR => 0x52,
            KeyCode.VcS => 0x53, KeyCode.VcT => 0x54, KeyCode.VcU => 0x55,
            KeyCode.VcV => 0x56, KeyCode.VcW => 0x57, KeyCode.VcX => 0x58,
            KeyCode.VcY => 0x59, KeyCode.VcZ => 0x5A,
            KeyCode.VcF1 => 0x70, KeyCode.VcF2 => 0x71, KeyCode.VcF3 => 0x72,
            KeyCode.VcF4 => 0x73, KeyCode.VcF5 => 0x74, KeyCode.VcF6 => 0x75,
            KeyCode.VcF7 => 0x76, KeyCode.VcF8 => 0x77, KeyCode.VcF9 => 0x78,
            KeyCode.VcF10 => 0x79, KeyCode.VcF11 => 0x7A, KeyCode.VcF12 => 0x7B,
            KeyCode.VcEscape => 0x1B, KeyCode.VcEnter => 0x0D, KeyCode.VcSpace => 0x20,
            KeyCode.VcBackspace => 0x08, KeyCode.VcTab => 0x09, KeyCode.VcCapsLock => 0x14,
            KeyCode.VcLeftShift => 0xA0, KeyCode.VcRightShift => 0xA1,
            KeyCode.VcLeftControl => 0xA2, KeyCode.VcRightControl => 0xA3,
            KeyCode.VcLeftAlt => 0xA4, KeyCode.VcRightAlt => 0xA5,
            KeyCode.VcInsert => 0x2D, KeyCode.VcDelete => 0x2E, KeyCode.VcHome => 0x24,
            KeyCode.VcEnd => 0x23, KeyCode.VcPageUp => 0x21, KeyCode.VcPageDown => 0x22,
            KeyCode.VcUp => 0x26, KeyCode.VcDown => 0x28,
            KeyCode.VcLeft => 0x25, KeyCode.VcRight => 0x27,
            KeyCode.VcComma => 0xBC, KeyCode.VcPeriod => 0xBE, KeyCode.VcSlash => 0xBF,
            KeyCode.VcBackQuote => 0xC0, KeyCode.VcMinus => 0xBD, KeyCode.VcEquals => 0xBB,
            KeyCode.VcBackslash => 0xDC, KeyCode.VcOpenBracket => 0xDB, KeyCode.VcCloseBracket => 0xDD,
            KeyCode.VcSemicolon => 0xBA, KeyCode.VcQuote => 0xDE,
            KeyCode.VcPrintScreen => 0x2C,
            KeyCode.VcLeftMeta => 0x5B, KeyCode.VcRightMeta => 0x5C,
            KeyCode.VcPause => 0x13,
            _ => (int)sharpHookKeyCode
        };
    }

    public void StartRecording(bool trackLang = false)
    {
        lock (_lock)
        {
            if (isRecording)
            {
                Log("⚠️ Запись уже идёт");
                return;
            }

            if (hook != null)
            {
                try
                {
                    hook.MousePressed -= OnMousePressed;
                    hook.MouseReleased -= OnMouseReleased;
                    hook.MouseMoved -= OnMouseMoved;
                    hook.KeyPressed -= OnKeyPressed;
                    hook.KeyReleased -= OnKeyReleased;
                    hook.Dispose();
                }
                catch { }
                hook = null;
            }

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

            try
            {
                hook = new TaskPoolGlobalHook();
                hook.MousePressed += OnMousePressed;
                hook.MouseReleased += OnMouseReleased;
                hook.MouseMoved += OnMouseMoved;
                hook.KeyPressed += OnKeyPressed;
                hook.KeyReleased += OnKeyReleased;
                Log("▶ Начата запись макроса");
                hook.RunAsync();
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка при создании hook: {ex.Message}");
                isRecording = false;
                hook = null;
                throw;
            }
        }
    }

    public void PauseRecording()
    {
        lock (_lock)
        {
            if (!isRecording || isPaused) return;

            isPaused = true;
            pauseStartTime = DateTime.Now;
            TimeSpan currentDuration = RecordingDuration;
            string timeText = $"{currentDuration:mm\\:ss\\.ff}";
            Log($"⏸ Запись приостановлена на {timeText}");
        }
    }

    public void ResumeRecording()
    {
        lock (_lock)
        {
            if (!isRecording || !isPaused) return;

            isPaused = false;
            totalPauseDuration += (DateTime.Now - pauseStartTime).Ticks;
            Log("▶ Запись возобновлена");
        }
    }

    public void StopRecording()
    {
        TaskPoolGlobalHook? hookToDispose = null;
        string durationText = "00:00.00";

        try
        {
            lock (_lock)
            {
                if (!isRecording) return;

                hookToDispose = hook;

                try
                {
                    TimeSpan recordingDuration = RecordingDuration;
                    durationText = $"{recordingDuration:mm\\:ss\\.ff}";
                    savedDurationTicks = recordingDuration.Ticks;
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка при получении длительности записи: {ex.Message}");
                    savedDurationTicks = 0;
                }

                if (hookToDispose != null)
                {
                    try
                    {
                        hookToDispose.MousePressed -= OnMousePressed;
                        hookToDispose.MouseReleased -= OnMouseReleased;
                        hookToDispose.MouseMoved -= OnMouseMoved;
                        hookToDispose.KeyPressed -= OnKeyPressed;
                        hookToDispose.KeyReleased -= OnKeyReleased;
                    }
                    catch { }
                }

                isRecording = false;
                isPaused = false;
                hook = null;
            }

            if (hookToDispose != null)
            {
                try
                {
                    hookToDispose.Dispose();
                }
                catch { }
            }

            Log($"⏹ Запись остановлена. Длительность записи: {durationText}");
        }
        catch (Exception ex)
        {
            Log($"❌ Критическая ошибка при остановке записи: {ex.Message}");

            lock (_lock)
            {
                isRecording = false;
                isPaused = false;
                hook = null;
            }

            if (hookToDispose != null)
            {
                try
                {
                    hookToDispose.Dispose();
                }
                catch { }
            }
        }
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        try
        {
            lock (_lock)
            {
                if (!isRecording || isPaused || hook == null) return;

                TimeSpan elapsed = DateTime.Now - recordingStartTime;
                long adjustedTicks = elapsed.Ticks - totalPauseDuration;

                recordedEvents.Add(new InputEvent
                {
                    EventType = "MouseMove",
                    X = e.Data.X,
                    Y = e.Data.Y,
                    Timestamp = adjustedTicks
                });
            }
        }
        catch { }
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        try
        {
            lock (_lock)
            {
                if (!isRecording || isPaused || hook == null) return;

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
        }
        catch { }
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        try
        {
            lock (_lock)
            {
                if (!isRecording || isPaused || hook == null) return;

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
        }
        catch { }
    }

    private bool IsIgnoredKey(int keyCode)
    {
        return ignoredKeyCodes.Contains(keyCode);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            lock (_lock)
            {
                if (!isRecording || isPaused || hook == null) return;

                int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
                if (IsIgnoredKey(virtualKeyCode)) return;

                TimeSpan elapsed = DateTime.Now - recordingStartTime;
                long adjustedTicks = elapsed.Ticks - totalPauseDuration;

                recordedEvents.Add(new InputEvent
                {
                    EventType = "KeyPress",
                    KeyCode = virtualKeyCode,
                    IsPressed = true,
                    Timestamp = adjustedTicks
                });

                Log($"⌨️ {GetKeyName(virtualKeyCode)}");
            }
        }
        catch { }
    }

    private string GetKeyName(int keyCode)
    {
        if (keyCode >= 0x30 && keyCode <= 0x39) return ((char)keyCode).ToString();
        if (keyCode >= 0x41 && keyCode <= 0x5A) return ((char)keyCode).ToString();

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
        try
        {
            lock (_lock)
            {
                if (!isRecording || isPaused || hook == null) return;

                int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
                if (IsIgnoredKey(virtualKeyCode)) return;

                TimeSpan elapsed = DateTime.Now - recordingStartTime;
                long adjustedTicks = elapsed.Ticks - totalPauseDuration;

                recordedEvents.Add(new InputEvent
                {
                    EventType = "KeyPress",
                    KeyCode = virtualKeyCode,
                    IsPressed = false,
                    Timestamp = adjustedTicks
                });
            }
        }
        catch { }
    }

    public void SaveToFile(string filename = "macro.json")
    {
        try
        {
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
        }
        catch (Exception ex)
        {
            Log($"❌ Ошибка при сохранении: {ex.Message}");
        }
    }

    public List<InputEvent>? LoadFromFile(string filename = "macro.json")
    {
        if (!File.Exists(filename))
        {
            Log($"❌ Файл {filename} не существует");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filename);

            try
            {
                var macroData = System.Text.Json.JsonSerializer.Deserialize<MacroData>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                if (macroData != null && macroData.Events != null)
                {
                    recordedKeyboardLayout = macroData.KeyboardLayout;
                    loadedDurationTicks = macroData.DurationTicks;
                    if (recordedKeyboardLayout.HasValue)
                    {
                        IntPtr layout = new IntPtr(recordedKeyboardLayout.Value);
                        Log($"🌐 Загружен сохраненный язык: {GetLanguageName(layout)}");
                    }
                    return macroData.Events;
                }
            }
            catch { }

            return System.Text.Json.JsonSerializer.Deserialize<List<InputEvent>>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            Log($"❌ Ошибка при загрузке: {ex.Message}");
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
        return layoutId switch
        {
            0x0419 => "Русский",
            0x0409 => "Английский (US)",
            0x0407 => "Немецкий",
            0x040C => "Французский",
            0x0410 => "Итальянский",
            0x0405 => "Чешский",
            0x0415 => "Польский",
            0x0412 => "Корейский",
            0x0411 => "Японский",
            0x0804 => "Китайский (упрощенный)",
            0x0404 => "Китайский (традиционный)",
            _ => $"Язык (0x{layoutId:X4})"
        };
    }

    public long? GetRecordedDurationTicks()
    {
        return loadedDurationTicks;
    }

    public List<InputEvent> GetRecordedEvents()
    {
        lock (_lock)
        {
            return new List<InputEvent>(recordedEvents);
        }
    }
}