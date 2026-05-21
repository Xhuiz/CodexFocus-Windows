using System.IO;

namespace CodexFocus.Windows.Services;

public sealed class DebugFileLogger
{
    private readonly string path;

    public DebugFileLogger()
    {
        path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexFocusWindows",
            "logs",
            "debug.log");
    }

    public void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
