// Fixed MacroPlayer.cs - оптимизированная работа с CancellationToken
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MacroPlayer
{
    private readonly Random _random = new Random();
    private POINT _lastMousePos = new POINT { X = 0, Y = 0 };
    private volatile bool _stopRequested = false;

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        _stopRequested = false;
        
        // Регистрируем callback для отмены
        using (cancellationToken.Register(() => _stopRequested = true))
        {
            Console.WriteLine($"Воспроизведение {events.Count} событий...");
            Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");

            // Задержка с проверкой отмены каждые 100мс
            for (int i = 0; i < 30; i++)
            {
                if (_stopRequested)
                {
                    Console.WriteLine("Воспроизведение отменено во время задержки");
                    return;
                }
                await Task.Delay(100);
            }

            events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            do
            {
                if (_stopRequested) break;

                long startTime = events[0].Timestamp;
                long playbackStartTime = DateTime.Now.Ticks;

                foreach (var e in events)
                {
                    if (_stopRequested) break;

                    long eventTimeFromStart = e.Timestamp - startTime;
                    long targetTime = playbackStartTime + eventTimeFromStart;

                    // Оптимизированное ожидание с проверкой отмены каждые 10мс
                    while (DateTime.Now.Ticks < targetTime)
                    {
                        if (_stopRequested) break;

                        long remainingTicks = targetTime - DateTime.Now.Ticks;
                        int remainingMs = (int)(remainingTicks / TimeSpan.TicksPerMillisecond);

                        if (remainingMs > 20)
                        {
                            // Для больших интервалов ждем блоками по 10мс
                            await Task.Delay(Math.Min(10, remainingMs / 2));
                        }
                        else if (remainingMs > 5)
                        {
                            // Для средних интервалов ждем 1мс
                            await Task.Delay(1);
                        }
                        else
                        {
                            // Для маленьких интервалов используем Thread.Sleep(0) для точности
                            Thread.Sleep(0);
                        }
                    }

                    if (_stopRequested) break;

                    // Выполняем событие
                    ExecuteEvent(e);
                }

                if (!_stopRequested)
                {
                    Console.WriteLine("Воспроизведение завершено.");
                }

            } while (loop && !_stopRequested);

            if (_stopRequested)
            {
                Console.WriteLine("Воспроизведение остановлено пользователем");
            }
        }
    }

    private void ExecuteEvent(InputEvent e)
    {
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

    private void SimulateKeyPress(int keyCode, bool isPressed)
    {
        if (keyCode == 0) return;

        try
        {
            if (isPressed)
            {
                WinAPI.keybd_event((byte)keyCode, 0, WinAPI.KEYEVENTF_KEYDOWN, 0);
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
            // Устанавливаем позицию курсора
            if (Math.Abs(_lastMousePos.X - x) > 2 || Math.Abs(_lastMousePos.Y - y) > 2)
            {
                WinAPI.SetCursorPos(x, y);
                _lastMousePos.X = x;
                _lastMousePos.Y = y;
            }

            if (button == 1) // Левая кнопка
            {
                if (isPressed)
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                }
                else
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                }
            }
            else if (button == 2) // Правая кнопка
            {
                if (isPressed)
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                }
                else
                {
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
                }
            }
        }
        catch
        {
            // Резервный способ для абсолютных координат
            try
            {
                int screenWidth = WinAPI.GetSystemMetrics(0);
                int screenHeight = WinAPI.GetSystemMetrics(1);

                int absX = x * 65535 / Math.Max(1, screenWidth);
                int absY = y * 65535 / Math.Max(1, screenHeight);

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

    // Метод для принудительной остановки (можно вызывать извне)
    public void Stop()
    {
        _stopRequested = true;
    }
}