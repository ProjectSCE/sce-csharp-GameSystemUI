#if CLIENT
using EngineInterface.BaseType;

using GameCore.AbilitySystem.Data;
using GameCore.AbilitySystem.Data.Enum;
using GameCore.ActorSystem.Data;
using GameCore.AISystem.Data;

using GameUI.CameraSystem.Data;

using GameCore.Container;
using GameCore.Container.Data;
using GameCore.CooldownSystem.Data;
using GameCore.Data;
using GameCore.EntitySystem.Data.Enum;
using GameCore.Execution.Data;
using GameCore.GameSystem.Data;
using GameCore.PlayerAndUsers.Enum;
using GameCore.ResourceType.Data;
using GameCore.ResourceType.Data.Enum;
using GameCore.SceneSystem.Data;
using GameCore.SceneSystem.Data.Struct;

using GameData;

using GameCore.TargetingSystem.Data;
using GameCore.ActorSystem.Data.Enum;
using GameCore.Animation.Enum;
using GameCore.ResourceType;
using GameUI.Brush;
using GameUI.Control.Data;
using GameUI.Control.Enum;
using GameUI.Control.Primitive;
using GameUI.Enum;

using System.Drawing;


using static GameCore.ScopeData;
using GameCore.Behavior;
using System.Diagnostics;
using GameCore.OrderSystem;
using GameCore.Struct;
using GameCore.Platform.SDL;
using GameSystemUI.AbilitySystemUI.Data;
using GameSystemUI.AbilitySystemUI.Advanced;
using Events;
using GameCore.Event;
using GameUI.Control.Data.Struct;
using GameUI.Struct;

namespace GameSystemUI.AbilitySystemUI;
public class ScopeData : IGameClass
{

    public static class Control
    {
        public static bool EnableDefaultAbilityJoyStick = false;
        public static readonly GameLink<GameDataControl, GameDataControlAbilityJoyStick> DefaultAbilityJoyStick = new("DefaultAbilityJoyStick");
        public static readonly GameLink<GameDataBindKeyConfig, GameDataBindKeyConfig> DefaultBindKeyConfig = new("DefaultBindKeyConfig");
        public static readonly GameLink<GameDataControl, GameDataControlCoolDownUI> DefaultCoolDownUI = new("DefaultCoolDownUI");
        public static readonly GameLink<GameDataControl, GameDataControlCoolDownChargeUI> DefaultCoolDownChargeUI = new("DefaultCoolDownChargeUI");
        public static readonly GameLink<GameDataControl, GameDataStopCastingButton> DefaultStopCastingButton = new("DefaultStopCastingButton");
    }
    public static class Actor
    {
        public static readonly GameLink<GameDataActor, GameDataActorHighlight> AbilityJoyStickTargetingHighlight = new("AbilityJoyStickTargetingHighlight"u8);
    }
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
        // 等出UI编辑器了删掉
        if(Control.EnableDefaultAbilityJoyStick)
        {
            Game.OnGameTriggerInitialization += Game_OnGameTriggerInitialization;
        }
    }

    private static void Game_OnGameTriggerInitialization()
    {
        Trigger<EventGameStart> trigger1 = new(async (s, d) =>
        {
            AbilityJoyStickGroup? abilityJoyStickGroup = null;
            Trigger<EventPlayerMainUnitChanged> trigger1 = new(async (s, d) =>
            {
                var mainUnit = Player.LocalPlayer!.MainUnit;
                if (d.Unit != mainUnit)
                {
                    return false;
                }
                if (abilityJoyStickGroup == null)
                {
                    abilityJoyStickGroup = new()
                    {
                        WidthStretchRatio = 0.5f,
                        HeightStretchRatio = 0.5f,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                    };
                    _ = abilityJoyStickGroup.AddToVisualTree();
                }
                abilityJoyStickGroup.BindUnit = mainUnit;
                await Task.CompletedTask;
                return true;
            });
            var player = Player.LocalPlayer ?? throw new InvalidOperationException("Failed to get LocalPlayer");
            trigger1.Register(player);
            await Task.CompletedTask;
            return true;
        });
        trigger1.Register(Game.Instance);
    }

    private static void OnGameDataInitialization()
    {
        _ = new GameDataControlAbilityJoyStick(Control.DefaultAbilityJoyStick)
        {
        };
        _ = new GameDataControlCoolDownUI(Control.DefaultCoolDownUI)
        {
            // 冷却UI配置
            CooldownOverlay = new GameCore.ResourceType.Image("@gameui/image/control/冷却.png"),
            CoolTextFontSize = 42,
            CoolTextColor = "#FFFFFF"
        };
        _ = new GameDataControlCoolDownChargeUI(Control.DefaultCoolDownChargeUI)
        {
            // 充能冷却UI配置
            BackgroundImage = new GameCore.ResourceType.Image("@gameui/image/control/冷却.png"),
            ChargeProgressImage = new GameCore.ResourceType.Image("@gameui/image/control/充能技能冷却条.png"),
            StackLabelFontSize = 30,
            StackLabelColor = "#FFFFFF"
        };
        _ = new GameDataBindKeyConfig(Control.DefaultBindKeyConfig)
        {
            DefaultKeyBindingOrder = [VirtualKey.Number1, VirtualKey.Number2, VirtualKey.Number3, VirtualKey.Number4, VirtualKey.Number5],
        };
        _ = new GameDataStopCastingButton(Control.DefaultStopCastingButton)
        {
            // 停止施法按钮配置
            Layout = new Layout()
            {
                Position = new UIPosition(-100, 100),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            },
            ButtonImage = new GameCore.ResourceType.Image("@gameui/image/取消施法区域.png"),
            MaskImage = new GameCore.ResourceType.Image("@gameui/image/禁止施法.png"),
        };
        _ = new GameDataActorHighlight(Actor.AbilityJoyStickTargetingHighlight)
        {
            From = new()
            {
                Value = new(255, 192, 192)
            },
            To = new()
            {
                Value = new(255, 128, 128)
            },
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            Duration = TimeSpan.FromSeconds(0.15),
        };
    }
}
#endif