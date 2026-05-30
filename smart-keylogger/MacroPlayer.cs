using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MacroPlayer
{
    private POINT _lastMousePos = new POINT { X = 0, Y = 0 };
    private volatile bool _stopRequested = false;
    private volatile bool _isPaused = false;
    private volatile bool _playbackStarted = false;
    private volatile bool _skipWarmupDelay = false;
    private CancellationTokenSource? _internalCancellation;
    private List<InputEvent>? _currentEvents;
    private int _currentEventIndex = 0;
    private long _playbackStartTime = 0;
    private long _totalPauseDuration = 0;
    private DateTime _pauseStartTime;
    private ManualResetEventSlim _pauseWaitHandle = new ManualResetEventSlim(true);
    private long? _savedTotalDurationTicks = null;

    public TimeSpan CurrentPlaybackTime
    {
        get
        {
            if (!_playbackStarted || _currentEvents == null || _currentEvents.Count == 0)
                return TimeSpan.Zero;

            long totalEventDuration;
            if (_savedTotalDurationTicks.HasValue && _savedTotalDurationTicks.Value > 0)
            {
                totalEventDuration = _savedTotalDurationTicks.Value;
            }
            else
            {
                var sortedEvents = new List<InputEvent>(_currentEvents);
                sortedEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                long startEventTime = sortedEvents[0].Timestamp;
                long endEventTime = sortedEvents[sortedEvents.Count - 1].Timestamp;
                totalEventDuration = endEventTime - startEventTime;
            }

            if (totalEventDuration <= 0)
                return TimeSpan.Zero;

            long currentElapsed;
            if (_isPaused)
            {
                currentElapsed = _pauseStartTime.Ticks - _playbackStartTime - _totalPauseDuration;
            }
            else
            {
                currentElapsed = DateTime.Now.Ticks - _playbackStartTime - _totalPauseDuration;
            }

            long currentCycleTime = currentElapsed % totalEventDuration;
            if (currentCycleTime < 0) currentCycleTime = 0;
            if (currentCycleTime > totalEventDuration) currentCycleTime = totalEventDuration;

            return TimeSpan.FromTicks(currentCycleTime);
        }
    }

    public TimeSpan TotalPlaybackTime
    {
        get
        {
            if (_currentEvents == null || _currentEvents.Count == 0)
                return TimeSpan.Zero;

            if (_savedTotalDurationTicks.HasValue && _savedTotalDurationTicks.Value > 0)
            {
                return TimeSpan.FromTicks(_savedTotalDurationTicks.Value);
            }

            var sortedEvents = new List<InputEvent>(_currentEvents);
            sortedEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            long startTime = sortedEvents[0].Timestamp;
            long endTime = sortedEvents[sortedEvents.Count - 1].Timestamp;
            return TimeSpan.FromTicks(endTime - startTime);
        }
    }

    public bool IsPaused => _isPaused;
    public bool PlaybackStarted => _playbackStarted;

    //пропуск 3-секундного отсчёта перед стартом
    public void SkipStartupDelay()
    {
        _skipWarmupDelay = true;
    }

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

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default, IntPtr? keyboardLayout = null, long? savedDurationTicks = null)
    {
        if (events == null || events.Count == 0)
        {
            Log("⚠️ Нет событий для воспроизведения");
            return;
        }

        _stopRequested = false;
        _isPaused = false;
        _playbackStarted = false;
        _skipWarmupDelay = false;
        _internalCancellation = new CancellationTokenSource();
        _currentEvents = events;
        _currentEventIndex = 0;
        _totalPauseDuration = 0;
        _savedTotalDurationTicks = savedDurationTicks;
        _pauseWaitHandle.Set();

        var eventsCopy = new List<InputEvent>(events);
        eventsCopy.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        long startEventTime = eventsCopy[0].Timestamp;
        long endEventTime = eventsCopy[eventsCopy.Count - 1].Timestamp;

        long totalMacroDuration;
        if (savedDurationTicks.HasValue && savedDurationTicks.Value > 0)
        {
            totalMacroDuration = savedDurationTicks.Value;
        }
        else
        {
            totalMacroDuration = endEventTime - startEventTime;
        }
        TimeSpan macroDuration = TimeSpan.FromTicks(totalMacroDuration);
        string durationText = $"{macroDuration:mm\\:ss\\.ff}";

        if (keyboardLayout.HasValue && keyboardLayout.Value != IntPtr.Zero)
        {
            IntPtr currentLayout = WinAPI.GetKeyboardLayout(0);
            if (currentLayout != keyboardLayout.Value)
            {
                WinAPI.ActivateKeyboardLayout(keyboardLayout.Value, WinAPI.KLF_ACTIVATE);
                Log($"🌐 Язык клавиатуры переключен: {GetLanguageName(keyboardLayout.Value)}");
            }
        }

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCancellation.Token))
        {
            try
            {
                Log($"▶ Начато воспроизведение. Длительность записи: {durationText}");

                for (int i = 0; i < 30; i++)
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    if (_skipWarmupDelay)
                        break;
                    await Task.Delay(100);
                }

                _skipWarmupDelay = false;
                _playbackStartTime = DateTime.Now.Ticks;
                _playbackStarted = true;

                do
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                    Log("▶ Цикл воспроизведения начался");

                    long cycleStartTime = DateTime.Now.Ticks - _totalPauseDuration;

                    for (_currentEventIndex = 0; _currentEventIndex < eventsCopy.Count; _currentEventIndex++)
                    {
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(50, linkedCts.Token);
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        var e = eventsCopy[_currentEventIndex];
                        long eventTimeFromStart = e.Timestamp - startEventTime;
                        long targetTime = cycleStartTime + eventTimeFromStart;
                        long currentAdjustedTime = DateTime.Now.Ticks - _totalPauseDuration;

                        if (currentAdjustedTime >= targetTime)
                        {
                            ExecuteEvent(e);
                            continue;
                        }

                        long timeToWait = targetTime - currentAdjustedTime;
                        int waitMs = (int)(timeToWait / TimeSpan.TicksPerMillisecond);

                        if (waitMs > 0)
                        {
                            int waited = 0;

                            while (waited < waitMs && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                            {
                                if (_isPaused)
                                {
                                    while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(50, linkedCts.Token);
                                    }

                                    currentAdjustedTime = DateTime.Now.Ticks - _totalPauseDuration;
                                    timeToWait = targetTime - currentAdjustedTime;
                                    waitMs = (int)(timeToWait / TimeSpan.TicksPerMillisecond);
                                    waited = 0;

                                    if (waitMs <= 0) break;
                                }

                                int smallWait = Math.Min(waitMs - waited, 10);
                                await Task.Delay(smallWait, linkedCts.Token);
                                waited += smallWait;
                            }
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        ExecuteEvent(e);

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;
                    }

                    if (!_stopRequested && !linkedCts.Token.IsCancellationRequested && !_isPaused)
                    {
                        Log("✓ Цикл воспроизведения завершен");
                        if (!loop) break;
                        await Task.Delay(50, linkedCts.Token);
                    }

                } while (loop && !_stopRequested && !linkedCts.Token.IsCancellationRequested);

                if (_stopRequested || linkedCts.Token.IsCancellationRequested)
                {
                    Log("⏹ Воспроизведение остановлено пользователем");
                }
            }
            finally
            {
                _internalCancellation.Cancel();
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
        if (!_isPaused && _playbackStarted)
        {
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            TimeSpan currentPlaybackTime = CurrentPlaybackTime;
            string timeText = $"{currentPlaybackTime:mm\\:ss\\.ff}";
            _pauseWaitHandle.Reset();
            ReleaseAllKeys();
            Log($"⏸ Воспроизведение приостановлено на {timeText}");
        }
    }

    public void ResumePlayback()
    {
        if (_isPaused && _playbackStarted)
        {
            var pauseDuration = (DateTime.Now - _pauseStartTime).Ticks;
            _isPaused = false;
            _totalPauseDuration += pauseDuration;
            _pauseWaitHandle.Set();
            Log("▶ Воспроизведение возобновлено");
        }
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
        catch { }
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
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                else
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
            }
            else if (button == 2)
            {
                if (isPressed)
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                else
                    WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
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
            catch { }
        }
    }

    public void Stop()
    {
        _stopRequested = true;
        _isPaused = false;
        _playbackStarted = false;
        _pauseWaitHandle.Set();
        _internalCancellation?.Cancel();
        Task.Run(() => ReleaseAllKeys());
        Log("⏹ Воспроизведение остановлено");
    }

    private void ReleaseAllKeys()
    {
        try
        {
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }
        catch { }

        byte[] importantKeys = new byte[]
        {
            0x10, 0x11, 0x12, 0x5B, 0x5C,
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5
        };

        foreach (byte keyCode in importantKeys)
        {
            try
            {
                WinAPI.keybd_event(keyCode, 0, WinAPI.KEYEVENTF_KEYUP, 0);
            }
            catch { }
        }
    }
}
