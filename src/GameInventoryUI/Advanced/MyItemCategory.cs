#if CLIENT
using GameCore.Struct;

using GameSystemUI.GameInventoryUI.Data;

namespace GameSystemUI.GameInventoryUI.Advanced;

[EnumExtension(Extends = typeof(ItemCategory))]
enum EMyItemCategory
{
    Test1,
    Test2,
    Test3,
}

[EnumExtension(Extends = typeof(ItemCategory))]
enum EDefaultItemCategory
{
    All,
}
#endif