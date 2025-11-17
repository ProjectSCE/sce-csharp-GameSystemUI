#if CLIENT
using EngineInterface.BaseType;
using GameCore.Container;
using GameCore.Data;
using GameData;
using GameSystemUI.BuffSystemUI.Data;
using GameUI.Control.Data;

namespace GameSystemUI.ChatSystem;

public class ScopeData : IGameClass
{
    public static class Control
    {
        public static readonly GameLink<GameDataControl, GameDataPlayerItem> DefaultPlayerItem = new("DefaultPlayerItem");
    }

    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }

    private static void OnGameDataInitialization()
    {
        _ = new GameDataPlayerItem(Control.DefaultPlayerItem)
        {
        };
    }
}
#endif

