using System.Collections.Generic;
using Jotunn.Utils;
using UnityEngine;

namespace DwarvenStorage;

public static class AssetHolder
{
    private static AssetBundle _bundle;
    public static AssetBundle Bundle => _bundle;

    private static readonly Dictionary<string, GameObject> Prefabs = new();

    private static readonly Dictionary<string, string> PrefabNames = new()
    {
        { "Interface", "assets/dwarvenstorage/prefabs/dwarven_interface.prefab" },
        { "Extension_Bronze", "assets/dwarvenstorage/prefabs/extension_bronze.prefab" },
        { "Extension_Iron", "assets/dwarvenstorage/prefabs/extension_iron.prefab" },
        { "Extension_Black_Metal", "assets/dwarvenstorage/prefabs/extension_black_metal.prefab" },
        { "Extension_Flametal", "assets/dwarvenstorage/prefabs/extension_flametal.prefab" }
    };

    private static bool _loaded;

    public static GameObject GetPrefab(string key)
    {
        if (!_loaded) return null;
        
        return Prefabs.TryGetValue(key, out var prefab) ? prefab : null;
    }

    public static void LoadAssetBundle()
    {
        if (_loaded) return;
        
        _bundle = AssetUtils.LoadAssetBundleFromResources($"{Plugin.ModName}.Assets.Bundles.dwarvenstorage");
        Plugin.Logger.LogInfo("Loaded asset bundle");

        if (_bundle == null)
        {
            Plugin.Logger.LogError("Failed to load asset bundle");
            return;
        }
        
        foreach (var asset in _bundle.GetAllAssetNames())
        {
            Plugin.Logger.LogInfo($"Loaded asset: {asset}");
        }

        foreach (var prefabNamesValue in PrefabNames.Values)
        {
            var prefab = _bundle.LoadAsset<GameObject>(prefabNamesValue);
            if (prefab == null)
            {
                Plugin.Logger.LogError($"Failed to load prefab: {prefabNamesValue}");
                continue;
            }
            
            Prefabs.Add(prefabNamesValue, prefab);
        }
        
        Plugin.Logger.LogInfo($"Loaded prefabs: {Prefabs.Count}");
        
        _loaded = true;
    }

    public static void PrintAssetNames()
    {
        if (!_loaded)
        {
            Plugin.Logger.LogInfo("Asset bundle not loaded");
            return;
        }

        if (_bundle == null)
        {
            Plugin.Logger.LogInfo("Asset bundle is null");
            return;
        }
        
        Plugin.Logger.LogInfo("printing asset bundle");
        
        foreach (var asset in _bundle.GetAllAssetNames())
        {
            Plugin.Logger.LogInfo($"Loaded asset: {asset}");
        }
    }
}