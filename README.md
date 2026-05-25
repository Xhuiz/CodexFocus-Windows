# Codex Focus Windows

Codex Focus Windows 是一个 Windows 桌面工具，用来在 Codex Desktop 执行任务时自动切到抖音，等 Codex 完成、失败、终止或需要权限确认时暂停抖音并切回 Codex。

这个项目参考了 [GeniusMarker/CodexFocus](https://github.com/GeniusMarker/CodexFocus) 的产品思路，但 Windows 版是基于 .NET 8 和 WPF 重新实现的版本。

## 功能

- 监听本机 Codex Desktop 的 session transcript。
- Codex 开始执行任务时自动切到抖音窗口并继续播放。
- Codex 完成任务、任务中断或等待权限确认时暂停抖音并切回 Codex。
- 支持“切换延迟”设置，`0` 表示检测到任务后立即切换。
- 支持自定义 Codex 和抖音进程名。
- 支持启动后自动监听和开机自启。
- 提供窗口检测、手动测试切换和最近事件日志。

## 使用前准备

1. 安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。
2. 打开 Codex Desktop。
3. 打开抖音 Windows 客户端。
4. 启动 Codex Focus Windows。
5. 确认界面里能检测到 Codex 窗口和抖音窗口。
6. 保持“启动后自动监听”开启，或手动点击“开始监听”。

## 配置

用户配置保存在：

```text
%APPDATA%\CodexFocusWindows\settings.json
```

常用配置项：

- `PollIntervalSeconds`：轮询间隔，默认 `1` 秒。
- `TaskSwitchDelaySeconds`：Codex 开始任务后多久切到抖音，`0` 表示立即切换。
- `ActivationDelayMilliseconds`：激活抖音窗口后的点击等待时间。
- `ReturnDelayMilliseconds`：暂停抖音后切回 Codex 的等待时间。
- `DouyinWindowKeywords`：匹配抖音窗口的进程名，默认包含 `douyin`。
- `CodexWindowKeywords`：匹配 Codex 窗口的进程名，默认包含 `Codex`。

如果觉得切换太慢，把 `TaskSwitchDelaySeconds` 设为 `0`。如果觉得短任务会来回切，把它调成 `1` 到 `3`。

## 构建

需要 .NET 8 SDK。

```powershell
dotnet build CodexFocus.sln -c Release
```

Release 产物位置：

```text
src\CodexFocus.Windows\bin\Release\net8.0-windows\CodexFocus.Windows.exe
```

## 测试

```powershell
dotnet run --project tests\CodexFocus.Tests\CodexFocus.Tests.csproj
```

## 运行逻辑

应用会读取 `%USERPROFILE%\.codex\sessions` 下的 Codex Desktop transcript，并只处理 Codex Desktop 产生的任务事件。

主要状态：

- `Idle`：正在监听，没有活跃任务。
- `Busy`：检测到 Codex 正在执行任务。
- `WaitingApproval`：Codex 正在等待权限确认。

窗口切换通过 Win32 API 完成：枚举顶层窗口、按进程名精确匹配目标软件、激活目标窗口，并向抖音窗口中心发送鼠标点击来播放或暂停。窗口标题不会参与匹配，避免误切到浏览器、文档或聊天窗口。

## 注意事项

- 首版主要面向抖音 Windows 客户端，不保证浏览器版抖音可用。
- 暂停/播放依赖窗口中心点击，如果抖音客户端界面布局变化，可能需要手动测试。
- 如果切回 Codex 时没有回到正确窗口，请检查 Codex 进程名设置是否与真实进程名一致。
- 本仓库当前未声明开源许可证；公开代码不等于自动授予再分发或商用许可。

## 撰稿人

详见 [CONTRIBUTORS.md](CONTRIBUTORS.md)。
