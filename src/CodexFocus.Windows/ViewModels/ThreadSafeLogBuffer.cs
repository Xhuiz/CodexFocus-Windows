using System.Collections.ObjectModel;

namespace CodexFocus.Windows.ViewModels;

public sealed class ThreadSafeLogBuffer
{
    private readonly Func<bool> hasUiAccess;
    private readonly Action<Action> dispatch;
    private readonly Action<string> persist;
    private readonly Func<DateTime> now;
    private readonly int capacity;

    public ThreadSafeLogBuffer(
        Func<bool> hasUiAccess,
        Action<Action> dispatch,
        Action<string> persist,
        Func<DateTime>? now = null,
        int capacity = 80)
    {
        this.hasUiAccess = hasUiAccess;
        this.dispatch = dispatch;
        this.persist = persist;
        this.now = now ?? (() => DateTime.Now);
        this.capacity = capacity;
    }

    public ObservableCollection<string> Entries { get; } = [];

    public void Add(string message)
    {
        if (!hasUiAccess())
        {
            dispatch(() => Add(message));
            return;
        }

        var line = $"{now():HH:mm:ss} {message}";
        Entries.Insert(0, line);
        while (Entries.Count > capacity)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }

        persist(message);
    }
}
