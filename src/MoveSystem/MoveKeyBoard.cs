#if CLIENT
using Events;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameUI.Control.Primitive;
using GameUI.Struct;
using GameCore.ResourceType;
using System.Numerics;
using GameUI.TriggerEvent;
using GameCore.AbilitySystem.Manager;
using GameCore.AbilitySystem;
using TriggerEncapsulation;
using GameSystemUI.MoveKeyBoard.Data;
using GameData.Extension;
using GameData;
using GameCore.GameSystem.Enum;
using GameUI.Control.Data;
using GameSystemUI.CmdResultSystemUI;
using GameSystemUI.MoveSystem;
using System.Collections.ObjectModel;
using GameUI.Control.Struct;
namespace GameSystemUI.MoveKeyBoard.Advanced;

[GameObject<GameDataControlMoveKeyBoard>]
public partial class MoveKeyBoard : MoveControlBase
{
    public static new readonly IGameLink<GameDataControlMoveKeyBoard> DefaultTemplate = new GameLink<GameDataControl, GameDataControlMoveKeyBoard>(typeof(MoveKeyBoard).GetHashCode(true));
    private bool WPressed = false;
    private bool SPressed = false;
    private bool APressed = false;
    private bool DPressed = false;
    private int pressedCount = 0;
    private int axisX = 0;
    private int axisY = 0;
    private readonly Panel upBoardPanel;
    private readonly Panel downBoardPanel;
    private readonly Panel leftBoardPanel;
    private readonly Panel rightBoardPanel;
    private readonly Trigger<EventPlayerMainUnitChanged> mainUnitChangedTrigger;
    private Image _upBoardActiveImage = "@gameui/image/keyboard/W_on.png"u8;
    private Image _downBoardActiveImage = "@gameui/image/keyboard/S_on.png"u8;
    private Image _leftBoardActiveImage = "@gameui/image/keyboard/A_on.png"u8;
    private Image _rightBoardActiveImage = "@gameui/image/keyboard/D_on.png"u8;
    private Image _upBoardInactiveImage = "@gameui/image/keyboard/W_off.png"u8;
    private Image _downBoardInactiveImage = "@gameui/image/keyboard/S_off.png"u8;
    private Image _leftBoardInactiveImage = "@gameui/image/keyboard/A_off.png"u8;
    private Image _rightBoardInactiveImage = "@gameui/image/keyboard/D_off.png"u8;

    public Image UpBoardActiveImage
    {
        get => _upBoardActiveImage;
        set
        {
            _upBoardActiveImage = value;
            if (WPressed)
                upBoardPanel.Image = value.Path;
        }
    }

    public Image DownBoardActiveImage
    {
        get => _downBoardActiveImage;
        set
        {
            _downBoardActiveImage = value;
            if (SPressed)
                downBoardPanel.Image = value.Path;
        }
    }

    public Image LeftBoardActiveImage
    {
        get => _leftBoardActiveImage;
        set
        {
            _leftBoardActiveImage = value;
            if (APressed)
                leftBoardPanel.Image = value.Path;
        }
    }

    public Image RightBoardActiveImage
    {
        get => _rightBoardActiveImage;
        set
        {
            _rightBoardActiveImage = value;
            if (DPressed)
                rightBoardPanel.Image = value.Path;
        }
    }

    public Image UpBoardInactiveImage
    {
        get => _upBoardInactiveImage;
        set
        {
            _upBoardInactiveImage = value;
            if (!WPressed)
                upBoardPanel.Image = value.Path;
        }
    }

    public Image DownBoardInactiveImage
    {
        get => _downBoardInactiveImage;
        set
        {
            _downBoardInactiveImage = value;
            if (!SPressed)
                downBoardPanel.Image = value.Path;
        }
    }

    public Image LeftBoardInactiveImage
    {
        get => _leftBoardInactiveImage;
        set
        {
            _leftBoardInactiveImage = value;
            if (!APressed)
                leftBoardPanel.Image = value.Path;
        }
    }

    public Image RightBoardInactiveImage
    {
        get => _rightBoardInactiveImage;
        set
        {
            _rightBoardInactiveImage = value;
            if (!DPressed)
                rightBoardPanel.Image = value.Path;
        }
    }
    private Unit? _bindUnit;
    private bool _useMainUnit = true;


    /// <summary>
    /// 是否绑定主控单位模式
    /// </summary>
    public bool UseMainUnit
    {
        get => _useMainUnit;
        set
        {
            if (_useMainUnit == value) return;
            _useMainUnit = value;
            // 启用主控单位模式时，自动绑定当前主控单位
            if (value)
            {
                _bindUnit = Player.LocalPlayer?.MainUnit;
            }
        }
    }

