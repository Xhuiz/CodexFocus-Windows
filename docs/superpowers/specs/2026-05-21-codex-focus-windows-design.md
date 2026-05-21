# Codex Focus Windows 版设计

## 背景

参考项目 `GeniusMarker/CodexFocus` 是一个 macOS 菜单栏应用。它监听本地 Codex session transcript，在 Codex 开始工作时切到抖音并继续播放，在 Codex 完成、被中断或等待权限确认时暂停抖音并切回 Codex 或 VSCode。

Windows 版按相同行为重新实现，不直接复制参考项目代码。目标是构建一个面向 Windows 的桌面窗口应用，优先支持抖音 Windows 客户端，并只监控 Codex 桌面端。

参考项目许可证为非商业 Source-Available。Windows 版实现应保留合理归因，并避免直接搬运源代码。

## 产品范围

本版本只做 Windows 桌面应用：

- 技术栈：.NET 8 + WPF。
- 监控来源：Codex Desktop。
- 媒体目标：抖音 Windows 客户端。
- 控制方式：激活抖音窗口后点击主窗口中心，模拟播放和暂停。
- 运行形态：可视化桌面窗口，提供状态、日志、设置和测试按钮。

不在首版范围内：

- 监控 VSCode Codex 插件。
- 控制浏览器里的抖音或其他视频网站。
- 使用 UI Automation 精准识别播放按钮。
- 打包安装器和签名发布流程。
- 云同步、多用户配置或远程控制。

## 用户体验

应用启动后直接进入工具界面，而不是介绍页。主窗口应该适合长期挂着观察，也适合最小化后后台运行。

主界面包含：

- 当前状态：监听中、Codex 工作中、等待权限确认、已停止、发生错误。
- 窗口检测：显示 Codex Desktop 和抖音客户端是否已找到，以及匹配到的窗口标题或进程名。
- 操作按钮：开始监听、停止监听、测试切到抖音、测试切回 Codex。
- 设置项：轮询间隔、抖音窗口标题关键词、启动后自动开始监听、开机自启。
- 事件日志：展示最近的 transcript 事件、窗口切换动作和错误信息。

如果找不到抖音窗口，应用不应崩溃；它应该提示用户先打开抖音客户端，并继续监听 Codex。找不到 Codex 窗口时同理，监听继续运行，但自动切回会显示失败状态。

## 系统架构

### WPF 应用层

`MainWindow` 负责呈现界面，`MainWindowViewModel` 暴露状态、日志、窗口检测结果、设置项和按钮命令。

界面不直接读 transcript，也不直接调用 Win32 API。所有副作用通过服务接口完成，便于测试状态机和解析逻辑。

### 设置服务

`AppSettings` 保存用户配置：

- `PollIntervalSeconds`：默认 2 秒。
- `AutoStartMonitoring`：默认开启。
- `StartWithWindows`：默认关闭。
- `DouyinWindowKeywords`：默认包含 `抖音`、`Douyin`、`TikTok`。
- `CodexWindowKeywords`：默认包含 `Codex`。

设置先保存为本地 JSON 文件，例如 `%APPDATA%\CodexFocusWindows\settings.json`。开机自启通过当前用户注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 实现。

### Transcript 读取

`SessionTranscriptReader` 负责扫描和读取 `%USERPROFILE%\.codex\sessions`：

- 只读取最近 24 小时修改过的 `.jsonl` 文件。
- 每次只读取文件尾部，避免大文件造成卡顿。
- 跳过无法解析的半行或损坏 JSON。
- 通过 `session_meta.payload.originator == "Codex Desktop"` 判断来源。
- 解析这些事件：
  - `task_started`
  - `task_complete`
  - `turn_aborted`
  - `response_item` 中等待权限确认的 `function_call`
  - `response_item` 中对应的 `function_call_output`

权限确认只识别参数里包含 `sandbox_permissions` 且值为 `require_escalated` 的调用，避免把普通工具调用误判为需要用户处理。

### 状态机

`CodexActivityMonitor` 是核心状态机：

- `Idle`：没有正在跟踪的 Codex 任务。
- `Busy`：已发现 `task_started`，Codex 正在工作。
- `WaitingApproval`：当前任务中出现未完成的权限确认请求。

启动监听时，应用把当前最新任务事件作为 baseline，不对历史事件执行动作。

