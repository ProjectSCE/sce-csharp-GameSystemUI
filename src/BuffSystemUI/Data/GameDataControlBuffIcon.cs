using System.Drawing;
#if CLIENT
using GameUI.Control;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameSystemUI.BuffSystemUI.Advanced;
using GameData;
#endif

namespace GameSystemUI.BuffSystemUI.Data;

/// <summary>
/// BuffIcon控件数据配置
/// 双端通用的数编配置
/// </summary>
public partial class GameDataControlBuffIcon
{

    /// <summary>
    /// Buff图标路径
    /// </summary>
    public string BuffIcon { get; set; } = "@gameui/image/buff/buff_1.png";

    /// <summary>
    /// 正面极性外框颜色
    /// </summary>
    public Color BuffBackgroundPositiveColor { get; set; } = Color.FromArgb(52, 180, 31);

    /// <summary>
    /// 负面极性外框颜色
    /// </summary>
    public Color BuffBackgroundNegativeColor { get; set; } = Color.FromArgb(231, 67, 57);

    /// <summary>
    /// 中性极性外框颜色
    /// </summary>
    public Color BuffBackgroundNeutralColor { get; set; } = Color.FromArgb(154, 154, 154);

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
    /// 字体大小
    /// </summary>
    public int FontSize { get; set; } = 24;

    /// <summary>
    /// 正面极性CD序列帧类型
    /// </summary>
    public ProgressType BuffPositiveProgressType { get; set; } = ProgressType.Clockwise;

    /// <summary>
    /// 负面极性CD序列帧类型
    /// </summary>
    public ProgressType BuffNegativeProgressType { get; set; } = ProgressType.Clockwise;

    /// <summary>
    /// 中性极性CD序列帧类型
    /// </summary>
    public ProgressType BuffNeutralProgressType { get; set; } = ProgressType.Clockwise;
}

#if CLIENT

/// <summary>
/// BuffIcon控件UI创建部分 - 仅客户端
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlBuffIcon
{
    public override Control CreateControl()
    {
        return new BuffIcon(Link);
    }
}
#endif

