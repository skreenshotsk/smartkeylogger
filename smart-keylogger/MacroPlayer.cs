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

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default)
    {
        if (events == null || events.Count == 0)
        {
            Console.WriteLine("Нет событий для воспроизведения!");
            return;
        }

        _stopRequested = false;
        _isPaused = false;
        _internalCancellation = new CancellationTokenSource();
        _currentEvents = events;
        _currentEventIndex = 0;
        _totalPauseDuration = 0;
        _pauseWaitHandle.Set();
        
        var keyCheckTask = StartKeyCheck(_internalCancellation.Token);

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCancellation.Token))
        {
            try
            {
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
                    await Task.Delay(100);
                }

                events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                do
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                    long startTime = events[0].Timestamp;
                    _playbackStartTime = DateTime.Now.Ticks;

                    for (_currentEventIndex = 0; _currentEventIndex < events.Count; _currentEventIndex++)
                    {
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        // Проверяем паузу перед каждым событием
                        _pauseWaitHandle.Wait(linkedCts.Token);

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        var e = events[_currentEventIndex];
                        long eventTimeFromStart = e.Timestamp - startTime;
                        long targetTime = _playbackStartTime + eventTimeFromStart + _totalPauseDuration;

                        // Ожидание времени события с проверкой паузы
                        while (DateTime.Now.Ticks < targetTime)
                        {
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                            
                            // Быстрая проверка паузы
                            if (_isPaused)
                            {
                                _pauseWaitHandle.Wait(linkedCts.Token);
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
                                Thread.Sleep(0);
                            }
                            
                            if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        ExecuteEvent(e);
                        
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                    }

                    if (!_stopRequested && !linkedCts.Token.IsCancellationRequested && !_isPaused)
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
                _internalCancellation.Cancel();
                if (keyCheckTask != null)
                    await keyCheckTask;
                _internalCancellation.Dispose();
                _isPaused = false;
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
            Console.WriteLine("Воспроизведение приостановлено");
        }
    }

    public void ResumePlayback()
    {
        if (_isPaused)
        {
            _isPaused = false;
            _totalPauseDuration += (DateTime.Now - _pauseStartTime).Ticks;
            _pauseWaitHandle.Set();
            Console.WriteLine("Воспроизведение возобновлено");
        }
    }

    private async Task StartKeyCheck(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            const int VK_BACKQUOTE = 0xC0;
            const int VK_HOME = 0x24;
            
            // Убрал флаги для предыдущего состояния - теперь мгновенная реакция
            bool backquotePressed = false;
            bool homePressed = false;

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

                // Проверка Home для паузы/продолжения - мгновенная реакция
                short homeState = GetAsyncKeyState(VK_HOME);
                bool isHomePressed = (homeState & 0x8000) != 0;

                if (isHomePressed && !homePressed)
                {
                    // Мгновенное переключение без задержек
                    if (_isPaused)
                    {
                        ResumePlayback();
                    }
                    else
                    {
                        PausePlayback();
                    }
                }
                homePressed = isHomePressed;

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
        _pauseWaitHandle.Set();
        _internalCancellation?.Cancel();
    }
}