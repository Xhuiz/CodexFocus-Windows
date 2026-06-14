# Contributing

Thanks for helping improve Codex Focus Windows.

Codex Focus Windows is a Windows utility for Codex Desktop and Douyin. In Chinese search terms, it is a Codex 抖音自动切换工具: Codex Desktop 工作时自动切到抖音，任务完成或等待确认时暂停抖音并切回 Codex。

## Development Setup

Requirements:

- Windows
- .NET 8 SDK
- Codex Desktop
- Douyin Windows client, for manual switching tests

Build:

```powershell
dotnet build CodexFocus.sln -c Release
```

Run tests:

```powershell
dotnet run --project tests\CodexFocus.Tests\CodexFocus.Tests.csproj
```

## Useful Areas For Contributions

- More reliable Codex task-state detection.
- Better window detection for Codex Desktop and Douyin.
- Safer pause/resume behavior for the Douyin Windows client.
- Documentation for Windows setup and troubleshooting.
- Optional adapters for other media apps, if implemented clearly and separately.

## Community Guidelines

Please follow [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Do not post private Codex prompts, transcripts, credentials, tokens, or personal data.

## Issue And PR Guidelines

- Keep changes focused.
- Include reproduction steps for bugs.
- Add or update tests for state-detection logic when possible.
- Do not include private Codex transcripts, prompts, credentials, tokens, or personal data.

## License Note

This repository currently does not declare an open-source license. Public source code does not automatically grant redistribution or commercial-use rights. Please discuss licensing expectations before contributing substantial code.