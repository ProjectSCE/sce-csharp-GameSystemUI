
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
public partial class GameDataControlAbilityJoyStick
{
    // public string Name => throw new NotImplementedException();
    
    /// <summary>
    /// 激活状态技能框图片
    /// </summary>
    public Image? AbilityActiveFrame { get; set; }
    
    /// <summary>
    /// 默认背景技能框图片
    /// </summary>
    public Image? AbilityBackgroundFrame { get; set; }
    
    /// <summary>
    /// 禁用状态技能框图片
    /// </summary>
    public Image? AbilityDisabledFrame { get; set; }

    /// <summary>
    /// 施法摇杆的默认背景图片
    /// </summary>
    public Image? CastingJoyStickDefaultBackground { get; set; }
    
    /// <summary>
    /// 施法摇杆的禁用状态背景图片
    /// </summary>
    public Image? CastingJoyStickDefaultBackgroundDisable { get; set; }

    /// <summary>
    /// 冷却UI模板
    /// </summary>
    public IGameLink<GameDataControlCoolDownUI>? CoolDownUITemplate { get; set; }
    
    /// <summary>
    /// 充能冷却UI模板
    /// </summary>
    public IGameLink<GameDataControlCoolDownChargeUI>? CoolDownChargeUITemplate { get; set; }
    
    /// <summary>
    /// 施法摇杆的冷却时施法提前量（毫秒）
    /// </summary>
    public double? CooldownThresholdMs { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new AbilityJoyStick(Link);
    }

    public AbilityJoyStick CreateAbilityJoyStick()
    {
        return new AbilityJoyStick(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为AbilityJoyStick类型
        if (control is AbilityJoyStick abilityJoyStick)
        {
            // 应用图片配置（只有当属性不为null时才应用）
            if (AbilityActiveFrame != null)
                abilityJoyStick.AbilityActiveFrame = AbilityActiveFrame.Value;
            if (AbilityBackgroundFrame != null)
                abilityJoyStick.AbilityBackgroundFrame = AbilityBackgroundFrame.Value;
            if (AbilityDisabledFrame != null)
                abilityJoyStick.AbilityDisabledFrame = AbilityDisabledFrame.Value;
            
            // 应用施法摇杆图片配置
            if (CastingJoyStickDefaultBackground != null)
                abilityJoyStick.CastingJoyStickDefaultBackground = CastingJoyStickDefaultBackground.Value;
            if (CastingJoyStickDefaultBackgroundDisable != null)
                abilityJoyStick.CastingJoyStickDefaultBackgroundDisable = CastingJoyStickDefaultBackgroundDisable.Value;
            
            // 冷却UI配置现在通过模板在构造函数中应用，无需在此处处理
            
            // 应用施法摇杆配置
            if (CooldownThresholdMs.HasValue)
                abilityJoyStick.CastingJoyStickCooldownThreshold = TimeSpan.FromMilliseconds(CooldownThresholdMs.Value);
        }
    }
    #endif
}
