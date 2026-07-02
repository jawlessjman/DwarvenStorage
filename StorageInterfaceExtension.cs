using UnityEngine;

namespace DwarvenStorage;

public class StorageInterfaceExtension : MonoBehaviour
{
    public string ExtensionId;
    public int AddedRows;
    public int AddedColumns;
    public float range = 10f;

    public bool IsInRange(StorageInterface storageInterface)
    {
        if (storageInterface == null) return false;
        
        return Vector3.Distance(transform.position, storageInterface.transform.position) < range;
    }
}