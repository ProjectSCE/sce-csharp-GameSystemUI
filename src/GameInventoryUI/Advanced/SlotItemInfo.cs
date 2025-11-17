#if CLIENT
using System.ComponentModel;
using GameCore.BaseType;
using GameCore.Behavior;
using GameCore.Components;
using GameCore.Container;
using GameCore.OrderSystem;
using GameCore.Struct;
using GameUI.Brush;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Enum;
using GameUI.Struct;
using Microsoft.Extensions.Logging;

using GameSystemUI.GameInventoryUI.Data;
using GameCore.DisplayInfo;
using GameData;
using GameData.Extension;
using GameCore.GameSystem.Data;
using static GameCore.ScopeData;
using GameSystemUI.CmdResultSystemUI;
using GameCore.GameSystem.Enum;
using TriggerEncapsulation;

namespace GameSystemUI.GameInventoryUI.Advanced;

// 有些物品的属性还没实现，UI就暂时忽略了
public class ItemInfo : Panel
{
    private InventorySlotUI? slotUI;
    // 顶部UI组件
    private readonly SlotUI itemUI;
    private readonly Label itemNameLabel;
    private readonly Label qualityLabel;
    private readonly Button discardButton;

    // 中部UI组件
    private readonly Panel modificationsPanel;  // 属性增益面板
    private readonly Label tipsPanel;          // 提示面板

    // 底部UI组件
    private readonly Button leftButton;
    private readonly Button rightButton;
    private readonly Label leftButtonLabel;
    private readonly Label rightButtonLabel;

    // 背景,点击关闭
    private readonly Panel background;

    private bool isEquip = true;
    // 左键是否处于装备模式(反之则为卸下)
    public bool IsEquipMode{
        get => isEquip;
        set{
            if(isEquip == value) return;
            isEquip = value;
            if(isEquip)
            {
                leftButton.Image = "@gameui/image/inventory/long_button_equip.png";
                leftButtonLabel.Text = "装备";
            }
            else
            {
                leftButton.Image = "@gameui/image/inventory/long_button_unequip.png";
                leftButtonLabel.Text = "卸下";
            }
        }
    }
    public ItemInfo()
    {
        this.WidthStretchRatio = 1;
        this.HeightStretchRatio = 1;
        this.ZIndex = StandardUIType.ConfirmDialog.ExpectedZIndex ?? 0;
        background = new Panel
        {
            Width = 435,
            Height = 686,
            Image = "@gameui/image/inventory/info_bg_single.png",
            FlowOrientation = Orientation.Vertical,
            Padding = new Thickness(30),
            RoutedEvents = GameUI.Control.Enum.RoutedEvents.None,
        };
        this.AddChild(background);
        this.OnPointerClicked += (sender, e) => {
            this.Visible = false;
            slotUI = null;
        };
        
        // 创建顶部水平布局面板
        var topPanel = new Panel
        {
            WidthStretchRatio = 1,
            Height = 120,
        };
        background.AddChild(topPanel);

        // 创建物品格子
        itemUI = new SlotUI(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 0),
        };
        topPanel.AddChild(itemUI);
        
        // 创建物品名称标签
        itemNameLabel = new Label
        {
            Width = 200,
            Height = -1,
            Position = new UIPosition(50, 20),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,

            Text = "",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 30,
            Bold = true,
            TextTrimming = GameUI.Control.Enum.TextTrimming.Ellipsis,
            
        };
        topPanel.AddChild(itemNameLabel);

        // 创建物品品质标签
        qualityLabel = new Label
        {
            Width = 200,
            Height = -1,
            Position = new UIPosition(50, -20),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,

            Text = "",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 26,
            Bold = true,
            TextTrimming = GameUI.Control.Enum.TextTrimming.Ellipsis,
            
        };
        topPanel.AddChild(qualityLabel);

        // 创建丢弃按钮
        discardButton = new Button
        {
            Width = 46,
            Height = 46,
            Image = "@gameui/image/inventory/icon_drop.png",
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            Margin = new Thickness(10, 10, 0, 0)
        };
        topPanel.AddChild(discardButton);
        discardButton.OnPointerClicked += OnDiscardButtonClicked;

        var midPanel = new PanelScrollable
        {
            WidthStretchRatio = 1,
            Height = 400,
            FlowOrientation = Orientation.Vertical,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
            ScrollOrientation = Orientation.Vertical,
            ScrollEnabled = true,
        };
        background.AddChild(midPanel);

        // 创建属性增益面板
        modificationsPanel = new Panel
        {
            WidthStretchRatio = 1,
            Height = -1,
            Margin = new Thickness(0, 20, 0, 0),
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
            ItemTemplate = ScopeData.Control.ModificationPanel,
            FlowOrientation = Orientation.Vertical,
        };
        midPanel.AddChild(modificationsPanel);

        // 创建提示面板
        tipsPanel = new Label
        {
            Width = 380,
            Height = -1,
            Margin = new Thickness(0, 10, 0, 0),
            Text = "",
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
            FontSize = 24,
        };
        midPanel.AddChild(tipsPanel);

        // 创建底部水平布局面板
        var bottomPanel = new Panel
        {
            WidthStretchRatio = 1,
            Height = 59,
            Margin = new Thickness(0, 10, 0, 0),
        };
        background.AddChild(bottomPanel);

        // 创建底部按钮
        leftButton = new Button
        {
            WidthStretchRatio = 1,
            Height = 59,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Image = "@gameui/image/inventory/long_button_equip.png",
        };
        bottomPanel.AddChild(leftButton);

        leftButtonLabel = new Label
        {
            FontSize = 24,
            Text = "装备",
        };
        leftButton.AddChild(leftButtonLabel);

