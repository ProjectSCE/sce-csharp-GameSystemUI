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
/// 技能冷却显示UI组件
/// </summary>
[GameObject<GameDataControlCoolDownUI>]
public partial class CoolDownUI : Panel, IThinker, IGameObject<GameDataControlCoolDownUI>, IGameObject
{
    private Panel MainPanel { get; set; } = new Panel();
    private Progress CoolProgressBar { get; set; } = new Progress();
    private Label CoolLabel { get; set; } = new Label();
    private AbilityActive? _skill;
    private float _coolProgress;
    private string _coolText = string.Empty;
    private Image _cooldownOverlay = new Image("@gameui/image/control/冷却.png");

    /// <summary>
    /// 绑定的技能
    /// </summary>
    public AbilityActive? BindSkill
    {
        get => _skill;
        set
        {
            if(_skill == value)
            {
                return;
            }

            // 解绑老的
            if(_skill != null && _skill.IsValid && _skill.Cooldown != null)
            {
                _skill.Cooldown.OnCooldown -= OnSkillCooldownChanged;
            }

            // 更新_skill顺便刷新UI
            _skill = value;
            Update();

            // 绑定新的
            if(_skill != null && _skill.IsValid && _skill.Cooldown != null)
            {
                _skill.Cooldown.OnCooldown += OnSkillCooldownChanged;
            }
        }
    }

    /// <summary>
    /// 冷却进度 (0-1)
    /// </summary>
    public float CoolProgress
    {
        get => _coolProgress;
        private set
        {
            _coolProgress = value;
            if (CoolProgressBar != null)
                CoolProgressBar.Value = value;
        }
    }

    /// <summary>
    /// 冷却进度条遮罩
    /// </summary>
    public Image CooldownOverlay 
    { 
        get => _cooldownOverlay;
        set
        {
            _cooldownOverlay = value;
            if (CoolProgressBar != null)
                CoolProgressBar.Image = _cooldownOverlay.Path;
        }
    }

    /// <summary>
    /// 冷却时间文本
    /// </summary>
    public string CoolText
    {
        get => _coolText;
        private set
        {
            _coolText = value;
            if (CoolLabel != null)
                CoolLabel.Text = value;
        }
    }

    /// <summary>
    /// 当前冷却时间
    /// </summary>
    private float CurrentCd => (float)((_skill != null && _skill.IsValid) ? ((IDisplayInfo?)_skill)?.Cooldown ?? 0.0 : 0.0);
    
    /// <summary>
    /// 最大冷却时间
    /// </summary>
    private float CdMax => (float)((_skill != null && _skill.IsValid) ? ((IDisplayInfo?)_skill)?.CoolDownMax ?? 1.0 : 1.0);
  
    /// <summary>
    /// 默认模板
    /// </summary>
    public static new readonly IGameLink<GameDataControlCoolDownUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlCoolDownUI>(typeof(CoolDownUI).GetHashCode(true));

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public CoolDownUI() : this(DefaultTemplate)
    {
    }
    
    /// <summary>
    /// GameData 构造函数
    /// </summary>
    public CoolDownUI(IGameLink<GameDataControlCoolDownUI> link) : base(link)
    {
        Initialize();
    }

    /// <summary>
    /// 创建并初始化CoolDownUI实例，同时设置技能
    /// </summary>
    public static CoolDownUI Create(AbilityActive skill)
    {
        var ui = new CoolDownUI();
        ui.Initialize();
        ui.BindSkill = skill;
        return ui;
    }

    private void Initialize()
    {
        MainPanel.WidthStretchRatio = 1.0f;
        MainPanel.HeightStretchRatio = 1.0f;
        MainPanel.WidthCompactRatio = 1.0f;
        MainPanel.HeightCompactRatio = 1.0f;

        CoolProgressBar.Image = CooldownOverlay.Path;
        CoolProgressBar.WidthStretchRatio = 1.0f;
        CoolProgressBar.HeightStretchRatio = 1.0f;
        CoolProgressBar.WidthCompactRatio = 1.0f;
        CoolProgressBar.HeightCompactRatio = 1.0f;
        CoolProgressBar.Value = 0;
        CoolProgressBar.ZIndex = 10;
        CoolProgressBar.ProgressionMode = ProgressionMode.Clockwise;

        CoolLabel.Text = "";
        CoolLabel.FontSize = 42;
        CoolLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
        CoolLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Center;
        CoolLabel.ZIndex = 20;

        MainPanel.AddChild(CoolProgressBar);
        MainPanel.AddChild(CoolLabel);
        this.AddChild(MainPanel);

        if (!string.IsNullOrEmpty(CooldownOverlay.Path) && CoolProgressBar != null)
        {
            CoolProgressBar.Image = CooldownOverlay.Path;
        }
    }

    private void OnSkillCooldownChanged()
    {
        Update();
    }

    /// <summary>
    /// IThinker接口实现，定期更新冷却状态
    /// </summary>
    /// <param name="delta">时间间隔（毫秒）</param>
    public void Think(int delta)
    {
        Update();
    }


    /// <summary>
    /// 更新冷却显示，顺便开关IThinker
    /// </summary>
    private void Update()
    {
        if (_skill == null || !_skill.IsValid)
        {
            CoolProgress = 0;
            CoolText = "";
            ((IThinker)this).DoesThink = false;
            return;
        }

        float cd = CurrentCd;
        float total = CdMax;
        
        if (cd <= 0 || total <= 0)
        {
            CoolProgress = 0;
            CoolText = "";
            ((IThinker)this).DoesThink = false;
        }
        else
        {
            total = Math.Max(total, cd);
            CoolProgress = cd / total;
            CoolText = FormatCoolTime(cd);
            ((IThinker)this).DoesThink = true;
        }
    }

    private string FormatCoolTime(float seconds, bool showDecimal = true)
    {
        if (seconds < 1)
        {
            return showDecimal ? $"{seconds:F1}" : "1";
        }
        else if (seconds < 60)
        {
            return showDecimal && seconds < 10 ? $"{seconds:F1}" : $"{(int)seconds}";
        }
        else
        {
            var minutes = (int)(seconds / 60);
            var remainingSeconds = (int)(seconds % 60);
            return $"{minutes}:{remainingSeconds:D2}";
        }
    }

    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        if (_skill != null && _skill.IsValid && _skill.Cooldown != null)
        {
            _skill.Cooldown.OnCooldown -= OnSkillCooldownChanged;
        }
        ((IThinker)this).DoesThink = false;

        base.DisposeManaged();
        
        MainPanel?.Destroy();
        CoolProgressBar?.Destroy();
        CoolLabel?.Destroy();
        _skill = null;
    }
}
#endif 