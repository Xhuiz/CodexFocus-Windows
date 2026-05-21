using CodexFocus.Core.Settings;

namespace CodexFocus.Tests;

internal static class AppSettingsTests
{
    public static void DefaultsMatchDesign()
    {
        var settings = AppSettings.CreateDefault();

        TestAssert.Equal(2, settings.PollIntervalSeconds);
        TestAssert.True(settings.AutoStartMonitoring, "AutoStartMonitoring should default to true");
        TestAssert.False(settings.StartWithWindows, "StartWithWindows should default to false");
        TestAssert.True(settings.DouyinWindowKeywords.Contains("Douyin"), "Douyin keyword should be present");
        TestAssert.True(settings.DouyinWindowKeywords.Contains("TikTok"), "TikTok keyword should be present");
        TestAssert.True(settings.CodexWindowKeywords.Contains("Codex"), "Codex keyword should be present");
    }

    public static void ClampsInvalidPollInterval()
    {
        var settings = AppSettings.CreateDefault();
        settings.PollIntervalSeconds = 0;

        settings.Normalize();

        TestAssert.Equal(1, settings.PollIntervalSeconds);
    }
}
