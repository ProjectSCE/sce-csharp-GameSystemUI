#if CLIENT
using GameCore.Container;
using GameUI.Brush;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameUI.Struct;
using GameUI.Control.Enum;
using GameCore.ResourceType;
using GameData;
using GameUI.Control;
using GameCore.Timers;

using GameSystemUI.GameInventoryUI.Data;
using GameCore.AbilitySystem.Manager;
using GameCore.AbilitySystem;
using GameCore.GameSystem.Enum;
using GameUI.Control.Data;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 快捷物品栏UI类，支持GameData配置
/// </summary>
public class QuickBarUI : InventoryUI
{
    private Inventory? currentInventory;
    public override Inventory? CurrentInventory {
        get => currentInventory;
        set {
            if( CurrentInventory != null && !Inventories.Contains(value)) 
            {
                Game.Logger.LogError("Current inventory not in inventories");
                return;
            }
            currentInventory = value;
            // 切换物品栏时重置到第一页
            currentPage = 1;
            // 会根据currentPage刷新格子内容
            Refresh();
        }
    }
    // 存储所有物品格子的UI
    private List<AbilitySlotUI> slotUIs = null!;
    // 主界面面板
    private Panel mainPanel = null!;
    // 模式切换按钮背景
    private Panel modeChangeButtonIcon = null!;
    // 模式切换按钮(锁定/解锁)
    private Panel modeChangeButton = null!;
    // 左移按钮
    private Panel leftButton = null!;
    // 右移按钮
    private Panel rightButton = null!;
    // 物品格子容器
    private Panel slotContainer = null!;
    public new Unit? BindUnit{
        get => base.BindUnit;
        set{
            if(base.BindUnit == value) return;
            if(base.BindUnit != null)
            {
                AbilityManager? abilityManager = base.BindUnit.GetOrCreateComponent<AbilityManager>();
                abilityManager.OnObjectAttached -= OnAbilityAttached;
            }
            if(value != null)
            {
                AbilityManager? abilityManager = value.GetOrCreateComponent<AbilityManager>();
                abilityManager.OnObjectAttached += OnAbilityAttached;
            }
            base.BindUnit = value;
        }
    }
    private int currentPage = 1;
    
    // GameData 相关属性
    public static new readonly IGameLink DefaultTemplate = ScopeData.Control.DefaultQuickBarUI;
    
    /// <summary>
    /// 格子数量
    /// </summary>
    internal int SlotCount { get; set; } = 6;
    
    /// <summary>
    /// 模式切换按钮图片（锁定状态）
    /// </summary>
    internal Image ModeButtonLocked { get; set; } = new Image("@gameui/image/inventory/locked.png");
    
    /// <summary>
    /// 模式切换按钮图片（解锁状态）
    /// </summary>
    internal Image ModeButtonUnlocked { get; set; } = new Image("@gameui/image/inventory/unlock.png");
    
    /// <summary>
    /// 左移按钮图片
    /// </summary>
    internal Image LeftButtonImage { get; set; } = new Image("@gameui/image/inventory/left_button.png");
    
    /// <summary>
    /// 右移按钮图片
    /// </summary>
    internal Image RightButtonImage { get; set; } = new Image("@gameui/image/inventory/right_button.png");
    
    /// <summary>
    /// 快捷栏背景图片
    /// </summary>
    internal Image BackgroundImage { get; set; } = new Image("@gameui/image/inventory/quick_frame.png");

    private bool isInitialized = false;
    

    private bool isLocked = true;
    /// <summary>
    /// 是否锁定状态
    /// </summary>
    public bool IsLocked
    {
        get => isLocked;
        set
        {
            if (isLocked == value) return;
            isLocked = value;
            UpdateLockState();
        }
    }

    public QuickBarUI() : this(DefaultTemplate)
    {
    }

    public QuickBarUI(IGameLink link) : base(ScopeData.Control.DefaultQuickBarUI)
    {
        InitializeUI();
        isInitialized = true;
    }

