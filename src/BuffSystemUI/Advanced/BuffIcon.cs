#if CLIENT
using System;
using System.Drawing;
using GameUI.Control.Primitive;
using GameUI.Control.Enum;
using GameUI.Control.Data;
using GameUI.Brush;
using GameUI.Enum;
using GameSystemUI.BuffSystemUI.Data;
using EngineInterface.BaseInterface;
using GameCore.Data;
using GameData;
using GameCore.AbilitySystem.Manager;
using GameCore.Behavior;
using GameCore.BuffSystem;
using GameCore.BuffSystem.Data;
using GameCore.BuffSystem.Manager;
using GameCore;
using GameCore.Event;
using GameCore.DisplayInfo;
using GameCore.Timers;
using GameCore.Animation;
using GameCore.Animation.EasingFunction;
using Events;
using GameCore.BuffSystem.Data.Enum;

namespace GameSystemUI.BuffSystemUI.Advanced;

/// <summary>
/// Buff显示数据结构 - 整合所有Buff显示所需的信息
/// </summary>
public class BuffDisplayData
{
    public Buff? Buff { get; set; }
    public string Name { get; set; } = "";
    public string IconPath { get; set; } = "";
    public GameCore.BuffSystem.Data.Enum.BuffPolarity Polarity { get; set; } = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral;
    public int Stack { get; set; } = 1;
    public float Remaining { get; set; } = 0;
    public float Duration { get; set; } = 0;
    public string Description { get; set; } = "";
    
    // 🎯 新增UI配置选项
    /// <summary>
    /// 是否显示这个Buff图标
    /// </summary>
    public bool ShowIcon { get; set; } = true;
    
    /// <summary>
    /// 是否显示持续时间
    /// </summary>
    public bool ShowDuration { get; set; } = true;
    
    /// <summary>
    /// 持续时间显示格式
    /// </summary>
    public GameCore.BuffSystem.Data.Enum.UIDurationFormat DurationFormat { get; set; } = GameCore.BuffSystem.Data.Enum.UIDurationFormat.Numeric;
    
    /// <summary>
    /// 是否显示叠加层数
    /// </summary>
    public bool ShowStack { get; set; } = true;
    
    /// <summary>
    /// 是否在即将消失时闪烁
    /// </summary>
    public bool BlinkWhenExpiring { get; set; } = true;
    
    /// <summary>
    /// 闪烁阈值时间（剩余时间小于等于此值时开始闪烁）
    /// </summary>
    public TimeSpan? BlinkThreshold { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// 是否多个实例合并显示
    /// </summary>
    public bool MergeInstances { get; set; } = false;
}

/// <summary>
/// Buff图标UI控件
/// 显示具体的Buff图标，包含图标、CD进度、堆叠数量等
/// </summary>
[GameObject<GameDataControlBuffIcon>]
public partial class BuffIcon : Panel
{
    // UI组件
    private readonly Panel mainPanel;
    private readonly Panel buffBgPanel;    // 背景边框
    private readonly Panel buffIconPanel;  // 图标面板
    private readonly Panel buffCooldownPanel;    // 冷却面板
    private readonly Progress buffCooldownProgress; // 冷却进度条
    private readonly Label buffCooldownNumLabel;    // 冷却数字标签
    private readonly Label buffStackNumLabel; // 堆叠数量标签

    // 数据属性
    private Buff? _buff;
    private GameCore.BuffSystem.Data.Enum.BuffPolarity _polarity = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral;
    private float _cooldownProgress;
    private string _cooldownText = string.Empty;
    private string _stackText = string.Empty;
    private bool _isBlinking = false;
    
    // 闪烁相关
    private NumberAnimation<float>? _blinkAnimation;
    private bool _isBlinkAnimationRunning = false;

    // 默认模板 - 使用类型哈希码作为默认模板ID
    public static new readonly IGameLink<GameDataControlBuffIcon> DefaultTemplate = 
        new GameLink<GameDataControl, GameDataControlBuffIcon>(typeof(BuffIcon).GetHashCode());

