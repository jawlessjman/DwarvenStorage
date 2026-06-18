using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace DwarvenStorage;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public const string ModGuid = "jawlessjman.DwarvenStorage";
    public const string ModName = "Dwarven Storage";
    public const string ModVersion = "1.0.0";
    
    private Harmony _harmony;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        
        _harmony = new Harmony(ModGuid);
        _harmony.PatchAll();

        PrefabManager.OnVanillaPrefabsAvailable += CreateTestInterface;
        
        Logger.LogInfo($"Plugin {ModName}-{ModVersion} is loaded!");
    }

    private void CreateTestInterface()
    {
        var workbench = new PieceConfig
        {
            Name = "Test Interface",
            PieceTable = PieceTables.Hammer,
            Category = PieceCategories.Misc,
        };
        
        workbench.AddRequirement("Wood", 2);

        var piece = new CustomPiece("test_interface", "wood_wall_quarter", workbench);
        
        var interfaceComp = piece.PiecePrefab.AddComponent<StorageInterface>();
        var storageSlotsComp = piece.PiecePrefab.AddComponent<StorageUpgradeSlots>();

        if (piece.PiecePrefab.GetComponent<Piece>() is var pieceComp && pieceComp != null)
        {
            pieceComp.m_name = "Test Interface";
            pieceComp.m_description = "Open interface";
        }
        
        PieceManager.Instance.AddPiece(piece);
        
        PrefabManager.OnVanillaPrefabsAvailable -= CreateTestInterface;
    }
}
