#if CLIENT
using System;
using System.Threading.Tasks;
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameUI.Control.Primitive;
using GameUI.Control.Data;
using GameUI.Enum;
using GameUI.Device;
using GameData;
using GameData.Interface;
using GameData.Extension;
using Events;
using GameSystemUI.ConversationSystemUI.Data;
using GameUI.Struct;
using System.Diagnostics;
using GameCore.GameSystem.Enum;
using GameUI.Control.Extensions;
using System.Drawing;
using GameCore.ActorSystem;

namespace GameSystemUI.ConversationSystemUI.Advanced;

/// <summary>
/// 气泡UI组件 - 跟随单位显示的UI气泡
/// </summary>
/// <remarks>
/// <para>主要功能：</para>
/// <list type="bullet">
/// <item><description>实现IThinker接口，每帧更新UI位置</description></item>
/// <item><description>可以绑定单位，自动跟随单位位置</description></item>
/// <item><description>使用世界坐标到UI坐标的转换</description></item>
/// <item><description>包含Label子控件用于显示文本</description></item>
/// <item><description>支持打字机效果</description></item>
/// <item><description>包含可完成指示器（canFinishPanel）</description></item>
/// </list>
/// <para>偏移量说明：</para>
/// <list type="bullet">
/// <item><description>OffsetX/OffsetY: UI坐标的偏移（在世界坐标转换为UI坐标后应用）</description></item>
/// <item><description>不受相机角度影响，始终是屏幕坐标偏移</description></item>
/// </list>
/// <para>使用示例：</para>
/// <list type="bullet">
/// <item><description>对话气泡</description></item>
/// </list>
/// </remarks>
[GameObject<GameDataControlBubbleUI>]
public partial class BubbleUI : Panel, IThinker, IGameObject<GameDataControlBubbleUI>, IGameObject
{
    private Unit? _bindUnit;
    private readonly Label _label;
    private readonly Panel _canFinishPanel;
    private readonly TypewriterEffectBehavior _typewriterEffect;
    private BubbleDirection _direction;
    private UIPosition _lastPosition = new UIPosition(0, 0);  // 记录上一次的位置
    private string _socketName = "socket_root";  // 绑点名称
    
    /// <summary>
    /// UI 坐标偏移（X/Y 轴，屏幕坐标，不受镜头旋转影响）
    /// </summary>
    public UIPosition UIOffset { get; set; } = new UIPosition(0, 0);
    
    /// <summary>
    /// Z 轴偏移（世界坐标，影响深度/前后位置）
    /// </summary>
    public float OffsetZ { get; set; } = 0.0f;
    
    /// <summary>
    /// 气泡方向
    /// </summary>
    public BubbleDirection Direction
    {
        get => _direction;
        set
        {
            if (_direction != value)
            {
                _direction = value;
                ApplyDirectionConfig(value);
            }
        }
    }
    
    /// <summary>
    /// 绑定的单位
    /// </summary>
    public Unit? BindUnit
    {
        get => _bindUnit;
        set
        {
            if (_bindUnit == value) return;
            
            _bindUnit = value;
            
            // 更新思考状态
            UpdateThinkState();
        }
    }
    
    /// <summary>
    /// 获取Label控件（用于高级自定义）
    /// </summary>
    public Label Label => _label;
    
    /// <summary>
    /// 是否启用打字机效果
    /// </summary>
    public bool EnableTypewriter
    {
        get => _typewriterEffect.Enabled;
        set => _typewriterEffect.Enabled = value;
    }
    
    /// <summary>
    /// 打字速度：每帧显示的字符数
    /// </summary>
    public float TypewriterSpeed
    {
        get => _typewriterEffect.CharsPerFrame;
        set => _typewriterEffect.CharsPerFrame = value;
    }
    
    /// <summary>
    /// 打字机效果是否正在播放
    /// </summary>
    public bool IsTypewriterPlaying => _typewriterEffect.IsPlaying;
    
