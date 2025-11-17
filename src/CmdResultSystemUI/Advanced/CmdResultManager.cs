#if CLIENT
using Events;
using GameCore.Event;
using GameSystemUI.CmdResultSystemUI.Advanced;
namespace GameSystemUI.CmdResultSystemUI;

public class CmdResultManager: IGameClass
{
    public static CmdResultPanel CmdResultPanel = null!; //不会真在GameStart前访问吧
    public static void ShowCmdResult(CmdResult cmdResult)
    {
        CmdResultPanel.CmdResult = cmdResult;
    }
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameDataInitialization;
    }

    private static void OnGameDataInitialization()
    {
        Trigger<EventGameStart> trigger1 = new(async (s, d) =>
        {
            CmdResultPanel = new CmdResultPanel();
            _ = CmdResultPanel.AddToVisualTree();
            await Task.CompletedTask;
            return true;
        });
        trigger1.Register(Game.Instance);
    }
}
#endif

