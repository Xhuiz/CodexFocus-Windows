namespace CodexFocus.Core.Settings;

public sealed class AppSettings
{
    public int PollIntervalSeconds { get; set; }

    public bool AutoStartMonitoring { get; set; }

    public bool StartWithWindows { get; set; }

    public List<string> DouyinWindowKeywords { get; set; } = [];

    public List<string> CodexWindowKeywords { get; set; } = [];

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            PollIntervalSeconds = 2,
            AutoStartMonitoring = true,
            StartWithWindows = false,
            DouyinWindowKeywords = ["抖音", "Douyin", "TikTok"],
            CodexWindowKeywords = ["Codex"]
        };
    }

    public void Normalize()
    {
        PollIntervalSeconds = Math.Clamp(PollIntervalSeconds, 1, 30);

        if (DouyinWindowKeywords.Count == 0)
        {
            DouyinWindowKeywords = ["抖音", "Douyin", "TikTok"];
        }

        if (CodexWindowKeywords.Count == 0)
        {
            CodexWindowKeywords = ["Codex"];
        }
    }
}
