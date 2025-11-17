
using GameData;
using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif
namespace GameSystemUI.GameInventoryUI.Data;

/// <summary>
/// 拾取列表控件数据配置
/// </summary>
[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlPickList : GameDataControlPanel, IGameData<GameDataControlPickList>, IGameData
{
    /// <summary>
    /// PickListItem模板配置
    /// </summary>
    public IGameLink<GameDataControlPickListItem>? PickListItemTemplate { get; set; }
    
    /// <summary>
    /// 列表高度
    /// </summary>
    public int? ListHeight { get; set; }
    
    /// <summary>
    /// 列表宽度
    /// </summary>
    public int? ListWidth { get; set; }

    #if CLIENT
    public override Control CreateControl()
    {
        return new PickList(Link);
    }

    public PickList CreatePickList()
    {
        return new PickList(Link);
    }

    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        
        // 将控件转换为PickList类型
        if (control is PickList pickList)
        {
            // PickListItem模板配置现在通过构造函数中的GameData模板应用，无需在此处处理
        }
    }
    #endif
}
