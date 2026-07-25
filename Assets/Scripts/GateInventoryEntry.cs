// GateInventoryEntry.cs - Individual gate inventory entry
using UnityEngine;

[System.Serializable]
public class GateInventoryEntry
{
    public GateData gateData;
    public int ownedQuantity = 0;        // Total owned by player
    public int maxPerRun = 3;            // Max allowed per run
    public int currentLevel = 1;         // Current gate level
    public bool unlocked = false;        // Is this gate unlocked?
    
    [Header("Unlock Conditions")]
    public int unlockWave = 1;           // Wave required to unlock
    
    // Get the actual quantity to load in a run
    public int GetRunQuantity()
    {
        if (!unlocked || ownedQuantity <= 0)
            return 0;
            
        return Mathf.Min(ownedQuantity, maxPerRun);
    }
    
    // Get radius at current level
    public float GetRadius()
    {
        if (gateData == null) return 3f;
        return gateData.baseRadius + (gateData.radiusPerLevel * (currentLevel - 1));
    }
    
    // Get max targets at current level
    public int GetMaxTargets()
    {
        if (gateData == null) return 3;
        return gateData.baseMaxTargets + (gateData.maxTargetsPerLevel * (currentLevel - 1));
    }
}