    /// <summary>
    /// 绑定的Buff对象
    /// </summary>
    public Buff? Buff
    {
        get => _buff;
        set
        {
            _buff = value;
            if (value == null)
            {
                // Game.Logger?.LogDebug("🖼️ BuffIcon设置为null，隐藏图标");
                Visible = false;
                StopBlinking();
            }
            else
            {
                // Game.Logger?.LogDebug("🖼️ BuffIcon绑定Buff: {type}，显示图标", value.GetType().Name);
                Visible = true;
                UpdateBuffDisplay();
            }
        }
    }

    /// <summary>
    /// 创建BuffData - 从Buff对象中提取所有显示信息
    /// 这个方法整合了原来BuffBar.CreateBuffData的逻辑
    /// </summary>
    public static BuffDisplayData? CreateBuffDisplayData(Buff? buff)
    {
        if (buff == null) return null;

        try
        {
            // 使用和BuffListTestExample相同的方式：检查是否实现IDisplayInfo接口
            if (buff is IDisplayInfo displayInfo)
            {
                // 🎯 使用IDisplayInfo接口获取显示信息（和BuffListTestExample完全一致）
                var name = displayInfo.DisplayName ?? "未知Buff";
                var remainingTime = displayInfo.Cooldown ?? 0;       // IDisplayInfo.Cooldown = 剩余时间
                var duration = displayInfo.CoolDownMax ?? 0;        // IDisplayInfo.CoolDownMax = 最大时间  
                var stack = displayInfo.Stack ?? 1;                 // IDisplayInfo.Stack = 堆叠数
                
                // 🎯 检查非永久buff且剩余时间<=0的情况，直接返回null
                bool isPermanent = false;
                try
                {
                    if (buff.Cache?.BuffFlags != null)
                    {
                        isPermanent = buff.Cache.BuffFlags.Permanent;
                    }
                }
                catch (Exception ex)
                {
                    Game.Logger?.LogWarning("检查BuffFlags.Permanent时出错: {ex}", ex.Message);
                }
                if (isPermanent == false && displayInfo.CoolDownMax == null)
                {
                    isPermanent = true;
                }
                
                // 如果不是永久buff且剩余时间<=0，直接返回null
                if (!isPermanent && remainingTime <= 0)
                {
                    // Game.Logger?.LogDebug("非永久buff且剩余时间<=0，返回null: {name}, 剩余时间: {remainingTime}", name, remainingTime);
                    return null;
                }
                
                // 🖼️ 尝试获取图标信息
                string iconPath = "@gameui/image/buff/buff_1.png";
                try
                {
                    // displayInfo.Icon返回的是GameCore.ResourceType.Icon?类型，需要转换为string
                    var icon = displayInfo.Icon;
                    if (icon.HasValue)
                    {
                        // 尝试不同的属性来获取图片路径
                        var fileName = icon.Value.FileName;
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            iconPath = fileName;
                            // Game.Logger?.LogDebug("🖼️ 从Icon.FileName获取图片路径: {path}", iconPath);
                        }
                        else
                        {
                            // 如果FileName为空，使用默认路径但记录日志
                            iconPath = "@gameui/image/buff/buff_1.png"; // 使用已知存在的图片
                            // Game.Logger?.LogDebug("🖼️ Icon.FileName为空，使用默认图片: {path}", iconPath);
                        }
                    }
                    else
                    {
                        iconPath = "@gameui/image/buff/buff_1.png"; // 使用已知存在的图片
                        // Game.Logger?.LogDebug("🖼️ Icon为null，使用默认图片: {path}", iconPath);
                    }
                }
                catch (Exception ex)
                {
                    iconPath = "@gameui/image/buff/buff_1.png"; // 使用已知存在的图片
                    Game.Logger?.LogWarning("🖼️ 获取图标路径失败，使用默认图片: {ex}", ex.Message);
                }

                // 🎯 从GameDataBuff Cache中读取BuffPolarity和UISettings
                GameCore.BuffSystem.Data.Enum.BuffPolarity polarity = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral;
                
                // 🎯 UI配置默认值
                bool showIcon = true; // 暂时保留，可能需要其他方式判断
                bool showDuration = true;
                GameCore.BuffSystem.Data.Enum.UIDurationFormat durationFormat = GameCore.BuffSystem.Data.Enum.UIDurationFormat.Numeric; // 默认数字倒计时
                bool showStack = true; // 默认显示堆叠
                bool blinkWhenExpiring = true;
                TimeSpan? blinkThreshold = TimeSpan.FromSeconds(2); // 默认2秒开始闪烁
                bool mergeInstances = false; // 默认不合并多个实例
                
                try
                {
                    if (buff.Cache != null)
                    {
                        // 直接读取BuffPolarity属性
                        polarity = buff.Cache.Polarity;
                        // Game.Logger?.LogDebug("🎯 从GameDataBuff.Cache读取到BuffPolarity: {polarity}", polarity);
                        
                        // 🎯 直接获取UISettings配置
                        try
                        {
                            // 直接访问UISettings属性
                            var uiSettings = buff.Cache.UISettings;
                            if (uiSettings != null)
                            {
                                // 🎯 直接获取各个UI配置属性
                                showStack = uiSettings.ShowStackCount;
                                showDuration = uiSettings.DurationFormat != GameCore.BuffSystem.Data.Enum.UIDurationFormat.None;
                                durationFormat = uiSettings.DurationFormat; // 🎯 获取持续时间显示格式
                                mergeInstances = uiSettings.CombineInstances;
                                blinkWhenExpiring = uiSettings.BlinkThreshold.HasValue;
                                blinkThreshold = uiSettings.BlinkThreshold; // 🎯 获取真正的闪烁阈值
                                
                                // Game.Logger?.LogDebug("🎯 从GameDataBuff.Cache.UISettings读取到UI配置: 显示堆叠{stack}, 显示时间{duration}, 合并实例{merge}, 闪烁{blink}", 
                                    // showStack, showDuration, mergeInstances, blinkWhenExpiring);
                            }
                            else
                            {
                                // Game.Logger?.LogDebug("🔍 GameDataBuff.Cache.UISettings为空，使用默认配置");
                            }
                        }
                        catch (Exception uiEx)
                        {
                            // UISettings获取失败，使用默认值
                            Game.Logger?.LogDebug("获取UISettings失败，使用默认配置: {ex}", uiEx.Message);
                        }
                    }
                    else
                    {
                        // Cache为空，使用默认中性
                        polarity = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral;
                        // Game.Logger?.LogDebug("🔍 GameDataBuff.Cache为空，使用默认中性");
                    }
                }
                catch (Exception ex)
                {
                    // 出错时使用默认中性
                    polarity = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral;
                    Game.Logger?.LogWarning("读取GameDataBuff.BuffPolarity时出错，使用默认中性: {ex}", ex.Message);
                }

                var buffData = new BuffDisplayData
                {
                    Buff = buff,
                    Name = name,
                    Stack = (int)stack,
                    Remaining = (float)remainingTime, // ✅ 使用IDisplayInfo的剩余时间
                    Duration = (float)(duration > 0 ? duration : remainingTime), // ✅ 使用IDisplayInfo的总时间
                    Description = name, // 使用displayName作为描述
                    IconPath = iconPath,
                    Polarity = polarity, // ✅ 使用从GameDataBuff.Cache读取的BuffPolarity
                    
                    // 🎯 设置UI配置选项
                    ShowIcon = showIcon,
                    ShowDuration = showDuration,
                    DurationFormat = durationFormat,
                    ShowStack = showStack,
                    BlinkWhenExpiring = blinkWhenExpiring,
                    BlinkThreshold = blinkThreshold,
                    MergeInstances = mergeInstances
                };

                // Game.Logger?.LogDebug("通过IDisplayInfo创建BuffDisplayData: {name}, 剩余时间: {remaining}s, 总时间: {total}s, 堆叠: {stack}", 
                    // name, remainingTime, duration, stack);
                return buffData;
            }
            else
            {
                // 如果不是标准Buff类型，使用默认值
                // Game.Logger?.LogDebug("Buff对象不是GameCore.Behavior.Buff类型: {type}", buff.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            Game.Logger?.LogWarning("读取Buff属性时出错（使用IDisplayInfo方式）: {ex}", ex.Message);
            // 出错的情况下直接返回null，让buff隐藏
            return null;
        }

        // 返回兜底的默认数据（用于非标准Buff或读取失败的情况）
        return new BuffDisplayData
        {
            Buff = buff,
            Name = $"默认Buff_{buff.GetHashCode()}",
            IconPath = "@gameui/image/buff/default.png",
            Polarity = GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral,
            Stack = 1,
            Remaining = 15.0f, // 兜底默认值
            Duration = 30.0f,
            Description = $"默认Buff_{buff.GetHashCode()}",
            
            // 默认UI配置
            ShowIcon = true,
            ShowDuration = true,
            DurationFormat = GameCore.BuffSystem.Data.Enum.UIDurationFormat.Numeric, // 默认数字倒计时
            ShowStack = true, // 默认显示堆叠
            BlinkWhenExpiring = true,
            BlinkThreshold = TimeSpan.FromSeconds(2), // 默认2秒开始闪烁
            MergeInstances = false // 默认不合并多个实例
        };
    }



