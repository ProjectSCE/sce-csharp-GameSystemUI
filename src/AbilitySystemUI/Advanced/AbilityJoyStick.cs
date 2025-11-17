#if CLIENT
using Events;
using GameCore.AbilitySystem;
using GameCore.DisplayInfo;
using GameCore.Event;
using GameCore.Platform.SDL;
using GameData;
using GameUI.Control.Primitive;
using GameUI.Struct;
using GameUI.Control.Struct;
using GameCore.ResourceType;
using System.Numerics;
using GameUI.Control.Data;
using GameData.Extension;
using GameSystemUI.AbilitySystemUI.Data;
using GameSystemUI.CmdResultSystemUI;
using GameUI.Control.Enum;
using System;
using System.Drawing;
using System.Threading.Tasks;
using GameCore.AbilitySystem.Enum;
using System.Data;

namespace GameSystemUI.AbilitySystemUI.Advanced;

/// <summary>
/// 技能摇杆控件，支持施法和建造模式，自动根据技能类型选择合适的摇杆
/// </summary>
[GameObject<GameDataControlAbilityJoyStick>]
public partial class AbilityJoyStick : Panel
{
    /// <summary>
    /// 默认模板链接
    /// </summary>
    public static new readonly IGameLink<GameDataControlAbilityJoyStick> DefaultTemplate = new GameLink<GameDataControl, GameDataControlAbilityJoyStick>(typeof(AbilityJoyStick).GetHashCode(true));


    private CastingJoyStick? castingJoyStick = null;
    private BuildingJoyStick? buildingJoyStick = null;
    private Joystick? currentJoyStick = null;
    private StopCastingButton? stopCastingButton = null;
    private readonly Panel abilityIconPanel = new Panel();
    private readonly Panel abilityFramePanel = new Panel() { Visible = false };
    private readonly Label abilityNameLabel = new Label() { Text = "" };
    private readonly Label passiveAbilityLabel = new Label() { Text = "被动", Visible = false };
    private readonly Panel abilityDisabledIconPanel = new Panel() { Visible = false };
    private readonly GameUI.Control.Primitive.Particle toggleOnParticle;
    private readonly CoolDownUI cooldownUI;
    private readonly CoolDownChargeUI coolDownChargeUI;
    private readonly BindKeyUI bindKeyUI = new BindKeyUI();
    private Ability? ability;
    private bool isEnabled = true;
    private bool isActivated = false;
    private Image _abilityActiveFrame = new Image("@gameui/image/control/点击技能框.png");
    private Image _abilityBackgroundFrame = new Image("@gameui/image/Control/默认技能框a.png");
    private Image _abilityDisabledFrame = new Image("@gameui/image/control/禁用.png");

    /// <summary>
    /// 激活状态技能框图片
    /// </summary>
    public Image AbilityActiveFrame 
    { 
        get => _abilityActiveFrame;
        set 
        {
            _abilityActiveFrame = value;
            if (abilityFramePanel != null)
                abilityFramePanel.Image = _abilityActiveFrame.Path;
        }
    }
    
    /// <summary>
    /// 默认背景技能框图片
    /// </summary>
    public Image AbilityBackgroundFrame 
    { 
        get => _abilityBackgroundFrame;
        set 
        {
            _abilityBackgroundFrame = value;
            Image = _abilityBackgroundFrame.Path;
        }
    }
    
    /// <summary>
    /// 禁用状态技能框图片
    /// </summary>
    public Image AbilityDisabledFrame 
    { 
        get => _abilityDisabledFrame;
        set 
        {
            _abilityDisabledFrame = value;
            if (abilityDisabledIconPanel != null)
                abilityDisabledIconPanel.Image = _abilityDisabledFrame.Path;
        }
    }

    /// <summary>
    /// 绑定的技能（支持所有Ability类型）
    /// </summary>
    public Ability? Ability
    {
        get => ability;
        set
        {
            // 解绑旧技能的事件
            if (ability != null)
            {
                ability.OnAttachedObjectStateChanged -= OnAttachedObjectStateChanged;
            }
            
            ability = value;
            
            // 根据技能类型选择合适的摇杆
            SelectAppropriateJoyStick();
            
            // 冷却UI绑定：只有AbilityActive类型才能绑定到冷却UI
            var abilityActive = ability as AbilityActive;
            if (cooldownUI != null)
                cooldownUI.BindSkill = abilityActive;
            
            if (coolDownChargeUI != null)
                coolDownChargeUI.BindSkill = abilityActive;
            
            // 订阅新技能状态变化事件
            if(ability is not null)
            {
                ability.OnAttachedObjectStateChanged += OnAttachedObjectStateChanged;
            }
            
            // 初始化切换技能特效显示状态
            if (ability is AbilityToggle abilityToggle)
            {
                toggleOnParticle.Visible = abilityToggle.ToggledOn;
            }
            else
            {
                toggleOnParticle.Visible = false;
            }
                
            UpdateAbilityDisplay();
            IsEnabled = ability?.IsEnabled ?? false;
        }
    }
    
    
    /// <summary>
    /// 是否为被动技能（基于技能类型判断）
    /// </summary>
    public bool IsPassiveAbility => ability != null && !(ability is AbilityActive);
    
