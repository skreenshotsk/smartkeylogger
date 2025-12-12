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
    private long? _savedTotalDurationTicks = null; // Сохраненная длительность записи для согласованности

    // свойства для таймера воспроизведения
    public TimeSpan CurrentPlaybackTime 
    { 
        get 
        {
            if (!_playbackStarted || _currentEvents == null || _currentEvents.Count == 0) 
                return TimeSpan.Zero;
            
            // Определяем общую длительность - используем сохраненную если доступна
            long totalEventDuration;
            if (_savedTotalDurationTicks.HasValue && _savedTotalDurationTicks.Value > 0)
            {
                totalEventDuration = _savedTotalDurationTicks.Value;
            }
            else
            {
                // Fallback: вычисляем из событий
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
                // На паузе - время на момент паузы
                currentElapsed = _pauseStartTime.Ticks - _playbackStartTime - _totalPauseDuration;
            }
            else
            {
                // Воспроизведение идет
                currentElapsed = DateTime.Now.Ticks - _playbackStartTime - _totalPauseDuration;
            }
            
            // Для циклического воспроизведения используем модуль от общей длительности
            long currentCycleTime = currentElapsed % totalEventDuration;
            
            // Убедимся, что время не отрицательное
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
            
            // Используем сохраненную длительность если доступна (для согласованности с логами)
            if (_savedTotalDurationTicks.HasValue && _savedTotalDurationTicks.Value > 0)
            {
                return TimeSpan.FromTicks(_savedTotalDurationTicks.Value);
            }
            
            // Fallback: вычисляем из событий (для старых файлов без сохраненной длительности)
            var sortedEvents = new List<InputEvent>(_currentEvents);
            sortedEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            
            long startTime = sortedEvents[0].Timestamp;
            long endTime = sortedEvents[sortedEvents.Count - 1].Timestamp;
            return TimeSpan.FromTicks(endTime - startTime);
        } 
    }

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

    public async Task PlayEvents(List<InputEvent>? events, bool loop = false, CancellationToken cancellationToken = default, IntPtr? keyboardLayout = null, long? savedDurationTicks = null)
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
        _savedTotalDurationTicks = savedDurationTicks; // Сохраняем длительность для использования в свойствах
        _pauseWaitHandle.Set();
        
        // СОРТИРУЕМ события перед началом
        var eventsCopy = new List<InputEvent>(events);
        eventsCopy.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        
        // Вычисляем время начала и конца событий (нужно для воспроизведения)
        long startEventTime = eventsCopy[0].Timestamp;
        long endEventTime = eventsCopy[eventsCopy.Count - 1].Timestamp;
        
        // Получаем длительность макроса - используем сохраненную длительность если доступна
        // Это обеспечивает согласованность с логом остановки записи
        long totalMacroDuration;
        if (savedDurationTicks.HasValue && savedDurationTicks.Value > 0)
        {
            totalMacroDuration = savedDurationTicks.Value;
        }
        else
        {
            // Fallback: вычисляем из событий (для старых файлов без сохраненной длительности)
            totalMacroDuration = endEventTime - startEventTime;
        }
        TimeSpan macroDuration = TimeSpan.FromTicks(totalMacroDuration);
        string durationText = $"{macroDuration:mm\\:ss\\.ff}";
        
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
                Log($"▶ Начато воспроизведение. Длительность записи: {durationText}");
                Console.WriteLine($"Воспроизведение. Длительность макроса: {durationText}");
                Console.WriteLine("Переключитесь в нужное окно за 3 секунды...");
                Console.WriteLine("Нажмите ` (Backquote) для остановки, Home для паузы");

                // Ожидание перед началом
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

                // Таймер воспроизведения начинается здесь
                _playbackStartTime = DateTime.Now.Ticks;
                _playbackStarted = true;
                
                do
                {
                    if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                    Log($"▶ Цикл воспроизведения начался");

                    // Время начала текущего цикла
                    long cycleStartTime = DateTime.Now.Ticks - _totalPauseDuration;
                    
                    for (_currentEventIndex = 0; _currentEventIndex < eventsCopy.Count; _currentEventIndex++)
                    {
                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        // Ждем пока не закончится пауза
                        while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(50, linkedCts.Token);
                        }

                        if (_stopRequested || linkedCts.Token.IsCancellationRequested) break;

                        var e = eventsCopy[_currentEventIndex];
                        long eventTimeFromStart = e.Timestamp - startEventTime;
                        
                        // Рассчитываем когда должно произойти событие
                        long targetTime = cycleStartTime + eventTimeFromStart;
                        long currentAdjustedTime = DateTime.Now.Ticks - _totalPauseDuration;
                        
                        // Если событие уже должно было произойти, выполняем его немедленно
                        if (currentAdjustedTime >= targetTime)
                        {
                            ExecuteEvent(e);
                            continue;
                        }
                        
                        // Ждем до времени события
                        long timeToWait = targetTime - currentAdjustedTime;
                        int waitMs = (int)(timeToWait / TimeSpan.TicksPerMillisecond);
                        
                        if (waitMs > 0)
                        {
                            // Разбиваем ожидание на мелкие части для проверки паузы
                            int smallWait = Math.Min(waitMs, 10);
                            int waited = 0;
                            
                            while (waited < waitMs && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                            {
                                if (_isPaused)
                                {
                                    // На паузе - ждем пока она не закончится
                                    while (_isPaused && !_stopRequested && !linkedCts.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(50, linkedCts.Token);
                                    }
                                    
                                    // После паузы пересчитываем время
                                    currentAdjustedTime = DateTime.Now.Ticks - _totalPauseDuration;
                                    timeToWait = targetTime - currentAdjustedTime;
                                    waitMs = (int)(timeToWait / TimeSpan.TicksPerMillisecond);
                                    waited = 0;
                                    
                                    if (waitMs <= 0) break;
                                }
                                
                                smallWait = Math.Min(waitMs - waited, 10);
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
                        
                        // Если не цикличное воспроизведение - выходим
                        if (!loop) break;
                        
                        // Короткая пауза между циклами (50мс)
                        await Task.Delay(50, linkedCts.Token);
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
        if (!_isPaused && _playbackStarted)
        {
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            
            // получаем текущее время воспроизведения
            TimeSpan currentPlaybackTime = CurrentPlaybackTime;
            string timeText = $"{currentPlaybackTime:mm\\:ss\\.ff}";
            
            _pauseWaitHandle.Reset();
            ReleaseAllKeys();
            
            Log($"⏸ Воспроизведение приостановлено на {timeText}");
            Console.WriteLine($"Воспроизведение приостановлено на {timeText}");
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
        _internalCancellation?.Cancel();
        
        // Запускаем ReleaseAllKeys асинхронно, чтобы не блокировать остановку
        Task.Run(() => ReleaseAllKeys());
        
        Log("⏹ Воспроизведение остановлено");
    }
    
    private void ReleaseAllKeys()
    {
        // Отжимаем кнопки мыши сразу
        try
        {
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }
        catch
        {
            // Игнорируем ошибки
        }
        
        // Отжимаем только часто используемые клавиши для ускорения
        // Основные клавиши модификаторов
        byte[] importantKeys = new byte[] 
        {
            0x10, // Shift
            0x11, // Ctrl
            0x12, // Alt
            0x5B, // Left Win
            0x5C, // Right Win
            0xA0, // Left Shift
            0xA1, // Right Shift
            0xA2, // Left Ctrl
            0xA3, // Right Ctrl
            0xA4, // Left Alt
            0xA5  // Right Alt
        };
        
        foreach (byte keyCode in importantKeys)
        {
            try
            {
                WinAPI.keybd_event(keyCode, 0, WinAPI.KEYEVENTF_KEYUP, 0);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }
}