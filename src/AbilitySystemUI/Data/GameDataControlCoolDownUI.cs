
using System;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
using GameCore.ResourceType;
#if CLIENT
using GameSystemUI.AbilitySystemUI.Advanced;
#endif


namespace GameSystemUI.AbilitySystemUI.Data;

/// <summary>
/// 冷却UI控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlCoolDownUI : GameDataControlPanel, IGameData<GameDataControlCoolDownUI>, IGameData
{
    /// <summary>
    /// 冷却进度条遮罩图片
    /// </summary>
    public Image? CooldownOverlay { get; set; }
    
    /// <summary>
    /// 冷却文本字体大小
    /// </summary>
    public int? CoolTextFontSize { get; set; }
    
    /// <summary>
    /// 冷却文本颜色
    /// </summary>
    public string? CoolTextColor { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new CoolDownUI(Link);
    }

    public CoolDownUI CreateCoolDownUI()
    {
        return new CoolDownUI(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为CoolDownUI类型
        if (control is CoolDownUI coolDownUI)
        {
            // 应用冷却UI配置
            if (CooldownOverlay != null)
                coolDownUI.CooldownOverlay = CooldownOverlay.Value;
                
            // TODO: 应用其他配置如字体大小、颜色等
        }
    }
    #endif
}

