using System;
using System.Reflection;
using HarmonyLib;

namespace DwarvenStorage;

internal static class InventoryAccess
{
    private static readonly FieldInfo WidthField =
        AccessTools.Field(typeof(Inventory), "m_width")
        ?? throw new MissingFieldException(typeof(Inventory).FullName, "m_width");

    private static readonly MethodInfo ChangedMethod =
        AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) })
        ?? throw new MissingMethodException(typeof(Inventory).FullName, "Changed(bool, bool)");

    internal static void SetSize(Inventory inventory, int columns, int rows)
    {
        // There is a public height setter, but no equivalent for width.
        WidthField.SetValue(inventory, columns);
        inventory.SetHeight(rows);
    }

    internal static void NotifyChanged(Inventory inventory)
    {
        // Reflection requires arguments even for optional parameters. These are
        // the defaults for success and cheatedStateChanged in the game's API.
        ChangedMethod.Invoke(inventory, new object[] { false, false });
    }
}
