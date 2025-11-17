#if CLIENT
using Events;
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameUI.Control.Primitive;
using GameUI.Device;
using GameUI.Struct;
using GameUI.Control.Struct;
using GameData;
using GameData.Extension;
using GameUI.Control.Data;
using GameUI.Enum;
using GameSystemUI.AbilitySystemUI.Data;
using GameSystemUI.CmdResultSystemUI;
using GameSystemUI.MoveSystem;

using Microsoft.Extensions.Logging;
using System.Numerics;
using GameCore.GameSystem.Enum;
using GameSystemUI.AbilitySystemUI.Advanced;
using GameSystemUI.MoveJoystick.Data;

namespace GameSystemUI.MoveJoystick.Advanced;

/// <summary>
/// 移动摇杆控件 - 专门用于控制单位移动的摇杆
/// 支持死区、镜头角度补偿、安全区域限制等功能
/// </summary>
[GameObject<GameDataControlMoveJoyStick>]
public partial class MoveJoyStick : MoveControlBase
{
    /// <summary>
    /// 默认模板链接
    /// </summary>
    public static new readonly IGameLink<GameDataControlMoveJoyStick> DefaultTemplate = new GameLink<GameDataControl, GameDataControlMoveJoyStick>(typeof(MoveJoyStick).GetHashCode(true));

    private readonly Joystick joystick;
    private int PressedCount = 0; //按下计数,第一次按下和最后一次松开才触发逻辑

    // 或许有技能摇杆时可以改成static
    private readonly Trigger<EventPlayerMainUnitChanged> unitChangedTrigger;


    /// <summary>
    /// 摇杆是否当前处于活动状态（可见且有指针捕获）
    /// </summary>
    public bool IsActive => joystick.Visible && joystick.HasActivePointer;

    /// <summary>
    /// 摇杆是否当前可见
    /// </summary>
    public bool IsVisible => joystick.Visible;

    private bool isInitialized = false;
    private Angle? currentMoveAngle = null; // 记录当前移动角度

    private Unit? InnerBindUnit
    {
        get => joystick.BindUnit;
        set
        {
            if (!isInitialized) return;
            
            var oldUnit = joystick.BindUnit;
            var newUnit = value;
            
            // 使用基类的单位变化处理逻辑
            HandleBindUnitChange(oldUnit, newUnit);
        }
    }

    private bool useMainUnit = true;
    public bool UseMainUnit
    {
        get => useMainUnit;
        set
        {
            if (useMainUnit == value) return;
            useMainUnit = value;
            // 启用则始终绑定主控
            if (value && Player.LocalPlayer?.MainUnit != null)
            {
                this.InnerBindUnit = Player.LocalPlayer.MainUnit;
            }
        }
    }

    public override Unit? BindUnit
    {
        get => this.InnerBindUnit;
        set
        {
            this.UseMainUnit = false;
            if (value == this.InnerBindUnit) return;
            this.InnerBindUnit = value;
        }
    }

    /// <summary>
    /// 死区范围（像素）
    /// </summary>
    public int DeadBand { get; set; } = 10;

    // 修改公共事件接口为EventHandler
    public event EventHandler<JoystickMoveEventArgs>? OnJoystickMove
    {
        add => joystick.OnJoystickMove += value;
        remove => joystick.OnJoystickMove -= value;
    }

    public MoveJoyStick() : this(DefaultTemplate)
    {
    }

    public MoveJoyStick(IGameLink<GameDataControlMoveJoyStick> link) : base(link)
    {
        // 应用GameData配置
        ApplyGameDataSettings();

        this.WidthStretchRatio = 0.5f;
        this.HeightStretchRatio = 1;
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.OnPointerPressed += OnPressed;
        this.OnPointerReleased += OnReleased;

        if(link.Data?.ZIndex != null)
        {
            this.ZIndex = link.Data.ZIndex.Value;
        }
        else
        {
            this.ZIndex = StandardUIType.Joystick.ExpectedZIndex ?? 0;
        }

        joystick = new Joystick();
        joystick.EnablePressed = false; // 禁用摇杆自身的按下抬起逻辑
        _ = joystick.AddToParent(this);
        // 默认左上角方便调整位置
        joystick.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        joystick.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        // 默认不可见
        joystick.Visible = false;
        this.OnJoystickMove += OnMove;
        unitChangedTrigger = new(OnUnitChange);
        unitChangedTrigger.Register(Player.LocalPlayer);
        isInitialized = true;
        if(useMainUnit && Player.LocalPlayer?.MainUnit != null)
        {
            this.InnerBindUnit = Player.LocalPlayer?.MainUnit;
        }
    }

    /// <summary>
    /// 应用GameData配置设置
    /// </summary>
    private void ApplyGameDataSettings()
    {
        if (Cache is GameDataControlMoveJoyStick moveJoyStickGameData)
        {
            if (moveJoyStickGameData.DeadBand.HasValue)
            {
                this.DeadBand = moveJoyStickGameData.DeadBand.Value;
            }
            
            if (moveJoyStickGameData.StopMoveOnUnitChange.HasValue)
            {
                this.StopMoveOnUnitChange = moveJoyStickGameData.StopMoveOnUnitChange.Value;
            }
            
            if (moveJoyStickGameData.UseMainUnit.HasValue)
            {
                this.UseMainUnit = moveJoyStickGameData.UseMainUnit.Value;
            }
        }
    }