    // 要大改，暂时先这样
    private void InitializeUI()
    {
        this.slotUIs = [];
        this.Height = 150;
        this.Width = 884;
        this.Margin = new Thickness(left: 324, top: 70, 126, 0);
        this.Background = new SolidColorBrush(System.Drawing.Color.FromArgb(alpha: 215, 23, green: 23, 23));
        this.RoutedEvents = RoutedEvents.None;
        this.ZIndex = StandardUIType.Hotbar.ExpectedZIndex ?? 0;
        // 创建主界面面板
        this.mainPanel = new Panel
        {
            Width = 1005,
            Height = 197,
            Image = BackgroundImage.Path,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Position = new UIPosition(0, 17)
        };
        this.AddChild(mainPanel);

        // 创建物品格子容器
        this.slotContainer = new Panel(){
            FlowOrientation = Orientation.Horizontal,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
            Width = 796,
            Height = 150,
            Position = new UIPosition(Left: 13, Top: -10),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.mainPanel.AddChild(slotContainer);

        // 创建可配置数量的格子
        IGameLink<GameDataControlAbilitySlotUI>? slotTemplate = null;
        if (Cache is GameDataControlQuickBarUI quickBarGameData)
        {
            slotTemplate = quickBarGameData.SlotTemplate;
        }
        
        for (int i = 0; i < SlotCount; i++)
        {
            // 使用GameData配置的格子模板，否则使用默认模板
            var slotUI = slotTemplate != null 
                ? new AbilitySlotUI(slotTemplate)
                : new AbilitySlotUI(ScopeData.Control.DefaultAbilitySlotUI);
            
            slotUI.Slot = null;
            slotUI.IsLocked = IsLocked;
            slotUIs.Add(slotUI);
            slotContainer.AddChild(slotUI);
        }

        // 创建模式切换按钮
        this.modeChangeButton = new Panel
        {
            Width = 114,
            Height = 114,
            Background = new SolidColorBrush(System.Drawing.Color.FromArgb(77, 0, 0, 0)),
            Position = new UIPosition(-210, 0),
            Margin = new Thickness(left: 0, 0, 0, 12),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.modeChangeButton.OnPointerClicked += OnModeChangeClicked;
        this.AddChild(modeChangeButton);

        // 创建模式切换按钮图标
        this.modeChangeButtonIcon = new Panel
        {
            Width = 54,
            Height = 54,
            Image = ModeButtonLocked.Path,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center
        };
        
        this.modeChangeButton.AddChild(modeChangeButtonIcon);

        // 创建左移按钮
        this.leftButton = new Panel
        {
            Width = 58,
            Height = 114,
            Image = LeftButtonImage.Path,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Position = new UIPosition(-90, 0),
            Margin = new Thickness(left: 0, 0, 0, 12)
        };
        this.leftButton.OnPointerClicked += OnLeftButtonClicked;
        this.AddChild(leftButton);

        // 创建右移按钮
        this.rightButton = new Panel
        {
            Width = 58,
            Height = 114,
            Image = RightButtonImage.Path,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Position = new UIPosition(Left: 90, 0),
            Margin = new Thickness(0, 0, 0, 12)
        };
        this.rightButton.OnPointerClicked += OnRightButtonClicked;
        this.AddChild(rightButton);

        // 创建页签列表
        this.pageButtonList.Width = 760;
        this.pageButtonList.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        this.pageButtonList.Position = new UIPosition(Left: 0, Top: -70);
        this.AddChild(pageButtonList);
        
        // 初始刷新UI
        Refresh();
    }
    
    /// <summary>
    /// 更新锁定状态UI
    /// </summary>
    private void UpdateLockState()
    {
        // 可能从gamedata触发，这时候还没初始化完
        if(!isInitialized)
        {
            return;
        }
        modeChangeButtonIcon.Image = IsLocked ? ModeButtonLocked.Path : ModeButtonUnlocked.Path;
        // 更新所有格子的拖动模式
        foreach (var slotUI in slotUIs)
        {
            slotUI.IsLocked = IsLocked;
        }
    }

    private void OnAbilityAttached(Ability a)
    {
        if(this.CurrentInventory != null && a.IsGrantedByItem && a.Item is ItemMod itemMod)
        {
            if(itemMod.Slot is not null && itemMod.Slot.Inventory == CurrentInventory)
            {
                var slotUI = GetSlotUI(itemMod.Slot);
                if(slotUI is not null)
                {
                    slotUI.CurrentAbilityExecute = a as AbilityExecute;
                }
            }
        }        
    }

    private void OnModeChangeClicked(object? sender, EventArgs e)
    {
        IsLocked = !IsLocked;
    }

    private void OnLeftButtonClicked(object? sender, EventArgs e)
    {
        if (currentPage > 1)
        {
            ChangePage(currentPage - 1);
        }
    }

    private void OnRightButtonClicked(object? sender, EventArgs e)
    {
        if (CurrentInventory != null && currentPage < Math.Ceiling(CurrentInventory.Slots.Count / (double)SlotCount))
        {
            ChangePage(currentPage + 1);
        }
    }

    private void ChangePage(int page)
    {
        if (CurrentInventory == null) return;
        
        currentPage = page;

        // 更新格子内容
        for (int i = 0; i < SlotCount; i++)
        {
            int slotIndex = (currentPage - 1) * SlotCount + i;
            slotUIs[i].Slot = slotIndex < CurrentInventory.Slots.Count ? CurrentInventory.Slots[slotIndex] : null;
        }
        
        UpdatePageButtons();
    }

    private void UpdatePageButtons()
    {
        if (CurrentInventory == null) return;
        
        int totalPages = (int)Math.Ceiling(CurrentInventory.Slots.Count / (double)SlotCount);
        leftButton.Visible = currentPage > 1;
        rightButton.Visible = currentPage < totalPages;
    }

    public void Refresh()
    {
        ChangePage(currentPage);
    }

    private AbilitySlotUI? GetSlotUI(InventorySlot changedSlot)
    {
        if (CurrentInventory == null) return null;

        int changedSlotIndex = changedSlot.SlotIndex;
        
        int pageIndex = changedSlotIndex / SlotCount + 1;
        if(pageIndex != currentPage)
        {
            return null;
        }

        return slotUIs[changedSlotIndex % SlotCount];
    }

    protected override void UpdateSlotUI(InventorySlot changedSlot)
    {
        var slotUI = GetSlotUI(changedSlot);
        if(slotUI is not null)
        {
            slotUI.UpdateUI();
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }
}
#endif
