#if CLIENT
using Events;
using GameCore.AbilitySystem;
using GameCore.AbilitySystem.Data.Enum;
using GameCore.ActorSystem;
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameCore.ResourceType;
using GameCore.SceneSystem;
using GameCore.TargetingSystem;
using GameUI.Control.Primitive;
using GameUI.Device;
using GameUI.Struct;
using TriggerEncapsulation;
using GameSystemUI.CmdResultSystemUI;

using Microsoft.Extensions.Logging;
using System.Numerics;
using GameSystemUI.GameInventoryUI;
using GameCore.AbilitySystem.Enum;

namespace GameSystemUI.AbilitySystemUI.Advanced;

/// <summary>
/// 施法摇杆，支持键盘和鼠标两种激活模式
/// </summary>
public class CastingJoyStick : Joystick, IThinker
{
    private StopCastingButton? stopCastingButton;

    private Image _defaultBackground = new Image("@gameui/image/施法轮盘.png");
    private Image _defaultBackgroundDisable = new Image("@gameui/image/施法轮盘_禁止.png");
    // 全局静态施法指示器，确保同时只能有一个
    private static TargetingIndicator? globalTargetingIndicator;
    // 当前拥有指示器控制权的实例
    private static CastingJoyStick? indicatorOwner;
    /// <summary>
    /// 标记当前是否是键盘激活状态
    /// </summary>
    public bool IsKeyboardActivated { get; private set; } = false;
    
    /// <summary>
    /// 是否显示摇杆UI（基于技能的AlwaysAcquireTarget属性）
    /// </summary>
    public bool ShouldShowJoyStickUI { get; private set; } = true;
    
    /// <summary>
    /// 标记在摇杆按下施法模式时是否已经移动过
    /// </summary>
    private bool hasMovedInJoystickMode = false;
    
    private Ability? ability;
    private UIPosition lastPosition;
    private ScenePoint lastHostPosition;
    private ICommandTarget? target;
    private Unit? highlightedUnit;
    private Actor? highlightActor;

    /// <summary>
    /// 停止施法阶段事件 - 进入禁用状态时触发
    /// </summary>
    public event EventHandler? OnStopCasting;
    
    /// <summary>
    /// 停止施法结束阶段事件 - 恢复正常状态时触发
    /// </summary>
    public event EventHandler? OnStopCastingEnd;

    /// <summary>
    /// 默认背景图片
    /// </summary>
    public Image DefaultBackground 
    { 
        get => _defaultBackground;
        set 
        {
            _defaultBackground = value;
            if (BackgroundImage == _defaultBackgroundDisable.Path)
            {
                // 当前是禁用状态，不更新
            }
            else
            {
                BackgroundImage = _defaultBackground.Path;
            }
        }
    }
    
    /// <summary>
    /// 禁用状态背景图片
    /// </summary>
    public Image DefaultBackgroundDisable 
    { 
        get => _defaultBackgroundDisable;
        set 
        {
            _defaultBackgroundDisable = value;
            if (BackgroundImage == _defaultBackgroundDisable.Path)
            {
                BackgroundImage = _defaultBackgroundDisable.Path;
            }
        }
    }

    /// <summary>
    /// 绑定的技能（支持所有Ability类型，包括AbilityExecute）
    /// </summary>
    public Ability? Ability
    {
        get => ability;
        set
        {
            if (ability == value) return;
            ability = value;
            UpdateShouldShowJoyStickUI();
            // 如果没有技能且当前拥有指示器控制权，释放控制权
            if (ability == null && indicatorOwner == this)
            {
                ReleaseIndicatorControl();
            }
        }
    }

    /// <summary>
    /// 技能宿主单位
    /// </summary>
    private Unit? host => ability?.Host;
    
    /// <summary>
    /// 更新是否应该显示摇杆UI
    /// </summary>
    private void UpdateShouldShowJoyStickUI()
    {
        bool newShouldShow;
        
        if (ability != null)
        {
            if (ability is AbilityExecute abilityExecute)
            {
                // AbilityExecute: 检查是否需要目标选择或者是矢量目标类型
                newShouldShow = abilityExecute.AlwaysAcquireTarget || abilityExecute.Cache.TargetType == AbilityTargetType.Vector;
            }
            else
            {
                newShouldShow = false;
            }
        }
        else
        {
            newShouldShow = false;
        }
        
        if (ShouldShowJoyStickUI != newShouldShow)
        {
            ShouldShowJoyStickUI = newShouldShow;
            ShouldShowUI = newShouldShow;
            UpdateUIVisibility();
        }
    }
    
