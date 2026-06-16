using Avalonia;
using Serilog;
using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindroseServerManager.App;

sealed class Program
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindroseServerManager");

    private static readonly string CrashDir = Path.Combine(AppDataDir, "crashes");
    private static readonly string LogDir = Path.Combine(AppDataDir, "logs");

    public static string CrashDirectory => CrashDir;

    /// <summary>True wenn die App via --tray oder --minimized gestartet wurde (Autostart-Modus).</summary>
    public static bool StartMinimizedToTray { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var cliCommand = ParseCliCommand(args);
        if (cliCommand is not null)
        {
            RunCliAsync(cliCommand).GetAwaiter().GetResult();
            return;
        }

        foreach (var a in args)
        {
            if (string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase))
            {
                StartMinimizedToTray = true;
                break;
            }
        }

        CleanupUpdateTemp();

        try
        {
            Directory.CreateDirectory(CrashDir);
            Directory.CreateDirectory(LogDir);
        }
        catch { }

        ConfigureSerilog();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Log.Information("Startup...");

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // RedirectionSurface = GDI-sichtbare Rendering-Oberfläche. Default wäre
            // WinUIComposition/DirectComposition — GPU-Surfaces, die BitBlt/PrintWindow
            // (Greenshot, ShareX-GDI, klassische Screenshot-APIs) schwarz zurückgeben.
            .With(new Avalonia.Win32PlatformOptions
            {
                CompositionMode = new[] { Avalonia.Win32CompositionMode.RedirectionSurface },
            })
            .WithInterFont()
            .LogToTrace();

    private static string? ParseCliCommand(string[] args)
    {
        var cliArgs = new[] { "--start", "--stop", "--restart", "--backup", "--status" };
        foreach (var a in args)
        {
            var match = cliArgs.FirstOrDefault(c =>
                string.Equals(a, c, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match.TrimStart('-');
        }
        return null;
    }

    private static async Task RunCliAsync(string command)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", "WindroseServerManager",
                PipeDirection.InOut, PipeOptions.None);

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
            await pipe.ConnectAsync(cts.Token).ConfigureAwait(false);

            var cmd = JsonSerializer.Serialize(new { Command = command });
            var cmdBytes = Encoding.UTF8.GetBytes(cmd);
            await pipe.WriteAsync(cmdBytes, 0, cmdBytes.Length).ConfigureAwait(false);
            await pipe.FlushAsync().ConfigureAwait(false);

            using var ms = new MemoryStream();
            var buffer = new byte[4096];
            while (true)
            {
                var read = await pipe.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read == 0) break;
                ms.Write(buffer, 0, read);
                if (ms.Length > 65536) break;
            }

            var response = Encoding.UTF8.GetString(ms.ToArray());
            Console.WriteLine(response);
        }
        catch (TimeoutException)
        {
            if (string.Equals(command, "status", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(new { running = false, status = "Stopped" }));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "No running instance found." }));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        }
    }

    private static void CleanupUpdateTemp()
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "WindroseUpdate");
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch { }
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(LogDir, "app-.log"),
                rollingInterval: Serilog.RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogCrash(ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            LogCrash(e.Exception);
            Log.Error(e.Exception, "Unobserved task exception (marked as observed)");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to log unobserved task exception");
        }
        finally
        {
            e.SetObserved();
        }
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(CrashDir);
            var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(CrashDir, $"crash-{ts}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OS:           {Environment.OSVersion}");
            sb.AppendLine($".NET:         {Environment.Version}");
            sb.AppendLine($"App-Version:  {Assembly.GetExecutingAssembly().GetName().Version}");
            sb.AppendLine($"64-bit:       {Environment.Is64BitProcess}");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine("Exception Chain:");
            sb.AppendLine();

            var current = ex;
            int depth = 0;
            while (current is not null)
            {
                sb.AppendLine($"[{depth}] {current.GetType().FullName}: {current.Message}");
                sb.AppendLine(current.StackTrace);
                sb.AppendLine();
                current = current.InnerException;
                depth++;
            }

            File.WriteAllText(path, sb.ToString());

            try { Log.Fatal(ex, "Crash persisted to {Path}", path); } catch { }
        }
        catch
        {
            // Crash-Logger darf nie selbst crashen.
        }
    }
}
