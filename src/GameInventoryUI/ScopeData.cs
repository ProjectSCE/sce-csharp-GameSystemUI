#if CLIENT

using GameCore.Container;
using GameData;
using GameUI.Struct;
using Events;
using GameCore.Event;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameSystemUI.GameInventoryUI.Data;
using GameSystemUI.GameInventoryUI.Advanced;
using GameUI.Control.Enum;
using GameSystemUI.AbilitySystemUI.Data;

namespace GameSystemUI.GameInventoryUI;
public class ScopeData : IGameClass
{
    public static class Control
    {
        public static bool EnableDefaultInventoryUI = false;
        public static readonly GameLink<GameDataControl, GameDataControlInventorySlotUI> DefaultInventorySlotUI = new("DefaultInventorySlotUI");
        public static readonly GameLink<GameDataControl, GameDataControlSlotListUI> DefaultSlotListUI = new("DefaultSlotListUI");
        public static readonly GameLink<GameDataControl, GameDataControlPageButton> DefaultPageButton = new("DefaultPageButton");
        public static readonly GameLink<GameDataControl, GameDataControlPanel> DefaultPanel = new("DefaultPanel");
        public static readonly GameLink<GameDataControl, GameDataControlListOption> DefaultListOption = new("DefaultListOption");
        public static readonly GameLink<GameDataControl, GameDataControlDefaultInventoryUI> DefaultInventoryUI = new("DefaultInventoryUI");
        public static readonly GameLink<GameDataControl, GameDataControlPanel> ModificationPanel = new("ModificationPanel");
        public static readonly GameLink<GameDataControl, GameDataControlDefaultInventoryUI> TestInventoryUI = new("TestInventoryUI");
        public static readonly GameLink<GameDataControl, GameDataControlPickListItem> PickListItem = new("PickListItem");
        public static readonly GameLink<GameDataControl, GameDataControlQuickBarUI> DefaultQuickBarUI = new("DefaultQuickBarUI");
        public static readonly GameLink<GameDataControl, GameDataControlAbilitySlotUI> DefaultAbilitySlotUI = new("DefaultAbilitySlotUI");
        public static readonly GameLink<GameDataControl, GameDataControlPickList> DefaultPickList = new("DefaultPickList");
        public static readonly GameLink<GameDataControl, GameDataControlPickButton> DefaultPickButton = new("DefaultPickButton");
        
        /// <summary>
        /// 自定义页标按钮模板
        /// </summary>
        public static readonly GameLink<GameDataControl, GameDataControlPageButton> CustomPageButton = new("CustomPageButton");
        public static readonly GameLink<GameDataControl, GameDataControlAbilityJoyStick> AbilitySlotUIJoyStick = new("AbilitySlotUIJoyStick");
        public static readonly GameLink<GameDataControl, GameDataControlInventoryUIEntrance> DefaultInventoryUIEntrance = new("DefaultInventoryUIEntrance");
    }

