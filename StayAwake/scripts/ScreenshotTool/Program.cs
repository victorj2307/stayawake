using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace StayAwake.ScreenshotTool;

internal static class Program
{
    private const string WindowTitle = "StayAwake";

    public static int Main(string[] args)
    {
        var repoRoot = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        var exeDir = Path.Combine(repoRoot, "StayAwake", "bin", "Release", "net8.0-windows");
        var exe = Path.Combine(exeDir, "StayAwake.exe");
        var outDir = Path.Combine(repoRoot, "docs", "screenshots");

        if (!File.Exists(exe))
        {
            Console.Error.WriteLine($"Build Release first. Missing: {exe}");
            return 1;
        }

        Directory.CreateDirectory(outDir);

        CaptureState(exe, exeDir, outDir, "main-disabled", new AppSettingsDto
        {
            Enabled = false,
            MovementPixels = 1,
            IdleSeconds = 60,
            MinimizeToTray = true,
            MovementMode = "Horizontal",
            SessionDurationHours = 0
        });

        CaptureState(exe, exeDir, outDir, "main-active", new AppSettingsDto
        {
            Enabled = true,
            MovementPixels = 1,
            IdleSeconds = 60,
            MinimizeToTray = true,
            MovementMode = "Horizontal",
            SessionDurationHours = 1
        });

        CaptureSessionCompleted(exe, exeDir, outDir);

        Console.WriteLine($"Screenshots written to {outDir}");
        return 0;
    }

    private static void CaptureState(string exe, string exeDir, string outDir, string name, AppSettingsDto settings)
    {
        StopRunning();
        WriteSettings(Path.Combine(exeDir, "settings.json"), settings);
        using var proc = StartApp(exe, exeDir);
        Thread.Sleep(2000);
        SaveCapture(outDir, name);
        StopRunning();
        Thread.Sleep(300);
    }

    private static void CaptureSessionCompleted(string exe, string exeDir, string outDir)
    {
        StopRunning();
        WriteSettings(Path.Combine(exeDir, "settings.json"), new AppSettingsDto
        {
            Enabled = false,
            MovementPixels = 1,
            IdleSeconds = 60,
            MinimizeToTray = true,
            MovementMode = "Horizontal",
            SessionDurationHours = 0
        });

        var startInfo = new System.Diagnostics.ProcessStartInfo(exe)
        {
            WorkingDirectory = exeDir,
            UseShellExecute = false
        };
        startInfo.Environment["STAYAWAKE_SCREENSHOT"] = "session-completed";

        using var proc = System.Diagnostics.Process.Start(startInfo)!;
        Thread.Sleep(3000);
        SaveCapture(outDir, "main-session-completed");
        StopRunning();
    }

    private static System.Diagnostics.Process StartApp(string exe, string exeDir)
    {
        return System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
        {
            WorkingDirectory = exeDir,
            UseShellExecute = false
        })!;
    }

    private static void StopRunning()
    {
        foreach (var proc in System.Diagnostics.Process.GetProcessesByName("StayAwake"))
        {
            try { proc.Kill(); }
            catch { /* ignore */ }
        }
    }

    private static void WriteSettings(string path, AppSettingsDto settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static void SaveCapture(string outDir, string name)
    {
        using var bmp = CaptureWindow(WindowTitle);
        if (bmp is null)
        {
            Console.Error.WriteLine($"Could not capture window for {name}");
            return;
        }

        var outPath = Path.Combine(outDir, $"{name}.png");
        bmp.Save(outPath, ImageFormat.Png);
        Console.WriteLine($"Saved {outPath}");
    }

    private static Bitmap? CaptureWindow(string title)
    {
        var hwnd = FindWindow(null, title);
        if (hwnd == IntPtr.Zero)
            return null;

        SetForegroundWindow(hwnd);
        if (!GetWindowRect(hwnd, out var rect))
            return null;

        var w = rect.Right - rect.Left;
        var h = rect.Bottom - rect.Top;
        if (w <= 0 || h <= 0)
            return null;

        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        var hdc = g.GetHdc();
        try
        {
            PrintWindow(hwnd, hdc, 2);
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }

        return bmp;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    private sealed class AppSettingsDto
    {
        public bool Enabled { get; set; }
        public int MovementPixels { get; set; }
        public int IdleSeconds { get; set; }
        public bool MinimizeToTray { get; set; }
        public string MovementMode { get; set; } = "Horizontal";
        public int SessionDurationHours { get; set; }
        public int? SessionDurationMinutes { get; set; }
    }
}
