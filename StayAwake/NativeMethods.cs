using System.Runtime.InteropServices;

namespace StayAwake;

/// <summary>Win32 APIs for idle detection, simulated input, and keep-awake state.</summary>
internal static class NativeMethods
{
    private const uint InputMouse = 0;
    private const uint MouseEventMove = 0x0001;

    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;
    private const uint EsDisplayRequired = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint Type;
        public MOUSEINPUT Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    /// <summary>Seconds since last keyboard/mouse input.</summary>
    internal static double GetIdleSeconds()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return 0;

        var idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
        return idleMs / 1000.0;
    }

    /// <summary>
    /// Injects a tiny relative mouse move via SendInput (counts as real input for Windows idle/lock),
    /// then moves back so the cursor stays put.
    /// </summary>
    internal static void JiggleMouse(int pixels, MovementMode movementMode)
    {
        var (dx, dy) = GetOffset(pixels, movementMode);
        SendRelativeMouseMove(dx, dy);
        SendRelativeMouseMove(-dx, -dy);
    }

    /// <summary>
    /// Prevents display sleep and system idle while active.
    /// Pass false to restore normal power management.
    /// </summary>
    internal static void SetKeepAwake(bool enabled)
    {
        if (enabled)
            SetThreadExecutionState(EsContinuous | EsSystemRequired | EsDisplayRequired);
        else
            SetThreadExecutionState(EsContinuous);
    }

    private static void SendRelativeMouseMove(int dx, int dy)
    {
        var input = new INPUT
        {
            Type = InputMouse,
            Mouse = new MOUSEINPUT
            {
                X = dx,
                Y = dy,
                Flags = MouseEventMove
            }
        };

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static (int dx, int dy) GetOffset(int pixels, MovementMode movementMode) =>
        movementMode switch
        {
            MovementMode.Vertical => (0, pixels),
            MovementMode.Random => Random.Shared.Next(2) == 0 ? (pixels, 0) : (0, pixels),
            _ => (pixels, 0)
        };
}
