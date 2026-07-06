using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DwarvenStorage;

// Reference: https://github.com/aedenthorn/ValheimMods/blob/master/CraftFromContainers/BepInExPlugin.cs
// Reference used for finding which methods to patch

[HarmonyPatch]
public static class CraftingPatches
{
    private static readonly System.Reflection.MethodInfo ContainerSaveMethod =
        AccessTools.Method(typeof(Container), "Save");

    private static readonly System.Reflection.MethodInfo InventoryChangedMethod =
        AccessTools.Method(typeof(Inventory), "Changed");

    /// <summary>
    /// Check if the crafting station is usable
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="player"></param>
    /// <param name="showMessage"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get the level of the crafting station
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="checkExtensions"></param>
    /// <param name="__result"></param>
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
    
    /// <summary>
    /// Check if the player has the required items to craft the item
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <param name="piece"></param>
    /// <param name="discover"></param>
    /// <param name="qualityLevel"></param>
    /// <param name="___m_knownMaterial"></param>
    /// <param name="amount"></param>
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

        // Save references to the inventory
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

    /// <summary>
    /// Consume the required items from the player inventory
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="requirements"></param>
    /// <param name="qualityLevel"></param>
    /// <param name="multiplier"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Show the amount of the required items in the crafting interface
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="elementRoot"></param>
    /// <param name="req"></param>
    /// <param name="player"></param>
    /// <param name="craft"></param>
    /// <param name="quality"></param>
    /// <param name="craftMultiplier"></param>
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
    
    // Helper methods

    /// <summary>
    /// Save the storage container
    /// </summary>
    /// <param name="container"></param>
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

    
    /// <summary>
    /// Mark the inventory as changed
    /// </summary>
    /// <param name="inventory"></param>
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