    /// <summary>
    /// 获取当前技能的范围
    /// </summary>
    private float AbilityRange => ability is AbilityExecute abilityExecute ? abilityExecute.Range : 0f;
    
    /// <summary>
    /// 获取当前技能的目标类型
    /// </summary>
    private AbilityTargetType AbilityTargetType => ability?.Cache.TargetType ?? AbilityTargetType.None;

    /// <summary>
    /// 绑定的停止施法按钮
    /// </summary>
    public StopCastingButton? StopCastingButton
    {
        get => stopCastingButton;
        set
        {
            stopCastingButton = value;
        }
    }

    /// <summary>
    /// 初始化CastingJoyStick实例
    /// </summary>
    public CastingJoyStick()
    {
        BackgroundImage = DefaultBackground.Path;
        IsRotationFollow = true;
        OnJoystickPressed += OnPressed;
        OnJoystickReleased += OnReleased;
        OnJoystickMove += OnMove;
        
        // 初始状态不思考，等待获取控制权后启动
        ((IThinker)this).DoesThink = false;
        
        // 初始化UI可见性
        UpdateUIVisibility();
    }


    /// <summary>
    /// 设置按下状态
    /// </summary>
    public void SetPressed(PointerButtons pointerButtons)
    {
        OnPressed(this, new JoystickPressedEventArgs(pointerButtons));
        // 按下模式
        IsKeyboardActivated = false;
        
        this.Activate(pointerButtons);
    }

    /// <summary>
    /// 设置键盘激活状态
    /// </summary>
    public void SetKeyboardPressed()
    {
        OnPressed(this, new JoystickPressedEventArgs(PointerButtons.None));
        // 键盘激活模式
        IsKeyboardActivated = true;
        // 直接设置基础摇杆状态，不激活UI
        this.IsPressed = true;
        this.index = null;
        
        // 键盘模式下如果已经有控制权且需要目标选择，启动思考
        if (indicatorOwner == this && ShouldShowJoyStickUI)
        {
            ((IThinker)this).DoesThink = true;
        }
    }

    /// <summary>
    /// 设置释放状态
    /// </summary>
    public void SetReleased()
    {
        var currentPointer = this.CurrentPointer ?? PointerButtons.None;
        
        // 清除键盘激活标志
        IsKeyboardActivated = false;
        
        // 重置摇杆移动标志
        hasMovedInJoystickMode = false;
        
        // 停止思考
        ((IThinker)this).DoesThink = false;
        
        this.Deactivate();
        OnReleased(this, new JoystickReleasedEventArgs(currentPointer));
    }

    internal void OnStopCastingInternal()
    {
        BackgroundImage = DefaultBackgroundDisable.Path;
        
        if (ability is not null && indicatorOwner == this)
        {
            // 只有拥有控制权的实例才销毁指示器
            ReleaseIndicatorControl();
            UpdateUnitHighlight(null);
        }
        
        OnStopCasting?.Invoke(this, EventArgs.Empty);
    }

    internal void OnStopCastingEndInternal()
    {
        BackgroundImage = DefaultBackground.Path;
        if (ability is not null)
        {
            // 夺取指示器控制权并重新创建
            TakeIndicatorControl();
            var target = this.target as ITarget;
            if(target is not null)
            {
                globalTargetingIndicator?.UpdateCursorTarget(target);
            }
        }
        
        OnStopCastingEnd?.Invoke(this, EventArgs.Empty);
    }

    private void OnPressed(object? sender, JoystickPressedEventArgs e)
    {
        if (ability == null || IsActivated) return;
        // 夺取指示器控制权
        TakeIndicatorControl();
        
        if (stopCastingButton != null)
        {
            stopCastingButton.Visible = true;
            stopCastingButton.CurrentCastingJoyStick = this;
        }
    }

