using System.Collections.Generic;
using Jotunn.Configs;

namespace DwarvenStorage;

public class StorageUpgrade
{
    public string Name; 
    public string Description;
    public string CraftingStation;
    public string StationKey;
    public List<ItemRequirements> ItemRequirements;
    public int MinQualityLevel;
}

public class ItemRequirements()
{
    public string Name;
    public int Amount;
    public int UpgradeAmount;
}