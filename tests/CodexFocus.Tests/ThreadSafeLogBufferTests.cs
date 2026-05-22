using CodexFocus.Windows.ViewModels;

namespace CodexFocus.Tests;

internal static class ThreadSafeLogBufferTests
{
    public static void DispatchesBackgroundWrites()
    {
        var hasUiAccess = false;
        var dispatched = new List<Action>();
        var persisted = new List<string>();
        var buffer = new ThreadSafeLogBuffer(
            () => hasUiAccess,
            action => dispatched.Add(action),
            message => persisted.Add(message),
            () => new DateTime(2026, 5, 22, 19, 10, 0));

        buffer.Add("已点击抖音窗口中心继续播放");

        TestAssert.Equal(0, buffer.Entries.Count);
        TestAssert.Equal(1, dispatched.Count);

        hasUiAccess = true;
        dispatched.Single().Invoke();

        TestAssert.Equal(1, buffer.Entries.Count);
        TestAssert.True(buffer.Entries[0].Contains("已点击抖音窗口中心继续播放"), "Expected dispatched log entry");
        TestAssert.Equal("已点击抖音窗口中心继续播放", persisted.Single());
    }
}
