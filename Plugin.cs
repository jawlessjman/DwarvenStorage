using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace DwarvenStorage;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    public const string ModGuid = "jawlessjman.DwarvenStorage";
    public const string ModName = "DwarvenStorage";
    public const string ModVersion = "1.0.0";

    public static readonly Dictionary<string, string> UpgradeStationsByPrefabName = new();
    public static readonly Dictionary<string, int> UpgradeAmountsByPrefabName = new();

    private static readonly List<StorageExtensionData> StorageExtensions = [
        new()
        {
            Name = "$extension_tier_1",
            PrefabName = "extension_bronze",
            Description = "$extension_tier_1_desc",
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
            Name = "$extension_tier_2",
            PrefabName = "extension_iron",
            Description = "$extension_tier_2_desc",
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
            Name = "$extension_tier_3",
            Description = "$extension_tier_3_desc",
            PrefabName = "extension_black_metal",
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
            Name = "$extension_tier_4",
            PrefabName = "extension_flametal",
            Description = "$extension_tier_4_desc",
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
        new() {Name="$workbench_upgrade_1", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 }
            ], 
            SpriteName = "Workbench_1",
            Description = "$workbench_upgrade_1_desc", StationKey = "workbench", MinQualityLevel = 1
        },
        new() {Name="$workbench_upgrade_2", CraftingStation = CraftingStations.Workbench, ItemRequirements =
                [
                    new ItemRequirements { Name = "$workbench_upgrade_1", Amount = 1, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Flint", Amount = 5, UpgradeAmount = 0 }
                ], 
            SpriteName = "Workbench_2",
            Description = "$workbench_upgrade_2_desc", StationKey = "workbench", MinQualityLevel = 2
        },
        new() {Name="$workbench_upgrade_3", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "$workbench_upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Flint", Amount = 7, UpgradeAmount = 0 },
                new ItemRequirements { Name = "LeatherScraps", Amount = 10, UpgradeAmount = 0 },
                new ItemRequirements { Name = "DeerHide", Amount = 2, UpgradeAmount = 0 }
            ], SpriteName = "Workbench_3",
            
            Description = "$workbench_upgrade_3_desc", StationKey = "workbench", MinQualityLevel = 3
        },
        new() {Name="$workbench_upgrade_4", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "$workbench_upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Workbench_4",
            Description = "$workbench_upgrade_4_desc", StationKey = "workbench", MinQualityLevel = 4
        },
        new() {Name="$workbench_upgrade_5", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [
                new ItemRequirements { Name = "$workbench_upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Obsidian", Amount = 2, UpgradeAmount = 0 }
            ], 
            SpriteName = "Workbench_5",
            Description = "$workbench_upgrade_5_desc", StationKey = "workbench", MinQualityLevel = 5
        },
        
        // Forge
        new() {Name="$forge_upgrade_1", CraftingStation = CraftingStations.Forge, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Stone", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Coal", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Copper", Amount = 3, UpgradeAmount = 0 }
                ], 
            SpriteName = "Forge_1",
            Description = "$forge_upgrade_1_desc", StationKey = "forge", MinQualityLevel = 1
        },
        new() {Name="$forge_upgrade_2", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 12, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 5, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_2",
            Description = "$forge_upgrade_2_desc", StationKey = "forge", MinQualityLevel = 2
        },
        new() {Name="$forge_upgrade_3", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_3",
            Description = "$forge_upgrade_3_desc", StationKey = "forge", MinQualityLevel = 3
        },
        new() {Name="$forge_upgrade_4", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 10, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_4",
            Description = "$forge_upgrade_4_desc", StationKey = "forge", MinQualityLevel = 4
        },
        new() {Name="$forge_upgrade_5", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 7, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_5",
            Description = "$forge_upgrade_5_desc", StationKey = "forge", MinQualityLevel = 5
        },
        new() {Name="$forge_upgrade_6", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_5", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "DeerHide", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Chain", Amount = 2, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_6",
            Description = "$forge_upgrade_6_desc", StationKey = "forge", MinQualityLevel = 6
        },
        new() {Name="$forge_upgrade_7", CraftingStation = CraftingStations.Forge, ItemRequirements =
            [
                new ItemRequirements { Name = "$forge_upgrade_6", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Wood", Amount = 12, UpgradeAmount = 0 },
                new ItemRequirements { Name = "SharpeningStone", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Forge_7",
            Description = "$forge_upgrade_7_desc", StationKey = "forge", MinQualityLevel = 7
        },
        
        // Cauldron
        new() {Name="$cauldron_upgrade_1", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
                [
                    new ItemRequirements { Name = "Tin", Amount = 5, UpgradeAmount = 0 }
                ], 
            SpriteName = "Cauldron_1",
            Description = "$cauldron_upgrade_1_desc", StationKey = "cauldron", MinQualityLevel = 1
        },
        new() {Name="$cauldron_upgrade_2", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "$cauldron_upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Dandelion", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Carrot", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Mushroom", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Thistle", Amount = 3, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Turnip", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Cauldron_2",
            Description = "$cauldron_upgrade_2_desc", StationKey = "cauldron", MinQualityLevel = 2
        },
        new() {Name="$cauldron_upgrade_3", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "$cauldron_upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "RoundLog", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "ElderBark", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Silver", Amount = 1, UpgradeAmount = 0 },
            ], 
            SpriteName = "Cauldron_3",
            Description = "$cauldron_upgrade_3_desc", StationKey = "cauldron", MinQualityLevel = 3
        },
        new() {Name="$cauldron_upgrade_4", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "$cauldron_upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMetal", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 5, UpgradeAmount = 0 },
            ], 
            SpriteName = "Cauldron_4",
            Description = "$cauldron_upgrade_4_desc", StationKey = "cauldron", MinQualityLevel = 4
        },
        new() {Name="$cauldron_upgrade_5", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "$cauldron_upgrade_4", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "RoundLog", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 3, UpgradeAmount = 0 },
            ], 
            SpriteName = "Cauldron_5",
            Description = "$cauldron_upgrade_5_desc", StationKey = "cauldron", MinQualityLevel = 5
        },
        new() {Name="$cauldron_upgrade_6", CraftingStation = CraftingStations.Cauldron, ItemRequirements =
            [
                new ItemRequirements { Name = "$cauldron_upgrade_5", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FineWood", Amount = 3, UpgradeAmount = 0 },
            ], 
            SpriteName = "Cauldron_6",
            Description = "$cauldron_upgrade_6_desc", StationKey = "cauldron", MinQualityLevel = 6
        },
        
        // Stonecutter
        new() {Name="$stonecutter_upgrade", CraftingStation = CraftingStations.Stonecutter, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Iron", Amount = 1, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Stone", Amount = 2, UpgradeAmount = 0 }
                ], 
            SpriteName = "Stonecutter_1",
            Description = "$stonecutter_upgrade_desc", StationKey = "stonecutter", MinQualityLevel = 1
        },
        
        // Artisan Table
        new() {Name="$artisan_table_upgrade_1", CraftingStation = CraftingStations.ArtisanTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "Wood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "DragonTear", Amount = 1, UpgradeAmount = 0 },
                ], 
            SpriteName = "Artisan_1",
            Description = "$artisan_table_upgrade_1_desc", StationKey = "artisan", MinQualityLevel = 1
        },
        new() {Name="$artisan_table_upgrade_2", CraftingStation = CraftingStations.ArtisanTable, ItemRequirements =
            [
                new ItemRequirements { Name = "$artisan_table_upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Bronze", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "QueenDrop", Amount = 1, UpgradeAmount = 0 },
            ], 
            SpriteName = "Artisan_2",
            Description = "$artisan_table_upgrade_2_desc", StationKey = "artisan", MinQualityLevel = 2
        },
        
        // Prep table
        new() {Name="$prep_table_upgrade", CraftingStation = CraftingStations.FoodPreparationTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "FineWood", Amount = 10, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "LeatherScraps", Amount = 7, UpgradeAmount = 0 }
                ], 
            SpriteName = "Prep_Table_1",
            Description = "$prep_table_upgrade_desc", StationKey = "preptable", MinQualityLevel = 1
        },
        
        // Blackforge
        new() {Name="$blackforge_upgrade_1", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
                [
                    new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "YggdrasilWood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "BlackCore", Amount = 2, UpgradeAmount = 0 },
                ], 
            SpriteName = "Black_Forge_1",
            Description = "$blackforge_upgrade_1_desc", StationKey = "blackforge", MinQualityLevel = 1
        },
        new() {Name="$blackforge_upgrade_2", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_1", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
            ], 
            SpriteName = "Black_Forge_2",
            Description = "$blackforge_upgrade_2_desc", StationKey = "blackforge", MinQualityLevel = 2
        },
        new() {Name="$blackforge_upgrade_3", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_2", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MechanicalSpring", Amount = 1, UpgradeAmount = 0 },
            ], 
            SpriteName = "Black_Forge_3",
            Description = "$blackforge_upgrade_3_desc", StationKey = "blackforge", MinQualityLevel = 3
        },
        new() {Name="$blackforge_upgrade_4", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_3", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "CharredBone", Amount = 2, UpgradeAmount = 0 }
            ], 
            SpriteName = "Black_Forge_4",
            Description = "$blackforge_upgrade_4_desc", StationKey = "blackforge", MinQualityLevel = 4
        },
        new() {Name="$blackforge_upgrade_5", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_4", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "GemstoneRed", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "FlametalNew", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MorgenSinew", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Black_Forge_5",
            Description = "$blackforge_upgrade_5_desc", StationKey = "blackforge", MinQualityLevel = 5
        },
        
        // GaldrTable
        new() {Name="$galdr_table_upgrade_1", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
                [
                    new ItemRequirements { Name = "BlackMetal", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "YggdrasilWood", Amount = 10, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "BlackCore", Amount = 2, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "Eitr", Amount = 2, UpgradeAmount = 0 }
                ], 
            SpriteName = "Mage_table_1",
            Description = "$galdr_table_upgrade_1_desc", StationKey = "galdr", MinQualityLevel = 1
        },
        new() {Name="$galdr_table_upgrade_2", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "$galdr_table_upgrade_1", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "YggdrasilWood", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 }
            ], 
            SpriteName = "Mage_table_2",
            Description = "$galdr_table_upgrade_2_desc", StationKey = "galdr", MinQualityLevel = 2
        },
        new() {Name="$galdr_table_upgrade_3", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "$galdr_table_upgrade_2", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Resin", Amount = 7, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 }, 
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "TrophySkeleton", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Mage_table_3",
            Description = "$galdr_table_upgrade_3_desc", StationKey = "galdr", MinQualityLevel = 3
        },
        new() {Name="$galdr_table_upgrade_4", CraftingStation = CraftingStations.GaldrTable, ItemRequirements =
            [
                new ItemRequirements { Name = "$galdr_table_upgrade_3", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Blackwood", Amount = 1, UpgradeAmount = 0 },
                new ItemRequirements { Name = "CelestialFeather", Amount = 4, UpgradeAmount = 0 }, 
                new ItemRequirements { Name = "Eitr", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "TrophyAsksvin", Amount = 1, UpgradeAmount = 0 }
            ], 
            SpriteName = "Mage_table_4",
            Description = "$galdr_table_upgrade_4_desc", StationKey = "galdr", MinQualityLevel = 4
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
        
        // Load Assets
        AssetHolder.LoadAssetBundle();
        
        const string resourceName = $"{ModName}.Assets.Translations.English.DwarvenStorage.json";
        var englishLocalized = AssetUtils.LoadTextFromResources(resourceName);
        if (string.IsNullOrEmpty(englishLocalized))
        {
            Logger.LogError($"Failed to load English translation file: {resourceName}");
        }
        else
        {
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", englishLocalized);
        }
        
        // Create Prefabs
        PrefabManager.OnVanillaPrefabsAvailable += CreateInterface;
        PrefabManager.OnVanillaPrefabsAvailable += CreateStorageExtensions;
        PrefabManager.OnVanillaPrefabsAvailable += CreateUpgradeItems;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }
    
    private static void CreateInterface()
    {
        var config = new PieceConfig
        {
            Name = "$piece_dwarven_interface",
            Description = "$piece_dwarven_interface_desc",
            PieceTable = PieceTables.Hammer,
            CraftingStation = CraftingStations.Workbench,
            Category = PieceCategories.Misc,
        };

        config.AddRequirement("Wood", 1);

        var customPiece = new CustomPiece(
            AssetHolder.Bundle,
            "dwarven_interface",
            true,
            config
        );

        var piece = customPiece.PiecePrefab.GetComponent<Piece>();
        var woodWallPrefab = PrefabManager.Instance.GetPrefab("wood_wall_quarter");
        Piece woodWallPiece = null;
        if (woodWallPrefab != null)
        {
            woodWallPiece = woodWallPrefab.GetComponent<Piece>();
        }

        if (piece != null)
        {
            piece.m_clipEverything = false;
            if (woodWallPiece != null)
            {
                piece.m_placeEffect = woodWallPiece.m_placeEffect;
            }
        }

        var prefab = customPiece.PiecePrefab;

        prefab.name = "dwarven_interface";

        var zNetView = prefab.GetComponent<ZNetView>();
        if (zNetView == null)
        {
            Logger.LogError("dwarven_interface is missing ZNetView component.");
            return;
        }
        
        zNetView.m_persistent = true;
        zNetView.m_type = ZDO.ObjectType.Solid;
        zNetView.m_syncInitialScale = true;
        
        prefab.AddComponent<StorageInterface>();
        prefab.AddComponent<StorageUpgradeSlots>();

        PieceManager.Instance.AddPiece(customPiece);

        PrefabManager.OnVanillaPrefabsAvailable -= CreateInterface;
    }

    private static void CreateStorageExtensions()
    {
        foreach (var storage in StorageExtensions)
        {
            var pieceConfig = new PieceConfig()
            {
                Name = storage.Name,
                PieceTable = PieceTables.Hammer,
                Description = storage.Description,
                CraftingStation = storage.CraftingStation,
                Category = PieceCategories.Misc,
            };

            foreach (var requirement in storage.ItemRequirements)
            {
                pieceConfig.AddRequirement(requirement.Name, requirement.Amount);
            }
            
            var piece = new CustomPiece(
                AssetHolder.Bundle, 
                storage.PrefabName, 
                true,
                pieceConfig
                );
            
            var prefab = piece.PiecePrefab;
            var zNetView = prefab.GetComponent<ZNetView>();
            if (zNetView == null)
            {
                Logger.LogError("dwarven_interface is missing ZNetView component.");
                return;
            }
        
            zNetView.m_persistent = true;
            zNetView.m_type = ZDO.ObjectType.Solid;
            zNetView.m_syncInitialScale = true;
            
            var extension = piece.PiecePrefab.AddComponent<StorageInterfaceExtension>();
            extension.addedRows = storage.Rows;
            extension.addedColumns = 0;
            extension.hoverText = storage.Name;
            extension.hoverName = storage.Name;
            
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
                Name = item.Name,
                Description = item.Description,
                CraftingStation = item.CraftingStation,
                StackSize = 1,
                Weight = 2f,
                MinStationLevel = item.MinQualityLevel,
                Icon = AssetHolder.GetSprite(item.SpriteName)
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
