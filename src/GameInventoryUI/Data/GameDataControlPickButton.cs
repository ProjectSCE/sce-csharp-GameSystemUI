
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif
namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 拾取按钮控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlPickButton : GameDataControlPanel, IGameData<GameDataControlPickButton>, IGameData
{
    /// <summary>
    /// 刷新周期（毫秒）
    /// </summary>
    public uint? Period { get; set; }
    
    /// <summary>
    /// PickList模板配置
    /// </summary>
    public IGameLink<GameDataControlPickList>? PickListTemplate { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new PickButton(Link);
    }

    public PickButton CreatePickButton()
    {
        return new PickButton(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        if (control is PickButton pickButton)
        {
            if (Period.HasValue)
            {
                Game.Logger.LogInformation("ApplyTo PickButton, period: {Period}", Period.Value);
                pickButton.period = Period.Value;
            }
        }
    }
    #endif
}

