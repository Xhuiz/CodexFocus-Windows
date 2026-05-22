using CodexFocus.Core.Monitoring;
using CodexFocus.Core.Settings;

namespace CodexFocus.Windows.Windows;

public sealed class DouyinController : ICodexFocusActions
{
    private readonly WindowActivator windowActivator;
    private readonly AppSettings settings;
    private readonly Action<string> log;
    private bool pausedByApp = true;
    private int actionId;

    public DouyinController(WindowActivator windowActivator, AppSettings settings, Action<string>? log = null)
    {
        this.windowActivator = windowActivator;
        this.settings = settings;
        this.log = log ?? (_ => { });
    }

    public async Task ResumeDouyinAsync(CancellationToken cancellationToken)
    {
        var currentActionId = Interlocked.Increment(ref actionId);
        var douyin = windowActivator.FindFirst(settings.DouyinWindowKeywords);
        if (douyin is null)
        {
            log("未找到抖音窗口");
            return;
        }

        log($"激活抖音：{douyin.Description}");
        windowActivator.Activate(douyin);

        if (!pausedByApp)
        {
            log("抖音不是由本应用暂停，跳过继续播放点击");
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(settings.ActivationDelayMilliseconds), cancellationToken).ConfigureAwait(false);
        if (currentActionId != actionId)
        {
            return;
        }

        if (windowActivator.ClickCenter(douyin))
        {
            pausedByApp = false;
            log("已点击抖音窗口中心继续播放");
        }
        else
        {
            log("点击抖音窗口中心失败");
        }
    }

    public async Task PauseDouyinAndReturnToCodexAsync(CancellationToken cancellationToken)
    {
        var currentActionId = Interlocked.Increment(ref actionId);
        var douyin = windowActivator.FindFirst(settings.DouyinWindowKeywords);
        if (douyin is null)
        {
            log("未找到抖音窗口，跳过暂停");
            ActivateCodex();
            return;
        }

        log($"激活抖音准备暂停：{douyin.Description}");
        windowActivator.Activate(douyin);

        if (!pausedByApp)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(settings.ActivationDelayMilliseconds), cancellationToken).ConfigureAwait(false);
            if (currentActionId == actionId && windowActivator.ClickCenter(douyin))
            {
                pausedByApp = true;
                log("已点击抖音窗口中心暂停播放");
            }
        }
        else
        {
            log("抖音已由本应用暂停，跳过重复点击");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(settings.ReturnDelayMilliseconds), cancellationToken).ConfigureAwait(false);
        if (currentActionId == actionId)
        {
            ActivateCodex();
        }
    }

    public bool ActivateDouyinForTest()
    {
        var douyin = windowActivator.FindFirst(settings.DouyinWindowKeywords);
        if (douyin is null)
        {
            log("测试失败：未找到抖音窗口");
            return false;
        }

        var activated = windowActivator.Activate(douyin);
        log(activated ? $"已切到抖音：{douyin.Description}" : "切到抖音失败");
        return activated;
    }

    public bool ActivateCodexForTest()
    {
        return ActivateCodex();
    }

    private bool ActivateCodex()
    {
        var codex = windowActivator.FindFirst(settings.CodexWindowKeywords);
        if (codex is null)
        {
            log("未找到 Codex 窗口");
            return false;
        }

        var activated = windowActivator.Activate(codex);
        log(activated ? $"已切回 Codex：{codex.Description}" : "切回 Codex 失败");
        return activated;
    }
}
