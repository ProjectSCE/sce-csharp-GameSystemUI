#if CLIENT
using GameCore.Container;
using GameCore.DisplayInfo;
using GameUI.Brush;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Struct;
using System.Drawing;
using GameUI.Control;
using Microsoft.Extensions.Logging;
using GameCore.GameSystem.Data;
using GameCore.GameSystem.Enum;
using GameSystemUI.CmdResultSystemUI;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 独立的物品UI类，用于拖动时显示物品
/// </summary>
public class ItemUI : Panel
{
    private readonly Panel backgroundPanel; // 背景面板
    private readonly Panel framePanel;     // 物品边框面板
    private readonly Panel itemPanel;      // 物品图片面板
    private readonly Label stackLabel;     // 堆叠数量标签

    // 格子大小
    private int itemSize = 112;
    private int slotSize = 117;

    public ItemPickable? Item { get; private set; }

    // 控制是否允许拖动的标志
    private bool enableDrag = false;
    public bool EnableDrag
    {
        get => enableDrag;
        set
        {
            if (enableDrag == value) return;
            enableDrag = value;
            this.AllowDrag = value;
        }
    }

    public int ItemSize
    {
        get => itemSize;
        set
        {
            if (itemSize == value) return;
            itemSize = value;
            UpdateSizes();
        }
    }

    public int SlotSize
    {
        get => slotSize;
        set
        {
            if (slotSize == value) return;
            slotSize = value;
            UpdateSizes();
        }
    }

    public ItemUI()
    {
        // 创建UI组件
        backgroundPanel = new Panel();
        framePanel = new Panel();
        itemPanel = new Panel();
        stackLabel = new Label();

        // 初始化UI
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 设置自身大小
        Width = slotSize;
        Height = slotSize;
        Image = "@gameui/image/inventory/item_frame.png";
        
        // 初始化背景面板
        backgroundPanel.Width = itemSize;
        backgroundPanel.Height = itemSize;

        // 初始化物品边框面板
        framePanel.Width = slotSize;
        framePanel.Height = slotSize;

        // 初始化物品图片面板
        itemPanel.Width = itemSize;
        itemPanel.Height = itemSize;

        // 初始化堆叠数量标签
        stackLabel.Text = "";
        stackLabel.TextColor = new SolidColorBrush(System.Drawing.Color.White);
        stackLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        stackLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        stackLabel.Position = new UIPosition(-5, 2);
        stackLabel.FontSize = 23;
        stackLabel.StrokeColor = Color.Black;
        stackLabel.StrokeSize = 1;
        stackLabel.ZIndex = 100;
        
        // 设置层级关系
        _ = backgroundPanel.AddToParent(this);
        _ = itemPanel.AddToParent(backgroundPanel);
        _ = stackLabel.AddToParent(backgroundPanel);
        _ = framePanel.AddToParent(backgroundPanel);

        // 设置为拖动源，但不接受拖放（默认不允许拖动，通过EnableDrag属性控制）
        this.AllowDrag = false;
        this.AllowDrop = false;

        // 绑定拖拽开始事件、点击事件和拖放结束事件
        this.OnDrag += HandleOnDrag;
        this.OnPointerClicked += HandleOnClick;
        this.OnDrop += HandleOnDrop;
    }

    /// <summary>
    /// 更新组件尺寸
    /// </summary>
    private void UpdateSizes()
    {
        // 更新自身尺寸
        Width = slotSize;
        Height = slotSize;
        
        // 更新背景面板尺寸
        backgroundPanel.Width = itemSize;
        backgroundPanel.Height = itemSize;
        
        // 更新边框面板尺寸
        framePanel.Width = slotSize;
        framePanel.Height = slotSize;
        
        // 更新物品面板尺寸
        itemPanel.Width = itemSize;
        itemPanel.Height = itemSize;
    }

    /// <summary>
    /// 绑定物品并更新显示
    /// </summary>
    public void BindItem(ItemPickable? item)
    {
        Item = item;
        UpdateUI();
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (Item == null)
        {
            itemPanel.Image = "";
            backgroundPanel.Image = "@gameui/image/inventory/default_bg_icon.png";
            framePanel.Image = "";
            stackLabel.Visible = false;
        }
        else
        {
            var displayInfo = Item as IDisplayInfo;
            if(displayInfo is not null)
            {
                itemPanel.Image = displayInfo.Icon?.Path ?? "";
                // 更新堆叠数量显示
                stackLabel.Visible = true;
                stackLabel.Text = displayInfo.Stack?.ToString() ?? "";
            }

            backgroundPanel.Image = Item?.QualityConfiguration?.BackgroundImage ?? "@gameui/image/inventory/品质底灰.png";
            framePanel.Image = Item?.QualityConfiguration?.BorderImage ?? "@gameui/image/inventory/品质框灰.png";
        }
    }

    /// <summary>
    /// 处理拖拽开始事件
    /// </summary>
    private void HandleOnDrag(object? sender, EventArgs e)
    {
        if (Item == null) return;

        // 查找父控件InventorySlotUI
        if (this.Parent is not InventorySlotUI parentSlotUI) return;


        parentSlotUI.HandleItemDrag(Item);
    }

    /// <summary>
    /// 处理点击事件
    /// </summary>
    private void HandleOnClick(object? sender, EventArgs e)
    {
        if (Item == null) return;

        // 查找父控件InventorySlotUI
        if (this.Parent is not InventorySlotUI parentSlotUI) return;

        // 调用父控件处理点击逻辑
        parentSlotUI.HandleItemClick(Item);
    }

    /// <summary>
    /// 处理拖放结束事件
    /// </summary>
    private void HandleOnDrop(object? sender, DropEventArgs e)
    {
        InventorySlotUI.HandleItemDrop(e);
    }
    
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        // 清理事件订阅
        this.OnDrag -= HandleOnDrag;
        this.OnPointerClicked -= HandleOnClick;
        this.OnDrop -= HandleOnDrop;
        
        // 清理UI组件
        backgroundPanel?.Destroy();
        framePanel?.Destroy();
        itemPanel?.Destroy();
        stackLabel?.Destroy();
    }
}
#endif
