using System.Collections.Generic;
using Jotunn.Utils;
using UnityEngine;

namespace DwarvenStorage;

public static class AssetHolder
{
    private static AssetBundle _bundle;
    public static AssetBundle Bundle => _bundle;

    private static readonly Dictionary<string, GameObject> Prefabs = new();
    private static readonly Dictionary<string, Sprite> Sprites = new();

    private static readonly Dictionary<string, string> PrefabNames = new()
    {
        { "Interface", "assets/dwarvenstorage/prefabs/dwarven_interface.prefab" },
        { "Extension_Bronze", "assets/dwarvenstorage/prefabs/extension_bronze.prefab" },
        { "Extension_Iron", "assets/dwarvenstorage/prefabs/extension_iron.prefab" },
        { "Extension_Black_Metal", "assets/dwarvenstorage/prefabs/extension_black_metal.prefab" },
        { "Extension_Flametal", "assets/dwarvenstorage/prefabs/extension_flametal.prefab" },
    };

    private static readonly Dictionary<string, string> SpriteNames = new()
    {
        { "Interface", "assets/dwarvenstorage/models/icons/interface_icon.png" },
        
        { "Workbench_1", "assets/dwarvenstorage/models/icons/items/workbench_upgrade_1.png" },
        { "Workbench_2", "assets/dwarvenstorage/models/icons/items/workbench_upgrade_2.png" },
        { "Workbench_3", "assets/dwarvenstorage/models/icons/items/workbench_upgrade_3.png" },
        { "Workbench_4", "assets/dwarvenstorage/models/icons/items/workbench_upgrade_4.png" },
        { "Workbench_5", "assets/dwarvenstorage/models/icons/items/workbench_upgrade_5.png" },

        { "Forge_1", "assets/dwarvenstorage/models/icons/items/forge_upgrade_1.png" },
        { "Forge_2", "assets/dwarvenstorage/models/icons/items/forge_upgrade_2.png" },
        { "Forge_3", "assets/dwarvenstorage/models/icons/items/forge_upgrade_3.png" },
        { "Forge_4", "assets/dwarvenstorage/models/icons/items/forge_upgrade_4.png" },
        { "Forge_5", "assets/dwarvenstorage/models/icons/items/forge_upgrade_5.png" },
        { "Forge_6", "assets/dwarvenstorage/models/icons/items/forge_upgrade_6.png" },
        { "Forge_7", "assets/dwarvenstorage/models/icons/items/forge_upgrade_7.png" },

        { "Cauldron_1", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_1.png" },
        { "Cauldron_2", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_2.png" },
        { "Cauldron_3", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_3.png" },
        { "Cauldron_4", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_4.png" },
        { "Cauldron_5", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_5.png" },
        { "Cauldron_6", "assets/dwarvenstorage/models/icons/items/cauldron_upgrade_6.png" },

        { "Black_forge_1", "assets/dwarvenstorage/models/icons/items/black_forge_upgrade_1.png" },
        { "Black_forge_2", "assets/dwarvenstorage/models/icons/items/black_forge_upgrade_2.png" },
        { "Black_forge_3", "assets/dwarvenstorage/models/icons/items/black_forge_upgrade_3.png" },
        { "Black_forge_4", "assets/dwarvenstorage/models/icons/items/black_forge_upgrade_4.png" },
        { "Black_forge_5", "assets/dwarvenstorage/models/icons/items/black_forge_upgrade_5.png" },

        { "Artisan_1", "assets/dwarvenstorage/models/icons/items/artisan_upgrade_1.png" },
        { "Artisan_2", "assets/dwarvenstorage/models/icons/items/artisan_upgrade_2.png" },

        { "Stonecutter_1", "assets/dwarvenstorage/models/icons/items/stonecutter_upgrade_1.png" },

        { "Prep_table_1", "assets/dwarvenstorage/models/icons/items/prep_table_upgrade_1.png" },

        { "Mage_table_1", "assets/dwarvenstorage/models/icons/items/mage_table_upgrade_1.png" },
        { "Mage_table_2", "assets/dwarvenstorage/models/icons/items/mage_table_upgrade_2.png" },
        { "Mage_table_3", "assets/dwarvenstorage/models/icons/items/mage_table_upgrade_3.png" },
        { "Mage_table_4", "assets/dwarvenstorage/models/icons/items/mage_table_upgrade_4.png" },
    };

    private static bool _loaded;

    /// <summary>
    /// Get a prefab from the bundle
    /// </summary>
    /// <param name="key">prefab key</param>
    /// <returns>Prefab</returns>
    public static GameObject GetPrefab(string key)
    {
        if (!_loaded) return null;

        return Prefabs.TryGetValue(key, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Get a sprite loaded from the bundle
    /// </summary>
    /// <param name="key">sprite key</param>
    /// <returns>A sprite</returns>
    public static Sprite GetSprite(string key)
    {
        if (!_loaded) return null;

        return Sprites.TryGetValue(key, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// Load all the assets from the bundle
    /// </summary>
    public static void LoadAssetBundle()
    {
        if (_loaded) return;

        _bundle = AssetUtils.LoadAssetBundleFromResources($"{Plugin.ModName}.Assets.Bundles.dwarvenstorage");

        if (_bundle == null)
        {
            Plugin.Logger.LogError("Failed to load asset bundle");
            return;
        }

        // Load sprites from the bundle
        foreach (var spriteNamePair in SpriteNames)
        {
            var key = spriteNamePair.Key;
            var path = spriteNamePair.Value;

            var sprite = _bundle.LoadAsset<Sprite>(path);
            if (sprite == null)
            {
                Plugin.Logger.LogError($"Failed to load sprite: key={key}, path={path}");
                continue;
            }
            
            Plugin.Logger.LogInfo($"Loaded sprite: {key} - {path}");

            Sprites[key] = sprite;
        }

        // Load the prefabs from the bundle
        foreach (var prefabNamePair in PrefabNames)
        {
            var key = prefabNamePair.Key;
            var path = prefabNamePair.Value;

            var prefab = _bundle.LoadAsset<GameObject>(path);
            if (prefab == null)
            {
                Plugin.Logger.LogError($"Failed to load prefab: key={key}, path={path}");
                continue;
            }

            Prefabs[key] = prefab;
        }

        Plugin.Logger.LogInfo($"Loaded prefabs: {Prefabs.Count}");
        Plugin.Logger.LogInfo($"Loaded sprites: {Sprites.Count}");

        _loaded = true;
    }
}