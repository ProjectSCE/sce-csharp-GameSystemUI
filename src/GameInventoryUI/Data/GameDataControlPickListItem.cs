
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif

namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 拾取列表项控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlPickListItem
{
    /// <summary>
    /// 物品宽度
    /// </summary>
    public int? ItemWidth { get; set; }
    
    /// <summary>
    /// 物品高度
    /// </summary>
    public int? ItemHeight { get; set; }
    
    /// <summary>
    /// 槽位大小
    /// </summary>
    public int? SlotSize { get; set; }
    
    /// <summary>
    /// 物品名称字体大小
    /// </summary>
    public int? NameFontSize { get; set; }
    
    /// <summary>
    /// 品质文字字体大小
    /// </summary>
    public int? QualityFontSize { get; set; }
    
    /// <summary>
    /// 分类文字字体大小
    /// </summary>
    public int? CategoryFontSize { get; set; }

    #if CLIENT
    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        if (control is PickListItem pickListItem)
        {
            if (ItemWidth.HasValue)
            {
                pickListItem.Width = ItemWidth.Value;
            }
            if (ItemHeight.HasValue)
            {
                pickListItem.Height = ItemHeight.Value;
            }
            // 其他属性可以在这里应用
        }
    }

    public override Control CreateControl()
    {
        return new PickListItem(Link);
    }

    public PickListItem CreatePickListItem()
    {
        return new PickListItem(Link);
    }
    #endif
}
