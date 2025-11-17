#if CLIENT
using System.Drawing;
using GameCore.DisplayInfo;
using GameCore.Extension;
using GameCore.PlayerAndUsers.Enum;
using GameCore.ResourceType;
using GameData;
using GameData.Interface;
using GameUI.Brush;
using GameUI.Control;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Enum;
using GameUI.Struct;

namespace GameSystemUI.ChatSystem;
[GameObject<GameDataPlayerItem>]
public partial class PlayerItem : Panel, IGameObject<GameDataPlayerItem>, IGameObject
{
    public new static readonly IGameLink<GameDataPlayerItem> DefaultTemplate = new GameLink<GameDataControl, GameDataPlayerItem>(typeof(PlayerItem).GetHashCode());

    // Relation-based resources
    private static readonly Image AllyAvatarFrame = "@gameui/image/chat/友方头像框.png"u8;
    private static readonly Image EnemyAvatarFrame = "@gameui/image/chat/敌方头像框.png"u8;
    private static readonly Image NeutralAvatarFrame = "@gameui/image/chat/中立头像框.png"u8;

    internal static Image ResolveAvatarFrame(PlayerRelationShip relation)
    {
        return relation switch
        {
            PlayerRelationShip.Ally => AllyAvatarFrame,
            PlayerRelationShip.Enemy => EnemyAvatarFrame,
            PlayerRelationShip.Neutral => NeutralAvatarFrame,
            _ => AllyAvatarFrame,
        };
    }

    // Resources (optional to set from outside)
    public Image AvatarFrame 
    { 
        get => avatarFrame; 
        set 
        { 
            avatarFrame = value; 
            avatarFramePanel.Image = value.Path; 
        } 
    }
    
    public Image Avatar 
    { 
        get => avatar; 
        set 
        { 
            avatar = value; 
            avatarPanel.Image = value.Path; 
        } 
    }
    
    public Image NameplateBackground 
    { 
        get => nameplateBackground; 
        set 
        { 
            nameplateBackground = value; 
            nameplateBackgroundPanel.Image = value.Path; 
        } 
    }

    public Image MuteButtonImage
    {
        get => muteButtonImage;
        set
        {
            muteButtonImage = value;
            muteButton.Image = value.Path;
        }
    }

    // Data bindings
    public string? TeamNumber 
    { 
        get => teamNumber; 
        set 
        { 
            teamNumber = value; 
            teamNumberLabel.Text = value?.ToString(); 
        } 
    }
    
    public string? Nickname 
    { 
        get => nicknameLabel.Text; 
        set => nicknameLabel.Text = value; 
    }
    
    public string? UnitDisplayName 
    { 
        get => unitNameLabel.Text; 
        set => unitNameLabel.Text = value; 
    }
    
    public bool ShowDivider 
    { 
        get => divider.Visible; 
        set => divider.Visible = value; 
    }

    public Action<Panel>? OnMuteButtonClick;

    private Image avatarFrame = new("@gameui/image/chat/友方头像框.png");
    private Image avatar = new("@gameui/image/chat/默认头像.png");
    private Image nameplateBackground = new("@gameui/image/chat/姓名板.png");
    private Image muteButtonImage = new("@gameui/image/chat/聊天显示中.png");
    private string? teamNumber;

    private Panel avatarFramePanel = null!;
    private Panel avatarPanel = null!;
    private Panel nameplateBackgroundPanel = null!;
    private Label teamNumberLabel = null!;
    private Label nicknameLabel = null!;
    private Label unitNameLabel = null!;
    private Panel muteButton = null!;
    private Panel divider = null!;

    public PlayerItem()
        : this(DefaultTemplate)
    {
    }

    public PlayerItem(IGameLink<GameDataPlayerItem> link)
        : base(link)
    {
        InitializeControls();
    }

    private void InitializeControls()
    {
        // Root panel, corresponds to "友方玩家信息"
        this.WidthCompactRatio = 1.0f; // grow_width = 1
        this.WidthStretchRatio = 1.0f;
        this.Height = 125;              // height = 115

        // 姓名板背景
        nameplateBackgroundPanel = new Panel
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            // Position = new UIPosition(80, 0),
            Width = 339,
            Height = 38,
            Image = nameplateBackground.Path,
        };
        this.AddChild(nameplateBackgroundPanel);