    /// <summary>
    /// Buff极性
    /// </summary>
    public GameCore.BuffSystem.Data.Enum.BuffPolarity Polarity
    {
        get => _polarity;
        private set
        {
            _polarity = value;
            UpdateBuffBgColor();
        }
    }

    /// <summary>
    /// 冷却进度 (0-1)
    /// </summary>
    public float CooldownProgress
    {
        get => _cooldownProgress;
        private set
        {
            _cooldownProgress = value;
            buffCooldownProgress.Value = value;
        }
    }

    /// <summary>
    /// 冷却时间文本
    /// </summary>
    public string CooldownText
    {
        get => _cooldownText;
        private set
        {
            _cooldownText = value;
            buffCooldownNumLabel.Text = value;
        }
    }

    /// <summary>
    /// 堆叠数量文本
    /// </summary>
    public string StackText
    {
        get => _stackText;
        private set
        {
            _stackText = value;
            buffStackNumLabel.Text = value;
        }
    }

    public BuffIcon(IGameLink<GameDataControlBuffIcon> link) : base(link)
    {
        // 创建UI组件
        mainPanel = new Panel();
        buffBgPanel = new Panel();
        buffIconPanel = new Panel();
        buffCooldownPanel = new Panel();
        buffCooldownProgress = new Progress();
        buffCooldownNumLabel = new Label();
        buffStackNumLabel = new Label();

        InitializeUI();
        SubscribeToBuffUpdates();
    }

