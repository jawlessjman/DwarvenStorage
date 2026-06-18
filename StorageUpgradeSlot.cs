using System;
using System.Collections.Generic;
using UnityEngine;

namespace DwarvenStorage;

public class StorageUpgradeSlots : MonoBehaviour
{
    public event Action OnUpgradesChanged;

    private const string ZdoKey = "dwarven_upgrade_inventory";
    private const int Width = 1;
    private const int Height = 6;

    private ZNetView _nview;
    private Inventory _inventory;

    private void Awake()
    {
        _nview = GetComponent<ZNetView>();
        _inventory = new Inventory("Dwarven Upgrades", null, Width, Height);

        Load();
    }

    public Inventory GetInventory()
    {
        return _inventory;
    }

    public bool CanAcceptItem(ItemDrop.ItemData item)
    {
        if (item == null) return false;

        return item.m_dropPrefab != null &&
               item.m_dropPrefab.name.StartsWith("DwarvenUpgrade_");
    }

    public void InvokeChanged()
    {
        OnUpgradesChanged?.Invoke();
    }

    public bool AddItem(ItemDrop.ItemData item)
    {
        if (!CanAcceptItem(item)) return false;

        var added = _inventory.AddItem(item.Clone());

        if (!added) return false;

        Save();
        OnUpgradesChanged?.Invoke();

        return true;
    }

    public bool RemoveItem(ItemDrop.ItemData item)
    {
        if (item == null) return false;

        var removed = _inventory.RemoveItem(item);

        if (!removed) return false;

        Save();
        OnUpgradesChanged?.Invoke();

        return true;
    }

    public void Save()
    {
        if (_nview == null || !_nview.IsValid()) return;

        var pkg = new ZPackage();
        _inventory.Save(pkg);

        _nview.GetZDO().Set(ZdoKey, pkg.GetBase64());
    }

    private void Load()
    {
        if (_nview == null || !_nview.IsValid()) return;

        var data = _nview.GetZDO().GetString(ZdoKey, "");
        if (string.IsNullOrEmpty(data)) return;

        var pkg = new ZPackage(data);
        _inventory.Load(pkg);
    }
}