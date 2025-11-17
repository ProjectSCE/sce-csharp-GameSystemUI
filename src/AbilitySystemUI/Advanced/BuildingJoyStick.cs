#if CLIENT
using Events;
using GameCore.AbilitySystem;
using GameCore.AbilitySystem.Data.Enum;
using GameCore.DisplayInfo;
using GameCore.ActorSystem;
using GameCore.ActorSystem.Data;
using GameCore.ActorSystem.Struct;
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameCore.ResourceType;
using GameCore.SceneSystem;
using GameCore.TargetingSystem;
using GameData;
using GameData.Extension;
using GameUI.Control.Primitive;
using GameUI.Device;
using GameUI.Struct;

using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Drawing;
using GameSystemUI.CmdResultSystemUI;

namespace GameSystemUI.AbilitySystemUI.Advanced;

/// <summary>
/// 建造技能摇杆 - 专门用于建造类技能的摇杆控件
/// 支持建造预览、网格对齐、碰撞检测等功能
/// </summary>
public class BuildingJoyStick : Joystick
{
    private static StopCastingButton? stopCastingButton;
    private static Trigger<EventGameTick>? gameTickTrigger;
    private static Action? Updater;

    private Image _defaultBackground = "@gameui/image/施法轮盘.png"u8;
    private Image _defaultBackgroundDisable = "@gameui/image/施法轮盘_禁止.png"u8;

    private AbilityExecute? abilityExecute;
    private UIPosition lastPosition;
    private ICommandTarget? target;
    
    // 建造功能相关字段
    private Actor? buildingPreviewActor;
    private ActorGrid? buildingGridActor; // 建造网格Actor（显示有效/无效建造位置）
    private ScenePoint lastBuildingPosition;
    
    // 🎯 格子位置优化：记录上一次的格子位置，避免频繁检测
    private (int gridX, int gridY)? lastGridPosition;
    
    // 🏗️ 静态碰撞信息缓存系统 - 参考Lua的base.collision_info实现
    private static readonly Dictionary<string, Dictionary<int, Dictionary<int, bool>>> SceneCollisionInfo = new();
    private static readonly object CollisionInfoLock = new();

    /// <summary>
    /// 建筑物建造完成事件 - 用于更新碰撞信息
    /// </summary>
    public static event Action<Unit, ScenePoint, GameCore.CollisionSystem.Data.Struct.Footprint>? OnBuildingCompleted;

    /// <summary>
    /// 停止建造阶段事件 - 进入禁用状态时触发
    /// </summary>
    public event EventHandler? OnStopBuilding;
    
    /// <summary>
    /// 停止建造结束阶段事件 - 恢复正常状态时触发
    /// </summary>
    public event EventHandler? OnStopBuildingEnd;

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
    /// 绑定的技能执行器
    /// </summary>
    public AbilityExecute? AbilityExecute
    {
        get => abilityExecute;
        set
        {
            if (abilityExecute == value) return;
            abilityExecute = value;
        }
    }

    /// <summary>
    /// 技能宿主单位
    /// </summary>
    private Unit? host => abilityExecute?.Host;

    static BuildingJoyStick()
    {
        stopCastingButton = new StopCastingButton(){
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Position = new UIPosition(-100, 100),
        };
        stopCastingButton.AddToVisualTree();
        stopCastingButton.OnPointerEntered += (object? sender, EventArgs e)=>{
            stopCastingButton.IsActive = true;
            // 建造摇杆有自己的停止逻辑
        };
        stopCastingButton.OnPointerExited += (object? sender, EventArgs e)=>{
            stopCastingButton.IsActive = false;
            // 建造摇杆有自己的停止逻辑
        };
        gameTickTrigger = new Trigger<EventGameTick>(async (s, d) =>
        {
            Updater?.Invoke();
            await Task.CompletedTask;
            return true;
        });
        gameTickTrigger.Register(Game.Instance);
    }

    public BuildingJoyStick()
    {
        BackgroundImage = DefaultBackground.Path;
        IsRotationFollow = false; // 建造摇杆不需要旋转跟随
        OnJoystickPressed += OnPressed;
        OnJoystickReleased += OnReleased;
        OnJoystickMove += OnMove;
        
        // 🏗️ 监听建筑完成事件，用于实时更新碰撞信息
        OnBuildingCompleted += (unit, position, footprint) =>
        {
        };
    }

    /// <summary>
    /// 设置按下状态
    /// </summary>
    public void SetPressed(PointerButtons pointerButtons)
    {
        OnPressed(this, new JoystickPressedEventArgs(pointerButtons));
        this.Activate(pointerButtons);
    }

    /// <summary>
    /// 设置键盘激活状态
    /// </summary>
    public void SetKeyboardPressed()
    {
        OnPressed(this, new JoystickPressedEventArgs(PointerButtons.None));
        // 直接设置基础摇杆状态，不激活UI
        this.IsPressed = true;
        this.index = null;
    }

    /// <summary>
    /// 设置释放状态
    /// </summary>
    public void SetReleased()
    {
        var currentPointer = this.CurrentPointer ?? PointerButtons.None;
        this.Deactivate();
        OnReleased(this, new JoystickReleasedEventArgs(currentPointer));
    }

