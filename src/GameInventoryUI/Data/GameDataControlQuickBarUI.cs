
using GameData;
using GameUI.Brush;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.GameInventoryUI.Advanced;
#endif
using GameUI.Struct;
using GameCore.ResourceType;

namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 快捷栏UI控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlInventoryUI>]
public partial class GameDataControlQuickBarUI
{
    
    /// <summary>
    /// 模式切换按钮图片（锁定状态）
    /// </summary>
    public Image? ModeButtonLocked { get; set; }
    
    /// <summary>
    /// 模式切换按钮图片（解锁状态）  
    /// </summary>
    public Image? ModeButtonUnlocked { get; set; }
    
    /// <summary>
    /// 左移按钮图片
    /// </summary>
    public Image? LeftButtonImage { get; set; }
    
    /// <summary>
    /// 右移按钮图片
    /// </summary>
    public Image? RightButtonImage { get; set; }
    
    /// <summary>
    /// 快捷栏背景图片
    /// </summary>
    public Image? BackgroundImage { get; set; }

    public IGameLink<GameDataControlAbilitySlotUI>? SlotTemplate { get; set; }
    #if CLIENT
    public override Control CreateControl()
    {
        return new QuickBarUI(Link);
    }

    public QuickBarUI CreateQuickBarUI()
    {
        return new QuickBarUI(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为QuickBarUI类型
        if (control is QuickBarUI quickBarUI)
        {
            // 应用配置（只有当属性不为null时才应用）
            if (ModeButtonLocked != null)
                quickBarUI.ModeButtonLocked = ModeButtonLocked.Value;
                
            if (ModeButtonUnlocked != null)
                quickBarUI.ModeButtonUnlocked = ModeButtonUnlocked.Value;
                
            if (LeftButtonImage != null)
                quickBarUI.LeftButtonImage = LeftButtonImage.Value;
                
            if (RightButtonImage != null)
                quickBarUI.RightButtonImage = RightButtonImage.Value;
                
            if (BackgroundImage != null)
                quickBarUI.BackgroundImage = BackgroundImage.Value;
        }
    }
    #endif
}