    /// <summary>
    /// 按钮是否可用
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            abilityDisabledIconPanel.Visible = !isEnabled;
        }
    }
    
    /// <summary>
    /// 施法摇杆的默认背景图片
    /// </summary>
    public Image CastingJoyStickDefaultBackground
    {
        get => (currentJoyStick as CastingJoyStick)?.DefaultBackground ?? (currentJoyStick as BuildingJoyStick)?.DefaultBackground ?? new Image();
        set
        {
            if (currentJoyStick != null)
            {
                if (currentJoyStick is CastingJoyStick castingJoyStick)
                    castingJoyStick.DefaultBackground = value;
                else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                    buildingJoyStick.DefaultBackground = value;
            }
        }
    }
    
    /// <summary>
    /// 施法摇杆的禁用状态背景图片
    /// </summary>
    public Image CastingJoyStickDefaultBackgroundDisable
    {
        get => (currentJoyStick as CastingJoyStick)?.DefaultBackgroundDisable ?? (currentJoyStick as BuildingJoyStick)?.DefaultBackgroundDisable ?? new Image();
        set
        {
            if (currentJoyStick != null)
            {
                if (currentJoyStick is CastingJoyStick castingJoyStick)
                    castingJoyStick.DefaultBackgroundDisable = value;
                else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                    buildingJoyStick.DefaultBackgroundDisable = value;
            }
        }
    }
    
    // 冷却UI配置现在通过GameData模板在构造函数中应用，不再支持动态修改

    /// <summary>
    /// 施法摇杆的冷却时施法提前量（毫秒）
    /// </summary>
    public TimeSpan CastingJoyStickCooldownThreshold
    {
        get => (currentJoyStick as CastingJoyStick)?.CooldownThreshold ?? TimeSpan.FromMilliseconds(67);
        set
        {
            if (currentJoyStick is CastingJoyStick castingJoyStick)
            {
                castingJoyStick.CooldownThreshold = value;
            }
        }
    }
    
    /// <summary>
    /// 绑定的快捷键
    /// </summary>
    public VirtualKey? BindKey
    {
        get => bindKeyUI?.BindKey;
        set{
            bindKeyUI.BindKey = value;
            bindKeyUI.Visible = value != null;
        }
    }
    
    /// <summary>
    /// 施法摇杆的大小（同时设置宽度和高度）
    /// </summary>
    public float CastingJoyStickSize
    {
        get => currentJoyStick?.Width ?? 0;
        set
        {
            if (currentJoyStick != null)
            {
                currentJoyStick.Width = value;
                currentJoyStick.Height = value;
            }
        }
    }
    
    /// <summary>
    /// 绑定的停止施法按钮
    /// </summary>
    public StopCastingButton? StopCastingButton
    {
        get => stopCastingButton;
        set
        {
            stopCastingButton = value;
            // 同步绑定到施法摇杆
            if (castingJoyStick != null)
            {
                castingJoyStick.StopCastingButton = stopCastingButton;
            }
        }
    }


    /// <summary>
    /// 使用默认模板创建技能摇杆实例
    /// </summary>
    public AbilityJoyStick() : this(DefaultTemplate)
    {
    }
    
    /// <summary>
    /// 使用指定模板创建技能摇杆实例
    /// </summary>
    /// <param name="link">技能摇杆控件模板链接</param>
    public AbilityJoyStick(IGameLink<GameDataControlAbilityJoyStick> link) : base(link)
    {
        // 使用GameData模板创建冷却UI实例
        var gameData = ((GameDataControlAbilityJoyStick)Cache);
        
        // 创建冷却UI实例，使用指定模板或默认模板
        cooldownUI = gameData.CoolDownUITemplate != null 
            ? new CoolDownUI(gameData.CoolDownUITemplate) 
            : new CoolDownUI();
            
        coolDownChargeUI = gameData.CoolDownChargeUITemplate != null 
            ? new CoolDownChargeUI(gameData.CoolDownChargeUITemplate) 
            : new CoolDownChargeUI();
            
        // 创建切换技能开启状态粒子效果
        toggleOnParticle = new GameUI.Control.Primitive.Particle() { 
            Visible = false,
            Resource = "effect/effect_new/effect_ui/eff_ui_biankuang_007/particle.effect"u8,
            Speed = 1,
            ParticleScale = new SizeF(0.7f, 0.7f),
            ParticleOffset = new PointF(0.5f, 0.5f),
            IsPlaying = true,
            
        };
        
        ConfigureUIComponents();
        SetupChildHierarchy();
        InitializeImages();
        SubscribeToEvents();
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    public void UpdateButtonState()
    {
        if (ability == null)
        {
            IsEnabled = false;
            return;
        }
        
        IsEnabled = ability.IsEnabled;
            
        abilityFramePanel.Visible = isActivated;
    }

    private void ConfigureUIComponents()
    {
        this.RoutedEvents = RoutedEvents.None;

        abilityIconPanel.WidthStretchRatio = 1f;
        abilityIconPanel.HeightStretchRatio = 1f;
        
        cooldownUI.WidthStretchRatio = 0.8f;
        cooldownUI.HeightStretchRatio = 0.8f;
        cooldownUI.WidthCompactRatio = 1f;
        cooldownUI.HeightCompactRatio = 1f;
        
        coolDownChargeUI.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        coolDownChargeUI.Width = 45;
        coolDownChargeUI.Height = 45;
        coolDownChargeUI.Position = new UIPosition(0, -17);
        
        abilityFramePanel.WidthStretchRatio = 1;
        abilityFramePanel.HeightStretchRatio = 1;
        
        abilityNameLabel.Width = -1;
        abilityNameLabel.Height = -1;
        abilityNameLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
        abilityNameLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        abilityNameLabel.TextTrimming = GameUI.Control.Enum.TextTrimming.Clip;
        abilityNameLabel.ShadowOffset = new System.Numerics.Vector2(2f, 2f);
        abilityNameLabel.ShadowColor = System.Drawing.Color.Black;
        abilityNameLabel.FontSize = 24;
        abilityNameLabel.Bold = true;
        
        passiveAbilityLabel.Width = -1;
        passiveAbilityLabel.Height = 32;
        passiveAbilityLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
        passiveAbilityLabel.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        passiveAbilityLabel.Position = new UIPosition(0, -10);
        passiveAbilityLabel.TextTrimming = GameUI.Control.Enum.TextTrimming.Clip;
        passiveAbilityLabel.ShadowOffset = new System.Numerics.Vector2(2f, 2f);
        passiveAbilityLabel.ShadowColor = System.Drawing.Color.Black;
        passiveAbilityLabel.FontSize = 24;
        passiveAbilityLabel.Bold = true;
        
        abilityDisabledIconPanel.WidthStretchRatio = 1;
        abilityDisabledIconPanel.HeightStretchRatio = 1;
        abilityDisabledIconPanel.WidthCompactRatio = 1;
        abilityDisabledIconPanel.HeightCompactRatio = 1;
        
        // 配置切换技能开启状态粒子效果
        toggleOnParticle.WidthStretchRatio = 1;
        toggleOnParticle.HeightStretchRatio = 1;
        
        // 配置绑定按键UI，位于右上方
        bindKeyUI.Width = 40;
        bindKeyUI.Height = 32;
        bindKeyUI.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        bindKeyUI.VerticalAlignment = GameUI.Enum.VerticalAlignment.Top;
        bindKeyUI.Position = new UIPosition(-8, 8);
        bindKeyUI.Visible = false;
    }

    private void SetupChildHierarchy()
    {
        // 摇杆会在SelectAppropriateJoyStick中动态添加
        
        this.AddChild(abilityIconPanel);
        this.AddChild(abilityFramePanel);
        this.AddChild(cooldownUI);
        this.AddChild(coolDownChargeUI);
        this.AddChild(abilityNameLabel);
        this.AddChild(passiveAbilityLabel);
        this.AddChild(abilityDisabledIconPanel);
        this.AddChild(toggleOnParticle);
        this.AddChild(bindKeyUI);
    }

    private void InitializeImages()
    {
        Image = AbilityBackgroundFrame.Path;
        abilityFramePanel.Image = AbilityActiveFrame.Path;
        abilityDisabledIconPanel.Image = AbilityDisabledFrame.Path;
    }

    private void SubscribeToEvents()
    {
        this.OnPointerPressed += OnButtonPressed;
        this.OnPointerReleased += OnButtonReleased;
        // 摇杆事件会在SelectAppropriateJoyStick中动态订阅
        bindKeyUI.KeyPressed += OnBindKeyPressed;
        bindKeyUI.KeyReleased += OnBindKeyReleased;
    }

    
    /// <summary>
    /// 根据技能类型选择合适的摇杆
    /// </summary>
    private void SelectAppropriateJoyStick()
    {
        if (ability == null || (ability is not AbilityActive))
        {
            // 清理所有摇杆
            if (castingJoyStick != null)
            {
                castingJoyStick.Visible = false;
                castingJoyStick.Ability = null;
            }
            if (buildingJoyStick != null)
            {
                buildingJoyStick.Visible = false;
                buildingJoyStick.AbilityExecute = null; // BuildingJoyStick仍然使用AbilityExecute
            }
            currentJoyStick = null;
            return;
        }

        bool isBuildingAbility = IsBuildingAbility(ability);
        
        if (isBuildingAbility)
        {
            // 建造技能：使用BuildingJoyStick
            if (buildingJoyStick == null)
            {
                buildingJoyStick = new BuildingJoyStick() { Visible = false };
                buildingJoyStick.AddToParent(this);
                // 设置为子控件模式，取消内部事件订阅
                buildingJoyStick.IsChildMode = true;
            }
            
            currentJoyStick = buildingJoyStick;
            buildingJoyStick.AbilityExecute = ability as AbilityExecute;
            
            // 隐藏施法摇杆
            if (castingJoyStick != null)
            {
                castingJoyStick.Visible = false;
                castingJoyStick.Ability = null;
            }
            
        }
        else
        {
            // 普通技能：使用CastingJoyStick
            if (castingJoyStick == null)
            {
                castingJoyStick = new CastingJoyStick() { Visible = false };
                castingJoyStick.AddToParent(this);
                // 设置为子控件模式，取消内部事件订阅
                castingJoyStick.IsChildMode = true;
                // 绑定停止施法按钮
                castingJoyStick.StopCastingButton = stopCastingButton;
            }
            
            currentJoyStick = castingJoyStick;
            castingJoyStick.Ability = ability;
            
            // 隐藏建造摇杆
            if (buildingJoyStick != null)
            {
                buildingJoyStick.Visible = false;
                buildingJoyStick.AbilityExecute = null; // BuildingJoyStick仍然使用AbilityExecute
            }
            
        }
    }

    /// <summary>
    /// 判断是否为建造技能 - 通过类型判断而非名字
    /// </summary>
    private bool IsBuildingAbility(Ability? ability)
    {
        if (ability?.Cache == null) return false;
        
        // 通过类名约定判断：检查类型名是否包含"BuildingAbility"或"AbilityExecuteBuilding"
        var typeName = ability.Cache.GetType().Name;
        var fullTypeName = ability.Cache.GetType().FullName;
        bool isBuilding = typeName.Contains("BuildingAbility") || typeName.Contains("AbilityExecuteBuilding");
        
        var displayInfo = ability as IDisplayInfo;
        var displayName = ((IDisplayInfo)ability).DisplayName ?? ability.Cache.Name ?? "";
            
        return isBuilding;
    }

    /// <summary>
    /// 处理技能附加对象状态变化事件
    /// </summary>
    /// <param name="state">新的状态</param>
    private void OnAttachedObjectStateChanged(AttachedObjectState state)
    {
        switch (state)
        {
            case AttachedObjectState.Disabled:
                // 更新IsEnabled状态
                IsEnabled = ability?.IsEnabled ?? true;
                break;
    
            case AttachedObjectState.Deactivated:
                // 技能被停用时（如切换技能关闭），更新Toggle特效显示状态
                if (ability is AbilityToggle toggle)
                {
                    toggleOnParticle.Visible = toggle.ToggledOn;
                }
                break;
        }
    }

    private void UpdateAbilityDisplay()
    {
        if (ability?.Cache != null)
        {
            if (ability.Cache.Icon.HasValue)
            {
                
                abilityIconPanel.Image = ability.Cache.Icon.Value.Path;
                abilityIconPanel.Scale = new Vector2(0.82f, 0.82f);
                Game.NextTick().ContinueWith(async (t) =>
                {
                    if(abilityIconPanel != null && abilityIconPanel.IsValid){
                        abilityIconPanel.CornerRadius = abilityIconPanel.ActualSize.Width;
                    }
                    await Task.CompletedTask;
                });
            }
            // 优先使用DisplayName，提供完整的回退机制
            var displayName = ((IDisplayInfo)ability).DisplayName ?? ability.Cache.Name ?? "";
            abilityNameLabel.Text = displayName;
        }
        else
        {
            abilityIconPanel.Image = "";
            abilityNameLabel.Text = "";
        }

        
        
        // 只有当技能有充能时才显示充能UI
        var abilityActive = ability as AbilityActive;
        coolDownChargeUI.Visible = abilityActive?.Charge != null;

        // 被动技能标签显示
        passiveAbilityLabel.Visible = IsPassiveAbility;
        
    }
    
    private void OnButtonPressed(object? sender, PointerEventArgs e)
    {
        if (!isEnabled || isActivated || ability == null || IsPassiveAbility) return;

        isActivated = true;
        
        var abilityName = ability?.Cache?.Name ?? "未知技能";
        var joystickType = currentJoyStick?.GetType().Name ?? "Unknown";
        
        if (currentJoyStick != null)
        {
            currentJoyStick.Visible = true;
            if (currentJoyStick is CastingJoyStick castingJoyStick)
                castingJoyStick.SetPressed(e.PointerButtons);
            else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                buildingJoyStick.SetPressed(e.PointerButtons);
        }
        
        abilityFramePanel.Visible = isActivated;
    }
    
    private void OnReleased(object? sender, JoystickReleasedEventArgs e)
    {
        isActivated = false;
        this.ReleasePointer(e.PointerButtons);
        if (currentJoyStick != null)
        {
            if (currentJoyStick is CastingJoyStick castingJoyStick)
                castingJoyStick.SetReleased();
            else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                buildingJoyStick.SetReleased();
            currentJoyStick.Visible = false;
        }
        abilityFramePanel.Visible = isActivated;
    }
    
    private void OnButtonReleased(object? sender, PointerEventArgs e)
    {
        OnReleased(this, new JoystickReleasedEventArgs(e.PointerButtons));
    }
    
    
    /// <summary>
    /// 处理绑定按键按下事件，触发施法模式
    /// </summary>
    private void OnBindKeyPressed(object? sender, VirtualKey key)
    {
        if (!isEnabled || isActivated || ability == null || IsPassiveAbility) return;

        // 键盘模式：激活状态和功能，但不显示摇杆UI
        isActivated = true;
        
        var abilityName = ability?.Cache?.Name ?? "未知技能";
        var joystickType = currentJoyStick?.GetType().Name ?? "Unknown";
        
        if (currentJoyStick != null)
        {
            currentJoyStick.Visible = false; // 键盘模式下不显示摇杆UI
            if (currentJoyStick is CastingJoyStick castingJoyStick)
                castingJoyStick.SetKeyboardPressed(); // 使用专门的键盘激活方法
            else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                buildingJoyStick.SetKeyboardPressed(); // 建造摇杆也支持键盘模式
        }
        
        abilityFramePanel.Visible = isActivated;
    }
    
    /// <summary>
    /// 处理绑定按键松开事件，触发释放逻辑
    /// </summary>
    private void OnBindKeyReleased(object? sender, VirtualKey key)
    {
        if (!isActivated) return;

        // 键盘模式：触发释放逻辑
        isActivated = false;
        if (currentJoyStick != null)
        {
            if (currentJoyStick is CastingJoyStick castingJoyStick)
                castingJoyStick.SetReleased();
            else if (currentJoyStick is BuildingJoyStick buildingJoyStick)
                buildingJoyStick.SetReleased();
            currentJoyStick.Visible = false;
        }
        abilityFramePanel.Visible = isActivated;
    }


        
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        
        this.OnPointerPressed -= OnButtonPressed;
        this.OnPointerReleased -= OnButtonReleased;
        bindKeyUI.KeyPressed -= OnBindKeyPressed;
        bindKeyUI.KeyReleased -= OnBindKeyReleased;
        
        // 清理技能状态变化事件订阅
        if (ability != null)
        {
            ability.OnAttachedObjectStateChanged -= OnAttachedObjectStateChanged;
        }
        
        castingJoyStick?.Destroy();
        buildingJoyStick?.Destroy();
        bindKeyUI?.Destroy();
        cooldownUI?.Destroy();
        coolDownChargeUI?.Destroy();
    }
}

#endif
