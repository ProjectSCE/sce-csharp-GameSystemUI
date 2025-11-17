
using GameCore.Extension;
using GameCore.ResourceType;
using GameData;
using GameUI.Brush;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.AbilitySystemUI.Advanced;
#endif

namespace GameSystemUI.AbilitySystemUI.Data;

[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlAbilityJoyStickGroup
{
    /// <summary>
    /// 最大显示技能数量，超过此数量的技能将不会显示在摇杆组中
    /// </summary>
    public int? MaxSkillCount { get; set; }
    
    /// <summary>
    /// 是否自动绑定快捷键到摇杆按钮
    /// </summary>
    public bool? AutoBindKey { get; set; }
    
    /// <summary>
    /// 普通技能摇杆按钮的大小（像素）
    /// </summary>
    public float? ButtonSize { get; set; }
    
    /// <summary>
    /// 中心技能摇杆按钮的大小（像素），通常比普通技能按钮更大
    /// </summary>
    public float? AttackButtonSize { get; set; }
    
    /// <summary>
    /// 环绕技能摇杆距离基准位置的最小距离（像素）
    /// 实际距离会根据技能数量动态调整：distance = MinAroundDistance + max(skillCount-4, 0) * ButtonSize/3.2
    /// </summary>
    public float? MinAroundDistance { get; set; }
    
    /// <summary>
    /// 环绕技能摇杆分布的总角度范围（度），技能将在此角度范围内均匀分布
    /// 例如：130度表示技能摇杆将分布在130度的扇形区域内
    /// </summary>
    public float? TotalAngleDelta { get; set; }
    
    /// <summary>
    /// 环绕技能摇杆分布的初始角度（度），决定第一个环绕技能的位置
    /// 0度为右侧，90度为上方，-90度为下方，180度为左侧
    /// </summary>
    public float? InitAngle { get; set; }
    
    /// <summary>
    /// 环绕技能摇杆的基准位置X坐标（像素），环绕技能将围绕此点分布
    /// </summary>
    public float? BaseX { get; set; }
    
    /// <summary>
    /// 环绕技能摇杆的基准位置Y坐标（像素），环绕技能将围绕此点分布
    /// </summary>
    public float? BaseY { get; set; }
    
    /// <summary>
    /// 攻击技能摇杆的X坐标位置（像素），通常为主要攻击技能的固定位置
    /// </summary>
    public float? AttackX { get; set; }
    
    /// <summary>
    /// 攻击技能摇杆的Y坐标位置（像素），通常为主要攻击技能的固定位置
    /// </summary>
    public float? AttackY { get; set; }
    
    /// <summary>
    /// 手动添加的子控件是否参与自动布局
    /// true: 手动添加的子控件会参与自动布局排版
    /// false: 手动添加的子控件保持原有位置，不参与自动布局
    /// </summary>
    public bool? AutoLayoutManualChildren { get; set; }

    /// <summary>
    /// 摇杆模板
    /// </summary>
    public IGameLink<GameDataControlAbilityJoyStick>? JoyStickTemplate { get; set; }
    
    /// <summary>
    /// 停止施法按钮模板
    /// </summary>
    public IGameLink<GameDataStopCastingButton>? StopCastingButtonTemplate { get; set; }
    
    /// <summary>
    /// 是否自动创建并绑定停止施法按钮
    /// </summary>
    public bool AutoCreateStopCastingButton { get; set; } = true;

    #if CLIENT
    public override Control CreateControl()
    {
        return new AbilityJoyStickGroup(Link);
    }

    public AbilityJoyStickGroup CreateAbilityJoyStickGroup()
    {
        return new AbilityJoyStickGroup(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为AbilityJoyStickGroup类型
        if (control is AbilityJoyStickGroup abilityJoyStickGroup)
        {
            // 应用布局配置（只有当属性不为null时才应用）
            if (MaxSkillCount != null)
                abilityJoyStickGroup.MaxSkillCount = MaxSkillCount.Value;
            if (AutoBindKey != null)
                abilityJoyStickGroup.AutoBindKey = AutoBindKey.Value;
            if (ButtonSize != null)
                abilityJoyStickGroup.ButtonSize = ButtonSize.Value;
            if (AttackButtonSize != null)
                abilityJoyStickGroup.AttackButtonSize = AttackButtonSize.Value;
            if (MinAroundDistance != null)
                abilityJoyStickGroup.MinAroundDistance = MinAroundDistance.Value;
            if (TotalAngleDelta != null)
                abilityJoyStickGroup.TotalAngleDelta = TotalAngleDelta.Value;
            if (InitAngle != null)
                abilityJoyStickGroup.InitAngle = InitAngle.Value;
            if (BaseX != null)
                abilityJoyStickGroup.BaseX = BaseX.Value;
            if (BaseY != null)
                abilityJoyStickGroup.BaseY = BaseY.Value;
            if (AttackX != null)
                abilityJoyStickGroup.AttackX = AttackX.Value;
            if (AttackY != null)
                abilityJoyStickGroup.AttackY = AttackY.Value;
            if (AutoLayoutManualChildren != null)
                abilityJoyStickGroup.AutoLayoutManualChildren = AutoLayoutManualChildren.Value;
            if (JoyStickTemplate != null)
                abilityJoyStickGroup.JoyStickTemplate = JoyStickTemplate;
            if (StopCastingButtonTemplate != null)
                abilityJoyStickGroup.StopCastingButtonTemplate = StopCastingButtonTemplate;
            abilityJoyStickGroup.AutoCreateStopCastingButton = AutoCreateStopCastingButton;
        }
    }
    #endif
}