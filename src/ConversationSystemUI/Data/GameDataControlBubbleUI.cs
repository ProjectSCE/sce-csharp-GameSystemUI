using GameCore.BaseType;
using GameData;
using GameData.Interface;
using GameUI.Control.Data;
using System.Collections.Generic;

namespace GameSystemUI.ConversationSystemUI.Data;

/// <summary>
/// BubbleUI组件的GameData配置类
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlBubbleUI
{
    /// <summary>
    /// 默认方向（当没有指定方向或找不到对应方向配置时使用）
    /// </summary>
    public BubbleDirection DefaultDirection { get; set; } = BubbleDirection.Down;
    
    /// <summary>
    /// 方向布局配置字典（方向 -> 布局配置）
    /// </summary>
    public Dictionary<BubbleDirection, IGameLink<GameDataBubbleUILayout>> DirectionLayouts { get; set; } 
        = new Dictionary<BubbleDirection, IGameLink<GameDataBubbleUILayout>>();
    
    /// <summary>
    /// Label字体大小
    /// </summary>
    public int FontSize { get; set; } = 24;
    
    /// <summary>
    /// 可完成指示器图片
    /// </summary>
    public Image? CanFinishIndicatorImage { get; set; }
}

