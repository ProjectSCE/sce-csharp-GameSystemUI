#if CLIENT
using GameCore;
using GameCore.BaseType;
using GameUI.Control;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Enum;
using System;
using System.Collections.Generic;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 虚拟化面板自动滚动包装器
/// 包装一个VirtualizingPanel，在边缘提供拖拽悬停触发区，实现自动滚动功能
/// </summary>
public class VirtualizingPanelAutoScrollWrapper : Panel, IThinker
{
    #region 字段

    private VirtualizingPanel innerPanel;  // 内部的虚拟化面板（会滚动的部分）
    
    private Panel? triggerStartPanel;  // 起始方向的触发区Panel（固定，不滚动）
    private Panel? triggerEndPanel;    // 结束方向的触发区Panel（固定，不滚动）
    
    private DragHoverDelayBehavior? triggerStartBehavior;
    private DragHoverDelayBehavior? triggerEndBehavior;
    
    private ScrollDirection currentScrollDirection = ScrollDirection.None;
    
    #endregion

    #region 公共属性

    /// <summary>
    /// 触发区域的宽度（垂直滚动时）或高度（水平滚动时）
    /// </summary>
    public float TriggerZoneSize { get; set; } = 30f;

    /// <summary>
    /// 悬停延迟时间（毫秒）
    /// </summary>
    public int HoverDelayMs { get; set; } = 150;

    /// <summary>
    /// 滚动速度（item/帧）
    /// 例如：0.5 表示每帧滚动 0.5 个 item（每2帧滚动1个item）
    /// </summary>
    public double ScrollSpeedItemPerFrame { get; set; } = 0.5;

    /// <summary>
    /// 是否启用自动滚动功能
    /// </summary>
    public bool AutoScrollEnabled { get; set; } = true;

    /// <summary>
    /// 获取内部的VirtualizingPanel（用于直接访问高级功能）
    /// </summary>
    public VirtualizingPanel InnerPanel => innerPanel ?? throw new InvalidOperationException("Inner panel not initialized yet");

    #endregion

    #region 转发的VirtualizingPanel属性

    /// <summary>
    /// ItemsSource（转发到内部面板）
    /// </summary>
    public override IEnumerable<object>? ItemsSource
    {
        get => innerPanel?.ItemsSource;
        set
        {
            if (innerPanel != null)
                innerPanel.ItemsSource = value;
        }
    }

    /// <summary>
    /// ScrollBarValue（转发到内部面板）
    /// </summary>
    public float ScrollBarValue
    {
        get => innerPanel?.ScrollBarValue ?? 0f;
        set
        {
            if (innerPanel != null)
                innerPanel.ScrollBarValue = value;
        }
    }

    /// <summary>
    /// ScrollOrientation（转发到内部面板）
    /// </summary>
    public Orientation ScrollOrientation
    {
        get => innerPanel?.ScrollOrientation ?? Orientation.Vertical;
        set
        {
            if (innerPanel != null)
            {
                innerPanel.ScrollOrientation = value;
                UpdateTriggerZonesLayout();  // 滚动方向改变时更新触发区布局
            }
        }
    }

    /// <summary>
    /// ScrollEnabled（转发到内部面板）
    /// </summary>
    public bool ScrollEnabled
    {
        get => innerPanel?.ScrollEnabled ?? true;
        set
        {
            if (innerPanel != null)
                innerPanel.ScrollEnabled = value;
        }
    }

