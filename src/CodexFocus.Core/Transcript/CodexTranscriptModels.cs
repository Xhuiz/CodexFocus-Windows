namespace CodexFocus.Core.Transcript;

public enum CodexTranscriptEventKind
{
    TaskStarted,
    TaskComplete,
    TurnAborted
}

public sealed record CodexTaskEvent(
    CodexTranscriptEventKind Kind,
    string TurnId,
    DateTimeOffset Timestamp,
    string Path,
    int LineNumber)
{
    public string Key => $"{Path}#{LineNumber}#{Kind}#{TurnId}";
}

public sealed record CodexApprovalEvent(string CallId, string Path, int LineNumber);

public sealed record CodexSessionState(CodexTaskEvent? Completion, CodexApprovalEvent? PendingApproval);
