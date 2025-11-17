using GameCore.Extension;
using GameCore.Platform.SDL;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;

namespace GameSystemUI.AbilitySystemUI.Data;

[GameDataCategory]
public partial class GameDataBindKeyConfig : GameDataCategory<GameDataBindKeyConfig>, IGameDataCategory<GameDataBindKeyConfig>, IGameData<GameDataBindKeyConfig> ,IGameData
{
    /// <summary>
    /// 默认按键绑定顺序，定义技能按钮的默认快捷键序列
    /// 例如：[VirtualKey.Q, VirtualKey.W, VirtualKey.E, VirtualKey.R] 
    /// 表示第一个技能绑定Q键，第二个技能绑定W键，以此类推
    /// </summary>
    public VirtualKey[]? DefaultKeyBindingOrder { get; set; }
}