        leftButton.OnPointerClicked += OnLeftButtonClicked;

        rightButton = new Button
        {
            WidthStretchRatio = 1,
            Height = 59,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            Image = "@gameui/image/inventory/long_button_equip.png",
        };
        bottomPanel.AddChild(rightButton);

        rightButtonLabel = new Label
        {
            FontSize = 24,
            Text = "使用",
        };
        rightButton.AddChild(rightButtonLabel);

        rightButton.OnPointerClicked += OnRightButtonClicked;
    }

    private void OnDiscardButtonClicked(object? sender, EventArgs e)
    {
        if (slotUI == null) return;
        var item = slotUI.Slot?.Item;
        if (item == null) return;
        InventorySlotUI.SendDropRequest(item);
        this.Visible = false;
        this.slotUI = null;
    }

    private static bool IsEquipItem(Item item)
    {
        var itemMod = item as ItemMod;
        if(itemMod == null) return false;
        var manager = itemMod.GetModificationManager(ItemSlotType.Equip);
        return manager != null;
    }

    private static bool IsAbilityItem(Item item)
    {
        var itemMod = item as ItemMod;
        if(itemMod == null) return false;
        var ability = itemMod.ActiveAbility;
        return ability != null;
    }

    public void Open(InventorySlotUI slotUI)
    {
        this.Visible = true;
        this.slotUI = slotUI;

        var slot = slotUI.Slot;
        var item = slot?.Item;

        // 位置改到SlotUI附近
        var pos = slotUI.ScreenPosition;
        
        this.background.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.background.Position = new UIPosition(pos.Left, 0);

        itemUI.UpdateUI(item);
        
        // 名称
        var displayInfo = item as IDisplayInfo;
        itemNameLabel.Text = displayInfo?.DisplayName ?? "";
        // 描述相关
        tipsPanel.Text = displayInfo?.Description ?? "";
        // 品质相关
        var qualityData = item?.QualityConfiguration;
        qualityLabel.Text = qualityData?.Name ?? "";
        qualityLabel.TextColor = qualityData?.Color ?? System.Drawing.Color.White;

        
        if(slot is not null && item is not null)
        {
            // 属性修改相关
            var items = new List<Tuple<string, string, bool>>();
            if (item is ItemMod itemMod)
            {
                // 动态遍历所有ItemSlotType枚举值
                foreach (ItemSlotType slotType in Enum.GetValues<ItemSlotType>())
                {
                    var modManager = itemMod.GetModificationManager(slotType);

                    if(modManager?.Modifications != null || itemMod.EnumerateDynamicModifications(slotType).Any())
                    {
                        items.Add(new Tuple<string, string, bool>(slotType == ItemSlotType.Equip ? "装备:" : "背包:", "", true));
                    }
                    
                    if (modManager?.Modifications != null)
                    {
                        foreach (var mod in modManager.Modifications)
                        {
                            if (mod?.Property?.FriendlyName != null)
                            {
                                var stackCount = Math.Max(item.Stack, 1);
                                items.Add(new Tuple<string, string, bool>($"·{mod.Property.FriendlyName}", $"{(mod.Value >= 0 ? "+" : "")}{mod.Value * stackCount}", false));
                            }
                        }
                    }

                    foreach(var mod in itemMod.EnumerateDynamicModifications(slotType))
                    {
                        var propertydata = mod.PropertyLink.Data;
                        if(propertydata is not null)
                        {
                            var stackCount = Math.Max(item.Stack, 1);
                            items.Add(new Tuple<string, string, bool>($"·{propertydata.Name}", $"{(mod.Value >= 0 ? "+" : "")}{mod.Value * stackCount}", false));
                        }
                    }
                    // 格式要做变化
                }
            }
            modificationsPanel.ItemsSource = items;
            modificationsPanel.GenerateChildren();
            // 底部按钮
            bool isEquip = IsEquipItem(item);
            bool isAbility = IsAbilityItem(item);
            // 控制显示
            leftButton.Visible = isEquip;
            rightButton.Visible = isAbility;
            // 两个都显示就改下大小
            if(isEquip && isAbility)
            {
                leftButton.WidthStretchRatio = 0.4f;
                rightButton.WidthStretchRatio = 0.4f;
            }
            else
            {
                leftButton.WidthStretchRatio = 1;
                rightButton.WidthStretchRatio = 1;
            }
            // 装备按钮
            if(isEquip)
            {
                IsEquipMode = slot.Cache.Type != ItemSlotType.Equip;
            }
        }
    }

    private void OnLeftButtonClicked(object? sender, EventArgs e)
    {
        if(slotUI == null) return;
        var item = slotUI.Slot?.Item;

        if(item == null) return;

        if(IsEquipMode)
        {
            InventoryUI.TryEquipItem(item);
        }
        else
        {
            InventoryUI.TryUnequipItem(item);
        }
        this.Visible = false;
    }

    private void OnRightButtonClicked(object? sender, EventArgs e)
    {
        if(slotUI == null)
        {
            CmdResultManager.ShowCmdResult((CmdError)CmdErrorInventory.SlotUINotFound);
            return;
        }
        var carrier = slotUI.Slot?.Inventory.Carrier;
        if(carrier == null)
        {
            CmdResultManager.ShowCmdResult((CmdError)CmdErrorInventory.ItemCarrierNotFound);
            return;
        }
        Command commandUse = new()
        {
            Index = CommandIndexInventory.Use,
            Item = slotUI.Slot?.Item,
            Type = ComponentTagEx.InventoryManager,
            Flag = CommandFlag.Queued
        };
        var result = commandUse.IssueOrder(carrier);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
            return;
        }
        this.Visible = false;
    }


    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }
    
}
#endif
