using GameUI.Struct;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.ConversationSystemUI.Advanced;
using GameData;
#endif

namespace GameSystemUI.ConversationSystemUI.Data;



/// <summary>
/// ConversationChoiceItem控件UI创建部分 - 仅客户端
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlConversationChoiceItem
{
    /// <summary>
    /// 默认图标
    /// </summary>
    public Icon Icon { get; set; } = "@gameui/image/conversation/聊天图标.png"u8; 
    #if CLIENT
    public override Control CreateControl()
    {
        return new ConversationChoiceItem(Link);
    }

    public ConversationChoiceItem CreateConversationChoiceItem()
    {
        return new ConversationChoiceItem(Link);
    }

    #endif
}