    /// <summary>
    /// 是否显示可完成指示器
    /// </summary>
    public bool ShowCanFinish
    {
        get => _canFinishPanel.Visible;
        set => _canFinishPanel.Visible = value;
    }
    
    /// <summary>
    /// 默认模板
    /// </summary>
    public static new readonly IGameLink<GameDataControlBubbleUI> DefaultTemplate = 
        new GameLink<GameDataControl, GameDataControlBubbleUI>(typeof(BubbleUI).GetHashCode(true));
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public BubbleUI() : this(DefaultTemplate)
    {
    }
    
    /// <summary>
    /// GameData构造函数
    /// </summary>
    public BubbleUI(IGameLink<GameDataControlBubbleUI> link) : base(link)
    {
        // 从GameData读取配置
        var data = link.Data;
        
        // 创建Label子控件
        _label = new Label
        {
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            Height = -1,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
            // VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
            FontSize = data?.FontSize ?? 24,
        };
        
        // 创建可完成指示器
        _canFinishPanel = new Panel
        {
            Width = 30,
            Height = 20,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            Visible = false,
            Image = data?.CanFinishIndicatorImage?.Path ?? "@gameui/image/conversation/可完成.png",
        };
        
        // 创建打字机效果
        _typewriterEffect = new TypewriterEffectBehavior(_label);
        
        // 设置默认面板属性
        this.ZIndex = StandardUIType.Dialogue.ExpectedZIndex ?? 0;
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        this.AddToVisualTree();
        
        // 添加子控件到面板
        this.AddChild(_label);
        this.AddChild(_canFinishPanel);
        
        // 应用默认方向配置
        _direction = data?.DefaultDirection ?? BubbleDirection.Right;
        ApplyDirectionConfig(_direction);
        
        // 初始化时不启动思考
        ((IThinker)this).DoesThink = false;
    }
    
    /// <summary>
    /// 应用方向配置
    /// </summary>
    private void ApplyDirectionConfig(BubbleDirection direction)
    {
        var data = this.Link.Data;
        GameDataBubbleUILayout? layout = null;
        
        // 尝试从字典获取布局配置
        if (data?.DirectionLayouts.TryGetValue(direction, out var layoutLink) == true)
        {
            layout = layoutLink?.Data;
        }
        
        if (layout != null)
        {
            // 应用方向布局配置
            this.Image = layout.BubbleImage?.Path ?? "@gameui/image/conversation/气泡右.png";
            
            // 应用九宫格切片配置
            if (layout.SlicedEdges.HasValue)
            {
                this.SlicedEdges = layout.SlicedEdges.Value;
            }
            
            // 根据每行文字数和字体大小计算宽度
            var fontSize = data?.FontSize ?? 24;
            _label.Width = layout.CharsPerLine * fontSize;
            this.Width =-1;
            this.Height = -1; // 固定为自适应
            
            this.UIOffset = layout.UIOffset;
            this.OffsetZ = layout.OffsetZ;
            _label.Margin = layout.TextMargin;
            
            // 应用绑点配置
            _socketName = layout.SocketName ?? "socket_root";
        }
        else
        {
            // 使用默认配置
            this.Image = "@gameui/image/conversation/气泡右.png";
            _socketName = "socket_root";
            // 默认：20个字 * 24字体 + 边距 (26+15)
            var fontSize = data?.FontSize ?? 24;
            var defaultMargin = new Thickness(36, 21, right: 18, bottom: 20);
            this.SlicedEdges = new Thickness(36, 21, right: 18, bottom: 20);
            _label.Width = 20 * fontSize;
            this.Width = -1;
            this.Height = -1; // 固定为自适应
            _label.Margin = defaultMargin;
        }
        _canFinishPanel.Margin = _label.Margin;
    }
    
    /// <summary>
    /// 更新思考状态 - 只有绑定了有效单位且可见时才启动思考
    /// </summary>
    private void UpdateThinkState()
    {
        bool shouldThink = _bindUnit != null && _bindUnit.IsValid && this.Visible;
        ((IThinker)this).DoesThink = shouldThink;
    }
    
