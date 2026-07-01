using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace DwarvenStorage;

public class StorageInterface : MonoBehaviour, Interactable
{
    public static StorageInterface Instance { get; private set; }
    
    private const int DefaultRows = 8;
    private const int DefaultColumns = 4;
    private const int UpgradeSlotCount = 6;

    private static readonly List<string> StationNames =
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

    private string _currentStationName = "workbench";
    
    public string CurrentStationName => _currentStationName;

    private GameObject _panel;
    private Container _container;
    private CraftingStation _craftingStation;
    public CraftingStation CraftingStation => _craftingStation;
    
    private StorageUpgradeSlots _upgradeSlots;

    private readonly List<UpgradeSlotUI> _upgradeSlotUis = [];

    private void Awake()
    {
        _container = gameObject.AddComponent<Container>();
        _container.m_name = "Storage Interface";
        _container.m_width = DefaultColumns;
        _container.m_height = DefaultRows;

        _craftingStation = gameObject.AddComponent<CraftingStation>();
        _craftingStation.m_name = "$piece_workbench";
        _craftingStation.m_rangeBuild = 0f;
        _craftingStation.m_buildRange = 0f;
        _craftingStation.m_discoverRange = 30f;
        _craftingStation.m_useDistance = 30f;
        _craftingStation.m_craftRequireRoof = false;

        _upgradeSlots = GetComponent<StorageUpgradeSlots>();
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;

        SetCraftingStation(_currentStationName);
        
        Instance = this;

        InventoryGui.instance.Show(_container, 1);
        TogglePanel();

        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void FixedUpdate()
    {
        if (_panel == null || !_panel.activeSelf) return;

        if (InventoryGui.instance.IsContainerOpen()) return;

        _panel.SetActive(false);
        ResetPlayerCrafting();
    }

    private void TogglePanel()
    {
        if (GUIManager.Instance == null) return;
        if (GUIManager.CustomGUIFront == null) return;

        if (_panel == null)
        {
            CreatePanel();
            _panel.SetActive(false);
        }

        var state = !_panel.activeSelf;
        _panel.SetActive(state);

        if (state)
        {
            RefreshUpgradeSlots();
        }
        else
        {
            InventoryGui.instance.Hide();
            ResetPlayerCrafting();
        }
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

        var craftingTransform = InventoryGui.instance.m_container.transform;

        _panel.transform.SetParent(craftingTransform.parent, false);
        _panel.transform.SetSiblingIndex(
            Mathf.Max(0, craftingTransform.GetSiblingIndex() - 1)
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
            button.onClick.AddListener(() => OnUpgradeSlotClicked(index));

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
        var dragItem = InventoryGui.instance.m_dragItem;

        if (dragItem != null)
        {
            // if (!_upgradeSlots.CanAcceptItem(dragItem))
            // {
            //     Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Invalid upgrade item");
            //     return;
            // }

            var existing = inventory.GetItemAt(0, index);
            if (existing != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Upgrade slot is occupied");
                return;
            }

            var clone = dragItem.Clone();
            clone.m_stack = 1;
            clone.m_gridPos = new Vector2i(0, index);

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
        var dropdownObject = GUIManager.Instance.CreateDropDown(
            parent: _panel.transform,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            position: new Vector2(-130f, -90f),
            fontSize: 16,
            width: 180f,
            height: 35f
        );

        var dropdown = dropdownObject.GetComponent<Dropdown>();
        if (dropdown == null) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(StationNames);
        dropdown.value = StationNames.IndexOf(_currentStationName);
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
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
            button.onClick.AddListener(TogglePanel);
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= StationNames.Count) return;

        SetCraftingStation(StationNames[index]);
    }

    private void SetCraftingStation(string stationName)
    {
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

        Player.m_localPlayer.SetCraftingStation(_craftingStation);
        InventoryGui.instance.SetupCrafting();

        _currentStationName = stationName;
    }

    private static void ResetPlayerCrafting()
    {
        if (Player.m_localPlayer == null) return;
        if (InventoryGui.instance == null) return;
        
        Instance = null;

        Player.m_localPlayer.SetCraftingStation(null);
        InventoryGui.instance.SetupCrafting();
    }

    private class UpgradeSlotUI
    {
        public GameObject Root;
        public Button Button;
        public Image Icon;
    }
}