        // 友方玩家昵称
        nicknameLabel = new Label
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Position = new UIPosition(45, 0), 
            Width = 287,
            Height = 38,
            FontSize = 24,
            TextColor = Color.FromArgb(255, 112, 227, 131), // #70E383
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Center,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
        };
        nameplateBackgroundPanel.AddChild(nicknameLabel);

        // 头像框
        avatarFramePanel = new Panel
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(27, 18), // relative = {27,18}
            Width = 74,
            Height = 74,
            Image = avatarFrame.Path,
        };
        this.AddChild(avatarFramePanel);

        // 友方主控单位头像（圆角70 -> 半径35）
        avatarPanel = new Panel
        {
            Width = 70,
            Height = 70,
            CornerRadius = 35,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
        };
        avatarFramePanel.AddChild(avatarPanel);

        // 友方玩家队伍号
        teamNumberLabel = new Label
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(27, 98), // relative = {27,90}
            Width = 74,
            Height = 20,
            FontSize = 20,
            TextColor = Color.FromArgb(255, 179, 181, 188), // #B3B5BC
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Bottom,
        };
        this.AddChild(teamNumberLabel);

        // 友方主控单位名
        unitNameLabel = new Label
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Position = new UIPosition(0, 30), 
            Width = 252,
            FontSize = 22,
            TextColor = Color.FromArgb(255, 179, 181, 188), // #B3B5BC
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
        };
        this.AddChild(unitNameLabel);

        // 友方屏蔽按钮
        muteButton = new Panel
        {
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right, // col_self = 'center'
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,      // row_self = 'end'
            // Position = new UIPosition(-25, 0),
            Width = 70,
            Height = 70,
            Image = muteButtonImage.Path,
        };
        muteButton.OnPointerClicked += (sender, e) =>
        {
            OnMuteButtonClick?.Invoke(muteButton);
        };
        this.AddChild(muteButton);

        // 分隔线
        divider = new Panel
        {
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            Height = 1,
            Margin = new Thickness(10, 0, 10, 0),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.FromArgb(255, 76, 76, 81)), // #4C4C51
        };
        this.AddChild(divider);
    }

    public void UpdateBlockStatus(bool isBlocked)
    {
        MuteButtonImage = isBlocked ? 
            new("@gameui/image/chat/聊天屏蔽中.png") : 
            new("@gameui/image/chat/聊天显示中.png");
    }
}

public class PlayerListTitle : Panel
{
    private Label title;
    public string? Title
    {
        get => title.Text;
        set => title.Text = value;
    }
    public PlayerListTitle()
    {
        this.WidthCompactRatio = 1.0f;
        this.WidthStretchRatio = 1.0f;
        this.Height = 46;
        this.Background = new SolidColorBrush(Color.FromArgb(145, 0, 0, 0));
        title = new Label
        {
            Text = "友方",
            TextColor = Color.FromArgb(255, 111, 114, 121),
            FontSize = 32,
        };
        this.AddChild(title);
    }
}

public class PlayerList : PanelScrollable
{
    // 盟友
    private PlayerListTitle allyTitle;
    private Panel allyList;
    // 敌方
    private PlayerListTitle enemyTitle;
    private Panel enemyList;
    // 中立
    private PlayerListTitle neutralTitle;
    private Panel neutralList;
    private Dictionary<Player, bool> muteStatus = new();
    private HashSet<int> blockedPlayers = new();
    private bool blockAllPlayers = false;
    public bool IsBlockAllPlayersEnabled => blockAllPlayers;
    
    public PlayerList()
    {
        this.ScrollEnabled = true;
        this.FlowOrientation = Orientation.Vertical;
        this.VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top;
        this.ScrollOrientation = Orientation.Vertical;
        this.ScrollBarSize = 10;

        // 盟友
        allyTitle = new PlayerListTitle(){
            Title = "友方",
        };
        this.AddChild(allyTitle);
        allyList = new Panel(){
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            ItemTemplate = ScopeData.Control.DefaultPlayerItem,
            FlowOrientation = Orientation.Vertical,
        };
        allyList.OnChildPostInitialization += OnChildPostInitializationInternal;
        this.AddChild(allyList);
        
        // 敌方
        enemyTitle = new PlayerListTitle(){
            Title = "敌方",
        };
        this.AddChild(enemyTitle);
        enemyList = new Panel(){
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            ItemTemplate = ScopeData.Control.DefaultPlayerItem,
            FlowOrientation = Orientation.Vertical,
        };
        enemyList.OnChildPostInitialization += OnChildPostInitializationInternal;
        this.AddChild(enemyList);

        // 中立     
        neutralTitle = new PlayerListTitle(){
            Title = "中立",
        };
        this.AddChild(neutralTitle);
        neutralList = new Panel(){
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            ItemTemplate = ScopeData.Control.DefaultPlayerItem,
            FlowOrientation = Orientation.Vertical,
        };
        neutralList.OnChildPostInitialization += OnChildPostInitializationInternal;
        this.AddChild(neutralList);
    }

