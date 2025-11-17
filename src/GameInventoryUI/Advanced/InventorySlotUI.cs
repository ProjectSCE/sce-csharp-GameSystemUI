#if CLIENT
using GameCore.Container;
using GameCore.OrderSystem;
using GameData;
using GameData.Interface;
using GameData.Extension;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Struct;
using GameUI.Control;
using Microsoft.Extensions.Logging;

using GameSystemUI.GameInventoryUI.Data;
using GameCore.DisplayInfo;
using GameCore.GameSystem.Data;
using static GameCore.ScopeData;
using System.Drawing;
using GameSystemUI.CmdResultSystemUI;
using GameCore.GameSystem.Enum;

namespace GameSystemUI.GameInventoryUI.Advanced;

// 基础格子UI类，包含纯UI部分
public class SlotUI : Panel
{
    // UI组件
    protected readonly Panel borderPanel;    // 格子选中边框面板
    protected ItemUI itemUI;                 // 物品显示UI

    // 是否初始化结束
    private bool isInitialized = false;    
    // 格子大小
    private int itemSize = 112;
    private int slotSize = 117;

    public int ItemSize{
        get => itemSize;
        set{
            if(itemSize == value) return;
            itemSize = value;
            UpdateSize();
        }
    }   

    public int SlotSize{
        get => slotSize;
        set{
            if(slotSize == value) return;
            slotSize = value;
            UpdateSize();
        }
    }
    
    // 选择时显示的边框图片
    private string defaultBorderImage = "@gameui/image/inventory/icon_active.png";

    
    // 构造函数，初始化物品格子UI
    public SlotUI() : this(Panel.DefaultTemplate)
    {
    }

    public SlotUI(IGameLink<GameDataControlPanel> link) : base(link)
    {
        // 创建UI组件
        borderPanel = new Panel();
        itemUI = new ItemUI();
        
        // 设置ItemUI的尺寸
        itemUI.SlotSize = SlotSize;
        itemUI.ItemSize = ItemSize;

        // 初始化UI
        InitializeUI();

        isInitialized = true;
    }

    private void InitializeUI()
    {
        // 设置自身大小
        Width = SlotSize;
        Height = SlotSize;
        Margin = new Thickness(5);
        Image = "@gameui/image/inventory/item_frame.png";
        
        // 初始化边框面板
        borderPanel.Width = SlotSize;
        borderPanel.Height = SlotSize;
        borderPanel.Image = defaultBorderImage;
        borderPanel.Visible = false;
        
        // 设置层级关系
        _ = borderPanel.AddToParent(this);
        _ = itemUI.AddToParent(this);
    }

    private void UpdateSize()
    {
        if (!isInitialized) return;
        this.Width = SlotSize;
        this.Height = SlotSize;
        borderPanel.Width = SlotSize;
        borderPanel.Height = SlotSize;
        
        // 更新ItemUI的尺寸
        if (itemUI != null)
        {
            itemUI.SlotSize = SlotSize;
            itemUI.ItemSize = ItemSize;
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        itemUI?.Destroy();
        borderPanel?.Destroy();
    }

    // 更新UI显示
    public virtual void UpdateUI(ItemPickable? item)
    {
        itemUI.BindItem(item);
    }
}

// 物品格子UI类，继承自基础格子UI
public class InventorySlotUI : SlotUI, IGameObject<GameDataControlInventorySlotUI>, IGameObject
{
    public static bool IsDragging => currentDraggingItemUI != null;
    // 丢弃确认
    private static readonly Dialog dialog = new(){
        ZIndex = StandardUIType.ConfirmDialog.ExpectedZIndex ?? 999,
    }; //理论上整个背包UI公用一个实例

    // 物品详情
    private static readonly ItemInfo itemInfo = new();
    private bool isInitialized = false;
    private InventorySlot? slot;
    // 暂时只有QuickBar用了
    private bool isLocked = false;
    
    public bool IsLocked{
        get => isLocked;
        set {
            if (isLocked == value) return;
            isLocked = value;
            UpdateUI();
        }
    }
    // 绑定的物品格子
    public InventorySlot? Slot
    {
        get => slot;
        set
        {
            if (slot == value) return;
            slot = value;
            UpdateUI();
        }
    }

