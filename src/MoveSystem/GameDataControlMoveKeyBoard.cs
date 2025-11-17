
using GameCore.Extension;
using GameCore.ResourceType;
using GameData;
using GameUI.Brush;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.MoveKeyBoard.Advanced;
#endif
namespace GameSystemUI.MoveKeyBoard.Data;

[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlMoveKeyBoard
{
    /// <summary>
    /// 是否绑定主控单位模式
    /// </summary>
    public bool? UseMainUnit { get; set; }
    
    /// <summary>
    /// 当绑定的单位变化时，是否停止移动
    /// </summary>
    public bool? StopMoveOnUnitChange { get; set; }
    
    
    /// <summary>
    /// 手动添加的子控件是否参与自动布局
    /// true: 手动添加的子控件会参与自动布局排版
    /// false: 手动添加的子控件保持原有位置，不参与自动布局
    /// </summary>
    public bool? AutoLayoutManualChildren { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new Advanced.MoveKeyBoard(Link);
    }

    public Advanced.MoveKeyBoard CreateMoveKeyBoard()
    {
        return new Advanced.MoveKeyBoard(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为AbilityJoyStickGroup类型
        if (control is Advanced.MoveKeyBoard moveKeyBoard)
        {
            // 应用布局配置（只有当属性不为null时才应用）
            if (UseMainUnit != null)
            {
                moveKeyBoard.UseMainUnit = UseMainUnit.Value;
            }
            
            if (StopMoveOnUnitChange != null)
            {
                moveKeyBoard.StopMoveOnUnitChange = StopMoveOnUnitChange.Value;
            }
        }
    }
    #endif
}