    public BuffIcon() : this(DefaultTemplate)
    {
    }

    /// <summary>
    /// 创建BuffIcon实例
    /// </summary>
    public static BuffIcon Create()
    {
        return new BuffIcon();
    }

    /// <summary>
    /// 创建BuffIcon实例并绑定Buff
    /// </summary>
    public static BuffIcon Create(Buff buff)
    {
        var icon = new BuffIcon();
        icon.Buff = buff;
        return icon;
    }

    private void InitializeUI()
    {
        // 设置主面板 - 修改：不拉伸，使用固定尺寸，避免影响父级布局
        WidthStretchRatio = 0.0f; // 修改：不拉伸宽度
        HeightStretchRatio = 0.0f; // 修改：不拉伸高度
        Width = 64; // 修改：设置固定宽度
        Height = 64; // 修改：设置固定高度

        // 主面板设置 - 使用硬编码值避免Cache访问问题
        mainPanel.Width = 64;
        mainPanel.Height = 64;
        mainPanel.Margin = new GameUI.Struct.Thickness(14);
        mainPanel.Opacity = 1.0f;

        // 背景边框面板 - 使用硬编码值
        buffBgPanel.Width = 64 + Math.Max(1, (int)Math.Ceiling(64 / 16.0)) * 2;
        buffBgPanel.Height = 64 + Math.Max(1, (int)Math.Ceiling(64 / 16.0)) * 2;
        buffBgPanel.Background = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));