    private void OnChildPostInitializationInternal(object? sender, ApplyControlTemplateEventArgs e)
    {
        var item = e.Control as PlayerItem;
        if(item == null)
        {
            return;
        }
        var player = item.DataContext as Player;
        if(player == null)
        {
            item.Visible = false;
            return;
        }
        // Relation-specific visuals
        var local = Player.LocalPlayer;
        var relation = local.GetRelationShip(player);
        item.AvatarFrame = PlayerItem.ResolveAvatarFrame(relation);
        item.TeamNumber = $"队伍{player.Team.Id}";
        item.Nickname = player.ToString();
        // 优先使用DisplayName，提供更好的显示名称
        item.UnitDisplayName = player.MainUnit?.Cache != null ? 
            (((IDisplayInfo)player.MainUnit.Cache).DisplayName ?? player.MainUnit.Cache.Name ?? "") : 
            "无主控单位";
        item.OnMuteButtonClick = (muteButton) =>
        {
            TogglePlayerBlock(player.Id);
            var isBlocked = IsPlayerBlocked(player.Id);
            item.UpdateBlockStatus(isBlocked);
        };
        
        // 初始化按钮状态
        var isBlocked = IsPlayerBlocked(player.Id);
        item.UpdateBlockStatus(isBlocked);
    }

    public void Update()
    {
        List<Player> allis = [];
        List<Player> enemies = [];
        List<Player> neutrals = [];
        var localPlayer = Player.LocalPlayer;
        foreach (var player in Player.AllPlayers)
        {
            if(!muteStatus.ContainsKey(player))
            {
                muteStatus[player] = false;
            }
          
            switch(localPlayer.GetRelationShip(player))
            {
            case PlayerRelationShip.Ally:
                allis.Add(player);
                break;
            case PlayerRelationShip.Enemy:
                enemies.Add(player);
                break;
            case PlayerRelationShip.Neutral:
                neutrals.Add(player);
                break;
            case PlayerRelationShip.Player:
                break;
            }
        }
        allyList.ItemsSource = allis;
        allyList.GenerateChildren();
        enemyList.ItemsSource = enemies;
        enemyList.GenerateChildren();
        neutralList.ItemsSource = neutrals;
        neutralList.GenerateChildren();
    }

    private void TogglePlayerBlock(int playerId)
    {
        if (blockedPlayers.Contains(playerId))
            blockedPlayers.Remove(playerId);
        else
            blockedPlayers.Add(playerId);
    }



    public bool IsPlayerBlocked(int playerId)
    {
        // 如果开启了"屏蔽所有玩家"，则所有玩家都被屏蔽
        if (blockAllPlayers)
            return true;
            
        // 否则检查是否在单独屏蔽列表中
        return blockedPlayers.Contains(playerId);
    }

    public void ToggleBlockAllPlayers()
    {
        blockAllPlayers = !blockAllPlayers;
        
        if (!blockAllPlayers)
        {
            // 关闭全体屏蔽时，清空所有单独屏蔽的玩家
            blockedPlayers.Clear();
        }
        
        // 更新所有玩家项的按钮状态
        UpdateAllPlayerItemsBlockStatus();
    }

    private void UpdateAllPlayerItemsBlockStatus()
    {
        // 更新友方玩家列表
        UpdateListItemsBlockStatus(allyList);
        
        // 更新敌方玩家列表
        UpdateListItemsBlockStatus(enemyList);
        
        // 更新中立玩家列表
        UpdateListItemsBlockStatus(neutralList);
    }

    private void UpdateListItemsBlockStatus(Panel playerListPanel)
    {
        if (playerListPanel.Children == null) return;
        
        foreach (var child in playerListPanel.Children)
        {
            if (child is PlayerItem playerItem && playerItem.DataContext is Player player)
            {
                var isBlocked = IsPlayerBlocked(player.Id);
                playerItem.UpdateBlockStatus(isBlocked);
            }
        }
    }

    public bool CheckMessageBlocked(ChatMessage message)
    {
        // 屏蔽所有玩家
        if (blockAllPlayers)
            return true;
            
        // 屏蔽特定玩家
        if (message.SenderID.HasValue && blockedPlayers.Contains(message.SenderID.Value))
            return true;
            
        return false;
    }
}

#endif

