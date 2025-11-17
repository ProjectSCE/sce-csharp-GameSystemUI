
using GameCore.BaseType;
using GameCore.Extension;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.GameInventoryUI.Advanced;
#endif
using GameCore.Struct;

namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 默认背包UI控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlInventoryUI>]
public partial class GameDataControlDefaultInventoryUI
{   
    /// <summary>
    /// 格子模板配置
    /// </summary>
    public IGameLink<GameDataControlInventorySlotUI>? SlotTemplate { get; set; }
    
    /// <summary>
    /// 物品分类过滤器，只显示指定分类的物品
    /// </summary>
    public List<ItemCategory> FilterCategories { get; set; } = [];
    
    /// <summary>
    /// 背包界面背景图片
    /// </summary>
    public Image? BackgroundImage { get; set; }
    
    /// <summary>
    /// 关闭按钮图片
    /// </summary>
    public Image? CloseButtonImage { get; set; }
    
    /// <summary>
    /// 丢弃按钮激活状态图片
    /// </summary>
    public Image? DropButtonActiveImage { get; set; }
    
    /// <summary>
    /// 丢弃按钮非激活状态图片
    /// </summary>
    public Image? DropButtonInactiveImage { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new DefaultInventoryUI(Link);
    }

    public DefaultInventoryUI CreateDefaultInventoryUI()
    {
        return new DefaultInventoryUI(Link);
    }
    #endif
}