using CodexFocus.Core.Transcript;

namespace CodexFocus.Tests;

internal static class TranscriptReaderTests
{
    public static void KeepsOnlyCodexDesktopSessions()
    {
        using var sandbox = TestSandbox.Create();
        var now = DateTimeOffset.Parse("2026-05-21T10:00:00.000Z");

        sandbox.WriteSession(
            "desktop.jsonl",
            """
            {"timestamp":"2026-05-21T09:59:58.000Z","type":"session_meta","payload":{"originator":"Codex Desktop"}}
            {"timestamp":"2026-05-21T09:59:59.000Z","type":"event","payload":{"type":"task_started","turn_id":"desktop-turn"}}
            """);
        sandbox.WriteSession(
            "vscode.jsonl",
            """
            {"timestamp":"2026-05-21T09:59:58.000Z","type":"session_meta","payload":{"originator":"codex_vscode"}}
            {"timestamp":"2026-05-21T10:00:00.000Z","type":"event","payload":{"type":"task_started","turn_id":"vscode-turn"}}
            """);

        var reader = new CodexTranscriptReader(sandbox.Root, now);
        var latest = reader.LatestTaskEvent();

        TestAssert.NotNull(latest, "Expected a Codex Desktop task event");
        TestAssert.Equal("desktop-turn", latest!.TurnId);
    }

    public static void ReadsLatestTaskEvent()
    {
        using var sandbox = TestSandbox.Create();
        var now = DateTimeOffset.Parse("2026-05-21T10:00:00.000Z");
        sandbox.WriteSession(
            "desktop.jsonl",
            """
            {"timestamp":"2026-05-21T09:59:56.000Z","type":"session_meta","payload":{"originator":"Codex Desktop"}}
            {"timestamp":"2026-05-21T09:59:57.000Z","type":"event","payload":{"type":"task_started","turn_id":"turn-1"}}
            {"timestamp":"2026-05-21T09:59:58.000Z","type":"event","payload":{"type":"task_complete","turn_id":"turn-1"}}
            """);

        var reader = new CodexTranscriptReader(sandbox.Root, now);
        var latest = reader.LatestTaskEvent();

        TestAssert.NotNull(latest, "Expected latest task event");
        TestAssert.Equal(CodexTranscriptEventKind.TaskComplete, latest!.Kind);
        TestAssert.Equal("turn-1", latest.TurnId);
        TestAssert.Equal(3, latest.LineNumber);
    }

    public static void DetectsPendingApproval()
    {
        using var sandbox = TestSandbox.Create();
        var now = DateTimeOffset.Parse("2026-05-21T10:00:00.000Z");
        sandbox.WriteSession(
            "desktop.jsonl",
            """
            {"timestamp":"2026-05-21T09:59:56.000Z","type":"session_meta","payload":{"originator":"Codex Desktop"}}
            {"timestamp":"2026-05-21T09:59:57.000Z","type":"event","payload":{"type":"task_started","turn_id":"turn-1"}}
            {"timestamp":"2026-05-21T09:59:58.000Z","type":"response_item","payload":{"type":"function_call","call_id":"call-1","arguments":"{\"sandbox_permissions\":\"require_escalated\"}"}}
            """);

        var reader = new CodexTranscriptReader(sandbox.Root, now);
        var start = reader.LatestTaskEvent();
        var state = reader.CurrentSessionStateAfter(start!);

        TestAssert.NotNull(state.PendingApproval, "Expected pending approval");
        TestAssert.Equal("call-1", state.PendingApproval!.CallId);
    }

    public static void IgnoresCompletedApproval()
    {
        using var sandbox = TestSandbox.Create();
        var now = DateTimeOffset.Parse("2026-05-21T10:00:00.000Z");
        sandbox.WriteSession(
            "desktop.jsonl",
            """
            {"timestamp":"2026-05-21T09:59:56.000Z","type":"session_meta","payload":{"originator":"Codex Desktop"}}
            {"timestamp":"2026-05-21T09:59:57.000Z","type":"event","payload":{"type":"task_started","turn_id":"turn-1"}}
            {"timestamp":"2026-05-21T09:59:58.000Z","type":"response_item","payload":{"type":"function_call","call_id":"call-1","arguments":"{\"sandbox_permissions\":\"require_escalated\"}"}}
            {"timestamp":"2026-05-21T09:59:59.000Z","type":"response_item","payload":{"type":"function_call_output","call_id":"call-1"}}
            """);

        var reader = new CodexTranscriptReader(sandbox.Root, now);
        var start = reader.LatestTaskEvent();
        var state = reader.CurrentSessionStateAfter(start!);

        TestAssert.True(state.PendingApproval is null, "Expected approval to be resolved");
    }
}

internal sealed class TestSandbox : IDisposable
{
    private TestSandbox(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static TestSandbox Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "CodexFocusTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TestSandbox(root);
    }

    public void WriteSession(string relativePath, string text)
    {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
