#if CLIENT
using Events;

using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameCore.ResourceType;
using GameUI.Control.Primitive;
using GameUI.Device;
using GameUI.Struct;
using GameUI.Control.Struct;

using Microsoft.Extensions.Logging;
using System.Numerics;
using GameSystemUI.CmdResultSystemUI;
using GameCore.GameSystem.Enum;

namespace GameSystemUI.AbilitySystemUI.Advanced;

// 添加摇杆事件参数类
public class JoystickMoveEventArgs : EventArgs
{
    public UIPosition Position { get; }
    
    public JoystickMoveEventArgs(UIPosition position)
    {
        Position = position;
    }
}

public class JoystickPressedEventArgs : EventArgs
{
    public PointerButtons PointerButtons { get; }
    
    public JoystickPressedEventArgs(PointerButtons pointerButtons)
    {
        PointerButtons = pointerButtons;
    }
}

public class JoystickReleasedEventArgs : EventArgs
{
    public PointerButtons PointerButtons { get; }
    
    public JoystickReleasedEventArgs(PointerButtons pointerButtons)
    {
        PointerButtons = pointerButtons;
    }
}

public class Joystick : Panel
{
    private const int Threshold = 1;// 阈值
    private bool isPressed = false;
    protected bool IsPressed
    {
        get { return isPressed; }
        set
        {
            if (isPressed == value) return;
            isPressed = value;
            ResetToCenter();
        }
    }
    private bool enablePressed = true;
    public bool EnablePressed
    {
        //是否启用按下抬起逻辑
        get
        {
            return enablePressed;
        }
        set
        {
            if (enablePressed == value) //避免重复订阅
            {
                return;
            }
            enablePressed = value;
            if (enablePressed)
            {
                this.OnPointerPressed += OnPressed;
                this.OnPointerReleased += OnReleased;
            }
            else
            {
                this.OnPointerPressed -= OnPressed;
                this.OnPointerReleased -= OnReleased;
            }
        }
    }
    private bool isRotationFollow = false;
    public bool IsRotationFollow
    {
        get => isRotationFollow;
        set
        {
            isRotationFollow = value;
        }
    }
    protected PointerButtons? index = null;
    public PointerButtons? Index {get => index;}
    
    private UIPosition lastPosition = new(0, 0);
    private readonly Panel background;
    private readonly Button button;
    

    
    /// <summary>
    /// 绑定的单位（可选，用于移动摇杆等场景）
    /// </summary>
    public Unit? BindUnit { get; set; }
    
    /// <summary>
    /// 当前是否有活动的指针捕获
    /// </summary>
    public bool HasActivePointer => this.IsPressed && this.index != null;
    
    /// <summary>
    /// 当前捕获的指针类型
    /// </summary>
    public PointerButtons? CurrentPointer => this.index;
    
    /// <summary>
    /// 检查摇杆是否处于激活状态
    /// </summary>
    public bool IsActivated => HasActivePointer;
    
    public string BackgroundImage
    {
        get => background.Image;
        set => background.Image = value;
    }
    public string ButtonImage
    {
        get => button.Image;
        set => button.Image = value;
    }
    
    /// <summary>
    /// 子类可重写此方法来控制是否显示UI
    /// </summary>
    protected virtual bool ShouldShowUI 
    { 
        get => _shouldShowUI; 
        set 
        { 
            if (_shouldShowUI != value) 
            { 
                _shouldShowUI = value; 
                UpdateUIVisibility(); 
            } 
        } 
    }
    private bool _shouldShowUI = true;
    
    /// <summary>
    /// 更新UI控件的可见性
    /// </summary>
    protected virtual void UpdateUIVisibility()
    {
        background.Visible = ShouldShowUI;
        button.Visible = ShouldShowUI;
    }

    // 将Action事件改为EventHandler事件
    public event EventHandler<JoystickMoveEventArgs>? OnJoystickMove;// 摇杆移动事件,传入摇杆按钮的偏移量,析构时需要移除订阅的函数
    public event EventHandler<JoystickPressedEventArgs>? OnJoystickPressed;
    public event EventHandler<JoystickReleasedEventArgs>? OnJoystickReleased;
    
    // 子控件模式标志：为true时取消内部事件订阅，避免与父控件冲突
    private bool _isChildMode = false;
    
    /// <summary>
    /// 子控件模式：为true时取消内部事件订阅，避免与父控件事件冲突
    /// </summary>
    public bool IsChildMode 
    { 
        get => _isChildMode; 
        set => SetChildMode(value); 
    }



    public Joystick()
    {
        this.Width = 268;
        this.Height = 268;
        background = new Panel()
        {
            WidthCompactRatio = 1,
            HeightCompactRatio = 1,
            Image = "@gameui/image/inventory/joystick.png",
        };
        _ = background.AddToParent(this);

        button = new Button()
        {
            Width = 59,
            Height = 59,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            Image = "@gameui/image/inventory/joystick_center.png",
        };
        _ = button.AddToParent(this);

        // 默认情况下订阅事件，可通过IsChildMode属性控制
        SubscribeToPointerEvents();
        this.OnPointerCapturedMove += OnMove;
    }
    
    /// <summary>
    /// 订阅按下抬起事件
    /// </summary>
    private void SubscribeToPointerEvents()
    {
        this.OnPointerPressed += OnPressed;
        this.OnPointerReleased += OnReleased;
    }
    
