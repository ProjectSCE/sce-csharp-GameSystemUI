
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

[GameDataNodeType<GameDataControl, GameDataControlButton>]
public partial class GameDataStopCastingButton
{
    /// <summary>
    /// 停止施法按钮图片
    /// </summary>
    public Image? ButtonImage { get; set; }
    
    /// <summary>
    /// 停止施法遮罩图片
    /// </summary>
    public Image? MaskImage { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new StopCastingButton(Link);
    }

    public StopCastingButton CreateStopCastingButton()
    {
        return new StopCastingButton(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 配置已在构造函数中应用，ApplyTo 用于动态修改属性
        if (control is StopCastingButton stopCastingButton)
        {
            // 动态应用图片配置（只有当属性不为null时才应用）
            if (ButtonImage != null)
                stopCastingButton.ButtonImage = ButtonImage.Value;
            if (MaskImage != null)
                stopCastingButton.MaskImage = MaskImage.Value;
        }
    }
    #endif
}

