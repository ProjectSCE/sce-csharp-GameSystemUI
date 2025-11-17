using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;  
#endif

namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 背包入口按钮控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlInventoryUIEntrance : GameDataControlPanel, IGameData<GameDataControlInventoryUIEntrance>, IGameData
{
    #if CLIENT
    public InventoryUIEntrance CreateInventoryUIEntrance(InventoryUI inventoryUI)
    {
        return new InventoryUIEntrance(Link, inventoryUI);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        if (control is InventoryUIEntrance entrance)
        {
        }
    }
    #endif
}
