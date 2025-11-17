#if CLIENT
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameUI.Control.Primitive;
using GameCore.Container;
using GameCore.SceneSystem;
using GameCore.Components;
using Microsoft.Extensions.Logging;
using GameCore.OrderSystem;
using static GameCore.ScopeData;
using GameCore.Timers;
using GameUI.Brush;
using GameUI.Enum;
using GameUI.Struct;
using System.Drawing;
using GameData.Extension;
using GameCore.Struct;
using GameData;
using GameData.Interface;
using GameCore.DisplayInfo;
using GameUI.Control.Data;

using GameUI.Control.Struct;

using GameSystemUI.GameInventoryUI.Data;
using GameCore.GameSystem.Enum;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 拾取列表物品项UI，包含左侧SlotUI和右侧文本信息
/// </summary>
public class PickListItem : Panel, IGameObject<GameDataControlPickListItem>, IGameObject
{
    public new static readonly IGameLink<GameDataControlPickListItem> DefaultTemplate = ScopeData.Control.PickListItem;
    
    IGameLink IGameObject.Link => Link;
    IGameData IGameObject.Cache => Cache;
    public new IGameLink<GameDataControlPickListItem> Link => (IGameLink<GameDataControlPickListItem>)base.Link;
    public new GameDataControlPickListItem Cache => (GameDataControlPickListItem)base.Cache;

    private readonly SlotUI slotUI;
    private readonly Label nameLabel;
    private readonly Label categoryLabel;
    private readonly Panel itemButton;

    private Item? bindItem;
    public Action<Item>? OnItemClicked { get; set; }

    public Item? BindItem
    {
        get => bindItem;
        set
        {
            if (bindItem == value) return;
            bindItem = value;
            UpdateUI();
        }
    }

    public PickListItem() : this(DefaultTemplate)
    {
    }

    public PickListItem(IGameLink<GameDataControlPickListItem> link) : base(link)
    {
        // 设置整体布局
        this.Width = 264;
        this.Height = 76;
        // this.Margin = new Thickness(5, 2, 5, 2);
        // this.FlowOrientation = Orientation.Horizontal;
        this.OnPointerClicked += OnSlotClicked;

        // 创建背景按钮
        itemButton = new Panel
        {
            Width = 264,
            Height = 72,
            Image = "@gameui/image/inventory/bg_item.png",
            SlicedEdges = new Thickness(5, 5, 5, 5),
        };
        this.AddChild(itemButton);

        // 左侧SlotUI
        slotUI = new SlotUI
        {
            SlotSize = 64,
            ItemSize = 62,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 0, 0)
        };
        this.AddChild(slotUI);

        // 右侧文本区域
        var textContainer = new Panel
        {
            Width = 180,
            Height = 76,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Margin = new Thickness(85, top: 10, 0, 10),
            FlowOrientation = Orientation.Vertical,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Center
        };
        this.AddChild(textContainer);

        // 物品名称
        nameLabel = new Label
        {
            Width = 180,
            Height = 38,
            Text = "",
            FontSize = 32,
            TextColor = new SolidColorBrush(Color.FromArgb(255, 209, 204, 198)),
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
        };
        textContainer.AddChild(nameLabel);

        // 分类标签
        categoryLabel = new Label
        {
            Width = 180,
            Height = 32,
            Text = "",
            FontSize = 28,
            TextColor = new SolidColorBrush(Color.FromArgb(255, 146, 150, 155)),
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
        };
        textContainer.AddChild(categoryLabel);
    }

    private void OnSlotClicked(object? sender, EventArgs e)
    {
        if (bindItem != null && !this.IsDisposed)
        {
            OnItemClicked?.Invoke(bindItem);
        }
    }

    private void UpdateUI()
    {
        if (bindItem == null)
        {
            slotUI.UpdateUI(null);
            nameLabel.Text = "";
            categoryLabel.Text = "";
            return;
        }

        // 使用IDisplayInfo接口获取显示信息
        var displayInfo = bindItem as IDisplayInfo;
        if (displayInfo != null)
        {
            // 暂时传递null给SlotUI，因为我们无法直接创建ItemPickable
            slotUI.UpdateUI(bindItem as ItemPickable);
            
            // 设置物品名称
            nameLabel.Text = GetItemName(bindItem);

            // 设置品质颜色到名称标签
            var qualityData = bindItem.QualityConfiguration;
            if (qualityData != null && !string.IsNullOrEmpty(qualityData.Name))
            {
                nameLabel.TextColor = new SolidColorBrush(qualityData.Color);
            }
            else
            {
                nameLabel.TextColor = new SolidColorBrush(System.Drawing.Color.FromArgb(255, 209, 204, 198));
            }
            
            // 设置分类信息
            var isEquip = IsEquipment(bindItem);
            var itemType = GetItemType(bindItem);
            var categoryText = isEquip ? "装备" : "物品";
            if (!string.IsNullOrEmpty(itemType))
            {
                categoryText += ":" + itemType;
            }
            categoryLabel.Text = categoryText;
        }
        else
        {
            slotUI.UpdateUI(null);
            nameLabel.Text = "未知物品";
            categoryLabel.Text = "";
        }
    }

    private string GetItemName(Item item)
    {
        // 暂时返回一个默认名称，实际应该从物品数据中获取
        var displayInfo = item as IDisplayInfo;
        return displayInfo?.DisplayName ?? "";
    }

    private bool IsEquipment(Item item)
    {
        // 这里应该根据实际的装备判断逻辑
        // 暂时返回false
        return false;
    }

    private string GetItemType(Item item)
    {
        // 这里应该根据实际的物品类型获取逻辑
        // 暂时返回空字符串
        return "";
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        itemButton.OnPointerClicked -= OnSlotClicked;
        // SlotUI会在父控件Dispose时自动释放
    }
}

