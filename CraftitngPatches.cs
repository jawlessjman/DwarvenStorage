using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DwarvenStorage;

[HarmonyPatch]
public static class CraftingPatches
{
    private static readonly System.Reflection.MethodInfo ContainerSaveMethod =
        AccessTools.Method(typeof(Container), "Save");

    private static readonly System.Reflection.MethodInfo InventoryChangedMethod =
        AccessTools.Method(typeof(Inventory), "Changed");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CheckUsable))]
    private static bool CheckUsablePrefix(
        CraftingStation __instance,
        Player player,
        bool showMessage,
        ref bool __result)
    {
        var storage = StorageInterface.Instance;
        if (storage == null) return true;
        if (storage.CraftingStation == null) return true;

        if (__instance != storage.CraftingStation) return true;

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

        __result = Mathf.Max(1, storage.CurrentStationLevel);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    private static void HaveRequirementItemsPostfix(
        Player __instance,
        ref bool __result,
        Recipe piece,
        bool discover,
        int qualityLevel,
        HashSet<string> ___m_knownMaterial,
        int amount)
    {
        if (__result) return;
        if (discover) return;

        var storage = StorageInterface.Instance;
        if (storage == null) return;

        var container = storage.Container;
        if (container == null) return;

        var playerInventory = __instance.GetInventory();
        var storageInventory = container.GetInventory();

        if (playerInventory == null || storageInventory == null) return;
        if (piece == null || piece.m_resources == null) return;

        foreach (var requirement in piece.m_resources)
        {
            if (requirement.m_resItem == null) continue;

            var requiredAmount = requirement.GetAmount(qualityLevel) * amount;
            if (requiredAmount <= 0) continue;

            var itemName = requirement.m_resItem.m_itemData.m_shared.m_name;

            var availableAmount =
                playerInventory.CountItems(itemName) +
                storageInventory.CountItems(itemName);

            if (availableAmount < requiredAmount)
            {
                return;
            }
        }

        __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    private static bool ConsumeResourcesPrefix(
        Player __instance,
        Piece.Requirement[] requirements,
        int qualityLevel,
        int multiplier)
    {
        var storage = StorageInterface.Instance;
        if (storage == null) return true;

        var container = storage.Container;
        if (container == null) return true;

        var playerInventory = __instance.GetInventory();
        var storageInventory = container.GetInventory();

        if (playerInventory == null || storageInventory == null) return true;

        foreach (var requirement in requirements)
        {
            if (requirement.m_resItem == null) continue;

            var requiredAmount = requirement.GetAmount(qualityLevel) * multiplier;
            if (requiredAmount <= 0) continue;

            var itemName = requirement.m_resItem.m_itemData.m_shared.m_name;

            var playerAmount = playerInventory.CountItems(itemName);
            var storageAmount = storageInventory.CountItems(itemName);
            var totalAvailable = playerAmount + storageAmount;

            if (totalAvailable < requiredAmount)
            {
                Plugin.Logger.LogWarning(
                    $"Not enough {itemName}. Required={requiredAmount}, Available={totalAvailable}"
                );

                // This should not normally happen because HaveRequirementItems should block crafting.
                // Returning true lets vanilla handle it instead of silently making items free.
                return true;
            }

            var removeFromPlayer = Mathf.Min(playerAmount, requiredAmount);
            if (removeFromPlayer > 0)
            {
                playerInventory.RemoveItem(itemName, removeFromPlayer);
            }

            var remaining = requiredAmount - removeFromPlayer;
            if (remaining > 0)
            {
                storageInventory.RemoveItem(itemName, remaining);
            }
        }

        MarkInventoryChanged(playerInventory);
        MarkInventoryChanged(storageInventory);
        SaveContainer(container);

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    private static void SetupRequirementPostfix(
        InventoryGui __instance,
        Transform elementRoot,
        Piece.Requirement req,
        Player player,
        bool craft,
        int quality,
        int craftMultiplier)
    {
        var storage = StorageInterface.Instance;
        if (storage == null) return;

        var container = storage.Container;
        if (container == null) return;
        if (req.m_resItem == null) return;

        var playerInventory = player.GetInventory();
        var storageInventory = container.GetInventory();

        if (playerInventory == null || storageInventory == null) return;

        var requiredAmount = req.GetAmount(quality) * craftMultiplier;
        if (requiredAmount <= 0) return;

        var itemName = req.m_resItem.m_itemData.m_shared.m_name;

        var playerAmount = playerInventory.CountItems(itemName);
        var storageAmount = storageInventory.CountItems(itemName);
        var totalAmount = playerAmount + storageAmount;

        var amountText = elementRoot.Find("res_amount")?.GetComponent<TMP_Text>();
        if (amountText == null) return;

        amountText.text = $"{totalAmount}/{requiredAmount}";

        if (playerAmount < requiredAmount && totalAmount >= requiredAmount)
        {
            amountText.color = Color.yellow;
        }
    }

    private static void SaveContainer(Container container)
    {
        if (container == null) return;

        try
        {
            ContainerSaveMethod?.Invoke(container, Array.Empty<object>());
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Failed to save storage container: {e.Message}");
        }
    }

    private static void MarkInventoryChanged(Inventory inventory)
    {
        if (inventory == null) return;

        try
        {
            InventoryChangedMethod?.Invoke(inventory, Array.Empty<object>());
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Failed to mark inventory changed: {e.Message}");
        }
    }
}