    /// <summary>
    /// 取消订阅按下抬起事件
    /// </summary>
    private void UnsubscribeFromPointerEvents()
    {
        this.OnPointerPressed -= OnPressed;
        this.OnPointerReleased -= OnReleased;
    }
    
    /// <summary>
    /// 设置子控件模式并管理事件订阅
    /// </summary>
    private void SetChildMode(bool isChildMode)
    {
        if (_isChildMode == isChildMode) return;
        
        _isChildMode = isChildMode;
        
        if (_isChildMode)
        {
            // 进入子控件模式：取消事件订阅
            UnsubscribeFromPointerEvents();
        }
        else
        {
            // 退出子控件模式：重新订阅事件
            SubscribeToPointerEvents();
        }
    }

    // 释放引用
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        // 使用统一的事件取消订阅方法
        UnsubscribeFromPointerEvents();
        
        OnJoystickMove = null;
        OnJoystickPressed = null;
        OnJoystickReleased = null;
        EnablePressed = false;
        BindUnit = null;
    }

    /// <summary>
    /// 强制释放所有指针捕获并重置状态
    /// </summary>
    public void ForceReset()
    {
        Deactivate();
    }

    /// <summary>
    /// 激活摇杆并捕获指定指针
    /// </summary>
    /// <param name="pointerIndex">要捕获的指针按钮类型</param>
    public void Activate(PointerButtons pointerIndex)
    {
        SetPressedState(pointerIndex);
    }
    


    /// <summary>
    /// 停用摇杆并释放所有指针捕获
    /// </summary>
    public void Deactivate()
    {
        SetReleasedState();
    }

    /// <summary>
    /// 设置按下状态并捕获指针
    /// </summary>
    private void SetPressedState(PointerButtons pointerIndex)
    {
        // 先释放之前的指针捕获（如果有的话）
        if (this.IsPressed && this.index != null)
        {
            this.ReleasePointer(this.index.Value);
        }

        // 设置新状态
        this.IsPressed = true;
        this.index = pointerIndex;
        
        // 捕获新指针
        this.CapturePointer(pointerIndex);
    }

    /// <summary>
    /// 设置释放状态并释放指针捕获
    /// </summary>
    private void SetReleasedState()
    {
        if (this.IsPressed && this.index != null)
        {
            this.ReleasePointer(this.index.Value);
        }
        
        this.IsPressed = false;
        this.index = null;
    }

    private void OnMove(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isPressed || !ShouldShowUI)
        {
            return;
        }

        var position = DeviceInfo.PrimaryViewport.GetPointerInputPosition(e.PointerButtons);
        if (position != null)
        {
            // 做个转换
            var Left = position.Value.Left - (this.ScreenPosition.Left + this.ActualSize.Width / 2);
            var parent = this.Parent as Panel;
            var Top = position.Value.Top - (this.ScreenPosition.Top + this.ActualSize.Height / 2);

            // 限制区域不能超过摇杆的区域
            // 计算到中心点的距离
            var distance = Math.Sqrt(Left * Left + Top * Top);
            // 计算最大允许距离（摇杆半径）
            var maxDistance = this.ActualSize.Width / 2;

            // 如果距离超过最大允许距离，将点限制在圆形边界上
            if (distance > maxDistance)
            {
                // 计算缩放比例
                var scale = (float)(maxDistance / distance);
                Left *= scale;
                Top *= scale;
            }

            button.Position = new UIPosition(Left, Top);

            // 如果移动了，则触发事件,传入摇杆按钮的偏移量
            if (CheckMove(button.Position, lastPosition))
            {
                OnJoystickMove?.Invoke(this, new JoystickMoveEventArgs(button.Position));
                lastPosition = button.Position;
                // Rotation控制
                if (isRotationFollow)
                {
                    double angle = (Math.Atan2(lastPosition.Top, lastPosition.Left) * 180 / Math.PI + 360 + 90) % 360;
                    background.Rotation = (float)angle;
                }
            }
        }
        else
        {
            Game.Logger.Log(LogLevel.Warning, "OnMove Failed,position:{position}", position);
        }
    }

    // 判断摇杆是否移动
    private static bool CheckMove(UIPosition position, UIPosition lastPosition)
    {
        return Math.Sqrt((position.Left - lastPosition.Left) * (position.Left - lastPosition.Left) + (position.Top - lastPosition.Top) * (position.Top - lastPosition.Top)) > Threshold;
    }

    private void OnPressed(object? sender, GameUI.Control.Struct.PointerEventArgs e)
    {
        if(isPressed)
        {
            return;
        }

        IsPressed = true;
        index = e.PointerButtons;
        this.CapturePointer(e.PointerButtons);
        OnJoystickPressed?.Invoke(this, new JoystickPressedEventArgs(e.PointerButtons));
    }

    private void OnReleased(object? sender, GameUI.Control.Struct.PointerEventArgs e)
    {
        if(!isPressed)
        {
            return;
        }
        IsPressed = false;
        this.ReleasePointer(e.PointerButtons);
        OnJoystickReleased?.Invoke(this, new JoystickReleasedEventArgs(e.PointerButtons));
        background.Rotation = 0;
    }

    private void ResetToCenter()
    {
        lastPosition = new UIPosition(0, 0);
        button.Position = new UIPosition(0, 0);
    }
    

}

#endif