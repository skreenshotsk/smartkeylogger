using SharpHook;
using SharpHook.Native;

class Program
{
    static async Task Main(string[] args)
    {
        var recorder = new MacroRecorder();
        var player = new MacroPlayer();

        Console.WriteLine("Smart Macro Recorder");
        Console.WriteLine("1 - Начать запись");
        Console.WriteLine("2 - Воспроизвести последний макрос");
        Console.WriteLine("3 - Воспроизвести в цикле");
        Console.WriteLine("4 - Выход");

        while (true)
        {
            Console.Write("\nВыберите действие: ");
            var choice = Console.ReadKey().KeyChar;
            Console.WriteLine();

            switch (choice)
            {
                case '1':
                    recorder.StartRecording();
                    Console.WriteLine("Запись идет... Нажмите Enter в этом окне для остановки");
                    while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
                    recorder.StopRecording();
                    recorder.SaveToFile();
                    break;

                case '2':
                    var events = recorder.LoadFromFile();
                    if (events != null)
                    {
                        await player.PlayEvents(events, false);
                    }
                    break;

                case '3':
                    var loopEvents = recorder.LoadFromFile();
                    if (loopEvents != null)
                    {
                        Console.WriteLine("Циклическое воспроизведение. Нажмите Ctrl+C для остановки");
                        await player.PlayEvents(loopEvents, true);
                    }
                    break;

                case '4':
                    return;

                default:
                    Console.WriteLine("Неверный выбор!");
                    break;
            }
        }
    }
}