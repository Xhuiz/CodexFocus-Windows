using CodexFocus.Core.Transcript;

namespace CodexFocus.Core.Monitoring;

public sealed class CodexActivityMonitor
{
    private readonly ICodexTranscriptSource transcriptSource;
    private readonly ICodexFocusActions actions;
    private string? observedEventKey;
    private CodexTaskEvent? activeStartEvent;
    private string? activeApprovalCallId;

    public CodexActivityMonitor(ICodexTranscriptSource transcriptSource, ICodexFocusActions actions)
    {
        this.transcriptSource = transcriptSource;
        this.actions = actions;
    }

    public FocusMonitorState State { get; private set; } = FocusMonitorState.Stopped;

    public string StatusText { get; private set; } = "已停止";

    public void Start()
    {
        observedEventKey = transcriptSource.LatestTaskEvent()?.Key;
        activeStartEvent = null;
        activeApprovalCallId = null;
        State = FocusMonitorState.Idle;
        StatusText = "正在监听 Codex Desktop";
    }

    public void Stop()
    {
        observedEventKey = null;
        activeStartEvent = null;
        activeApprovalCallId = null;
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

    private async Task TickIdleAsync(CancellationToken cancellationToken)
    {
        var latest = transcriptSource.LatestTaskEvent();
        if (latest is null)
        {
            StatusText = "未找到 Codex Desktop 任务事件";
            return;
        }

        if (latest.Key == observedEventKey)
        {
            StatusText = "正在监听 Codex Desktop";
            return;
        }

        observedEventKey = latest.Key;
        if (latest.Kind != CodexTranscriptEventKind.TaskStarted)
        {
            StatusText = "最新 Codex 任务已结束，继续监听";
            return;
        }

        activeStartEvent = latest;
        activeApprovalCallId = null;
        State = FocusMonitorState.Busy;
        StatusText = "Codex 任务进行中";
        await actions.ResumeDouyinAsync(cancellationToken).ConfigureAwait(false);
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
            observedEventKey = sessionState.Completion.Key;
            activeStartEvent = null;
            activeApprovalCallId = null;
            State = FocusMonitorState.Idle;
            StatusText = sessionState.Completion.Kind == CodexTranscriptEventKind.TurnAborted
                ? "Codex 任务已中断，已切回"
                : "Codex 任务完成，已切回";
            await actions.PauseDouyinAndReturnToCodexAsync(cancellationToken).ConfigureAwait(false);
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
            StatusText = "权限已确认，已回到抖音";
            await actions.ResumeDouyinAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        State = FocusMonitorState.Busy;
        StatusText = "Codex 任务进行中";
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
        StatusText = "Codex 等待权限确认，已切回";
        await actions.PauseDouyinAndReturnToCodexAsync(cancellationToken).ConfigureAwait(false);
    }
}
