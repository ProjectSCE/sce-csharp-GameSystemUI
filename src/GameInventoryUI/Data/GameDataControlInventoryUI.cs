
using GameCore.BaseType;
using GameCore.Extension;
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameUI.Control.Primitive;
using GameSystemUI.GameInventoryUI.Advanced;
#endif
using GameCore.Struct;


namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 背包UI控件数据配置基类 - 不可直接实例化
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public abstract partial class GameDataControlInventoryUI
{
    /// <summary>
    /// 页标按钮模板配置
    /// </summary>
    public IGameLink<GameDataControlPageButton> PageButtonTemplate { get; set; }
    
    #if CLIENT
    public abstract override Control CreateControl();
    #endif
}