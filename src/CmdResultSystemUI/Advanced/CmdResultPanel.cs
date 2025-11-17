#if CLIENT
using Events;
using GameCore.Event;
using GameCore.OrderSystem;
using GameCore.Platform.SDL;
using GameUI.Control.Primitive;
using GameUI.Struct;
using GameCore.ResourceType;
using System.Numerics;
using GameUI.TriggerEvent;
using GameCore.AbilitySystem.Manager;
using GameCore.AbilitySystem;
using TriggerEncapsulation;
using GameData.Extension;
using GameData;
using GameCore.GameSystem.Enum;
using GameUI.Control.Data;
using System.Drawing;
namespace GameSystemUI.CmdResultSystemUI.Advanced;

public class CmdResultPanel : Panel
{
    private readonly Label cmdResultLabel;
    private readonly GameCore.Timers.Timer autoHideDelay;
    private readonly Trigger<EventGameCmdResultNotify> cmdResultNotifyTrigger;
    public int Time { get; set; } = 3000; // 毫秒(ms)
    private CmdResult? _cmdResult;
    public CmdResult? CmdResult
    {
        get => _cmdResult;
        set 
        {
            _cmdResult = value;
            if (value == null)
            {
                this.Visible = false;
                return;
            }
            this.Visible = true;
            autoHideDelay.Start();
            cmdResultLabel.Text = value.Value.LocalizedString;
        }
    }
    public CmdResultPanel()
    {
        this.Height = -1;
        this.Width = -1;
        this.Position = new UIPosition(0, 400);
        this.ZIndex = StandardUIType.Notifications.ExpectedZIndex ?? 1000;
        cmdResultLabel = new Label()
        {
            Height = -1,
            Width = -1,
            FontSize = 30,
            // Bold = true,
            StrokeColor = Color.Black,
            ShadowOffset = new Vector2(1, 1),
            StrokeSize = 1,
            TextColor = Color.White,
        };
        this.AddChild(cmdResultLabel);
        autoHideDelay = new GameCore.Timers.Timer(Time)
        {
            Enabled = false,
        };
        autoHideDelay.Elapsed += (s,e) => {
            this.Visible = false;
            autoHideDelay.Stop();
        };
        cmdResultNotifyTrigger = new Trigger<EventGameCmdResultNotify>(async (d,e)=>{
            this.CmdResult = e.CmdResult;
            await Task.CompletedTask;
            return true;
        });
        cmdResultNotifyTrigger.Register(Game.Instance);
    }
}

#endif
