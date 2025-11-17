using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif
using GameCore.ResourceType;
namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 页面按钮控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlButton>]
public partial class GameDataControlPageButton
{
    /// <summary>
    /// 激活状态图片
    /// </summary>
    public Image? ActiveImage { get; set; }
    
    /// <summary>
    /// 非激活状态图片
    /// </summary>
    public Image? InactiveImage { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new PageButton(Link);
    }

    public PageButton CreatePageButton()
    {
        return new PageButton(Link);
    }
    #endif
}