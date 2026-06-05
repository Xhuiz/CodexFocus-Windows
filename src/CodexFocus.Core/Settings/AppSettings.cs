namespace CodexFocus.Core.Settings;

public sealed class AppSettings
{
    public double PollIntervalSeconds { get; set; }

    public int ActivationDelayMilliseconds { get; set; }

    public int ReturnDelayMilliseconds { get; set; }

    public int TaskSwitchDelaySeconds { get; set; }

    public bool AutoStartMonitoring { get; set; }

    public bool StartWithWindows { get; set; }

    public List<string> DouyinWindowKeywords { get; set; } = [];

    public List<string> CodexWindowKeywords { get; set; } = [];

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            PollIntervalSeconds = 0.2,
            ActivationDelayMilliseconds = 100,
            ReturnDelayMilliseconds = 0,
            TaskSwitchDelaySeconds = 3,
            AutoStartMonitoring = true,
            StartWithWindows = false,
            DouyinWindowKeywords = ["抖音", "Douyin", "TikTok", "douyin"],
            CodexWindowKeywords = ["Codex"]
        };
    }

    public void Normalize()
    {
        PollIntervalSeconds = PollIntervalSeconds <= 0 ? 0.2 : Math.Clamp(PollIntervalSeconds, 0.1, 30);
        ActivationDelayMilliseconds = ActivationDelayMilliseconds <= 0 ? 100 : Math.Clamp(ActivationDelayMilliseconds, 50, 2_000);
        ReturnDelayMilliseconds = ReturnDelayMilliseconds < 0 ? 0 : Math.Clamp(ReturnDelayMilliseconds, 0, 2_000);
        TaskSwitchDelaySeconds = TaskSwitchDelaySeconds < 0 ? 3 : Math.Clamp(TaskSwitchDelaySeconds, 0, 60);

        if (DouyinWindowKeywords.Count == 0)
        {
            DouyinWindowKeywords = ["抖音", "Douyin", "TikTok", "douyin"];
        }

        if (CodexWindowKeywords.Count == 0)
        {
            CodexWindowKeywords = ["Codex"];
        }
    }
}
