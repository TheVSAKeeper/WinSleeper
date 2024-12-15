using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSleeper;

internal static partial class Program
{
    private const int VK_ESCAPE = 0x1B;
    private const int VK_RETURN = 0x0D;
    private const int VK_ADD = 0x6B;
    private const int KeyCheckDelayMilliseconds = 50;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private enum ExitMode : byte
    {
        Cancel = 0,
        Sleep = 1,
        ShutDown = 2,
        Reboot = 3,
    }

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
        Console.WriteLine($"Компьютер перейдёт в спящий режим через {Timeout.TotalSeconds} секунд.");
        Console.WriteLine("Нажмите ESC для отмены или Enter для переключения режима.");
        Console.WriteLine();

        ExitMode mode = ExitMode.Sleep;
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.WriteLine("[INFO] Отсчёт времени запущен.");
        int c = 0;

        while (stopwatch.Elapsed < Timeout)
        {
            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            double remainingSeconds = Timeout.TotalSeconds - elapsedSeconds;
            int progress = (int)(elapsedSeconds / Timeout.TotalSeconds * 100);

            string filled = new('=', progress / 2);
            string space = new(' ', 50 - progress / 2);

            Console.ForegroundColor = mode switch
            {
                ExitMode.Sleep => ConsoleColor.DarkYellow,
                ExitMode.ShutDown => ConsoleColor.DarkRed,
                ExitMode.Reboot => ConsoleColor.DarkCyan,
                var _ => defaultColor,
            };

            Console.Write($"\r[{filled}{space}] {progress}% ({remainingSeconds:F1} сек)");

            Console.ForegroundColor = defaultColor;

            if (GetAsyncKeyState(VK_ESCAPE) != 0)
            {
                Console.WriteLine();
                Console.WriteLine("[INFO] Обнаружено нажатие клавиши ESC. Процесс отменён.");
                mode = ExitMode.Cancel;
                break;
            }

            if (mode != ExitMode.Reboot && GetAsyncKeyState(VK_ADD) != 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("[INFO] Обнаружено нажатие клавиши ADD. Переключен режим.");
                stopwatch.Restart();
                mode = ExitMode.Reboot;
            }

            if (mode != ExitMode.ShutDown && GetAsyncKeyState(VK_RETURN) is not 0 and not 1)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("[INFO] Обнаружено нажатие клавиши Enter. Переключен режим.");
                stopwatch.Restart();
                mode = ExitMode.ShutDown;
            }

            await Task.Delay(KeyCheckDelayMilliseconds);
        }

        Console.WriteLine();

        switch (mode)
        {
            case ExitMode.Cancel:
                Cancel();
                break;

            case ExitMode.Sleep:
                Sleep();
                break;

            case ExitMode.ShutDown:
                ShutDown();
                break;

            case ExitMode.Reboot:
                Reboot();
                break;
        }

        Console.WriteLine();
        Console.WriteLine("=========================");
        Console.WriteLine("       Program End      ");
        Console.WriteLine("=========================");
    }

    private static void Reboot()
    {
        ProcessStartInfo info = new("shutdown", "/r /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };

        Process.Start(info);
        Console.WriteLine("[SUCCESS] Перезагрузка активирована.");
    }

    private static void ShutDown()
    {
        ProcessStartInfo info = new("shutdown", "/s /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };

        Process.Start(info);
        Console.WriteLine("[SUCCESS] Завершение работы активировано.");
    }

    private static void Sleep()
    {
        Console.WriteLine("[INFO] Переход в спящий режим...");
        bool state = SetSuspendState(false, true, true);
        Console.WriteLine(state ? "[SUCCESS] Спящий режим активирован." : "[ERROR] Не удалось перейти в спящий режим.");
    }

    private static void Cancel()
    {
        Console.WriteLine("[INFO] Отменёно пользователем.");
    }
}
