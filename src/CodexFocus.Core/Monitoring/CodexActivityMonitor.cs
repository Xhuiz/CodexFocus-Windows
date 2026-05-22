using CodexFocus.Core.Transcript;

namespace CodexFocus.Core.Monitoring;

public sealed class CodexActivityMonitor
{
    private readonly ICodexTranscriptSource transcriptSource;
    private readonly ICodexFocusActions actions;
    private readonly TimeSpan switchDelay;
    private readonly Func<DateTimeOffset> now;
    private string? observedEventKey;
    private CodexTaskEvent? activeStartEvent;
    private DateTimeOffset? activeStartedAt;
    private string? activeApprovalCallId;
    private bool switchedToDouyin;
    private bool currentlyInDouyin;

    public CodexActivityMonitor(ICodexTranscriptSource transcriptSource, ICodexFocusActions actions)
        : this(transcriptSource, actions, TimeSpan.FromSeconds(3), () => DateTimeOffset.UtcNow)
    {
    }

    public CodexActivityMonitor(
        ICodexTranscriptSource transcriptSource,
        ICodexFocusActions actions,
        TimeSpan switchDelay,
        Func<DateTimeOffset> now)
    {
        this.transcriptSource = transcriptSource;
        this.actions = actions;
        this.switchDelay = switchDelay;
        this.now = now;
    }

    public FocusMonitorState State { get; private set; } = FocusMonitorState.Stopped;

    public string StatusText { get; private set; } = "已停止";

    public void Start()
    {
        observedEventKey = transcriptSource.LatestTaskEvent()?.Key;
        activeStartEvent = null;
        activeStartedAt = null;
        activeApprovalCallId = null;
        switchedToDouyin = false;
        currentlyInDouyin = false;
        State = FocusMonitorState.Idle;
        StatusText = "正在监听 Codex Desktop";
    }

    public void Stop()
    {
        observedEventKey = null;
        activeStartEvent = null;
        activeStartedAt = null;
        activeApprovalCallId = null;
        switchedToDouyin = false;
        currentlyInDouyin = false;
        State = FocusMonitorState.Stopped;
        StatusText = "已停止";
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        if (State == FocusMonitorState.Stopped)
        {
            return;
        }

        if (State == FocusMonitorState.Idle)
        {
            await TickIdleAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await TickActiveAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task TickIdleAsync(CancellationToken cancellationToken)
    {
        var latest = transcriptSource.LatestTaskEvent();
        if (latest is null)
        {
            StatusText = "未找到 Codex Desktop 任务事件";
            return Task.CompletedTask;
        }

        if (latest.Key == observedEventKey)
        {
            StatusText = "正在监听 Codex Desktop";
            return Task.CompletedTask;
        }

        observedEventKey = latest.Key;
        if (latest.Kind != CodexTranscriptEventKind.TaskStarted)
        {
            StatusText = "最新 Codex 任务已结束，继续监听";
            return Task.CompletedTask;
        }

        activeStartEvent = latest;
        activeStartedAt = now();
        activeApprovalCallId = null;
        switchedToDouyin = false;
        currentlyInDouyin = false;
        State = FocusMonitorState.Busy;
        StatusText = "Codex 任务进行中，等待确认是否为长任务";
        return Task.CompletedTask;
    }

    private async Task TickActiveAsync(CancellationToken cancellationToken)
    {
        if (activeStartEvent is null)
        {
            State = FocusMonitorState.Idle;
            StatusText = "任务状态已重置";
            return;
        }

        var sessionState = transcriptSource.CurrentSessionStateAfter(activeStartEvent);
        if (sessionState.Completion is not null)
        {
            await CompleteActiveTaskAsync(sessionState.Completion, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sessionState.PendingApproval is not null)
        {
            await HandlePendingApprovalAsync(sessionState.PendingApproval, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (State == FocusMonitorState.WaitingApproval && activeApprovalCallId is not null)
        {
            activeApprovalCallId = null;
            State = FocusMonitorState.Busy;
            StatusText = switchedToDouyin ? "权限已确认，已回到抖音" : "权限已确认，继续监听任务";
            if (switchedToDouyin && !currentlyInDouyin)
            {
                await actions.ResumeDouyinAsync(cancellationToken).ConfigureAwait(false);
                currentlyInDouyin = true;
            }

            return;
        }

        State = FocusMonitorState.Busy;
        if (!switchedToDouyin && activeStartedAt is not null && now() - activeStartedAt >= switchDelay)
        {
            switchedToDouyin = true;
            currentlyInDouyin = true;
            StatusText = "Codex 长任务进行中，已切到抖音";
            await actions.ResumeDouyinAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        StatusText = switchedToDouyin ? "Codex 任务进行中" : "Codex 任务进行中，暂不切换";
    }

    private async Task CompleteActiveTaskAsync(CodexTaskEvent completion, CancellationToken cancellationToken)
    {
        observedEventKey = completion.Key;
        var shouldPauseAndReturn = currentlyInDouyin;
        activeStartEvent = null;
        activeStartedAt = null;
        activeApprovalCallId = null;
        switchedToDouyin = false;
        currentlyInDouyin = false;
        State = FocusMonitorState.Idle;
        StatusText = completion.Kind == CodexTranscriptEventKind.TurnAborted
            ? "Codex 任务已中断"
            : "Codex 任务完成";

        if (shouldPauseAndReturn)
        {
            StatusText += "，正在暂停抖音并切回 Codex";
            await actions.PauseDouyinAndReturnToCodexAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            StatusText += "，未切换抖音";
        }
    }

    private async Task HandlePendingApprovalAsync(CodexApprovalEvent approval, CancellationToken cancellationToken)
    {
        State = FocusMonitorState.WaitingApproval;
        if (activeApprovalCallId == approval.CallId)
        {
            StatusText = "Codex 正在等待权限确认";
            return;
        }

        activeApprovalCallId = approval.CallId;
        StatusText = switchedToDouyin ? "Codex 等待权限确认，正在暂停抖音并切回" : "Codex 等待权限确认";
        if (currentlyInDouyin)
        {
            await actions.PauseDouyinAndReturnToCodexAsync(cancellationToken).ConfigureAwait(false);
            currentlyInDouyin = false;
        }
    }
}
