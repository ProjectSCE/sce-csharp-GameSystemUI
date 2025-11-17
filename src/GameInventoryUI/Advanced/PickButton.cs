#if CLIENT
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameUI.Control.Primitive;
using GameCore.Container;
using GameCore.SceneSystem;
using GameCore.AbilitySystem.Manager;
using GameCore.Components;
using Microsoft.Extensions.Logging;
using GameCore.OrderSystem;
using static GameCore.ScopeData;
using GameCore.Timers;

using GameSystemUI.GameInventoryUI.Data;
using GameSystemUI.CmdResultSystemUI;
using GameUI.Control.Data;

namespace GameSystemUI.GameInventoryUI.Advanced;

public class PickButton : Panel
{
    /// <summary>
    /// 默认模板链接
    /// </summary>
    public static new readonly IGameLink<GameDataControlPickButton> DefaultTemplate = new GameLink<GameDataControl, GameDataControlPickButton>(typeof(PickButton).GetHashCode(true));

    private Unit? bindUnit;
    internal uint period = 300; //周期，理论上要从数编读
    private uint range; //范围，理论上要从数编读
    private readonly GameCore.Timers.Timer timer;
    private IEnumerable<Item>? items;
    private readonly PickList pickList;
    // private readonly Trigger<EventItemSlotChange> itemSlotChangeTrigger;
    public Unit? BindUnit {
        get {
            return bindUnit;
        }
        set {
            if (bindUnit == value) return;
            bindUnit = value;
            if (bindUnit != null)
            {
                range = (uint)bindUnit.Cache.Properties[UnitProperty.InventoryPickUpRange];
            }
            else
            {
                range = 0;
            }
        }
    }
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public PickButton() : this(DefaultTemplate)
    {
    }

    /// <summary>
    /// 绑定单位构造函数
    /// </summary>
    public PickButton(Unit unit) : this(DefaultTemplate)
    {
        this.BindUnit = unit;
    }

    /// <summary>
    /// 完整构造函数
    /// </summary>
    public PickButton(IGameLink<GameDataControlPickButton> link) : base(link)
    {
        this.Width = 95;
        this.Height = 95;
        this.Image = "@gameui/image/inventory/pick_button.png";
        this.Visible = false;
        
        // 创建PickList，使用GameData配置的模板
        IGameLink<GameDataControlPickList>? pickListTemplate = null;
        if (Cache is GameDataControlPickButton pickButtonGameData)
        {
            pickListTemplate = pickButtonGameData.PickListTemplate;

        }
        
        pickList = pickListTemplate != null 
            ? new PickList(pickListTemplate)
            : new PickList();
        pickList.AddToVisualTree();
        pickList.OnItemPicked = OnItemPicked;
        pickList.OnHidden = OnPickListHidden;
        
        if (bindUnit == null)
        {
            this.BindUnit = Player.LocalPlayer.MainUnit;
        }
        else
        {
            this.BindUnit = bindUnit;
        }
        this.OnPointerClicked += OnClicked;
        timer = new GameCore.Timers.Timer(period);
        timer.Elapsed += (sender, e) => RefreshPickupState();
        timer.Start();

    }

    private void OnClicked(object? sender, EventArgs e)
    {
        if (bindUnit != null && items != null && items.Any())
        {
            // 显示时更新物品列表
            pickList.UpdateItems(items);
            pickList.Show();
            this.Visible = false;
        }
    }

    private void OnItemPicked(Item item)
    {
        Pick(item);
    }

    private void OnPickListHidden()
    {
        // PickList关闭时，如果有物品则重新显示PickButton
        if (items != null && items.Any())
        {
            this.Visible = true;
        }
    }

    private void RefreshPickupState()
    {
        if (this.bindUnit == null) return;

        var scene = this.bindUnit.Scene;
        items = scene.SearchCircle(this.bindUnit.Position, range, (e) => e.GetComponent<Item>());
        
        if (items == null || !items.Any())
        {
            this.Visible = false;
            pickList.Hide();
        }
        else
        {
            // 如果PickList可见，则更新物品列表但不显示PickButton
            if (pickList.IsVisible)
            {
                this.Visible = false;
                pickList.UpdateItems(items);
            }
            else
            {
                this.Visible = true;
            }
        }
    }

    private void Pick(Item item)
    {
        if (bindUnit == null)
        {
            return;
        }
        Command command = new()
        {
            Index = CommandIndexInventory.PickUp,
            Target = item,
            Type = ComponentTagEx.InventoryManager,
            Flag = CommandFlag.Queued
        };
        var result = command.IssueOrder(bindUnit);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        timer.Stop();
        pickList.Destroy();
    }

}

#endif