    private async Task<bool> OnUnitChange(object sender, EventPlayerMainUnitChanged eventArgs)
    {
        if (this.UseMainUnit)
        {
            this.InnerBindUnit = eventArgs.Unit;
        }
        await Task.CompletedTask;
        return true;
    }

    // 点击时，显示摇杆
    private void OnPressed(object? sender, PointerEventArgs e)
    {
        PressedCount++;
        if (PressedCount > 1)
        {
            return;
        }
        
         // 清除记录的移动角度
         currentMoveAngle = null;
         // 调用基类的开始移动方法
         OnStartMoving();
        var index = e.PointerButtons;
        var position = DeviceInfo.PrimaryViewport.GetPointerInputPosition(index);
        if (position != null)
        {
            joystick.Visible = true;
            // 计算相对位置，使摇杆中心对准点击位置
            var relativeX = position.Value.Left - (this.ScreenPosition.Left + joystick.ActualSize.Width / 2);
            var relativeY = position.Value.Top - (this.ScreenPosition.Top + joystick.ActualSize.Height / 2);

            var padding = ScreenViewport.Primary.SafeZonePadding;
            var screenWidth = ScreenViewport.Primary.WidthPx / ScreenViewport.Primary.DevicePixelRatio;
            var screenHeight = ScreenViewport.Primary.HeightPx / ScreenViewport.Primary.DevicePixelRatio;

            //如果相对位置小于padding，则将相对位置设置为padding
            relativeX = Math.Max(relativeX, padding.Left);
            relativeY = Math.Max(relativeY, padding.Top);

            // 计算实际的安全区域边界,还要加上摇杆的直径
            var safeRight = screenWidth - padding.Right - joystick.ActualSize.Width;
            var safeBottom = screenHeight - padding.Bottom - joystick.ActualSize.Height;

            //如果相对位置大于安全区域边界，则将相对位置设置为边界
            relativeX = Math.Min(relativeX, safeRight);
            relativeY = Math.Min(relativeY, safeBottom);

            // 设置摇杆和摇杆按钮位置
            joystick.Position = new UIPosition(relativeX, relativeY);
            joystick.Activate(index);
        }
        else
        {
            Game.Logger.Log(LogLevel.Warning, "GetPointerInputPosition Return Null");
        }
    }

    // 松开时，隐藏摇杆,停止移动
    private void OnReleased(object? sender, PointerEventArgs e)
    {
        PressedCount--;
        if (PressedCount > 0)
        {
            return;
        }
        joystick.Visible = false;
        // 使用更清晰的方法来重置摇杆状态
        joystick.ForceReset();
        // 停止移动
        MoveStop();
        
         // 清除记录的角度
         currentMoveAngle = null;
         // 调用基类的停止移动方法
         OnStopMoving();
    }

    private void OnMove(object? sender, JoystickMoveEventArgs e)
    {
        // 检查是否在死区内
        if (Math.Sqrt(e.Position.Left * e.Position.Left + e.Position.Top * e.Position.Top) < DeadBand)
        {
            MoveStop();
            // 清除记录的移动角度
            currentMoveAngle = null;
            return;
        }

         // 直接使用摇杆的原始位置计算角度（不需要手动应用镜头偏移）
         // 基类会自动在Move方法中应用镜头偏移
        
         // 记录当前移动角度（原始角度，不包含镜头偏移）
         currentMoveAngle = Angle.FromVector2(new Vector2((float)e.Position.Left, (float)e.Position.Top));
         
         // 使用基类的执行移动方法（基类会自动应用镜头偏移）
         ExecuteMove(currentMoveAngle.Value);
    }



    protected override void DisposeManaged()
    {
        base.DisposeManaged();

        // 确保摇杆状态被正确重置
        joystick.ForceReset();

        this.OnJoystickMove -= OnMove;
        this.OnPointerPressed -= OnPressed;
        this.OnPointerReleased -= OnReleased;
        unitChangedTrigger.Destroy();
    }

    #region MoveControlBase抽象方法实现

    /// <summary>
    /// 检查是否正在移动中
    /// </summary>
    protected override bool IsCurrentlyMoving => IsActive;

    /// <summary>
    /// 获取当前移动角度
    /// </summary>
    protected override Angle? GetCurrentMoveAngle() => currentMoveAngle;

    /// <summary>
    /// 处理单位绑定变化的特定逻辑
    /// </summary>
    protected override void OnBindUnitChanged(Unit? oldUnit, Unit? newUnit, bool wasMoving)
    {
        // 更新摇杆绑定的单位
        if (joystick != null)
        {
            joystick.BindUnit = newUnit;
        }
    }

    #endregion
}
#endif
