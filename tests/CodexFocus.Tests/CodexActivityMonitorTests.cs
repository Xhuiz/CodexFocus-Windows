using CodexFocus.Core.Monitoring;
using CodexFocus.Core.Transcript;

namespace CodexFocus.Tests;

internal static class CodexActivityMonitorTests
{
    public static void BaselinesExistingLatestEventOnStart()
    {
        var start = TaskEvent(CodexTranscriptEventKind.TaskStarted, "turn-1", line: 2);
        var source = new FakeTranscriptSource { Latest = start };
        var actions = new FakeFocusActions();
        var monitor = new CodexActivityMonitor(source, actions);

        monitor.Start();
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        TestAssert.Equal(FocusMonitorState.Idle, monitor.State);
        TestAssert.Equal(0, actions.Calls.Count);
    }

    public static void ResumesDouyinOnNewTask()
    {
        var source = new FakeTranscriptSource();
        var actions = new FakeFocusActions();
        var monitor = new CodexActivityMonitor(source, actions);

        monitor.Start();
        source.Latest = TaskEvent(CodexTranscriptEventKind.TaskStarted, "turn-1", line: 2);
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        TestAssert.Equal(FocusMonitorState.Busy, monitor.State);
        TestAssert.Equal("resume", actions.Calls.Single());
    }

    public static void PausesAndReturnsOnTaskCompletion()
    {
        var start = TaskEvent(CodexTranscriptEventKind.TaskStarted, "turn-1", line: 2);
        var complete = TaskEvent(CodexTranscriptEventKind.TaskComplete, "turn-1", line: 5);
        var source = new FakeTranscriptSource();
        var actions = new FakeFocusActions();
        var monitor = new CodexActivityMonitor(source, actions);

        monitor.Start();
        source.Latest = start;
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();
        source.State = new CodexSessionState(complete, null);
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        TestAssert.Equal(FocusMonitorState.Idle, monitor.State);
        TestAssert.Equal("resume,pause", string.Join(",", actions.Calls));
    }

    public static void PausesOnceForApprovalAndResumesAfterOutput()
    {
        var start = TaskEvent(CodexTranscriptEventKind.TaskStarted, "turn-1", line: 2);
        var approval = new CodexApprovalEvent("call-1", "session.jsonl", 3);
        var source = new FakeTranscriptSource();
        var actions = new FakeFocusActions();
        var monitor = new CodexActivityMonitor(source, actions);

        monitor.Start();
        source.Latest = start;
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        source.State = new CodexSessionState(null, approval);
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        source.State = new CodexSessionState(null, null);
        monitor.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

        TestAssert.Equal(FocusMonitorState.Busy, monitor.State);
        TestAssert.Equal("resume,pause,resume", string.Join(",", actions.Calls));
    }

    private static CodexTaskEvent TaskEvent(CodexTranscriptEventKind kind, string turnId, int line)
    {
        return new CodexTaskEvent(
            kind,
            turnId,
            DateTimeOffset.Parse($"2026-05-21T10:00:0{line}.000Z"),
            "session.jsonl",
            line);
    }

    private sealed class FakeTranscriptSource : ICodexTranscriptSource
    {
        public CodexTaskEvent? Latest { get; set; }

        public CodexSessionState State { get; set; } = new(null, null);

        public CodexTaskEvent? LatestTaskEvent()
        {
            return Latest;
        }

        public CodexSessionState CurrentSessionStateAfter(CodexTaskEvent startEvent)
        {
            return State;
        }
    }

    private sealed class FakeFocusActions : ICodexFocusActions
    {
        public List<string> Calls { get; } = [];

        public Task ResumeDouyinAsync(CancellationToken cancellationToken)
        {
            Calls.Add("resume");
            return Task.CompletedTask;
        }

        public Task PauseDouyinAndReturnToCodexAsync(CancellationToken cancellationToken)
        {
            Calls.Add("pause");
            return Task.CompletedTask;
        }
    }
}
