#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Control.Data;
using GameUI.ConversationSystem.Proto;
using GameUI.ConversationSystem.Data;
using GameUI.ConversationSystem.Event;
using GameUI.Enum;
using GameUI.Struct;
using GameUI.ConversationSystem;
using GameCore.Timers;
using System.Drawing;

namespace GameSystemUI.ConversationSystemUI.Advanced;

/// <summary>
/// 对话面板基类，用于显示对话台词和选择
/// </summary>
/// <remarks>
/// <para>派生类需要实现以下抽象方法：</para>
/// <list type="bullet">
/// <item><description>ShowLine: 显示对话台词</description></item>
/// <item><description>ShowChoices: 显示对话选择</description></item>
/// <item><description>Clear: 清空对话内容</description></item>
/// <item><description>Hide: 隐藏对话面板</description></item>
/// <item><description>HideLine: 隐藏台词</description></item>
/// <item><description>HideChoices: 隐藏选择</description></item>
/// </list>
/// <para>派生类可以使用以下可选功能：</para>
/// <list type="bullet">
/// <item><description>TypewriterEffectBehavior: 为台词添加打字机效果（文字逐个显示）</description></item>
/// </list>
/// <para>派生类应在适当时机（通常是交互的回调中）调用以下受保护方法通知对话系统：</para>
/// <list type="bullet">
/// <item><description>CompleteLineDisplay(): 台词显示完成</description></item>
/// <item><description>CompleteChoiceSelection(int): 用户选择完成</description></item>
/// </list>
/// </remarks>
public abstract class ConversationPanelBase : Panel, IConversationUI
{
    // 在台词确认后调用，用于等待台词显示完成
    private TaskCompletionSource? LineTCS { get; set; }
    // 在选择确认后调用，用于等待选择显示完成
    private TaskCompletionSource<int>? ChoicesTCS { get; set; }
    
    // 台词自动完成计时器
    private readonly GameCore.Timers.Timer _lineTimer;
    
    // 当前是否等待确认（WaitForConfirmation）
    private bool _waitForConfirmation;
    
    // CanFinish 的私有字段
    private bool _canFinish;
    
    /// <summary>
    /// 当前台词是否可以完成（通过点击等方式）
    /// 如果 AllowSkip 为 true，则始终为 true
    /// 如果 WaitForConfirmation 为 true，则在 Duration 结束后变为 true
    /// </summary>
    public bool CanFinish 
    { 
        get => _canFinish;
        protected set
        {
            if (_canFinish != value)
            {
                _canFinish = value;
                OnCanFinishChanged(value);
            }
        }
    }

    public ConversationPanelBase()
    {
        _lineTimer = new GameCore.Timers.Timer { Enabled = false };
        _lineTimer.Elapsed += OnLineTimerElapsed;
    }

    /// <summary>
    /// 台词计时器触发事件处理 - Duration超时时的行为
    /// </summary>
    private void OnLineTimerElapsed(object? sender, EventArgs e)
    {
        _lineTimer.Stop();
        
        // 如果需要等待确认，则只设置 CanFinish 标志
        if (_waitForConfirmation)
        {
            CanFinish = true;
        }
        else
        {
            // 否则直接完成台词显示
            CompleteLineDisplay();
        }
    }

    public abstract void ShowLine(ConversationLineInfo lineInfo, GameDataConversationLine? lineData);

    public abstract void ShowChoices(List<ConversationChoiceInfo> choices, ConversationChoicePromptInfo? promptInfo, GameDataConversationChoiceGroup? choiceGroupData);

    public abstract void HideLine();
    public abstract void HideChoices();
    
    public abstract void Clear();

    public abstract void Hide();

    #region Protected Methods for Derived Classes
    
    /// <summary>
    /// 派生类在台词显示完成后调用此方法，用于通知对话系统继续
    /// </summary>
    protected virtual void CompleteLineDisplay()
    {
        LineTCS?.TrySetResult();
    }
    
    /// <summary>
    /// 派生类在用户选择完成后调用此方法，用于通知对话系统用户的选择
    /// </summary>
    /// <param name="choiceIndex">用户选择的选项索引（从choices列表中，从0开始）</param>
    protected virtual void CompleteChoiceSelection(int choiceIndex)
    {
        ChoicesTCS?.TrySetResult(choiceIndex);
    }
    
    /// <summary>
    /// 派生类可以调用此方法取消当前的台词显示
    /// </summary>
    protected virtual void CancelLineDisplay()
    {
        LineTCS?.TrySetCanceled();
    }
    
    /// <summary>
    /// 派生类可以调用此方法取消当前的选择
    /// </summary>
    protected virtual void CancelChoiceSelection()
    {
        ChoicesTCS?.TrySetCanceled();
    }
    
    /// <summary>
    /// 当 CanFinish 状态改变时调用，派生类可以重写此方法来响应变化
    /// </summary>
    /// <param name="canFinish">新的 CanFinish 状态</param>
    protected virtual void OnCanFinishChanged(bool canFinish)
    {
        // 派生类可以重写此方法来响应 CanFinish 状态变化
    }
    
    #endregion

    public async Task<int> ShowChoicesAsync(List<ConversationChoiceInfo> choices, ConversationChoicePromptInfo? promptInfo, ConversationChoiceGroup? choiceGroup, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<int>();
        ChoicesTCS = tcs;
        cancellationToken.Register(() =>
        {
            ChoicesTCS?.TrySetCanceled(cancellationToken);
        });
        try
        {
            ShowChoices(choices, promptInfo, choiceGroup?.Cache);
            int result = await tcs.Task;
            return result;
        }
        catch (OperationCanceledException ex)
        {
            // 取消时抛出异常，让调用者知道操作被取消
            Game.Logger.LogWarning(ex, "ShowChoicesAsync canceled");
            throw;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "ShowChoicesAsync error");
            return 0;
        }
        finally
        {
            // 清理 TCS 引用
            ChoicesTCS = null;
            HideLine();
            HideChoices();
            this.Visible = false;
        }
    }

    public async Task ShowLineAsync(ConversationLineInfo lineInfo, ConversationLine? line, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        LineTCS = tcs;
        cancellationToken.Register(() =>
        {
            LineTCS?.TrySetCanceled(cancellationToken);
        });

        // 保存 WaitForConfirmation 状态
        _waitForConfirmation = lineInfo.WaitForConfirmation;

        // 设置 CanFinish 标志
        CanFinish = lineInfo.AllowSkip;

        // 如果有Duration，启动计时器
        if (lineInfo.Duration.HasValue && lineInfo.Duration.Value.TotalMilliseconds > 0)
        {
            _lineTimer.Stop();
            _lineTimer.Interval = (int)lineInfo.Duration.Value.TotalMilliseconds;
            _lineTimer.Start();
        }

        try
        {
            ShowLine(lineInfo, line?.Cache);
            await tcs.Task;
        }
        catch (OperationCanceledException ex)
        {
            // 取消时清理UI状态并重新抛出异常，让调用者知道操作被取消
            this.Visible = false;
            Game.Logger.LogWarning(ex, "ShowLineAsync canceled");
            throw;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "ShowLineAsync error");
        }
        finally
        {
            // 停止计时器并清理状态
            _lineTimer.Stop();
            LineTCS = null;
            CanFinish = false;
            _waitForConfirmation = false;
            HideLine();
            this.Visible = false;
        }
    }

    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        _lineTimer?.Dispose();
        base.DisposeManaged();
    }
}
#endif