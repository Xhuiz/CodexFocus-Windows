# Codex Focus Windows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows `.NET 8 + WPF` desktop app that monitors Codex Desktop transcript events and controls the Douyin Windows client by activating its window and clicking the center of the video window.

**Architecture:** Put transcript parsing, monitor state, settings models, and action abstractions in a testable `CodexFocus.Core` class library. Put WPF UI, Win32 window activation, mouse input, registry startup, and file logging in `CodexFocus.Windows`. Use a no-NuGet console test harness in `CodexFocus.Tests` so tests can run with only the .NET SDK.

**Tech Stack:** .NET 8 SDK, WPF on `net8.0-windows`, C# 12, Win32 P/Invoke, `System.Text.Json`, custom console-based unit tests.

---

## File Structure

- `CodexFocus.sln` - solution containing all projects.
- `src/CodexFocus.Core/CodexFocus.Core.csproj` - pure logic library.
- `src/CodexFocus.Core/Transcript/CodexTranscriptReader.cs` - scans and parses Codex JSONL transcript files.
- `src/CodexFocus.Core/Transcript/CodexTranscriptModels.cs` - transcript event models.
- `src/CodexFocus.Core/Monitoring/CodexActivityMonitor.cs` - idle, busy, and approval state machine.
- `src/CodexFocus.Core/Monitoring/MonitorModels.cs` - monitor state, status, and action abstractions.
- `src/CodexFocus.Core/Settings/AppSettings.cs` - persisted app settings model and defaults.
- `src/CodexFocus.Windows/CodexFocus.Windows.csproj` - WPF app.
- `src/CodexFocus.Windows/App.xaml` and `App.xaml.cs` - WPF startup and dependency wiring.
- `src/CodexFocus.Windows/MainWindow.xaml` and `MainWindow.xaml.cs` - main tool UI.
- `src/CodexFocus.Windows/ViewModels/MainWindowViewModel.cs` - state, settings, commands, and log bindings.
- `src/CodexFocus.Windows/ViewModels/ObservableObject.cs` - minimal property notification helper.
- `src/CodexFocus.Windows/ViewModels/RelayCommand.cs` - command helper.
- `src/CodexFocus.Windows/Services/JsonSettingsStore.cs` - `%APPDATA%` JSON settings persistence.
- `src/CodexFocus.Windows/Services/DebugFileLogger.cs` - `%LOCALAPPDATA%` debug log writer.
- `src/CodexFocus.Windows/Services/StartupRegistryService.cs` - current-user startup registry integration.
- `src/CodexFocus.Windows/Windows/NativeMethods.cs` - Win32 declarations.
- `src/CodexFocus.Windows/Windows/WindowActivator.cs` - top-level window discovery and activation.
- `src/CodexFocus.Windows/Windows/DouyinController.cs` - pause and resume actions.
- `tests/CodexFocus.Tests/CodexFocus.Tests.csproj` - console test harness.
- `tests/CodexFocus.Tests/Program.cs` - runs tests and exits non-zero on failure.
- `tests/CodexFocus.Tests/TestAssert.cs` - minimal assertion helpers.
- `tests/CodexFocus.Tests/TranscriptReaderTests.cs` - parser tests.
- `tests/CodexFocus.Tests/CodexActivityMonitorTests.cs` - state machine tests.
- `tests/CodexFocus.Tests/AppSettingsTests.cs` - settings default tests.

---

### Task 1: Tooling and Solution Skeleton

**Files:**
- Create: `.gitignore`
- Create: `global.json`
- Create: `CodexFocus.sln`
- Create: `src/CodexFocus.Core/CodexFocus.Core.csproj`
- Create: `src/CodexFocus.Windows/CodexFocus.Windows.csproj`
- Create: `tests/CodexFocus.Tests/CodexFocus.Tests.csproj`
- Create: `tests/CodexFocus.Tests/Program.cs`
- Create: `tests/CodexFocus.Tests/TestAssert.cs`

