#if CLIENT
using Events;
using GameCore.AbilitySystem;
using GameCore.AbilitySystem.Manager;
using GameCore.BaseType;
using GameCore.EntitySystem;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameCore.SceneSystem;
using GameUI.Control.Primitive;
using GameData;
using GameUI.Control.Data;
using GameData.Extension;
using GameSystemUI.AbilitySystemUI.Data;
using GameCore.GameSystem.Enum;

namespace GameSystemUI.AbilitySystemUI.Advanced;

[GameObject<GameDataControlAbilityJoyStickGroup>]
public partial class AbilityJoyStickGroup : Panel
{
    public static new readonly IGameLink<GameDataControlAbilityJoyStickGroup> DefaultTemplate = new GameLink<GameDataControl, GameDataControlAbilityJoyStickGroup>(typeof(AbilityJoyStickGroup).GetHashCode(true));

    private Unit? bindUnit;
    private List<AbilityJoyStick> abilityJoySticks = new List<AbilityJoyStick>();
    private AbilityManager? currentAbilityManager;
    private List<Ability> subscribedAbilities = new List<Ability>();
    private StopCastingButton? stopCastingButton = null;

    /// <summary>
    /// 绑定的单位
    /// </summary>
    public Unit? BindUnit
    {
        get => bindUnit;
        set
        {
            if(bindUnit == value)
                return;
            
            // 取消之前的事件监听
            UnsubscribeAbilityEvents();
            
            bindUnit = value;
            
            // 订阅新的事件监听
            SubscribeAbilityEvents();
            
            _ = RefreshabilityJoySticks();
        }
    }
    
    /// <summary>
    /// 最大显示技能数量
    /// </summary>
    internal int MaxSkillCount { get; set; } = 8;
    
    /// <summary>
    /// 是否自动绑定快捷键到摇杆按钮
    /// </summary>
    internal bool AutoBindKey { get; set; } = true;
    
    /// <summary>
    /// 普通技能摇杆按钮的大小（像素）
    /// </summary>
    internal float ButtonSize { get; set; } = 150f;
    
    /// <summary>
    /// 攻击技能摇杆按钮的大小（像素）
    /// </summary>
    internal float AttackButtonSize { get; set; } = 250f;
    
    /// <summary>
    /// 环绕技能摇杆距离基准位置的最小距离（像素）
    /// </summary>
    internal float MinAroundDistance { get; set; } = 350f;
    
    /// <summary>
    /// 环绕技能摇杆分布的总角度范围（度）
    /// </summary>
    internal float TotalAngleDelta { get; set; } = 130f;
    
    /// <summary>
    /// 环绕技能摇杆分布的初始角度（度）
    /// </summary>
    internal float InitAngle { get; set; } = -18f;
    
    /// <summary>
    /// 环绕技能摇杆的基准位置X坐标（像素）
    /// </summary>
    internal float BaseX { get; set; } = -150f;
    
    /// <summary>
    /// 环绕技能摇杆的基准位置Y坐标（像素）
    /// </summary>
    internal float BaseY { get; set; } = -120f;
    
    /// <summary>
    /// 攻击技能摇杆的X坐标位置（像素）
    /// </summary>
    internal float AttackX { get; set; } = -150f;
    
    /// <summary>
    /// 攻击技能摇杆的Y坐标位置（像素）
    /// </summary>
    internal float AttackY { get; set; } = -120f;
    
    /// <summary>
    /// 非自动生成的子控件是否参与自动布局
    /// </summary>
    internal bool AutoLayoutManualChildren { get; set; } = true;

    /// <summary>
    /// 摇杆模板
    /// </summary>
    internal IGameLink<GameDataControlAbilityJoyStick> JoyStickTemplate { get; set; } = ScopeData.Control.DefaultAbilityJoyStick;
    
    /// <summary>
    /// 停止施法按钮模板
    /// </summary>
    internal IGameLink<GameDataStopCastingButton> StopCastingButtonTemplate { get; set; } = ScopeData.Control.DefaultStopCastingButton;
    
    /// <summary>
    /// 是否自动创建并绑定停止施法按钮
    /// </summary>
    internal bool AutoCreateStopCastingButton { get; set; } = true;
    
    public AbilityJoyStickGroup() : this(DefaultTemplate)
    {
    }

    public AbilityJoyStickGroup(IGameLink<GameDataControlAbilityJoyStickGroup> link):base(link)
    {
        ItemTemplate = JoyStickTemplate;
        if(link.Data?.ZIndex != null)
        {
            this.ZIndex = link.Data.ZIndex.Value;
        }
        else
        {
            this.ZIndex = StandardUIType.Joystick.ExpectedZIndex ?? 0;
        }
        
        // 自动创建停止施法按钮
        if (AutoCreateStopCastingButton)
        {
            CreateStopCastingButton();
        }
    }
    