    public static class PickButtonConfig
    {
        public static readonly int Period = 300;
        // public static readonly int Range = 500;
    }


    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
        // 等出UI编辑器了删掉
        if(Control.EnableDefaultInventoryUI)
        {
            Game.OnGameTriggerInitialization += Game_OnGameTriggerInitialization;
        }
    }

    private static void Game_OnGameTriggerInitialization()
    {
        Trigger<EventGameStart> trigger1 = new(async (s, d) =>
        {
            DefaultInventoryUI? inv = null;
            PickButton? pickButton = null;
            Trigger<EventPlayerMainUnitChanged> trigger1 = new(async (s, d) =>
            {
                
                var mainUnit = Player.LocalPlayer!.MainUnit;
                if (d.Unit != mainUnit)
                {
                    return false;
                }
                var inventoryManager = mainUnit?.GetComponent<InventoryManager>() ?? throw new InvalidOperationException("Failed to get inventory manager");
                // 获取第一个背包
                var inventoryMain = inventoryManager.Inventories[0];
                if (inventoryMain != null)
                {
                    if(inv == null)
                    {
                        // 等一会，不然取不到背包
                        // await Game.Delay(TimeSpan.FromSeconds(1));
                        inv = new DefaultInventoryUI(Control.DefaultInventoryUI)
                        {
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            BindUnit = mainUnit
                        };

                        var entrance = new InventoryUIEntrance(inv){
                            VerticalAlignment = VerticalAlignment.Top,
                        };
                        entrance.AddToVisualTree();
                        inv.AddToVisualTree();
                        inv.Visible = false;
                        var quickBarUI = new QuickBarUI(){
                            VerticalAlignment = VerticalAlignment.Bottom,
                        };
                        quickBarUI.BindUnit = mainUnit;
                        _ = quickBarUI.AddToVisualTree();   

                        pickButton = new(Control.DefaultPickButton){
                            BindUnit = mainUnit,
                        };
                        _ = pickButton.AddToVisualTree();
                    }
                    else
                    {
                        inv.BindUnit = mainUnit;
                        if(pickButton != null)
                        {
                            pickButton.BindUnit = mainUnit;
                        }
                    }
                }
                return true;
            });
            var player = Player.LocalPlayer ?? throw new InvalidOperationException("Failed to get LocalPlayer");
            trigger1.Register(player);
            await Task.CompletedTask;
            return true;
        });
        trigger1.Register(Game.Instance);
    }


    private static void OnGameDataInitialization()
    {
        // 数编数据作为控件模板的样例
        
         _ = new GameDataControlDefaultInventoryUI(Control.TestInventoryUI)
        {
            FilterCategories = [MyItemCategory.Test1, MyItemCategory.Test2],
            PageButtonTemplate = Control.CustomPageButton,
        };
        _ = new GameDataControlInventorySlotUI(Control.DefaultInventorySlotUI)
        {
            ItemSize = 116,
            SlotSize = 116,
        };

        _ = new GameDataControlPickListItem(Control.PickListItem)
        {
            ItemWidth = 264,
            ItemHeight = 76,
            SlotSize = 76,
            NameFontSize = 20,
            QualityFontSize = 16,
            CategoryFontSize = 16,
        };
        
        _ = new GameDataControlPickList(Control.DefaultPickList)
        {
            PickListItemTemplate = Control.PickListItem,
            ListWidth = 274,
            ListHeight = 368,
        };
        
        _ = new GameDataControlPickButton(Control.DefaultPickButton)
        {
            Period = 300,
            PickListTemplate = Control.DefaultPickList,
        };

        _ = new GameDataControlQuickBarUI(Control.DefaultQuickBarUI)
        {
        };

        _ = new GameDataControlAbilityJoyStick(Control.AbilitySlotUIJoyStick)
        {
            AbilityActiveFrame = new Image(),
            AbilityBackgroundFrame = new Image(),
        };

        _ = new GameDataControlAbilitySlotUI(Control.DefaultAbilitySlotUI)
        {
            Layout = new()
            {
                Margin = new Thickness(10,0,10,0),
            },
            AbilityJoyStickTemplate = Control.AbilitySlotUIJoyStick,
        };

        _ = new GameDataControlSlotListUI(Control.DefaultSlotListUI)
        {
        };

        _ = new GameDataControlPageButton(Control.DefaultPageButton)
        {
        };
        
        _ = new GameDataControlPageButton(Control.CustomPageButton)
        {
            // ActiveImage = "@gameui/image/inventory/tab_check.png"u8,
            // InactiveImage = "@gameui/image/inventory/tab_default.png"u8,
            InactiveImage = new Image("@gameui/image/inventory/tab_check.png"),
            ActiveImage = new Image("@gameui/image/inventory/tab_default.png"),
        };
        _ = new GameDataControlPanel(Control.DefaultPanel)
        {
            Background = new SolidColorBrush(System.Drawing.Color.Red),
            Layout = new()
            {
                Width = 117,
                Height = 117,
                Margin = new Thickness(10),
            },
        };
        _ = new GameDataControlListOption(Control.DefaultListOption)
        {
        };
        _ = new GameDataControlDefaultInventoryUI(Control.DefaultInventoryUI)
        {
        };
        
        _ = new GameDataControlInventoryUIEntrance(Control.DefaultInventoryUIEntrance)
        {
        };
        _ = new GameDataControlPanel(Control.ModificationPanel)
        {
            Layout = new()
            {
                WidthStretchRatio = 1,
                Height = -1,
                Margin = new Thickness(0, 0, 0, 10),
            },
            Children = [
                new GameDataControlLabel(new("ModificationPanelLabel1"))
                {
                    Text = "属性增益",
                    Layout = new()
                    {
                        WidthStretchRatio = 0.5f,
                        HorizontalContentAlignment = HorizontalContentAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    },
                    TextTrimming = TextTrimming.Ellipsis,
                    FontSize = 24,
                }.Link,
                new GameDataControlLabel(new("ModificationPanelLabel2"))
                {
                    Text = "属性增益",
                    Layout = new()
                    {
                        WidthStretchRatio = 0.5f,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        HorizontalContentAlignment = HorizontalContentAlignment.Right,
                    },
                    TextTrimming = TextTrimming.Ellipsis,
                    FontSize = 24,
                }.Link,
            ],
            OnPostInitialization = static (c) =>
            {
                if(c is Panel && c.DataContext is Tuple<string, string, bool> item)
                {
                    if(c.Children is not null && c.Children[0] is Label label1 && c.Children[1] is Label label2)
                    {
                        label1.Text = item.Item1;
                        label1.Bold = item.Item3;
                        label2.Text = item.Item2;
                    }
                }
            },
        };
    }
}
#endif