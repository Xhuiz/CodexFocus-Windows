namespace CodexFocus.Core.Settings;

public sealed class AppSettings
{
    public int PollIntervalSeconds { get; set; }

    public int ActivationDelayMilliseconds { get; set; }

    public int ReturnDelayMilliseconds { get; set; }

    public bool AutoStartMonitoring { get; set; }

    public bool StartWithWindows { get; set; }

    public List<string> DouyinWindowKeywords { get; set; } = [];

    public List<string> CodexWindowKeywords { get; set; } = [];

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            PollIntervalSeconds = 1,
            ActivationDelayMilliseconds = 250,
            ReturnDelayMilliseconds = 150,
            AutoStartMonitoring = true,
            StartWithWindows = false,
            DouyinWindowKeywords = ["抖音", "Douyin", "TikTok"],
            CodexWindowKeywords = ["Codex"]
        };
    }

    public void Normalize()
    {
        PollIntervalSeconds = Math.Clamp(PollIntervalSeconds, 1, 30);
        ActivationDelayMilliseconds = ActivationDelayMilliseconds <= 0 ? 250 : Math.Clamp(ActivationDelayMilliseconds, 50, 2_000);
        ReturnDelayMilliseconds = ReturnDelayMilliseconds <= 0 ? 150 : Math.Clamp(ReturnDelayMilliseconds, 50, 2_000);

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
