#if CLIENT
using System;
using System.Drawing;
using GameCore.AbilitySystem;
using GameCore.BaseType;
using GameCore.ResourceType;
using GameUI.Brush;
using GameUI.Control.Enum;
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameCore.DisplayInfo;
using GameData;
using GameData.Interface;
using GameData.Extension;
using GameSystemUI.AbilitySystemUI.Data;
using GameUI.Control.Data;

namespace GameSystemUI.AbilitySystemUI.Advanced;

/// <summary>
/// 充能技能冷却显示UI组件
/// </summary>
[GameObject<GameDataControlCoolDownChargeUI>]
public partial class CoolDownChargeUI : Panel, IThinker, IGameObject<GameDataControlCoolDownChargeUI>, IGameObject
{
    private Label stackLabel = null!;
    private Progress chargeProgressBar = null!;
    private AbilityActive? _skill;
    private int _stackCount;
    private Image _backgroundImage = new Image("@gameui/image/control/冷却.png");
    private Image _chargeProgressImage = new Image("@gameui/image/control/充能技能冷却条.png");

    /// <summary>
    /// 绑定的技能
    /// </summary>
    public AbilityActive? BindSkill
    {
        get => _skill;
        set
        {
            if(_skill == value)
                return;
            
            // 移除旧技能的事件监听
            if (_skill != null && _skill.IsValid && _skill.Charge != null)
            {
                _skill.Charge.OnCooldown -= OnSkillCooldownChanged;
            }
                
            _skill = value;
            
            // 添加新技能的事件监听
            if (_skill != null && _skill.IsValid && _skill.Charge != null)
            {
                _skill.Charge.OnCooldown += OnSkillCooldownChanged;
            }
            
            Update();
        }
    }

    /// <summary>
    /// 层数计数
    /// </summary>
    public int StackCount
    {
        get => _stackCount;
        private set
        {
            _stackCount = value;
            stackLabel.Text = value.ToString();
        }
    }

    /// <summary>
    /// 充能进度 (0-1)
    /// </summary>
    public float ChargeProgress
    {
        get => chargeProgressBar.Value;
        private set => chargeProgressBar.Value = value;
    }
    
    /// <summary>
    /// 背景图片
    /// </summary>
    public Image BackgroundImage
    {
        get => _backgroundImage;
        set
        {
            _backgroundImage = value;
            Image = _backgroundImage.Path;
        }
    }
    
    /// <summary>
    /// 充能进度条图片
    /// </summary>
    public Image ChargeProgressImage
    {
        get => _chargeProgressImage;
        set
        {
            _chargeProgressImage = value;
            if (chargeProgressBar != null)
                chargeProgressBar.Image = _chargeProgressImage.Path;
        }
    }

    /// <summary>
    /// 是否为充能技能
    /// </summary>
    private bool IsChargeSkill =>
        (_skill != null && _skill.IsValid) && ((IDisplayInfo?)_skill)?.ChargeCooldown.HasValue == true && ((IDisplayInfo?)_skill)?.ChargeCooldownMax.HasValue == true;

    /// <summary>
    /// 是否有层数系统
    /// </summary>
    private bool HasStackSystem => (_skill != null && _skill.IsValid) && ((IDisplayInfo?)_skill)?.Stack.HasValue == true;

    /// <summary>
    /// 当前充能冷却时间
    /// </summary>
    private float CurrentChargeCd => (float)((_skill != null && _skill.IsValid) ? ((IDisplayInfo?)_skill)?.ChargeCooldown ?? 0.0 : 0.0);
    
    /// <summary>
    /// 最大充能冷却时间
    /// </summary>
    private float ChargeCdMax => (float)((_skill != null && _skill.IsValid) ? ((IDisplayInfo?)_skill)?.ChargeCooldownMax ?? 1.0 : 1.0);
    
    /// <summary>
    /// 当前层数
    /// </summary>
    private int CurrentStackCount => (_skill != null && _skill.IsValid) ? ((IDisplayInfo?)_skill)?.Stack ?? 0 : 0;

