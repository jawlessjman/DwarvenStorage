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

    private bool IsOwned => _owningInterface != null;

    private void Awake()
    {
        _cachedHoverText = LocalizationManager.Instance.TryTranslate(hoverText);
        _cachedHoverName = LocalizationManager.Instance.TryTranslate(hoverName);
    }

    /// <summary>
    /// Is this extension owned by a specific storage interface?
    /// </summary>
    /// <param name="storageInterface">an instance of a storage interface</param>
    /// <returns>If it is owned by a specific interface</returns>
    public bool IsOwnedBy(StorageInterface storageInterface)
    {
        return storageInterface && _owningInterface == storageInterface;
    }

    /// <summary>
    /// Refresh the owner for the extension
    /// </summary>
    /// <param name="force">If the extension owner should be forced to change</param>
    public void RefreshOwner(bool force = false)
    {
        if (!force && Time.time < _nextOwnerRefreshTime)
        {
            return;
        }

        _nextOwnerRefreshTime = Time.time + OwnerRefreshInterval;
        
        if (_owningInterface && IsInRangeOf(_owningInterface))
        {
            return;
        }

        _owningInterface = null;

        var interfaces = FindObjectsByType<StorageInterface>(FindObjectsSortMode.None);

        _owningInterface = interfaces
            .Where(storageInterface => storageInterface)
            .Where(IsInRangeOf)
            .OrderBy(storageInterface =>
                Vector3.Distance(transform.position, storageInterface.transform.position)
            )
            .FirstOrDefault();
    }

    /// <summary>
    /// Check if the extension is in range of a storage interface.
    /// </summary>
    /// <param name="storageInterface">storage interface</param>
    /// <returns>If the interface is in range of the extension</returns>
    private bool IsInRangeOf(StorageInterface storageInterface)
    {
        if (!storageInterface) return false;

        return Vector3.Distance(
            transform.position,
            storageInterface.transform.position
        ) <= range;
    }

    /// <summary>
    /// Release the ownership of the extension.
    /// </summary>
    /// <param name="storageInterface"></param>
    public void ReleaseOwner(StorageInterface storageInterface)
    {
        if (_owningInterface == storageInterface)
        {
            _owningInterface = null;
        }
    }

    /// <summary>
    /// Get the hover text for the extension.
    /// </summary>
    /// <returns>hover text</returns>
    public string GetHoverText()
    {
        RefreshOwner();

        var status = IsOwned
            ? " <color=yellow>(in use)</color>"
            : " <color=grey>(not in use)</color>";

        return _cachedHoverText + status;
    }

    /// <summary>
    /// Get the hover name for the extension.
    /// </summary>
    /// <returns>hover name</returns>
    public string GetHoverName()
    {
        RefreshOwner();

        var status = IsOwned
            ? " <color=yellow>(in use)</color>"
            : " <color=grey>(not in use)</color>";

        return _cachedHoverName + status;
    }

    public float GetHoverOffset()
    {
        return 0.1f;
    }
}