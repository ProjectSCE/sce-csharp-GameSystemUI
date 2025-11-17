using GameUI.Control;
using GameUI.Control.Data;
#if CLIENT
using GameSystemUI.GameInventoryUI.Advanced;
#endif

namespace GameSystemUI.GameInventoryUI.Data;

[GameDataNodeType<GameDataControl, GameDataControlPanel>]
public partial class GameDataControlInventorySlotUI : GameDataControlPanel, IGameData<GameDataControlInventorySlotUI>, IGameData
{
    public int? ItemSize{get; set;}
    public int? SlotSize{get; set;}

    #if CLIENT
    public override void ApplyTo(Control control)
    {
        base.ApplyTo(control);
        if (control is InventorySlotUI slotUI)
        {
            if (ItemSize.HasValue)
            {
                slotUI.ItemSize = ItemSize.Value;
            }
            if (SlotSize.HasValue)
            {
                slotUI.SlotSize = SlotSize.Value;
            }
        }
    }

    public override Control CreateControl()
    {
        return new InventorySlotUI(Link);
    }
    #endif
}