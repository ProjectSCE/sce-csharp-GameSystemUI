using System.Text;
using System.Text.Json.Serialization;
using GameCore.BaseInterface;
using GameCore.PlayerAndUsers.Enum;
using TriggerEncapsulation.Messaging;
namespace GameSystemUI.ChatSystem;

/// <summary>
/// ChatSystem 初始化类，负责注册聊天系统
/// </summary>
public class ChatSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
#if CLIENT
        ChatMessageClient.OnRegisterGameClass();
#endif

#if SERVER
        ChatMessageServer.OnRegisterGameClass();
#endif
        TypedMessageHandler.Initialize();
    }
}

public enum ChatMessageType
{
    Private,
    Team,
    All,
    World,
    System,
}

public class ChatMessage
{
    public Guid guid { get; set; } = Guid.NewGuid();
    public int? SenderID { get; set; }
    public int? ReceiverID { get; set; }
    public ChatMessageType Type { get; set; } = ChatMessageType.Private;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Content { get; set; } = string.Empty;
    #if CLIENT
    public override string ToString()
    {
        String result = string.Empty;
        Player? sender = Player.GetById(SenderID ?? 0);
        // 暂时不知道用户名用ID代替
        string username = $"Player{SenderID}";
        switch (Type)
        {
            case ChatMessageType.Private:
                if (SenderID == Player.LocalPlayer.Id)
                {
                    string receiver = $"Player{ReceiverID}";
                    result = $"<color=#FF50EE>[发送给]{receiver}: {Content}</color>";
                }
                else
                {
                    result = $"<color=#FF50EE>[来自]{username}: {Content}</color>";
                }
                break;
            case ChatMessageType.Team:
                result = $"<color=#70E383>[队伍]{username}:</color> {Content}";
                break;
            case ChatMessageType.World:
            case ChatMessageType.All:
                if (sender != null)
                {
                    switch (sender.GetRelationShip(Player.LocalPlayer))
                    {
                        case PlayerRelationShip.Player:
                        case PlayerRelationShip.Ally:
                            result = $"<color=#70E383>[全体]{username}:</color> {Content}";
                            break;
                        case PlayerRelationShip.Enemy:
                            result = $"<color=#E74747>[全体]{username}:</color> {Content}";
                            break;
                        case PlayerRelationShip.Neutral:
                            result = $"<color=#B3B5BC>[全体]{username}:</color> {Content}";
                            break;
                    }
                }
                break;
            case ChatMessageType.System:
                result = $"<color=#FFF395>[系统]: {Content}</color>";
                break;
        }
        return result;
    }
    #endif
    #if SERVER
    public override string ToString()
    {
        return $" [{Type}] Player{SenderID} : {Content}";
    }
    #endif
}