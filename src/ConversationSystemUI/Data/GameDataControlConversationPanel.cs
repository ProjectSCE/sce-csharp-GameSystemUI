using GameCore.BaseType;
using GameData;
using GameData.Interface;
using GameUI.Control;
using GameUI.Control.Data;
using GameUI.Struct;
#if CLIENT
using GameSystemUI.ConversationSystemUI.Advanced;
#endif

namespace GameSystemUI.ConversationSystemUI.Data;

/// <summary>
/// ConversationPanel 控件的 GameData 配置类
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlConversationPanel
{
    /// <summary>
    /// 背景图片
    /// </summary>
    public Image? BackgroundImage { get; set; } = "@gameui/image/conversation/背景.png"u8;
    
    /// <summary>
    /// 分割线图片
    /// </summary>
    public Image? SplitLineImage { get; set; } = "@gameui/image/conversation/分割线.png"u8;
    
    /// <summary>
    /// 跳过按钮图片
    /// </summary>
    public Image? SkipButtonImage { get; set; } = "@gameui/image/conversation/跳过.png"u8;
    
    /// <summary>
    /// 可完成指示器图片
    /// </summary>
    public Image? CanFinishIndicatorImage { get; set; } = "@gameui/image/conversation/可完成.png"u8;
    
    /// <summary>
    /// 是否启用打字机效果（默认关闭）
    /// </summary>
    public bool EnableTypewriter { get; set; } = false;
    
    /// <summary>
    /// 打字机速度：每帧显示的字符数（默认0.5）
    /// </summary>
    public float TypewriterSpeed { get; set; } = 0.5f;
    
    /// <summary>
    /// BubbleUI模板配置
    /// </summary>
    public IGameLink<GameDataControlBubbleUI>? BubbleUITemplate { get; set; }
    
    /// <summary>
    /// BubbleUI气泡方向（默认为 Right）
    /// </summary>
    public BubbleDirection BubbleDirection { get; set; } = BubbleDirection.Right;
    
    /// <summary>
    /// 选择项模板配置
    /// </summary>
    public IGameLink<GameDataControlConversationChoiceItem>? ChoiceItemTemplate { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new ConversationPanel(Link);
    }

    public ConversationPanel CreateConversationPanel()
    {
        return new ConversationPanel(Link);
    }
    #endif
}