- [ ] **Step 1: Ensure worktree isolation**

Run from the main repository:

```powershell
git status --short
if (-not (Test-Path .gitignore)) { New-Item -ItemType File .gitignore | Out-Null }
```

Expected: no uncommitted application code. If `.worktrees/` is not ignored, add `.worktrees/` to `.gitignore` and commit that before creating the worktree.

- [ ] **Step 2: Install local .NET SDK if needed**

Run:

```powershell
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  New-Item -ItemType Directory -Force .tools | Out-Null
  Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile .tools/dotnet-install.ps1
  powershell -ExecutionPolicy Bypass -File .tools/dotnet-install.ps1 -Channel 8.0 -InstallDir .tools/dotnet
  $env:PATH = (Resolve-Path .tools/dotnet).Path + ";" + $env:PATH
}
dotnet --info
```

Expected: `dotnet --info` prints an SDK version under channel 8.0.

- [ ] **Step 3: Create project files and empty test harness**

Create `global.json`:

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "8.0.100"
  }
}
```

Create `tests/CodexFocus.Tests/Program.cs`:

```csharp
using CodexFocus.Tests;

var tests = new (string Name, Action Body)[]
{
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
```

Create `tests/CodexFocus.Tests/TestAssert.cs`:

```csharp
namespace CodexFocus.Tests;

internal static class TestAssert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}");
        }
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void NotNull(object? value, string message)
    {
        if (value is null)
        {
            throw new InvalidOperationException(message);
        }
    }
}
```

- [ ] **Step 4: Run baseline tests**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: exits with code 0 and no failed tests.

- [ ] **Step 5: Commit skeleton**

```powershell
git add .gitignore global.json CodexFocus.sln src tests
git commit -m "chore: scaffold Codex Focus Windows solution"
```

---

### Task 2: Transcript Parser

**Files:**
- Create: `src/CodexFocus.Core/Transcript/CodexTranscriptModels.cs`
- Create: `src/CodexFocus.Core/Transcript/CodexTranscriptReader.cs`
- Create: `tests/CodexFocus.Tests/TranscriptReaderTests.cs`
- Modify: `tests/CodexFocus.Tests/Program.cs`

- [ ] **Step 1: Write failing parser tests**

Add tests for these behaviors:

```csharp
("Transcript parser keeps only Codex Desktop sessions", TranscriptReaderTests.KeepsOnlyCodexDesktopSessions),
("Transcript parser reads latest task event", TranscriptReaderTests.ReadsLatestTaskEvent),
("Transcript parser detects pending approval", TranscriptReaderTests.DetectsPendingApproval),
("Transcript parser ignores completed approval", TranscriptReaderTests.IgnoresCompletedApproval),
```

The first test creates two JSONL files, one with `originator` set to `Codex Desktop` and one set to `codex_vscode`, then asserts only the Codex Desktop file contributes events.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: compile fails because `CodexTranscriptReader` and related models do not exist.

- [ ] **Step 3: Implement parser models and reader**

Implement:

```csharp
public enum CodexTranscriptEventKind { TaskStarted, TaskComplete, TurnAborted }
public sealed record CodexTaskEvent(CodexTranscriptEventKind Kind, string TurnId, DateTimeOffset Timestamp, string Path, int LineNumber);
public sealed record CodexApprovalEvent(string CallId, string Path, int LineNumber);
public sealed record CodexSessionState(CodexTaskEvent? Completion, CodexApprovalEvent? PendingApproval);
```

`CodexTranscriptReader` must expose:

```csharp
public CodexTaskEvent? LatestTaskEvent();
public CodexSessionState CurrentSessionStateAfter(CodexTaskEvent startEvent);
```

It reads `.jsonl` files from the configured sessions root, filters `session_meta.payload.originator == "Codex Desktop"`, parses task events, and pairs approval `function_call` with `function_call_output`.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: all parser tests pass.

- [ ] **Step 5: Commit parser**

```powershell
git add src/CodexFocus.Core tests/CodexFocus.Tests
git commit -m "feat: parse Codex Desktop transcript events"
```

---

### Task 3: Monitor State Machine

**Files:**
- Create: `src/CodexFocus.Core/Monitoring/MonitorModels.cs`
- Create: `src/CodexFocus.Core/Monitoring/CodexActivityMonitor.cs`
- Create: `tests/CodexFocus.Tests/CodexActivityMonitorTests.cs`
- Modify: `tests/CodexFocus.Tests/Program.cs`

- [ ] **Step 1: Write failing monitor tests**

Add tests for:

```csharp
("Monitor baselines existing latest event on start", CodexActivityMonitorTests.BaselinesExistingLatestEventOnStart),
("Monitor resumes Douyin on new task", CodexActivityMonitorTests.ResumesDouyinOnNewTask),
("Monitor pauses and returns on task completion", CodexActivityMonitorTests.PausesAndReturnsOnTaskCompletion),
("Monitor pauses once for approval and resumes after output", CodexActivityMonitorTests.PausesOnceForApprovalAndResumesAfterOutput),
```

Use fake transcript source and fake action controller. The fake action controller records calls like `ResumeDouyin`, `PauseDouyinAndReturnToCodex`.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: compile fails because monitor interfaces and state machine do not exist.

- [ ] **Step 3: Implement state machine**

Implement:

```csharp
public enum FocusMonitorState { Stopped, Idle, Busy, WaitingApproval }
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
```

`CodexActivityMonitor` exposes `Start()`, `Stop()`, and `TickAsync(CancellationToken)`. It baselines the latest task on `Start()`, tracks the active `turn_id`, and calls actions on task start, completion, abort, and approval transitions.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: all monitor tests pass.

- [ ] **Step 5: Commit monitor**

```powershell
git add src/CodexFocus.Core tests/CodexFocus.Tests
git commit -m "feat: add Codex activity monitor state machine"
```

---

### Task 4: Settings Model and Persistence

**Files:**
- Create: `src/CodexFocus.Core/Settings/AppSettings.cs`
- Create: `src/CodexFocus.Windows/Services/JsonSettingsStore.cs`
- Create: `src/CodexFocus.Windows/Services/StartupRegistryService.cs`
- Create: `tests/CodexFocus.Tests/AppSettingsTests.cs`
- Modify: `tests/CodexFocus.Tests/Program.cs`

- [ ] **Step 1: Write failing settings tests**

Add tests for:

```csharp
("Settings defaults match design", AppSettingsTests.DefaultsMatchDesign),
("Settings clamps invalid poll interval", AppSettingsTests.ClampsInvalidPollInterval),
```

Assert defaults: poll interval 2 seconds, auto start monitoring true, start with Windows false, Douyin keywords include `Douyin` and `TikTok`, Codex keywords include `Codex`.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: compile fails because `AppSettings` does not exist.

- [ ] **Step 3: Implement settings**

Implement `AppSettings.CreateDefault()` and `Normalize()` in core. Implement `JsonSettingsStore` in the Windows app with `Load()` and `Save(AppSettings settings)`, storing JSON under `%APPDATA%\CodexFocusWindows\settings.json`. Implement current-user startup registry methods in `StartupRegistryService`.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: all settings tests pass.

- [ ] **Step 5: Commit settings**

```powershell
git add src tests
git commit -m "feat: add settings defaults and persistence"
```

---

### Task 5: Win32 Window Control

**Files:**
- Create: `src/CodexFocus.Windows/Windows/NativeMethods.cs`
- Create: `src/CodexFocus.Windows/Windows/WindowActivator.cs`
- Create: `src/CodexFocus.Windows/Windows/DouyinController.cs`

- [ ] **Step 1: Build against missing Win32 classes**

Add Windows app references to the planned classes from `App.xaml.cs` only after the files exist. This task does not add unit tests because real foreground activation and `SendInput` are OS side effects; the testable behavior is already covered by `ICodexFocusActions` fakes.

- [ ] **Step 2: Implement native declarations**

Declare `EnumWindows`, `GetWindowText`, `GetWindowTextLength`, `IsWindowVisible`, `GetWindowRect`, `IsIconic`, `ShowWindow`, `SetForegroundWindow`, `GetWindowThreadProcessId`, and `SendInput`.

- [ ] **Step 3: Implement window matching**

`WindowActivator` exposes:

```csharp
public WindowMatch? FindFirst(IReadOnlyList<string> keywords);
public bool Activate(WindowMatch match);
public bool ClickCenter(WindowMatch match);
```

It matches visible top-level windows by title keywords and process name keywords, restores minimized windows, activates the target, and uses its rectangle center for clicks.

- [ ] **Step 4: Implement Douyin actions**

`DouyinController` implements `ICodexFocusActions`. `ResumeDouyinAsync()` activates Douyin and clicks center only when `pausedByApp` is true. `PauseDouyinAndReturnToCodexAsync()` activates Douyin, clicks center when needed, sets `pausedByApp`, then activates Codex.

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build CodexFocus.sln
```

