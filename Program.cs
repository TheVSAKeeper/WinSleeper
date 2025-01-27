using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSleeper;

internal static partial class Program
{
    private const int VK_ESCAPE = 0x1B;
    private const int VK_RETURN = 0x0D;
    private const int VK_ADD = 0x6B;
    private const int KeyCheckDelayMilliseconds = 50;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    //private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usage_stats.csv");

    private static readonly string LogFilePath = Path.Combine(@"C:\Users\admin\Documents", "usage_stats.csv");

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
        if (!File.Exists(LogFilePath))
        {
            Log("Timestamp,Event,Mode,ElapsedSeconds,RemainingSeconds,KeyPressed,Success");
        }

        Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},ProgramStarted,None,0,{Timeout.TotalSeconds},None,True");

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

        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        double remainingSeconds = Timeout.TotalSeconds - elapsedSeconds;

        Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},TimerStarted,{mode},{elapsedSeconds},{remainingSeconds},None,True");

        bool isAutoSwitch = false;

        if (DateTime.Now.TimeOfDay >= new TimeSpan(0, 30, 0))
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine("[INFO] Время больше 0:30. Автоматическое переключение на выключение.");
            Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},AutoSwitch,{mode},{elapsedSeconds},{remainingSeconds},None,True");
            stopwatch.Restart();
            mode = ExitMode.ShutDown;
            isAutoSwitch = true;
        }

        while (stopwatch.Elapsed < Timeout)
        {
            elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            remainingSeconds = Timeout.TotalSeconds - elapsedSeconds;
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
                Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},KeyPressed,{mode},{elapsedSeconds},{remainingSeconds},ESC,True");
                mode = ExitMode.Cancel;
                break;
            }

            if (mode != ExitMode.Reboot && GetAsyncKeyState(VK_ADD) != 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine("[INFO] Обнаружено нажатие клавиши ADD. Перезагрузка.");
                Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},KeyPressed,{mode},{elapsedSeconds},{remainingSeconds},ADD,True");
                stopwatch.Restart();
                mode = ExitMode.Reboot;
            }

            if (GetAsyncKeyState(VK_RETURN) is not 0 and not 1)
            {
                if (isAutoSwitch)
                {
                    if (mode == ExitMode.ShutDown)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.WriteLine("[INFO] Обнаружено нажатие клавиши Enter. Спящий режим.");
                        Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},KeyPressed,{mode},{elapsedSeconds},{remainingSeconds},Enter,True");
                        stopwatch.Restart();
                        mode = ExitMode.Sleep;
                    }
                }
                else if (mode != ExitMode.ShutDown)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("[INFO] Обнаружено нажатие клавиши Enter. Завершение работы.");
                    Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},KeyPressed,{mode},{elapsedSeconds},{remainingSeconds},Enter,True");
                    stopwatch.Restart();
                    mode = ExitMode.ShutDown;
                }
            }

            await Task.Delay(KeyCheckDelayMilliseconds);
        }

        Console.WriteLine();

        bool success = true;

        Action? onEnd = null;

        switch (mode)
        {
            case ExitMode.Cancel:
                Cancel();
                break;

            case ExitMode.Sleep:
                success = Sleep(out onEnd);
                break;

            case ExitMode.ShutDown:
                success = ShutDown(out onEnd);
                break;

            case ExitMode.Reboot:
                success = Reboot(out onEnd);
                break;
        }

        Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},ProgramEnded,{mode},{stopwatch.Elapsed.TotalSeconds},0,None,{success}");

        Console.WriteLine();
        Console.WriteLine("=========================");
        Console.WriteLine("       Program End      ");
        Console.WriteLine("=========================");

        onEnd?.Invoke();
    }

    private static bool Reboot(out Action? onEnd)
    {
        onEnd = () =>
        {
            ProcessStartInfo info = new("shutdown", "/r /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(info);
        };

        Console.WriteLine("[SUCCESS] Перезагрузка активирована.");

        return true;
    }

    private static bool ShutDown(out Action? onEnd)
    {
        onEnd = () =>
        {
            ProcessStartInfo info = new("shutdown", "/s /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(info);
        };

        Console.WriteLine("[SUCCESS] Завершение работы активировано.");
        return true;
    }

    private static bool Sleep(out Action? onEnd)
    {
        onEnd = () =>
        {
            SetSuspendState(false, true, true);
        };

        Console.WriteLine("[SUCCESS] Спящий режим активирован.");
        return true;
    }

    private static void Cancel()
    {
        Console.WriteLine("[INFO] Отменёно пользователем.");
    }

    private static void Log(string message)
    {
        try
        {
            using StreamWriter writer = new(LogFilePath, true);

            writer.WriteLine(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Не удалось записать в лог: {ex.Message}");
        }
    }
}
