using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CodexFocus.Core.Monitoring;
using CodexFocus.Core.Settings;
using CodexFocus.Core.Transcript;
using CodexFocus.Windows.Services;
using CodexFocus.Windows.Windows;

namespace CodexFocus.Windows.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly JsonSettingsStore settingsStore = new();
    private readonly StartupRegistryService startupRegistry = new();
    private readonly DebugFileLogger logger = new();
    private readonly DispatcherTimer timer = new();
    private readonly WindowActivator windowActivator = new();
    private readonly string sessionsRoot;
    private AppSettings settings;
    private CodexActivityMonitor monitor;
    private DouyinController douyinController;
    private bool tickRunning;
    private string statusText = "初始化中";
    private string codexWindowText = "未检测";
    private string douyinWindowText = "未检测";
    private int pollIntervalSeconds;
    private bool autoStartMonitoring;
    private bool startWithWindows;
    private string douyinKeywordsText = "";
    private string codexKeywordsText = "";
    private bool isMonitoring;

    public MainWindowViewModel()
    {
        sessionsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "sessions");
        settings = settingsStore.Load();
        startWithWindows = startupRegistry.IsEnabled() || settings.StartWithWindows;
        ApplySettingsToProperties();

        douyinController = CreateDouyinController();
        monitor = CreateMonitor();
        timer.Tick += TimerOnTick;

        StartCommand = new RelayCommand(StartMonitoring, () => !IsMonitoring);
        StopCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RefreshWindowsCommand = new RelayCommand(RefreshWindowStatus);
        TestDouyinCommand = new RelayCommand(() => douyinController.ActivateDouyinForTest());
        TestCodexCommand = new RelayCommand(() => douyinController.ActivateCodexForTest());

        RefreshWindowStatus();
        StatusText = settings.AutoStartMonitoring ? "准备自动监听" : "已停止";
        if (settings.AutoStartMonitoring)
        {
            StartMonitoring();
        }
    }

    public ObservableCollection<string> Logs { get; } = [];

    public RelayCommand StartCommand { get; }

    public RelayCommand StopCommand { get; }

    public RelayCommand SaveSettingsCommand { get; }

    public RelayCommand RefreshWindowsCommand { get; }

    public RelayCommand TestDouyinCommand { get; }

    public RelayCommand TestCodexCommand { get; }

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    public string CodexWindowText
    {
        get => codexWindowText;
        private set => SetProperty(ref codexWindowText, value);
    }

    public string DouyinWindowText
    {
        get => douyinWindowText;
        private set => SetProperty(ref douyinWindowText, value);
    }

    public int PollIntervalSeconds
    {
        get => pollIntervalSeconds;
        set => SetProperty(ref pollIntervalSeconds, value);
    }

    public bool AutoStartMonitoring
    {
        get => autoStartMonitoring;
        set => SetProperty(ref autoStartMonitoring, value);
    }

    public bool StartWithWindows
    {
        get => startWithWindows;
        set => SetProperty(ref startWithWindows, value);
    }

    public string DouyinKeywordsText
    {
        get => douyinKeywordsText;
        set => SetProperty(ref douyinKeywordsText, value);
    }

    public string CodexKeywordsText
    {
        get => codexKeywordsText;
        set => SetProperty(ref codexKeywordsText, value);
    }

    public bool IsMonitoring
    {
        get => isMonitoring;
        private set
        {
            if (SetProperty(ref isMonitoring, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public void Dispose()
    {
        timer.Stop();
    }

    private CodexActivityMonitor CreateMonitor()
    {
        var reader = new CodexTranscriptReader(sessionsRoot);
        return new CodexActivityMonitor(reader, douyinController);
    }

    private DouyinController CreateDouyinController()
    {
        return new DouyinController(windowActivator, settings, AddLog);
    }

    private void StartMonitoring()
    {
        SaveSettings();
        monitor = CreateMonitor();
        monitor.Start();
        timer.Interval = TimeSpan.FromSeconds(settings.PollIntervalSeconds);
        timer.Start();
        IsMonitoring = true;
        StatusText = monitor.StatusText;
        AddLog("开始监听 Codex Desktop");
    }

    private void StopMonitoring()
    {
        timer.Stop();
        monitor.Stop();
        IsMonitoring = false;
        StatusText = monitor.StatusText;
        AddLog("停止监听");
    }

    private async void TimerOnTick(object? sender, EventArgs e)
    {
        if (tickRunning)
        {
            return;
        }

        tickRunning = true;
        try
        {
            await monitor.TickAsync(CancellationToken.None);
            StatusText = monitor.StatusText;
            RefreshWindowStatus();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AddLog($"监听失败：{ex.Message}");
        }
        finally
        {
            tickRunning = false;
        }
    }

    private void SaveSettings()
    {
        settings.PollIntervalSeconds = PollIntervalSeconds;
        settings.AutoStartMonitoring = AutoStartMonitoring;
        settings.StartWithWindows = StartWithWindows;
        settings.DouyinWindowKeywords = SplitKeywords(DouyinKeywordsText);
        settings.CodexWindowKeywords = SplitKeywords(CodexKeywordsText);
        settings.Normalize();
        ApplySettingsToProperties();
        settingsStore.Save(settings);
        startupRegistry.SetEnabled(settings.StartWithWindows);
        douyinController = CreateDouyinController();
        AddLog("设置已保存");
    }

    private void RefreshWindowStatus()
    {
        var codex = windowActivator.FindFirst(settings.CodexWindowKeywords);
        var douyin = windowActivator.FindFirst(settings.DouyinWindowKeywords);
        CodexWindowText = codex?.Description ?? "未找到 Codex 窗口";
        DouyinWindowText = douyin?.Description ?? "未找到抖音窗口";
    }

    private void ApplySettingsToProperties()
    {
        settings.Normalize();
        PollIntervalSeconds = settings.PollIntervalSeconds;
        AutoStartMonitoring = settings.AutoStartMonitoring;
        StartWithWindows = settings.StartWithWindows || startupRegistry.IsEnabled();
        DouyinKeywordsText = string.Join(", ", settings.DouyinWindowKeywords);
        CodexKeywordsText = string.Join(", ", settings.CodexWindowKeywords);
    }

    private void AddLog(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} {message}";
        Logs.Insert(0, line);
        while (Logs.Count > 80)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }

        logger.Write(message);
    }

    private static List<string> SplitKeywords(string text)
    {
        return text.Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
