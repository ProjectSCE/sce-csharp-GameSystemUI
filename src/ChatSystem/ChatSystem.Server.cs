#if SERVER
using System.Text.Json.Serialization;
using GameCore.UserCloudData;
using TriggerEncapsulation;
using TriggerEncapsulation.Messaging;
namespace GameSystemUI.ChatSystem;
// 负责接受客户端消息并转发给对应接收客户端
public static class ChatMessageServer
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }

    private static void OnGameTriggerInitialization()
    {
        // 注册服务器消息处理器
        RegisterServerMessageHandlers();
    }

    /// <summary>
    /// 注册服务器消息处理器
    /// </summary>
    private static void RegisterServerMessageHandlers()
    {
        // 聊天消息处理
        TypedMessageHandler.Register<ChatMessage>(OnChatMessageReceived,
            MessagePriority.Normal, "ServerChatMessageHandler");
    }

    private static async Task<bool> OnChatMessageReceived(Player? sender, ChatMessage message)
    {
        // 防止伪造发送者信息，用sender的id替换message的senderid
        message.SenderID = sender?.Id;
        if (message.SenderID == null)
        {
            Game.Logger.LogError("OnChatMessageReceived: sender is null");
            return false;
        }
        message.Content = CensorMessage(message.Content);
        switch (message.Type)
        {
            case ChatMessageType.Private:
                SendMessagePrivate(message);
                break;
            case ChatMessageType.Team:
                SendMessageTeam(message);
                break;
            case ChatMessageType.All:
                SendMessageAll(message);
                break;
            case ChatMessageType.World:
                SendMessageAll(message);
                break;
        }
        await Task.CompletedTask;
        return true;
    }

    // TODO:检查并替换消息中的敏感词
    private static string CensorMessage(string message)
    {
        return message;
    }


    public static bool IsValidMessage(ChatMessage message)
    {
        return true;
    }

    // 发出的信息留档
    // public static void SaveMessage(ChatMessage message)
    // {
    //     // 可以保存到数据库
    // }

    
    // ██╗    ██╗ █████╗ ██████╗ ███╗   ██╗██╗███╗   ██╗ ██████╗ 
    // ██║    ██║██╔══██╗██╔══██╗████╗  ██║██║████╗  ██║██╔════╝ 
    // ██║ █╗ ██║███████║██████╔╝██╔██╗ ██║██║██╔██╗ ██║██║  ███╗
    // ██║███╗██║██╔══██║██╔══██╗██║╚██╗██║██║██║╚██╗██║██║   ██║
    // ╚███╔███╔╝██║  ██║██║  ██║██║ ╚████║██║██║ ╚████║╚██████╔╝
    //  ╚══╝╚══╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝╚═╝  ╚═══╝ ╚═════╝ 
    // TODO: 需要改成订阅发布以便跨局聊天
    public static void SendMessagePrivate(ChatMessage message)
    {
        if (message.ReceiverID == null || message.SenderID == null)
        {
            return;
        }
        var receiver = Player.GetById(message.ReceiverID.Value);
        var sender = Player.GetById(message.SenderID.Value);
        if (receiver == null || sender == null)
        {
            return;
        }
        // 可以检查下是不是好友之类的
        
        // 给接收者和发送者都发送消息（统一回音机制）
        MessageBuilder.Create(message)
            .ToPlayersWhere(p => p.Id == receiver.Id || p.Id == sender.Id)
            .SendAsync();
    }

    public static void SendMessageTeam(ChatMessage message)
    {
        if (message.SenderID == null)
        {
            return;
        }
        var sender = Player.GetById(message.SenderID.Value);
        if (sender == null)
        {
            return;
        }
        MessageBuilder.Create(message)
            .ToPlayersWhere(p => p.Team.Id == sender.Team.Id) // 包含发送者，使用服务端回音
            .SendAsync();
    }

    public static void SendMessageAll(ChatMessage message)
    {
        // 可以检查下有没有权限给全体发
        MessageBuilder.Create(data: message)
            .WithPriority(MessagePriority.Normal)
            .ToPlayersWhere((Player p) => {
                return true;
            }) // 包含发送者，使用服务端回音
            .SendAsync();
    }

    public static void SendMessageSystem(ChatMessage message, Func<Player, bool> filter)
    {
        MessageBuilder.Create(message)
            .ToPlayersWhere(filter)
            .SendAsync();
    }
}
#endif