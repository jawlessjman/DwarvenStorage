using HarmonyLib;
using Jotunn.Managers;
namespace DwarvenStorage;

[HarmonyPatch]
public class CraftingPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CheckUsable))]
    private static bool CheckUsablePrefix(CraftingStation __instance, Player player, bool showMessage, ref bool __result)
    {
        if (StorageInterface.Instance == null) return true;
        if (StorageInterface.Instance.CraftingStation == null) return true;

        if (__instance != StorageInterface.Instance.CraftingStation) return true;

        __result = true;
        return false;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
    private static void GetLevelPostfix(
        CraftingStation __instance,
        bool checkExtensions,
        ref int __result)
    {
        var storage = StorageInterface.Instance;
        if (storage == null) return;

        if (__instance != storage.CraftingStation) return;

        __result = 1;
    }
}