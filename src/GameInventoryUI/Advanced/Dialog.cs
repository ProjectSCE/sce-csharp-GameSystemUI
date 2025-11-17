#if CLIENT
using GameUI.Brush;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Enum;
using GameUI.Struct;

using GameSystemUI.GameInventoryUI.Data;
using GameCore.DisplayInfo;
using GameCore.GameSystem.Enum;
using GameSystemUI.CmdResultSystemUI;

namespace GameSystemUI.GameInventoryUI.Advanced;

public class Dialog : Panel
{
    private readonly Panel dialogPanel;
    private readonly Label titleLabel;
    private readonly Label contentLabel;
    private readonly Button closeButton;
    private readonly Button leftButton;
    private readonly Button rightButton;
    private readonly Label leftButtonLabel;
    private readonly Label rightButtonLabel;
    private readonly Panel fullScreenPanel;
    private ItemPickable? item;

    public string? Title{
        get{
            return titleLabel.Text;
        }
        set{
            titleLabel.Text = value;
        }
    }

    public string? Content{
        get{
            return contentLabel.Text;
        }
        set{
            contentLabel.Text = value;
        }
    }
    public Dialog()
    {
        // 设置全屏底
        this.WidthStretchRatio = 1;
        this.HeightStretchRatio = 1;
        this.ZIndex = StandardUIType.ConfirmDialog.ExpectedZIndex ?? 0;

        // 创建背景
        fullScreenPanel = new Panel
        {
            WidthStretchRatio = 1,
            HeightStretchRatio = 1,
            Background = new SolidColorBrush(System.Drawing.Color.Gray),
            Opacity = 0.5f,
        };
        this.AddChild(fullScreenPanel);
        // 创建对话框面板
        dialogPanel = new Panel
        {
            Width = 834,
            Height = 489,
            Image = "@gameui/image/inventory/Dialog.png",
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center
        };
        this.AddChild(dialogPanel);

        // 创建标题标签
        titleLabel = new Label
        {
            Width = -1,
            Height = 89,
            Text = "",
            Bold = true,
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 48,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            // Margin = new Thickness(0, 20, 0, 0)
        };
        dialogPanel.AddChild(titleLabel);

        // 创建关闭按钮
        closeButton = new Button
        {
            Width = 51,
            Height = 51,
            Image = "@gameui/image/inventory/close.png",
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Margin = new Thickness(20),
        };
        closeButton.OnPointerClicked += (sender, args) => this.Visible = false;
        dialogPanel.AddChild(closeButton);

        // 创建内容标签
        contentLabel = new Label
        {
            Height = 71,
            Width = -1,
            Text = "",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 36,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 20, 0)
        };
        dialogPanel.AddChild(contentLabel);

        // 创建左侧按钮
        leftButton = new Button
        {
            Width = 306,
            Height = 82,
            Image = "@gameui/image/inventory/button_confirm.png",
            Margin = new Thickness(34, 30, 0, 60),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        dialogPanel.AddChild(leftButton);
        leftButton.OnPointerReleased += (sender, args) => {
            this.Visible = false;
            Drop();
        };

        // 创建左侧按钮文本
        leftButtonLabel = new Label
        {
            Text = "确认",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 36,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center
        };
        leftButton.AddChild(leftButtonLabel);

        // 创建右侧按钮
        rightButton = new Button
        {
            Width = 306,
            Height = 82,
            Image = "@gameui/image/inventory/button_cancel.png",
            Margin = new Thickness(0, 30, 34, 60),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        dialogPanel.AddChild(rightButton);
        rightButton.OnPointerReleased += (sender, args) => this.Visible = false;

        // 创建右侧按钮文本
        rightButtonLabel = new Label
        {
            Text = "取消",
            TextColor = new SolidColorBrush(System.Drawing.Color.White),
            FontSize = 36,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center
        };
        rightButton.AddChild(rightButtonLabel);
    }

    public void Open(ItemPickable item){
        this.Visible = true;
        this.Title = "丢弃物品";
        this.item = item;
        var displayInfo = item as IDisplayInfo;
        this.Content = displayInfo?.DisplayName ?? "";
    }
    
    private void Drop()
    {
        if (item == null || item.Slot == null) return;
        Command commandDrop = new()
        {
            Index = CommandIndexInventory.Drop,
            Item = item,
            Type = ComponentTagEx.InventoryManager,
            Flag = CommandFlag.Queued
        };
        // 给物品栏的所有者发送交换命令
        var result = commandDrop.IssueOrder(item.Slot.Inventory.Carrier);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
    }
}
#endif