    private bool enableDrop = true;
    public bool EnableDrop{
        get => enableDrop;
        set{
            if (enableDrop == value) return;
            enableDrop = value;
            this.AllowDrop = value;
            // maskPanel可见性设置改为在拖动逻辑中控制
        }
    }

    public new static readonly IGameLink<GameDataControlInventorySlotUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlInventorySlotUI>(typeof(InventorySlotUI).GetHashCode(deterministic: true));

    IGameLink IGameObject.Link => Link;

    IGameData IGameObject.Cache => Cache;

    public new IGameLink<GameDataControlInventorySlotUI> Link => (IGameLink<GameDataControlInventorySlotUI>)base.Link;

    public new GameDataControlInventorySlotUI Cache => (GameDataControlInventorySlotUI)base.Cache;

    private readonly Panel maskPanel;
    internal static event Action<ItemPickable?>? OnCanDrop;
    private static event Action? OnCanDropEnd;
    
    // 当前正在拖动的ItemUI引用
    internal static ItemUI? currentDraggingItemUI;

    static InventorySlotUI()
    {
        dialog.AddToVisualTree();
        dialog.Visible = false;
        itemInfo.AddToVisualTree();
        itemInfo.Visible = false;
    }

    public InventorySlotUI(IGameLink<GameDataControlInventorySlotUI> link) : base(link)
    {        
        this.OnPointerEntered += SlotOnPointerEntered;
        this.OnPointerExited += SlotOnPointerExited;
        this.AllowDrop = true;
        
        maskPanel = new Panel
        {
            WidthStretchRatio = 1,
            HeightStretchRatio = 1,
            Background = new SolidColorBrush(System.Drawing.Color.Black),
            Opacity = 0.6f,
        };
        _ = maskPanel.AddToParent(this);
        maskPanel.Visible = false;
                
        // ItemUI内部会处理事件
        OnCanDrop += SlotOnCanDrop;
        OnCanDropEnd += SlotOnCanDropEnd;
        isInitialized = true;
    }

    public InventorySlotUI() : this(DefaultTemplate)
    {
    }

    /// <summary>
    /// 处理物品点击逻辑
    /// </summary>
    internal void HandleItemClick(ItemPickable item)
    {
        if (isLocked || IsDragging) return;
        // 如果拖拽中，则不打开物品详情,所以暂且不用担心没有slotui的情况
        itemInfo.Open(this);
    }


    private void SlotOnCanDrop(ItemPickable? item)
    {
        if(item is not null && slot is not null && !isLocked && slot.CanAssign(item) && slot != item.Slot)
        {
            EnableDrop = true;
            // 拖动时且自身enableDrop为true时显示maskPanel
            maskPanel.Visible = false;
        }
        else
        {
            EnableDrop = false;
            // 不满足拖放条件时显示maskPanel
            maskPanel.Visible = true;
            if(slot is not null && slot.Item is not null && slot.Item == item)
            {
                itemUI.Visible = false;
            }
        }
    }

    private void SlotOnCanDropEnd()
    {
        this.UpdateUI();
        maskPanel.Visible = false;
    }

    /// <summary>
    /// 触发OnCanDrop事件
    /// </summary>
    internal void HandleItemDrag(ItemPickable? item)
    {
        SwapItemUIForDragging();
        OnCanDrop?.Invoke(item);
    }

    /// <summary>
    /// 处理物品拖放逻辑
    /// </summary>
    internal static void HandleItemDrop(DropEventArgs e)
    {
        if (currentDraggingItemUI?.Item == null) return;
        
        var target = e.TargetControl;
        if (target is InventorySlotUI targetUI && targetUI.EnableDrop)
        {
            Swap(currentDraggingItemUI.Item,targetUI);
            targetUI.borderPanel.Visible = false;
        }
        else if (target is InventoryDropButton inventoryDropButton)
        {
            SendDropRequest(currentDraggingItemUI.Item);
        }

        // 销毁正在拖动的ItemUI
        if (currentDraggingItemUI != null)
        {
            try
            {
                currentDraggingItemUI.RemoveFromParent();
                currentDraggingItemUI.Destroy();
            }
            catch (Exception ex)
            {
                Game.Logger.LogWarning("Failed to destroy dragging ItemUI: {ex}", ex);
            }
            finally
            {
                currentDraggingItemUI = null;
            }
        }

        // 触发OnCanDropEnd事件
        OnCanDropEnd?.Invoke();
    }