Expected: solution builds with no errors.

- [ ] **Step 6: Commit Win32 control**

```powershell
git add src/CodexFocus.Windows
git commit -m "feat: control Douyin and Codex windows"
```

---

### Task 6: WPF UI and ViewModel

**Files:**
- Create: `src/CodexFocus.Windows/App.xaml`
- Create: `src/CodexFocus.Windows/App.xaml.cs`
- Create: `src/CodexFocus.Windows/MainWindow.xaml`
- Create: `src/CodexFocus.Windows/MainWindow.xaml.cs`
- Create: `src/CodexFocus.Windows/ViewModels/ObservableObject.cs`
- Create: `src/CodexFocus.Windows/ViewModels/RelayCommand.cs`
- Create: `src/CodexFocus.Windows/ViewModels/MainWindowViewModel.cs`
- Create: `src/CodexFocus.Windows/Services/DebugFileLogger.cs`

- [ ] **Step 1: Implement UI helpers**

Create `ObservableObject` using `INotifyPropertyChanged`, and `RelayCommand` using `ICommand`.

- [ ] **Step 2: Implement ViewModel**

`MainWindowViewModel` loads settings, creates `CodexTranscriptReader`, `CodexActivityMonitor`, `WindowActivator`, and `DouyinController`, exposes status text, window detection text, log entries, start/stop commands, and test action commands.

- [ ] **Step 3: Implement XAML**

Build a practical tool UI with status header, window detection area, settings fields, buttons, and recent log list. Use standard WPF controls, no decorative landing page.

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build CodexFocus.sln
```

Expected: solution builds with no errors.

- [ ] **Step 5: Commit UI**

```powershell
git add src/CodexFocus.Windows
git commit -m "feat: add WPF control panel"
```

---

### Task 7: Verification and Manual Run

**Files:**
- Modify only if verification finds a bug in files from earlier tasks.

- [ ] **Step 1: Run automated tests**

Run:

```powershell
dotnet run --project tests/CodexFocus.Tests/CodexFocus.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 2: Build release**

Run:

```powershell
dotnet build CodexFocus.sln -c Release
```

Expected: build succeeds.

- [ ] **Step 3: Launch app for smoke test**

Run:

```powershell
dotnet run --project src/CodexFocus.Windows/CodexFocus.Windows.csproj
```

Expected: WPF window opens. If Codex or Douyin are not running, status shows clear missing-window messages without crashing.

- [ ] **Step 4: Commit verification fixes**

If any fixes were needed:

```powershell
git add src tests
git commit -m "fix: address verification issues"
```

If no fixes were needed, do not create an empty commit.