    /// <summary>
    /// ItemTemplate（转发到内部面板）
    /// </summary>
    public new IGameLink<GameDataControl>? ItemTemplate
    {
        get => innerPanel?.ItemTemplate;
        set
        {
            if (innerPanel != null)
                innerPanel.ItemTemplate = value;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建包装器（使用默认的VirtualizingPanel）
    /// </summary>
    public VirtualizingPanelAutoScrollWrapper() : this(new VirtualizingPanel
    {
        WidthStretchRatio = 1.0f,
        HeightStretchRatio = 1.0f,
        ScrollEnabled = true,
        ArrangeOnScroll = true
    })
    {
    }

    /// <summary>
    /// 创建包装器（使用自定义的VirtualizingPanel）
    /// </summary>
    /// <param name="panel">要包装的VirtualizingPanel实例</param>
    public VirtualizingPanelAutoScrollWrapper(VirtualizingPanel panel)
    {
        // 立即设置 innerPanel，防止基类构造函数访问转发属性时出错
        innerPanel = panel ?? throw new ArgumentNullException(nameof(panel));
        
        // 确保内部面板填满整个包装器
        innerPanel.WidthStretchRatio = 1.0f;
        innerPanel.HeightStretchRatio = 1.0f;
        
        // 延迟添加子控件，避免在基类构造期间触发
        this.AddChild(innerPanel);
        InitializeTriggerZones();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化触发区Panel和延迟行为
    /// </summary>
    private void InitializeTriggerZones()
    {
        // 创建起始触发区Panel（固定在容器边缘，不随内容滚动）
        triggerStartPanel = new Panel
        {
            ZIndex = 1000  // 确保在最上层
        };
        this.AddChild(triggerStartPanel);

        // 创建结束触发区Panel（固定在容器边缘，不随内容滚动）
        triggerEndPanel = new Panel
        {
            ZIndex = 1000  // 确保在最上层
        };
        this.AddChild(triggerEndPanel);

        // 创建起始触发区的延迟行为
        triggerStartBehavior = new DragHoverDelayBehavior(triggerStartPanel)
        {
            DelayMs = HoverDelayMs,
            Enabled = AutoScrollEnabled,
            OnDelayElapsed = OnStartTriggerActivated,
            OnEnter = () => OnTriggerEnter(true),
            OnExit = isCompleted => OnTriggerExit(isCompleted, true)
        };

        // 创建结束触发区的延迟行为
        triggerEndBehavior = new DragHoverDelayBehavior(triggerEndPanel)
        {
            DelayMs = HoverDelayMs,
            Enabled = AutoScrollEnabled,
            OnDelayElapsed = OnEndTriggerActivated,
            OnEnter = () => OnTriggerEnter(false),
            OnExit = isCompleted => OnTriggerExit(isCompleted, false)
        };

        // 绑定触发区Panel的鼠标事件
        triggerStartPanel.OnPointerEntered += (_, _) => triggerStartBehavior.OnPointerEntered();
        triggerStartPanel.OnPointerExited += (_, _) => triggerStartBehavior.OnPointerExited();

        triggerEndPanel.OnPointerEntered += (_, _) => triggerEndBehavior.OnPointerEntered();
        triggerEndPanel.OnPointerExited += (_, _) => triggerEndBehavior.OnPointerExited();

        // 监听尺寸变化以更新触发区位置
        OnSizeChanged += (_, _) => UpdateTriggerZonesLayout();
        
        // 初始化布局
        UpdateTriggerZonesLayout();
    }

    /// <summary>
    /// 更新触发区Panel的布局和位置
    /// </summary>
    private void UpdateTriggerZonesLayout()
    {
        if (triggerStartPanel == null || triggerEndPanel == null)
            return;

        if (ScrollOrientation == Orientation.Vertical)
        {
            // 垂直滚动：触发区在顶部和底部
            triggerStartPanel.WidthStretchRatio = 1.0f;
            triggerStartPanel.Height = TriggerZoneSize;
            triggerStartPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
            triggerStartPanel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;

            triggerEndPanel.WidthStretchRatio = 1.0f;
            triggerEndPanel.Height = TriggerZoneSize;
            triggerEndPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
            triggerEndPanel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        }
        else
        {
            // 水平滚动：触发区在左侧和右侧
            triggerStartPanel.Width = TriggerZoneSize;
            triggerStartPanel.HeightStretchRatio = 1.0f;
            triggerStartPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
            triggerStartPanel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;

            triggerEndPanel.Width = TriggerZoneSize;
            triggerEndPanel.HeightStretchRatio = 1.0f;
            triggerEndPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
            triggerEndPanel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        }
    }

    #endregion

    #region 触发回调

    /// <summary>
    /// 触发区进入回调
    /// </summary>
    /// <param name="isStart">是否是起始触发区</param>
    private void OnTriggerEnter(bool isStart)
    {  
    }

    /// <summary>
    /// 起始触发区激活（开始向起始方向滚动）
    /// </summary>
    private void OnStartTriggerActivated()
    {
        if (!AutoScrollEnabled || !InventorySlotUI.IsDragging)
            return;

        currentScrollDirection = ScrollDirection.ToStart;
        ((IThinker)this).DoesThink = true;
    }

    /// <summary>
    /// 结束触发区激活（开始向结束方向滚动）
    /// </summary>
    private void OnEndTriggerActivated()
    {
        if (!AutoScrollEnabled || !InventorySlotUI.IsDragging)
            return;

        currentScrollDirection = ScrollDirection.ToEnd;
        ((IThinker)this).DoesThink = true;
    }

    /// <summary>
    /// 触发区离开回调
    /// </summary>
    /// <param name="isCompleted">是否是延迟完成后离开</param>
    /// <param name="isStart">是否是起始触发区</param>
    private void OnTriggerExit(bool isCompleted, bool isStart)
    {
        
        // 离开触发区时停止对应方向的滚动
        if (isStart && currentScrollDirection == ScrollDirection.ToStart)
        {
            currentScrollDirection = ScrollDirection.None;
            ((IThinker)this).DoesThink = false;
        }
        else if (!isStart && currentScrollDirection == ScrollDirection.ToEnd)
        {
            currentScrollDirection = ScrollDirection.None;
            ((IThinker)this).DoesThink = false;
        }
    }

    #endregion

    #region IThinker 实现

    /// <summary>
    /// 持续滚动更新
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public void Think(int delta)
    {
        // 如果不再拖拽，停止滚动
        if (!InventorySlotUI.IsDragging)
        {
            currentScrollDirection = ScrollDirection.None;
            ((IThinker)this).DoesThink = false;
            return;
        }

        // 如果没有滚动方向，停止
        if (currentScrollDirection == ScrollDirection.None)
        {
            ((IThinker)this).DoesThink = false;
            return;
        }

        // 计算滚动增量
        float scrollDelta = CalculateScrollDelta();

        if (currentScrollDirection == ScrollDirection.ToStart)
        {
            // 向起始方向滚动（减小 ScrollBarValue）
            ScrollBarValue = Math.Max(0f, ScrollBarValue - scrollDelta);
        }
        else if (currentScrollDirection == ScrollDirection.ToEnd)
        {
            // 向结束方向滚动（增大 ScrollBarValue）
            ScrollBarValue = Math.Min(1f, ScrollBarValue + scrollDelta);
        }

        // 如果已经到达边界，停止滚动
        if (ScrollBarValue <= 0f || ScrollBarValue >= 1f)
        {
            currentScrollDirection = ScrollDirection.None;
            ((IThinker)this).DoesThink = false;
        }
    }

    /// <summary>
    /// 计算滚动增量（基于 item）
    /// </summary>
    /// <returns>ScrollBarValue 的变化量</returns>
    private float CalculateScrollDelta()
    {
        try
        {
            // 获取 ItemsSource 的总数
            int totalItemCount = GetTotalItemCount();
            
            if (totalItemCount <= 0)
            {
                // 如果没有 item，停止滚动
                return 0f;
            }

            // 计算每个 item 在 ScrollBarValue (0-1) 中占的比例
            double itemRatio = 1.0 / totalItemCount;

            // 计算本帧应该滚动的 item 数量（Think 每帧调用一次）
            double itemsToScroll = ScrollSpeedItemPerFrame;

            // 转换为 ScrollBarValue 的变化量
            float scrollDelta = (float)(itemsToScroll * itemRatio);

            return scrollDelta;
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning($"VirtualizingPanelAutoScrollWrapper: Error calculating scroll delta: {ex.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// 获取 ItemsSource 的总 item 数量
    /// </summary>
    /// <returns>item 总数，如果无法获取则返回 0</returns>
    private int GetTotalItemCount()
    {
        if (ItemsSource == null)
            return 0;

        // 尝试转换为 ICollection 以获取 Count
        if (ItemsSource is System.Collections.ICollection collection)
        {
            return collection.Count;
        }

        // 如果不是 ICollection，尝试枚举计数（性能较差）
        int count = 0;
        foreach (var _ in ItemsSource)
        {
            count++;
        }

        return count;
    }

    #endregion

    #region 清理

    protected override void DisposeManaged()
    {
        // 停止思考
        ((IThinker)this).DoesThink = false;

        // 清理行为
        triggerStartBehavior?.Destroy();
        triggerEndBehavior?.Destroy();

        // 清理触发区Panel（会自动从Children中移除）
        triggerStartPanel?.Destroy();
        triggerEndPanel?.Destroy();

        // 清理内部面板
        innerPanel?.Destroy();

        base.DisposeManaged();
    }

    #endregion

    #region 内部枚举

    /// <summary>
    /// 滚动方向枚举
    /// </summary>
    private enum ScrollDirection
    {
        /// <summary>无滚动</summary>
        None,
        /// <summary>向起始位置滚动（上/左）</summary>
        ToStart,
        /// <summary>向结束位置滚动（下/右）</summary>
        ToEnd
    }

    #endregion
}
#endif

