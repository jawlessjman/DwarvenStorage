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
    private const int DefaultColumns = 8;
    private const int MaxRows = 75;
    private const int MaxColumns = 8;
    private const int UpgradeSlotCount = 8;

    private const float ExtensionScanInterval = 2f;

    private bool _hasBeenOpened;
    private bool _suppressDropdownCallback;

    private ZNetView _zNetView;

    private float _extensionScanTimer;

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

    private GameObject _panel;
    private GameObject _stationDropdown;

    private Container _container;
    private CraftingStation _craftingStation;
    private StorageUpgradeSlots _upgradeSlots;
    private bool _hasDroppedUpgradeItems;
    

    public string CurrentStationName => _currentStationName;
    public int CurrentStationLevel => GetStationLevel(_currentStationName);
    public CraftingStation CraftingStation => _craftingStation;
    public Container Container => _container;
    
    private WearNTear _wearNTearComponent;

    private Dropdown StationDropdownObject => _stationDropdown != null
        ? _stationDropdown.GetComponent<Dropdown>()
        : null;

    private void Awake()
    {
        // Add the container component to the game object
        _container = gameObject.AddComponent<Container>();
        _container.m_name = "$piece_dwarven_interface";
        _container.name = "$piece_dwarven_interface";
        _container.m_width = MaxColumns;
        _container.m_height = MaxRows;

        // AddComponent runs Container.Awake before we can configure its size.
        // Update the existing inventory too. Keep room for saved extension slots
        // during loading; OpenInterface applies the actual extension capacity.
        if (_container.GetInventory() != null)
        {
            InventoryAccess.SetSize(_container.GetInventory(), MaxColumns, MaxRows);
        }

        _zNetView = GetComponent<ZNetView>();

        // Create the crafting station component
        _craftingStation = gameObject.AddComponent<CraftingStation>();
        _craftingStation.m_name = "$piece_workbench";
        _craftingStation.m_rangeBuild = 0f;
        _craftingStation.m_discoverRange = 30f;
        _craftingStation.m_useDistance = 30f;
        _craftingStation.m_craftRequireRoof = false;

        _craftingStation.m_icon = AssetHolder.GetSprite("Interface");
        
        // Set the sound effects for the crafting station
        var workbenchPrefab = PrefabManager.Instance.GetPrefab("piece_workbench")?.GetComponent<CraftingStation>();
        if (workbenchPrefab != null)
        {
            _craftingStation.m_craftItemDoneEffects = workbenchPrefab.m_craftItemDoneEffects;
            _craftingStation.m_craftItemEffects = workbenchPrefab.m_craftItemEffects;
            _craftingStation.m_repairItemDoneEffects = workbenchPrefab.m_repairItemDoneEffects;
        }

        _upgradeSlots = GetComponent<StorageUpgradeSlots>();
        
        // Add a method for when the storage interface is destroyed
        _wearNTearComponent = gameObject.GetComponent<WearNTear>();
        if (_wearNTearComponent != null)
        {
            _wearNTearComponent.m_onDestroyed += OnDestruction;
        }

        // Add an event for when the upgrades change
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

    /// <summary>
    /// This method is called when the object is destroyed by its health reaching 0.
    /// </summary>
    private void OnDestruction()
    {
        if (_hasDroppedUpgradeItems) return;
        _hasDroppedUpgradeItems = true;

        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
        {
            return;
        }

        // Which items to drop (the upgrades) items drop on their own
        var itemsToDrop = inventory.GetAllItems()
            .Where(item => item != null)
            .ToList();

        foreach (var item in itemsToDrop)
        {
            var dropItem = item.Clone();
            dropItem.m_stack = item.m_stack;

            inventory.RemoveItem(item);

            var dropPosition =
                transform.position +
                transform.forward * 1.2f +
                Vector3.up * 0.75f;

            ItemDrop.DropItem(
                dropItem,
                dropItem.m_stack,
                dropPosition,
                Quaternion.identity
            );

            Plugin.Logger.LogInfo($"Dropped upgrade item: {dropItem.m_shared.m_name} x{dropItem.m_stack}");
        }

        _upgradeSlots.Save();
        _upgradeSlots.InvokeChanged();
    }

    /// <summary>
    /// When the object is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (_upgradeSlots != null)
        {
            _upgradeSlots.OnUpgradesChanged -= OnUpgradesChanged;
        }
        
        if (_wearNTearComponent != null)
        {
            _wearNTearComponent.m_onDestroyed -= OnDestruction;
        }

        // Make it so that the extensions stop recognizing this interface
        foreach (var extension in FindObjectsByType<StorageInterfaceExtension>(FindObjectsSortMode.None))
        {
            extension.ReleaseOwner(this);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// When interacted with by the player
    /// </summary>
    /// <param name="user">Player who interacted</param>
    /// <param name="hold">I think this for if they are holding the interacted key</param>
    /// <param name="alt"></param>
    /// <returns>If they interacted</returns>
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

    /// <summary>
    /// This is a part of the interface, but it's not used for anything'
    /// </summary>
    /// <param name="user"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <summary>
    /// Update the storage size based on the extensions
    /// </summary>
    private void Update()
    {
        if (!_hasBeenOpened) return;
        if (!_panel || !_panel.activeSelf) return;

        _extensionScanTimer += Time.deltaTime;

        if (_extensionScanTimer < ExtensionScanInterval) return;

        _extensionScanTimer = 0f;
        RebuildStorageSizeFromExtensions();
    }

    /// <summary>
    /// Fixed update for checking if the player is still in the inventory
    /// </summary>
    private void FixedUpdate()
    {
        if (!_panel || !_panel.activeSelf) return;
        if (InventoryGui.instance && InventoryGui.instance.IsContainerOpen()) return;

        CloseInterface();
    }

    /// <summary>
    /// Opens the interface
    /// </summary>
    private void OpenInterface()
    {
        if (GUIManager.Instance == null) return;
        if (InventoryGui.instance == null) return;

        _hasBeenOpened = true;
        Instance = this;

        var createdPanel = _panel == null;
        if (createdPanel)
        {
            CreatePanel();
            _panel.SetActive(false);
        }
        
        RebuildStationUpgrades();
        EnsureValidCurrentStation();
        
        // Rebuild the container size
        RebuildStorageSizeFromExtensions();

        // Refresh crafting stations and upgrades
        RefreshStationDropdown();
        RefreshUpgradeSlots();

        // Show the container for the interface
        InventoryGui.instance.Show(_container);

        _panel.SetActive(true);
        if (createdPanel) CenterPanel();

        ApplyCurrentCraftingStation();
    }

    /// <summary>
    /// Closes the interface
    /// </summary>
    private void CloseInterface()
    {
        if (_panel)
        {
            _panel.SetActive(false);
        }

        if (InventoryGui.instance)
        {
            InventoryGui.instance.Hide();
        }

        // Reset the crafting station in the inventory so it does not still show the crafting station from the interface
        ResetPlayerCrafting();
    }

    /// <summary>
    /// Rebuild the storage size from the extensions around it
    /// </summary>
    private void RebuildStorageSizeFromExtensions()
    {
        var targetRows = DefaultRows;
        var targetColumns = DefaultColumns;

        var extensionCount = 0;
        var addedRows = 0;

        // Get the extensions
        var extensions = FindObjectsByType<StorageInterfaceExtension>(FindObjectsSortMode.None);

        foreach (var extension in extensions)
        {
            if (!extension) continue;

            extension.RefreshOwner(true);

            if (!extension.IsOwnedBy(this)) continue;

            extensionCount++;

            addedRows += extension.addedRows;

            targetRows += extension.addedRows;
            targetColumns += extension.addedColumns;
        }

        targetRows = Mathf.Clamp(targetRows, DefaultRows, MaxRows);
        targetColumns = Mathf.Clamp(targetColumns, DefaultColumns, MaxColumns);

        // Update the text on the interface
        UpdateExtensionSummaryText(extensionCount, addedRows, targetRows, targetColumns);

        // Resize the storage to the new size
        ResizeStorage(targetRows, targetColumns);
    }
    
    /// <summary>
    /// Update the extension summary text
    /// </summary>
    /// <param name="extensionCount"></param>
    /// <param name="addedRows">rows to be added</param>
    /// <param name="totalRows"></param>
    /// <param name="totalColumns"></param>
    private void UpdateExtensionSummaryText(
        int extensionCount,
        int addedRows,
        int totalRows,
        int totalColumns)
    {
        if (!_extensionSummaryText) return;

        _extensionSummaryText.text =
            $"Extensions: {extensionCount}   Added Rows: +{addedRows}   Storage: {totalColumns}x{totalRows}";
    }

    /// <summary>
    /// Resize the storage to the new size
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    private void ResizeStorage(int rows, int columns)
    {
        if (!_container || _container.GetInventory() == null) return;
        var inventory = _container.GetInventory();
        if (inventory.GetWidth() == columns && inventory.GetHeight() == rows &&
            _container.m_width == columns && _container.m_height == rows) return;

        var isShrinking = inventory.GetWidth() > columns || inventory.GetHeight() > rows;

        // If the size is smaller than drop the items that were in those slots
        if (isShrinking)
        {
            var canDropOverflow = !_zNetView || !_zNetView.IsValid() || _zNetView.IsOwner();

            if (canDropOverflow)
            {
                DropItemsOutsideBounds(columns, rows);
            }
        }

        _container.m_width = columns;
        _container.m_height = rows;
        InventoryAccess.SetSize(inventory, columns, rows);

        MarkStorageChanged();

        if (InventoryGui.instance && InventoryGui.instance.IsContainerOpen())
        {
            InventoryGuiAccess.RefreshCrafting(InventoryGui.instance);
        }

        Plugin.Logger.LogInfo($"Storage resized to {columns}x{rows}");
    }

    /// <summary>
    /// Drop items from the columns and rows
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="rows"></param>
    private void DropItemsOutsideBounds(int columns, int rows)
    {
        var inventory = _container.GetInventory();
        if (inventory == null) return;

        var itemsToDrop = new List<ItemDrop.ItemData>();

        foreach (var item in inventory.GetAllItems())
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
            var dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.75f;

            var dropItem = item.Clone();
            dropItem.m_stack = item.m_stack;

            inventory.RemoveItem(item);

            ItemDrop.DropItem(dropItem, dropItem.m_stack, dropPosition, Quaternion.identity);

            Plugin.Logger.LogInfo($"Dropped item {item.m_shared.m_name} outside bounds");
        }
    }

    /// <summary>
    /// Save the inventory of the interface
    /// </summary>
    private void MarkStorageChanged()
    {
        if (!_container) return;

        try
        {
            var inventory = _container.GetInventory();
            if (inventory == null) return;

            // Container subscribes to this notification and saves on the owner.
            InventoryAccess.NotifyChanged(inventory);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning($"Failed to mark storage changed: {e.Message}");
        }
    }

    /// <summary>
    /// Rebuild the upgrades for the interface
    /// </summary>
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

    /// <summary>
    /// Rebuild the upgrades for the interface
    /// </summary>
    private void RebuildStationUpgrades()
    {
        foreach (var station in StationNames)
        {
            _stationLevels[station] = 0;
        }

        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        foreach (var item in inventory.GetAllItems())
        {
            if (!TryGetUpgradeInfo(item, out var stationName, out var stationLevel)) continue;
            if (!_stationLevels.ContainsKey(stationName)) continue;

            _stationLevels[stationName] = Mathf.Max(_stationLevels[stationName], stationLevel);
        }
    }

    /// <summary>
    /// Ensures that the current station is valid.
    /// </summary>
    private void EnsureValidCurrentStation()
    {
        if (HasStationUpgrade(_currentStationName)) return;

        _currentStationName = GetUnlockedStations().FirstOrDefault() ?? "None";
    }

    /// <summary>
    /// Get the unlocked stations
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> GetUnlockedStations()
    {
        return StationNames.Where(HasStationUpgrade);
    }

    /// <summary>
    /// Has an upgrade for a specific station
    /// </summary>
    /// <param name="stationName"></param>
    /// <returns></returns>
    private bool HasStationUpgrade(string stationName)
    {
        return !string.IsNullOrEmpty(stationName)
               && _stationLevels.TryGetValue(stationName, out var level)
               && level > 0;
    }

    /// <summary>
    /// Get the station level for a specific station
    /// </summary>
    /// <param name="stationName"></param>
    /// <returns></returns>
    private int GetStationLevel(string stationName)
    {
        return !string.IsNullOrEmpty(stationName)
               && _stationLevels.TryGetValue(stationName, out var level)
            ? level
            : 0;
    }

    /// <summary>
    /// Try to get the upgrade info for an item.
    /// </summary>
    /// <param name="item">Upgrade item</param>
    /// <param name="stationName">out station name</param>
    /// <param name="stationLevel">out station level</param>
    /// <returns></returns>
    private static bool TryGetUpgradeInfo(
        ItemDrop.ItemData item,
        out string stationName,
        out int stationLevel)
    {
        stationName = null;
        stationLevel = 0;

        if (item == null) return false;

        // If the m_customData dictionary contains the value
        if (item.m_customData.TryGetValue("DwarvenUpgrade", out var customStation))
        {
            stationName = customStation;
        }

        if (item.m_customData.TryGetValue("DwarvenUpgradeLevel", out var customLevel) &&
            int.TryParse(customLevel, out var parsedLevel))
        {
            stationLevel = parsedLevel;
        }

        // If the m_shared dictionary is not working, then use the table in the Plugin class
        if (!string.IsNullOrEmpty(stationName) && stationLevel > 0)
        {
            return true;
        }

        if (item.m_dropPrefab == null) return false;

        var prefabName = item.m_dropPrefab.name.Replace("(Clone)", "");

        if (string.IsNullOrEmpty(stationName) &&
            !Plugin.UpgradeStationsByPrefabName.TryGetValue(prefabName, out stationName))
        {
            return false;
        }

        if (stationLevel <= 0 &&
            !Plugin.UpgradeAmountsByPrefabName.TryGetValue(prefabName, out stationLevel))
        {
            stationLevel = 1;
        }

        return !string.IsNullOrEmpty(stationName) && stationLevel > 0;
    }

    /// <summary>
    /// Get the display name for a station.
    /// </summary>
    /// <param name="stationName">station name</param>
    /// <returns></returns>
    private static string GetStationDisplayName(string stationName)
    {
        var key = stationName switch
        {
            "workbench" => "$piece_workbench",
            "forge" => "$piece_forge",
            "blackforge" => "$piece_blackforge",
            "cauldron" => "$piece_cauldron",
            "stonecutter" => "$piece_stonecutter",
            "artisan" => "$piece_artisanstation",
            "galdr" => "$piece_magetable",
            "preptable" => "$piece_preptable",
            _ => stationName
        };

        return LocalizationManager.Instance.TryTranslate(key);
    }

    /// <summary>
    /// Create the main UI panel for the interface
    /// </summary>
    private void CreatePanel()
    {
        var containerTransform = InventoryGui.instance.m_container.transform;
        _panel = GUIManager.Instance.CreateWoodpanel(
            parent: containerTransform.parent,
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            position: Vector2.zero,
            width: 700,
            height: 900,
            draggable: true
        );

        _panel.transform.SetSiblingIndex(
            Mathf.Max(0, containerTransform.GetSiblingIndex() - 1)
        );

        // Create the UI elements
        CreateTitle();
        CreateExtensionSummaryText();
        CreateUpgradeSlots();
        CreateStationDropdown();
        CreateCloseButton();
    }

    private void CenterPanel()
    {
        // The container's parent need not fill the screen. Center against the
        // canvas after layout, retaining the sibling order and drag behavior.
        Canvas.ForceUpdateCanvases();
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        var canvas = _panel.GetComponentInParent<Canvas>();
        if (canvas && canvas.rootCanvas.transform is RectTransform canvasRect)
        {
            panelRect.position = canvasRect.TransformPoint(canvasRect.rect.center);
        }
    }

    // Variables for the UI
    private GameObject _extensionSummaryObject;
    private Text _extensionSummaryText;
    
    /// <summary>
    /// Creates the summary text for the interface, like the number of extensions and added rows.
    /// </summary>
    private void CreateExtensionSummaryText()
    {
        _extensionSummaryObject = GUIManager.Instance.CreateText(
            text: "Extensions: 0 | Added Rows: 0",
            parent: _panel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -85f),
            font: GUIManager.Instance.AveriaSerif,
            fontSize: 20,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 500f,
            height: 35f,
            addContentSizeFitter: false
        );

        _extensionSummaryText = _extensionSummaryObject.GetComponent<Text>();

        if (_extensionSummaryText != null)
        {
            _extensionSummaryText.alignment = TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// Create the title for the interface.
    /// </summary>
    private void CreateTitle()
    {
        GUIManager.Instance.CreateText(
            text: "Storage Interface",
            parent: _panel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(90f, -45f),
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

    /// <summary>
    /// Create the upgrade slots for the interface.
    /// </summary>
    private void CreateUpgradeSlots()
    {
        const float slotSize = 64f;
        const float slotSpacing = 76f;
        const float startX = -240f;
        const float startY = 320f;

        const float stationTextX = -110f;
        const float levelTextX = 15f;

        const float stationTextWidth = 170f;
        const float levelTextWidth = 100f;
        const float labelHeight = 45f;

        for (var i = 0; i < UpgradeSlotCount; i++)
        {
            var index = i;
            var y = startY - i * slotSpacing;

            var slotObject = GUIManager.Instance.CreateButton(
                text: "",
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(startX, y),
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

            // Station name
            var stationLabelObject = GUIManager.Instance.CreateText(
                text: "Empty",
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(stationTextX, y),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 18,
                color: Color.grey,
                outline: true,
                outlineColor: Color.black,
                width: stationTextWidth,
                height: labelHeight,
                addContentSizeFitter: false
            );

            var stationLabel = stationLabelObject.GetComponent<Text>();
            if (stationLabel != null)
            {
                stationLabel.alignment = TextAnchor.MiddleLeft;
            }

            // Station level
            var levelLabelObject = GUIManager.Instance.CreateText(
                text: "",
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(levelTextX, y),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 18,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: levelTextWidth,
                height: labelHeight,
                addContentSizeFitter: false
            );

            var levelLabel = levelLabelObject.GetComponent<Text>();
            if (levelLabel != null)
            {
                levelLabel.alignment = TextAnchor.MiddleLeft;
            }

            _upgradeSlotUis.Add(new UpgradeSlotUI
            {
                Icon = icon,
                StationLabel = stationLabel,
                LevelLabel = levelLabel
            });
        }
    }

    private void OnUpgradeSlotClicked(int index)
{
    if (_upgradeSlots == null) return;

    var upgradeInventory = _upgradeSlots.GetInventory();
    if (upgradeInventory == null) return;

    var inventoryGui = InventoryGui.instance;
    var dragItem = InventoryGuiAccess.GetDragItem(inventoryGui);
    var dragInventory = InventoryGuiAccess.GetDragInventory(inventoryGui);

    if (dragItem != null)
    {
        if (!TryGetUpgradeInfo(dragItem, out var stationName, out var stationLevel))
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Invalid upgrade item");
            return;
        }

        var existing = upgradeInventory.GetItemAt(0, index);
        if (existing != null)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Upgrade slot is occupied");
            return;
        }

        var clone = dragItem.Clone();
        clone.m_stack = 1;
        clone.m_gridPos = new Vector2i(0, index);
        clone.m_customData["DwarvenUpgrade"] = stationName;
        clone.m_customData["DwarvenUpgradeLevel"] = stationLevel.ToString();

        if (!upgradeInventory.AddItem(clone))
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Could not add upgrade item");
            return;
        }

        var removedFromSource = false;

        if (dragInventory != null)
        {
            removedFromSource = dragInventory.RemoveItem(dragItem, 1);
            
            if (removedFromSource && dragInventory == _container?.GetInventory())
            {
                MarkStorageChanged();
            }
        }

        if (!removedFromSource)
        {
            removedFromSource = Player.m_localPlayer != null &&
                                Player.m_localPlayer.GetInventory().RemoveItem(dragItem, 1);
        }

        if (!removedFromSource && _container?.GetInventory() != null)
        {
            removedFromSource = _container.GetInventory().RemoveItem(dragItem, 1);
            MarkStorageChanged();
        }

        if (!removedFromSource)
        {
            // Roll back the upgrade slot insert if we failed to remove the source item.
            upgradeInventory.RemoveItem(clone);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Could not move upgrade item");
            return;
        }

        InventoryGuiAccess.ClearDragItem(inventoryGui);

        _upgradeSlots.Save();
        _upgradeSlots.InvokeChanged();

        RefreshUpgradeSlots();
        return;
    }

    var item = upgradeInventory.GetItemAt(0, index);
    if (item == null) return;

    upgradeInventory.RemoveItem(item);

    var addedToPlayer = Player.m_localPlayer != null &&
                        Player.m_localPlayer.GetInventory().AddItem(item);

    if (!addedToPlayer)
    {
        var addedToStorage = _container?.GetInventory() != null &&
                             _container.GetInventory().AddItem(item);

        if (addedToStorage)
        {
            MarkStorageChanged();
        }
        else
        {
            ItemDrop.DropItem(
                item,
                item.m_stack,
                transform.position + transform.forward * 1.2f + Vector3.up * 0.75f,
                Quaternion.identity
            );
        }
    }

    _upgradeSlots.Save();
    _upgradeSlots.InvokeChanged();

    RefreshUpgradeSlots();
}

    /// <summary>
    /// Refresh the upgrade slots for the interface.
    /// </summary>
    private void RefreshUpgradeSlots()
    {
        if (_upgradeSlots == null) return;

        var inventory = _upgradeSlots.GetInventory();
        if (inventory == null) return;

        for (var i = 0; i < _upgradeSlotUis.Count; i++)
        {
            var item = inventory.GetItemAt(0, i);
            var ui = _upgradeSlotUis[i];

            if (item == null)
            {
                ui.Icon.sprite = null;
                ui.Icon.enabled = false;

                if (ui.StationLabel != null)
                {
                    ui.StationLabel.text = "Empty";
                    ui.StationLabel.color = Color.grey;
                }

                if (ui.LevelLabel != null)
                {
                    ui.LevelLabel.text = "";
                    ui.LevelLabel.color = Color.grey;
                }

                continue;
            }

            if (item.m_shared.m_icons is { Length: > 0 })
            {
                ui.Icon.sprite = item.m_shared.m_icons[0];
                ui.Icon.enabled = true;
            }
            else
            {
                ui.Icon.sprite = null;
                ui.Icon.enabled = false;
            }

            if (TryGetUpgradeInfo(item, out var stationName, out var stationLevel))
            {
                if (ui.StationLabel != null)
                {
                    ui.StationLabel.text = GetStationDisplayName(stationName);
                    ui.StationLabel.color = Color.white;
                }

                if (ui.LevelLabel == null) continue;
                ui.LevelLabel.text = $"Level {stationLevel}";
                ui.LevelLabel.color = Color.white;
            }
            else
            {
                if (ui.StationLabel != null)
                {
                    ui.StationLabel.text = "Unknown";
                    ui.StationLabel.color = Color.red;
                }

                if (ui.LevelLabel == null) continue;
                ui.LevelLabel.text = "";
                ui.LevelLabel.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Create the dropdown for the station selection.
    /// </summary>
    private void CreateStationDropdown()
    {
        _stationDropdown = GUIManager.Instance.CreateDropDown(
            parent: _panel.transform,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            position: new Vector2(-115f, -120f),
            fontSize: 16,
            width: 180f,
            height: 35f
        );

        var dropdown = StationDropdownObject;
        if (dropdown == null) return;

        RefreshStationDropdown();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    /// <summary>
    /// Refresh the station dropdown.
    /// </summary>
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

    /// <summary>
    /// Create the close button for the interface.
    /// </summary>
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

    /// <summary>
    /// When the dropdown value is changed, update the current station name.
    /// </summary>
    /// <param name="index">dropdown index</param>
    private void OnDropdownValueChanged(int index)
    {
        if (_suppressDropdownCallback) return;
        if (index < 0 || index >= _currentDropdownOptions.Count) return;

        SetCraftingStation(_currentDropdownOptions[index]);
    }

    /// <summary>
    /// Apply the current crafting station to the player.
    /// </summary>
    private void ApplyCurrentCraftingStation()
    {
        if (_currentStationName == "None")
        {
            ClearPlayerCraftingStation();
            return;
        }

        SetCraftingStation(_currentStationName);
    }

    /// <summary>
    /// Set the crafting station to use for the interface
    /// </summary>
    /// <param name="stationName">station name</param>
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
        InventoryGuiAccess.RefreshCrafting(InventoryGui.instance);
    }

    /// <summary>
    /// Clear the current crafting station from the player.
    /// </summary>
    private static void ClearPlayerCraftingStation()
    {
        Player.m_localPlayer?.SetCraftingStation(null);
        InventoryGuiAccess.RefreshCrafting(InventoryGui.instance);
    }

    /// <summary>
    /// Check if the player has the upgrade for a station.
    /// </summary>
    private static void ResetPlayerCrafting()
    {
        Instance = null;
        ClearPlayerCraftingStation();
    }

    /// <summary>
    /// Helper class for UI
    /// </summary>
    private class UpgradeSlotUI
    {
        public Image Icon;
        public Text StationLabel;
        public Text LevelLabel;
    }
}
