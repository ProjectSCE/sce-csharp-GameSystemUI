
using System;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.AbilitySystemUI.Advanced;
#endif
using GameCore.ResourceType;

namespace GameSystemUI.AbilitySystemUI.Data;

/// <summary>
/// 充能冷却UI控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlCoolDownChargeUI : GameDataControlPanel, IGameData<GameDataControlCoolDownChargeUI>, IGameData
{
    /// <summary>
    /// 背景图片
    /// </summary>
    public Image? BackgroundImage { get; set; }
    
    /// <summary>
    /// 充能进度条图片
    /// </summary>
    public Image? ChargeProgressImage { get; set; }
    
    /// <summary>
    /// 层数文本字体大小
    /// </summary>
    public int? StackLabelFontSize { get; set; }
    
    /// <summary>
    /// 层数文本颜色
    /// </summary>
    public string? StackLabelColor { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new CoolDownChargeUI(Link);
    }

    public CoolDownChargeUI CreateCoolDownChargeUI()
    {
        return new CoolDownChargeUI(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为CoolDownChargeUI类型
        if (control is CoolDownChargeUI coolDownChargeUI)
        {
            // 应用充能冷却UI配置
            if (BackgroundImage != null)
                coolDownChargeUI.BackgroundImage = BackgroundImage.Value;
                
            if (ChargeProgressImage != null)
                coolDownChargeUI.ChargeProgressImage = ChargeProgressImage.Value;
                
            // TODO: 应用其他配置如字体大小、颜色等
        }
    }
    #endif
}
