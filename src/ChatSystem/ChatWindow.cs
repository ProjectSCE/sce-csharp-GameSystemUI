#if CLIENT
using System;
using System.Drawing;
using System.Threading.Tasks;
using Events;
using GameCore.Platform.SDL;
using GameCore.ResourceType;
using GameUI.Brush;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Enum;
using GameUI.Struct;
using GameUI.Control.Enum;
using GameUI.TriggerEvent;
using GameCore.GameSystem.Enum;

namespace GameSystemUI.ChatSystem;

public enum TabType
{
    ChatRecord,
    PlayerList
}

public class ChatEntrance : Panel
{
    public ChatWindow chatWindow;
    public ChatEntrance(ChatWindow chatWindow)
    {
        this.Width = 70;
        this.Height = 70;
        this.Image = "@gameui/image/chat/聊天入口.png";
        this.chatWindow = chatWindow;

        this.OnPointerClicked += (sender, e) => {
            chatWindow.Visible = true;
        };
    }
}

public class ChatWindow : Panel
{
    // 背景和基础UI
    public Image BackgroundImage = "@gameui/image/chat/聊天面板.png"u8;
    private Label titleLabel = null!;
    private Panel closeButton = null!;
    
    // 主内容区域
    private ChatRecord chatRecord = null!;
    private PlayerList playerList = null!;
    
    // 页签相关
    private Panel tabPanel = null!;
    private Panel chatRecordTab = null!;
    private Panel playerListTab = null!;
    
    // 输入相关UI
    private ChatInputPanel chatInputPanel = null!;
    private Button sendButton = null!;
    
    // 屏蔽功能
    private Panel blockAllPlayersOption = null!;
    private Panel blockAllButton = null!;
    
    // 状态
    private ChatMessageType currentChannel = ChatMessageType.All;
    
    public ChatWindow()
    {
        InitializeWindow();
        InitializeBasicUI();
        InitializeContentArea();
        InitializeTabs();
        InitializeInputArea();
        InitializeBlockUI();
        SetupEvents();
    }

    /// <summary>
    /// 初始化窗口基础属性（大小、位置、背景等）
    /// </summary>
    private void InitializeWindow()
    {
        this.Visible = false;
        this.RoutedEvents = RoutedEvents.None;
        this.Width = 761;
        this.Height = 1080;
        this.Padding = new Thickness(3, 0, 105, 0);
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        this.Image = BackgroundImage.Path;
        this.ZIndex = StandardUIType.Chat.ExpectedZIndex ?? 1000;
    }

    /// <summary>
    /// 初始化基础UI组件（标题、关闭按钮）
    /// </summary>
    private void InitializeBasicUI()
    {
        titleLabel = new Label(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(36, 26),
            Width = -1,
            Text = "聊天",
            FontSize = 36,
            TextColor = Color.FromArgb(alpha: 255, 212, 198, 157),
            Bold = true,
        };
        this.AddChild(titleLabel);

        closeButton = new Panel(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(-15, 28),
            Width = 50,
            Height = 50,
            Image = "@gameui/image/chat/关闭按钮.png",
        };
        closeButton.OnPointerClicked += (sender, e) => this.Visible = false;
        this.AddChild(closeButton);
    }

    /// <summary>
    /// 初始化主内容区域（聊天记录、玩家列表）
    /// </summary>
    private void InitializeContentArea()
    {
        chatRecord = new ChatRecord(){
            Margin = new Thickness(22, 132, 99, 136),
            WidthStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            HeightStretchRatio = 1.0f,
            HeightCompactRatio = 1.0f,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
        };
        this.AddChild(chatRecord);

        playerList = new PlayerList(){
            Margin = new Thickness(22, 132, 100, 136),
            WidthStretchRatio = 1.0f,
            HeightStretchRatio = 1.0f,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Visible = false,
        };
        this.AddChild(playerList);
        playerList.Update();
    }

    /// <summary>
    /// 初始化页签容器和页签按钮
    /// </summary>
    private void InitializeTabs()
    {
        tabPanel = new Panel(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(10, 0),
            FlowOrientation = Orientation.Vertical,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
            Width = 99,
            Margin = new Thickness(0, 100, 0, 0),
            HeightCompactRatio = 1.0f,
            HeightStretchRatio = 1.0f,
        };
        this.AddChild(tabPanel);

        CreateTabButtons();
    }

