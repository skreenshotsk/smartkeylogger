using SharpHook;
using SharpHook.Data;
using SharpHook.Native;

public class MacroRecorder
{
    private TaskPoolGlobalHook? hook;
    private List<InputEvent> recordedEvents = new List<InputEvent>();
    private bool isRecording = false;
    private DateTime recordingStartTime;

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
            // По умолчанию оставляем как есть
            _ => (int)sharpHookKeyCode
        };
    }

    public void StartRecording()
    {
        recordedEvents.Clear();
        isRecording = true;
        recordingStartTime = DateTime.Now;
        
        hook = new TaskPoolGlobalHook();
        
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;
        hook.MouseMoved += OnMouseMoved;
        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        
        Console.WriteLine("Запись начата! Нажмите Enter в этом окне чтобы остановить и сохранить...");
        hook.RunAsync();
    }

    public void StopRecording()
    {
        isRecording = false;
        hook?.Dispose();
        hook = null;
        Console.WriteLine("Запись остановлена.");
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseMove",
            X = e.Data.X,
            Y = e.Data.Y,
            Timestamp = elapsed.Ticks
        });
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = true,
            Timestamp = elapsed.Ticks
        });
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "MouseClick",
            X = e.Data.X,
            Y = e.Data.Y,
            MouseButton = (int)e.Data.Button,
            IsPressed = false,
            Timestamp = elapsed.Ticks
        });
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = virtualKeyCode,
            IsPressed = true,
            Timestamp = elapsed.Ticks
        });
        
        Console.WriteLine($"Записана клавиша: VK=0x{virtualKeyCode:X2}, SharpHook={e.Data.KeyCode}");
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (!isRecording) return;
        
        TimeSpan elapsed = DateTime.Now - recordingStartTime;
        
        int virtualKeyCode = SharpHookToVirtualKey(e.Data.KeyCode);
        
        recordedEvents.Add(new InputEvent
        {
            EventType = "KeyPress",
            KeyCode = virtualKeyCode,
            IsPressed = false,
            Timestamp = elapsed.Ticks
        });
    }

    public void SaveToFile(string filename = "macro.json")
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(recordedEvents, options);
            File.WriteAllText(filename, json);
            Console.WriteLine($"Макрос сохранен в файл: {filename} ({recordedEvents.Count} событий)");
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
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            
            return System.Text.Json.JsonSerializer.Deserialize<List<InputEvent>>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            return null;
        }
    }
}