状态转换：

- `Idle -> Busy`：发现新的 `task_started`，记录 `turn_id`，通知 `DouyinController.ResumeAsync()`。
- `Busy -> Idle`：同一 `turn_id` 出现 `task_complete` 或 `turn_aborted`，通知 `DouyinController.PauseAsync()`，再切回 Codex。
- `Busy -> WaitingApproval`：发现未完成的权限确认请求，暂停抖音并切回 Codex。
- `WaitingApproval -> Busy`：发现对应 `function_call_output`，说明权限确认已处理，切回抖音继续播放。

状态机需要防抖：

- 同一个 transcript 行只处理一次。
- 同一个权限调用只触发一次暂停。
- 自动切回 Codex 后设置短暂抑制窗口，避免刚完成任务时重复校准造成播放状态反转。

### 窗口控制

`WindowActivator` 封装 Win32 API：

- 枚举顶层窗口。
- 根据进程名和窗口标题关键词匹配目标窗口。
- 判断窗口是否可见、是否最小化。
- 还原最小化窗口。
- 调用 `SetForegroundWindow` 激活窗口。
- 获取窗口矩形。

`DouyinController` 基于 `WindowActivator` 实现媒体流程：

- 继续播放：激活抖音，等待约 1 秒，点击抖音主窗口中心。
- 暂停播放：激活抖音，等待约 1 秒，点击抖音主窗口中心，然后切回 Codex。
- 播放状态由应用内部的 `pausedByApp` 近似追踪，避免已由应用暂停后再次点击导致反转。

点击通过 `SendInput` 发送鼠标移动、按下和抬起事件。坐标使用抖音主窗口矩形中心点。首版不尝试解析抖音 UI 内部结构。

## 错误处理

- transcript 根目录不存在：显示“未找到 Codex 会话目录”，继续等待。
- 无法解析某一行 JSON：跳过该行并记录调试日志。
- 找不到抖音窗口：不执行点击，状态栏显示错误。
- 找不到 Codex 窗口：完成或权限确认时仍暂停抖音，但显示“无法切回 Codex”。
- Win32 激活失败：记录失败原因，UI 保持可操作。
- 连续事件过快：通过动作序号取消过期的延迟点击。

日志分两类：

- UI 事件日志：最近几十条，展示给用户。
- 调试日志：写入 `%LOCALAPPDATA%\CodexFocusWindows\logs\debug.log`。

## 测试策略

单元测试覆盖纯逻辑：

- JSONL 解析。
- Codex Desktop 来源过滤。
- 最新任务事件选择。
- `task_started` 到 `task_complete` 的状态转换。
- `turn_aborted` 的状态转换。
- 权限确认请求和完成请求的配对。
- 重复事件防抖。

窗口控制通过接口隔离：

- 单元测试使用 fake `IWindowActivator` 和 fake `IDouyinController`。
- 不在单元测试中真实激活窗口或发送鼠标点击。

手工验证覆盖真实工作流：

1. 打开 Codex Desktop 和抖音 Windows 客户端。
2. 启动应用并确认两个窗口都能被检测到。
3. 发起一次 Codex 任务，确认应用切到抖音并点击继续播放。
4. 等待任务完成，确认应用暂停抖音并切回 Codex。
5. 触发一次需要权限确认的操作，确认应用暂停抖音并切回 Codex。
6. 确认权限后，确认应用重新切回抖音。

## 实现顺序

1. 创建 `.NET 8` WPF 解决方案和测试项目。
2. 实现 transcript 数据模型和解析器。
3. 实现状态机与 fake 控制器测试。
4. 实现 Win32 窗口枚举、激活和点击封装。
5. 接入 WPF ViewModel 和主界面。
6. 实现设置持久化和开机自启。
7. 执行单元测试和真实窗口手工验证。

## 验收标准

- 应用能在 Windows 上启动为 WPF 桌面窗口。
- 只监控 Codex Desktop session。
- Codex 新任务开始时能自动切到抖音客户端并点击播放。
- Codex 任务完成或中断时能自动暂停抖音并切回 Codex。
- Codex 等待权限确认时能暂停抖音并切回 Codex，确认完成后能继续播放。
- 找不到目标窗口或 transcript 目录时有清晰状态提示，应用不崩溃。
- transcript 解析和状态机有单元测试覆盖。
