
using GameCore.Extension;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.MoveJoystick.Advanced;
#endif
namespace GameSystemUI.MoveJoystick.Data;

/// <summary>
/// 移动摇杆的GameData配置类
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlMoveJoyStick
{
    /// <summary>
    /// 死区范围（像素），在此范围内摇杆不会触发移动
    /// </summary>
    public int? DeadBand { get; set; }
    
    /// <summary>
    /// 当绑定的单位变化时，是否停止移动
    /// </summary>
    public bool? StopMoveOnUnitChange { get; set; }
    
    /// <summary>
    /// 是否自动绑定到主控单位
    /// </summary>
    public bool? UseMainUnit { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new MoveJoyStick();
    }

    public override void ApplyTo(Control control)
    {
        if (control is not MoveJoyStick moveJoyStick)
        {
            Game.Logger.LogError($"GameDataControlMoveJoyStick can only be applied to MoveJoyStick, but got {control.GetType().Name}");
            return;
        }

        base.ApplyTo(control);

        // 应用移动摇杆特有的配置
        if (DeadBand.HasValue)
            moveJoyStick.DeadBand = DeadBand.Value;
            
        if (StopMoveOnUnitChange.HasValue)
            moveJoyStick.StopMoveOnUnitChange = StopMoveOnUnitChange.Value;
            
        if (UseMainUnit.HasValue)
            moveJoyStick.UseMainUnit = UseMainUnit.Value;
    }
    #endif
}

