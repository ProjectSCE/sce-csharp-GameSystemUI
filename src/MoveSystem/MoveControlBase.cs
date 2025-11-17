#if CLIENT
using GameCore;
using GameCore.GameSystem.Enum;
using GameUI.Control;
using GameUI.Control.Data;
using GameSystemUI.CmdResultSystemUI;
using System;
using GameUI.Control.Primitive;

namespace GameSystemUI.MoveSystem;

/// <summary>
/// 移动控制系统的抽象基类
/// 提供移动命令发送、镜头角度检测、单位切换等通用逻辑
/// </summary>
public abstract class MoveControlBase : Panel, IThinker
{
    #region 公共属性
    
    /// <summary>
    /// 是否在单位变化时停止移动
    /// </summary>
    public bool StopMoveOnUnitChange { get; set; } = true;
    
    /// <summary>
    /// 当前绑定的单位
    /// </summary>
    public abstract Unit? BindUnit { get; set; }
    
    #endregion

    #region 受保护字段
    
    /// <summary>
    /// 标记是否是第一次移动
    /// </summary>
    protected bool isFirstMove = true;
    
    /// <summary>
    /// 记录上一次的镜头yaw角度
    /// </summary>
    protected float lastCameraYaw = 0f;
    
    #endregion

    #region 构造函数
    
    protected MoveControlBase()
    {
        // 初始化镜头角度
        lastCameraYaw = Utility.GetCameraAngleOffset();
    }
    
    protected MoveControlBase(IGameLink<GameDataControlPanel> link) : base(link)
    {
        // 初始化镜头角度
        lastCameraYaw = Utility.GetCameraAngleOffset();
    }
    
    #endregion

    #region 抽象方法 - 子类需要实现
    
    /// <summary>
    /// 检查是否正在移动中
    /// </summary>
    /// <returns>true表示正在移动</returns>
    protected abstract bool IsCurrentlyMoving { get; }
    
    /// <summary>
    /// 获取当前移动角度（如果正在移动）
    /// </summary>
    /// <returns>当前移动角度，如果没有则返回null</returns>
    protected abstract Angle? GetCurrentMoveAngle();
    
    /// <summary>
    /// 处理单位绑定变化的特定逻辑
    /// </summary>
    /// <param name="oldUnit">原来的单位</param>
    /// <param name="newUnit">新的单位</param>
    /// <param name="wasMoving">变化前是否正在移动</param>
    protected abstract void OnBindUnitChanged(Unit? oldUnit, Unit? newUnit, bool wasMoving);
    
    #endregion

    #region 移动命令方法
    
    /// <summary>
    /// 应用镜头角度偏移
    /// </summary>
    /// <param name="originalAngle">原始角度</param>
    /// <returns>应用镜头偏移后的角度</returns>
    private Angle ApplyCameraOffset(Angle originalAngle)
    {
        float cameraAngleOffset = Utility.GetCameraAngleOffset();
        
        // 将角度转换为向量
        var vector = originalAngle.ToVector2();
        
        // 应用镜头角度补偿
        float cosAngle = (float)Math.Cos(cameraAngleOffset * Math.PI / 180.0);
        float sinAngle = (float)Math.Sin(cameraAngleOffset * Math.PI / 180.0);
        float adjustedX = vector.X * cosAngle - vector.Y * sinAngle;
        float adjustedY = vector.X * sinAngle + vector.Y * cosAngle;
        
        // 转换回角度
        return Angle.FromVector2(new System.Numerics.Vector2(adjustedX, adjustedY));
    }

    /// <summary>
    /// 发送移动命令（自动应用镜头偏移）
    /// </summary>
    /// <param name="angle">原始移动角度</param>
    protected void Move(Angle angle)
    {
        if (BindUnit == null || !BindUnit.IsValid) return;
        
        // 应用镜头偏移
        var adjustedAngle = ApplyCameraOffset(angle);
        
        Command command = new()
        {
            Index = CommandIndex.VectorMove,
            Type = ComponentTag.Walkable,
            Target = adjustedAngle
        };
        var result = command.IssueOrder(BindUnit);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }

