#if CLIENT
using GameCore.BaseType;
using GameCore.Collection;
using GameCore.Container;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.Struct;
using GameData;
using GameData.Interface;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameUI.Struct;
using GameData.Extension;

using Microsoft.Extensions.Logging;
using Events;
using GameCore.OrderSystem;

using GameSystemUI.GameInventoryUI.Data;
using GameUI.Control.Enum;
using GameCore.GameSystem.Enum;
using GameSystemUI.CmdResultSystemUI;

namespace GameSystemUI.GameInventoryUI.Advanced;

public interface IInventoryUI
{
    public Inventory? CurrentInventory{get;set;}
    public OrderedSet<Inventory> Inventories{get;}
    public Unit? BindUnit{get;set;}
}


// 背包未考虑背包和格子数量动态变化，如有需要触发器事件。
// 背包抽象类，自带页标列表和(格子、堆叠、品质)的同步。
public abstract class InventoryUI : Panel, IInventoryUI
{
    public static bool TryEquipItem(ItemPickable item)
    {
        var carrier = item.Carrier;
        if(carrier == null) return false;
        var inventories = carrier.GetComponent<InventoryManager>()?.Inventories ?? [];
        
        foreach(var inventory in inventories)
            foreach(var slot in inventory.Slots)
            {
                if(slot.Item == null && slot.Cache.Type == ItemSlotType.Equip && slot.CanAssign(item))
                {
                    Command commandEquip = new()
                    {
                        Index = CommandIndexInventory.Swap,
                        Item = item,
                        Target = slot,
                        Type = ComponentTagEx.InventoryManager,
                    };
                    var result = commandEquip.IssueOrder(carrier);
                    if (!result.IsSuccess)
                    {
                        CmdResultManager.ShowCmdResult(result);
                        return false;
                    }
                    return result == CmdResult.Ok;
                }
            }
        CmdResultManager.ShowCmdResult((CmdError)CmdErrorInventory.AssignableSlotNotFound);
        return false;
    }

    public static bool TryUnequipItem(ItemPickable item)
    {
        var carrier = item.Carrier;
        if(carrier == null) return false;
        var inventories = carrier.GetComponent<InventoryManager>()?.Inventories ?? [];

        foreach(var inventory in inventories)
            foreach(var slot in inventory.Slots)
            {
                if(slot.Item == null && slot.Cache.Type == ItemSlotType.Carry && slot.CanAssign(item))
                {
                    Command commandEquip = new()
                    {
                        Index = CommandIndexInventory.Swap,
                        Item = item,
                        Target = slot,
                        Type = ComponentTagEx.InventoryManager,
                    };
                    var result = commandEquip.IssueOrder(carrier);
                    if (!result.IsSuccess)
                    {
                        CmdResultManager.ShowCmdResult(result);
                        return false;
                    }
                    return result == CmdResult.Ok;
                }                
            }
        CmdResultManager.ShowCmdResult((CmdError)CmdErrorInventory.AssignableSlotNotFound);
        return false;
    }

    private Unit? bindUnit;
    public Unit? BindUnit{
        get => bindUnit;
        set{
            if(bindUnit == value) return;

            // 移除旧的背包管理器事件
            if(bindUnit != null)
            {
                InventoryManager? oldInventoryManager = bindUnit.GetOrCreateComponent<InventoryManager>();
                if(oldInventoryManager != null)
                {
                    oldInventoryManager.InventoryAttached -= OnInventoryAttached;
                    oldInventoryManager.InventoryDetached -= OnInventoryDetached;
                }
            }

            // 添加新的背包管理器事件
            bindUnit = value;
            inventories = bindUnit?.GetComponent<InventoryManager>()?.Inventories ?? [];
            InventoryManager? inventoryManager = bindUnit?.GetOrCreateComponent<InventoryManager>();
            if(inventoryManager != null)
            {
                inventoryManager.InventoryAttached += OnInventoryAttached;
                inventoryManager.InventoryDetached += OnInventoryDetached;
            }

            // 不为空的初始化
            if(inventories.Count == 0) return;
            CurrentInventory = inventories[0];
            _ = this.pageButtonList.UpdateButtons();
        }
    }
    private OrderedSet<Inventory> inventories = [];
    public OrderedSet<Inventory> Inventories{
        get => inventories;
    }
    public abstract Inventory? CurrentInventory{get;set;}