        // 图标面板 - 使用硬编码值
        buffIconPanel.WidthStretchRatio = 1.0f;
        buffIconPanel.HeightStretchRatio = 1.0f;
        // buffIconPanel.Image = "@gameui/image/buff/buff_1.png"; // 暂时不设置图片路径

        // 冷却面板
        buffCooldownPanel.WidthStretchRatio = 1.0f;
        buffCooldownPanel.HeightStretchRatio = 1.0f;

        // 冷却进度条
        buffCooldownProgress.WidthStretchRatio = 1.0f;
        buffCooldownProgress.HeightStretchRatio = 1.0f;
        buffCooldownProgress.Value = 0.5f;
        buffCooldownProgress.ProgressionMode = ProgressionMode.Clockwise;
        buffCooldownProgress.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)); // 半透明黑色
        buffCooldownProgress.Visible = false; // 🎯 默认隐藏进度条

        // 冷却数字标签
        buffCooldownNumLabel.WidthStretchRatio = 1.0f;
        buffCooldownNumLabel.HeightStretchRatio = 1.0f;
        buffCooldownNumLabel.Text = "5";
        buffCooldownNumLabel.FontSize = 24; // 硬编码字体大小
        buffCooldownNumLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
        buffCooldownNumLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Center;
        buffCooldownNumLabel.TextColor = new SolidColorBrush(Color.White);
        buffCooldownNumLabel.Visible = false; // 🎯 默认隐藏数字标签

        // 堆叠数量标签
        buffStackNumLabel.Width = -1;
        buffStackNumLabel.Height = -1;
        buffStackNumLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        buffStackNumLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        buffStackNumLabel.Margin = new GameUI.Struct.Thickness(0, 0, 4, 2);
        buffStackNumLabel.Text = "5";
        buffStackNumLabel.FontSize = 24; // 硬编码字体大小
        buffStackNumLabel.TextColor = new SolidColorBrush(Color.White);

        // 构建层级关系
        buffCooldownPanel.AddChild(buffCooldownProgress);
        buffCooldownPanel.AddChild(buffCooldownNumLabel);
        
        mainPanel.AddChild(buffBgPanel);
        mainPanel.AddChild(buffIconPanel);
        mainPanel.AddChild(buffCooldownPanel);
        mainPanel.AddChild(buffStackNumLabel);

        AddChild(mainPanel);

        // 初始状态
        Visible = false;
    }

    /// <summary>
    /// 设置Buff极性
    /// </summary>
    public void SetPolarity(GameCore.BuffSystem.Data.Enum.BuffPolarity polarity)
    {
        Polarity = polarity;
    }

    /// <summary>
    /// 设置Buff图标
    /// </summary>
    public void SetIcon(string iconPath)
    {
        try
        {
            // Game.Logger?.LogDebug("🖼️ BuffIcon设置图片路径: {path}", iconPath);
            buffIconPanel.Image = iconPath;
            
            // 验证图片是否设置成功
            if (string.IsNullOrEmpty(buffIconPanel.Image))
            {
                // Game.Logger?.LogWarning("🖼️ BuffIcon图片设置后为空，尝试设置默认图片");
                buffIconPanel.Image = "@gameui/image/buff/buff_1.png";
            }
        }
        catch (Exception ex)
        {
            Game.Logger?.LogError(ex, "🖼️ BuffIcon设置图片失败: {path}, 错误: {message}", iconPath, ex.Message);
            // 设置一个确定存在的默认图片
            buffIconPanel.Image = "@gameui/image/buff/buff_1.png";
        }
    }

    /// <summary>
    /// 设置冷却进度 - 支持不同的UI显示格式
    /// </summary>
    public void SetCooldown(float remaining, float total, GameCore.BuffSystem.Data.Enum.UIDurationFormat format = GameCore.BuffSystem.Data.Enum.UIDurationFormat.Numeric)
    {
        if (remaining <= 0 || total <= 0)
        {
            CooldownProgress = 0;
            CooldownText = "";
            // 隐藏进度条和数字
            buffCooldownProgress.Visible = false;
            buffCooldownNumLabel.Visible = false;
            return;
        }

        // 根据极性设置进度条样式
        ProgressType progressType = _polarity switch
        {
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Positive => ProgressType.Clockwise,
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Negative => ProgressType.Clockwise,
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral => ProgressType.Clockwise,
            _ => ProgressType.Clockwise
        };

        buffCooldownProgress.ProgressionMode = progressType switch
        {
            ProgressType.CounterClockwise => ProgressionMode.CounterClockwise,
            _ => ProgressionMode.Clockwise
        };

        CooldownProgress = remaining / total;
        CooldownText = ((int)Math.Ceiling(remaining)).ToString();

        // 🎯 根据DurationFormat控制显示内容
        switch (format)
        {
            case GameCore.BuffSystem.Data.Enum.UIDurationFormat.None:
                // 不显示任何时间信息
                buffCooldownProgress.Visible = false;
                buffCooldownNumLabel.Visible = false;
                break;
                
            case GameCore.BuffSystem.Data.Enum.UIDurationFormat.Numeric:
                // 只显示数字，不显示进度条
                buffCooldownProgress.Visible = false;
                buffCooldownNumLabel.Visible = true;
                break;
                
            case GameCore.BuffSystem.Data.Enum.UIDurationFormat.CircularProgress:
                // 只显示圆形进度条，不显示数字
                buffCooldownProgress.Visible = true;
                buffCooldownNumLabel.Visible = false;
                break;
                
            default:
                // 默认显示数字
                buffCooldownProgress.Visible = false;
                buffCooldownNumLabel.Visible = true;
                break;
        }
    }

    /// <summary>
    /// 设置堆叠数量
    /// </summary>
    public void SetStack(int stack)
    {
        StackText = stack == 1 ? "" : stack.ToString();
    }

    /// <summary>
    /// 设置闪烁效果
    /// </summary>
    public void SetBlink()
    {
        if (_isBlinking) 
            return;

        StartBlinking();
    }



    private void UpdateBuffDisplay()
    {
        try
        {
            if (_buff != null)
            {
                // 使用统一的方法来创建Buff显示数据
                var buffData = CreateBuffDisplayData(_buff);
                if (buffData != null)
                {
                    // 🎯 检查是否应该显示这个Buff图标
                    if (!buffData.ShowIcon)
                    {
                        Visible = false;
                        return;
                    }
                    
                    // 设置显示信息
                    SetPolarity(buffData.Polarity);
                    SetIcon(buffData.IconPath);
                    
                    // 🎯 根据UI配置设置CD显示
                    if (buffData.ShowDuration)
                    {
                        // 设置CD显示 - 处理永久buff
                        if (buffData.Duration <= 0 && buffData.Remaining <= 0)
                        {
                            SetCooldown(0, 0, buffData.DurationFormat); // 永久buff不显示冷却
                        }
                        else
                        {
                            SetCooldown(buffData.Remaining, buffData.Duration, buffData.DurationFormat); // 🎯 传递DurationFormat参数
                        }
                    }
                    else
                    {
                        // 不显示持续时间
                        SetCooldown(0, 0, GameCore.BuffSystem.Data.Enum.UIDurationFormat.None);
                    }
                    
                    // 🎯 根据UI配置设置堆叠显示
                    if (buffData.ShowStack)
                    {
                        SetStack(buffData.Stack);
                    }
                    else
                    {
                        SetStack(1); // 不显示堆叠数量，传入1会隐藏数字
                    }
                    
                    // 🎯 根据UI配置设置闪烁逻辑 - 使用真正的BlinkThreshold值
                    if (buffData.BlinkWhenExpiring && buffData.BlinkThreshold.HasValue && buffData.Remaining > 0)
                    {
                        // 使用配置的闪烁阈值
                        double thresholdSeconds = buffData.BlinkThreshold.Value.TotalSeconds;
                        if (buffData.Remaining <= thresholdSeconds)
                        {
                            SetBlink();
                        }
                        else
                        {
                            StopBlinking();
                        }
                    }
                    else
                    {
                        StopBlinking();
                    }
                    
                    // Game.Logger?.LogDebug("BuffIcon显示: {name}, 剩余时间{remaining}s, 总时间{total}s, 堆叠{stack}, UI配置: 显示图标{showIcon}, 显示时间{showDuration}, 显示堆叠{showStack}, 闪烁{blink}", 
                        // buffData.Name, buffData.Remaining, buffData.Duration, buffData.Stack, buffData.ShowIcon, buffData.ShowDuration, buffData.ShowStack, buffData.BlinkWhenExpiring);
                }
                else
                {
                    // Game.Logger?.LogWarning("无法创建BuffDisplayData，隐藏BuffIcon");
                    Visible = false;
                    return;
                }
                
                Visible = true;
            }
            else
            {
                // Game.Logger?.LogDebug("BuffIcon没有绑定Buff对象");
                Visible = false;
            }
        }
        catch (Exception ex)
        {
            Game.Logger?.LogError(ex, "更新BuffIcon显示时出错: {message}", ex.Message);
        }
    }

    private void UpdateBuffBgColor()
    {
        Color bgColor = _polarity switch
        {
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Positive => Color.FromArgb(52, 180, 31), // 正面极性绿色
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Negative => Color.FromArgb(231, 67, 57), // 负面极性红色
            GameCore.BuffSystem.Data.Enum.BuffPolarity.Neutral => Color.FromArgb(154, 154, 154), // 中性极性灰色
            _ => Color.FromArgb(154, 154, 154) // 默认灰色
        };

        buffBgPanel.Background = new SolidColorBrush(bgColor);
    }

    private void StartBlinking()
    {
        if (_isBlinking)
            return; // 已经在闪烁中，避免重复启动
            
        _isBlinking = true;
        
        // 创建NumberAnimation<float>来控制透明度 - 使用专门的数值动画类
        _blinkAnimation = new NumberAnimation<float>
        {
            From = 0.0f,  // 从透明开始
            To = 1.0f,    // 到不透明
            Duration = TimeSpan.FromMilliseconds(200), // 单次动画200ms
            AutoReverse = true, // 自动反向（1.0f -> 0.0f）
            RepeatBehavior = GameCore.Animation.Enum.RepeatBehavior.Forever, // 无限循环
            EasingFunction = new SineEase 
            { 
                EasingMode = GameCore.Animation.Enum.EasingMode.EaseInOut 
            }, // 使用正弦缓动，开始和结束缓慢，中间快速
            OnUpdate = (opacity) => {
                // 每次动画更新时设置主面板的透明度
                mainPanel.Opacity = opacity;
            }
        };
        
        // 启动动画
        _blinkAnimation.Start();
        _isBlinkAnimationRunning = true;
    }


    private void StopBlinking()
    {
        _isBlinking = false;
        
        // 停止并释放动画
        if (_blinkAnimation != null && _isBlinkAnimationRunning)
        {
            _blinkAnimation.Cancel(GameCore.Animation.Enum.CancelBehavior.Hold); // 取消动画并保持当前状态
            _blinkAnimation = null;
            _isBlinkAnimationRunning = false;
        }
        
        // 恢复正常透明度
        mainPanel.Opacity = 1.0f;
    }





    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        StopBlinking();
        
        // 清理UI组件
        mainPanel?.Destroy();
        buffBgPanel?.Destroy();
        buffIconPanel?.Destroy();
        buffCooldownPanel?.Destroy();
        buffCooldownProgress?.Destroy();
        buffCooldownNumLabel?.Destroy();
        buffStackNumLabel?.Destroy();
        
        _buff = null;
        
        // 取消自动更新订阅
        UnsubscribeFromBuffUpdates();
    }
    
    private void SubscribeToBuffUpdates()
    {
        // 订阅BuffBar的更新事件来自动刷新BuffIcon
        BuffBar.OnBuffUpdateRequested += HandleBuffUpdate;
    }
    
    private void UnsubscribeFromBuffUpdates()
    {
        // 取消订阅
        BuffBar.OnBuffUpdateRequested -= HandleBuffUpdate;
    }
    
    private void HandleBuffUpdate()
    {
        try
        {
            UpdateBuffDisplay();
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("BuffIcon自动更新时出错: {ex}", ex.Message);
        }
    }
}
#endif