    /// <summary>
    /// 创建停止施法按钮
    /// </summary>
    private void CreateStopCastingButton()
    {
        if (stopCastingButton == null)
        {
            // 使用模板创建或默认创建
            if (StopCastingButtonTemplate?.Data != null)
            {
                stopCastingButton = StopCastingButtonTemplate.Data.CreateStopCastingButton();
            }
            else
            {
                stopCastingButton = new StopCastingButton()
                {
                    HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
                    VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
                    Position = new GameUI.Struct.UIPosition(-100, 100),
                };
            }
            stopCastingButton.AddToVisualTree();
        }
    }

    /// <summary>
    /// 刷新摇杆组，并将绑定单位的技能同步到摇杆上
    /// </summary>
    public async Task RefreshabilityJoySticks()
    {   
        try{
        if (bindUnit == null)
        {
            foreach (var joyStick in abilityJoySticks)
            {
                joyStick.Ability = null;
                joyStick.Visible = false;
            }
            return;
        }

        var abilityManager = bindUnit.GetComponent<AbilityManager>();
        if (abilityManager == null)
        {
            Game.Logger?.LogWarning("绑定单位 {unit} 没有 AbilityManager 组件", bindUnit);
            return;
        }

        var allAbilities = abilityManager.GetAll();
        var unitAbilities = allAbilities.Where(x => x is AbilityActive && !x.IsGrantedByItem).ToList();

        var skillCount = Math.Min(unitAbilities.Count, MaxSkillCount);
        
        UpdateChildrenAbilityJoySticks();

        var manualCount = abilityJoySticks.Count;
        

        if (skillCount > abilityJoySticks.Count)
        {
            var list = new List<object>();
            for (int i = abilityJoySticks.Count; i < skillCount; i++)
            {
                list.Add(unitAbilities[i]);
            }
            ItemsSource = list;
            this.GenerateChildren();
        }

        await Game.NextTick();

        if(GeneratedChildren != null)
        {
            foreach (var child in GeneratedChildren)
            {
                var joyStick = child as AbilityJoyStick;
                if(joyStick != null)
                {
                    abilityJoySticks.Add(joyStick);
                }
            }
        }

        for (int i = 0; i < skillCount; i++)
        {
            abilityJoySticks[i].Ability = unitAbilities[i];
            abilityJoySticks[i].Visible = true;
            // 自动绑定停止施法按钮
            if (stopCastingButton != null)
            {
                abilityJoySticks[i].StopCastingButton = stopCastingButton;
            }
        }
        
        // 自动绑定快捷键(仅PC)
        if (AutoBindKey && Utility.IsPC())
        {
            ApplyAutoKeyBinding(skillCount);
        }

        for(int i = skillCount; i < abilityJoySticks.Count; i++)
        {
            abilityJoySticks[i].Ability = null;
            abilityJoySticks[i].Visible = false;
        }

        if (AutoLayoutManualChildren)
        {
            ApplyLayout(0, abilityJoySticks.Count);
        }
        else
        {
            ApplyLayout(manualCount, abilityJoySticks.Count);
        }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("刷新摇杆组时出错: {ex}", ex);
        }
    }

    /// <summary>
    /// 应用布局到指定范围的摇杆
    /// </summary>
    /// <param name="startIndex">开始索引（包含）</param>
    /// <param name="endIndex">结束索引（不包含）</param>
    private void ApplyLayout(int startIndex, int endIndex)
    {
        var layoutCount = endIndex - startIndex;
        if (layoutCount <= 0) return;

        var aroundNum = layoutCount - 1;
        var angleDelta = aroundNum > 0 ? TotalAngleDelta / aroundNum : 0f;
        var aroundDistance = MinAroundDistance + Math.Max(aroundNum - 4, 0) * ButtonSize / 3.2f;

        for (int i = 0; i < layoutCount; i++)
        {
            var joyStick = abilityJoySticks[startIndex + i];

            if (i == 0)
            {
                SetJoyStickLayout(joyStick, AttackX, AttackY, AttackButtonSize);
            }
            else
            {
                var angleDegrees = InitAngle + (i + 1 - 1.5f) * angleDelta;
                var angleRadians = angleDegrees * (float)(Math.PI / 180.0);
                var x = BaseX - aroundDistance * (float)Math.Cos(angleRadians);
                var y = BaseY - aroundDistance * (float)Math.Sin(angleRadians);
                    
                SetJoyStickLayout(joyStick, x, y, ButtonSize);
            }
        }
    }

    /// <summary>
    /// 设置摇杆的位置和大小
    /// </summary>
    private void SetJoyStickLayout(AbilityJoyStick joyStick, float x, float y, float size)
    {
        joyStick.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        joyStick.VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom;
        joyStick.Position = new GameUI.Struct.UIPosition(x, y);
        joyStick.Width = size;
        joyStick.Height = size;
        
        // 设置施法摇杆大小为1.5倍
        joyStick.CastingJoyStickSize = size * 1.8f;
    }

