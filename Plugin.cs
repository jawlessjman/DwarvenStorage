using System.Collections.Generic;
using System.IO;
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
            SpriteName = "Prep_table_1",
            Description = "$prep_table_upgrade_desc", StationKey = "preptable", MinQualityLevel = 1
        },
        
        // Blackforge
        new() {Name="$blackforge_upgrade_1", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
                [
                    new ItemRequirements { Name = "BlackMarble", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "YggdrasilWood", Amount = 5, UpgradeAmount = 0 },
                    new ItemRequirements { Name = "BlackCore", Amount = 2, UpgradeAmount = 0 },
                ], 
            SpriteName = "Black_forge_1",
            Description = "$blackforge_upgrade_1_desc", StationKey = "blackforge", MinQualityLevel = 1
        },
        new() {Name="$blackforge_upgrade_2", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_1", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "BlackMarble", Amount = 2, UpgradeAmount = 0 },
            ], 
            SpriteName = "Black_forge_2",
            Description = "$blackforge_upgrade_2_desc", StationKey = "blackforge", MinQualityLevel = 2
        },
        new() {Name="$blackforge_upgrade_3", CraftingStation = CraftingStations.BlackForge, ItemRequirements =
            [
                new ItemRequirements { Name = "$blackforge_upgrade_2", Amount = 5, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Iron", Amount = 2, UpgradeAmount = 0 },
                new ItemRequirements { Name = "Copper", Amount = 4, UpgradeAmount = 0 },
                new ItemRequirements { Name = "MechanicalSpring", Amount = 1, UpgradeAmount = 0 },
            ], 
            SpriteName = "Black_forge_3",
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
            SpriteName = "Black_forge_4",
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
            SpriteName = "Black_forge_5",
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
        
        // Load Translations
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
        
        // Load translations for other languages
        LoadTranslations();
        
        // Create Prefabs
        PrefabManager.OnVanillaPrefabsAvailable += CreateInterface;
        PrefabManager.OnVanillaPrefabsAvailable += CreateStorageExtensions;
        PrefabManager.OnVanillaPrefabsAvailable += CreateUpgradeItems;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }
    
    /// <summary>
    /// Loads translations from the Translations folder.
    /// </summary>
    private void LoadTranslations()
    {
        var root = Path.Combine(
            Path.GetDirectoryName(Info.Location)!,
            "Translations"
        );

        if (!Directory.Exists(root))
        {
            return;
        }
        
        foreach (var file in Directory.GetFiles(root, "*.json", SearchOption.AllDirectories))
        {
            Logger.LogInfo($"Loading translation file: {file}");
            LocalizationManager.Instance.GetLocalization().AddFileByPath(file, isJson:true);
        }
    }

    /// <summary>
    /// Creates a custom piece
    /// </summary>
    /// <param name="name">translation key for the name of the piece</param>
    /// <param name="description">translation key for the description of the piece</param>
    /// <param name="craftingStation">Crafting station to use</param>
    /// <param name="category">Category for the storage system</param>
    /// <param name="itemRequirements">Item requirements</param>
    /// <param name="prefabName">Name of the prefab from the asset bundle</param>
    /// <returns></returns>
    private static CustomPiece CreatePiece(string name, string description, string craftingStation, string category, List<ItemRequirements> itemRequirements, string prefabName)
    {
        // Create the config
        var config = new PieceConfig
        {
            Name = name,
            Description = description,
            PieceTable = PieceTables.Hammer,
            CraftingStation = craftingStation,
            Category = category,
        };

        // Add the requirements
        foreach (var requirement in itemRequirements)
        {
            config.AddRequirement(requirement.Name, requirement.Amount);
        }
        
        // Create the prefab
        var customPiece = new CustomPiece(
            AssetHolder.Bundle,
            prefabName,
            true,
            config
        );
        
        var prefab = customPiece.PiecePrefab;
        
        // Get the piece and WearNTear components
        var piece = prefab.GetComponent<Piece>();
        var pieceTear = prefab.GetComponent<WearNTear>();
        var woodWallPrefab = PrefabManager.Instance.GetPrefab("wood_wall_quarter");
        Piece woodWallPiece = null;
        WearNTear wearNTear = null;
        if (woodWallPrefab != null)
        {
            woodWallPiece = woodWallPrefab.GetComponent<Piece>();
            wearNTear = woodWallPrefab.GetComponent<WearNTear>();
        }

        // Apply the place effect
        if (piece != null)
        {
            piece.m_clipEverything = false;
            if (woodWallPiece != null)
            {
                piece.m_placeEffect = woodWallPiece.m_placeEffect;
            }
        }

        // Apply the destroy effects
        if (pieceTear != null)
        {
            if (wearNTear != null)
            {
                pieceTear.m_destroyedEffect = wearNTear.m_destroyedEffect;
                pieceTear.m_hitEffect = wearNTear.m_hitEffect;
            }
        }
        
        // Apply ZNet Logic
        var zNetView = prefab.GetComponent<ZNetView>();
        if (zNetView == null) return customPiece;
        zNetView.m_persistent = true;
        zNetView.m_type = ZDO.ObjectType.Solid;
        zNetView.m_syncInitialScale = true;

        return customPiece;
    }
    
    /// <summary>
    /// Create the main storage interface
    /// </summary>
    private static void CreateInterface()
    {
        // Use helper method to create the prefab
        var prefab = CreatePiece("$piece_dwarven_interface", "$piece_dwarven_interface_desc", CraftingStations.Workbench, PieceCategories.Misc,
            [new ItemRequirements {Name="Wood", Amount = 10}, new ItemRequirements {Name="Stone", Amount = 4}, new ItemRequirements {Name="SurtlingCore", Amount = 2}], "dwarven_interface");
        
        // Add the interface components
        prefab.PiecePrefab.AddComponent<StorageInterface>();
        prefab.PiecePrefab.AddComponent<StorageUpgradeSlots>();

        PieceManager.Instance.AddPiece(prefab);

        PrefabManager.OnVanillaPrefabsAvailable -= CreateInterface;
    }

    /// <summary>
    /// Create the storage extensions
    /// </summary>
    private static void CreateStorageExtensions()
    {
        // Create the storage extensions
        foreach (var storage in StorageExtensions)
        {
            var piece = CreatePiece(storage.Name, storage.Description, storage.CraftingStation, PieceCategories.Misc, storage.ItemRequirements, storage.PrefabName);
            
            // Add and set the interface extension component
            var extension = piece.PiecePrefab.AddComponent<StorageInterfaceExtension>();
            extension.addedRows = storage.Rows;
            extension.addedColumns = 0;
            extension.hoverText = storage.Name;
            extension.hoverName = storage.Name;
            
            PieceManager.Instance.AddPiece(piece);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateStorageExtensions;
    }

    /// <summary>
    /// Create the upgrade items
    /// </summary>
    private static void CreateUpgradeItems()
    {
        // Create the upgrade items
        foreach (var item in StorageUpgrades)
        {
            // Create the item
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

            // Add the requirements
            foreach (var requirement in item.ItemRequirements)
            {
                itemConfig.AddRequirement(requirement.Name, requirement.Amount, requirement.UpgradeAmount);
            }
            
            // Create the item
            var customItem = new CustomItem(item.Name, "AskHide", itemConfig);
            
            // Add the custom data
            customItem.ItemDrop.m_itemData.m_quality = item.MinQualityLevel;
            customItem.ItemDrop.m_itemData.m_customData["DwarvenUpgrade"] = item.StationKey;
            
            // Save the keys to be used by the interface for when the m_customData resets
            UpgradeStationsByPrefabName[item.Name] = item.StationKey;
            UpgradeAmountsByPrefabName[item.Name] = item.MinQualityLevel;
            
            ItemManager.Instance.AddItem(customItem);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateUpgradeItems;
    }
}