    private readonly Trigger<EventItemSlotChange> slotChangeTrigger;
    private readonly Trigger<EventItemStackChange> stackChangeTrigger;
    private readonly Trigger<EventItemQualityChange> qualityChangeTrigger;
    
    // 页标列表
    protected readonly PageButtonList pageButtonList;

    public InventoryUI(IGameLink<GameDataControlPanel> link) : base(link)
    {
        // 从GameData获取页标按钮模板
        IGameLink<GameDataControlPageButton>? pageButtonTemplate = null;
        if (Cache is GameDataControlInventoryUI inventoryUIData)
        {
            pageButtonTemplate = inventoryUIData.PageButtonTemplate;
        }
        
        this.pageButtonList = new PageButtonList(pageButtonTemplate){
            InventoryUI = this,
        };

        this.slotChangeTrigger = new Trigger<EventItemSlotChange>(OnSlotChanged);
        this.slotChangeTrigger.Register(Game.Instance);
        this.stackChangeTrigger = new Trigger<EventItemStackChange>(OnStackChanged);
        this.stackChangeTrigger.Register(Game.Instance);
        this.qualityChangeTrigger = new Trigger<EventItemQualityChange>(OnQualityChanged);
        this.qualityChangeTrigger.Register(Game.Instance);
        if(link.Data?.ZIndex != null)
        {
            this.ZIndex = link.Data.ZIndex.Value;
        }
        else
        {
            this.ZIndex = StandardUIType.Inventory.ExpectedZIndex ?? 0;
        }
    }

    // 更新格子UI，交给子类实现
    protected abstract void UpdateSlotUI(InventorySlot slot);

    private void OnInventoryAttached(Inventory inventory, InventoryManager inventoryManager)
    {
        _ = this.pageButtonList.UpdateButtons();
        if(CurrentInventory == null)
        {
            CurrentInventory = inventory;
        }
    }

    private void OnInventoryDetached(Inventory inventory, InventoryManager inventoryManager)
    {
        _ = this.pageButtonList.UpdateButtons();
        if(CurrentInventory == inventory)
        {
            CurrentInventory = inventories.Count > 0 ? inventories[0] : null;
        }
    }


    private async Task<bool> OnSlotChanged(object sender, EventItemSlotChange data)
    {
        // 检查是否是当前背包的格子发生变化
        if (data.Slot?.Inventory == CurrentInventory && data.Slot is not null)
        {
            UpdateSlotUI(data.Slot);
        }
        if(data.SlotPrevious?.Inventory == CurrentInventory && data.SlotPrevious is not null)
        {
            UpdateSlotUI(data.SlotPrevious);
        }
        await Task.CompletedTask;
        return true;
    }
    private async Task<bool> OnStackChanged(object sender, EventItemStackChange data)
    {
        if(data.Item.Slot?.Inventory == CurrentInventory && data.Item.Slot is not null)
        {
            UpdateSlotUI(data.Item.Slot);
        }
        await Task.CompletedTask;
        return true;
    }

    private async Task<bool> OnQualityChanged(object sender, EventItemQualityChange data)
    {
        if(data.Item is ItemPickable item && item.Slot?.Inventory == CurrentInventory && item.Slot is not null)
        {
            UpdateSlotUI(item.Slot);
        }
        await Task.CompletedTask;
        return true;
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        this.BindUnit = null;
        this.CurrentInventory = null;
        this.slotChangeTrigger.Destroy();
        this.stackChangeTrigger.Destroy();
        this.qualityChangeTrigger.Destroy();
    }
}

