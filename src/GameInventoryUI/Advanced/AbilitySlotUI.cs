#if CLIENT
using GameCore.AbilitySystem;
using GameCore.Container;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameData;
using GameData.Interface;
using GameData.Extension;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using GameUI.Control.Struct;
using GameUI.Struct;
using GameUI.Device;
using Microsoft.Extensions.Logging;

using GameSystemUI.GameInventoryUI.Data;
using GameSystemUI.AbilitySystemUI.Advanced;
using GameCore.DisplayInfo;
using System.Threading.Tasks;
using GameCore.ResourceType;

namespace GameSystemUI.GameInventoryUI.Advanced;

/// <summary>
/// 技能物品格子UI类 - 集成了施法摇杆功能的物品格子
/// 当QuickBar锁定时，如果格子内有物品且物品有ActiveAbility，则显示技能摇杆
/// 当QuickBar解锁时，恢复为普通物品格子
/// </summary>
[GameObject<GameDataControlAbilitySlotUI>]
public partial class AbilitySlotUI : InventorySlotUI, IGameObject<GameDataControlAbilitySlotUI>, IGameObject
{
    private AbilityJoyStick abilityJoyStick = null!; // 技能摇杆
    private bool isAbilityMode = false;
    private Ability? currentAbility;

    /// <summary>
    /// 是否处于技能模式
    /// </summary>
    public bool IsAbilityMode
    {
        get => isAbilityMode;
        private set
        {
            if (isAbilityMode == value) return;
            isAbilityMode = value;
            UpdateModeDisplay();
        }
    }

    /// <summary>
    /// 当前绑定的技能（支持所有Ability类型）
    /// </summary>
    public Ability? CurrentAbility
    {
        get => currentAbility;
        set
        {
            if (currentAbility == value) return;
            currentAbility = value;
            
            // 将技能绑定到AbilityJoyStick
            if (abilityJoyStick != null)
            {
                abilityJoyStick.Ability = value;
            }
            UpdateAbilityMode();
        }
    }

    /// <summary>
    /// 兼容性属性：当前绑定的技能执行器（向后兼容）
    /// </summary>
    public AbilityExecute? CurrentAbilityExecute
    {
        get => currentAbility as AbilityExecute;
        set => CurrentAbility = value;
    }

    public static new readonly IGameLink<GameDataControlAbilitySlotUI> DefaultTemplate = new GameLink<GameDataControl, GameDataControlAbilitySlotUI>(typeof(AbilitySlotUI).GetHashCode(true));

    public AbilitySlotUI(IGameLink<GameDataControlAbilitySlotUI> link) : base(link)
    {
        InitializeAbilityComponents();
    }

    public AbilitySlotUI() : this(DefaultTemplate)
    {
    }
    
    /// <summary>
    /// 初始化技能相关组件
    /// </summary>
    private void InitializeAbilityComponents()
    {
        // 从GameData获取AbilityJoyStick模板配置
        
        // 创建AbilityJoyStick实例，使用指定模板或默认模板
        abilityJoyStick = Cache.AbilityJoyStickTemplate != null 
            ? new AbilityJoyStick(Cache.AbilityJoyStickTemplate)
            : new AbilityJoyStick();
            
        abilityJoyStick.Width = this.SlotSize;
        abilityJoyStick.Height = this.SlotSize;
        
        // 让层数显示在摇杆上方
        abilityJoyStick.Visible = false;
        abilityJoyStick.AddToParent(this);
        
        // AbilityJoyStick会直接处理用户交互，不需要转发事件
    }



    /// <summary>
    /// 重写UpdateUI方法，在更新物品显示的同时更新技能绑定
    /// </summary>
    public override void UpdateUI()
    {
        base.UpdateUI();
        UpdateAbilityBinding();
    }

    /// <summary>
    /// 更新技能绑定，另外由于可能存在的同步延迟，需要在父控件监听技能变化手动绑定
    /// </summary>
    private void UpdateAbilityBinding()
    {
        CurrentAbility = null;
        var itemmod = Slot?.Item as ItemMod;
        if (itemmod?.ActiveAbility != null)
        {
            // 直接使用物品的技能，支持所有Ability类型
            CurrentAbility = itemmod.ActiveAbility;
        }
    }



    /// <summary>
    /// 更新技能模式
    /// </summary>
    private void UpdateAbilityMode()
    {
        // 只有在锁定状态下且有可用技能时才启用技能模式
        bool shouldEnableAbilityMode = IsLocked && CurrentAbility != null;
        IsAbilityMode = shouldEnableAbilityMode;
    }

    /// <summary>
    /// 更新模式显示
    /// </summary>
    private void UpdateModeDisplay()
    {
        if (IsAbilityMode)
        {
            // 进入技能模式
            // 1. 显示AbilityJoyStick，让用户可以直接与之交互
            if (abilityJoyStick != null)
            {
                abilityJoyStick.Visible = true;
            }
        }
        else
        {
            // 退出技能模式
            // 1. 隐藏AbilityJoyStick
            if (abilityJoyStick != null)
            {
                abilityJoyStick.Visible = false;
            }
        }
    }

    /// <summary>
    /// 设置键盘绑定（用于键盘控制）
    /// </summary>
    public VirtualKey? BindKey
    {
        get => abilityJoyStick?.BindKey;
        set
        {
            if (abilityJoyStick != null)
            {
                abilityJoyStick.BindKey = value;
            }
        }
    }


    /// <summary>
    /// 检查当前格子是否有可用技能
    /// </summary>
    public bool HasActiveAbility()
    {
        return CurrentAbility != null;
    }

    protected override void DisposeManaged()
    {
        if (abilityJoyStick != null)
        {
            abilityJoyStick.Destroy();
        }
        
        CurrentAbility = null;
        base.DisposeManaged();
    }
}

#endif
