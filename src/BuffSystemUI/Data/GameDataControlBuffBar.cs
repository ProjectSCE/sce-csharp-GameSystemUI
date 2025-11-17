#if CLIENT
using GameUI.Control;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameSystemUI.BuffSystemUI.Advanced;
using GameData;
#endif

namespace GameSystemUI.BuffSystemUI.Data;

/// <summary>
/// BuffBar控件数据配置
/// 双端通用的数编配置
/// </summary>
public partial class GameDataControlBuffBar
{

    /// <summary>
    /// Buff宽度
    /// </summary>
    public int BuffWidth { get; set; } = 64;

    /// <summary>
    /// Buff高度
    /// </summary>
    public int BuffHeight { get; set; } = 64;

    /// <summary>
    /// Buff间隔
    /// </summary>
    public int BuffMargin { get; set; } = 7;

    /// <summary>
    /// Buff分类过滤：符合该字段的Buff分类才会显示在这个控件中
    /// </summary>
    public BuffCategoryFilter BuffCategoryFilter { get; set; } = BuffCategoryFilter.All;

    /// <summary>
    /// Buff极性过滤：可选择显示的极性类型
    /// </summary>
    public BuffPolarityFilter BuffPolarityFilter { get; set; } = BuffPolarityFilter.All;
}

#if CLIENT

/// <summary>
/// BuffBar控件UI创建部分 - 仅客户端
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlBuffBar
{
    public override Control CreateControl()
    {
        return new BuffBar(Link);
    }
}
#endif