/// 背包UI类，用于显示和管理单位身上的背包(没考虑背包数量变化)
public partial class DefaultInventoryUI : InventoryUI
{
    public new static readonly IGameLink<GameDataControlDefaultInventoryUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlDefaultInventoryUI>(typeof(InventoryUI).GetHashCode(deterministic: true));

    private ItemCategory? filterCategory;
    public ItemCategory? FilterCategory{
        get => filterCategory;
        set{
            filterCategory = value;
            this.slotContainer.FilterCategory = filterCategory;
        }
    }
    private Inventory? currentInventory;
    public override Inventory? CurrentInventory{
        get => currentInventory;
        set{
            if(currentInventory == value) return;
            if( CurrentInventory != null && !Inventories.Contains(value)) 
            {
                Game.Logger.LogError("Current inventory not in inventories");
                return;
            }
            currentInventory = value;
            this.slotContainer.BindInventory = currentInventory;
            filterCategory = DefaultItemCategory.All;
        }
    }

    // UI部分 
    // 关闭按钮
    private readonly Button closeButton;
    // 物品格子容器（带自动滚动功能）
    private readonly SlotContainer slotContainer;
    // 丢弃按钮
    private readonly InventoryDropButton dropButton;
    private readonly Panel midPanel;
    // 分类列表
    private readonly DropList filterList;
    public DefaultInventoryUI():this(DefaultTemplate)
    {
    }

    public DefaultInventoryUI(IGameLink<GameDataControlDefaultInventoryUI> link) : base(link)
    {
        // 初始化自身面板属性
        this.Width = 800;
        this.Height = 855;
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        this.RoutedEvents = RoutedEvents.None;
        
        // 应用GameData配置的背景图片
        if (Cache is GameDataControlDefaultInventoryUI inventoryGameData)
        {
            this.Image = inventoryGameData.BackgroundImage?.Path ?? "@gameui/image/inventory/up_bg.png";
        }
        else
        {
            this.Image = "@gameui/image/inventory/up_bg.png";
        }

        // 创建中间面板
        this.midPanel = new Panel(){
            WidthStretchRatio = 1,
            Height = 600,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Margin = new Thickness(0, 110, 0, 0),
            FlowOrientation = Orientation.Vertical,
        };
        this.AddChild(midPanel);

        // 创建页标列表
        this.pageButtonList.WidthStretchRatio = 1;
        this.pageButtonList.InventoryUI = this;
        this.pageButtonList.Height = 65;
        this.pageButtonList.Margin = new Thickness(50, 0, 50, 20);
        
        // 页标按钮模板已在基类构造函数中配置
        
        midPanel.AddChild(pageButtonList);

        // 创建物品格子容器，使用GameData配置的格子模板
        IGameLink<GameDataControlInventorySlotUI>? slotTemplate = null;
        if(Cache is GameDataControlDefaultInventoryUI inventoryGameData2)
        {
            slotTemplate = inventoryGameData2.SlotTemplate;
        }
        this.slotContainer = new SlotContainer(slotTemplate){
            WidthStretchRatio = 1,
            HeightStretchRatio = 1,
            Margin = new Thickness(50, 20, 50, 0),
        };
        midPanel.AddChild(slotContainer);

        // 创建关闭按钮
        this.closeButton = new Button
        {
            Width = 57,
            Height = 57,
            Margin = new Thickness(23, 23, 23, 23),
            Position = new UIPosition(-23, 0),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
        };
        
        // 应用GameData配置的关闭按钮图片
        if (Cache is GameDataControlDefaultInventoryUI closeButtonGameData)
        {
            this.closeButton.Image = closeButtonGameData.CloseButtonImage?.Path ?? "@gameui/image/inventory/close.png";
        }
        else
        {
            this.closeButton.Image = "@gameui/image/inventory/close.png";
        }
        
        this.AddChild(closeButton);
        // 关闭按钮点击事件
        this.closeButton.OnPointerClicked += (sender, args) =>{
            this.Visible = false;
        };
        

        // 创建丢弃按钮
        this.dropButton = new InventoryDropButton(this){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 20),
            Position = new UIPosition(128, 0),
        };
        
