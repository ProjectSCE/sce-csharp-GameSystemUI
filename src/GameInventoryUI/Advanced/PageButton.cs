#if CLIENT
using System.Drawing;
using GameCore.Container;
using GameData;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameData.Extension;
using Microsoft.Extensions.Logging;
using GameCore.Struct;
using GameUI.Control.Struct;

using GameSystemUI.GameInventoryUI.Data;
using GameUI.Control.Extensions;

namespace GameSystemUI.GameInventoryUI.Advanced;


public class PageButton : Button
{
    private readonly Label label;
    private Image _activeImage = new("@gameui/image/inventory/tab_check.png");
    private Image _inactiveImage = new("@gameui/image/inventory/tab_default.png");
    
    // 拖拽悬停延迟行为组件
    private DragHoverDelayBehavior? dragHoverBehavior;
    
    /// <summary>
    /// 激活状态图片
    /// </summary>
    public Image ActiveImage
    {
        get => _activeImage;
        set
        {
            _activeImage = value;
            if (isActive)
                SetActive(true);
        }
    }
    
    /// <summary>
    /// 非激活状态图片
    /// </summary>
    public Image InactiveImage
    {
        get => _inactiveImage;
        set
        {
            _inactiveImage = value;
            if (!isActive)
                SetActive(false);
        }
    }
    private static readonly SolidColorBrush activeTextColor = new(System.Drawing.Color.White);
    private static readonly SolidColorBrush inactiveTextColor = new(System.Drawing.Color.FromArgb(204, 255, 255, 255));
    public new static readonly IGameLink<GameDataControlPageButton> DefaultTemplate = new GameLink<GameDataControl, GameDataControlPageButton>(typeof(SlotListUI).GetHashCode(deterministic: true));

    private bool isActive = false;
    public bool IsActive {
        get => isActive;
        set {
            isActive = value;
            SetActive(isActive);
        }
    }
    public string? Text {
        get => label.Text;
        set => label.Text = value;
    }
    static PageButton()
    {
        // 未来或许可以支持读配置
    }
    public PageButton():this(DefaultTemplate)
    {
    }

    public PageButton(IGameLink<GameDataControlPageButton> link) : base(link)
    {
        this.Width = 200;
        this.Height = 65;
        this.Margin = new(0, 0, 10, 0);
        
        // 从GameData获取图片配置
        if (Cache is GameDataControlPageButton pageButtonData)
        {
            if (pageButtonData.ActiveImage != null)
                _activeImage = pageButtonData.ActiveImage.Value;
            if (pageButtonData.InactiveImage != null)
                _inactiveImage = pageButtonData.InactiveImage.Value;
        }
        
        this.Image = _inactiveImage.Path;
        
        label = new()
        {
            Text = "Inventory",
            Width = -1,
            Height = -1,
            FontSize = 32,
            TextColor = activeTextColor,
            TextTrimming = GameUI.Control.Enum.TextTrimming.Ellipsis,
            Bold = true,
            Margin = new(10, top: 10, 10, 10),
        };
        this.AddChild(label);
        this.OnPointerClicked += OnClick;
        this.OnPointerEntered += OnPageButtonPointerEntered;
        this.OnPointerExited += OnPageButtonPointerExited;
        
        // 初始化拖拽悬停延迟行为组件
        dragHoverBehavior = new DragHoverDelayBehavior(this, () =>
        {
            // 延迟触发页面切换
            if (this.Parent is PageButtonList parent)
            {
                parent.OnPageButtonClicked?.Invoke(this);
            }
        });
    }

    private void SetActive(bool active)
    {
        this.Image = active ? _activeImage.Path : _inactiveImage.Path;
        this.label.TextColor = active ? activeTextColor : inactiveTextColor;
    }

    // 或许可以丢进数编
    private void OnClick(object? sender, EventArgs e)
    {
        if(this.Parent is PageButtonList parent)
        {
            parent.OnPageButtonClicked?.Invoke(this);
        }
    }

    /// <summary>
    /// 鼠标进入事件，用于拖拽时自动切换背包（带延迟防误触）
    /// </summary>
    private void OnPageButtonPointerEntered(object? sender, EventArgs e)
    {
        dragHoverBehavior?.OnPointerEntered();
    }

    /// <summary>
    /// 鼠标离开事件，取消延迟触发
    /// </summary>
    private void OnPageButtonPointerExited(object? sender, EventArgs e)
    {
        dragHoverBehavior?.OnPointerExited();
    }

    protected override void DisposeManaged()
    {
        // 清理拖拽悬停延迟行为组件
        dragHoverBehavior?.Destroy();
        dragHoverBehavior = null;
        
        base.DisposeManaged();
    }
}

public class PageButtonList : VirtualizingPanel
{
    private IInventoryUI? inventoryUI;
    
    public IInventoryUI? InventoryUI {
        get => inventoryUI;
        set {   
            inventoryUI = value;
        }
    }

    public Action<PageButton>? OnPageButtonClicked { get; set; }
    
    public PageButtonList(IGameLink<GameDataControlPageButton>? pageButtonTemplate)
    {
        this.Width = 500;
        this.ScrollEnabled = true;
        this.ScrollBarSize = 0.1f;
        this.ArrangeOnScroll = true;
        this.FlowOrientation = Orientation.Horizontal;
        this.ScrollOrientation = Orientation.Horizontal;
        this.HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left;
        this.ItemSize = new SizeF(210, 65);
        this.ItemTemplate = pageButtonTemplate ?? ScopeData.Control.DefaultPageButton;

        this.OnChildVirtualizationPhase += OnPageButtonVirtualizationPhase;
        this.OnPageButtonClicked = (button) => {
            if(this.InventoryUI is not null && button.DataContext is Inventory inventory)
            {
                this.InventoryUI.CurrentInventory = inventory;
                this.SetGeneratedChildrenInactive();
                button.IsActive = true;
            }
        };
    }

    public async Task UpdateButtons()
    {
        if(inventoryUI is null) return;
        this.ItemsSource = inventoryUI.Inventories;
        await Game.NextTick();
        await Game.NextTick(); //暂时多等一帧
        this.InvalidateMeasure();
    }

    private void SetGeneratedChildrenInactive()
    {
        if(this.children is null) return;
        foreach(var child in this.children)
        {
            if(child is PageButton button) button.IsActive = false;
        }
    }

    private void OnPageButtonVirtualizationPhase(object? sender, ApplyControlTemplatePhaseEventArgs e)
    {
        if(e.Control is PageButton c)
        {
            // 模板在ItemTemplate中已经应用，这里无需额外处理
            
            if(c.DataContext is GameCore.Container.Inventory inventory)
            {
                c.Text = inventory.Cache.Name;
                c.IsActive = false;
                if(this.InventoryUI is not null)
                {
                    var currentInventory = this.InventoryUI.CurrentInventory;
                    if(currentInventory is not null && currentInventory == inventory)
                    {
                        c.IsActive = true;
                    }
                }
            }
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        this.InventoryUI = null;
        this.ItemsSource = null;
    }
}
#endif
