#if CLIENT
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Events;
using TriggerEncapsulation;
using TriggerEncapsulation.Event;
using TriggerEncapsulation.Messaging;
namespace GameSystemUI.ChatSystem;

public static class ChatMessageClient
{
    // 每一段时间内最多发送多少条消息
    private static TimeSpan timeLimit = TimeSpan.FromSeconds(30);
    private static int messageLimit = 5;
    private static Queue<DateTime> messageQueue = new();
    // 每条消息的长度
    private static int messageLengthLimit = 80;
    public static EventHandler<ChatMessage>? OnChatMessageReceived; //信息到达
    public static EventHandler<ChatMessage>? OnLocalSystemMessageSent; //本地系统信息发送
    public static Trigger<EventServerMessage>? OnServerMessageReceived;

    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }

    private static void OnGameTriggerInitialization()
    {
        // 注册客户端消息处理器
        RegisterClientMessageHandlers();
    }

    /// <summary>
    /// 注册客户端消息处理器
    /// </summary>
    private static void RegisterClientMessageHandlers()
    {
        // 聊天消息处理
        try
        {
            TypedMessageHandler.Register<ChatMessage>(ChatMessageReceivedHandler,
                MessagePriority.Normal, "ClientChatMessageHandler");
            // OnServerMessageReceived = new Trigger<EventServerMessage>(async (d,e)=>{
            //     await Task.CompletedTask;
            //     return true;
            // });
            // OnServerMessageReceived.Register(Game.Instance);
        }
        catch (Exception e)
        {
            Game.Logger.LogError("ChatMessageReceivedHandler register failed: {e}", e);
        }
    }

    private static bool IsMessageTimeLimitExceeded()
    {
        // 清理过期的消息时间戳
        while (messageQueue.Count > 0 && DateTime.Now - messageQueue.Peek() > timeLimit)
        {
            messageQueue.Dequeue(); // 移除过期的时间戳
        }

        // 检查当前消息数量是否超过限制
        if (messageQueue.Count >= messageLimit)
        {
            return true;
        }
        return false;
    }

    private static bool IsMessageLengthExceeded(string message)
    {
        if (message.Length > messageLengthLimit)
        {
            return true;
        }
        return false;
    }

    private static async Task<bool> ChatMessageReceivedHandler(Player? sender, ChatMessage message)
    {
        OnChatMessageReceived?.Invoke(sender, message);
        switch (message.Type)
        {
            case ChatMessageType.Private:
                break;
            case ChatMessageType.Team:
                break;
            case ChatMessageType.All:
                break;
            case ChatMessageType.World:
                break;
        }
        await Task.CompletedTask;
        return true;
    }

    public static async Task SendMessagePrivate(Player player, string message)
    {
        // 不能给自己发消息，给个本地警告
        if (player == Player.LocalPlayer)
        {
            OnLocalSystemMessageSent?.Invoke(Player.LocalPlayer, new ChatMessage()
            {
                Type = ChatMessageType.System,
                Content = "尝试给自己发消息"
            });
            return;
        }
        
        // 检查消息发送频率限制
        if (IsMessageTimeLimitExceeded() || IsMessageLengthExceeded(message))
        {
            OnLocalSystemMessageSent?.Invoke(Player.LocalPlayer, new ChatMessage()
            {
                Type = ChatMessageType.System,
                Content = "发送消息过于频繁，请稍后再试"
            });
            return;
        }
        
        var chatMessage = new ChatMessage
        {
            SenderID = Player.LocalPlayer.Id,
            ReceiverID = player.Id,
            Type = ChatMessageType.Private,
            Content = message
        };
        
        // 记录发送时间
        messageQueue.Enqueue(DateTime.Now);
        
        await MessageBuilder.Create(chatMessage)
            .SendToServerAsync();
    }

    public static async Task SendMessageTeam(string message)
    {
        // 检查消息发送频率限制
        if (IsMessageTimeLimitExceeded() || IsMessageLengthExceeded(message))
        {
            OnLocalSystemMessageSent?.Invoke(Player.LocalPlayer, new ChatMessage()
            {
                Type = ChatMessageType.System,
                Content = "发送消息过于频繁，请稍后再试"
            });
            return;
        }
        
        var chatMessage = new ChatMessage
        {
            SenderID = Player.LocalPlayer.Id,
            Type = ChatMessageType.Team,
            Content = message
        };
        
        // 记录发送时间
        messageQueue.Enqueue(DateTime.Now);
        
        await MessageBuilder.Create(chatMessage)
            .SendToServerAsync();
    }

    public static async Task SendMessageAll(string message)
    {
        // 检查消息发送频率限制
        if (IsMessageTimeLimitExceeded() || IsMessageLengthExceeded(message))
        {
            OnLocalSystemMessageSent?.Invoke(Player.LocalPlayer, new ChatMessage()
            {
                Type = ChatMessageType.System,
                Content = "发送消息过于频繁，请稍后再试"
            });
            return;
        }
        
        var chatMessage = new ChatMessage
        {
            SenderID = Player.LocalPlayer.Id,
            Type = ChatMessageType.All,
            Content = message
        };
        
        // 记录发送时间
        messageQueue.Enqueue(DateTime.Now);
        
        await MessageBuilder.Create(chatMessage)
            .SendToServerAsync();
    }

}
#endif