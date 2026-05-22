using CodexFocus.Tests;

var tests = new (string Name, Action Body)[]
{
    ("Transcript parser keeps only Codex Desktop sessions", TranscriptReaderTests.KeepsOnlyCodexDesktopSessions),
    ("Transcript parser reads latest task event", TranscriptReaderTests.ReadsLatestTaskEvent),
    ("Transcript parser detects pending approval", TranscriptReaderTests.DetectsPendingApproval),
    ("Transcript parser ignores completed approval", TranscriptReaderTests.IgnoresCompletedApproval),
    ("Transcript parser reads active session file", TranscriptReaderTests.ReadsActiveSessionFile),
    ("Monitor baselines existing latest event on start", CodexActivityMonitorTests.BaselinesExistingLatestEventOnStart),
    ("Monitor waits before switching to Douyin", CodexActivityMonitorTests.WaitsBeforeSwitchingToDouyin),
    ("Monitor switches immediately when delay is zero", CodexActivityMonitorTests.SwitchesImmediatelyWhenDelayIsZero),
    ("Monitor skips switching when task completes before delay", CodexActivityMonitorTests.SkipsSwitchingWhenTaskCompletesBeforeDelay),
    ("Monitor resumes Douyin after sustained task", CodexActivityMonitorTests.ResumesDouyinAfterSustainedTask),
    ("Monitor pauses and returns on task completion", CodexActivityMonitorTests.PausesAndReturnsOnTaskCompletion),
    ("Monitor pauses once for approval and resumes after output", CodexActivityMonitorTests.PausesOnceForApprovalAndResumesAfterOutput),
    ("Monitor does not pause twice after approval return", CodexActivityMonitorTests.DoesNotPauseTwiceAfterApprovalReturn),
    ("Settings defaults match design", AppSettingsTests.DefaultsMatchDesign),
    ("Settings clamps invalid poll interval", AppSettingsTests.ClampsInvalidPollInterval),
    ("Settings allows zero task switch delay", AppSettingsTests.AllowsZeroTaskSwitchDelay),
    ("Log buffer dispatches background writes", ThreadSafeLogBufferTests.DispatchesBackgroundWrites),
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
