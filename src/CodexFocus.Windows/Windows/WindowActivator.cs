using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CodexFocus.Windows.Windows;

public sealed record WindowMatch(IntPtr Handle, string Title, string ProcessName, int Left, int Top, int Right, int Bottom)
{
    public string Description => string.IsNullOrWhiteSpace(Title)
        ? ProcessName
        : $"{Title} ({ProcessName})";
}

public sealed class WindowActivator
{
    public WindowMatch? FindFirst(IReadOnlyList<string> keywords)
    {
        return EnumerateWindows()
            .Where(window => Matches(window, keywords))
            .OrderByDescending(window => (window.Right - window.Left) * (window.Bottom - window.Top))
            .FirstOrDefault();
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

        return new WindowMatch(handle, title, processName, rect.Left, rect.Top, rect.Right, rect.Bottom);
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

    private static bool Matches(WindowMatch window, IReadOnlyList<string> keywords)
    {
        return keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Any(keyword =>
                window.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                window.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
