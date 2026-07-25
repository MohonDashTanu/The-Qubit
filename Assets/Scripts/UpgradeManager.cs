using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeProgress
{
    public UpgradeData upgradeData;
    public int currentLevel = 0;
    public int totalSpent = 0;
    
    public bool CanUpgrade(int currentCurrency)
    {
        if (currentLevel >= upgradeData.maxLevel) return false;
        int nextLevelCost = upgradeData.GetCostForLevel(currentLevel + 1);
        return currentCurrency >= nextLevelCost;
    }
    
    public int GetNextLevelCost()
    {
        if (currentLevel >= upgradeData.maxLevel) return 0;
        return upgradeData.GetCostForLevel(currentLevel + 1);
    }
    
    public float GetCurrentEffect()
    {
        return upgradeData.GetEffectAtLevel(currentLevel);
    }
}

[CreateAssetMenu(fileName = "Upgrade Manager", menuName = "Quantum/Upgrade Manager")]
public class UpgradeManager : ScriptableObject
{
    [Header("Currency")]
    public int currentCurrency = 9999;
    public int totalEarned = 9999;
    
    [Header("Upgrades")]
    public List<UpgradeProgress> upgrades = new List<UpgradeProgress>();
    
    // Events for UI updates
    public System.Action<int> OnCurrencyChanged;
    public System.Action<UpgradeProgress> OnUpgradeChanged;
    
    /// <summary>
    /// Add currency to the player
    /// </summary>
    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        totalEarned += amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    /// <summary>
    /// Try to purchase an upgrade
    /// </summary>
    public bool TryUpgrade(UpgradeData upgradeData)
    {
        UpgradeProgress progress = GetUpgradeProgress(upgradeData);
        if (progress == null) return false;
        
        if (!progress.CanUpgrade(currentCurrency)) return false;
        
        int cost = progress.GetNextLevelCost();
        currentCurrency -= cost;
        progress.currentLevel++;
        progress.totalSpent += cost;
        
        OnCurrencyChanged?.Invoke(currentCurrency);
        OnUpgradeChanged?.Invoke(progress);
        
        Debug.Log($"Upgraded {upgradeData.upgradeName} to level {progress.currentLevel} for {cost} currency");
        return true;
    }
    
    /// <summary>
    /// Try to purchase an upgrade by UIUpgradeType
    /// </summary>
    public bool TryUpgrade(UIUpgradeType upgradeType)
    {
        UpgradeData upgradeData = GetUpgradeDataByType(upgradeType);
        if (upgradeData == null) return false;
        
        return TryUpgrade(upgradeData);
    }
    
    /// <summary>
    /// Get upgrade data by UIUpgradeType
    /// </summary>
    public UpgradeData GetUpgradeDataByType(UIUpgradeType upgradeType)
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade.upgradeData != null && upgrade.upgradeData.upgradeType == upgradeType)
            {
                return upgrade.upgradeData;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get upgrade progress by UIUpgradeType
    /// </summary>
    public UpgradeProgress GetUpgradeProgressByType(UIUpgradeType upgradeType)
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade.upgradeData != null && upgrade.upgradeData.upgradeType == upgradeType)
            {
                return upgrade;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Refund all upgrades
    /// </summary>
    public void RefundAllUpgrades()
    {
        int totalRefund = 0;
        
        foreach (var upgrade in upgrades)
        {
            totalRefund += upgrade.totalSpent;
            upgrade.currentLevel = 0;
            upgrade.totalSpent = 0;
        }
        
        currentCurrency += totalRefund;
        OnCurrencyChanged?.Invoke(currentCurrency);
        
        foreach (var upgrade in upgrades)
        {
            OnUpgradeChanged?.Invoke(upgrade);
        }
        
        Debug.Log($"Refunded {totalRefund} currency from all upgrades");
    }
    
    /// <summary>
    /// Get upgrade progress for a specific upgrade
    /// </summary>
    public UpgradeProgress GetUpgradeProgress(UpgradeData upgradeData)
    {
        return upgrades.Find(u => u.upgradeData == upgradeData);
    }
    
    /// <summary>
    /// Initialize upgrades if they don't exist
    /// </summary>
    public void InitializeUpgrade(UpgradeData upgradeData)
    {
        if (GetUpgradeProgress(upgradeData) == null)
        {
            upgrades.Add(new UpgradeProgress { upgradeData = upgradeData });
        }
    }
    
    /// <summary>
    /// Get total amount spent on upgrades
    /// </summary>
    public int GetTotalSpent()
    {
        int total = 0;
        foreach (var upgrade in upgrades)
        {
            total += upgrade.totalSpent;
        }
        return total;
    }
    
    /// <summary>
    /// Reset everything (for debugging)
    /// </summary>
    public void ResetAll()
    {
        currentCurrency = totalEarned;
        foreach (var upgrade in upgrades)
        {
            upgrade.currentLevel = 0;
            upgrade.totalSpent = 0;
        }
        
        OnCurrencyChanged?.Invoke(currentCurrency);
        foreach (var upgrade in upgrades)
        {
            OnUpgradeChanged?.Invoke(upgrade);
        }
    }
}