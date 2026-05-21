using System.Text;
using System.Text.Json;

namespace CodexFocus.Core.Transcript;

public sealed class CodexTranscriptReader
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);
    private readonly string sessionsRoot;
    private readonly DateTimeOffset now;

    public CodexTranscriptReader(string sessionsRoot)
        : this(sessionsRoot, DateTimeOffset.UtcNow)
    {
    }

    public CodexTranscriptReader(string sessionsRoot, DateTimeOffset now)
    {
        this.sessionsRoot = sessionsRoot;
        this.now = now;
    }

    public CodexTaskEvent? LatestTaskEvent()
    {
        return RecentCodexDesktopSessionFiles()
            .Select(LatestTaskEventInFile)
            .OfType<CodexTaskEvent>()
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.LineNumber)
            .LastOrDefault();
    }

    public CodexSessionState CurrentSessionStateAfter(CodexTaskEvent startEvent)
    {
        if (!File.Exists(startEvent.Path))
        {
            return new CodexSessionState(null, null);
        }

        var approvalCalls = new List<CodexApprovalEvent>();
        var finishedCallIds = new HashSet<string>(StringComparer.Ordinal);
        var lines = File.ReadLines(startEvent.Path).ToArray();

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];
            var timestamp = TryReadTimestamp(line);
            if (timestamp is null || timestamp < startEvent.Timestamp)
            {
                continue;
            }

            var taskEvent = TryParseTaskEvent(line, startEvent.Path, lineNumber);
            if (taskEvent is not null)
            {
                if (taskEvent.TurnId == startEvent.TurnId &&
                    (taskEvent.Kind == CodexTranscriptEventKind.TaskComplete ||
                     taskEvent.Kind == CodexTranscriptEventKind.TurnAborted))
                {
                    return new CodexSessionState(taskEvent, null);
                }

                continue;
            }

            var approval = TryParseApprovalRequest(line, startEvent.Path, lineNumber);
            if (approval is not null)
            {
                approvalCalls.Add(approval);
                continue;
            }

            var outputCallId = TryParseFunctionCallOutputId(line);
            if (outputCallId is not null)
            {
                finishedCallIds.Add(outputCallId);
            }
        }

        var pending = approvalCalls.LastOrDefault(approval => !finishedCallIds.Contains(approval.CallId));
        return new CodexSessionState(null, pending);
    }

    private CodexTaskEvent? LatestTaskEventInFile(string path)
    {
        CodexTaskEvent? latest = null;
        var lineNumber = 0;
        foreach (var line in ReadTailLines(path, 1_000_000))
        {
            lineNumber++;
            var taskEvent = TryParseTaskEvent(line, path, lineNumber);
            if (taskEvent is not null)
            {
                latest = taskEvent;
            }
        }

        return latest;
    }

    private IEnumerable<string> RecentCodexDesktopSessionFiles()
    {
        if (!Directory.Exists(sessionsRoot))
        {
            return [];
        }

        var cutoff = now.UtcDateTime - RecentWindow;
        return Directory.EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
            .Where(path => File.GetLastWriteTimeUtc(path) >= cutoff)
            .Where(IsCodexDesktopSession);
    }

    private static bool IsCodexDesktopSession(string path)
    {
        foreach (var line in File.ReadLines(path).Take(50))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                if (!StringEquals(document.RootElement, "type", "session_meta"))
                {
                    continue;
                }

                if (document.RootElement.TryGetProperty("payload", out var payload) &&
                    StringEquals(payload, "originator", "Codex Desktop"))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
            }
        }

        return false;
    }

    private static CodexTaskEvent? TryParseTaskEvent(string line, string path, int lineNumber)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("timestamp", out var timestampElement) ||
                !DateTimeOffset.TryParse(timestampElement.GetString(), out var timestamp) ||
                !document.RootElement.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("type", out var typeElement) ||
                !payload.TryGetProperty("turn_id", out var turnElement))
            {
                return null;
            }

            var kind = typeElement.GetString() switch
            {
                "task_started" => CodexTranscriptEventKind.TaskStarted,
                "task_complete" => CodexTranscriptEventKind.TaskComplete,
                "turn_aborted" => CodexTranscriptEventKind.TurnAborted,
                _ => (CodexTranscriptEventKind?)null
            };

            var turnId = turnElement.GetString();
            return kind is null || string.IsNullOrWhiteSpace(turnId)
                ? null
                : new CodexTaskEvent(kind.Value, turnId, timestamp, path, lineNumber);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CodexApprovalEvent? TryParseApprovalRequest(string line, string path, int lineNumber)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            if (!StringEquals(document.RootElement, "type", "response_item") ||
                !document.RootElement.TryGetProperty("payload", out var payload) ||
                !StringEquals(payload, "type", "function_call") ||
                !payload.TryGetProperty("call_id", out var callIdElement) ||
                !payload.TryGetProperty("arguments", out var argumentsElement))
            {
                return null;
            }

            var callId = callIdElement.GetString();
            var arguments = argumentsElement.GetString() ?? "";
            return string.IsNullOrWhiteSpace(callId) ||
                   !arguments.Contains("sandbox_permissions", StringComparison.Ordinal) ||
                   !arguments.Contains("require_escalated", StringComparison.Ordinal)
                ? null
                : new CodexApprovalEvent(callId, path, lineNumber);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryParseFunctionCallOutputId(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            if (!StringEquals(document.RootElement, "type", "response_item") ||
                !document.RootElement.TryGetProperty("payload", out var payload) ||
                !StringEquals(payload, "type", "function_call_output") ||
                !payload.TryGetProperty("call_id", out var callIdElement))
            {
                return null;
            }

            return callIdElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateTimeOffset? TryReadTimestamp(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.TryGetProperty("timestamp", out var timestampElement) &&
                   DateTimeOffset.TryParse(timestampElement.GetString(), out var timestamp)
                ? timestamp
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadTailLines(string path, int maxBytes)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var offset = stream.Length > maxBytes ? stream.Length - maxBytes : 0;
        stream.Seek(offset, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool StringEquals(JsonElement element, string propertyName, string expected)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               string.Equals(property.GetString(), expected, StringComparison.Ordinal);
    }
}