    private void OnReleased(object? sender, JoystickReleasedEventArgs e)
    {
        if (ability == null) return;
        
        // 只有拥有控制权的实例才能销毁指示器
        if (indicatorOwner == this)
        {
            ReleaseIndicatorControl();
        }

        BackgroundImage = DefaultBackground.Path;
        
        UpdateUnitHighlight(null);
        
        bool shouldCancel = stopCastingButton?.IsActive ?? false;
        Game.Logger.LogInformation("@zzh stopCastingButton: {stopCastingButton}", stopCastingButton?.IsActive);
        if (shouldCancel)
        {
            // 取消施法
        }
        else
        {
            Cast();
        }
        
        // 施法结束后重置移动标志
        hasMovedInJoystickMode = false;
        
        if (stopCastingButton != null)
        {
            stopCastingButton.Visible = false;
            stopCastingButton.CurrentCastingJoyStick = null;
        }
    }

    private void OnMove(object? sender, JoystickMoveEventArgs e)
    {
        if (!ShouldShowJoyStickUI)
        {
            // 简化模式：不处理移动事件
            return;
        }
        
        // 复杂模式：处理目标选择
        lastPosition = e.Position;
        
        // 在摇杆按下施法模式（非键盘模式）时，标记已移动
        if (!IsKeyboardActivated)
        {
            hasMovedInJoystickMode = true;
        }
        
        UpdateTargetingIndicator();
    }

    private Unit? FindNearestValidUnit(ScenePoint targetPosition)
    {
        if (ability is not AbilityExecute || host == null) return null;

        float searchRadius = AbilityRange;
        var scene = targetPosition.Scene;
        if (scene == null) return null;

        var nearbyEntities = scene.SearchCircle(host.Position, searchRadius, (e) => {
            if (e is not Unit unit)
                return false;

            // 获取目标筛选器
            if (ability is AbilityExecute abilityExecute)
            {
                var targetingFilter = abilityExecute.Cache.AcquireSettings.TargetingFilters;
                if (targetingFilter == null)
                    return true;
                return targetingFilter.Pass(host, unit);
            }
            
            // 对于普通 Ability，暂时返回 true（无筛选）
            return true;
        });
        if (nearbyEntities == null) return null;

        Unit? nearestUnit = null;
        float nearestDistanceSq = float.MaxValue;

        foreach (var entity in nearbyEntities)
        {
            if (entity is not Unit unit) continue;
            float dx = unit.Position.X - targetPosition.X;
            float dy = unit.Position.Y - targetPosition.Y;
            float distanceSq = dx * dx + dy * dy;

            if (distanceSq < nearestDistanceSq)
            {
                nearestDistanceSq = distanceSq;
                nearestUnit = unit;
            }
        }

        return nearestUnit;
    }

    private void UpdateUnitHighlight(Unit? newTarget)
    {
        if (highlightedUnit == newTarget) return;

        if (highlightActor != null)
        {
            highlightActor.Destroy();
            highlightActor = null;
        }

        highlightedUnit = newTarget;

        if (highlightedUnit != null && ScopeData.Actor.AbilityJoyStickTargetingHighlight.Data != null)
        {
            highlightActor = ScopeData.Actor.AbilityJoyStickTargetingHighlight.Data.CreateActor(highlightedUnit);
        }
    }

