using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace DwarvenStorage;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public const string ModGuid = "jawlessjman.DwarvenStorage";
    public const string ModName = "Dwarven Storage";
    public const string ModVersion = "1.0.0";

    public static Dictionary<string, string> UpgradeStationsByPrefabName = new();
    
    public static List<string> UpgradeItemNames = [
        "Workbench_Upgrade",
        "Forge_Upgrade",
        "BlackForge_Upgrade",
        "GaldrTable_Upgrade",
        "Cauldron_Upgrade",
        "ArtisanTable_Upgrade"
    ];

    private static readonly List<StorageUpgrade> StorageUpgrades = [
        new StorageUpgrade() {Name="Workbench_Upgrade", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="Forge_Upgrade", CraftingStation = CraftingStations.Forge, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your forge to hold more items.", StationKey = "forge", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="Blackforge_Upgrade", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="GaldrTable_Upgrade", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your Galdr table to hold more items.", StationKey = "galdr", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="ArtisanTable_Upgrade", CraftingStation = CraftingStations.ArtisanTable, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your Artisan to hold more items.", StationKey = "artisan", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="Cauldron_Upgrade", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", CanBeUpgraded = true
        },
        new StorageUpgrade() {Name="StoneCutter_Upgrade", CraftingStation = CraftingStations.Stonecutter, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 0 }], Description = "Upgrade your stonecutter to hold more items.", StationKey = "stonecutter", CanBeUpgraded = false
        },
        new StorageUpgrade() {Name="PrepTable_Upgrade", CraftingStation = CraftingStations.FoodPreparationTable, ItemRequirements =
                [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 0 }], Description = "Upgrade your prep table to hold more items.", StationKey = "preptable", CanBeUpgraded = false
        },
    ];
    
    private Harmony _harmony;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        // Harmony Patches
        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();
        
        // Create Prefabs
        PrefabManager.OnVanillaPrefabsAvailable += CreateTestInterface;
        PrefabManager.OnVanillaPrefabsAvailable += CreateUpgradeItems;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }
    
    private static void CreateTestInterface()
    {
        var workbench = new PieceConfig
        {
            Name = "Test Interface",
            PieceTable = PieceTables.Hammer,
            Category = PieceCategories.Misc,
        };
        
        workbench.AddRequirement("Wood", 2);

        var piece = new CustomPiece("test_interface", "wood_wall_quarter", workbench);
        
        piece.PiecePrefab.AddComponent<StorageInterface>();
        piece.PiecePrefab.AddComponent<StorageUpgradeSlots>();
        
        PieceManager.Instance.AddPiece(piece);
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateTestInterface;
    }

    private static void CreateUpgradeItems()
    {
        foreach (var item in StorageUpgrades)
        {
            var itemConfig = new ItemConfig()
            {
                Name = "$" + item.Name,
                Description = "$" + item.Description,
                CraftingStation = item.CraftingStation,
                StackSize = 1,
                Weight = 2f,
                MinStationLevel = 1
            };

            foreach (var requirement in item.ItemRequirements)
            {
                itemConfig.AddRequirement(requirement.Name, requirement.Amount, requirement.UpgradeAmount);
            }
            
            var customItem = new CustomItem(item.Name, "AskHide", itemConfig);
            
            customItem.ItemDrop.m_itemData.m_customData["DwarvenUpgrade"] = item.CraftingStation;
            UpgradeStationsByPrefabName[item.Name] = item.StationKey;
            ItemManager.Instance.AddItem(customItem);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateUpgradeItems;
    }
}
