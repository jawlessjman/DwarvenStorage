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
    
    public static List<string> UpgradeItemNames = [
        "Workbench_Upgrade",
        "Forge_Upgrade",
        "BlackForge_Upgrade",
        "GaldrTable_Upgrade",
        "Cauldron_Upgrade",
        "ArtisanTable_Upgrade"
    ];
    
    public static List<StorageUpgrade> StorageUpgrades = [
        new StorageUpgrade() {Name="Workbench_Upgrade", CraftingStation = CraftingStations.Workbench, ItemRequirements =
            [new ItemRequirements() { Name = "Wood", Amount = 1, UpgradeAmount = 2 }], Description = "Upgrade your workbench to hold more items."
        },
    ];
    
    private Harmony _harmony;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();

        PrefabManager.OnVanillaPrefabsAvailable += CreateTestInterface;
        PrefabManager.OnVanillaPrefabsAvailable += CreateUpgradeItems;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }

    private void Test()
    {
        var panel = GUIManager.Instance.CreateWoodpanel(
            parent: Hud.instance.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            position: Vector2.zero,
            width: 700,
            height: 900,
            draggable: true
        );
        
        panel.SetActive(true);
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
            customItem.ItemDrop.m_itemData.m_customData.Add("DwarvenUpgrade", item.CraftingStation);
            ItemManager.Instance.AddItem(customItem);
        }
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateUpgradeItems;
    }
}
