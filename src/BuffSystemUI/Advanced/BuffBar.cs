#if CLIENT
using System;
using System.Collections.Generic;
using System.Linq;
using GameUI.Control.Primitive;
using GameUI.Control.Enum;
using GameUI.Enum;
using GameUI.Control.Data;
using GameSystemUI.BuffSystemUI.Data;
using GameCore.Behavior;
using EngineInterface.BaseInterface;
using GameCore.Data;
using GameData;
using GameCore.EntitySystem;
using GameCore.BuffSystem;
using GameCore.BuffSystem.Data;
using GameCore.BuffSystem.Manager;
using GameCore.PlayerAndUsers;
using GameCore;
using GameCore.Event;
using GameCore.DisplayInfo;
using Events;
using GameCore.BuffSystem.Data.Enum;

namespace GameSystemUI.BuffSystemUI.Advanced;

/// <summary>
/// Buff条UI控件
/// 显示单位身上的Buff列表，支持过滤、排序等功能
/// </summary>
[GameObject<GameDataControlBuffBar>]
public partial class BuffBar : Panel
{
    // UI组件
    private readonly Panel barPanel;
    private readonly List<BuffIcon> buffIcons = new();
    
    // 数据属性
    private Unit? _unit;
    private readonly List<Buff> _allBuffs = new();
    
    // Buff跟踪机制 - 解决buff移除时显示错误的问题
    private readonly Dictionary<Buff, BuffIcon> _buffToIconMap = new();
    private readonly Dictionary<BuffIcon, Buff> _iconToBuffMap = new();
    
    // 事件管理 - 参考AbilityJoyStickGroup
    private BuffManager? currentBuffManager;

    // 默认模板 - 使用类型哈希码作为默认模板ID
    public static new readonly IGameLink<GameDataControlBuffBar> DefaultTemplate = 
        new GameLink<GameDataControl, GameDataControlBuffBar>(typeof(BuffBar).GetHashCode());

    // 自动更新机制 - 参考AbilityJoyStick
    public static event Action? OnBuffUpdateRequested;
    private static readonly Trigger<EventGameTick> buffUpdateTrigger;
    private static int subscriberCount = 0;
    private static int updateFrameInterval = 10; // Buff更新频率稍低一些
    private static int currentFrame = 0;

    /// <summary>
    /// Buff更新帧间隔
    /// </summary>
    public static int BuffUpdateFrameInterval
    {
        get => updateFrameInterval;
        set
        {
            if (value > 0)
                updateFrameInterval = value;
        }
    }

    static BuffBar()
    {
        try
        {
            buffUpdateTrigger = new Trigger<EventGameTick>(async (s, e) => 
            {
                OnGameTick();
                await Task.CompletedTask;
                return true;
            });
            buffUpdateTrigger.Register(Game.Instance);
        }
        catch (Exception ex)
        {
            Game.Logger.LogError("创建 BuffBar 更新触发器失败: {ex}", ex.Message);
            throw;
        }
    }



    /// <summary>
    /// 绑定的单位 - 参考AbilityJoyStickGroup的绑定方式
    /// </summary>
    public Unit? BindUnit
    {
        get => _unit as Unit;
        set
        {
            if (_unit == value) return;
            
            // 取消之前的事件监听
            UnsubscribeBuffEvents();
            
            _unit = value;
            
            // 订阅新的事件监听
            SubscribeBuffEvents();
            
            // 立即更新一次显示
            UpdateBuffList();
            
        }
    }


    public BuffBar(IGameLink<GameDataControlBuffBar> link) : base(link)
    {
        // 创建UI组件
        barPanel = new Panel();
        
        InitializeUI();
        SetUnit(null); // 初始化为默认显示主控单位
        SubscribeToBuffUpdates();
    }

    public BuffBar() : this(DefaultTemplate)
    {
    }

    /// <summary>
    /// 创建BuffBar实例
    /// </summary>
    public static BuffBar Create()
    {
        return new BuffBar();
    }

    /// <summary>
    /// 创建BuffBar实例并绑定单位
    /// </summary>
    public static BuffBar Create(Unit unit)
    {
        var bar = new BuffBar();
        bar.BindUnit = unit;
        return bar;
    }

