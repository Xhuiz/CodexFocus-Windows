# Codex Focus Windows

**Codex Focus Windows** 是一个面向 Windows 的 **Codex 抖音自动切换工具**。当 Codex Desktop 开始执行任务时，它会自动切到抖音 Windows 客户端并继续播放；当 Codex 任务完成、失败、终止或需要权限确认时，它会暂停抖音并切回 Codex。

English summary: **Codex Focus Windows** is a Windows productivity utility for Codex Desktop and Douyin. It automatically switches to Douyin while Codex is working, then pauses Douyin and returns to Codex when Codex needs your attention.

适合这些搜索和使用场景：

- Codex 执行长任务时自动切到抖音。
- Codex 等待权限确认时自动暂停抖音并切回 Codex。
- Windows 上的 Codex Desktop + 抖音客户端自动切换。
- Codex 自动播放/暂停抖音、Codex focus tool、Douyin auto switch for Codex Desktop。

这个项目参考了 [GeniusMarker/CodexFocus](https://github.com/GeniusMarker/CodexFocus) 的产品思路，但 Windows 版是基于 .NET 8 和 WPF 重新实现的版本。

## 功能

- 监听本机 Codex Desktop 的 session transcript。
- Codex 开始执行任务时自动切到抖音窗口并继续播放。
- Codex 完成任务、任务中断、失败或等待权限确认时暂停抖音并切回 Codex。
- 支持“切换延迟”设置，`0` 表示检测到任务后立即切换。
- 支持自定义 Codex 和抖音进程名。
- 支持启动后自动监听和开机自启。
- 提供窗口检测、手动测试切换和最近事件日志。

## 工作方式

1. 应用读取 `%USERPROFILE%\.codex\sessions` 下的 Codex Desktop transcript。
2. 检测到新的 Codex 任务开始后，激活抖音 Windows 客户端窗口。
3. 应用向抖音窗口中心发送一次点击，用于继续播放或暂停。
4. 检测到 Codex 完成、失败、中断或等待权限确认后，暂停抖音并切回 Codex。

窗口切换通过 Win32 API 完成，窗口匹配优先使用进程名，避免误切到浏览器、文档或聊天窗口。

## 使用前准备

1. 安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。
2. 打开 Codex Desktop。
3. 打开抖音 Windows 客户端。
4. 启动 Codex Focus Windows。
5. 确认界面里能检测到 Codex 窗口和抖音窗口。
6. 保持“启动后自动监听”开启，或手动点击“开始监听”。

## 下载和构建

如果仓库已经发布 Release，优先从 GitHub Releases 下载构建好的 Windows 程序。没有可下载版本时，可以从源码构建。

需要 .NET 8 SDK：

```powershell
dotnet build CodexFocus.sln -c Release
```

Release 产物位置：

```text
src\CodexFocus.Windows\bin\Release\net8.0-windows\CodexFocus.Windows.exe
```

## 配置

用户配置保存在：

```text
%APPDATA%\CodexFocusWindows\settings.json
```

常用配置项：

- `PollIntervalSeconds`：轮询间隔，默认 `0.2` 秒，支持小数。
- `TaskSwitchDelaySeconds`：Codex 开始任务后多久切到抖音，`0` 表示立即切换。
- `ActivationDelayMilliseconds`：激活抖音窗口后的点击等待时间，默认 `100` 毫秒。
- `ReturnDelayMilliseconds`：暂停抖音后切回 Codex 的等待时间，默认 `0` 毫秒。
- `DouyinWindowKeywords`：匹配抖音窗口的进程名，默认包含 `douyin`。
- `CodexWindowKeywords`：匹配 Codex 窗口的进程名，默认包含 `Codex`。

如果觉得切换太慢，把 `TaskSwitchDelaySeconds` 设为 `0`。如果短任务会来回切，把它调成 `1` 到 `3`。

## 测试

```powershell
dotnet run --project tests\CodexFocus.Tests\CodexFocus.Tests.csproj
```

## 常见问题

### Codex Focus Windows 是什么？

它是一个 Codex Desktop 和抖音 Windows 客户端之间的自动切换工具。Codex 工作时自动切到抖音，Codex 需要你处理时自动暂停抖音并切回 Codex。

### 它解决什么问题？

当 Codex 执行较长任务时，你可以把注意力临时切到抖音；当 Codex 完成任务或等待权限确认时，工具会把你带回 Codex，减少手动切窗口和漏看确认弹窗的情况。

### 支持网页版抖音吗？

首版主要面向抖音 Windows 客户端，不保证浏览器版抖音可用。

### 它会控制 Codex 吗？

不会。它只读取 Codex Desktop 的本地 transcript 来判断状态，并通过窗口激活和鼠标点击控制抖音播放/暂停。

### 为什么需要 .NET 8？

Windows 版基于 .NET 8 和 WPF 实现，因此运行和构建都需要对应的 .NET 运行时或 SDK。

### 可以用在 B 站、YouTube 或其他视频软件上吗？

当前版本优先支持抖音 Windows 客户端。其他媒体软件需要额外适配窗口匹配和播放/暂停逻辑。

## 搜索关键词

Codex 抖音自动切换、Codex 自动暂停抖音、Codex 自动播放抖音、CodexFocus Windows、Codex Desktop automation、Douyin auto switch、Windows focus tool、AI coding productivity tool。

## 注意事项

- 暂停/播放依赖窗口中心点击，如果抖音客户端界面布局变化，可能需要手动测试。
- 如果切回 Codex 时没有回到正确窗口，请检查 Codex 进程名设置是否与真实进程名一致。
- 本仓库当前未声明开源许可证；公开代码不等于自动授予再分发或商用许可。

## 撰稿人

详见 [CONTRIBUTORS.md](CONTRIBUTORS.md)。