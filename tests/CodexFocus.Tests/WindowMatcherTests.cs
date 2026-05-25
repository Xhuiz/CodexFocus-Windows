using CodexFocus.Windows.Windows;

namespace CodexFocus.Tests;

internal static class WindowMatcherTests
{
    public static void IgnoresTitleOnlyKeywordMatches()
    {
        var windows = new[]
        {
            Window("CodexFocus-Windows README - Chrome", "chrome", 1, area: 2_000_000)
        };

        var selected = WindowMatcher.SelectBest(windows, ["Codex"], currentProcessId: 99);

        TestAssert.Null(selected, "A browser title containing Codex should not match the Codex app");
    }

    public static void SelectsExactProcessNameOverMisleadingTitle()
    {
        var windows = new[]
        {
            Window("Codex notes - Chrome", "chrome", 1, area: 2_000_000),
            Window("Codex", "Codex", 2, area: 100_000)
        };

        var selected = WindowMatcher.SelectBest(windows, ["Codex"], currentProcessId: 99);

        TestAssert.NotNull(selected, "The real Codex process should be selected");
        TestAssert.Equal("Codex", selected!.ProcessName);
    }

    public static void IgnoresCurrentApplicationProcess()
    {
        var windows = new[]
        {
            Window("Codex Focus Windows", "CodexFocus.Windows", 42, area: 100_000)
        };

        var selected = WindowMatcher.SelectBest(windows, ["CodexFocus.Windows"], currentProcessId: 42);

        TestAssert.Null(selected, "The app should never target its own window");
    }

    public static void RejectsProcessNamesThatOnlyContainKeyword()
    {
        var windows = new[]
        {
            Window("Codex helper", "CodexFocus.Windows", 10, area: 100_000)
        };

        var selected = WindowMatcher.SelectBest(windows, ["Codex"], currentProcessId: 99);

        TestAssert.Null(selected, "CodexFocus.Windows should not match the exact Codex process target");
    }

    private static WindowMatch Window(string title, string processName, int processId, int area)
    {
        return new WindowMatch(
            (IntPtr)processId,
            title,
            processName,
            0,
            0,
            area,
            1,
            processId);
    }
}
