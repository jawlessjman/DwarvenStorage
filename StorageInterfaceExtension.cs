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

    private bool _isBeingUsed;

    public bool IsInRange(StorageInterface storageInterface)
    {
        if (storageInterface == null) return false;

        _isBeingUsed = Vector3.Distance(transform.position, storageInterface.transform.position) < range;
        
        return _isBeingUsed;
    }

    public string GetHoverText()
    {
        return hoverText + " " + (_isBeingUsed ? " ( in use ) " : " ( not in use ) ");
    }

    public string GetHoverName()
    {
        return hoverName + " " + (_isBeingUsed ? " ( in use ) " : " ( not in use ) ");
    }
}