    /// <summary>
    /// 绑定的单位
    /// </summary>
    public override Unit? BindUnit 
    { 
        get => _bindUnit;
        set
        {
            var oldUnit = _bindUnit;
            var newUnit = value;
            
            // 如果单位发生了变化
            if (newUnit != oldUnit)
            {
                // 记录当前是否正在移动中
                bool isCurrentlyMoving = (pressedCount > 0 && (axisX != 0 || axisY != 0));
                
                // 如果StopMoveOnUnitChange为true，停止前一个单位的移动
                if (StopMoveOnUnitChange)
                {
                    MoveStop();
                }
                
                // 手动设置绑定单位时，退出主控单位模式
                _useMainUnit = false;
                _bindUnit = newUnit;
                
                // 如果之前正在移动中，需要对新单位发送移动命令并重置firstMove
                if (isCurrentlyMoving && newUnit != null)
                {
                    // 重置firstMove标记
                    isFirstMove = true;
                    
                    // 发送当前方向的移动命令给新单位
                    Angle angle = Angle.FromVector2(new System.Numerics.Vector2(axisX, axisY));
                    Move(angle);
                }
            }
            else
            {
                // 手动设置绑定单位时，退出主控单位模式
                _useMainUnit = false;
                _bindUnit = value;
            }
        }
    }

    public MoveKeyBoard() : this(DefaultTemplate)
    {
       
    }