    /// <summary>
    /// 创建聊天记录和玩家列表页签按钮
    /// </summary>
    private void CreateTabButtons()
    {
        chatRecordTab = new Panel(){
            Width = 99,
            Height = 313,
            Image = "@gameui/image/chat/页签.png",
        };
        tabPanel.AddChild(chatRecordTab);

        var chatRecordLabel = new Label(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            FontSize = 42,
            TextColor = Color.FromArgb(alpha: 255, 212, 198, 157),
            Width = 60,
            Text = "聊天记录",
            Bold = true,
        };
        chatRecordTab.AddChild(chatRecordLabel);

        playerListTab = new Panel(){
            Width = 99,
            Height = 313,
        };
        tabPanel.AddChild(playerListTab);

        var playerListLabel = new Label(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            FontSize = 42,
            TextColor = Color.FromArgb(alpha: 255, 212, 198, 157),
            Width = 60,
            Text = "玩家列表",
            Bold = true,
        };
        playerListTab.AddChild(playerListLabel);
    }

    /// <summary>
    /// 初始化输入区域（输入面板、发送按钮）
    /// </summary>
    private void InitializeInputArea()
    {
        chatInputPanel = new ChatInputPanel();
        this.AddChild(chatInputPanel);

        sendButton = new Button(){
            Width = 168,
            Height = 54,
            Image = "@gameui/image/chat/按钮.png",
            Position = new UIPosition(-95, -22),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            KeyboardAccelerators = new List<KeyboardAccelerator>
            {
                new KeyboardAccelerator 
                { 
                    Key = VirtualKey.NumPadEnter,
                    // 默认会触发按钮的点击事件
                }
            }
        };
        this.AddChild(sendButton);

        var sendButtonLabel = new Label(){
            Text = "发送",
            FontSize = 32,
            TextColor = Color.FromArgb(255, 40, 52, 66),
            Bold = true,
            WidthStretchRatio = 1.0f,
            HeightStretchRatio = 1.0f,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
        };
        sendButton.AddChild(sendButtonLabel);
    }

    /// <summary>
    /// 初始化屏蔽功能UI（屏蔽所有玩家选项）
    /// </summary>
    private void InitializeBlockUI()
    {
        blockAllPlayersOption = new Panel(){
            FlowOrientation = Orientation.Horizontal,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Center,
            WidthStretchRatio = 1.0f,
            Margin = new Thickness(0, 0, 99, 32),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            // HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Visible = false,
        };
        this.AddChild(blockAllPlayersOption);

        blockAllButton = new Panel(){
            Width = 38,
            Height = 38,
            Margin = new Thickness(10, 0, 10, 0),
            Image = "@gameui/image/chat/按钮未按下.png",
        };
        blockAllPlayersOption.AddChild(blockAllButton);

        var blockAllLabel = new Label(){
            Text = "屏蔽所有玩家",
            FontSize = 32,
            TextColor = Color.FromArgb(255, 153, 153, 153),
        };
        blockAllPlayersOption.AddChild(blockAllLabel);
    }

    /// <summary>
    /// 设置所有UI组件的事件处理器
    /// </summary>
    private void SetupEvents()
    {
        // 页签事件
        chatRecordTab.OnPointerClicked += (sender, e) => CurrentTab = TabType.ChatRecord;
        playerListTab.OnPointerClicked += (sender, e) => CurrentTab = TabType.PlayerList;

        // 输入面板事件
        chatInputPanel.OnChannelChanged += (channel) => CurrentChannel = channel;
        chatInputPanel.OnSendMessage += (sender, e) => {
            _ = SendMessage();
        };

        // 发送按钮事件
        sendButton.OnPointerClicked += async (sender, e) => await SendMessage();

        // 屏蔽事件
        blockAllPlayersOption.OnPointerClicked += (sender, e) => {
            playerList.ToggleBlockAllPlayers();
            BlockAllEnabled = playerList.IsBlockAllPlayersEnabled;
        };

        // 聊天消息事件
        ChatMessageClient.OnChatMessageReceived += OnChatMessageReceived;
        ChatMessageClient.OnLocalSystemMessageSent += OnLocalSystemMessageSent;

        // 初始化状态
        playerList.Update();
    }


    private void OnChatMessageReceived(object? sender, ChatMessage message)
    {
        // 统一回音机制：所有消息类型都通过服务端回音显示
        // 包括私聊、公开、队伍消息，确保发送者也能看到自己的消息
        
        // 检查屏蔽
        if (playerList.CheckMessageBlocked(message))
            return;
            
        chatRecord.AddMessage(message.ToString());
    }

    private void OnLocalSystemMessageSent(object? sender, ChatMessage message)
    {
        chatRecord.AddMessage(message.ToString());
    }

