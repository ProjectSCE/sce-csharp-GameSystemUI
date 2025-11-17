#if CLIENT
using GameCore.ResourceType;
using GameUI.Control.Primitive;
using GameUI.Struct;
using GameData;
using GameData.Extension;
using GameUI.Control.Data;
using GameSystemUI.AbilitySystemUI.Data;

namespace GameSystemUI.AbilitySystemUI.Advanced;

[GameObject<GameDataStopCastingButton>]
public partial class StopCastingButton : Button
{
    /// <summary>
    /// 默认模板链接
    /// </summary>
    public static new readonly IGameLink<GameDataStopCastingButton> DefaultTemplate = new GameLink<GameDataControl, GameDataStopCastingButton>(typeof(StopCastingButton).GetHashCode(true));

    private bool isActive = false;
    private Image _buttonImage = new Image("@gameui/image/取消施法区域.png");
    private Image _maskImage = new Image("@gameui/image/禁止施法.png");
    private Panel maskPanel;

    /// <summary>
    /// 是否激活状态
    /// </summary>
    public bool IsActive
    {
        get => isActive;
        set
        {
            isActive = value;
            maskPanel.Visible = isActive;
        }
    }
    
    /// <summary>
    /// 停止施法按钮图片
    /// </summary>
    public Image ButtonImage
    {
        get => _buttonImage;
        set
        {
            _buttonImage = value;
            Image = _buttonImage.Path;
        }
    }
    
    /// <summary>
    /// 停止施法遮罩图片
    /// </summary>
    public Image MaskImage
    {
        get => _maskImage;
        set
        {
            _maskImage = value;
            if (maskPanel != null)
                maskPanel.Image = _maskImage.Path;
        }
    }

    /// <summary>
    /// 绑定的施法摇杆
    /// </summary>
    public CastingJoyStick? CurrentCastingJoyStick { get; set; }

    /// <summary>
    /// 使用默认模板创建停止施法按钮实例
    /// </summary>
    public StopCastingButton() : this(DefaultTemplate)
    {
    }

    /// <summary>
    /// 使用指定模板创建停止施法按钮实例
    /// </summary>
    /// <param name="link">停止施法按钮模板链接</param>
    public StopCastingButton(IGameLink<GameDataStopCastingButton> link) : base(link)
    {
        Width = 144;
        Height = 144;
        Visible = false;

        maskPanel = new Panel()
        {
            Width = 144,
            Height = 144,
            Visible = false,
        };
        maskPanel.AddToParent(this);
        
        Image = ButtonImage.Path;
        maskPanel.Image = MaskImage.Path;
        
        // 订阅悬停事件
        OnPointerEntered += OnStopCastingButtonEntered;
        OnPointerExited += OnStopCastingButtonExited;
    }
    
    /// <summary>
    /// 停止施法按钮悬停进入事件处理
    /// </summary>
    private void OnStopCastingButtonEntered(object? sender, EventArgs e)
    {
        IsActive = true;
        CurrentCastingJoyStick?.OnStopCastingInternal();
    }
    
    /// <summary>
    /// 停止施法按钮悬停退出事件处理
    /// </summary>
    private void OnStopCastingButtonExited(object? sender, EventArgs e)
    {
        IsActive = false;
        CurrentCastingJoyStick?.OnStopCastingEndInternal();
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        OnPointerEntered -= OnStopCastingButtonEntered;
        OnPointerExited -= OnStopCastingButtonExited;
    }
}

#endif

