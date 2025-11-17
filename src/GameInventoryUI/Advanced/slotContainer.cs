#if CLIENT
using GameCore.BaseType;
using GameCore.Container;
using GameCore.Event;
using GameData;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameData.Extension;
using GameUI.Enum;
using GameUI.Struct;
using Microsoft.Extensions.Logging;
using System.Drawing;
using Events;
using System.Linq.Expressions;
using GameCore.Struct;
using GameUI.Control.Struct;

using GameSystemUI.GameInventoryUI.Data;

namespace GameSystemUI.GameInventoryUI.Advanced;

public class SlotListUI : Panel
{
    public List<InventorySlot> Slots {
        set{
            this.ItemsSource = value;
            this.GenerateChildren();
        }
    }

    public new static readonly IGameLink<GameDataControlSlotListUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlSlotListUI>(typeof(SlotListUI).GetHashCode(deterministic: true));

    public SlotListUI() : this(DefaultTemplate)
    {
    }

    public SlotListUI(IGameLink<GameDataControlSlotListUI> link):base(link)
    {
        this.Height = 127;
        this.Width = - 1;
        this.FlowOrientation = Orientation.Horizontal;
        this.HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left;
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.ItemTemplate = ScopeData.Control.DefaultInventorySlotUI;
        this.OnChildPostInitialization += SlotOnPostInitialization;
    }

    private void SlotOnPostInitialization(object? sender, ApplyControlTemplateEventArgs e)
    {
        if(e.Control is InventorySlotUI inventorySlotUI && inventorySlotUI.DataContext is InventorySlot slot)
        {
            inventorySlotUI.Slot = slot;
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        this.OnChildPostInitialization -= SlotOnPostInitialization;
    }
}

public class SlotMartixUI : VirtualizingPanel
{
    private IGameLink<GameDataControlInventorySlotUI>? slotTemplate;
    private Inventory? bindInventory;
    public Inventory? BindInventory {
        get => bindInventory;
        set{
            if(bindInventory == value) return;
            bindInventory = value;
            filterCategory = DefaultItemCategory.All;
            _ = UpdateItemsSourceAsync();
        }
    }
    private int listSize = 6;
    private ItemCategory? filterCategory;
    public ItemCategory? FilterCategory {
        get => filterCategory;
        set{
            if(filterCategory == value) return;
            filterCategory = value;
            _ = UpdateItemsSourceAsync();
        }
    }
    // 每行格子数量
    public int ListSize {
        get => listSize;
        set{
            if(listSize == value) return;
            listSize = value;
            this.Width = 127 * listSize;
            _ = UpdateItemsSourceAsync();
        }
    }

    public SlotMartixUI() : this(null)
    {
    }

    public SlotMartixUI(IGameLink<GameDataControlInventorySlotUI>? slotTemplate)
    {
        this.slotTemplate = slotTemplate;
        this.Height = 127 * 4 + 10;
        this.Width = 127 * listSize;
        this.ItemTemplate = ScopeData.Control.DefaultSlotListUI;
        this.ItemSize = new SizeF(127 * listSize, height: 127);
        this.ScrollEnabled = true;
        this.ArrangeOnScroll = true;
        this.FlowOrientation = Orientation.Vertical;
        this.VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top;
        this.ScrollOrientation = Orientation.Vertical;
        this.ScrollBarSize = 10;


        this.OnChildVirtualizationPhase += OnSlotListVirtualizationPhase;
    }

    // 获取指定槽位的格子
    public InventorySlotUI? GetSlotUI(int index)
    {
        index ++;
        var row = (index - 1) / listSize;
        var column = index % listSize;
        if(column == 0) column = listSize;
        column --;

        var slotListUI = GetChildByItem(row) as SlotListUI;
        if(slotListUI is not null)
        {
            return slotListUI.GetChildByItem(column) as InventorySlotUI;
        }
        return null;
    }

    private async Task UpdateItemsSourceAsync(){
        if(bindInventory == null){
            this.Visible = false;
            return;
        }
        this.Visible = true;
        var martix = new List<List<InventorySlot>>();
        var list = new List<InventorySlot>();
        var count = 0;

        for(int i = 0; i < bindInventory.Slots.Count; i++)
        {
            // 实际下标从0开始
            var slot = bindInventory.Slots[i];
            // 筛选
            if(filterCategory.HasValue && filterCategory.Value != DefaultItemCategory.All)
            {
                if(slot.Item is not null && slot.Item.Categories.Contains(filterCategory.Value))
                {
                    list.Add(slot);
                    count ++;
                }
            }
            else
            {
                list.Add(slot);
                count ++;
            }

            if(count > 0 && count % listSize == 0)
            {
                martix.Add(list);
                list = [];
            }
        }
        
        if(count % listSize != 0)
        {
            martix.Add(list);
        }
        if(count == 0)
        {
            martix.Add([]);
        }
        this.ItemsSource = martix;

        // 让它下一帧更新
        await Game.NextTick();
        await Game.NextTick(); //暂时多等一帧
        this.InvalidateMeasure();
    }

    private void OnSlotListVirtualizationPhase(object? sender, ApplyControlTemplatePhaseEventArgs e)
    {
        if(e.Control is SlotListUI slotListUI && e.Phase == -1 && slotListUI.DataContext is List<InventorySlot> list)
        {
            // 如果有自定义格子模板，设置给SlotListUI
            if (slotTemplate != null)
            {
                slotListUI.ItemTemplate = slotTemplate;
            }
            slotListUI.Slots = list;
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }

}

public class SlotContainer : VirtualizingPanelAutoScrollWrapper
{
    private readonly SlotMartixUI slotMartixUI;

    public SlotContainer(IGameLink<GameDataControlInventorySlotUI>? slotTemplate) : base(new SlotMartixUI(slotTemplate))
    {
        slotMartixUI = (SlotMartixUI)InnerPanel;
    }

    /// <summary>
    /// 获取内部的 SlotMartixUI（用于访问特定功能）
    /// </summary>
    public SlotMartixUI SlotMartix => slotMartixUI;

    /// <summary>
    /// 绑定的背包
    /// </summary>
    public Inventory? BindInventory
    {
        get => slotMartixUI.BindInventory;
        set => slotMartixUI.BindInventory = value;
    }

    /// <summary>
    /// 筛选分类
    /// </summary>
    public ItemCategory? FilterCategory
    {
        get => slotMartixUI.FilterCategory;
        set => slotMartixUI.FilterCategory = value;
    }

    /// <summary>
    /// 每行格子数量
    /// </summary>
    public int ListSize
    {
        get => slotMartixUI.ListSize;
        set => slotMartixUI.ListSize = value;
    }

    /// <summary>
    /// 获取指定槽位的格子UI
    /// </summary>
    public InventorySlotUI? GetSlotUI(int index) => slotMartixUI.GetSlotUI(index);

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }
}

#endif