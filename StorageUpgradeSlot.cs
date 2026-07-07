using System;
using UnityEngine;

namespace DwarvenStorage;

public class StorageUpgradeSlots : MonoBehaviour
{
    public event Action OnUpgradesChanged;

    private const string ZdoKey = "dwarven_upgrade_inventory";
    private const int Width = 1;
    private const int Height = 8;

    private ZNetView _nview;
    private Inventory _inventory;

    private void Awake()
    {
        _nview = GetComponent<ZNetView>();
        _inventory = new Inventory("Dwarven Upgrades", null, Width, Height);

        Load();
    }

    /// <summary>
    /// Get the inventory for the upgrade slots
    /// </summary>
    /// <returns></returns>
    public Inventory GetInventory()
    {
        return _inventory;
    }

    /// <summary>
    /// Is the item a storage upgrade?
    /// </summary>
    /// <param name="item">item</param>
    /// <returns>if the item can be placed there</returns>
    private static bool CanAcceptItem(ItemDrop.ItemData item)
    {
        if (item == null) return false;

        Plugin.Logger.LogInfo($"item: {item.m_shared.m_name}");

        foreach (var data in item.m_customData)
        {
            Plugin.Logger.LogInfo($"data: {data.Key} - {data.Value}");
        }

        if (item.m_customData.ContainsKey("DwarvenUpgrade"))
        {
            return true;
        }

        if (item.m_dropPrefab == null)
        {
            return false;
        }

        var prefabName = item.m_dropPrefab.name.Replace("(Clone)", "");

        Plugin.Logger.LogInfo($"prefabName: {prefabName}");

        return Plugin.UpgradeStationsByPrefabName.ContainsKey(prefabName);
    }

    /// <summary>
    /// Invoke the changed event
    /// </summary>
    public void InvokeChanged()
    {
        OnUpgradesChanged?.Invoke();
    }

    /// <summary>
    /// Add an item to the inventory
    /// </summary>
    /// <param name="item">item to add</param>
    /// <returns>if it was added</returns>
    public bool AddItem(ItemDrop.ItemData item)
    {
        if (!CanAcceptItem(item)) return false;

        var added = _inventory.AddItem(item.Clone());

        if (!added) return false;

        Save();
        InvokeChanged();

        return true;
    }

    /// <summary>
    /// Remove an item from the inventory
    /// </summary>
    /// <param name="item">item to remove</param>
    /// <returns>if the item was removed</returns>
    public bool RemoveItem(ItemDrop.ItemData item)
    {
        if (item == null) return false;

        var removed = _inventory.RemoveItem(item);

        if (!removed) return false;

        Save();
        InvokeChanged();

        return true;
    }

    /// <summary>
    /// Save the inventory to the ZDO
    /// </summary>
    public void Save()
    {
        if (_nview == null || !_nview.IsValid()) return;

        var pkg = new ZPackage();
        _inventory.Save(pkg);

        _nview.GetZDO().Set(ZdoKey, pkg.GetBase64());
    }

    /// <summary>
    /// Load the inventory from the ZDO
    /// </summary>
    private void Load()
    {
        if (_nview == null || !_nview.IsValid()) return;

        var data = _nview.GetZDO().GetString(ZdoKey);
        if (string.IsNullOrEmpty(data)) return;

        var pkg = new ZPackage(data);
        _inventory.Load(pkg);
    }
}