    private TabType CurrentTab
    {
        set
        {
            switch (value)
            {
                case TabType.ChatRecord:
                    // 显示聊天记录页签
                    chatRecord.Visible = true;
                    playerList.Visible = false;
                    chatRecordTab.Image = "@gameui/image/chat/页签.png";
                    playerListTab.Image = "";
                    
                    // 显示输入相关UI
                    chatInputPanel.Visible = true;
                    sendButton.Visible = true;
                    blockAllPlayersOption.Visible = false;
                    break;

                case TabType.PlayerList:
                    // 显示玩家列表页签
                    chatRecord.Visible = false;
                    playerList.Visible = true;
                    chatRecordTab.Image = "";
                    playerListTab.Image = "@gameui/image/chat/页签.png";
                    
                    // 隐藏输入相关UI，显示屏蔽选项
                    chatInputPanel.Visible = false;
                    sendButton.Visible = false;
                    blockAllPlayersOption.Visible = true;
                    
                    playerList.Update();
                    break;
            }
        }
    }

    private ChatMessageType CurrentChannel
    {
        get => currentChannel;
        set
        {
            currentChannel = value;
            chatInputPanel.CurrentChannel = value;
        }
    }

    private async Task SendMessage()
    {
        // Game.Logger.LogInformation("发送消息{}",chatInputPanel.HasValidInput);
        if (!chatInputPanel.HasValidInput)
            return;

        var messageText = chatInputPanel.InputText.Trim();

        try
        {
            // 检查是否为私聊指令格式：/Player{x}....
            if (TryParsePrivateMessage(messageText, out int targetPlayerId, out string privateMessageContent))
            {
                // 私聊消息：统一使用服务端回音机制
                var targetPlayer = Player.GetById(targetPlayerId);
                if (targetPlayer != null)
                {
                    await ChatMessageClient.SendMessagePrivate(targetPlayer, privateMessageContent);
                    
                    // 私聊：等待服务端回音显示，与公开消息保持一致
                    chatInputPanel.ClearInput(); // 只清空输入框，消息显示等服务端回音
                }
                else
                {
                    return; // 发送失败，不清空输入框
                }
            }
            else
            {
                // 公开消息：等待服务端回音显示
                var channelType = CurrentChannel;
                switch (channelType)
                {
                    case ChatMessageType.All:
                        await ChatMessageClient.SendMessageAll(messageText);
                        break;
                    case ChatMessageType.Team:
                        await ChatMessageClient.SendMessageTeam(messageText);
                        break;
                }

                // 公开消息：不立即显示，等待服务端回音确保所有人看到相同的显示效果
                chatInputPanel.ClearInput(); // 只清空输入框，消息显示等服务端回音
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "发送消息失败");
        }
    }

    /// <summary>
    /// 解析私聊消息格式：/Player{x}....
    /// </summary>
    /// <param name="input">输入的消息文本</param>
    /// <param name="targetPlayerId">目标玩家ID</param>
    /// <param name="messageContent">消息内容</param>
    /// <returns>是否成功解析为私聊格式</returns>
    private bool TryParsePrivateMessage(string input, out int targetPlayerId, out string messageContent)
    {
        targetPlayerId = 0;
        messageContent = string.Empty;

        // 检查是否以 "/Player" 开头
        if (!input.StartsWith("/Player", StringComparison.OrdinalIgnoreCase))
            return false;

        // 查找玩家ID的结束位置
        int startIndex = 7; // "/Player".Length
        int endIndex = startIndex;
        
        // 查找数字部分的结束
        while (endIndex < input.Length && char.IsDigit(input[endIndex]))
        {
            endIndex++;
        }

        // 检查是否找到了有效的玩家ID
        if (endIndex == startIndex)
            return false;

        // 解析玩家ID
        string playerIdStr = input.Substring(startIndex, endIndex - startIndex);
        if (!int.TryParse(playerIdStr, out targetPlayerId))
            return false;

        // 提取消息内容（去掉 "/Player{x}" 部分）
        if (endIndex < input.Length)
        {
            messageContent = input.Substring(endIndex).Trim();
            // 如果消息内容为空，返回失败
            return !string.IsNullOrEmpty(messageContent);
        }

        return false;
    }

    /// <summary>
    /// 从输入面板获取当前频道类型
    /// </summary>
    private bool BlockAllEnabled
    {
        set
        {
            blockAllButton.Image = value ? 
                "@gameui/image/chat/按钮按下.png" : 
                "@gameui/image/chat/按钮未按下.png";
        }
    }
}


#endif