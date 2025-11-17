
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
using GameCore.ResourceType;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif
namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 技能槽位UI控件数据配置 - 继承自InventorySlotUI的配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlInventorySlotUI>]
public partial class GameDataControlAbilitySlotUI : GameDataControlInventorySlotUI, IGameData<GameDataControlAbilitySlotUI>, IGameData
{   
    /// <summary>
    /// AbilityJoyStick模板配置
    /// </summary>   
    public IGameLink<GameSystemUI.AbilitySystemUI.Data.GameDataControlAbilityJoyStick>? AbilityJoyStickTemplate { get; set; }
    
    #if CLIENT
    public override Control CreateControl()
    {
        return new AbilitySlotUI(Link);
    }

    public AbilitySlotUI CreateAbilitySlotUI()
    {
        return new AbilitySlotUI(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
    }
    #endif
}
