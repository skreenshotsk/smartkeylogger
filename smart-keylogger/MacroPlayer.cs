// Fixed MacroPlayer.cs - с фоновой проверкой клавиши остановки
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class MacroPlayer
{
    private readonly Random _random = new Random();
    private POINT _lastMousePos = new POINT { X = 0, Y = 0 };
    private volatile bool _stopRequested = false;
    private CancellationTokenSource _internalCancellation;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        _stopRequested = false;
        _internalCancellation = new CancellationTokenSource();
        
        // Запускаем фоновую проверку клавиши Backquote
        var keyCheckTask = StartBackquoteCheck(_internalCancellation.Token);

        // Объединяем внешний и внутренний cancellation token
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCancellation.Token))
        {
            try
            {
                Console.WriteLine($"Воспроизведение {events.Count} событий...");
                Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");
                Console.WriteLine("Нажмите ` (Backquote) для остановки");

                // Задержка с проверкой отмены
                for (int i = 0; i < 30; i++)
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Воспроизведение отменено во время задержки");
                        return;
                    }
                    await Task.Delay(100);
                }

                events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                do
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                    long startTime = events[0].Timestamp;
                    long playbackStartTime = DateTime.Now.Ticks;

                    foreach (var e in events)
                    {
                        // Проверяем отмену перед каждым событием
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        long eventTimeFromStart = e.Timestamp - startTime;
                        long targetTime = playbackStartTime + eventTimeFromStart;

                        // Ожидание с частой проверкой
                        while (DateTime.Now.Ticks < targetTime)
                        {
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                            long remainingTicks = targetTime - DateTime.Now.Ticks;
                            int remainingMs = (int)(remainingTicks / TimeSpan.TicksPerMillisecond);

                            if (remainingMs > 10)
                            {
                                // Ждем маленькими блоками по 5мс
                                await Task.Delay(Math.Min(5, remainingMs / 2));
                            }
                            else if (remainingMs > 2)
                            {
                                await Task.Delay(1);
                            }
                            else
                            {
                                Thread.Sleep(0);
                            }
                            
                            // Дополнительная проверка
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        // Выполняем событие
                        ExecuteEvent(e);
                        
                        // Проверка после выполнения события
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                    }

                    if (!_stopRequested && !linkedCts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Воспроизведение завершено.");
                    }

                } while (loop && !_stopRequested && !linkedCts.Token.IsCancellationRequested);

                if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Воспроизведение остановлено пользователем");
                }
            }
            finally
            {
                // Останавливаем фоновую проверку
                _internalCancellation.Cancel();
                await keyCheckTask;
                _internalCancellation.Dispose();
            }
        }
    }

    private async Task StartBackquoteCheck(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            const int VK_BACKQUOTE = 0xC0;
            bool wasPressed = false;

            while (!cancellationToken.IsCancellationRequested && !_stopRequested)
            {
                // Проверяем состояние клавиши Backquote
                short keyState = GetAsyncKeyState(VK_BACKQUOTE);
                bool isPressed = (keyState & 0x8000) != 0;

                if (isPressed && !wasPressed)
                {
                    // Клавиша только что нажата
                    Console.WriteLine("Обнаружено нажатие Backquote - остановка воспроизведения");
                    _stopRequested = true;
                    _internalCancellation.Cancel();
                    break;
                }

                wasPressed = isPressed;
                await Task.Delay(10); // Проверяем каждые 10мс
            }
        });
    }

    private void ExecuteEvent(InputEvent e)
    {
        if (_stopRequested) return;

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
        if (keyCode == 0 || _stopRequested) return;

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
        if (_stopRequested) return;

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
        _internalCancellation?.Cancel();
    }
}