/// <summary>
/// 物品拾取列表
/// </summary>
public class PickList : Panel
{
    /// <summary>
    /// 默认模板链接
    /// </summary>
    public static new readonly IGameLink<GameDataControlPickList> DefaultTemplate = new GameLink<GameDataControl, GameDataControlPickList>(typeof(PickList).GetHashCode(true));
    private readonly Panel backgroundPanel;
    private readonly Panel contentPanel;
    private readonly Label titleLabel;
    private readonly Button closeButton;
    private readonly VirtualizingPanel itemListPanel;

    public bool IsVisible
    {
        get => this.Visible;
        set => this.Visible = value;
    }

    public Action<Item>? OnItemPicked { get; set; }
    public Action? OnHidden { get; set; }

    public PickList() : this(DefaultTemplate)
    {
    }
    
    public PickList(IGameLink<GameDataControlPickList> link) : base(link)
    {
        this.Width = 274;
        this.Height = 368;
        // this.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        // this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        // this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        // this.Position = new UIPosition(1428, 270);
        this.Visible = false;

        // 背景
        backgroundPanel = new Panel
        {
            Width = 274,
            Height = 368,
            Image = "@gameui/image/inventory/bg_drop_list.png"
        };
        this.AddChild(backgroundPanel);

        // 内容面板
        contentPanel = new Panel
        {
            Width = 274,
            Height = 368,
            FlowOrientation = Orientation.Vertical
        };
        this.AddChild(contentPanel);

        // 标题栏
        var titlePanel = new Panel
        {
            Width = 274,
            Height = 42,
            Margin = new Thickness(2, 3, 2, 4)
        };
        contentPanel.AddChild(titlePanel);

        // 标题背景
        var titleBg = new Panel
        {
            Width = 274,
            Height = 42,
            Background = new SolidColorBrush(Color.FromArgb(23, 217, 217, 217))
        };
        titlePanel.AddChild(titleBg);

        // 标题文字
        titleLabel = new Label
        {
            Width = -1,
            Height = 42,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Margin = new Thickness(6, 10, 0, 0),
            Text = "附近",
            FontSize = 32,
            TextColor = new SolidColorBrush(Color.FromArgb(255, 209, 204, 198))
        };
        titlePanel.AddChild(titleLabel);

        // 关闭按钮
        closeButton = new Button
        {
            Width = 48,
            Height = 48,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right
        };
        closeButton.OnPointerClicked += OnCloseButtonClicked;
        titlePanel.AddChild(closeButton);

        var closeIcon = new Panel
        {
            Width = 26,
            Height = 26,
            Image = "@gameui/image/inventory/button_close_guofeng.png",
            Margin = new Thickness(0, 0, 0, 0),
        };
        closeButton.AddChild(closeIcon);

        // 虚拟化列表面板，使用GameData配置的PickListItem模板
        IGameLink<GameDataControlPickListItem>? pickListItemTemplate = null;
        if (Cache is GameDataControlPickList pickListGameData)
        {
            pickListItemTemplate = pickListGameData.PickListItemTemplate;
        }
        
        itemListPanel = new VirtualizingPanel
        {
            Width = 264,
            Height = 76*4,
            FlowOrientation = Orientation.Vertical,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
            Margin = new Thickness(3, 9, 4, 9),
            Background = new SolidColorBrush(Color.FromArgb(76, 0, 0, 0)),
            ItemTemplate = pickListItemTemplate ?? PickListItem.DefaultTemplate,
            ItemSize = new SizeF(264, 76), // 设置每个PickListItem的大小
            ScrollEnabled = true,
            ArrangeOnScroll = true,
            ScrollOrientation = Orientation.Vertical,
            ScrollBarSize = 10
        };
        itemListPanel.OnChildVirtualizationPhase += OnItemVirtualizationPhase;
        contentPanel.AddChild(itemListPanel);
    }

    private void OnCloseButtonClicked(object? sender, EventArgs e)
    {
        IsVisible = false;
    }

    private void OnItemVirtualizationPhase(object? sender, ApplyControlTemplatePhaseEventArgs e)
    {
        if (e.Control is PickListItem listItem && e.Phase == -1 && listItem.DataContext is Item item)
        {
            listItem.BindItem = item;
            listItem.OnItemClicked = OnItemClicked;
        }
    }

    public void UpdateItems(IEnumerable<Item>? items)
    {
        if (items == null)
        {
            Hide();
            return;
        }
        // 更新虚拟化列表的数据源
        itemListPanel.ItemsSource = items.Cast<object>().ToList();
        itemListPanel.InvalidateMeasure();
    }

    private void OnItemClicked(Item item)
    {
        OnItemPicked?.Invoke(item);
    }

    public void Show()
    {
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
        OnHidden?.Invoke();
    }
}

#endif
