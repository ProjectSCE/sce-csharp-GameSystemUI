using System;
using System.Collections.Generic;

namespace GameSystemUI.BuffSystemUI.Data;

/// <summary>
/// 进度条类型枚举
/// 双端通用的数编枚举
/// </summary>
public enum ProgressType
{
    /// <summary>
    /// 顺时针
    /// </summary>
    Clockwise,
    
    /// <summary>
    /// 逆时针
    /// </summary>
    CounterClockwise
}

/// <summary>
/// Buff极性过滤标志
/// 可扩展枚举，支持多选
/// 双端通用的数编枚举
/// </summary>
[Flags]
public enum BuffPolarityFilter
{
    /// <summary>
    /// 无过滤
    /// </summary>
    None = 0,
    
    /// <summary>
    /// 正面极性
    /// </summary>
    Positive = 1 << 0,
    
    /// <summary>
    /// 负面极性
    /// </summary>
    Negative = 1 << 1,
    
    /// <summary>
    /// 中性极性
    /// </summary>
    Neutral = 1 << 2,
    
    /// <summary>
    /// 所有极性
    /// </summary>
    All = Positive | Negative | Neutral
}

/// <summary>
/// Buff分类过滤标志
/// 可扩展枚举，支持多选
/// 双端通用的数编枚举
/// </summary>
[Flags]
public enum BuffCategoryFilter
{
    /// <summary>
    /// 无过滤
    /// </summary>
    None = 0,
    
    /// <summary>
    /// 可被禁用的Buff
    /// </summary>
    Dispellable = 1 << 0,
    
    /// <summary>
    /// 负面效果
    /// </summary>
    Debuff = 1 << 1,
    
    /// <summary>
    /// 增益效果
    /// </summary>
    Buff = 1 << 2,
    
    /// <summary>
    /// 状态效果
    /// </summary>
    Status = 1 << 3,
    
    /// <summary>
    /// 光环效果
    /// </summary>
    Aura = 1 << 4,
    
    /// <summary>
    /// 被动效果
    /// </summary>
    Passive = 1 << 5,
    
    /// <summary>
    /// 所有分类
    /// </summary>
    All = Dispellable | Debuff | Buff | Status | Aura | Passive
}