    /// <summary>
    /// IThinker接口实现 - 每帧更新UI位置
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public void Think(int delta)
    {
        // 检查单位有效性
        if (_bindUnit == null || !_bindUnit.IsValid)
        {
            ((IThinker)this).DoesThink = false;
            this.Visible = false;
            return;
        }
        // 获取单位的绑点世界坐标，并应用 Z 轴偏移（世界坐标）
        var socketPosition = _bindUnit.GetSocketPosition(_socketName);
        var worldPosition = new ScenePoint
        {
            X = socketPosition.X,
            Y = socketPosition.Y,
            Z = socketPosition.Z + OffsetZ  // 只在世界坐标应用 Z 轴偏移
        };
        
        // 将世界坐标转换为UI坐标
        var raycastResult = DeviceInfo.PrimaryViewport.RaycastWorldToUI(worldPosition);
        if (raycastResult.IsHit)
        {    
            // 根据气泡方向计算额外的 UI 坐标偏移（用于对齐气泡位置）
            float ExOffsetX = 0;
            float ExOffsetY = 0;
            if (Direction == BubbleDirection.Right)
            {
            }
            else if (Direction == BubbleDirection.Left)
            {
                ExOffsetX = -this.ActualSize.Width;
            }
            else if (Direction == BubbleDirection.Up)
            {
                ExOffsetY = -this.ActualSize.Height;
                ExOffsetX = -this.ActualSize.Width / 2;
            }
            else if (Direction == BubbleDirection.Down)
            {
                // ExOffsetY = this.ActualSize.Height;
                ExOffsetX = -this.ActualSize.Width / 2;
            }
            
            // 计算新位置：基础UI位置 + UI坐标偏移 + 气泡对齐偏移
            var newPosition = new UIPosition(
                raycastResult.UIPosition.Left + UIOffset.Left + ExOffsetX,
                raycastResult.UIPosition.Top + UIOffset.Top + ExOffsetY
            );
            
            // 只有当变化超过阈值（1像素）时才更新位置，避免抖动
            const float threshold = 1.0f;
            float deltaX = Math.Abs(newPosition.Left - _lastPosition.Left);
            float deltaY = Math.Abs(newPosition.Top - _lastPosition.Top);
            
            if (deltaX > threshold || deltaY > threshold)
            {
                this.Position = newPosition;
                _lastPosition = newPosition;
            }
        }
        else
        {
            // 转换失败（单位可能在屏幕外），隐藏UI
            this.Visible = false;
        }
    }
    
    /// <summary>
    /// 显示气泡UI
    /// </summary>
    public void Show()
    {
        this.Visible = true;
        UpdateThinkState();
    }
    
    /// <summary>
    /// 隐藏气泡UI
    /// </summary>
    public void Hide()
    {
        this.Visible = false;
        ((IThinker)this).DoesThink = false;
    }
    
    /// <summary>
    /// 设置文本并显示（支持打字机效果）
    /// </summary>
    /// <param name="text">要显示的文本</param>
    public void ShowText(string text)
    {
        if (_typewriterEffect.Enabled)
        {
            // 使用打字机效果播放文本
            _typewriterEffect.Play(text);
        }
        else
        {
            // 直接显示文本
            this.Label.Text = text;
        }
    }
    
    /// <summary>
    /// 立即完成打字效果（如果正在播放）
    /// </summary>
    public void CompleteTypewriter()
    {
        _typewriterEffect.Complete();
    }
    
    /// <summary>
    /// 停止打字效果（如果正在播放）
    /// </summary>
    public void StopTypewriter()
    {
        _typewriterEffect.Stop();
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        // 停止思考
        ((IThinker)this).DoesThink = false;
        
        // 停止打字机效果
        _typewriterEffect?.Destroy();
        
        // 清理资源
        _label?.Destroy();
        _bindUnit = null;
        
        base.DisposeManaged();
    }
}

#endif

