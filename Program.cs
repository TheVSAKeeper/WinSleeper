using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSleeper;

internal static partial class Program
{
    private const int VK_ESCAPE = 0x1B;
    private const int KeyCheckDelayMilliseconds = 50;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int vKey);

    [LibraryImport("Powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

    private static async Task Main()
    {
        Console.WriteLine("=========================");
        Console.WriteLine("     Windows Sleeper     ");
        Console.WriteLine("=========================");
        Console.WriteLine("Компьютер перейдёт в спящий режим через 10 секунд.");
        Console.WriteLine("Нажмите ESC для отмены.");
        Console.WriteLine();

        bool keyPressed = false;

        Stopwatch stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[INFO] Отсчёт времени запущен.");

        while (stopwatch.Elapsed < Timeout)
        {
            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            double remainingSeconds = Timeout.TotalSeconds - elapsedSeconds;
            int progress = (int)(elapsedSeconds / Timeout.TotalSeconds * 100);

            string filled = new('=', progress / 2);
            string space = new(' ', 50 - progress / 2);

            Console.Write($"\r[{filled}{space}] {progress}% ({remainingSeconds:F1} сек)");

            if (GetAsyncKeyState(VK_ESCAPE) != 0)
            {
                Console.WriteLine();
                Console.WriteLine("[INFO] Обнаружено нажатие клавиши ESC. Процесс отменён.");
                keyPressed = true;
                break;
            }

            await Task.Delay(KeyCheckDelayMilliseconds);
        }

        if (keyPressed)
        {
            Console.WriteLine("[INFO] Спящий режим отменён.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("[INFO] Переход в спящий режим...");
            bool result = SetSuspendState(false, true, true);
            Console.WriteLine(result ? "[SUCCESS] Спящий режим активирован." : "[ERROR] Не удалось перейти в спящий режим.");
        }

        Console.WriteLine();
        Console.WriteLine("=========================");
        Console.WriteLine("       Program End      ");
        Console.WriteLine("=========================");
    }
}