    /// <summary>
    /// 发送移动调整命令（自动应用镜头偏移）
    /// </summary>
    /// <param name="angle">原始移动角度</param>
    protected void MoveAdjust(Angle angle)
    {
        if (BindUnit == null || !BindUnit.IsValid) return;
        
        // 应用镜头偏移
        var adjustedAngle = ApplyCameraOffset(angle);
        
        Command command = new()
        {
            Index = CommandIndex.VectorMoveAdjust,
            Type = ComponentTag.Walkable,
            Target = adjustedAngle
        };
        var result = command.IssueOrder(BindUnit);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }

    /// <summary>
    /// 发送停止移动命令
    /// </summary>
    protected void MoveStop()
    {
        if (BindUnit == null || !BindUnit.IsValid) return;
        
        Command command = new()
        {
            Index = CommandIndex.VectorMoveStop,
            Type = ComponentTag.Walkable,
        };
        var result = command.IssueOrder(BindUnit);
        if (!result.IsSuccess)
        {
            CmdResultManager.ShowCmdResult(result);
        }
    }
    
    #endregion

    #region 移动状态管理
    
    /// <summary>
    /// 开始移动时调用
    /// </summary>
    protected void OnStartMoving()
    {
        isFirstMove = true;
        ((IThinker)this).DoesThink = true;
    }
    
    /// <summary>
    /// 停止移动时调用
    /// </summary>
    protected void OnStopMoving()
    {
        isFirstMove = true;
        ((IThinker)this).DoesThink = false;
    }
    
    /// <summary>
    /// 执行移动命令（根据isFirstMove决定使用Move还是MoveAdjust）
    /// </summary>
    /// <param name="angle">移动角度</param>
    protected void ExecuteMove(Angle angle)
    {
        if (isFirstMove)
        {
            Move(angle);
            isFirstMove = false;
        }
        else
        {
            MoveAdjust(angle);
        }
    }
    
    #endregion

    #region 单位绑定管理
    
    /// <summary>
    /// 处理单位绑定变化的通用逻辑
    /// </summary>
    /// <param name="oldUnit">原来的单位</param>
    /// <param name="newUnit">新的单位</param>
    protected void HandleBindUnitChange(Unit? oldUnit, Unit? newUnit)
    {
        if (newUnit == oldUnit) return;
        
        // 记录当前是否正在移动中
        bool wasMoving = IsCurrentlyMoving;
        
        // 如果StopMoveOnUnitChange为true，停止前一个单位的移动
        if (StopMoveOnUnitChange && wasMoving)
        {
            MoveStop();
        }
        
        // 调用子类特定的处理逻辑
        OnBindUnitChanged(oldUnit, newUnit, wasMoving);
        
        // 如果之前正在移动中，需要对新单位发送移动命令并重置firstMove
        if (wasMoving && newUnit != null)
        {
            var currentAngle = GetCurrentMoveAngle();
            if (currentAngle.HasValue)
            {
                // 重置firstMove标记
                isFirstMove = true;
                
                // 使用当前角度立即发送移动命令给新单位
                Move(currentAngle.Value);
                isFirstMove = false;
            }
        }
    }
    
    #endregion

    #region IThinker实现
    
    /// <summary>
    /// IThinker接口实现，检测镜头角度变化并发送修正移动命令
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public virtual void Think(int delta)
    {
        // 只有在移动过程中才检测镜头角度变化
        if (!IsCurrentlyMoving || BindUnit == null || !BindUnit.IsValid)
            return;

        float currentCameraYaw = Utility.GetCameraAngleOffset();
        
        // 检测镜头角度是否发生了变化（容差为1度，避免浮点数精度问题）
        if (Math.Abs(currentCameraYaw - lastCameraYaw) > 1f)
        {
            var currentAngle = GetCurrentMoveAngle();
            if (currentAngle.HasValue)
            {
                // 镜头角度发生变化，重新发送移动命令
                // 注意：这里发送的是原始角度，Move方法会自动应用新的镜头偏移
                Move(currentAngle.Value);
                
            }
            
            lastCameraYaw = currentCameraYaw;
        }
    }
    
    #endregion

    #region 资源清理
    
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        // 停止思考
        ((IThinker)this).DoesThink = false;
    }
    
    #endregion
}

#endif
