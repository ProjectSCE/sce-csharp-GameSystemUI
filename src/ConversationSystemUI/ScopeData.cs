#if CLIENT

using GameCore.Container;
using GameCore.Data;
using GameData;
using GameSystemUI.ConversationSystemUI.Data;
using GameUI.Control.Data;
using GameUI.Struct;

namespace GameSystemUI.ConversationSystemUI;
public class ScopeData : IGameClass
{
    public static class Control
    {
        /// <summary>
        /// 默认对话选择项模板
        /// </summary>
        public static readonly GameLink<GameDataControl, GameDataControlConversationChoiceItem> DefaultConversationChoiceItem = new("DefaultConversationChoiceItem");
        
        /// <summary>
        /// 默认气泡UI模板
        /// </summary>
        public static readonly GameLink<GameDataControl, GameDataControlBubbleUI> DefaultBubbleUI = new("DefaultBubbleUI");
        
        // public static readonly GameLink<GameDataControl, GameDataConversationPanel> DefaultConversationPanel = new("DefaultConversationPanel");
    }

    public static class BubbleDirectionLayout
    {
        public static readonly GameLink<GameDataBubbleUILayout, GameDataBubbleUILayout> Right = new("DefaultBubbleUIRight");
        public static readonly GameLink<GameDataBubbleUILayout, GameDataBubbleUILayout> Left = new("DefaultBubbleUILeft");
        public static readonly GameLink<GameDataBubbleUILayout, GameDataBubbleUILayout> Up = new("DefaultBubbleUIUp");
        public static readonly GameLink<GameDataBubbleUILayout, GameDataBubbleUILayout> Down = new("DefaultBubbleUIDown");
    }
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }

    private static void OnGameDataInitialization()
    {
        Game.Logger.LogDebug("初始化ConversationSystem UI数据");
        
        // 创建默认对话选择项模板
        _ = new GameDataControlConversationChoiceItem(Control.DefaultConversationChoiceItem)
        {
            Layout = new()
            {
                WidthStretchRatio = 1.0f,
                WidthCompactRatio = 1.0f,
                Height = 66,
                Margin = new Thickness(left: 0, top: 14, right: 0, bottom: 14),
            }
        };
        
        // 创建默认气泡UI模板
        _ = new GameDataControlBubbleUI(Control.DefaultBubbleUI)
        {
            DefaultDirection = BubbleDirection.Right,
            DirectionLayouts = new Dictionary<BubbleDirection, IGameLink<GameDataBubbleUILayout>>()
            {
                { BubbleDirection.Right, BubbleDirectionLayout.Right },
                { BubbleDirection.Left, BubbleDirectionLayout.Left },
                { BubbleDirection.Up, BubbleDirectionLayout.Up },
                { BubbleDirection.Down, BubbleDirectionLayout.Down },
            }
        };
        _ = new GameDataBubbleUILayout(BubbleDirectionLayout.Right)
        {
            BubbleImage = new Image("@gameui/image/conversation/气泡右.png"),
            UIOffset = new UIPosition(Left: 50, 0),  // UI坐标偏移（屏幕XY）
            OffsetZ = -75,  // 世界坐标Z轴偏移（深度）
            CharsPerLine = 20,
            TextMargin = new Thickness(36, 21, right: 18, bottom: 20),
            SlicedEdges = new Thickness(36, 21, right: 18, bottom: 20),  // 九宫格切片边界
            SocketName = "socket_blood_bar",  // 绑点名称
        };
        _ = new GameDataBubbleUILayout(BubbleDirectionLayout.Left)
        {
            BubbleImage = new Image("@gameui/image/conversation/气泡左.png"),
            UIOffset = new UIPosition(-50, 0),  // UI坐标偏移（屏幕XY）
            OffsetZ = -75,  // 世界坐标Z轴偏移（深度）
            CharsPerLine = 20,
            TextMargin = new Thickness(18, 20, right: 36, bottom: 21),
            SlicedEdges = new Thickness(18, 20, right: 36, bottom: 21),
            SocketName = "socket_blood_bar",  // 绑点名称
        };
        _ = new GameDataBubbleUILayout(BubbleDirectionLayout.Up)
        {
            BubbleImage = new Image("@gameui/image/conversation/气泡上.png"),
            UIOffset = new UIPosition(0, 0),  // UI坐标偏移（屏幕XY）
            OffsetZ = -50,  // 世界坐标Z轴偏移（深度）
            CharsPerLine = 20,
            TextMargin = new Thickness(24, 21, right: 13, bottom: 32),
            SlicedEdges = new Thickness(24, 21, right: 13, bottom: 32),
            SocketName = "socket_blood_bar",  // 绑点名称
        };
        _ = new GameDataBubbleUILayout(BubbleDirectionLayout.Down)
        {
            BubbleImage = new Image("@gameui/image/conversation/气泡下.png"),
            UIOffset = new UIPosition(0, 0),  // UI坐标偏移（屏幕XY）
            OffsetZ = 0,  // 世界坐标Z轴偏移（深度）
            CharsPerLine = 20,
            TextMargin = new Thickness(24, top: 32, right: 13, bottom: 21),
            SlicedEdges = new Thickness(24, top: 32, right: 13, bottom: 21),
            SocketName = "socket_root",  // 绑点名称
        };
        // _ = new GameDataConversationPanel(Control.DefaultConversationPanel);
    }
}

#endif