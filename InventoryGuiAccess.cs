using System;
using System.Reflection;
using HarmonyLib;

namespace DwarvenStorage;

// Valheim exposes no public equivalents for these operations. Keep private API
// access here so future game changes can be handled in one place.
internal static class InventoryGuiAccess
{
    private static readonly MethodInfo SetupCraftingMethod =
        AccessTools.Method(typeof(InventoryGui), "SetupCrafting", Type.EmptyTypes)
        ?? throw new MissingMethodException(typeof(InventoryGui).FullName, "SetupCrafting");

    private static readonly MethodInfo SetupDragItemMethod =
        AccessTools.Method(typeof(InventoryGui), "SetupDragItem",
            new[] { typeof(ItemDrop.ItemData), typeof(Inventory), typeof(int) })
        ?? throw new MissingMethodException(typeof(InventoryGui).FullName, "SetupDragItem");

    private static readonly FieldInfo DragItemField =
        AccessTools.Field(typeof(InventoryGui), "m_dragItem")
        ?? throw new MissingFieldException(typeof(InventoryGui).FullName, "m_dragItem");

    private static readonly FieldInfo DragInventoryField =
        AccessTools.Field(typeof(InventoryGui), "m_dragInventory")
        ?? throw new MissingFieldException(typeof(InventoryGui).FullName, "m_dragInventory");

    internal static void RefreshCrafting(InventoryGui gui)
    {
        if (gui) SetupCraftingMethod.Invoke(gui, Array.Empty<object>());
    }

    internal static ItemDrop.ItemData GetDragItem(InventoryGui gui)
    {
        return gui ? (ItemDrop.ItemData)DragItemField.GetValue(gui) : null;
    }

    internal static Inventory GetDragInventory(InventoryGui gui)
    {
        return gui ? (Inventory)DragInventoryField.GetValue(gui) : null;
    }

    internal static void ClearDragItem(InventoryGui gui)
    {
        // Use the game's cleanup to also remove the drag visual and reset its amount.
        if (gui) SetupDragItemMethod.Invoke(gui, new object[] { null, null, 1 });
    }
}
