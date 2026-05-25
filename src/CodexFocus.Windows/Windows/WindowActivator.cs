using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CodexFocus.Windows.Windows;

public sealed record WindowMatch(
    IntPtr Handle,
    string Title,
    string ProcessName,
    int Left,
    int Top,
    int Right,
    int Bottom,
    int ProcessId = 0)
{
    public string Description => string.IsNullOrWhiteSpace(Title)
        ? ProcessName
        : $"{Title} ({ProcessName})";

    public int Area => Math.Max(0, Right - Left) * Math.Max(0, Bottom - Top);
}

public sealed class WindowActivator
{
    public WindowMatch? FindFirst(IReadOnlyList<string> processNames)
    {
        return WindowMatcher.SelectBest(EnumerateWindows(), processNames, Environment.ProcessId);
    }

    public bool Activate(WindowMatch match)
    {
        if (NativeMethods.IsIconic(match.Handle))
        {
            NativeMethods.ShowWindow(match.Handle, NativeMethods.SwRestore);
        }

        return NativeMethods.SetForegroundWindow(match.Handle);
    }

    public bool ClickCenter(WindowMatch match)
    {
        if (!NativeMethods.GetWindowRect(match.Handle, out var rect) || rect.Width <= 0 || rect.Height <= 0)
        {
            return false;
        }

        if (!NativeMethods.SetCursorPos(rect.CenterX, rect.CenterY))
        {
            return false;
        }

        var inputs = new[]
        {
            new NativeMethods.Input
            {
                Type = NativeMethods.InputMouse,
                Mouse = new NativeMethods.MouseInput { Flags = NativeMethods.MouseEventLeftDown }
            },
            new NativeMethods.Input
            {
                Type = NativeMethods.InputMouse,
                Mouse = new NativeMethods.MouseInput { Flags = NativeMethods.MouseEventLeftUp }
            }
        };

        return NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>()) == inputs.Length;
    }

    public IReadOnlyList<WindowMatch> EnumerateWindows()
    {
        var windows = new List<WindowMatch>();
        NativeMethods.EnumWindows((handle, _) =>
        {
            var match = TryCreateWindowMatch(handle);
            if (match is not null)
            {
                windows.Add(match);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static WindowMatch? TryCreateWindowMatch(IntPtr handle)
    {
        if (!NativeMethods.IsWindowVisible(handle))
        {
            return null;
        }

        var title = GetWindowTitle(handle);
        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        var processName = TryGetProcessName(processId);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        if (!NativeMethods.GetWindowRect(handle, out var rect) || rect.Width <= 0 || rect.Height <= 0)
        {
            return null;
        }

        return new WindowMatch(handle, title, processName, rect.Left, rect.Top, rect.Right, rect.Bottom, unchecked((int)processId));
    }

    private static string GetWindowTitle(IntPtr handle)
    {
        var length = NativeMethods.GetWindowTextLengthW(handle);
        if (length <= 0)
        {
            return "";
        }

        var builder = new StringBuilder(length + 1);
        NativeMethods.GetWindowTextW(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string TryGetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "";
        }
        catch (InvalidOperationException)
        {
            return "";
        }
    }

}

public static class WindowMatcher
{
    public static WindowMatch? SelectBest(IEnumerable<WindowMatch> windows, IReadOnlyList<string> processNames, int currentProcessId)
    {
        return windows
            .Where(window => IsCandidate(window, processNames, currentProcessId))
            .OrderByDescending(window => window.Area)
            .FirstOrDefault();
    }

    public static bool IsCandidate(WindowMatch window, IReadOnlyList<string> processNames, int currentProcessId)
    {
        if (currentProcessId > 0 && window.ProcessId == currentProcessId)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(window.ProcessName))
        {
            return false;
        }

        return processNames
            .Where(processName => !string.IsNullOrWhiteSpace(processName))
            .Any(processName => ProcessNamesEqual(window.ProcessName, processName));
    }

    private static bool ProcessNamesEqual(string actual, string expected)
    {
        return string.Equals(
            NormalizeProcessName(actual),
            NormalizeProcessName(expected),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string value)
    {
        return Path.GetFileNameWithoutExtension(value.Trim());
    }
}
