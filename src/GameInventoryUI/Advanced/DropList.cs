#if CLIENT
using GameCore.BaseType;
using GameCore.Collection;
using GameCore.Container;
using GameCore.EntitySystem;
using GameCore.Event;
using GameData;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameUI.Struct;
using GameData.Extension;

using Microsoft.Extensions.Logging;
using System.Drawing;
using GameData.Interface;
using GameCore.Struct;
using GameUI.Control.Struct;
using TriggerEncapsulation;

using GameSystemUI.GameInventoryUI.Data;

namespace GameSystemUI.GameInventoryUI.Advanced;

// 下拉筛选列表的选项
public class ListOption : Button, IGameObject<GameDataControlListOption>, IGameObject
{
    public static readonly SolidColorBrush SELECTED_COLOR = new(System.Drawing.Color.FromArgb(255, 211, 223, 234));
    public static readonly SolidColorBrush UNSELECTED_COLOR = new(System.Drawing.Color.FromArgb(255, 99, 111, 122));
    public new static readonly IGameLink<GameDataControlListOption> DefaultTemplate = new GameLink<GameDataControl, GameDataControlListOption>(typeof(ListOption).GetHashCode(deterministic: true));
    IGameLink IGameObject.Link => Link;
    IGameData IGameObject.Cache => Cache;
    IGameLink<GameDataControlListOption> IGameObject<GameDataControlListOption>.Link => (IGameLink<GameDataControlListOption>)base.Link;

    GameDataControlListOption IGameObject<GameDataControlListOption>.Cache => (GameDataControlListOption)base.Cache;

    // 左侧标签
    private readonly Label leftLabel;
    // 右侧面板
    private readonly Panel rightPanel;
    // 分类
    public ItemCategory category = DefaultItemCategory.All;
    private bool isSelected = true;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if(isSelected == value) return;
            isSelected = value;
            rightPanel.Visible = isSelected;
            leftLabel.TextColor = isSelected ? SELECTED_COLOR : UNSELECTED_COLOR;
            this.UserDataSet("IsSelected", isSelected);
        }
    }

    // 设置左侧文本
    public string Text
    {
        get => leftLabel.Text ?? "";
        set => leftLabel.Text = value;
    }

    // 设置右侧图标
    public string RightIcon
    {
        get => rightPanel.Image;
        set => rightPanel.Image = value;
    }
    
    public ListOption():this(DefaultTemplate)
    {
    }

    public ListOption(IGameLink<GameDataControlListOption> link) : base(link)
    {
        // 设置按钮基本属性
        this.Width = 316;
        this.Height = 64;
        this.Padding = new Thickness(23, 0, 44, 0);

        // 创建左侧文本
        leftLabel = new Label
        {
            Text = "全部",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            Bold = true,
            FontSize = 31,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
        };
        this.AddChild(leftLabel);

        // 创建右侧图标面板
        rightPanel = new Panel
        {
            Width = 27,
            Height = 20,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            Position = new UIPosition(-10, 0),
            Image = "@gameui/image/inventory/true.png"
        };
        this.AddChild(rightPanel);
        this.OnPointerClicked += OnClick;
        this.IsSelected = false;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        if(this.Parent is Panel parent && parent.Parent is DropList dropList)
        {
            dropList.OnOptionSelected?.Invoke(this);
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }
}

public class DropList : Panel
{
    public Action<ListOption>? OnOptionSelected { get; set; }
    
    private readonly Panel listPanel;
    private readonly Button mainButton;
    private readonly Label mainLabel;
    public string? Text{
        get => mainLabel.Text;
        set => mainLabel.Text = value;
    }
    private readonly DefaultInventoryUI? bindInventoryUI;
    public DropList(InventoryUI inventoryUI)
    {
        this.Width = 316;
        this.Height = -1;
        Background = new SolidColorBrush(System.Drawing.Color.FromArgb(255, 8, 34, 59));
        this.FlowOrientation = Orientation.Vertical;
        this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        this.listPanel = new Panel
        {
            Width = 316,
            Height = -1,
            Visible = false,
            FlowOrientation = Orientation.Vertical,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Bottom,
            ItemTemplate = ScopeData.Control.DefaultListOption
        };
        this.AddChild(listPanel);
    
        this.mainButton = new Button
        {
            Width = 316,
            Height = 64,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Image = "@gameui/image/inventory/drop_list_bg_up.png"
        };
        this.AddChild(mainButton);

        this.mainButton.OnPointerClicked += (sender, e) =>
        {
            listPanel.Visible = !listPanel.Visible;
        };

        this.mainLabel = new Label
        {
            Text = "全部",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            Bold = true,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 30, 0),
            FontSize = 31,
        };
        this.mainButton.AddChild(mainLabel);

        this.listPanel.OnChildPostInitialization += OnOptionPostInitialization;
        // 设置选项选择命令
        OnOptionSelected = DefaultOptionSelected;

        // 绑定背包UI
        bindInventoryUI = inventoryUI as DefaultInventoryUI;
        if(bindInventoryUI is not null)
        {   
            // "全部"分类需要特殊处理
            if(bindInventoryUI.Cache is not null)
            {
                this.Visible = true;
                if(bindInventoryUI.Cache is GameDataControlDefaultInventoryUI defaultInventoryGameData)
                {
                    var categories = defaultInventoryGameData.FilterCategories;
                    var list = categories.Cast<object>().ToList();
                    list.Insert(0, DefaultItemCategory.All);
                    listPanel.ItemsSource = list;
                }
   
            }
            else
            {
                this.Visible = false;
                return;
            }
            listPanel.GenerateChildren();
        }
        else
        {
            this.Visible = false;
            OnOptionSelected = null;
        }
    }
    
    private void DefaultOptionSelected(ListOption option)
    {
        // 先取消选中所有页签
        this.SetOptionsInactive();
        // 选中当前页签
        option.IsSelected = true;
        // 更新文本
        this.Text = option.Text;
        // 更新背包筛选
        if(bindInventoryUI is not null)
        {
            bindInventoryUI.FilterCategory = option.category;
        }
    }

    private void SetOptionsInactive()
    {
        if(listPanel.GeneratedChildren is not null)
        {
            foreach(var child in listPanel.GeneratedChildren)
            {
                if(child is ListOption listOption)
                {
                    listOption.IsSelected = false;
                }
            }
        }
    }

    private void OnOptionPostInitialization(object? sender, ApplyControlTemplateEventArgs e)
    {
        if(e.Control is ListOption c)
        {
            var category = c.DataContext as ItemCategory?;
            if(category is not null)
            {
                c.category = category.Value;
                c.Text = category.ToString() ?? "全部";
            }
            if(c.category == DefaultItemCategory.All)
            {
                c.IsSelected = true;
            }
            else
            {
                c.IsSelected = false;
            }
        }
    }
}
#endif