// MacroPlayer.cs
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
    private volatile bool _isPaused = false;
    private volatile bool _playbackStarted = false;
    private CancellationTokenSource? _internalCancellation;
    private List<InputEvent>? _currentEvents;
    private int _currentEventIndex = 0;
    private long _playbackStartTime = 0;
    private long _totalPauseDuration = 0;
    private DateTime _pauseStartTime;
    private ManualResetEventSlim _pauseWaitHandle = new ManualResetEventSlim(true);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public bool IsPaused => _isPaused;
    public bool PlaybackStarted => _playbackStarted;
    
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

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default, IntPtr? keyboardLayout = null)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        _stopRequested = false;
        _isPaused = false;
        _playbackStarted = false;
        _internalCancellation = new CancellationTokenSource();
        _currentEvents = events;
        _currentEventIndex = 0;
        _totalPauseDuration = 0;
        _pauseWaitHandle.Set();
        
        // Переключаем язык клавиатуры если нужно
        if (keyboardLayout.HasValue && keyboardLayout.Value != IntPtr.Zero)
        {
            IntPtr currentLayout = WinAPI.GetKeyboardLayout(0);
            if (currentLayout != keyboardLayout.Value)
            {
                WinAPI.ActivateKeyboardLayout(keyboardLayout.Value, WinAPI.KLF_ACTIVATE);
                string langName = GetLanguageName(keyboardLayout.Value);
                Log($"🌐 Язык клавиатуры переключен: {langName}");
            }
        }
        
        var keyCheckTask = StartKeyCheck(_internalCancellation.Token);

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCancellation.Token))
        {
            try
            {
                Log($"▶ Начато воспроизведение ({events.Count} событий)");
                Console.WriteLine($"Воспроизведение {events.Count} событий...");
                Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");
                Console.WriteLine("Нажмите ` (Backquote) для остановки, Home для паузы");

                for (int i = 0; i < 30; i++)
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Воспроизведение отменено во время задержки");
                        return;
                    }
                    
                    // Проверяем паузу во время задержки асинхронно
                    while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(50, linkedCts.Token);
                    }
                    
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Воспроизведение отменено во время задержки");
                        return;
                    }
                    
                    await Task.Delay(100);
                }

                // Техническая пауза завершена, воспроизведение началось
                _playbackStarted = true;
                Log("▶ Воспроизведение началось");

                events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                do
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                    long startTime = events[0].Timestamp;
                    _playbackStartTime = DateTime.Now.Ticks;

                    for (_currentEventIndex = 0; _currentEventIndex < events.Count; _currentEventIndex++)
                    {
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        // Ожидаем паузу асинхронно перед каждым событием
                        while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(50, linkedCts.Token);
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        var e = events[_currentEventIndex];
                        long eventTimeFromStart = e.Timestamp - startTime;
                        long targetTime = _playbackStartTime + eventTimeFromStart + _totalPauseDuration;

                        // Ожидание времени события с проверкой паузы
                        while (DateTime.Now.Ticks < targetTime)
                        {
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                            
                            // Асинхронная проверка паузы
                            if (_isPaused)
                            {
                                while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                                {
                                    await Task.Delay(50, linkedCts.Token);
                                }
                                if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                                
                                // Пересчитываем время после паузы
                                targetTime = _playbackStartTime + eventTimeFromStart + _totalPauseDuration;
                                continue;
                            }

                            long remainingTicks = targetTime - DateTime.Now.Ticks;
                            int remainingMs = (int)(remainingTicks / TimeSpan.TicksPerMillisecond);

                            if (remainingMs > 10)
                            {
                                await Task.Delay(Math.Min(5, remainingMs / 2), linkedCts.Token);
                            }
                            else if (remainingMs > 2)
                            {
                                await Task.Delay(1, linkedCts.Token);
                            }
                            else
                            {
                                await Task.Delay(1, linkedCts.Token);
                            }
                            
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        ExecuteEvent(e);
                        
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                    }

                    if (!_stopRequested && !linkedCts.Token.IsCancellationRequested && !_isPaused)
                    {
                        Log("✓ Воспроизведение завершено");
                        Console.WriteLine("Воспроизведение завершено.");
                    }

                } while (loop && !_stopRequested && !linkedCts.Token.IsCancellationRequested);

                if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                {
                    Log("⏹ Воспроизведение остановлено пользователем");
                    Console.WriteLine("Воспроизведение остановлено пользователем");
                }
            }
            finally
            {
                _internalCancellation.Cancel();
                if (keyCheckTask != null)
                    await keyCheckTask;
                _internalCancellation.Dispose();
                _isPaused = false;
                _playbackStarted = false;
                _currentEventIndex = 0;
                _pauseWaitHandle.Set();
            }
        }
    }

    public void PausePlayback()
    {
        if (!_isPaused)
        {
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            _pauseWaitHandle.Reset();
            ReleaseAllKeys();
            Log("⏸ Воспроизведение приостановлено");
            Console.WriteLine("Воспроизведение приостановлено");
        }
    }

    public void ResumePlayback()
    {
        if (_isPaused)
        {
            var pauseDuration = (DateTime.Now - _pauseStartTime).Ticks;
            _isPaused = false;
            _totalPauseDuration += pauseDuration;
            _pauseWaitHandle.Set();
            Log($"▶ Воспроизведение возобновлено");
            Console.WriteLine($"Воспроизведение возобновлено (пауза: {pauseDuration / TimeSpan.TicksPerMillisecond} мс)");
        }
    }

    private async Task StartKeyCheck(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            const int VK_BACKQUOTE = 0xC0;
            
            bool backquotePressed = false;

            while (!cancellationToken.IsCancellationRequested && !_stopRequested)
            {
                // Проверка Backquote для остановки
                short backquoteState = GetAsyncKeyState(VK_BACKQUOTE);
                bool isBackquotePressed = (backquoteState & 0x8000) != 0;

                if (isBackquotePressed && !backquotePressed)
                {
                    Console.WriteLine("Обнаружено нажатие Backquote - остановка воспроизведения");
                    _stopRequested = true;
                    _internalCancellation?.Cancel();
                    break;
                }
                backquotePressed = isBackquotePressed;

                await Task.Delay(10, cancellationToken);
            }
        }, cancellationToken);
    }

    private void ExecuteEvent(InputEvent e)
    {
        if (_stopRequested || _isPaused) return;

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
        if (keyCode == 0 || _stopRequested || _isPaused) return;

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
        if (_stopRequested || _isPaused) return;

        try
        {
            if (Math.Abs(_lastMousePos.X - x) > 2 || Math.Abs(_lastMousePos.Y - y) > 2)
            {
                WinAPI.SetCursorPos(x, y);
                _lastMousePos.X = x;
                _lastMousePos.Y = y;
            }

            if (button == 1)
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
            else if (button == 2)
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

    public void Stop()
    {
        _stopRequested = true;
        _isPaused = false;
        _playbackStarted = false;
        _pauseWaitHandle.Set();
        ReleaseAllKeys();
        _internalCancellation?.Cancel();
        Log("⏹ Воспроизведение остановлено");
    }
    
    private void ReleaseAllKeys()
    {
        // Отжимаем все клавиши от 0x01 до 0xFE
        for (int keyCode = 0x01; keyCode <= 0xFE; keyCode++)
        {
            try
            {
                WinAPI.keybd_event((byte)keyCode, 0, WinAPI.KEYEVENTF_KEYUP, 0);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
        
        // Отжимаем кнопки мыши
        try
        {
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }
        catch
        {
            // Игнорируем ошибки
        }
    }
}