    private void UpdateTargetingIndicator()
    {
        // 只有AbilityExecute类型且拥有控制权的实例才能更新指示器
        if (ability is not AbilityExecute || indicatorOwner != this) return;
        if (host == null) return;

        ScenePoint unitPosition = host.Position;
        lastHostPosition = unitPosition;

        ScenePoint targetPosition;
        
        // 根据操作类型选择不同的目标计算逻辑
        if (IsKeyboardActivated)
        {
            // 键盘激活模式：使用鼠标屏幕坐标转世界坐标（类似Lua中的摇杆模式）
            targetPosition = CalculateKeyboardTargetPosition(unitPosition);
        }
        else
        {
            // 鼠标模式：基于摇杆偏移计算，考虑镜头角度
            float scaleX = lastPosition.Left / (this.ActualSize.Width / 2);
            float scaleY = lastPosition.Top / (this.ActualSize.Height / 2);
            float abilityRange = AbilityRange;
            
            // 获取镜头角度偏移
            float cameraAngleOffset = GetCameraAngleOffset();
            
            // 应用镜头角度补偿
            float cosAngle = (float)Math.Cos(cameraAngleOffset * Math.PI / 180.0);
            float sinAngle = (float)Math.Sin(cameraAngleOffset * Math.PI / 180.0);
            
            // 旋转坐标系
            float adjustedX = scaleX * cosAngle - scaleY * sinAngle;
            float adjustedY = scaleX * sinAngle + scaleY * cosAngle;
            
            targetPosition = new ScenePoint(
                unitPosition.X + abilityRange * adjustedX, 
                unitPosition.Y + abilityRange * adjustedY, 
                unitPosition.Scene)
            {
                Z = unitPosition.GetGroundHeight(),
            };
        }

        if (AbilityTargetType == AbilityTargetType.Unit)
        {
            Unit? nearestUnit = FindNearestValidUnit(targetPosition);
            UpdateUnitHighlight(nearestUnit);
            
            if (nearestUnit != null)
            {
                // 只有在摇杆按下施法模式且已移动时，或者键盘激活模式时，才更新target
                if (IsKeyboardActivated || (!IsKeyboardActivated && hasMovedInJoystickMode))
                {
                    target = nearestUnit;
                }
                globalTargetingIndicator?.UpdateCursorTarget(nearestUnit.Position);
            }
            else
            {
                // 只有在摇杆按下施法模式且已移动时，或者键盘激活模式时，才更新target
                if (IsKeyboardActivated || (!IsKeyboardActivated && hasMovedInJoystickMode))
                {
                    target = targetPosition;
                }
                globalTargetingIndicator?.UpdateCursorTarget(targetPosition);
            }
        }
        else
        {
            UpdateUnitHighlight(null);
            // 只有在摇杆按下施法模式且已移动时，或者键盘激活模式时，才更新target
            if (IsKeyboardActivated || (!IsKeyboardActivated && hasMovedInJoystickMode))
            {
                target = targetPosition;
            }
            globalTargetingIndicator?.UpdateCursorTarget(targetPosition);
        }
    }

    /// <summary>
    /// 夺取指示器控制权
    /// </summary>
    private void TakeIndicatorControl()
    {
        // 如果之前有其他实例拥有控制权，先让它停止思考
        if (indicatorOwner != null && indicatorOwner != this)
        {
            ((IThinker)indicatorOwner).DoesThink = false;
        }
        
        // 销毁现有指示器
        globalTargetingIndicator?.Destroy();
        globalTargetingIndicator = null;
        
        if (ability == null) return;
        
        // 设置当前实例为控制者
        indicatorOwner = this;
        
        // 只为 AbilityExecute 创建指示器并启动思考
        if (ability is AbilityExecute abilityExecute)
        {
            globalTargetingIndicator = TargetingIndicator.CreateFromAbility(abilityExecute);
            ((IThinker)this).DoesThink = true;
        }
        // 普通技能不需要指示器和思考，但仍需要控制权
    }
    
    /// <summary>
    /// 释放指示器控制权
    /// </summary>
    private void ReleaseIndicatorControl()
    {
        if (indicatorOwner == this)
        {
            ((IThinker)this).DoesThink = false;
            globalTargetingIndicator?.Destroy();
            globalTargetingIndicator = null;
            indicatorOwner = null;
        }
    }
    
    /// <summary>
    /// IThinker接口实现，在按下期间持续更新目标指示器位置
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public void Think(int delta)
    {
        // 只为AbilityExecute类型持续更新目标指示器，确保指示器跟随鼠标移动
        if (ability is AbilityExecute && indicatorOwner == this)
        {
            UpdateTargetingIndicator();
        }
    }
    
    /// <summary>
    /// 计算键盘激活模式下的目标位置（基于Lua逻辑）
    /// </summary>
    private ScenePoint CalculateKeyboardTargetPosition(ScenePoint unitPosition)
    {
        // 获取当前鼠标位置（屏幕坐标）
        var mousePosition = DeviceInfo.PrimaryViewport.GetPointerInputPosition(PointerButtons.None);
        if (mousePosition == null) 
        {
            Game.Logger.LogError("键盘模式下无法获取鼠标位置");
            return unitPosition;
        }

        try
        {
            // 使用API将屏幕坐标转换为世界坐标
            var viewport = DeviceInfo.PrimaryViewport;
            var raycastResult = viewport.RaycastWorldPanel(
                new UIPosition(mousePosition.Value.Left, mousePosition.Value.Top), 
                GameUI.Device.Enum.WorldPanel.XY,  // 使用XY平面（2D游戏常用）
                unitPosition.Z  // 固定Z轴为单位的Z坐标
            );
            
            if (raycastResult.IsHit)
            {
                var worldMousePos = raycastResult.Position;
                return new ScenePoint(
                     worldMousePos.X,
                     worldMousePos.Y,
                     unitPosition.Scene)
                {
                    Z = worldMousePos.Z,
                };
            }
            else
            {
                Game.Logger.LogError("键盘模式下raycast失败，无法转换屏幕坐标到世界坐标");
                return unitPosition;
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "键盘模式坐标转换失败：{ex}", ex.Message);
            return unitPosition;
        }
    }
    