    /// <summary>
    /// 返回非生成子控件中类型为AbilityJoyStick的控件
    /// </summary>
    private List<AbilityJoyStick> FindAbilityJoySticks()
    {
        var foundJoySticks = new List<AbilityJoyStick>();
        if(Children == null)
            return foundJoySticks;

        // 销毁生成的子控件
        if(GeneratedChildren != null)
        {
            // 先复制一份，避免在销毁过程中修改原集合
            var generatedChildrenCopy = GeneratedChildren.ToList();
            foreach (var child in generatedChildrenCopy)
            {
                child.RemoveFromParent();
                child.Destroy();
            }
        }

        // 销毁生成的控件后，Children集合已经稳定，可以安全遍历
        foreach (var child in Children)
        {
            if (child is AbilityJoyStick joyStick)
            {
                foundJoySticks.Add(joyStick);
            }
        }
        return foundJoySticks;
    }

    /// <summary>
    /// 更新内部的摇杆列表
    /// </summary>
    private void UpdateChildrenAbilityJoySticks()
    {
        abilityJoySticks.Clear();
        abilityJoySticks.AddRange(FindAbilityJoySticks());
    }

    /// <summary>
    /// 应用自动按键绑定
    /// </summary>
    /// <param name="skillCount">要绑定的技能数量</param>
    private void ApplyAutoKeyBinding(int skillCount)
    {
        try
        {
            // 从配置中获取默认按键绑定顺序
            var defaultKeys = ScopeData.Control.DefaultBindKeyConfig.Data?.DefaultKeyBindingOrder;
            
            if (defaultKeys == null || defaultKeys.Length == 0)
            {
                // 使用默认按键序列
                defaultKeys = new VirtualKey[] 
                { 
                    VirtualKey.Q, VirtualKey.W, VirtualKey.E, VirtualKey.R,
                    VirtualKey.T, VirtualKey.Y, VirtualKey.U, VirtualKey.I 
                };
            }
            
            int keyIndex = 0;
            for (int i = 0; i < skillCount && keyIndex < defaultKeys.Length; i++)
            {
                if (i < abilityJoySticks.Count)
                {
                    // 跳过被动技能，不为其分配按键
                    if (abilityJoySticks[i].IsPassiveAbility)
                    {
                        continue;
                    }
                    
                    abilityJoySticks[i].BindKey = defaultKeys[keyIndex];
                    keyIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("应用自动按键绑定时出错: {ex}", ex.Message);
        }
    }
    
    /// <summary>
    /// 订阅技能事件监听
    /// </summary>
    private void SubscribeAbilityEvents()
    {
        if (bindUnit == null)
            return;
            
        currentAbilityManager = bindUnit.GetComponent<AbilityManager>();
        if (currentAbilityManager == null)
            return;
            
        // 监听技能添加事件
        currentAbilityManager.OnObjectAttached += OnAbilityAttached;
    }
    
    /// <summary>
    /// 取消订阅技能事件监听
    /// </summary>
    private void UnsubscribeAbilityEvents()
    {
        if (currentAbilityManager != null)
        {
            currentAbilityManager.OnObjectAttached -= OnAbilityAttached;
        }
        
        // 清理所有已订阅的技能OnRemoved事件
        foreach (var ability in subscribedAbilities)
        {
            if (ability != null)
            {
                ability.OnRemoved -= OnAbilityRemoved;
            }
        }
        var count = subscribedAbilities.Count;
        subscribedAbilities.Clear();
        
        currentAbilityManager = null;
    }
    
    /// <summary>
    /// 技能添加时的回调
    /// </summary>
    private void OnAbilityAttached(Ability ability)
    {
        if(ability.IsGrantedByItem)
        {
            return;
        }
            
        // 监听技能移除事件
        ability.OnRemoved += OnAbilityRemoved;
        subscribedAbilities.Add(ability);
        
        // 刷新摇杆组
        _ = RefreshabilityJoySticks();
    }
    
    /// <summary>
    /// 技能移除时的回调
    /// </summary>
    private void OnAbilityRemoved()
    {
        // 刷新摇杆组
        _ = RefreshabilityJoySticks();
    }
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        // 取消事件订阅
        UnsubscribeAbilityEvents();
        
        // 清理停止施法按钮
        if (stopCastingButton != null)
        {
            stopCastingButton.RemoveFromParent();
            stopCastingButton.Destroy();
            stopCastingButton = null;
        }
        
        base.DisposeManaged();
        
        // 清理摇杆列表
        abilityJoySticks?.Clear();
    }
}

#endif