#if CLIENT
using GameCore.BaseType;
using GameCore.Timers;
using GameUI.Control;
using System;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 拖拽悬停延迟行为组件
/// 为UI控件提供拖拽时的延迟触发功能，防止误触
/// </summary>
public class DragHoverDelayBehavior : DisposableObject
{
    private readonly Control hostControl;
    private readonly GameCore.Timers.Timer delayTimer;
    private bool isDelayCompleted; // 标记延迟是否已完成
    
    /// <summary>
    /// 悬停延迟时间（毫秒）
    /// </summary>
    public int DelayMs { get; set; } = 150;
    
    /// <summary>
    /// 是否启用延迟功能
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 延迟时间到达时的回调函数（可动态更改）
    /// </summary>
    public Action? OnDelayElapsed { get; set; }
    
    /// <summary>
    /// 鼠标进入时的回调函数（可选）
    /// </summary>
    public Action? OnEnter { get; set; }
    
    /// <summary>
    /// 鼠标离开时的回调函数（可选）
    /// 参数：isCompleted - true表示延迟完成后离开，false表示中途离开
    /// </summary>
    public Action<bool>? OnExit { get; set; }
    
    /// <summary>
    /// 创建拖拽悬停延迟行为组件
    /// </summary>
    /// <param name="hostControl">宿主控件</param>
    public DragHoverDelayBehavior(Control hostControl)
    {
        this.hostControl = hostControl ?? throw new ArgumentNullException(nameof(hostControl));
        
        // 初始化定时器
        delayTimer = new GameCore.Timers.Timer(DelayMs) { Enabled = false };
        delayTimer.Elapsed += OnDelayTimerElapsed;
    }
    
    /// <summary>
    /// 创建拖拽悬停延迟行为组件（带初始回调）
    /// </summary>
    /// <param name="hostControl">宿主控件</param>
    /// <param name="onDelayElapsed">延迟时间到达时的回调函数</param>
    public DragHoverDelayBehavior(Control hostControl, Action? onDelayElapsed) : this(hostControl)
    {
        OnDelayElapsed = onDelayElapsed;
    }
    
    /// <summary>
    /// 处理鼠标进入事件
    /// 在宿主控件的 OnPointerEntered 事件中调用此方法
    /// </summary>
    public void OnPointerEntered()
    {
        if (!Enabled || !InventorySlotUI.IsDragging)
            return;
        
        // 重置完成标记
        isDelayCompleted = false;
            
        // 停止之前的延迟并重新开始
        delayTimer.Stop();
        delayTimer.Interval = DelayMs;
        delayTimer.Start();
        
        // 触发进入回调
        OnEnter?.Invoke();
    }
    
    /// <summary>
    /// 处理鼠标离开事件
    /// 在宿主控件的 OnPointerExited 事件中调用此方法
    /// </summary>
    public void OnPointerExited()
    {
        if (!Enabled)
            return;
        
        // 记录是否是完成后离开
        bool wasCompleted = isDelayCompleted;
            
        // 停止延迟定时器
        delayTimer.Stop();
        
        // 重置完成标记
        isDelayCompleted = false;
        
        // 触发离开回调，传入是否完成的标记
        OnExit?.Invoke(wasCompleted);
    }
    
    /// <summary>
    /// 定时器触发事件处理
    /// </summary>
    private void OnDelayTimerElapsed(object? sender, EventArgs e)
    {
        // 再次检查是否仍在拖拽状态
        if (InventorySlotUI.IsDragging)
        {
            // 标记延迟已完成
            isDelayCompleted = true;
            
            // 执行回调函数
            OnDelayElapsed?.Invoke();
            
        }
        
        // 停止定时器
        delayTimer.Stop();
    }
    
    /// <summary>
    /// 手动停止当前的延迟计时
    /// </summary>
    public void Stop()
    {
        delayTimer.Stop();
        isDelayCompleted = false;
    }
    
    /// <summary>
    /// 手动重启延迟计时
    /// </summary>
    public void Restart()
    {
        if (!Enabled || !InventorySlotUI.IsDragging)
            return;
        
        isDelayCompleted = false;
        delayTimer.Stop();
        delayTimer.Interval = DelayMs;
        delayTimer.Start();
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        delayTimer?.Dispose();
    }
}
#endif