    /// <summary>
    /// 获取镜头角度偏移量（基于当前活跃镜头的旋转角度）
    /// </summary>
    private float GetCameraAngleOffset()
    {
        try
        {
            // 获取当前活跃的镜头
            var camera = DeviceInfo.PrimaryViewport.Camera;
            if (camera == null) return 0f;
            
            // 获取镜头的Transform信息
            var cameraTransform = camera.Transform;
            
            // 获取Y轴旋转角度（通常用于水平旋转）
            // CameraRotation.Yaw 表示围绕Y轴的旋转（水平旋转）
            float yawAngle = cameraTransform.Rotation.Yaw;
            
            // 返回Y轴旋转角度作为偏移量
            // 注意：可能需要根据具体的坐标系调整角度
            // Game.Logger.LogInformation("获取当前镜头角度：{yawAngle}", yawAngle);
            return yawAngle + 90;
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("获取当前镜头角度失败：{ex}", ex.Message);
            return 0f;
        }
    }

    // 施法摇杆的冷却时施法提前量（毫秒） - 当剩余冷却时间小于此值时，允许提前施法
    public TimeSpan CooldownThreshold = TimeSpan.FromMilliseconds(67); // 2帧时间 (30fps)
    // 备选阈值参考：
    // TimeSpan.FromMilliseconds(33);  // 1帧时间 - 适用于高精度操作
    // TimeSpan.FromMilliseconds(50);  // MOBA游戏常用值 - 平衡流畅度和精确性  
    // TimeSpan.FromMilliseconds(100); // RTS游戏常用值 - 更宽松的容错
    
    private bool checkCooldown()
    {
        // 检查是否为有冷却系统的技能
        if (ability is not AbilityActive abilityActive)
            return true; // 无冷却系统的技能直接允许施法
            
        if (abilityActive.Cooldown == null)
            return true;

        var remainingTime = abilityActive.Cooldown.RemainingTime;
        if (remainingTime == null)
            return true;

        // 对于AbilityExecute类型，支持高级的冷却队列功能
        if (abilityActive is AbilityExecute abilityExecute && 
            abilityExecute.Cache.AbilityActiveFlags.AllowEnqueueInCooldown && 
            remainingTime > TimeSpan.Zero && 
            remainingTime <= CooldownThreshold)
        {
            return true;
        }
        
        return remainingTime <= TimeSpan.Zero;
    }

    private bool checkChargeCooldown()
    {
        // 检查是否为有充能系统的技能
        if (ability is not AbilityActive abilityActive)
            return true; // 无充能系统的技能直接允许施法
            
        if (abilityActive.Charge == null)
            return true;

        // 有层数可用
        if (abilityActive.Charge.Affordable)
            return true;

        var remainingTime = abilityActive.Charge.RemainingTime;
        // 没层数还不在充能
        if (remainingTime == null)
            return false;

        // 对于AbilityExecute类型，支持高级的充能队列功能
        if (abilityActive is AbilityExecute abilityExecute && 
            abilityExecute.Cache.AbilityActiveFlags.AllowEnqueueInCooldown && 
            remainingTime > TimeSpan.Zero && 
            remainingTime <= CooldownThreshold)
        {
            return true;
        }
        
        return remainingTime <= TimeSpan.Zero;
    }

    private void Cast()
    {
        // 检查技能类型并调用对应的施法方法
        if (ability is AbilityExecute)
        {
            CastAbilityExecute();
            return;
        }
        
        if (ability != null)
        {
            CastAbility();
            return;
        }
        
        CmdResultManager.ShowCmdResult(CmdError.AbilityNotFound);
    }
    
