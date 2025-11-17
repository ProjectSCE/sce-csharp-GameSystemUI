namespace GameSystemUI.GameInventoryUI;

[EnumExtension(Extends = typeof(CmdError))]
enum ECmdErrorInventory
{
    ItemCarrierNotFound, //物品所有者为空
    SlotUINotFound, //物品格子UI为空
    AssignableSlotNotFound, // 找不到满足条件的格子
}
