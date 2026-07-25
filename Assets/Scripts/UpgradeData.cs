using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Quantum/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Upgrade Info")]
    public string upgradeName;
    public string description;
    public Sprite icon;
    
    [Header("Upgrade Stats")]
    public UIUpgradeType upgradeType;
    public int maxLevel = 10;
    public int baseCost = 100;
    public float costMultiplier = 1.5f; // Cost increases by this factor each level
    
    [Header("Effects")]
    public float baseEffectValue = 10f; // Base improvement per level
    public float effectMultiplier = 1.2f; // Effect scaling per level
    
    /// <summary>
    /// Get the cost for upgrading to a specific level
    /// </summary>
    public int GetCostForLevel(int level)
    {
        if (level <= 0) return 0;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level - 1));
    }
    
    /// <summary>
    /// Get the effect value at a specific level
    /// </summary>
    public float GetEffectAtLevel(int level)
    {
        if (level <= 0) return 0f;
        return baseEffectValue * level * Mathf.Pow(effectMultiplier, level - 1);
    }
}

/// <summary>
/// UI-specific upgrade types for the main menu upgrade system
/// </summary>
public enum UIUpgradeType
{
    Attack,
    Health,
    Range,
    Speed,
    Generation
}