    private void OnStopBuildingInternal()
    {
        BackgroundImage = DefaultBackgroundDisable.Path;
        OnStopBuilding?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopBuildingEndInternal()
    {
        BackgroundImage = DefaultBackground.Path;
        OnStopBuildingEnd?.Invoke(this, EventArgs.Empty);
    }

    private void OnPressed(object? sender, JoystickPressedEventArgs e)
    {
        if (abilityExecute == null || IsActivated) return;
        
        var abilityName = ((IDisplayInfo)abilityExecute).DisplayName ?? abilityExecute.Cache?.Name ?? "";
        
        // 建造技能激活时创建预览
        CreateBuildingPreview();
        
        if (stopCastingButton != null)
        {
            stopCastingButton.Visible = true;
        }
    }

    private void OnReleased(object? sender, JoystickReleasedEventArgs e)
    {
        if (abilityExecute == null) return;
        
        var abilityName = abilityExecute.Cache?.Name ?? "未知技能";
        bool shouldCancel = stopCastingButton?.IsActive ?? false;
        
        BackgroundImage = DefaultBackground.Path;
        
        if (shouldCancel)
        {
            // 取消建造
            CancelBuilding();
        }
        else
        {
            // 确认建造
            Cast();
            
            // 建造确认后，清理预览Actor
            CancelBuilding(); // 复用清理逻辑
        }
        
        if (stopCastingButton != null)
        {
            stopCastingButton.Visible = false;
        }
    }

    private void OnMove(object? sender, JoystickMoveEventArgs e)
    {
        lastPosition = e.Position;
        
        // 建造技能：完全绕过摇杆限制，直接使用鼠标世界坐标
        UpdateBuildingPosition();
    }

    /// <summary>
    /// 判断当前技能是否为建造技能 - 通过类型判断而非名字
    /// </summary>
    private bool IsBuildingAbility()
    {
        if (abilityExecute?.Cache == null) 
        {
            return false;
        }
        
        // 通过类名约定判断：检查类型名是否包含"BuildingAbility"或"AbilityExecuteBuilding"
        var typeName = abilityExecute.Cache.GetType().Name;
        var fullTypeName = abilityExecute.Cache.GetType().FullName;
        bool isBuilding = typeName.Contains("BuildingAbility") || typeName.Contains("AbilityExecuteBuilding");
        
        return isBuilding;
    }

    /// <summary>
    /// 通过反射从建造技能获取建造单位信息
    /// </summary>
    private IGameLink<GameCore.EntitySystem.Data.GameDataUnit>? GetBuildingUnitFromAbility(object abilityCache)
    {
        try
        {
            // 通过反射查找Unit属性
            var unitProperty = abilityCache.GetType().GetProperty("Unit");
            if (unitProperty != null)
            {
                var unitValue = unitProperty.GetValue(abilityCache);
                if (unitValue is IGameLink<GameCore.EntitySystem.Data.GameDataUnit> unitLink)
                {
                    return unitLink;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "⚠️ 反射获取建造单位失败");
            return null;
        }
    }

    /// <summary>
    /// 获取当前建造技能对应的单位Footprint配置
    /// </summary>
    private GameCore.CollisionSystem.Data.Struct.Footprint? GetBuildingUnitFootprint()
    {
        try
        {
            if (abilityExecute?.Cache == null)
            {
                Game.Logger.LogWarning("⚠️ 技能数据表为空，使用默认3x3");
                return CreateDefaultFootprint();
            }

            // 🏗️ 通过类型转换获取建造单位信息
            GameCore.CollisionSystem.Data.Struct.Footprint? footprint = null;
            
            // 通过反射获取建造单位信息
            var buildingUnit = GetBuildingUnitFromAbility(abilityExecute.Cache);
            if (buildingUnit?.Data != null)
            {
                footprint = buildingUnit.Data.Footprint;
            }
            else
            {
                footprint = CreateFootprint(4, 4); // 新塔都是4x4
            }

            return footprint;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 获取建造单位Footprint失败，使用默认3x3");
            return CreateDefaultFootprint();
        }
    }

    /// <summary>
    /// 创建指定大小的Footprint（所有格子都占用）
    /// </summary>
    private GameCore.CollisionSystem.Data.Struct.Footprint CreateFootprint(int width, int height)
    {
        var footprint = new GameCore.CollisionSystem.Data.Struct.Footprint(
            (short)width, (short)height, 
            GameCore.CollisionSystem.Data.Enum.CollisionType.Static);

        // 设置所有格子为占用状态
        for (short x = 0; x < width; x++)
        {
            for (short y = 0; y < height; y++)
            {
                footprint[x, y] = true;
            }
        }

        return footprint;
    }

    /// <summary>
    /// 创建默认的3x3 Footprint
    /// </summary>
    private GameCore.CollisionSystem.Data.Struct.Footprint CreateDefaultFootprint()
    {
        return CreateFootprint(3, 3);
    }

    /// <summary>
    /// 创建建造预览Actor
    /// </summary>
    private void CreateBuildingPreview()
    {
        if (abilityExecute?.Cache == null || host == null) return;

        var abilityName = ((IDisplayInfo)abilityExecute).DisplayName ?? abilityExecute.Cache.Name;
        IGameLink<GameDataActor>? previewActorLink = null;

        // 通过反射获取建造技能的PreviewActor成员
        try
        {
            var previewActorProperty = abilityExecute.Cache.GetType().GetProperty("PreviewActor");
            if (previewActorProperty != null)
            {
                var previewActorValue = previewActorProperty.GetValue(abilityExecute.Cache);
                if (previewActorValue is IGameLink<GameDataActor> actorLink)
                {
                    previewActorLink = actorLink;
                }
            }
            
            if (previewActorLink == null)
            {
                previewActorLink = new GameLink<GameDataActor, GameDataActor>("TowerPreviewActor"u8);
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "⚠️ 反射获取PreviewActor失败，使用默认");
            previewActorLink = new GameLink<GameDataActor, GameDataActor>("TowerPreviewActor"u8);
        }

        if (previewActorLink?.Data != null)
        {
            try
            {
                // 🎭 直接在场景中创建独立的预览Actor（无scope附着）
                var hostScene = host?.Position.Scene;
                var actorData = previewActorLink.Data;
                if (hostScene != null && actorData != null)
                {
                    buildingPreviewActor = actorData.CreateActor(
                        scope: null,           // 无scope，不附着到任何单位
                        skipBirth: false,      // 正常的生命周期
                        scene: hostScene       // 指定场景
                    );
                }
                else
                {
                    Game.Logger.LogError("❌ 无法获取host所在场景或actor数据，跳过预览Actor创建");
                }
                
                if (buildingPreviewActor != null && actorData != null)
                {
                    // 🎯 创建建造网格Actor
                    CreateBuildingGrid();
                    
                    // 立即更新预览位置到鼠标位置
                    UpdateBuildingPreview();
                }
                else
                {
                    Game.Logger.LogError("❌ 独立预览Actor创建失败");
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "❌ 创建建造预览Actor异常");
            }
        }
        else
        {
            Game.Logger.LogError("❌ 预览Actor配置无效: {ActorLink}", previewActorLink?.ToString() ?? "null");
        }
    }

    /// <summary>
    /// 更新建造位置并同时更新预览Actor位置
    /// 建造技能始终使用鼠标当前位置，而不是相对于单位的偏移
    /// 🎯 优化：只在预览模型移动到新格子时才进行所有更新操作
    /// </summary>
    private void UpdateBuildingPosition()
    {
        if (!IsBuildingAbility() || host == null || abilityExecute == null) return;

        try
        {
            ScenePoint unitPosition = host.Position;
            
            // 🏗️ 建造技能：始终使用鼠标当前世界坐标位置
            ScenePoint targetPosition = CalculateMouseWorldPosition(unitPosition);
            
            // 🎯 计算当前所在的格子位置（网格坐标）
            var gridSize = buildingGridActor?.Cache?.GridSize ?? 64f;
            int currentGridX = (int)Math.Floor(targetPosition.X / gridSize);
            int currentGridY = (int)Math.Floor(targetPosition.Y / gridSize);
            var currentGridPosition = (currentGridX, currentGridY);
            
            // 🚀 检查是否移动到了新的格子位置
            bool hasGridChanged = lastGridPosition == null || 
                                 lastGridPosition.Value.gridX != currentGridPosition.currentGridX ||
                                 lastGridPosition.Value.gridY != currentGridPosition.currentGridY;
            
            // 🎯 只在格子位置发生变化时才更新所有内容
            if (hasGridChanged)
            {
                lastGridPosition = currentGridPosition;
                lastBuildingPosition = targetPosition;
                
                // 📍 更新预览Actor位置到新格子
                UpdateBuildingPreviewPosition();
                
                // 🔍 执行碰撞检测和网格视觉更新
                PerformGridValidationAndUpdate();

            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新建造位置失败");
        }
    }
    
    /// <summary>
    /// 计算鼠标当前位置的世界坐标
    /// </summary>
    private ScenePoint CalculateMouseWorldPosition(ScenePoint unitPosition)
    {
        try
        {
            // 获取当前鼠标位置（屏幕坐标）
            var mousePosition = DeviceInfo.PrimaryViewport.GetPointerInputPosition(PointerButtons.None);
            if (mousePosition == null) 
            {
                return unitPosition;
            }

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
                var targetPosition = new ScenePoint(
                     worldMousePos.X,
                     worldMousePos.Y,
                     unitPosition.Scene)
                {
                    Z = worldMousePos.Z,
                };
                
                return targetPosition;
            }
            else
            {
                return unitPosition;
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 计算鼠标世界坐标失败，使用单位位置: {ex}", ex.Message);
            return unitPosition;
        }
    }
    
    /// <summary>
    /// 将坐标对齐到网格中心点（暂时屏蔽，预览模型紧跟鼠标）
    /// </summary>
    private ScenePoint SnapToGrid(ScenePoint position)
    {
        // 🚫 暂时屏蔽网格对齐功能，预览模型紧跟鼠标
        return position;
        
        /*
        // 🎯 只有在建造模式下才进行网格对齐
        if (!IsBuildingAbility() || buildingGridActor?.Cache == null)
        {
            // 非建造模式或网格未创建时，直接返回原始位置
            return position;
        }
        
        // 🎯 动态获取网格大小
        float gridSize = buildingGridActor.Cache.GridSize;
        float gridCenter = gridSize / 2f;  // 网格中心偏移
        
        // 计算对齐后的坐标：将坐标对齐到最近的网格中心点
        float snappedX = (float)(Math.Floor(position.X / gridSize) * gridSize + gridCenter);
        float snappedY = (float)(Math.Floor(position.Y / gridSize) * gridSize + gridCenter);
        
        return new ScenePoint(snappedX, snappedY, position.Scene)
        {
            Z = position.Z
        };
        */
    }

    /// <summary>
    /// 更新建造预览位置
    /// </summary>
    private void UpdateBuildingPreview()
    {
        if (!IsBuildingAbility()) return;

        try
        {
            // 🏗️ 建造预览：使用鼠标当前世界坐标位置
            if (host != null && abilityExecute != null)
            {
                ScenePoint unitPosition = host.Position;
                
                // 🖱️ 始终使用鼠标当前位置的世界坐标
                ScenePoint rawTargetPosition = CalculateMouseWorldPosition(unitPosition);
                
                // 🎯 预览模型紧跟鼠标（SnapToGrid已屏蔽）
                ScenePoint targetPosition = SnapToGrid(rawTargetPosition);
                
                lastBuildingPosition = targetPosition;

                // 🎭 更新预览Actor位置到鼠标世界坐标
                if (buildingPreviewActor != null)
                {
                    try
                    {
                        // 检查Actor是否仍然有效
                        if (buildingPreviewActor.IsValid)
                        {
                            // 将鼠标世界坐标设置给预览Actor
                            buildingPreviewActor.Position = targetPosition;
                        }
                        else
                        {
                            buildingPreviewActor = null; // 清除无效引用
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.Logger.LogError(ex, "❌ 更新预览Actor位置失败");
                    }
                }
                
                // 检查位置有效性并更新预览外观
                bool isValidPosition = IsValidBuildingPosition(lastBuildingPosition);
                UpdateBuildingPreviewVisuals(isValidPosition);
                
                // 🎯 通过actor position更新网格位置到预览模型所在位置
                UpdateBuildingGridPosition();
                
                // 更新网格视觉效果
                UpdateBuildingGridVisuals(isValidPosition);
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新建造预览位置失败");
        }
    }
    
    /// <summary>
    /// 🎯 更新预览Actor位置到新格子位置（仅在格子变化时调用）
    /// </summary>
    private void UpdateBuildingPreviewPosition()
    {
        if (!IsBuildingAbility() || buildingPreviewActor == null) return;

        try
        {
            // 🎯 预览模型移动到格子位置（SnapToGrid已屏蔽）
            ScenePoint targetPosition = SnapToGrid(lastBuildingPosition);

            // 🎭 更新预览Actor位置到格子坐标
            if (buildingPreviewActor.IsValid)
            {
                // 将格子坐标设置给预览Actor
                buildingPreviewActor.Position = targetPosition;
            }
            else
            {
                buildingPreviewActor = null; // 清除无效引用
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新预览Actor位置失败");
        }
    }
    
    /// <summary>
    /// 🔍 执行碰撞检测和网格更新（仅在格子位置变化时调用）
    /// </summary>
    private void PerformGridValidationAndUpdate()
    {
        if (!IsBuildingAbility()) return;

        try
        {
            // 检查位置有效性并更新预览外观
            bool isValidPosition = IsValidBuildingPosition(lastBuildingPosition);
            UpdateBuildingPreviewVisuals(isValidPosition);
            
            // 🎯 通过actor position更新网格位置到预览模型所在位置
            UpdateBuildingGridPosition();
            
            // 更新网格视觉效果
            UpdateBuildingGridVisuals(isValidPosition);
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 执行网格验证和更新失败");
        }
    }

    /// <summary>
    /// 检查建造位置是否有效（整体位置检查）
    /// </summary>
    private bool IsValidBuildingPosition(ScenePoint position)
    {
        if (host == null || abilityExecute == null) return false;

        try
        {
            // 1. 基础范围检查
            float range = abilityExecute.Range;
            float distance = Vector3.Distance(
                new Vector3(host.Position.X, host.Position.Y, host.Position.Z), 
                new Vector3(position.X, position.Y, position.Z));
            
            if (distance > range)
            {
                return false;
            }

            // 2. 检查建造中心位置的静态碰撞
            if (!CheckStaticCollisionMask(position))
            {
                return false;
            }

            // 3. 建筑物碰撞检测 - 检查所有要占用的格子（包含每个格子的静态碰撞检测）
            if (!CheckBuildingCollision(position))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 检查建造位置有效性时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 检查建筑物碰撞 - 确保要建造的所有格子都没有被现有建筑占用
    /// </summary>
    private bool CheckBuildingCollision(ScenePoint position)
    {
        try
        {
            // 获取当前建造单位的Footprint
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogWarning("⚠️ 无法获取建造单位Footprint，跳过碰撞检测");
                return true;
            }

            var scene = position.Scene;
            if (scene == null)
            {
                Game.Logger.LogWarning("⚠️ 场景为空，跳过碰撞检测");
                return true;
            }

            // 获取网格大小（假设为64，实际应该从配置获取）
            float gridSize = buildingGridActor?.Cache?.GridSize ?? 64f;

            // 计算要检查的格子范围
            int width = footprint.Width;
            int height = footprint.Height;
            
            // 计算起始格子位置（以建造位置为中心）
            int startX = -(width / 2);
            int startY = -(height / 2);

            // 检查每个要占用的格子
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 检查Footprint中这个位置是否被占用
                    if (!footprint[(short)x, (short)y]) continue;

                    // 计算这个格子的世界坐标
                    float cellWorldX = position.X + (startX + x) * gridSize;
                    float cellWorldY = position.Y + (startY + y) * gridSize;
                    var cellPosition = new ScenePoint(cellWorldX, cellWorldY, scene)
                    {
                        Z = position.Z
                    };

                    // 检查这个格子是否与现有建筑冲突
                    if (!IsValidSingleBuildingCell(cellPosition, gridSize))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 建筑碰撞检测异常");
            return false;
        }
    }

    /// <summary>
    /// 检查单个格子是否适合建造 - 单格子碰撞检测入口
    /// </summary>
    private bool IsValidSingleBuildingCell(ScenePoint cellPosition, float gridSize)
    {
        try
        {
            var scene = cellPosition.Scene;
            if (scene == null) return false;

            // 🎯 使用完整的单格子有效性检查（包括建筑物碰撞 + 静态碰撞检测）
            return CheckSingleCellValidity(cellPosition, gridSize);
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 单格子检测异常: ({X:F1},{Y:F1}) - {Ex}", 
                cellPosition.X, cellPosition.Y, ex.Message);
            // 异常时假设无效，更安全的策略
            return false;
        }
    }

    /// <summary>
    /// 检查与现有建筑物的冲突 - 专注于建筑物碰撞检测，先搜索单位确保建筑存在，再使用缓存优化性能
    /// </summary>
    private bool CheckBuildingConflicts(ScenePoint cellPosition, float gridSize)
    {
        try
        {
            var scene = cellPosition.Scene;
            if (scene == null) return false;

            // 🔍 Step 1: 搜索附近的建筑物 - 确保建筑确实存在且有有效footprint
            float searchRadius = gridSize * 1.5f; // 适当扩大搜索半径确保不遗漏
            var nearbyEntities = scene.SearchCircle(cellPosition, searchRadius, (entity) =>
            {
                // 只检查单位类型的实体
                if (entity is not Unit unit) return false;
                
                // 排除自己（建造者）
                if (unit == host) return false;
                
                // 🎯 关键改进：排除已死亡或无效的单位
                if (unit.HasState(GameCore.BaseType.UnitState.Dead)) return false;
                
                // 🎯 关键改进：检查单位是否真的还存在（有有效的位置和缓存）
                if (!unit.IsValid || unit.Cache == null) return false;

                // 🎯 关键改进：尝试获取单位的footprint，如果没有footprint则认为不是建筑物或已被拆除
                var unitFootprint = GetUnitFootprint(unit);
                if (unitFootprint == null)
                {
                    return false; // 没有footprint说明可能已被拆除
                }

                return true; // 确认是有效的建筑物
            });

            if (nearbyEntities != null && nearbyEntities.Any())
            {
                // 🔍 对每个确认存在的建筑物，使用缓存数据检测footprint冲突
                foreach (var entity in nearbyEntities)
                {
                    if (entity is Unit conflictingUnit)
                    {
                        // 再次验证单位确实存在且有效
                        if (!conflictingUnit.IsValid || conflictingUnit.Cache == null)
                        {
                            continue;
                        }

                        // 🚀 建筑确实存在，使用缓存数据检查footprint冲突
                        if (CheckBuildingFootprintConflictWithCache(conflictingUnit, cellPosition, gridSize))
                        {
                            return false;
                        }
                    }
                }
            }

            return true; // 没有发现建筑物冲突
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 建筑物冲突检测异常: ({X:F1},{Y:F1}) - {Ex}", 
                cellPosition.X, cellPosition.Y, ex.Message);
            return false; // 异常时假设有冲突，更安全的策略
        }
    }

    /// <summary>
    /// 检查建筑物footprint是否与指定格子冲突 - 直接使用缓存数据，提高性能
    /// </summary>
    private bool CheckBuildingFootprintConflictWithCache(Unit buildingUnit, ScenePoint cellPosition, float gridSize)
    {
        try
        {
            // 🎯 首先严格验证建筑单位是否真的存在
            if (buildingUnit == null || !buildingUnit.IsValid)
            {
                return false; // 建筑不存在，无冲突
            }

            // 🎯 检查建筑是否已死亡或被销毁
            if (buildingUnit.HasState(GameCore.BaseType.UnitState.Dead))
            {
                return false; // 已死亡的建筑不产生冲突
            }

            // 🎯 验证建筑位置是否有效
            ScenePoint buildingPosition;
            try
            {
                buildingPosition = buildingUnit.Position;
                if (buildingPosition.Scene == null)
                {
                    return false; // 位置无效的建筑不产生冲突
                }
            }
            catch (Exception posEx)
            {
                Game.Logger.LogDebug("🚫 建筑 {BuildingName} 访问位置失败: {Ex}，可能已被拆除", 
                    buildingUnit.Cache?.Name ?? "未知建筑", posEx.Message);
                return false; // 无法访问位置的建筑不产生冲突
            }

            // 🚀 直接使用缓存中的footprint数据进行碰撞检测
            return CheckCachedCollisionInfo(buildingPosition.Scene, cellPosition, gridSize);
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 建筑物footprint检测异常: {BuildingName} - {Ex}", 
                buildingUnit?.Cache?.Name ?? "未知建筑", ex.Message);
            
            // 🎯 异常时假设建筑不存在，无冲突
            Game.Logger.LogDebug("🚫 检测异常，假设建筑不存在，无冲突");
            return false;
        }
    }



    /// <summary>
    /// 检查建筑物footprint是否与指定格子冲突 - 精确的格子级别碰撞检测，严格验证建筑存在性（保留原方法以备用）
    /// </summary>
    private bool CheckBuildingFootprintConflict(Unit buildingUnit, ScenePoint cellPosition, float gridSize)
    {
        try
        {
            // 🎯 首先严格验证建筑单位是否真的存在
            if (buildingUnit == null || !buildingUnit.IsValid)
            {
                return false; // 建筑不存在，无冲突
            }

            // 🎯 检查建筑是否已死亡或被销毁
            if (buildingUnit.HasState(GameCore.BaseType.UnitState.Dead))
            {
                Game.Logger.LogDebug("🚫 建筑 {BuildingName} 已死亡，跳过footprint检测", 
                    buildingUnit.Cache?.Name ?? "未知建筑");
                return false; // 已死亡的建筑不产生冲突
            }

            // 🎯 验证建筑位置是否有效
            ScenePoint buildingPosition;
            try
            {
                buildingPosition = buildingUnit.Position;
                if (buildingPosition.Scene == null)
                {
                    Game.Logger.LogDebug("🚫 建筑 {BuildingName} 位置无效，可能已被拆除", 
                        buildingUnit.Cache?.Name ?? "未知建筑");
                    return false; // 位置无效的建筑不产生冲突
                }
            }
            catch (Exception posEx)
            {
                Game.Logger.LogDebug("🚫 建筑 {BuildingName} 访问位置失败: {Ex}，可能已被拆除", 
                    buildingUnit.Cache?.Name ?? "未知建筑", posEx.Message);
                return false; // 无法访问位置的建筑不产生冲突
            }

            // 🏗️ 尝试获取建筑物的footprint信息（使用改进的严格验证方法）
            var buildingFootprint = GetUnitFootprint(buildingUnit);
            if (buildingFootprint == null)
            {
                Game.Logger.LogDebug("🚫 建筑 {BuildingName} 无footprint，可能已被拆除或不是建筑物", 
                    buildingUnit.Cache?.Name ?? "未知建筑");
                return false; // 没有footprint说明建筑不存在或已被拆除
            }

            // 🎯 精确的footprint检测
            int buildingWidth = buildingFootprint.Width;
            int buildingHeight = buildingFootprint.Height;
            
            // 计算建筑物占用的格子范围
            int buildingStartX = -(buildingWidth / 2);
            int buildingStartY = -(buildingHeight / 2);
            
            // 计算要检查的格子相对于建筑物的位置
            float relativeX = cellPosition.X - buildingPosition.X;
            float relativeY = cellPosition.Y - buildingPosition.Y;
            
            // 将相对位置转换为格子坐标
            int gridX = (int)Math.Round(relativeX / gridSize) - buildingStartX;
            int gridY = (int)Math.Round(relativeY / gridSize) - buildingStartY;
            
            // 检查格子坐标是否在footprint范围内
            if (gridX >= 0 && gridX < buildingWidth && gridY >= 0 && gridY < buildingHeight)
            {
                // 检查footprint中这个位置是否被占用
                bool isOccupied = buildingFootprint[(short)gridX, (short)gridY];
                
                if (isOccupied)
                {
                    Game.Logger.LogDebug("🔍 精确footprint检测冲突: 存在的建筑物={BuildingName}, " +
                        "格子({CellX:F1},{CellY:F1}) 对应footprint位置({GridX},{GridY})", 
                        buildingUnit.Cache?.Name ?? "未知建筑", 
                        cellPosition.X, cellPosition.Y, gridX, gridY);
                    return true; // 确认存在冲突
                }
            }
            
            Game.Logger.LogDebug("✅ footprint检测无冲突: 建筑物={BuildingName}, 格子({CellX:F1},{CellY:F1})", 
                buildingUnit.Cache?.Name ?? "未知建筑", cellPosition.X, cellPosition.Y);
            return false;
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 建筑物footprint检测异常: {BuildingName} - {Ex}", 
                buildingUnit?.Cache?.Name ?? "未知建筑", ex.Message);
            
            // 🎯 异常时不使用距离检测作为回退，直接返回false
            // 因为异常通常意味着建筑已不存在
            Game.Logger.LogDebug("🚫 检测异常，假设建筑不存在，无冲突");
            return false;
        }
    }

    /// <summary>
    /// 获取其他单位的footprint信息 - 严格验证单位是否真的存在且有有效footprint
    /// </summary>
    private GameCore.CollisionSystem.Data.Struct.Footprint? GetUnitFootprint(Unit unit)
    {
        try
        {
            // 🎯 首先严格验证单位是否真的存在
            if (unit == null || !unit.IsValid)
            {
                Game.Logger.LogDebug("🚫 单位为空或无效，无footprint");
                return null;
            }

            // 🎯 检查单位是否已死亡或被销毁
            if (unit.HasState(GameCore.BaseType.UnitState.Dead))
            {
                Game.Logger.LogDebug("🚫 单位 {UnitName} 已死亡，无footprint", 
                    unit.Cache?.Name ?? "未知单位");
                return null;
            }

            // 🔍 尝试从单位缓存中获取footprint信息
            var unitCache = unit.Cache;
            if (unitCache == null)
            {
                Game.Logger.LogDebug("🚫 单位 {UnitId} 缓存为空，可能已被拆除", unit.GetHashCode());
                return null;
            }

            // 🎯 尝试通过反射获取实际的Footprint属性（如果存在）
            try
            {
                var footprintProperty = unitCache.GetType().GetProperty("Footprint");
                if (footprintProperty != null)
                {
                    var footprintValue = footprintProperty.GetValue(unitCache);
                    if (footprintValue is GameCore.CollisionSystem.Data.Struct.Footprint actualFootprint)
                    {
                        Game.Logger.LogDebug("✅ 从单位缓存获取到实际footprint: {UnitName} {Width}x{Height}", 
                            unitCache.Name, actualFootprint.Width, actualFootprint.Height);
                        return actualFootprint;
                    }
                }
            }
            catch (Exception reflectionEx)
            {
                Game.Logger.LogDebug("🔍 反射获取footprint失败: {Ex}", reflectionEx.Message);
            }

            // 🎯 检查单位位置是否有效（确认建筑物真的存在于世界中）
            try
            {
                var position = unit.Position;
                if (position.Scene == null)
                {
                    Game.Logger.LogDebug("🚫 单位 {UnitName} 位置无效，可能已被拆除", unitCache.Name);
                    return null;
                }
            }
            catch (Exception posEx)
            {
                Game.Logger.LogDebug("🚫 单位 {UnitName} 访问位置失败: {Ex}，可能已被拆除", 
                    unitCache.Name, posEx.Message);
                return null;
            }
            
            // 🏗️ 根据单位类型推测footprint（备选方案）
            var unitName = unitCache.Name ?? "";
            if (unitName.Contains("Tower") || unitName.Contains("防御塔"))
            {
                Game.Logger.LogDebug("🔍 推测防御塔footprint: {UnitName} 4x4", unitName);
                return CreateFootprint(4, 4); // 防御塔4x4
            }
            else if (unitName.Contains("Wall") || unitName.Contains("城墙"))
            {
                Game.Logger.LogDebug("🔍 推测城墙footprint: {UnitName} 3x3", unitName);
                return CreateFootprint(3, 3); // 城墙3x3
            }
            else if (unitName.Contains("Building") || unitName.Contains("建筑"))
            {
                Game.Logger.LogDebug("🔍 推测建筑footprint: {UnitName} 2x2", unitName);
                return CreateFootprint(2, 2); // 一般建筑2x2
            }
            else if (unitName.Contains("House") || unitName.Contains("房屋"))
            {
                Game.Logger.LogDebug("🔍 推测房屋footprint: {UnitName} 2x2", unitName);
                return CreateFootprint(2, 2); // 房屋2x2
            }
            else if (unitName.Contains("Barracks") || unitName.Contains("兵营"))
            {
                Game.Logger.LogDebug("🔍 推测兵营footprint: {UnitName} 3x3", unitName);
                return CreateFootprint(3, 3); // 兵营3x3
            }
            
            // 🔍 使用碰撞半径作为最后的备选方案
            var collisionRadius = GetUnitCollisionRadius(unit);
            if (collisionRadius > 0)
            {
                // 根据碰撞半径估算footprint大小
                int estimatedSize = Math.Max(1, (int)Math.Ceiling(collisionRadius / 32f)); // 假设32为单格大小
                estimatedSize = Math.Min(estimatedSize, 6); // 限制最大大小
                Game.Logger.LogDebug("🔍 根据碰撞半径推测footprint: {UnitName} {Size}x{Size} (半径={Radius})", 
                    unitName, estimatedSize, estimatedSize, collisionRadius);
                return CreateFootprint(estimatedSize, estimatedSize);
            }
            
            // 🚫 如果无法确定footprint，说明可能不是建筑物或已被拆除
            Game.Logger.LogDebug("🚫 无法确定单位footprint: {UnitName}，可能不是建筑物或已被拆除", unitName);
            return null;
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 获取单位footprint失败: {UnitName} - {Ex}", 
                unit?.Cache?.Name ?? "未知单位", ex.Message);
            return null; // 异常时返回null，表示建筑不存在
        }
    }

    /// <summary>
    /// 解析footprint字符串（参考Lua的footpoint_to_map函数）
    /// </summary>
    private GameCore.CollisionSystem.Data.Struct.Footprint? ParseFootprintString(string footprintStr)
    {
        try
        {
            // 🔧 简化实现：解析 '■' 和 '○' 字符
            if (string.IsNullOrEmpty(footprintStr)) return null;
            
            var lines = footprintStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return null;
            
            int height = lines.Length;
            int width = 0;
            
            // 计算最大宽度
            foreach (var line in lines)
            {
                int lineWidth = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == '■' || line[i] == '○' || line[i] == ' ')
                    {
                        lineWidth++;
                    }
                }
                width = Math.Max(width, lineWidth);
            }
            
            if (width == 0 || height == 0) return null;
            
            // 创建footprint对象
            var footprint = new GameCore.CollisionSystem.Data.Struct.Footprint(
                (short)width, (short)height, 
                GameCore.CollisionSystem.Data.Enum.CollisionType.Static);
            
            // 解析每个格子
            for (int y = 0; y < height && y < lines.Length; y++)
            {
                var line = lines[y];
                for (int x = 0; x < width && x < line.Length; x++)
                {
                    // '■' 表示占用，'○' 和 ' ' 表示不占用
                    footprint[(short)x, (short)y] = (line[x] == '■');
                }
            }
            
            return footprint;
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 解析footprint字符串失败: {Ex}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 获取单位的碰撞半径
    /// </summary>
    private float GetUnitCollisionRadius(Unit unit)
    {
        try
        {
            // 🔍 尝试从单位缓存中获取碰撞半径
            var collisionRadius = unit.Cache?.CollisionRadius ?? 0f;
            if (collisionRadius > 0) return collisionRadius;
            
            // 🏗️ 根据单位类型推测碰撞半径（回退方案）
            var unitName = unit.Cache?.Name ?? "";
            if (unitName.Contains("Tower") || unitName.Contains("防御塔"))
            {
                return 128f; // 防御塔半径
            }
            else if (unitName.Contains("Wall") || unitName.Contains("城墙"))
            {
                return 96f; // 城墙半径
            }
            else if (unitName.Contains("Building") || unitName.Contains("建筑"))
            {
                return 64f; // 一般建筑半径
            }
            
            return 48f; // 默认半径
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 获取单位碰撞半径失败: {UnitName} - {Ex}", 
                unit.Cache?.Name ?? "未知单位", ex.Message);
            return 48f; // 默认半径
        }
    }

    /*
    /// <summary>
    /// 检查静态碰撞（地形障碍物、不可通行区域）- 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckStaticCollision(ScenePoint cellPosition, float gridSize)
    {
        // 此方法已被 GetStaticCollisionMask 替代，不再使用射线判断
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查地形可通行性 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckTerrainPassability(ScenePoint cellPosition, float gridSize)
    {
        // 此方法已被 GetStaticCollisionMask 替代，不再使用射线判断
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查世界面板射线障碍物 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckWorldPanelObstacles(ScenePoint cellPosition, float gridSize)
    {
        // 此方法已被 GetStaticCollisionMask 替代，不再使用射线判断
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查静态装饰物 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckStaticDecorations(ScenePoint cellPosition, float gridSize)
    {
        // 此方法已被 GetStaticCollisionMask 替代
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查特殊地形 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckSpecialTerrain(ScenePoint cellPosition, float gridSize)
    {
        // 此方法已被 GetStaticCollisionMask 替代
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查地形有效性 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckTerrainValidity(ScenePoint position)
    {
        // 此方法已被 GetStaticCollisionMask 替代
        return true;
    }
    */

    /*
    /// <summary>
    /// 检查单个格子的地形有效性 - 已替换为 GetStaticCollisionMask
    /// </summary>
    private bool CheckSingleCellTerrain(float worldX, float worldY, float worldZ)
    {
        // 此方法已被 GetStaticCollisionMask 替代
        return true;
    }
    */

    /// <summary>
    /// 更新建造预览的视觉效果
    /// </summary>
    private void UpdateBuildingPreviewVisuals(bool isValid)
    {
        if (buildingPreviewActor == null) return;

        try
        {
            // 🎨 根据有效性更改预览外观（颜色、透明度等）
            
            // 🎯 同时更新网格显示状态
            UpdateBuildingGridVisuals(isValid);
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新建造预览外观失败");
        }
    }

    /// <summary>
    /// 创建建造网格Actor
    /// </summary>
    private void CreateBuildingGrid()
    {
        if (host?.Position.Scene == null) return;

        try
        {
            // 🏗️ 获取建造单位的Footprint配置
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogError("❌ 无法获取建造单位的Footprint配置");
                return;
            }

            // 🎯 创建建造网格Actor - 显示网格和有效/无效位置
            var gridLink = new GameLink<GameDataActor, GameDataActorGrid>("BuildingValidGrid"u8);
            
            // 🎯 独立创建网格Actor（无scope附着），从(0,0)点通过bounds偏移定位
            buildingGridActor = gridLink.Data?.CreateActor(
                scope: null,  // 🔧 不附着到任何Actor，独立存在
                skipBirth: false,
                scene: host.Position.Scene
            ) as ActorGrid;
            
            if (buildingGridActor != null)
            {
                // 🎯 根据Footprint动态设置网格边界 - 正确的中心对齐计算
                int width = footprint.Width;
                int height = footprint.Height;
                
                // 🎯 bounds保持固定，从(0,0)开始，通过actor position来控制网格位置
                var gridBounds = new GridBounds(0, 0, width, height);
                buildingGridActor.Bounds = gridBounds;
                
                // 🎨 初始化所有网格格子为默认状态（状态1）
                InitializeGridStates();
                
                // 🎯 设置网格初始位置，通过actor position定位到预览模型位置
                UpdateBuildingGridPosition();
            }
            else
            {
                Game.Logger.LogError("❌ 建造网格Actor创建失败");
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 创建建造网格Actor时发生异常");
        }
    }

    /// <summary>
    /// 初始化网格状态 - 根据Footprint动态设置网格格子状态
    /// </summary>
    private void InitializeGridStates()
    {
        if (buildingGridActor?.GridStates == null) return;

        try
        {
            // 🏗️ 获取建造单位的Footprint配置
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogError("❌ 初始化网格状态时无法获取Footprint配置");
                return;
            }

            var bounds = buildingGridActor.Bounds;
            int width = footprint.Width;
            int height = footprint.Height;
            
            // 🎨 根据Footprint设置格子状态
            // 状态说明：0=隐藏, 1=默认显示, 2=有效高亮, 3=无效高亮
            for (int x = bounds.OffsetX; x < bounds.OffsetX + bounds.DimensionX; x++)
            {
                for (int y = bounds.OffsetY; y < bounds.OffsetY + bounds.DimensionY; y++)
                {
                    try
                    {
                        // 计算相对于Footprint的坐标（Footprint坐标从0开始）
                        int footprintX = x - bounds.OffsetX;
                        int footprintY = y - bounds.OffsetY;
                        
                        // 检查Footprint中这个位置是否被占用
                        bool isOccupied = footprint[(short)footprintX, (short)footprintY];
                        
                        // 设置格子状态：被占用的格子显示，未占用的格子隐藏
                        int cellState = isOccupied ? 1 : 0;  // 1=默认显示, 0=隐藏
                        buildingGridActor.GridStates[new GridIndex(x, y)] = cellState;
                    }
                    catch (Exception ex)
                    {
                        Game.Logger.LogWarning("⚠️ 设置网格状态失败 ({X}, {Y}): {Ex}", x, y, ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 初始化网格状态时发生异常");
        }
    }

    /// <summary>
    /// 更新建造网格视觉效果 - 网格通过bounds偏移定位，每个格子独立检测
    /// </summary>
    private void UpdateBuildingGridVisuals(bool isValid)
    {
        // 🎯 网格现在通过bounds偏移独立定位，每个格子独立检测有效性
        // 状态值：0=隐藏, 1=默认显示, 2=有效高亮, 3=无效高亮
        
        // 🎯 每个格子独立检测，不使用整体有效性
        UpdateCenterGridCellState(false); // 参数不再使用，内部会独立检测
    }

    /// <summary>
    /// 更新网格中被占用格子的状态 - 每个格子独立检查碰撞
    /// </summary>
    private void UpdateCenterGridCellState(bool unused = false)
    {
        if (buildingGridActor?.GridStates == null) return;

        try
        {
            // 🏗️ 获取建造单位的Footprint配置
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogError("❌ 更新格子状态时无法获取Footprint配置");
                return;
            }

            var bounds = buildingGridActor.Bounds;
            var gridSize = buildingGridActor.Cache?.GridSize ?? 64f;
            var gridPosition = buildingGridActor.Position;
            
            int validCells = 0;
            int invalidCells = 0;
            int updatedCells = 0;
            
            // 🎯 逐个格子进行独立的碰撞检测
            for (int x = bounds.OffsetX; x < bounds.OffsetX + bounds.DimensionX; x++)
            {
                for (int y = bounds.OffsetY; y < bounds.OffsetY + bounds.DimensionY; y++)
                {
                    try
                    {
                        // 计算相对于Footprint的坐标（Footprint坐标从0开始）
                        int footprintX = x - bounds.OffsetX;
                        int footprintY = y - bounds.OffsetY;
                        
                        // 检查Footprint中这个位置是否被占用
                        bool isOccupied = footprint[(short)footprintX, (short)footprintY];
                        
                        if (isOccupied)
                        {
                            // 🎯 计算这个格子的世界坐标
                            // 网格Actor现在是独立定位的，格子坐标 = 网格位置 + 格子在网格内的偏移
                            var cellWorldX = gridPosition.X + x * gridSize;
                            var cellWorldY = gridPosition.Y + y * gridSize;
                            var cellPosition = new ScenePoint(cellWorldX, cellWorldY, gridPosition.Scene)
                            {
                                Z = gridPosition.Z
                            };
                            
                            // 🔍 为每个格子单独进行完整的有效性检查
                            bool isCellValid = CheckSingleCellValidity(cellPosition, gridSize);
                            
                            // 设置格子状态：2=有效(绿色), 3=无效(红色)
                            int cellState = isCellValid ? 2 : 3;
                            var gridIndex = new GridIndex(x, y);
                            buildingGridActor.GridStates[gridIndex] = cellState;
                            
                            if (isCellValid) validCells++;
                            else invalidCells++;
                            updatedCells++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.Logger.LogWarning("⚠️ 更新格子状态失败 ({X}, {Y}): {Ex}", x, y, ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新格子状态失败");
        }
    }

    /// <summary>
    /// 检查单个格子的完整有效性（使用GetStaticCollisionMask替代原地形判断）
    /// </summary>
    private bool CheckSingleCellValidity(ScenePoint cellPosition, float gridSize)
    {
        try
        {
            // 1. 检查建筑物碰撞（已建造的建筑）
            if (!CheckBuildingConflicts(cellPosition, gridSize))
            {
                return false;
            }

            // 2. 使用 GetStaticCollisionMask 检查静态碰撞（替代原地形判断）
            if (!CheckStaticCollisionMask(cellPosition))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 格子({X:F1},{Y:F1}) 有效性检查异常: {Ex}", 
                cellPosition.X, cellPosition.Y, ex.Message);
            // 异常时假设无效，更安全
            return false;
        }
    }

    /// <summary>
    /// 使用 ScenePoint.GetStaticCollisionMask 检查静态碰撞（替代原地形判断）
    /// </summary>
    private bool CheckStaticCollisionMask(ScenePoint cellPosition)
    {
        try
        {
            // Game.Logger.LogDebug("🔍 开始检查静态碰撞掩码 - 格子({X:F1},{Y:F1})", 
            //     cellPosition.X, cellPosition.Y);
            // 使用 ScenePoint 的 GetStaticCollisionMask 方法检查静态碰撞
            var staticCollisionMask = cellPosition.GetStaticCollisionMask(checkDynamicApplied: true);
            
            // 🏗️ 检查是否包含阻挡建造的标志
            if (staticCollisionMask.HasFlag(GameCore.Struct.StaticCollisionMask.Unbuildable))
            {
                return false;
            }
            
            // 🏔️ 检查悬崖地形 - 通常不适合建造
            if (staticCollisionMask.HasFlag(GameCore.Struct.StaticCollisionMask.Cliff))
            {
                return false;
            }
            
            // 🦠 检查受污染区域 - 可能不适合建造
            if (staticCollisionMask.HasFlag(GameCore.Struct.StaticCollisionMask.Blighted))
            {
                return false;
            }
            
            // 📦 检查是否可以放置物品 - 如果连物品都不能放置，建筑物更不能建造
            if (staticCollisionMask.HasFlag(GameCore.Struct.StaticCollisionMask.UnItemplacable))
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 格子({X:F1},{Y:F1}) 静态碰撞检测异常: {Ex}", 
                cellPosition.X, cellPosition.Y, ex.Message);
            // 异常时假设不可建造，更安全
            return false;
        }
    }

    /// <summary>
    /// 更新建造网格位置 - 参考Lua实现，包含网格对齐和中心点计算
    /// </summary>
    private void UpdateBuildingGridPosition()
    {
        if (buildingGridActor == null || buildingPreviewActor == null) return;

        try
        {
            var previewPosition = buildingPreviewActor.Position;
            var gridSize = buildingGridActor.Cache?.GridSize ?? 64f; // building_block_size
            
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null) return;

            int width = footprint.Width;   // spellbuild_footpoint_size_x
            int height = footprint.Height; // spellbuild_footpoint_size_y
            
            // 🎯 参考Lua get_spellbuild_controller_pos() 的网格对齐逻辑
            // 获取鼠标控制的基础位置并进行网格对齐
            if (host == null) return;
            var mouseWorldPos = CalculateMouseWorldPosition(host.Position);
            
            // 🔧 Step 1: 计算建造技能指示器的网格对齐位置（参考Lua第417-433行）
            // 考虑建筑物足印大小，计算左下角起始位置
            float alignedPosX = mouseWorldPos.X - (width / 2f) * gridSize;
            float alignedPosY = mouseWorldPos.Y - (height / 2f) * gridSize;
            
            // 🎯 网格对齐：对齐到gridSize的倍数（参考Lua第427-432行）
            alignedPosX = alignedPosX - (alignedPosX % gridSize);
            alignedPosX = alignedPosX + gridSize / 2; // 对齐场景网格中心
            
            alignedPosY = alignedPosY - (alignedPosY % gridSize);
            alignedPosY = alignedPosY + gridSize / 2; // 对齐场景网格中心
            
            // 🔧 Step 2: 计算中心偏移位置（参考Lua get_spellbuild_controller_offset_pos()）
            // 考虑建筑物的中心点配置（默认为0.5, 0.5即中心）
            float footprintCenterX = 0.5f; // spellbuild_footpoint_center.X
            float footprintCenterY = 0.5f; // spellbuild_footpoint_center.Y
            
            // 计算从网格左下角到建筑中心的偏移（参考Lua第438-441行）
            float centerOffsetX = (width * gridSize * footprintCenterX - gridSize / 2);
            float centerOffsetY = (height * gridSize * footprintCenterY - gridSize / 2);
            
            // 🏗️ Step 3: 计算最终的网格Actor位置
            // 网格Actor应该放置在建筑占用区域的左下角
            float gridActorPosX = alignedPosX;
            float gridActorPosY = alignedPosY;
            
            // 🎯 设置网格actor的position（对应建筑占用区域的左下角）
            var gridPosition = new ScenePoint(gridActorPosX, gridActorPosY, previewPosition.Scene)
            {
                Z = previewPosition.Z
            };
            buildingGridActor.Position = gridPosition;
            
            // 🔧 Step 4: 同步更新预览Actor位置到对齐后的中心位置
            var previewCenterPosX = alignedPosX + centerOffsetX;
            var previewCenterPosY = alignedPosY + centerOffsetY;
            
            var alignedPreviewPosition = new ScenePoint(previewCenterPosX, previewCenterPosY, previewPosition.Scene)
            {
                Z = previewPosition.Z
            };
            buildingPreviewActor.Position = alignedPreviewPosition;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新网格position失败");
        }
    }
    /// <summary>
    /// 检查所有建造格子是否都有效（只有全绿色才能建造）
    /// </summary>
    private bool AreAllBuildingCellsValid()
    {
        try
        {
            if (buildingGridActor?.GridStates == null) 
            {
                Game.Logger.LogWarning("⚠️ 网格Actor或GridStates为空，无法验证格子有效性");
                return false;
            }

            // 🏗️ 获取建造单位的Footprint配置
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogError("❌ 检查格子有效性时无法获取Footprint配置");
                return false;
            }

            var bounds = buildingGridActor.Bounds;
            var gridSize = buildingGridActor.Cache?.GridSize ?? 64f;
            var gridPosition = buildingGridActor.Position;
            
            int validCells = 0;
            int totalOccupiedCells = 0;
            int invalidCells = 0;
            
            // 🎯 检查所有被占用的格子
            for (int x = bounds.OffsetX; x < bounds.OffsetX + bounds.DimensionX; x++)
            {
                for (int y = bounds.OffsetY; y < bounds.OffsetY + bounds.DimensionY; y++)
                {
                    // 计算相对于Footprint的坐标
                    int footprintX = x - bounds.OffsetX;
                    int footprintY = y - bounds.OffsetY;
                    
                    // 检查Footprint中这个位置是否被占用
                    bool isOccupied = footprint[(short)footprintX, (short)footprintY];
                    
                    if (isOccupied)
                    {
                        totalOccupiedCells++;
                        
                        // 🎯 计算这个格子的世界坐标
                        // 网格Actor现在是独立定位的，格子坐标 = 网格位置 + 格子在网格内的偏移
                        var cellWorldX = gridPosition.X + x * gridSize;
                        var cellWorldY = gridPosition.Y + y * gridSize;
                        var cellPosition = new ScenePoint(cellWorldX, cellWorldY, gridPosition.Scene)
                        {
                            Z = gridPosition.Z
                        };
                        
                        // 🔍 检查这个格子是否有效
                        if (CheckSingleCellValidity(cellPosition, gridSize))
                        {
                            validCells++;
                        }
                        else
                        {
                            invalidCells++;
                        }
                    }
                }
            }
            
            bool allValid = (validCells == totalOccupiedCells) && totalOccupiedCells > 0;
                
            return allValid;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 检查格子有效性异常");
            return false;
        }
    }

    private void Cast()
    {
        if (abilityExecute == null) return;
        var unit = abilityExecute.Host;
        if (unit == null) return;
        
        var abilityName = abilityExecute.Cache?.Name ?? "未知技能";
        
        // 🎯 检查所有格子是否都有效（只有全绿色才能建造）
        bool allCellsValid = AreAllBuildingCellsValid();
        
        if (!allCellsValid)
        {            
            // 不执行建造命令，直接返回
            return;
        }
        
        // 🎯 关键修复：使用预览模型的对齐位置作为建造目标，而不是鼠标原始位置
        ScenePoint actualBuildPosition = GetAlignedBuildingPosition();
        ICommandTarget commandTarget = actualBuildPosition;
        
        Command? cmd = null;
        if(abilityExecute.Item is ItemMod itemMod)
        {
            cmd = new ()
            {
                Item = itemMod,
                Target = commandTarget,
                Type = ComponentTagEx.InventoryManager,
                Index = CommandIndexInventory.Use,
                Flag = CommandFlag.IsUser,
                Player = Player.LocalPlayer,
            };
        }
        else
        {
            cmd = new ()
            {
                AbilityLink = abilityExecute.Link,
                Target = commandTarget,
                Flag = CommandFlag.IsUser,
                Player = Player.LocalPlayer,
            };
        }
        if(cmd == null)
        {
            Game.Logger.LogError("❌ 建造命令失败: {cmd}", cmd);
            return;
        }
        var result = cmd.Value.IssueOrder(unit);
        if (!result.IsSuccess)
        {
            var x = unit.GetTagComponent(ComponentTagEx.AbilityManager);
            CmdResultManager.ShowCmdResult(result);
            Game.Logger.LogError("❌ 建造命令失败: {result}", result);
        }
        else
        {
            // 🏗️ 建造命令成功发送后，立即更新碰撞信息缓存
            // 这样后续的建造检测就能立即识别这个建筑的占用区域
            var footprint = GetBuildingUnitFootprint();
            if (footprint != null)
            {
                try
                {
                    // 假设建造成功，预先更新碰撞信息
                    // 注意：这里使用actualBuildPosition，确保与实际建造位置一致
                    UpdateBuildingCollisionInfo(unit, actualBuildPosition, footprint, isRemove: false);
                }
                catch (Exception ex)
                {
                    Game.Logger.LogError(ex, "❌ 预更新建造碰撞信息失败");
                }
            }
        }
    }

    #region 碰撞信息缓存系统 - 参考Lua base.collision_info实现

    /// <summary>
    /// 更新建筑物碰撞信息 - 参考Lua的__update_collision_info函数
    /// </summary>
    /// <param name="unit">建造的单位</param>
    /// <param name="position">建造位置</param>
    /// <param name="footprint">建筑物足印</param>
    /// <param name="isRemove">是否移除（false=建造，true=拆除）</param>
    public static void UpdateBuildingCollisionInfo(Unit unit, ScenePoint position, GameCore.CollisionSystem.Data.Struct.Footprint footprint, bool isRemove = false)
    {
        if (unit?.Position.Scene == null || footprint == null) return;

        try
        {
            lock (CollisionInfoLock)
            {
                var sceneId = unit.Position.Scene.GetHashCode().ToString();
                
                // 确保场景碰撞信息表存在
                if (!SceneCollisionInfo.ContainsKey(sceneId))
                {
                    SceneCollisionInfo[sceneId] = new Dictionary<int, Dictionary<int, bool>>();
                }
                
                var sceneCollision = SceneCollisionInfo[sceneId];
                float gridSize = 64f; // building_block_size
                int collisionBlockSize = 32; // collision_block_size
                
                // 🏗️ 计算建筑物占用的格子范围（参考Lua逻辑）
                int width = footprint.Width;
                int height = footprint.Height;
                
                // 建筑物中心点
                float centerX = position.X;
                float centerY = position.Y;
                
                // 计算footprint左下角的起始位置
                float startX = centerX - (width / 2f) * gridSize;
                float startY = centerY - (height / 2f) * gridSize;
                
                // 遍历footprint的每个格子
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        // 检查footprint中这个位置是否被占用
                        if (!footprint[(short)i, (short)j]) continue;
                        
                        // 计算这个格子的世界坐标
                        float cellWorldX = startX + i * gridSize;
                        float cellWorldY = startY + j * gridSize;
                        
                        // 对齐到碰撞检测网格 (collision_block_size)
                        int collisionX = (int)Math.Floor(cellWorldX / collisionBlockSize) * collisionBlockSize;
                        int collisionY = (int)Math.Floor(cellWorldY / collisionBlockSize) * collisionBlockSize;
                        
                        // 更新碰撞信息
                        if (!sceneCollision.ContainsKey(collisionX))
                        {
                            sceneCollision[collisionX] = new Dictionary<int, bool>();
                        }
                        
                        sceneCollision[collisionX][collisionY] = !isRemove; // 建造=true, 拆除=false
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 更新建筑碰撞信息失败: {UnitName}", unit.Cache?.Name ?? "未知单位");
        }
    }

    /// <summary>
    /// 检查碰撞信息缓存 - 参考Lua的check_collision_info函数
    /// </summary>
    /// <param name="scene">场景</param>
    /// <param name="cellPosition">格子位置</param>
    /// <param name="gridSize">网格大小</param>
    /// <returns>是否有碰撞</returns>
    public static bool CheckCachedCollisionInfo(GameCore.SceneSystem.Scene scene, ScenePoint cellPosition, float gridSize)
    {
        if (scene == null) return false;

        try
        {
            lock (CollisionInfoLock)
            {
                var sceneId = scene.GetHashCode().ToString();
                
                if (!SceneCollisionInfo.ContainsKey(sceneId))
                {
                    return false; // 没有缓存信息，无碰撞
                }
                
                var sceneCollision = SceneCollisionInfo[sceneId];
                int collisionBlockSize = 32; // collision_block_size
                
                // 🔍 检查格子范围内的碰撞信息（参考Lua逻辑）
                float halfGrid = gridSize * 0.5f;
                float minX = cellPosition.X - halfGrid;
                float maxX = cellPosition.X + halfGrid;
                float minY = cellPosition.Y - halfGrid;
                float maxY = cellPosition.Y + halfGrid;
                
                // 遍历格子覆盖的碰撞块
                for (float x = minX; x <= maxX; x += collisionBlockSize)
                {
                    for (float y = minY; y <= maxY; y += collisionBlockSize)
                    {
                        int collisionX = (int)Math.Floor(x / collisionBlockSize) * collisionBlockSize;
                        int collisionY = (int)Math.Floor(y / collisionBlockSize) * collisionBlockSize;
                        
                        if (sceneCollision.ContainsKey(collisionX) && 
                            sceneCollision[collisionX].ContainsKey(collisionY) &&
                            sceneCollision[collisionX][collisionY])
                        {
                            return true; // 发现碰撞
                        }
                    }
                }
                
                return false; // 没有发现碰撞
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogDebug("⚠️ 检查缓存碰撞信息异常: ({X:F1},{Y:F1}) - {Ex}", 
                cellPosition.X, cellPosition.Y, ex.Message);
            return false; // 异常时假设无碰撞
        }
    }

    /// <summary>
    /// 清理场景的碰撞信息缓存
    /// </summary>
    /// <param name="scene">要清理的场景</param>
    public static void ClearSceneCollisionInfo(GameCore.SceneSystem.Scene scene)
    {
        if (scene == null) return;

        try
        {
            lock (CollisionInfoLock)
            {
                var sceneId = scene.GetHashCode().ToString();
                if (SceneCollisionInfo.ContainsKey(sceneId))
                {
                    SceneCollisionInfo.Remove(sceneId);
                }
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 清理场景碰撞信息失败");
        }
    }

    #endregion

    /// <summary>
    /// 获取对齐后的建造位置 - 使用预览模型的实际位置而不是鼠标原始位置
    /// </summary>
    private ScenePoint GetAlignedBuildingPosition()
    {
        // 🎯 优先使用预览模型的位置（已经过网格对齐）
        if (buildingPreviewActor != null && buildingPreviewActor.IsValid)
        {
            var previewPosition = buildingPreviewActor.Position;
            return previewPosition;
        }

        // 🔧 回退方案：如果预览模型不可用，手动计算对齐位置
        if (host == null) 
        {
            Game.Logger.LogWarning("⚠️ 无法获取对齐位置：host为空，使用原始位置");
            return lastBuildingPosition;
        }

        try
        {
            var gridSize = buildingGridActor?.Cache?.GridSize ?? 64f;
            var footprint = GetBuildingUnitFootprint();
            if (footprint == null)
            {
                Game.Logger.LogWarning("⚠️ 无法获取footprint，使用原始位置");
                return lastBuildingPosition;
            }

            int width = footprint.Width;
            int height = footprint.Height;
            
            // 🔧 重新计算对齐位置（与UpdateBuildingGridPosition逻辑一致）
            var mouseWorldPos = CalculateMouseWorldPosition(host.Position);
            
            // Step 1: 计算网格对齐位置
            float alignedPosX = mouseWorldPos.X - (width / 2f) * gridSize;
            float alignedPosY = mouseWorldPos.Y - (height / 2f) * gridSize;
            
            // Step 2: 网格对齐
            alignedPosX = alignedPosX - (alignedPosX % gridSize);
            alignedPosX = alignedPosX + gridSize / 2;
            
            alignedPosY = alignedPosY - (alignedPosY % gridSize);
            alignedPosY = alignedPosY + gridSize / 2;
            
            // Step 3: 计算中心偏移位置
            float footprintCenterX = 0.5f;
            float footprintCenterY = 0.5f;
            
            float centerOffsetX = (width * gridSize * footprintCenterX - gridSize / 2);
            float centerOffsetY = (height * gridSize * footprintCenterY - gridSize / 2);
            
            // Step 4: 最终对齐位置
            var previewCenterPosX = alignedPosX + centerOffsetX;
            var previewCenterPosY = alignedPosY + centerOffsetY;
            
            var alignedPosition = new ScenePoint(previewCenterPosX, previewCenterPosY, mouseWorldPos.Scene)
            {
                Z = mouseWorldPos.Z
            };
            
            return alignedPosition;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "❌ 计算对齐位置失败，使用原始位置");
            return lastBuildingPosition;
        }
    }

    /// <summary>
    /// 取消建造或清理建造预览
    /// </summary>
    private void CancelBuilding()
    {
        // 🗑️ 清理建造相关资源
        // 销毁预览Actor
        if (buildingPreviewActor != null)
        {
            try
            {
                buildingPreviewActor.Destroy();
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "❌ 销毁独立预览Actor失败");
            }
            finally
            {
                buildingPreviewActor = null;
            }
        }

        // 销毁网格Actor
        if (buildingGridActor != null)
        {
            try
            {
                buildingGridActor.Destroy();
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "❌ 销毁建造网格Actor失败");
            }
            finally
            {
                buildingGridActor = null;
            }
        }

        
        // 🎯 重置格子位置追踪
        lastGridPosition = null;
    }

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        OnJoystickPressed -= OnPressed;
        OnJoystickReleased -= OnReleased;
        OnJoystickMove -= OnMove;
        Updater -= UpdateBuildingPosition;
        
        // 清理建造相关资源
        CancelBuilding();
        
        OnStopBuilding = null;
        OnStopBuildingEnd = null;
        
        // 🏗️ 清理静态事件监听（注意：静态事件需要小心处理）
        // OnBuildingCompleted 是静态事件，这里不需要清理个别实例的监听
    }
}

#endif