    private void InitializeUI()
    {
        // 设置主面板 - 不拉伸，让外部控制位置
        WidthStretchRatio = 0.0f; // 修改：不拉伸宽度，避免占满整个屏幕
        HeightStretchRatio = 0.0f; // 修改：不拉伸高度，避免占满整个屏幕

        // 设置Bar面板 - 横向排列，紧凑布局
        barPanel.WidthStretchRatio = 1.0f; // 修改：在BuffBar内部拉伸
        barPanel.HeightStretchRatio = 1.0f; // 修改：在BuffBar内部拉伸
        barPanel.FlowOrientation = Orientation.Horizontal;
        barPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left; // 修改：左对齐，避免内部居中影响外部位置

        AddChild(barPanel);
    }

    /// <summary>
    /// 设置绑定的单位
    /// </summary>
    public void SetUnit(Unit? unit)
    {
        _unit = unit;
        
        // 启动运行时更新
        StartBuffUpdate();
    }

    /// <summary>
    /// 启动Buff更新 - 简化版本，不使用异步任务
    /// </summary>
    private void StartBuffUpdate()
    {
        // 简化实现：只更新一次，不使用定时循环
        // 避免使用Task.Run和Task.Delay，因为平台不支持ThreadPool
        try
        {
            UpdateBuffList();
            // Game.Logger?.LogDebug("BuffBar初始更新完成");
        }
        catch (Exception ex)
        {
            Game.Logger?.LogError(ex, "BuffBar初始更新出错: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// 更新Buff列表 - 使用真实的BuffManager获取buff
    /// </summary>
    private void UpdateBuffList()
    {
        _allBuffs.Clear();

        // 获取单位，如果没有指定则使用主控单位
        var targetUnit = _unit ?? GetLocalPlayerHero();
        if (targetUnit == null) return;

        try
        {
            // 真实的buff获取实现 - 模仿技能摇杆的方式
            if (targetUnit != null)
            {
                // 获取BuffManager组件 - 核心接口！
                var buffManager = targetUnit.GetComponent<BuffManager>();
                if (buffManager == null)
                {
                    // Game.Logger?.LogDebug("单位 {unit} 没有 BuffManager 组件", unit);
                    UpdateLayout();
                    return;
                }

                // 获取所有buff - 模仿AbilityManager.GetAll()的方式
                var allBuffs = buffManager.GetAll();
                
                foreach (var buff in allBuffs)
                {
                    if (buff is Buff typedBuff && ShouldShowBuff(typedBuff))
                    {
                        _allBuffs.Add(typedBuff);
                    }
                }

                // Game.Logger?.LogDebug("BuffBar获取到 {count} 个buff", _allBuffs.Count);
            }
            else
            {
                // Game.Logger?.LogDebug("绑定的目标不是Unit类型: {type}", targetUnit?.GetType().Name ?? "null");
            }
        }
        catch (Exception ex)
        {
            Game.Logger?.LogWarning("获取Buff列表时出错: {ex}", ex.Message);
        }

        UpdateLayout();
    }

    /// <summary>
    /// 处理Buff列表进行多实例合并显示
    /// 对于设置了MergeInstances=true的Buff，将相同类型的实例合并为一个显示
    /// </summary>
    private List<Buff> ProcessBuffsForMerging(List<Buff> originalBuffs)
    {
        var processedBuffs = new List<Buff>();
        var mergedBuffTypes = new Dictionary<Type, Buff>(); // 存储已合并的buff类型和其代表实例
        
        foreach (var buff in originalBuffs)
        {
            try
            {
                var buffData = BuffIcon.CreateBuffDisplayData(buff);
                if (buffData == null) continue;
                
                // 🎯 检查是否需要合并显示
                if (buffData.MergeInstances)
                {
                    var buffType = buff.GetType();
                    
                    // 🎯 如果已经有相同类型的buff被处理，比较剩余时间选择最长的
                    if (mergedBuffTypes.ContainsKey(buffType))
                    {
                        var existingBuff = mergedBuffTypes[buffType];
                        var existingBuffData = BuffIcon.CreateBuffDisplayData(existingBuff);
                        
                        // 🎯 选择剩余时间最长的实例作为代表
                        if (existingBuffData != null && buffData.Remaining > existingBuffData.Remaining)
                        {
                            // 当前buff剩余时间更长，替换已存储的buff
                            mergedBuffTypes[buffType] = buff;
                            
                            // 从processedBuffs中移除旧的实例，添加新的
                            processedBuffs.Remove(existingBuff);
                            processedBuffs.Add(buff);
                            
                        }
                        else
                        {
                            // 已存储的实例剩余时间更长，跳过当前buff
                        }
                        continue;
                    }
                    else
                    {
                        // 第一次遇到这个类型的buff，直接记录
                        mergedBuffTypes[buffType] = buff;
                        processedBuffs.Add(buff);
                    }
                }
                else
                {
                    // 不需要合并，直接添加
                    processedBuffs.Add(buff);
                }
            }
            catch (Exception ex)
            {
                Game.Logger?.LogWarning("处理buff合并显示时出错: {ex}，跳过该buff", ex.Message);
                // 出错时还是添加原始buff，避免丢失显示
                processedBuffs.Add(buff);
            }
        }
        
        return processedBuffs;
    }

    /// <summary>
    /// 更新布局显示 - 支持多实例合并显示功能
    /// </summary>
    private void UpdateLayout()
    {
        // 🎯 步骤0：预处理Buff列表，处理多实例合并显示
        var processedBuffs = ProcessBuffsForMerging(_allBuffs);
        
        // 排序Buff列表 - 现在使用简单的排序
        processedBuffs.Sort((a, b) => 
        {
            // 获取Buff的极性进行排序，这里使用BuffIcon的方法来获取显示数据
            var dataA = BuffIcon.CreateBuffDisplayData(a);
            var dataB = BuffIcon.CreateBuffDisplayData(b);
            
            if (dataA == null && dataB == null) return 0;
            if (dataA == null) return 1;
            if (dataB == null) return -1;
            
            return dataA.Polarity.CompareTo(dataB.Polarity);
        });

        // 🎯 步骤1：标记所有现存的Buff（使用处理后的列表）
        var currentBuffs = new HashSet<object>(processedBuffs);
        
        // 🎯 步骤2：隐藏已经不存在的Buff对应的BuffIcon，并清理映射关系
        foreach (var kvp in _iconToBuffMap.ToList())
        {
            var icon = kvp.Key;
            var buff = kvp.Value;
            
            if (!currentBuffs.Contains(buff))
            {
                // 这个buff已经被移除了，隐藏对应的图标
                icon.Visible = false;
                icon.Buff = null; // 清除绑定，这会自动更新显示
                
                // 清理映射关系
                _iconToBuffMap.Remove(icon);
                if (_buffToIconMap.ContainsKey(buff))
                {
                    _buffToIconMap.Remove(buff);
                }
                
                // Game.Logger?.LogDebug("🗑️ 隐藏已移除buff对应的BuffIcon");
            }
        }

        // 🎯 步骤3：为新的或更新的Buff分配BuffIcon（使用处理后的列表）
        var usedIcons = new HashSet<BuffIcon>();
        
        for (int i = 0; i < processedBuffs.Count; i++)
        {
            var buff = processedBuffs[i];

            BuffIcon? buffIcon = null;

            // 检查是否已经有对应的BuffIcon
            if (_buffToIconMap.TryGetValue(buff, out var existingIcon))
            {
                buffIcon = existingIcon;
                // Game.Logger?.LogDebug("🔄 复用现有BuffIcon for buff: {buffType}", buff.GetType().Name);
            }
            else
            {
                // 需要为这个buff分配一个新的BuffIcon
                // 寻找一个未使用的BuffIcon
                buffIcon = buffIcons.FirstOrDefault(icon => !usedIcons.Contains(icon) && !icon.Visible);
                
                if (buffIcon == null)
                {
                    // 没有可用的BuffIcon，创建新的
                    buffIcon = new BuffIcon();
                    buffIcons.Add(buffIcon);
                    barPanel.AddChild(buffIcon);
                    // Game.Logger?.LogDebug("✨ 创建新的BuffIcon for buff: {buffType}", buff.GetType().Name);
                }

                // 建立映射关系
                _buffToIconMap[buff] = buffIcon;
                _iconToBuffMap[buffIcon] = buff;
                // Game.Logger?.LogDebug("🔗 建立新的buff-icon映射关系");
            }

            usedIcons.Add(buffIcon);

            // 🎯 步骤4：更新BuffIcon的显示 - 现在BuffIcon会自主管理所有显示逻辑
            buffIcon.Buff = buff; // 这会触发BuffIcon的UpdateBuffDisplay方法
            buffIcon.Visible = true;

            // 设置间距：第一个图标左边距为0，其他图标左边距为14
            // if (i == 0)
            // {
            //     buffIcon.Margin = new GameUI.Struct.Thickness(0, 0, 0, 0);
            // }
            // else
            // {
                buffIcon.Margin = new GameUI.Struct.Thickness(7);
            // }
        }

        // 🎯 步骤5：确保所有未使用的BuffIcon都被隐藏
        foreach (var icon in buffIcons.Where(icon => !usedIcons.Contains(icon)))
        {
            if (icon.Visible)
            {
                icon.Visible = false;
                icon.Buff = null; // 这会清理BuffIcon的显示
                // Game.Logger?.LogDebug("🙈 隐藏未使用的BuffIcon");
            }
        }

        // Game.Logger?.LogDebug("🔄 UpdateLayout完成，显示{count}个Buff（原始{original}个）", processedBuffs.Count, _allBuffs.Count);
    }


    /// <summary>
    /// 获取本地玩家的主控单位 - 模仿摇杆的获取方式
    /// </summary>
    private Unit? GetLocalPlayerHero()
    {
        try
        {
            // 模仿JoyStickTestExample的方式获取主控单位
            return GameCore.PlayerAndUsers.Player.LocalPlayer?.MainUnit;
        }
        catch (Exception ex)
        {
            Game.Logger?.LogDebug("获取主控单位时出错: {ex}", ex.Message);
            return null;
        }
    }





    /// <summary>
    /// 判断Buff是否应该显示
    /// </summary>
    private bool ShouldShowBuff(Buff buff)
    {
        // 简单实现：显示所有buff
        return true;
        
        // 可以根据需要添加过滤逻辑：
        // if (buff is GameCore.Behavior.Buff realBuff)
        // {
        //     // 过滤掉某些类型的buff
        //     var buffName = realBuff.BuffLink?.FriendlyName ?? "";
        //     if (buffName.Contains("Hidden")) return false;
        //     
        //     return true;
        // }
        // return false;
    }



    /// <summary>
    /// 将极性枚举转换为字符串
    /// </summary>
    private string GetBuffPolarityString(GameCore.BuffSystem.Data.Enum.BuffPolarity polarity)
    {
        return polarity switch
        {
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Positive => "正面",
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Negative => "负面",
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral => "中性",
            _ => "中性"
        };
    }





    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        
        // 清理BuffIcon
        foreach (var buffIcon in buffIcons)
        {
            buffIcon?.Destroy();
        }
        buffIcons.Clear();
        
        // 清理UI组件
        barPanel?.Destroy();
        
        // 清理数据
        _allBuffs.Clear();
        _buffToIconMap.Clear();
        _iconToBuffMap.Clear();
        _unit = null;
        
        // 取消事件订阅
        UnsubscribeBuffEvents();
        UnsubscribeFromBuffUpdates();
    }
    
    /// <summary>
    /// 订阅Buff事件监听 - 参考AbilityJoyStickGroup
    /// </summary>
    private void SubscribeBuffEvents()
    {
        if (_unit is not Unit unit)
            return;
            
        currentBuffManager = unit.GetComponent<BuffManager>();
        if (currentBuffManager == null)
        {
            // Game.Logger?.LogDebug("单位 {unit} 没有 BuffManager 组件", unit);
            return;
        }
            
        // 监听Buff添加/移除事件 - 这些事件名称需要根据实际BuffManager API调整
        // currentBuffManager.OnObjectAttached += OnBuffAttached;
        // currentBuffManager.OnObjectDetached += OnBuffDetached;
        
        // Game.Logger?.LogDebug("已订阅单位 {unit} 的Buff事件", unit);
    }
    
    /// <summary>
    /// 取消订阅Buff事件监听
    /// </summary>
    private void UnsubscribeBuffEvents()
    {
        if (currentBuffManager != null)
        {
            // 取消事件订阅 - 这些事件名称需要根据实际BuffManager API调整
            // currentBuffManager.OnObjectAttached -= OnBuffAttached;
            // currentBuffManager.OnObjectDetached -= OnBuffDetached;
            // Game.Logger?.LogDebug("已取消订阅BuffManager事件");
        }
        
        currentBuffManager = null;
    }
    
    private static void OnGameTick()
    {
        if (subscriberCount <= 0) return;
        
        currentFrame++;
        
        if (currentFrame >= updateFrameInterval)
        {
            currentFrame = 0;
            
            try
            {
                OnBuffUpdateRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Game.Logger.LogWarning("发出Buff更新事件时出错: {ex}", ex.Message);
            }
        }
    }
    
    private void SubscribeToBuffUpdates()
    {
        OnBuffUpdateRequested += HandleBuffUpdate;
        subscriberCount++;
    }
    
    private void UnsubscribeFromBuffUpdates()
    {
        OnBuffUpdateRequested -= HandleBuffUpdate;
        subscriberCount--;
        if (subscriberCount < 0) subscriberCount = 0;
    }
    
    private void HandleBuffUpdate()
    {
        try
        {
            UpdateBuffList();
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("处理Buff更新时出错: {ex}", ex.Message);
        }
    }
}
#endif

