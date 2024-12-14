using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSleeper;

internal static partial class Program
{
    private const int VK_ESCAPE = 0x1B;

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
        Console.WriteLine("Компьютер перейдёт в спящий режим через 10 секунд. Нажмите ESC для отмены.");

        bool keyPressed = false;

        Stopwatch stopwatch = Stopwatch.StartNew();
        Console.WriteLine("Отсчёт времени запущен.");

        while (stopwatch.Elapsed < Timeout)
        {
            Console.WriteLine($"Прошло {stopwatch.ElapsedMilliseconds} мс, проверка нажатий клавиши ESC...");

            if (GetAsyncKeyState(VK_ESCAPE) != 0)
            {
                Console.WriteLine("Обнаружено нажатие клавиши ESC.");
                keyPressed = true;
                break;
            }

            await Task.Delay(50);
        }

        if (keyPressed)
        {
            Console.WriteLine("Спящий режим отменён.");
        }
        else
        {
            Console.WriteLine("Переход в спящий режим...");
            bool result = SetSuspendState(false, true, true);
            Console.WriteLine(result ? "Спящий режим активирован." : "Не удалось перейти в спящий режим.");
        }

        Console.WriteLine("Программа завершена.");
    }
}
