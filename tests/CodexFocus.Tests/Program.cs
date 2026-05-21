using CodexFocus.Tests;

var tests = new (string Name, Action Body)[]
{
    ("Transcript parser keeps only Codex Desktop sessions", TranscriptReaderTests.KeepsOnlyCodexDesktopSessions),
    ("Transcript parser reads latest task event", TranscriptReaderTests.ReadsLatestTaskEvent),
    ("Transcript parser detects pending approval", TranscriptReaderTests.DetectsPendingApproval),
    ("Transcript parser ignores completed approval", TranscriptReaderTests.IgnoresCompletedApproval),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failed == 0 ? 0 : 1;