    public MoveKeyBoard(IGameLink<GameDataControlMoveKeyBoard> link) : base(link)
    {
        this.Width = 184;
        this.Height = 120;
        this.Position = new UIPosition(101, -90);
        this.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        this.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        if(link.Data?.ZIndex != null)
        {
            this.ZIndex = link.Data.ZIndex.Value;
        }
        else
        {
            this.ZIndex = StandardUIType.Joystick.ExpectedZIndex ?? 0;
        }
        
        upBoardPanel = new Panel()
        {
            Image = _upBoardInactiveImage.Path,
            Width = 56,
            Height = 56,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
        };
        downBoardPanel = new Panel()
        {
            Image = _downBoardInactiveImage.Path,
            Width = 56,
            Height = 56,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        leftBoardPanel = new Panel()
        {
            Image = _leftBoardInactiveImage.Path,
            Width = 56,
            Height = 56,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        rightBoardPanel = new Panel()
        {
            Image = _rightBoardInactiveImage.Path,
            Width = 56,
            Height = 56,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.AddChild(upBoardPanel);
        this.AddChild(downBoardPanel);
        this.AddChild(leftBoardPanel);
        this.AddChild(rightBoardPanel);

        // 初始化 KeyboardAccelerators 集合
        InitializeKeyboardAccelerators();
        
        // 初始化主控单位事件监听
        mainUnitChangedTrigger = new(OnMainUnitChanged);
        if (Player.LocalPlayer != null)
        {
            mainUnitChangedTrigger.Register(Player.LocalPlayer);
            // 初始化时绑定当前主控单位
            _bindUnit = Player.LocalPlayer.MainUnit;
        }

    }

    /// <summary>
    /// 初始化键盘加速器
    /// </summary>
    private void InitializeKeyboardAccelerators()
    {
        // 初始化 KeyboardAccelerators 集合
        KeyboardAccelerators = new ObservableCollection<KeyboardAccelerator>
        {
            new() { Key = VirtualKey.W },
            new() { Key = VirtualKey.S },
            new() { Key = VirtualKey.A },
            new() { Key = VirtualKey.D }
        };

        // 绑定事件处理器
        OnKeyboardAcceleratorInvoked += OnKeyboardAcceleratorInvokedHandler;
        OnKeyboardAcceleratorReleased += OnKeyboardAcceleratorReleasedHandler;
    }

    /// <summary>
    /// 按键按下事件处理
    /// </summary>
    private void OnKeyboardAcceleratorInvokedHandler(object? sender, KeyboardAcceleratorEventArgs e)
    {
        int lastPressedCount = pressedCount;
        switch (e.Accelerator.Key)
        {
            case VirtualKey.W:
                if (WPressed) break;
                upBoardPanel.Image = _upBoardActiveImage.Path;
                axisY -= 1;
                pressedCount += 1;
                WPressed = true;
                break;
            case VirtualKey.S:
                if (SPressed) break;
                downBoardPanel.Image = _downBoardActiveImage.Path;
                axisY += 1;
                pressedCount += 1;
                SPressed = true;
                break;
            case VirtualKey.A:
                if (APressed) break;
                leftBoardPanel.Image = _leftBoardActiveImage.Path;
                axisX -= 1;
                pressedCount += 1;
                APressed = true;
                break;
            case VirtualKey.D:
                if (DPressed) break;
                rightBoardPanel.Image = _rightBoardActiveImage.Path;
                axisX += 1;
                pressedCount += 1;
                DPressed = true;
                break;
        }
        if (pressedCount != lastPressedCount)
        {
            // 从无按键到有按键时
            if (lastPressedCount == 0 && pressedCount > 0)
            {
                OnStartMoving();
            }
            RefreshMoveDirection();
        }
    }

    /// <summary>
    /// 按键释放事件处理
    /// </summary>
    private void OnKeyboardAcceleratorReleasedHandler(object? sender, KeyboardAcceleratorEventArgs e)
    {
        int lastPressedCount = pressedCount;
        switch (e.Accelerator.Key)
        {
            case VirtualKey.W:
                if (!WPressed) break;
                upBoardPanel.Image = _upBoardInactiveImage.Path;
                axisY += 1;
                WPressed = false;
                pressedCount -= 1;
                break;
            case VirtualKey.S:
                if (!SPressed) break;
                downBoardPanel.Image = _downBoardInactiveImage.Path;
                axisY -= 1;
                SPressed = false;
                pressedCount -= 1;
                break;
            case VirtualKey.A:
                if (!APressed) break;
                leftBoardPanel.Image = _leftBoardInactiveImage.Path;
                axisX += 1;
                APressed = false;
                pressedCount -= 1;
                break;
            case VirtualKey.D:
                if (!DPressed) break;
                rightBoardPanel.Image = _rightBoardInactiveImage.Path;
                axisX -= 1;
                DPressed = false;
                pressedCount -= 1;
                break;
        }
        if (pressedCount != lastPressedCount)
        {
            RefreshMoveDirection();
            // 从有按键到无按键时
            if (lastPressedCount > 0 && pressedCount == 0)
            {
                OnStopMoving();
            }
        }
    }

    /// <summary>
    /// 主控单位变化事件处理
    /// </summary>
    private async Task<bool> OnMainUnitChanged(object sender, EventPlayerMainUnitChanged eventArgs)
    {
        // 只有在主控单位模式下才自动更新绑定
        if (_useMainUnit)
        {
            var oldUnit = _bindUnit;
            var newUnit = eventArgs.Unit;
            
            // 如果单位发生了变化
            if (newUnit != oldUnit)
            {
                // 记录当前是否正在移动中
                bool isCurrentlyMoving = (pressedCount > 0 && (axisX != 0 || axisY != 0));
                
                // 如果StopMoveOnUnitChange为true，停止前一个单位的移动
                if (StopMoveOnUnitChange)
                {
                    MoveStop();
                }
                
                _bindUnit = newUnit;
                
                // 如果之前正在移动中，需要对新单位发送移动命令并重置firstMove
                if (isCurrentlyMoving && newUnit != null)
                {
                    // 重置firstMove标记
                    isFirstMove = true;
                    
                    // 发送当前方向的移动命令给新单位
                    Angle angle = Angle.FromVector2(new System.Numerics.Vector2(axisX, axisY));
                    Move(angle);
                    isFirstMove = false;
                }
            }
        }
        await Task.CompletedTask;
        return true;
    }

    private void RefreshMoveDirection()
    {
        if (_bindUnit == null)
        {
            return;
        }
        if (pressedCount == 0 || (axisX == 0 && axisY == 0))
        {
            // 停止移动
            MoveStop();
        }
        else
        {
            Angle angle = Angle.FromVector2(new Vector2(axisX, axisY));
            
            // 使用基类的执行移动方法
            ExecuteMove(angle);
        }
    }



    #region MoveControlBase抽象方法实现

    /// <summary>
    /// 检查是否正在移动中
    /// </summary>
    protected override bool IsCurrentlyMoving => pressedCount > 0;

    /// <summary>
    /// 获取当前移动角度
    /// </summary>
    protected override Angle? GetCurrentMoveAngle()
    {
        if (axisX == 0 && axisY == 0) return null;
        return Angle.FromVector2(new System.Numerics.Vector2(axisX, axisY));
    }

    /// <summary>
    /// 处理单位绑定变化的特定逻辑
    /// </summary>
    protected override void OnBindUnitChanged(Unit? oldUnit, Unit? newUnit, bool wasMoving)
    {
        // 更新内部绑定的单位
        _bindUnit = newUnit;
    }

    #endregion
}

#endif
