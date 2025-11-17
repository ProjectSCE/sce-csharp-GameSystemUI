#if CLIENT
using GameCore.BaseType;
using GameUI.Control.Primitive;
using System;

namespace GameSystemUI.ConversationSystemUI.Advanced;

/// <summary>
/// 打字机效果行为组件
/// 为Label控件提供文字逐个显示的效果
/// </summary>
public class TypewriterEffectBehavior : DisposableObject, IThinker
{
    private readonly Label targetLabel;
    private string fullText = string.Empty;
    private int currentCharIndex = 0;
    private float charAccumulator = 0f; // 累积的字符数（支持小数）
    
    /// <summary>
    /// 打字速度：每帧显示的字符数（可以是小数，如0.5表示每2帧显示1个字符）
    /// </summary>
    public float CharsPerFrame { get; set; } = 0.5f;
    
    /// <summary>
    /// 是否启用打字机效果
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 打字完成时的回调函数
    /// </summary>
    public Action? OnCompleted { get; set; }
    
    /// <summary>
    /// 打字开始时的回调函数
    /// </summary>
    public Action? OnStarted { get; set; }
    
    /// <summary>
    /// 是否正在播放打字效果
    /// </summary>
    public bool IsPlaying { get; private set; }
    
    /// <summary>
    /// 是否已完成打字
    /// </summary>
    public bool IsCompleted => currentCharIndex >= fullText.Length;
    
    /// <summary>
    /// 创建打字机效果行为组件
    /// </summary>
    /// <param name="targetLabel">目标Label控件</param>
    public TypewriterEffectBehavior(Label targetLabel)
    {
        this.targetLabel = targetLabel ?? throw new ArgumentNullException(nameof(targetLabel));
        ((IThinker)this).DoesThink = false;
    }
    
    /// <summary>
    /// 创建打字机效果行为组件（带完成回调）
    /// </summary>
    /// <param name="targetLabel">目标Label控件</param>
    /// <param name="onCompleted">完成回调函数</param>
    public TypewriterEffectBehavior(Label targetLabel, Action? onCompleted) : this(targetLabel)
    {
        OnCompleted = onCompleted;
    }
    
    /// <summary>
    /// 开始播放打字机效果
    /// </summary>
    /// <param name="text">要显示的完整文本</param>
    public void Play(string text)
    {
        fullText = text ?? string.Empty;
        
        // 如果未启用或速度小于等于0，直接显示完整文本
        if (!Enabled || CharsPerFrame <= 0)
        {
            targetLabel.Text = fullText;
            IsPlaying = false;
            OnStarted?.Invoke();
            OnCompleted?.Invoke();
            return;
        }
        
        currentCharIndex = 0;
        charAccumulator = 0f;
        IsPlaying = true;
        
        // 初始显示为空
        targetLabel.Text = string.Empty;
        
        // 启动Think更新
        ((IThinker)this).DoesThink = true;
        
        // 触发开始回调
        OnStarted?.Invoke();
    }
    
    /// <summary>
    /// 立即完成打字效果，显示完整文本
    /// </summary>
    public void Complete()
    {
        if (!IsPlaying)
            return;
        
        currentCharIndex = fullText.Length;
        targetLabel.Text = fullText;
        IsPlaying = false;
        
        // 停止Think更新
        ((IThinker)this).DoesThink = false;
        
        // 触发完成回调
        OnCompleted?.Invoke();
    }
    
    /// <summary>
    /// 停止打字效果（不触发完成回调）
    /// </summary>
    public void Stop()
    {
        IsPlaying = false;
        ((IThinker)this).DoesThink = false;
    }
    
    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        Stop();
        fullText = string.Empty;
        currentCharIndex = 0;
        charAccumulator = 0f;
        targetLabel.Text = string.Empty;
    }
    
    /// <summary>
    /// 每帧更新打字效果
    /// </summary>
    public void Think(int delta)
    {
        if (!IsPlaying || !Enabled)
        {
            ((IThinker)this).DoesThink = false;
            return;
        }
        
        // 检查是否已经完成
        if (currentCharIndex >= fullText.Length)
        {
            Complete();
            return;
        }
        
        // 累加字符数
        charAccumulator += CharsPerFrame;
        
        // 计算应该显示到第几个字符
        int targetCharIndex = Math.Min((int)charAccumulator, fullText.Length);
        
        // 如果有新的字符需要显示
        if (targetCharIndex > currentCharIndex)
        {
            currentCharIndex = targetCharIndex;
            
            // 更新显示的文本
            targetLabel.Text = fullText.Substring(0, currentCharIndex);
        }
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        Stop();
    }
}
#endif

