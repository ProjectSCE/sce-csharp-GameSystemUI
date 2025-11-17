#if CLIENT
using GameCore.Extension;
using GameCore.ResourceType;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;

namespace GameSystemUI.ChatSystem;
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataPlayerItem : GameDataControlPanel, IGameData<GameDataPlayerItem>, IGameData
{
    public Image AvatarFrame { get; set; } = "@gameui/image/chat/友方头像框.png"u8;
    public Image Avatar { get; set; } = "@gameui/image/chat/友方主控默认头像.png"u8;
    public Image NameplateBackground { get; set; } = "@gameui/image/chat/姓名板背景.png"u8;


    public override Control CreateControl()
    {
        return new PlayerItem((IGameLink<GameDataPlayerItem>)Link);
    }

    public PlayerItem CreatePlayerItem()
    {
        return new PlayerItem((IGameLink<GameDataPlayerItem>)Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        // if (control is PlayerItem playerItem)
        // {
        //     Game.Logger.LogInformation("AvatarFrame {AvatarFrame}", AvatarFrame);
        //     playerItem.AvatarFrame = AvatarFrame;
        //     playerItem.Avatar = Avatar;
        //     playerItem.NameplateBackground = NameplateBackground;
        // }
    }
}

#endif
