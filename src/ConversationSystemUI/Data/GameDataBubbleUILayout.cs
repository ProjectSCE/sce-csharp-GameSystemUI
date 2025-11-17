using GameCore.BaseType;
using GameData;
using GameUI.Struct;

namespace GameSystemUI.ConversationSystemUI.Data;

/// <summary>
/// 气泡UI布局配置 - 定义气泡的外观和布局
/// </summary>
[GameDataCategory]
public partial class GameDataBubbleUILayout
{
    /// <summary>
    /// 气泡背景图片
    /// </summary>
    public Image? BubbleImage { get; set; }
    
    /// <summary>
    /// UI 坐标偏移（X/Y 轴，屏幕坐标，不受镜头旋转影响）
    /// </summary>
    public UIPosition UIOffset { get; set; } = new UIPosition(0, 0);
    
    /// <summary>
    /// Z 轴偏移（世界坐标，影响深度/前后位置）
    /// </summary>
    public float OffsetZ { get; set; } = 0.0f;
    
    /// <summary>
    /// 每行文字数（用于计算气泡宽度：Width = CharsPerLine * FontSize + TextMargin.Left + TextMargin.Right）
    /// </summary>
    public int CharsPerLine { get; set; } = 15;
    
    /// <summary>
    /// 文本边距
    /// </summary>
    public Thickness TextMargin { get; set; } = new Thickness(26, 10, right: 15, bottom: 15);
    
    /// <summary>
    /// 九宫格切片边界（用于图片拉伸时保持边角不变形）
    /// </summary>
    public Thickness? SlicedEdges { get; set; }
    
    /// <summary>
    /// 绑点名称（气泡UI将跟随单位的这个绑点，默认为 "socket_root"）
    /// </summary>
    public string SocketName { get; set; } = "socket_root";
}

