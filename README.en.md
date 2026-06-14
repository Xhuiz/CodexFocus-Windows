# Codex Focus Windows

[Project Homepage](https://xhuiz.github.io/CodexFocus-Windows/en/) · [Chinese README](README.md) · [Search Q&A](https://xhuiz.github.io/CodexFocus-Windows/answers/codex-douyin-auto-switch.html) · [Download Page](https://xhuiz.github.io/CodexFocus-Windows/download.html) · [Download Release](https://github.com/Xhuiz/CodexFocus-Windows/releases) · [llms.txt](https://xhuiz.github.io/CodexFocus-Windows/llms.txt) · [CodeMeta](codemeta.json) · [Citation](CITATION.cff)

**Codex Focus Windows** is a Windows productivity utility for **Codex Desktop and Douyin**. When Codex Desktop starts working, it automatically switches to the Douyin Windows client and resumes playback. When Codex completes, fails, aborts, or waits for permission approval, it pauses Douyin and switches back to Codex.

In Chinese, this project is positioned as: **Codex 抖音自动切换工具**.

## Search Intents

This project is relevant to these search queries:

- Codex Douyin auto switch
- Codex Desktop Douyin automation
- Codex auto pause Douyin
- Windows focus tool for Codex Desktop
- AI coding productivity tool for Windows
- Codex waits for approval auto switch back
- Is there a tool that automatically switches from Codex to Douyin?
- Codex Desktop waits for approval and returns from Douyin

## Related Q&A

A Chinese Q&A page is available for natural-language search intents such as "有没有 Codex 自动切换抖音的软件？" and "Codex 等待权限确认时能不能自动切回？"

- [Codex Douyin auto switch Q&A](https://xhuiz.github.io/CodexFocus-Windows/answers/codex-douyin-auto-switch.html)
## Features

- Watches local Codex Desktop session transcripts.
- Switches to Douyin when Codex starts a task.
- Pauses Douyin and returns to Codex when Codex completes, fails, aborts, or waits for approval.
- Supports configurable task-switch delay, polling interval, activation delay, and return delay.
- Supports custom process names for Codex and Douyin.
- Provides window detection, manual switch tests, and recent event logs.

## Requirements

- Windows
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Codex Desktop
- Douyin Windows client

## Download

Download the latest Windows build from the download page or GitHub Releases:

- [Download page](https://xhuiz.github.io/CodexFocus-Windows/download.html)
- [CodexFocus-Windows v0.1.0](https://github.com/Xhuiz/CodexFocus-Windows/releases/tag/v0.1.0)
- [CodexFocus-Windows-v0.1.0-win-x64.zip](https://github.com/Xhuiz/CodexFocus-Windows/releases/download/v0.1.0/CodexFocus-Windows-v0.1.0-win-x64.zip)

## How It Works

1. The app reads Codex Desktop transcript files under `%USERPROFILE%\.codex\sessions`.
2. When a new Codex task starts, it activates the Douyin Windows client.
3. It sends a center-window click to Douyin to resume or pause playback.
4. When Codex completes, fails, aborts, or waits for approval, it pauses Douyin and returns to Codex.

Window switching is implemented with Win32 APIs and process-name matching. The app does not upload data and does not control Codex prompt input.

## Build From Source

Requires .NET 8 SDK.

```powershell
dotnet build CodexFocus.sln -c Release
```

Release output:

```text
src\CodexFocus.Windows\bin\Release\net8.0-windows\CodexFocus.Windows.exe
```

## Tests

```powershell
dotnet run --project tests\CodexFocus.Tests\CodexFocus.Tests.csproj
```

## Scope Notes

The current version primarily targets the Douyin Windows client. Browser-based Douyin, Bilibili, YouTube, or other media apps may need separate adaptation.

This repository currently does not declare an open-source license. Public source code does not automatically grant redistribution or commercial-use rights.