    /// <summary>
    /// 默认模板
    /// </summary>
    public static new readonly IGameLink<GameDataControlCoolDownChargeUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlCoolDownChargeUI>(typeof(CoolDownChargeUI).GetHashCode(true));

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public CoolDownChargeUI() : this(DefaultTemplate)
    {
    }
    
    /// <summary>
    /// GameData 构造函数
    /// </summary>
    public CoolDownChargeUI(IGameLink<GameDataControlCoolDownChargeUI> link) : base(link)
    {
        InitializeUI();
    }

    /// <summary>
    /// 初始化CoolDownChargeUI实例
    /// </summary>
    private void InitializeUI()
    {
        Width = 45;
        Height = 45;
        
        // 初始化层数标签
        stackLabel = new Label()
        {
            WidthStretchRatio = 1,
            HeightStretchRatio = 1,
            FontSize = 30,
            TextColor = Color.White,
            TextTrimming = TextTrimming.Shrink,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Center,
            Bold = true,
        };
        
        // 初始化充能进度条
        chargeProgressBar = new Progress()
        {
            WidthStretchRatio = 1,
            HeightStretchRatio = 1,
            WidthCompactRatio = 1,
            HeightCompactRatio = 1,
            Value = 1,
            ProgressionMode = ProgressionMode.CounterClockwise,
        };
        
        AddChild(stackLabel);
        AddChild(chargeProgressBar);
        
        // 设置初始图片
        Image = BackgroundImage.Path;
        chargeProgressBar.Image = ChargeProgressImage.Path;
        
        Update();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    public void Update()
    {
        if (_skill == null || !_skill.IsValid)
        {
            ResetDisplay();
            // 没有技能时停止思考以节省性能
            ((IThinker)this).DoesThink = false;
            return;
        }

        this.Visible = true;
        UpdateChargeDisplay();
        UpdateStackDisplay();
        
        // 根据充能状态控制是否需要思考
        bool needsThinking = HasActiveCharging();
        ((IThinker)this).DoesThink = needsThinking;
    }

    /// <summary>
    /// IThinker接口实现，定期更新充能状态
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public void Think(int delta)
    {
        Update();
    }
    
    /// <summary>
    /// 判断是否有正在进行的充能需要持续更新
    /// </summary>
    private bool HasActiveCharging()
    {
        if (_skill == null || !_skill.IsValid) return false;
        
        float cd = CurrentChargeCd;
        return cd > 0;
    }
    
    /// <summary>
    /// 技能冷却状态变化事件处理
    /// </summary>
    private void OnSkillCooldownChanged()
    {
        Update();
    }

    /// <summary>
    /// 更新充能显示
    /// </summary>
    private void UpdateChargeDisplay()
    {
        if (_skill == null || !_skill.IsValid)
        {
            ChargeProgress = 1.0f;
            return;
        }

        float cd = CurrentChargeCd;
        float total = ChargeCdMax;
        
        if (cd <= 0 || total <= 0)
        {
            ChargeProgress = 1.0f;
        }
        else
        {
            total = Math.Max(total, cd);
            // 充能进度：1=充能完成，0=正在充能
            ChargeProgress = 1.0f - (cd / total);
        }
    }

    /// <summary>
    /// 更新层数显示
    /// </summary>
    private void UpdateStackDisplay()
    {
        if (_skill != null && _skill.IsValid && HasStackSystem)
        {
            StackCount = CurrentStackCount;
            stackLabel.Visible = true;
        }
        else
        {
            StackCount = 0;
            stackLabel.Visible = false;
        }
    }

    /// <summary>
    /// 重置显示状态
    /// </summary>
    private void ResetDisplay()
    {
        ChargeProgress = 1.0f;
        StackCount = 0;
        stackLabel.Visible = false;
        this.Visible = false;
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        // 移除事件监听
        if (_skill != null && _skill.IsValid && _skill.Charge != null)
        {
            _skill.Charge.OnCooldown -= OnSkillCooldownChanged;
        }
        
        // 停止思考以避免资源泄露
        ((IThinker)this).DoesThink = false;
        
        base.DisposeManaged();
        
        stackLabel?.Destroy();
        chargeProgressBar?.Destroy();
        _skill = null;
    }
}

#endif