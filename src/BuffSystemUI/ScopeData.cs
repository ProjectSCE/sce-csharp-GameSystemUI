#if CLIENT
using EngineInterface.BaseType;
using GameCore.Container;
using GameCore.Data;
using GameData;
using GameSystemUI.BuffSystemUI.Data;
using GameUI.Control.Data;

namespace GameSystemUI.BuffSystemUI;

public class ScopeData : IGameClass
{
    public static class Control
    {
        public static readonly GameLink<GameDataControl, GameDataControlBuffIcon> DefaultBuffIcon = new("DefaultBuffIcon");
        public static readonly GameLink<GameDataControl, GameDataControlBuffBar> DefaultBuffBar = new("DefaultBuffBar");
    }

    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }

    private static void OnGameDataInitialization()
    {
        Game.Logger.LogDebug("初始化Buff系统UI数据");
        
        _ = new GameDataControlBuffIcon(Control.DefaultBuffIcon)
        {
            BuffIcon = "@gameui/image/buff/buff_1.png",
            BuffWidth = 64,
            BuffHeight = 64,
            BuffMargin = 7,
            FontSize = 24,
            BuffPositiveProgressType = ProgressType.Clockwise,
            BuffNegativeProgressType = ProgressType.Clockwise, 
            BuffNeutralProgressType = ProgressType.Clockwise
        };

        _ = new GameDataControlBuffBar(Control.DefaultBuffBar)
        {
            BuffWidth = 64,
            BuffHeight = 64,
            BuffMargin = 7,
            BuffCategoryFilter = BuffCategoryFilter.All,
            BuffPolarityFilter = BuffPolarityFilter.All
        };
    }
}
#endif

