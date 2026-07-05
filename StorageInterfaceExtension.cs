using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace DwarvenStorage;

public class StorageInterfaceExtension : MonoBehaviour, Hoverable
{
    public string extensionId;

    public int addedRows;
    public int addedColumns;

    public float range = 10f;

    public string hoverText;
    public string hoverName;

    private const float OwnerRefreshInterval = 1f;

    private StorageInterface _owningInterface;
    private float _nextOwnerRefreshTime;

    private string _cachedHoverText;
    private string _cachedHoverName;

    public bool IsOwned => _owningInterface != null;

    private void Awake()
    {
        _cachedHoverText = LocalizationManager.Instance.TryTranslate(hoverText);
        _cachedHoverName = LocalizationManager.Instance.TryTranslate(hoverName);
    }

    public bool IsOwnedBy(StorageInterface storageInterface)
    {
        return storageInterface != null && _owningInterface == storageInterface;
    }

    public void RefreshOwner(bool force = false)
    {
        if (!force && Time.time < _nextOwnerRefreshTime)
        {
            return;
        }

        _nextOwnerRefreshTime = Time.time + OwnerRefreshInterval;
        
        if (_owningInterface != null && IsInRangeOf(_owningInterface))
        {
            return;
        }

        _owningInterface = null;

        var interfaces = FindObjectsByType<StorageInterface>(FindObjectsSortMode.None);

        _owningInterface = interfaces
            .Where(storageInterface => storageInterface != null)
            .Where(IsInRangeOf)
            .OrderBy(storageInterface =>
                Vector3.Distance(transform.position, storageInterface.transform.position)
            )
            .FirstOrDefault();
    }

    public bool IsInRangeOf(StorageInterface storageInterface)
    {
        if (storageInterface == null) return false;

        return Vector3.Distance(
            transform.position,
            storageInterface.transform.position
        ) <= range;
    }

    public void ReleaseOwner(StorageInterface storageInterface)
    {
        if (_owningInterface == storageInterface)
        {
            _owningInterface = null;
        }
    }

    public string GetHoverText()
    {
        // Throttled, so this no longer scans every frame.
        RefreshOwner();

        var status = IsOwned
            ? " <color=yellow>(in use)</color>"
            : " <color=grey>(not in use)</color>";

        return _cachedHoverText + status;
    }

    public string GetHoverName()
    {
        // Throttled, so this no longer scans every frame.
        RefreshOwner();

        var status = IsOwned
            ? " <color=yellow>(in use)</color>"
            : " <color=grey>(not in use)</color>";

        return _cachedHoverName + status;
    }
}