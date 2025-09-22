public class MacroPlayer
{
    private readonly Random _random = new Random();
    private POINT _lastMousePos = new POINT { X = 0, Y = 0 };

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        Console.WriteLine($"Воспроизведение {events.Count} событий...");
        Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");
        await Task.Delay(3000);
        
        events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        
        do
        {
            long startTime = events[0].Timestamp;
            long playbackStartTime = DateTime.Now.Ticks;
            
            foreach (var e in events)
            {
                long eventTimeFromStart = e.Timestamp - startTime;
                long targetTime = playbackStartTime + eventTimeFromStart;
                
                long currentTime;
                while ((currentTime = DateTime.Now.Ticks) < targetTime)
                {
                    int remainingMs = (int)((targetTime - currentTime) / TimeSpan.TicksPerMillisecond);
                    if (remainingMs > 10)
                    {
                        await Task.Delay(1);
                    }
                    else
                    {
                        Thread.SpinWait(1000);
                    }
                }

                switch (e.EventType)
                {
                    case "KeyPress":
                        SimulateKeyPress(e.KeyCode, e.IsPressed);
                        break;
                        
                    case "MouseClick":
                        SimulateMouseClick(e.X, e.Y, e.MouseButton, e.IsPressed);
                        break;
                        
                    case "MouseMove":
                        WinAPI.SetCursorPos(e.X, e.Y);
                        _lastMousePos.X = e.X;
                        _lastMousePos.Y = e.Y;
                        break;
                }
            }
            
            Console.WriteLine("Воспроизведение завершено.");
            
        } while (loop);
    }

    private void SimulateKeyPress(int keyCode, bool isPressed)
    {
        if (keyCode == 0) return;

        try
        {
            if (isPressed)
            {
                WinAPI.keybd_event((byte)keyCode, 0, WinAPI.KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(_random.Next(1, 2));
            }
            else
            {
                WinAPI.keybd_event((byte)keyCode, 0, WinAPI.KEYEVENTF_KEYUP, 0);
            }
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    private void SimulateMouseClick(int x, int y, int button, bool isPressed)
    {
        try
        {
            WinAPI.SetCursorPos(x, y);
            _lastMousePos.X = x;
            _lastMousePos.Y = y;
            
            Thread.Sleep(1);
            
            if (button == 1)
            {
                if (isPressed)
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(1);
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                }
                else
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(1);
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                }
            }
            else if (button == 2)
            {
                if (isPressed)
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(1);
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                }
                else
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(1);
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
                }
            }
            
            Thread.Sleep(1);
        }
        catch
        {
            // Резервный способ
            try
            {
                int screenWidth = WinAPI.GetSystemMetrics(0);
                int screenHeight = WinAPI.GetSystemMetrics(1);
                
                int absX = x * 65535 / screenWidth;
                int absY = y * 65535 / screenHeight;
                
                uint flags = WinAPI.MOUSEEVENTF_ABSOLUTE;
                
                if (button == 1)
                    flags |= isPressed ? WinAPI.MOUSEEVENTF_LEFTDOWN : WinAPI.MOUSEEVENTF_LEFTUP;
                else if (button == 2)
                    flags |= isPressed ? WinAPI.MOUSEEVENTF_RIGHTDOWN : WinAPI.MOUSEEVENTF_RIGHTUP;
                
                WinAPI.mouse_event(flags, (uint)absX, (uint)absY, 0, 0);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }
}