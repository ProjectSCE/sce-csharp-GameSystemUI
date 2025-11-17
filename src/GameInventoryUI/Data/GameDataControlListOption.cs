using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif
namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 列表选项控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlButton>]
public partial class GameDataControlListOption
{
    #if CLIENT
    public override Control CreateControl()
    {
        return new ListOption(Link);
    }

    public ListOption CreateListOption()
    {
        return new ListOption(Link);
    }
    #endif
}