        // 应用GameData配置的按钮图片
        if(Cache is GameDataControlDefaultInventoryUI inventoryGameData3)
        {
            // 配置丢弃按钮图片
            if (inventoryGameData3.DropButtonActiveImage != null && inventoryGameData3.DropButtonInactiveImage != null)
            {
                dropButton.ActiveImage = inventoryGameData3.DropButtonActiveImage.Value;
                dropButton.InactiveImage = inventoryGameData3.DropButtonInactiveImage.Value;
            }
        }
        
        this.AddChild(dropButton);

        // 创建分类列表
        this.filterList = new DropList(this){
            Margin = new Thickness(0, 0, 0, 20),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.AddChild(filterList);
    }

    protected override void UpdateSlotUI(InventorySlot slot)
    {
        var slotUI = this.slotContainer.GetSlotUI(slot.SlotIndex);
        if(slotUI is not null && slotUI.Slot == slot)
        {
            slotUI.UpdateUI();
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }

}

public class InventoryUIEntrance : Panel, IGameObject<GameDataControlInventoryUIEntrance>, IGameObject
{
    private readonly InventoryUI inventoryUI;
    
    public new IGameLink<GameDataControlInventoryUIEntrance> Link { get; }
    public new GameDataControlInventoryUIEntrance Cache => Link.Data!;
    
    public InventoryUIEntrance(InventoryUI inventoryUI) : this(GameSystemUI.GameInventoryUI.ScopeData.Control.DefaultInventoryUIEntrance, inventoryUI)
    {
    }
    
    public InventoryUIEntrance(IGameLink<GameDataControlInventoryUIEntrance> link, InventoryUI inventoryUI) : base(link)
    {
        this.Link = link;
        this.inventoryUI = inventoryUI;
        
        // 应用ZIndex逻辑
        if(link.Data?.ZIndex != null)
        {
            this.ZIndex = link.Data.ZIndex.Value;
        }
        else
        {
            this.ZIndex = StandardUIType.Hotbar.ExpectedZIndex ?? 0;
        }
        
        // 应用GameData配置的默认值
        this.Width = 72;
        this.Height = 72;
        this.Image = "@gameui/image/inventory/entrance.png";
        this.OnPointerClicked += (sender, args) =>
        {
            this.inventoryUI.Visible = true;
        };
    }
}

public class InventoryDropButton : Button
{
    private InventoryUI? inventoryUI;
    private bool isActive = false;
    private Image _activeImage = new("@gameui/image/inventory/drop_active.png");
    private Image _inactiveImage = new("@gameui/image/inventory/drop.png");
    
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
                this.Image = _activeImage.Path;
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
                this.Image = _inactiveImage.Path;
        }
    }
    
    public bool IsActive{
        get => isActive;
        set{
            if(isActive == value) return;
            isActive = value;
            this.Image = isActive ? _activeImage.Path : _inactiveImage.Path;
        }
    }
    
    public InventoryDropButton(InventoryUI inventoryUI)
    {
        this.inventoryUI = inventoryUI;
        Width = 88;
        Height = 693;
        Image = _inactiveImage.Path;
        AllowDrop = true;
        this.OnPointerEntered += (sender, args) =>
        {
            if (InventorySlotUI.IsDragging)
            {
                this.IsActive = true;
            }
                
        };
        this.OnPointerExited += (sender, args) =>
        {
            if (InventorySlotUI.IsDragging)
            {
                this.IsActive = false;
            }
        };
    }
    
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        this.inventoryUI = null;
    }
}
#endif