    /// <summary>
    /// 施放 AbilityExecute 类型的技能
    /// </summary>
    private void CastAbilityExecute()
    {
        if (ability is not AbilityExecute abilityExecute || !abilityExecute.IsValid)
        {
            CmdResultManager.ShowCmdResult(CmdError.AbilityNotFound);
            return;
        }
        
        var unit = abilityExecute.Host;
        if (unit == null)
        {
            CmdResultManager.ShowCmdResult((CmdError)CmdErrorAbility.AbilityHostNotFound);
            return;
        }
        
        if (!checkCooldown())
        {
            CmdResultManager.ShowCmdResult(CmdError.IsInCooldown);
            return;
        }
        
        if (!checkChargeCooldown())
        {
            CmdResultManager.ShowCmdResult((CmdError)CmdErrorAbility.ChargeIsNotEnough);
            return;
        }
        
        var target_copy = target;
        target = null;
        
        Command? cmd = null;
        // 如果是物品自带技能用use命令
        if (abilityExecute.Item is ItemMod itemMod)
        {
            cmd = new()
            {
                Item = itemMod,
                Target = target_copy,
                Type = ComponentTagEx.InventoryManager,
                Player = Player.LocalPlayer,
                Index = CommandIndexInventory.Use,
            };
        }
        else
        {
            cmd = new()
            {
                AbilityLink = abilityExecute.Link,
                Target = target_copy,
                Flag = CommandFlag.IsUser,
                Player = Player.LocalPlayer,
            };
        }
        
        if (cmd == null)
        {
            Game.Logger.LogError("Failed to create command for AbilityExecute");
            return;
        }
        
        var result = cmd.Value.IssueOrder(unit);
        if (!result.IsSuccess)
        {
            Game.Logger.LogError("Failed to issue order for AbilityExecute: {result}", result);
            CmdResultManager.ShowCmdResult(result);
        }
    }
    
    /// <summary>
    /// 施放普通 Ability 类型的技能（复制自 AbilityJoyStick 的逻辑）
    /// </summary>
    private void CastAbility()
    {
        if (ability == null || !ability.IsValid)
        {
            CmdResultManager.ShowCmdResult(CmdError.AbilityNotFound);
            return;
        }
        
        var unit = ability.Host;
        if (unit == null)
        {
            CmdResultManager.ShowCmdResult((CmdError)CmdErrorAbility.AbilityHostNotFound);
            return;
        }
        
        // 检查冷却时间（如果技能有冷却）
        var abilityActive = ability as AbilityActive;
        if (abilityActive != null)
        {
            // 检查普通冷却时间
            if (abilityActive.Cooldown != null)
            {
                var remainingTime = abilityActive.Cooldown.RemainingTime;
                if (remainingTime != null && remainingTime > TimeSpan.Zero)
                {
                    CmdResultManager.ShowCmdResult(CmdError.IsInCooldown);
                    return;
                }
            }
            
            // 检查充能（如果技能有充能系统）
            if (abilityActive.Charge != null && !abilityActive.Charge.Affordable)
            {
                CmdResultManager.ShowCmdResult((CmdError)CmdErrorAbility.ChargeIsNotEnough);
                return;
            }
        }
        
        Command cmd = new()
        {
            AbilityLink = ability.Link,
            Flag = CommandFlag.IsUser,
            Player = Player.LocalPlayer,
        };
        
        // 为切换技能设置正确的命令索引
        if (ability is AbilityToggle abilityToggle)
        {
            // 根据当前状态决定开启或关闭
            cmd.Index = abilityToggle.ToggledOn ? CommandIndex.TurnOff : CommandIndex.TurnOn;
        }
        
        // 如果是需要目标的技能，设置目标
        if (AbilityTargetType != AbilityTargetType.None)
        {
            var target_copy = target;
            if (target_copy != null)
        {
            cmd.Target = target_copy;
        }
            target = null;
        }
        
        var result = cmd.IssueOrder(unit);
        if (!result.IsSuccess)
        {
            Game.Logger.LogError("Failed to issue order for ability {AbilityName}: {result}", ability.Cache?.Name ?? "未知技能", result);
            CmdResultManager.ShowCmdResult(result);
        }
    }

    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        OnJoystickPressed -= OnPressed;
        OnJoystickReleased -= OnReleased;
        OnJoystickMove -= OnMove;
        
        // 停止思考以避免资源泄露
        ((IThinker)this).DoesThink = false;
        
        OnStopCasting = null;
        OnStopCastingEnd = null;
        
        UpdateUnitHighlight(null);
    }
}

#endif