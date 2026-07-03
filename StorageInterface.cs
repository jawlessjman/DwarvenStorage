using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace DwarvenStorage;

public class StorageInterface : MonoBehaviour, Interactable
{
    public static StorageInterface Instance { get; private set; }

    private const int DefaultRows = 5;
    private const int DefaultColumns = 10;
    private const int MaxRows = 75;
    private const int MaxColumns = 10;
    private const int UpgradeSlotCount = 6;
    
    private bool _hasBeenOpened;
    
    private ZNetView _zNetView;

    private const float ExtensionScanInterval = 2f;
    
    private float _extensionScanTimer;
    private int _currentRows = DefaultRows;
    private int _currentColumns = DefaultColumns;

    private static readonly string[] StationNames =
    [
        "workbench",
        "forge",
        "blackforge",
        "cauldron",
        "stonecutter",
        "artisan",
        "galdr",
        "preptable"
    ];

    private readonly Dictionary<string, int> _stationLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["workbench"] = 0,
        ["forge"] = 0,
        ["blackforge"] = 0,
        ["cauldron"] = 0,
        ["stonecutter"] = 0,
        ["artisan"] = 0,
        ["galdr"] = 0,
        ["preptable"] = 0,
    };

    private readonly List<UpgradeSlotUI> _upgradeSlotUis = new();
    private readonly List<string> _currentDropdownOptions = new();

    private string _currentStationName = "None";

    public string CurrentStationName => _currentStationName;
    public int CurrentStationLevel => GetStationLevel(_currentStationName);
    public CraftingStation CraftingStation => _craftingStation;

    private GameObject _panel;
    private GameObject _stationDropdown;

    private Container _container;
    public Container Container => _container;
    
    private CraftingStation _craftingStation;
    private StorageUpgradeSlots _upgradeSlots;

    private bool _suppressDropdownCallback;

    private Dropdown StationDropdownObject => _stationDropdown != null
        ? _stationDropdown.GetComponent<Dropdown>()
        : null;

    private void Awake()
    {
        _container = gameObject.AddComponent<Container>();
        _container.m_name = "Storage Interface";
        _container.name = "Storage Interface";
        _container.m_width = MaxColumns;
        _container.m_height = MaxRows;

        if (_container.m_inventory != null)
        {
            _container.m_inventory.m_name = "Storage Interface";
            _container.m_inventory.m_width = MaxColumns;
            _container.m_inventory.m_height = MaxRows;
        }
        
        _zNetView = GetComponent<ZNetView>();

        _craftingStation = gameObject.AddComponent<CraftingStation>();
        _craftingStation.m_name = "$piece_workbench";
        _craftingStation.m_rangeBuild = 0f;
        _craftingStation.m_buildRange = 0f;
        _craftingStation.m_discoverRange = 30f;
        _craftingStation.m_useDistance = 30f;
        _craftingStation.m_craftRequireRoof = false;

        _currentColumns = MaxColumns;
        _currentRows = MaxRows;

        _upgradeSlots = GetComponent<StorageUpgradeSlots>();

        if (_upgradeSlots != null)
        {
            _upgradeSlots.OnUpgradesChanged += OnUpgradesChanged;
            RebuildStationUpgrades();
            EnsureValidCurrentStation();
        }
        else
        {
            Plugin.Logger.LogWarning("StorageInterface is missing StorageUpgradeSlots.");
        }
    }

    private void OnDestroy()
    {
        if (_upgradeSlots != null)
        {
            _upgradeSlots.OnUpgradesChanged -= OnUpgradesChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RebuildStorageSizeFromExtensions()
    {
        var targetRows = DefaultRows;
        var targetColumns = DefaultColumns;

        var extensions = FindObjectsByType<StorageInterfaceExtension>(FindObjectsSortMode.None);

        foreach (var extension in extensions)
        {
            if (extension == null) continue;
            if (!extension.IsInRange(this)) continue;

            targetRows += extension.AddedRows;
            targetColumns += extension.AddedColumns;
        }
        
        targetRows = Mathf.Clamp(targetRows, DefaultRows, MaxRows);
        targetColumns = Mathf.Clamp(targetColumns, DefaultColumns, MaxColumns);
        
        ResizeStorage(targetRows, targetColumns);
    }

    private void ResizeStorage(int rows, int columns)
    {
        if (_container == null || _container.m_inventory == null) return;
        if (_currentColumns == columns && _currentRows == rows) return;
        
        var isShrinking = _currentColumns > columns || _currentRows > rows;

        if (isShrinking)
        {
            var canDropOverflow = _zNetView == null || !_zNetView.IsValid() || _zNetView.IsOwner();

            if (canDropOverflow)
            {
                DropItemsOutsideBounds(columns, rows);
            }
        }
        
        _currentRows = rows;
        _currentColumns = columns;
        
        _container.m_width = columns;
        _container.m_height = rows;
        
        _container.m_inventory.m_width = columns;
        _container.m_inventory.m_height = rows;
        
        MarkStorageChanged();

        if (InventoryGui.instance != null && InventoryGui.instance.IsContainerOpen())
        {
            InventoryGui.instance.SetupCrafting();
        }
        
        Plugin.Logger.LogInfo($"Storage resized to {columns}x{rows}");
    }

    private void DropItemsOutsideBounds(int columns, int rows)
    {
        var inventory = _container.m_inventory;
        if (inventory == null) return;
        
        var itemsToDrop = new List<ItemDrop.ItemData>();

        foreach (var item in inventory.m_inventory)
        {
            if (item == null) continue;
            
            var x = item.m_gridPos.x;
            var y = item.m_gridPos.y;

            if (x >= columns || y >= rows)
            {
                itemsToDrop.Add(item);
            }
        }

        foreach (var item in itemsToDrop)
        {
            inventory.RemoveItem(item);
            
            var dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.75f;
            
            ItemDrop.DropItem(item, item.m_stack, dropPosition, Quaternion.identity);
            
            Plugin.Logger.LogInfo($"Dropped item {item.m_shared.m_name} outside bounds");
        }
    }
    
    private void MarkStorageChanged()
    {
        if (_container == null) return;

        try
        {
            var saveMethod = typeof(Container).GetMethod(
                "Save",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            saveMethod?.Invoke(_container, []);

            var changedMethod = typeof(Inventory).GetMethod(
                "Changed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            changedMethod?.Invoke(_container.m_inventory, []);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Failed to mark storage changed: {e.Message}");
        }
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;

        if (Instance == this && _panel != null && _panel.activeSelf)
        {
            CloseInterface();
        }
        else
        {
            OpenInterface();
        }

        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Update()
    {
        if (!_hasBeenOpened) return;
        if (_panel == null || !_panel.activeSelf) return;
        
        _extensionScanTimer += Time.deltaTime;

        if (!(_extensionScanTimer >= ExtensionScanInterval)) return;
        _extensionScanTimer = 0f;
        RebuildStorageSizeFromExtensions();
    }

    private void FixedUpdate()
    {
        if (_panel == null || !_panel.activeSelf) return;
        if (InventoryGui.instance != null && InventoryGui.instance.IsContainerOpen()) return;

        CloseInterface();
    }

    private void OpenInterface()
    {
        if (GUIManager.Instance == null) return;
        if (InventoryGui.instance == null) return;

        _hasBeenOpened = true;

        Instance = this;
        
        RebuildStorageSizeFromExtensions();

        RebuildStationUpgrades();
        EnsureValidCurrentStation();

        if (_panel == null)
        {
            CreatePanel();
            _panel.SetActive(false);
        }

        RefreshStationDropdown();
        RefreshUpgradeSlots();

        InventoryGui.instance.Show(_container);

        _panel.SetActive(true);

        ApplyCurrentCraftingStation();
    }

    private void CloseInterface()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }

        if (InventoryGui.instance != null)
        {
            InventoryGui.instance.Hide();
        }

        ResetPlayerCrafting();
    }

    private void OnUpgradesChanged()
    {
        RebuildStationUpgrades();
        EnsureValidCurrentStation();

        RefreshStationDropdown();
        RefreshUpgradeSlots();

        if (Instance == this)
        {
            ApplyCurrentCraftingStation();
        }
    }

    private void RebuildStationUpgrades()
    {
        foreach (var station in StationNames)
        {
            _stationLevels[station] = 0;
        }

        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        foreach (var item in inventory.m_inventory)
        {
            if (!TryGetStationFromUpgradeItem(item, out var stationName)) continue;
            if (!_stationLevels.ContainsKey(stationName)) continue;
            if (item.m_dropPrefab == null) return;

            var prefabName = item.m_dropPrefab.name.Replace("(Clone)", "");

            var level = Plugin.UpgradeAmountsByPrefabName[prefabName];
            _stationLevels[stationName] = Mathf.Max(_stationLevels[stationName], level);
        }
    }

    private void EnsureValidCurrentStation()
    {
        if (HasStationUpgrade(_currentStationName)) return;

        _currentStationName = GetUnlockedStations().FirstOrDefault() ?? "None";
    }

    private IEnumerable<string> GetUnlockedStations()
    {
        return StationNames.Where(HasStationUpgrade);
    }

    private bool HasStationUpgrade(string stationName)
    {
        return !string.IsNullOrEmpty(stationName)
               && _stationLevels.TryGetValue(stationName, out var level)
               && level > 0;
    }

    private int GetStationLevel(string stationName)
    {
        return !string.IsNullOrEmpty(stationName)
               && _stationLevels.TryGetValue(stationName, out var level)
            ? level
            : 0;
    }

    private static bool TryGetStationFromUpgradeItem(ItemDrop.ItemData item, out string stationName)
    {
        stationName = null;

        if (item == null) return false;

        if (item.m_customData.TryGetValue("DwarvenUpgrade", out stationName))
        {
            return !string.IsNullOrEmpty(stationName);
        }

        if (item.m_dropPrefab == null) return false;

        var prefabName = item.m_dropPrefab.name.Replace("(Clone)", "");

        return Plugin.UpgradeStationsByPrefabName.TryGetValue(prefabName, out stationName)
               && !string.IsNullOrEmpty(stationName);
    }

    private void CreatePanel()
    {
        _panel = GUIManager.Instance.CreateWoodpanel(
            parent: InventoryGui.instance.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            position: Vector2.zero,
            width: 700,
            height: 900,
            draggable: true
        );

        var containerTransform = InventoryGui.instance.m_container.transform;

        _panel.transform.SetParent(containerTransform.parent, false);
        _panel.transform.SetSiblingIndex(
            Mathf.Max(0, containerTransform.GetSiblingIndex() - 1)
        );

        CreateTitle();
        CreateUpgradeSlots();
        CreateStationDropdown();
        CreateCloseButton();
    }

    private void CreateTitle()
    {
        GUIManager.Instance.CreateText(
            text: "Storage Interface",
            parent: _panel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -45f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 32,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 400f,
            height: 50f,
            addContentSizeFitter: false
        );
    }

    private void CreateUpgradeSlots()
    {
        const float slotSize = 64f;
        const float slotSpacing = 76f;
        const float startX = -295f;
        const float startY = 320f;

        for (var i = 0; i < UpgradeSlotCount; i++)
        {
            var index = i;

            var slotObject = GUIManager.Instance.CreateButton(
                text: "",
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(startX, startY - i * slotSpacing),
                width: slotSize,
                height: slotSize
            );

            var button = slotObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnUpgradeSlotClicked(index));
            }

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(7f, 7f);
            iconRect.offsetMax = new Vector2(-7f, -7f);

            var icon = iconObject.GetComponent<Image>();
            icon.enabled = false;
            icon.raycastTarget = false;

            _upgradeSlotUis.Add(new UpgradeSlotUI
            {
                Root = slotObject,
                Button = button,
                Icon = icon
            });
        }
    }

    private void OnUpgradeSlotClicked(int index)
    {
        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        var dragItem = InventoryGui.instance?.m_dragItem;

        if (dragItem != null)
        {
            if (!TryGetStationFromUpgradeItem(dragItem, out var stationName))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Invalid upgrade item");
                return;
            }

            var existing = inventory.GetItemAt(0, index);
            if (existing != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Upgrade slot is occupied");
                return;
            }

            var clone = dragItem.Clone();
            clone.m_stack = 1;
            clone.m_gridPos = new Vector2i(0, index);
            clone.m_customData["DwarvenUpgrade"] = stationName;

            inventory.AddItem(clone);

            Player.m_localPlayer.GetInventory().RemoveItem(dragItem, 1);
            InventoryGui.instance.SetupDragItem(null, null, 1);

            _upgradeSlots.Save();
            _upgradeSlots.InvokeChanged();

            RefreshUpgradeSlots();
            return;
        }

        var item = inventory.GetItemAt(0, index);
        if (item == null) return;

        inventory.RemoveItem(item);
        Player.m_localPlayer.GetInventory().AddItem(item);

        _upgradeSlots.Save();
        _upgradeSlots.InvokeChanged();

        RefreshUpgradeSlots();
    }

    private void RefreshUpgradeSlots()
    {
        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        for (var i = 0; i < _upgradeSlotUis.Count; i++)
        {
            var item = inventory.GetItemAt(0, i);
            var icon = _upgradeSlotUis[i].Icon;

            if (item == null || item.m_shared.m_icons == null || item.m_shared.m_icons.Length == 0)
            {
                icon.sprite = null;
                icon.enabled = false;
                continue;
            }

            icon.sprite = item.m_shared.m_icons[0];
            icon.enabled = true;
        }
    }

    private void CreateStationDropdown()
    {
        _stationDropdown = GUIManager.Instance.CreateDropDown(
            parent: _panel.transform,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            position: new Vector2(-130f, -90f),
            fontSize: 16,
            width: 180f,
            height: 35f
        );

        var dropdown = StationDropdownObject;
        if (dropdown == null) return;

        RefreshStationDropdown();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void RefreshStationDropdown()
    {
        var dropdown = StationDropdownObject;
        if (dropdown == null) return;

        _currentDropdownOptions.Clear();
        _currentDropdownOptions.AddRange(GetUnlockedStations());

        if (_currentDropdownOptions.Count == 0)
        {
            _currentDropdownOptions.Add("None");
        }

        if (!_currentDropdownOptions.Contains(_currentStationName))
        {
            _currentStationName = _currentDropdownOptions[0];
        }

        _suppressDropdownCallback = true;

        dropdown.ClearOptions();
        dropdown.AddOptions(_currentDropdownOptions);
        dropdown.value = Mathf.Max(0, _currentDropdownOptions.IndexOf(_currentStationName));
        dropdown.RefreshShownValue();

        _suppressDropdownCallback = false;
    }

    private void CreateCloseButton()
    {
        var buttonObject = GUIManager.Instance.CreateButton(
            text: "Close",
            parent: _panel.transform,
            anchorMin: new Vector2(0.5f, 0f),
            anchorMax: new Vector2(0.5f, 0f),
            position: new Vector2(0f, 55f),
            width: 180f,
            height: 50f
        );

        var button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CloseInterface);
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (_suppressDropdownCallback) return;
        if (index < 0 || index >= _currentDropdownOptions.Count) return;

        SetCraftingStation(_currentDropdownOptions[index]);
    }

    private void ApplyCurrentCraftingStation()
    {
        if (_currentStationName == "None")
        {
            ClearPlayerCraftingStation();
            return;
        }

        SetCraftingStation(_currentStationName);
    }

    private void SetCraftingStation(string stationName)
    {
        if (string.IsNullOrEmpty(stationName) || stationName == "None")
        {
            _currentStationName = "None";
            ClearPlayerCraftingStation();
            return;
        }

        if (!HasStationUpgrade(stationName))
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Missing station upgrade");
            EnsureValidCurrentStation();
            RefreshStationDropdown();
            ApplyCurrentCraftingStation();
            return;
        }

        _craftingStation.m_name = stationName switch
        {
            "forge" => "$piece_forge",
            "blackforge" => "$piece_blackforge",
            "cauldron" => "$piece_cauldron",
            "stonecutter" => "$piece_stonecutter",
            "artisan" => "$piece_artisanstation",
            "galdr" => "$piece_magetable",
            "preptable" => "$piece_preptable",
            _ => "$piece_workbench"
        };

        _currentStationName = stationName;

        Player.m_localPlayer?.SetCraftingStation(_craftingStation);
        InventoryGui.instance?.SetupCrafting();
    }

    private static void ClearPlayerCraftingStation()
    {
        Player.m_localPlayer?.SetCraftingStation(null);
        InventoryGui.instance?.SetupCrafting();
    }

    private static void ResetPlayerCrafting()
    {
        Instance = null;
        ClearPlayerCraftingStation();
    }

    private class UpgradeSlotUI
    {
        public GameObject Root;
        public Button Button;
        public Image Icon;
    }
}