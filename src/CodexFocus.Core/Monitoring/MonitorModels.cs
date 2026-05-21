using CodexFocus.Core.Transcript;

namespace CodexFocus.Core.Monitoring;

public enum FocusMonitorState
{
    Stopped,
    Idle,
    Busy,
    WaitingApproval
}

public interface ICodexTranscriptSource
{
    CodexTaskEvent? LatestTaskEvent();

    CodexSessionState CurrentSessionStateAfter(CodexTaskEvent startEvent);
}

public interface ICodexFocusActions
{
    Task ResumeDouyinAsync(CancellationToken cancellationToken);

    Task PauseDouyinAndReturnToCodexAsync(CancellationToken cancellationToken);
}