    /// <summary>
    /// 物品交换逻辑
    /// </summary>
    private static void Swap(ItemPickable? item,InventorySlotUI? target)
    {
        if (target == null || item?.Slot == null) return;
        Command commandSwap = new()
        {
            Index = CommandIndexInventory.Swap,
            Item = item,
            Target = target.Slot,
            Type = ComponentTagEx.InventoryManager,
            Flag = CommandFlag.Queued
        };
        var result = commandSwap.IssueOrder(item.Slot.Inventory.Carrier);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }

    private void SlotOnPointerEntered(object? sender, EventArgs e)
    {
        if (IsDragging && !isLocked)
        {
            borderPanel.Visible = true;
        }
    }


    private void SlotOnPointerExited(object? sender, EventArgs e)  
    {
        borderPanel.Visible = false;
    }

    // 更新UI显示
    public virtual void UpdateUI()
    {
        if (!isInitialized || itemUI == null) return;
        
        if (slot == null)
        {
            // slot为空时的状态更新
            this.Visible = false;
            itemUI.EnableDrag = false;
            EnableDrop = false;
            itemUI.BindItem(null);
            itemUI.Visible = true; // 空格子时正常显示
        }
        else
        {
            // slot不为空时的状态更新
            this.Visible = true;
            
            // 更新拖拽功能状态
            itemUI.EnableDrag = !isLocked && slot.Item is not null;
            
            // 更新拖放接受状态 - 锁定的格子不接受拖放
            EnableDrop = !isLocked;
            
            // 更新ItemUI显示
            itemUI.BindItem(slot.Item);
            
            // 判断ItemUI可见性：如果当前拖动的物品和当前格子的物品一致，则隐藏；否则显示
            if (currentDraggingItemUI?.Item != null && currentDraggingItemUI.Item == slot.Item)
            {
                itemUI.Visible = false; // 正在拖动同一个物品时隐藏占位符
            }
            else
            {
                itemUI.Visible = true; // 其他情况正常显示
            }
            if(currentDraggingItemUI?.Item != null)
            {
                this.SlotOnCanDrop(currentDraggingItemUI.Item);
            }
        }
    }

    public static void SendDropRequest(ItemPickable item)
    {
        dialog.Open(item);
    }

    /// <summary>
    /// 将当前ItemUI移动到顶层拖动，在原位置创建新的ItemUI
    /// </summary>
    internal void SwapItemUIForDragging()
    {
        if (itemUI == null || slot?.Item == null) return;
        
        // 保存拖动的ItemUI引用到静态变量
        currentDraggingItemUI = itemUI;
        

        // 将当前ItemUI移动到顶层进行拖动
        currentDraggingItemUI.RemoveFromParent();
        currentDraggingItemUI.AddToVisualTree();
        currentDraggingItemUI.ZIndex = StandardUIType.Inventory.ExpectedZIndex ?? 9999; // 确保在最上层
        
        // 在原位置创建新的ItemUI（占位符，默认隐藏）
        var newItemUI = new ItemUI();
        // 设置新ItemUI的尺寸
        newItemUI.SlotSize = SlotSize;
        newItemUI.ItemSize = ItemSize;
        newItemUI.BindItem(slot.Item);
        newItemUI.Visible = false; // 默认隐藏，在UpdateUI中根据逻辑控制显示
        newItemUI.AddToParent(this);
        
        // 替换引用
        itemUI = newItemUI;
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        // 清理ItemUI事件
        if (itemUI != null)
        {
            itemUI.Destroy();
        }
        
        OnCanDrop -= SlotOnCanDrop;
        OnCanDropEnd -= SlotOnCanDropEnd;
        OnPointerExited -= SlotOnPointerExited;
        OnPointerEntered -= SlotOnPointerEntered;
    }
}
#endif
