using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace DwarvenStorage;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    public const string ModGuid = "jawlessjman.DwarvenStorage";
    public const string ModName = "Dwarven Storage";
    public const string ModVersion = "1.0.0";

    public static readonly Dictionary<string, string> UpgradeStationsByPrefabName = new();
    public static readonly Dictionary<string, int> UpgradeAmountsByPrefabName = new();

    private static readonly List<StorageExtensionData> StorageExtensions = [
        new()
        {
            Name = "Extension_Tier_1",
            Description = "Adds one row of slots to your storage system.",
            Rows = 1,
            CraftingStation = CraftingStations.Forge,
            ItemRequirements = [
                new ItemRequirements { Name = "Wood", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "SurtlingCore", Amount = 2, UpgradeAmount = 0 }
            ]
        },
        new()
        {
            Name = "Extension_Tier_2",
            Description = "Adds three rows of slots to your storage system.",
            CraftingStation = CraftingStations.Forge,
            Rows = 3,
            ItemRequirements = [
                new ItemRequirements { Name = "FineWood", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 8, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Chain", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "SurtlingCore", Amount = 2, UpgradeAmount = 0 }
            ]
        },
        new()
        {
            Name = "Extension_Tier_3",
            Description = "Adds five rows of slots to your storage system.",
            Rows = 5,
            CraftingStation = CraftingStations.BlackForge,
            ItemRequirements = [
                new ItemRequirements { Name = "YggdrasilWood", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Eitr", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMetal", Amount = 6, UpgradeAmount = 0 }
            ]
        },
        new()
        {
            Name = "Extension_Tier_4",
            Description = "Adds seven rows of slots to your storage system.",
            Rows = 7,
            CraftingStation = CraftingStations.BlackForge,
            ItemRequirements = [
                new ItemRequirements { Name = "Blackwood", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "CharredBone", Amount = 16, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MoltenCore", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 4, UpgradeAmount = 0 }
            ]
        },
    ];

    private static readonly List<StorageUpgrade> StorageUpgrades = [
        // Workbench
        new() {Name="Workbench_Upgrade_1", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", MinQualityLevel = 1
        },
        new() {Name="Workbench_Upgrade_2", CraftingStation = CraftingStations.Workbench, ItemRequirements =
                [
                    new ItemRequirements { Name = "Workbench_Upgrade_1", Amount = 1, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Flint", Amount = 5, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", MinQualityLevel = 2
        },
        new() {Name="Workbench_Upgrade_3", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "Workbench_Upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Flint", Amount = 7, UpgradeAmount = 0 },
                new ItemRequirements { Name = "LeatherScraps", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "DeerHide", Amount = 2, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", MinQualityLevel = 3
        },
        new() {Name="Workbench_Upgrade_4", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "Workbench_Upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", MinQualityLevel = 4
        },
        new() {Name="Workbench_Upgrade_5", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "Workbench_Upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Obsidian", Amount = 2, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your workbench to hold more items.", StationKey = "workbench", MinQualityLevel = 5
        },
        
        // Forge
        new() {Name="Forge_Upgrade_1", CraftingStation = CraftingStations.Forge, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Stone", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Coal", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Copper", Amount = 3, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 1
        },
        new() {Name="Forge_Upgrade_2", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 12, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 5, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 2
        },
        new() {Name="Forge_Upgrade_3", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 3
        },
        new() {Name="Forge_Upgrade_4", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 10, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 4
        },
        new() {Name="Forge_Upgrade_5", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 7, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 5
        },
        new() {Name="Forge_Upgrade_6", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_5", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "DeerHide", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Chain", Amount = 2, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 6
        },
        new() {Name="Forge_Upgrade_7", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "Forge_Upgrade_6", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 12, UpgradeAmount = 0 },
                new ItemRequirements { Name = "SharpeningStone", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your forge to hold more items.", StationKey = "forge", MinQualityLevel = 7
        },
        
        // Cauldron
        new() {Name="Cauldron_Upgrade_1", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
                [
                    new ItemRequirements { Name = "Tin", Amount = 5, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 1
        },
        new() {Name="Cauldron_Upgrade_2", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "Cauldron_Upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Dandelion", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Carrot", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Mushroom", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Thistle", Amount = 3, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Turnip", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 2
        },
        new() {Name="Cauldron_Upgrade_3", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "Cauldron_Upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "RoundLog", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "ElderBark", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Silver", Amount = 1, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 3
        },
        new() {Name="Cauldron_Upgrade_4", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "Cauldron_Upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMetal", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 4
        },
        new() {Name="Cauldron_Upgrade_5", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "Cauldron_Upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "RoundLog", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 3, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 5
        },
        new() {Name="Cauldron_Upgrade_6", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "Cauldron_Upgrade_5", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 3, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your cauldron to hold more items.", StationKey = "cauldron", MinQualityLevel = 6
        },
        
        // Stonecutter
        new() {Name="StoneCutter_Upgrade", CraftingStation = CraftingStations.Stonecutter, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Iron", Amount = 1, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Stone", Amount = 2, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your stonecutter to hold more items.", StationKey = "stonecutter", MinQualityLevel = 1
        },
        
        // Artisan Table
        new() {Name="ArtisanTable_Upgrade_1", CraftingStation = CraftingStations.ArtisanTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "DragonTear", Amount = 1, UpgradeAmount = 0 },
                ], 
            Description = "Upgrade your Artisan to hold more items.", StationKey = "artisan", MinQualityLevel = 1
        },
        new() {Name="ArtisanTable_Upgrade_2", CraftingStation = CraftingStations.ArtisanTable, ItemRequirements =
            [
                new ItemRequirements { Name = "ArtisanTable_Upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "QueenDrop", Amount = 1, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your Artisan to hold more items.", StationKey = "artisan", MinQualityLevel = 2
        },
        
        // Prep table
        new() {Name="PrepTable_Upgrade", CraftingStation = CraftingStations.FoodPreparationTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "FineWood", Amount = 10, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "LeatherScraps", Amount = 7, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your prep table to hold more items.", StationKey = "preptable", MinQualityLevel = 1
        },
        
        // Blackforge
        new() {Name="Blackforge_Upgrade_1", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
                [
                    new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "YggdrasilWood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "BlackCore", Amount = 2, UpgradeAmount = 0 },
                ], 
            Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", MinQualityLevel = 1
        },
        new() {Name="Blackforge_Upgrade_2", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "Blackforge_Upgrade_1", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", MinQualityLevel = 2
        },
        new() {Name="Blackforge_Upgrade_3", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "Blackforge_Upgrade_2", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MechanicalSpring", Amount = 1, UpgradeAmount = 0 },
            ], 
            Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", MinQualityLevel = 3
        },
        new() {Name="Blackforge_Upgrade_4", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "Blackforge_Upgrade_3", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "CharredBone", Amount = 2, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", MinQualityLevel = 4
        },
        new() {Name="Blackforge_Upgrade_5", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "Blackforge_Upgrade_4", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "GemstoneRed", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MorgenSinew", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your black forge to hold more items.", StationKey = "blackforge", MinQualityLevel = 5
        },
        
        // GaldrTable
        new() {Name="GaldrTable_Upgrade_1", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "BlackMetal", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "YggdrasilWood", Amount = 10, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "BlackCore", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Eitr", Amount = 2, UpgradeAmount = 0 }
                ], 
            Description = "Upgrade your Galdr table to hold more items.", StationKey = "galdr", MinQualityLevel = 1
        },
        new() {Name="GaldrTable_Upgrade_2", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "GaldrTable_Upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "YggdrasilWood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your Galdr table to hold more items.", StationKey = "galdr", MinQualityLevel = 2
        },
        new() {Name="GaldrTable_Upgrade_3", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "GaldrTable_Upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Resin", Amount = 7, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 }, 
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "TrophySkeleton", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your Galdr table to hold more items.", StationKey = "galdr", MinQualityLevel = 3
        },
        new() {Name="GaldrTable_Upgrade_4", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "GaldrTable_Upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "CelestialFeather", Amount = 4, UpgradeAmount = 0 }, 
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "TrophyAsksvin", Amount = 1, UpgradeAmount = 0 }
            ], 
            Description = "Upgrade your Galdr table to hold more items.", StationKey = "galdr", MinQualityLevel = 4
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
        PrefabManager.OnVanillaPrefabsAvailable += CreateStorageExtensions;
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

    private static void CreateStorageExtensions()
    {
        foreach (var storage in StorageExtensions)
        {
            var pieceConfig = new PieceConfig()
            {
                Name = "$" + storage.Name,
                PieceTable = PieceTables.Hammer,
                Description = "$" + storage.Description,
                CraftingStation = storage.CraftingStation,
                Category = PieceCategories.Misc,
            };

            foreach (var requirement in storage.ItemRequirements)
            {
                pieceConfig.AddRequirement(requirement.Name, requirement.Amount);
            }
            
            var piece = new CustomPiece(storage.Name, "wood_wall_quarter", pieceConfig);
            var extension = piece.PiecePrefab.AddComponent<StorageInterfaceExtension>();
            extension.AddedRows = storage.Rows;
            extension.AddedColumns = 0;
            
            PieceManager.Instance.AddPiece(piece);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateStorageExtensions;
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
                MinStationLevel = item.MinQualityLevel
            };

            foreach (var requirement in item.ItemRequirements)
            {
                itemConfig.AddRequirement(requirement.Name, requirement.Amount, requirement.UpgradeAmount);
            }
            
            var customItem = new CustomItem(item.Name, "AskHide", itemConfig);
            
            customItem.ItemDrop.m_itemData.m_quality = item.MinQualityLevel;
            
            customItem.ItemDrop.m_itemData.m_customData["DwarvenUpgrade"] = item.StationKey;
            
            UpgradeStationsByPrefabName[item.Name] = item.StationKey;
            UpgradeAmountsByPrefabName[item.Name] = item.MinQualityLevel;
            
            ItemManager.Instance.AddItem(customItem);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateUpgradeItems;
    }
}
