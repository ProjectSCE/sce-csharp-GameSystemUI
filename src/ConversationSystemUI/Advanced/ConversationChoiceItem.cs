#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Control.Data;
using GameUI.ConversationSystem;
using GameUI.Struct;
using GameUI.Control.Enum;
using GameData.Interface;
using System.Drawing;
using GameSystemUI.ConversationSystemUI.Data;

namespace GameSystemUI.ConversationSystemUI.Advanced;

/// <summary>
/// 对话选择项控件
/// </summary>
public class ConversationChoiceItem : Panel
{
    protected Panel IconPanel { get; private set; } = null!;
    protected Panel SeparatorPanel { get; private set; } = null!;
    protected Label TextLabel { get; private set; } = null!;
    
    private ConversationChoiceInfo? _choiceInfo;
    
    public ConversationChoiceInfo? ChoiceInfo {
        get => _choiceInfo;
        set
        {
            _choiceInfo = value;
            if(value is not null)
            {
                TextLabel.Text = value.Text;
                
                // 根据IsEnabled状态设置透明度
                if (value.IsEnabled)
                {
                    this.Opacity = 1.0f;
                }
                else
                {
                    this.Opacity = 0.5f;
                }
            }
        }
    }

    public ConversationChoiceItem() : base()
    {
        Initialize();
    }

    public ConversationChoiceItem(IGameLink<GameDataControlConversationChoiceItem> link) : base(link)
    {
        Initialize();
        
        // 从GameData读取Icon配置
        var data = link.Data;
        if (data != null && !string.IsNullOrEmpty(data.Icon.Path))
        {
            SetIcon(data.Icon.Path);
        }
    }

    private void Initialize()
    {
        // 初始化子控件
        IconPanel = new Panel();
        SeparatorPanel = new Panel();
        TextLabel = new Label();

        // 应用默认配置
        InitializeUI();

        // 添加子控件
        this.AddChild(IconPanel);
        this.AddChild(SeparatorPanel);
        this.AddChild(TextLabel);
    }

    protected virtual void InitializeUI()
    {
        // 容器配置
        this.FlowOrientation = GameUI.Enum.Orientation.Horizontal;
        this.Image = "@gameui/image/conversation/选择.png";

        // 图标面板配置
        IconPanel.Width = 48;
        IconPanel.Height = 48;
        IconPanel.Margin = new Thickness(left: 12, top: 9, right: 12, bottom: 9);

        // 分割线配置
        SeparatorPanel.Width = 2;
        SeparatorPanel.HeightStretchRatio = 1.0f;
        SeparatorPanel.HeightCompactRatio = 1.0f;
        SeparatorPanel.Background = Color.FromArgb(255, 142, green: 145, 149);
        SeparatorPanel.Margin = new Thickness(left: 0, top: 20, right: 0, bottom: 20);

        // 文本标签配置
        TextLabel.WidthStretchRatio = 1.0f;
        TextLabel.WidthCompactRatio = 1.0f;
        TextLabel.HeightCompactRatio = 1.0f;
        TextLabel.HeightStretchRatio = 1.0f;
        TextLabel.TextColor = Color.White;
        TextLabel.FontSize = 30;
        TextLabel.HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left;
        TextLabel.Margin = new Thickness(left: 12, top: 0, right: 0, bottom: 0);

        this.RoutedEvents = RoutedEvents.None;
    }

    /// <summary>
    /// 设置选择项图标（可选）
    /// </summary>
    public virtual void SetIcon(string? iconPath)
    {
        if (!string.IsNullOrEmpty(iconPath))
        {
            IconPanel.Image = iconPath;
        }
    }
}
#endif