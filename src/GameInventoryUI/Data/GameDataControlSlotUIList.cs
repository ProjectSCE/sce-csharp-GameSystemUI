
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif

namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 槽位列表UI控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlSlotListUI
{
    #if CLIENT
    public override Control CreateControl()
    {
        return new SlotListUI(Link);
    }

    public SlotListUI CreateSlotListUI()
    {
        return new SlotListUI(Link);
    }
    #endif
}
