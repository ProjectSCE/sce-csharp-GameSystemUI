#if CLIENT
using System;
using System.Drawing;
using GameCore.ResourceType;
using GameUI.Brush;
using GameUI.Control.Enum;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameUI.Struct;

namespace GameSystemUI.ChatSystem;

public class ChatInputPanel : Panel
{
    // UI组件
    private Panel channelSwitchButton = null!;
    private Panel channelSwitchList = null!;
    private Label currentChannelLabel = null!;
    private Input inputBox = null!;
    public string InputText = "";
    
    // 状态标志
    private bool isPlaceholderState = true; // 是否处于占位符状态
    
    // 事件
    public event Action<ChatMessageType>? OnChannelChanged;
    public event EventHandler? OnSendMessage;
    
    public bool HasValidInput => !isPlaceholderState && !string.IsNullOrWhiteSpace(InputText);
    
    public ChatMessageType CurrentChannel
    {
        set => currentChannelLabel.Text = value == ChatMessageType.All ? "全体" : "队伍";
    }
    
    public ChatInputPanel()
    {
        InitializeInputPanel();
    }
    
    private void InitializeInputPanel()
    {
        // 容器设置
        this.Image = "@gameui/image/chat/输入框.png";
        this.Margin = new Thickness(17, 0, 270, 26);
        this.WidthStretchRatio = 1.0f;
        this.WidthCompactRatio = 1.0f;
        this.Height = 52;
        this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.FlowOrientation = Orientation.Horizontal;

        CreateChannelSwitchUI();
        CreateDivider();
        CreateInputBox();
    }
    
    private void CreateChannelSwitchUI()
    {
        // 频道切换按钮
        channelSwitchButton = new Panel(){
            // Margin = new Thickness(0, 0, 135, 0),
            Width = 118,
            Height = 52,
            // Image = "@gameui/image/chat/输入框.png",
        };
        this.AddChild(channelSwitchButton);

        currentChannelLabel = new Label(){
            Text = "全体",
            FontSize = 28,
            TextColor = Color.FromArgb(255, 217, 217, 217), // #D9D9D9
            Bold = true,
            WidthStretchRatio = 1.0f,
            HeightStretchRatio = 1.0f,
        };
        channelSwitchButton.AddChild(currentChannelLabel);

        // var channelArrow = new Panel(){
        //     Width = 24,
        //     Height = 13,
        //     Image = "@gameui/image/chat/箭头.png",
        //     Position = new UIPosition(83, 10),
        // };
        // channelSwitchButton.AddChild(channelArrow);

        // 频道切换列表（默认隐藏）
        channelSwitchList = new Panel(){
            Width = 118,
            Height = 91,
            Image = "@gameui/image/chat/输入框.png",
            Position = new UIPosition(0, -70),
            Visible = false,
            FlowOrientation = Orientation.Vertical,
        };
        channelSwitchButton.AddChild(channelSwitchList);

        CreateChannelButtons();
        SetupChannelEvents();
    }
    
    private void CreateChannelButtons()
    {
        // 全体频道按钮
        var allChannelButton = new Panel(){
            Width = 100,
            Height = 45,
        };
        channelSwitchList.AddChild(allChannelButton);

        var allChannelLabel = new Label(){
            Text = "全体",
            FontSize = 28,
            TextColor = Color.FromArgb(255, 217, 217, 217),
            Bold = true,
            Width = 84,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
        };
        allChannelButton.AddChild(allChannelLabel);

        var divider = new Panel(){
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            // Margin = new Thickness(5, 0, 5, 0),
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(255, 76, 76, 81)),
        };
        channelSwitchList.AddChild(divider);

        // 队伍频道按钮
        var teamChannelButton = new Panel(){
            Width = 100,
            Height = 45,
        };
        channelSwitchList.AddChild(teamChannelButton);

        var teamChannelLabel = new Label(){
            Text = "队伍",
            FontSize = 28,
            TextColor = Color.FromArgb(255, 217, 217, 217),
            Bold = true,
            Width = 84,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
        };
        teamChannelButton.AddChild(teamChannelLabel);
        
        // 绑定事件
        allChannelButton.OnPointerClicked += (sender, e) => {
            OnChannelChanged?.Invoke(ChatMessageType.All);
            channelSwitchList.Visible = false;
        };

        teamChannelButton.OnPointerClicked += (sender, e) => {
            OnChannelChanged?.Invoke(ChatMessageType.Team);
            channelSwitchList.Visible = false;
        };
    }
    
    private void SetupChannelEvents()
    {
        // 只在当前频道标签上监听点击，避免子控件事件冒泡
        currentChannelLabel.OnPointerClicked += (sender, e) => {
            channelSwitchList.Visible = !channelSwitchList.Visible;
        };
    }
    
    private void CreateInputBox()
    {
        inputBox = new Input(){
            // Margin = new Thickness(135, 4, 31, 0),
            WidthStretchRatio = 1.0f,
            HeightStretchRatio = 1.0f,
            FontSize = 28,
            TextColor = Color.FromArgb(255, 35, 35, 35), // #353535
            Text = "点此输入文字...",
        };
        inputBox.OnInputKeyDown += (sender, e) => {
            if (e.Key == GameCore.Platform.SDL.VirtualKey.Return)
            {
                OnSendMessage?.Invoke(this, EventArgs.Empty);
            }
        };
        this.AddChild(inputBox);

        inputBox.OnInputTextChanged += (sender, e) => {
            InputText = e.Text ?? "";
            // 首次输入时，清除占位符并设置正常文本颜色
            if (isPlaceholderState )
            {
                isPlaceholderState = false;
                inputBox.TextColor = Color.FromArgb(255, 255, 255, 255); // 白色文字
            }
        };
    }
    
    private void CreateDivider()
    {
        var divider = new Panel(){
            Width = 2,
            Height = 40,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromArgb(255, 76, 76, 81)), // #4C4C51
        };
        this.AddChild(divider);
    }
    
    public void ClearInput()
    {
        inputBox.Text = "";
        isPlaceholderState = true; // 重置为占位符状态
    }
    

}

#endif
