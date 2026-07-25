using System.Collections.Generic;
using System.Linq; // Required for ToArray() extension method
using UnityEngine;

public class GlobalUpgradeManager : MonoBehaviour
{
    [Header("Upgrade Configuration")]
    [SerializeField] private int baseUpgradeCost = 10;
    [SerializeField] private float costMultiplier = 1.5f;
    
    [Header("Upgrade Multipliers")]
    [SerializeField] private float coreUpgradeMultiplier = 0.5f; // 50% increase per level
    [SerializeField] private float zeroQubitMultiplier = 0.3f;   // 30% increase per level
    [SerializeField] private float oneQubitMultiplier = 0.2f;    // 20% increase per level
    
    [Header("Current Upgrade Levels")]
    [SerializeField] private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Singleton instance
    public static GlobalUpgradeManager Instance { get; private set; }
    
    // Events - Made static and public for external systems
    public static event System.Action<string, int> OnUpgradeChanged;
    
    // Resource manager reference
    private ResourceManager resourceManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (showDebugLogs)
            Debug.Log("🌟 GlobalUpgradeManager Awake - Instance set");
        
        // Initialize upgrade levels
        InitializeUpgradeLevels();
    }
    
    private void Start()
    {
        // Find resource manager
        resourceManager = ResourceManager.Instance;
        if (resourceManager == null)
        {
            Debug.LogError("GlobalUpgradeManager: Could not find ResourceManager!");
        }
        
        if (showDebugLogs)
            Debug.Log("✅ GlobalUpgradeManager initialized");
    }
    
    private void InitializeUpgradeLevels()
    {
        // Initialize all upgrade types at level 0
        upgradeLevels["core"] = 0;
        upgradeLevels["zeroQubit"] = 0;
        upgradeLevels["oneQubit"] = 0;
        
        if (showDebugLogs)
            Debug.Log("📊 Initialized upgrade levels - Core: 0, ZeroQubit: 0, OneQubit: 0");
    }
    
    // Main upgrade method - NO MAX LEVEL CHECK
    public bool TryUpgrade(string upgradeType)
    {
        if (showDebugLogs)
            Debug.Log($"🎯 Attempting to upgrade: {upgradeType}");
        
        // Validate upgrade type
        if (!upgradeLevels.ContainsKey(upgradeType))
        {
            Debug.LogError($"Invalid upgrade type: {upgradeType}");
            return false;
        }
        
        // Get current level
        int currentLevel = upgradeLevels[upgradeType];
        
        // Calculate cost
        int cost = GetUpgradeCost(upgradeType);
        
        // Check resources
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager is null!");
            return false;
        }
        
        int currentResources = resourceManager.GetCurrentInformation();
        if (currentResources < cost)
        {
            if (showDebugLogs)
                Debug.Log($"❌ Insufficient resources. Need: {cost}, Have: {currentResources}");
            return false;
        }
        
        // Perform upgrade
        if (resourceManager.UseInformation(cost))
        {
            // INCREMENT THE LEVEL FIRST
            upgradeLevels[upgradeType]++;
            int newLevel = upgradeLevels[upgradeType];
            
            if (showDebugLogs)
                Debug.Log($"✅ Upgraded {upgradeType} to level {newLevel}! Cost: {cost}");
            
            // FIXED: Log event firing with detailed info
            Debug.Log($"🚀 FIRING OnUpgradeChanged EVENT: {upgradeType} -> Level {newLevel}");
            Debug.Log($"🚀 Event subscribers count: {(OnUpgradeChanged?.GetInvocationList()?.Length ?? 0)}");
            
            // Fire event FIRST so other systems can prepare
            OnUpgradeChanged?.Invoke(upgradeType, newLevel);
            
            // FIXED: Log after event fired
            Debug.Log($"🚀 OnUpgradeChanged event fired successfully");
            
            // Then apply upgrades to active objects
            ApplyUpgradesToObjects(upgradeType);
            
            return true;
        }
        
        return false;
    }
    
    // Get the cost for the next upgrade
    public int GetUpgradeCost(string upgradeType)
    {
        if (!upgradeLevels.ContainsKey(upgradeType))
            return -1;
            
        int currentLevel = upgradeLevels[upgradeType];
        
        // Different base costs for different types
        int baseCost = baseUpgradeCost;
        switch (upgradeType)
        {
            case "core":
                baseCost = 500;  // Core upgrades are more expensive
                break;
            case "zeroQubit":
                baseCost = 10;  // Resource generation upgrades
                break;
            case "oneQubit":
                baseCost = 15;  // Attack upgrades are moderate
                break;
        }
        
        // Exponential cost scaling
        int cost = Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
        
        return cost;
    }
    
    // Get current upgrade level
    public int GetUpgradeLevel(string upgradeType)
    {
        if (!upgradeLevels.ContainsKey(upgradeType))
            return 0;
            
        int level = upgradeLevels[upgradeType];
        
        // FIXED: Log when level is requested
        if (showDebugLogs && upgradeType == "core")
        {
            //Debug.Log($"📊 GetUpgradeLevel({upgradeType}) returning: {level}");
        }
        
        return level;
    }
    
    // Get max level for UI purposes (not actually enforced)
    public int GetMaxLevel(string upgradeType)
    {
        // Return a very high number since there's no actual max
        return 9999;
    }
    
    // Get the multiplier for a specific upgrade type
    public float GetUpgradeMultiplier(string upgradeType)
    {
        if (!upgradeLevels.ContainsKey(upgradeType))
            return 1f;
            
        int level = upgradeLevels[upgradeType];
        float baseMultiplier = 1f;
        
        switch (upgradeType)
        {
            case "core":
                baseMultiplier = 1f + (coreUpgradeMultiplier * level);
                break;
            case "zeroQubit":
                baseMultiplier = 1f + (zeroQubitMultiplier * level);
                break;
            case "oneQubit":
                baseMultiplier = 1f + (oneQubitMultiplier * level);
                break;
        }
        
        // If core upgrades exist, apply them as an additional multiplier to other types
        if (upgradeType != "core" && upgradeLevels["core"] > 0)
        {
            int coreLevel = upgradeLevels["core"];
            // REDUCED: Core only gives 20% of its normal bonus to other qubit types
            float reducedCoreBonus = 1f + (coreUpgradeMultiplier * coreLevel * 0.1f); 
            baseMultiplier *= reducedCoreBonus;
        }
        
        // FIXED: Log when multiplier is requested
        if (showDebugLogs && upgradeType == "core")
        {
            Debug.Log($"📊 GetUpgradeMultiplier({upgradeType}) - Level: {level}, Multiplier: {baseMultiplier:F2}");
        }
        
        return baseMultiplier;
    }
    
    // Apply upgrades to all active objects
    private void ApplyUpgradesToObjects(string upgradeType)
    {
        if (showDebugLogs)
            Debug.Log($"📡 Applying {upgradeType} upgrades to active objects");

        switch (upgradeType)
        {
            case "core":
                ApplyCoreUpgrades();
                break;

            case "zeroQubit":
                ApplyZeroQubitUpgrades();
                break;

            case "oneQubit":
                ApplyOneQubitUpgrades();
                break;
        }
    }
    
    private void ApplyCoreUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔧 Applying core upgrades");
        
        // Notify QuantumCore
        QuantumCore core = QuantumCore.Instance;
        if (core != null)
        {
            // FIXED: Call the correct method signature
            Debug.Log($"🎯 Calling QuantumCore.OnUpgradeChanged directly");
            core.OnUpgradeChanged("core", GetUpgradeLevel("core"));
            
            if (showDebugLogs)
                Debug.Log("✅ Notified QuantumCore of upgrade via direct method call");
        }
        else
        {
            Debug.LogWarning("⚠️ QuantumCore not found!");
        }
        
        // Core upgrades also affect all qubits (through multiplier system)
        // This is handled automatically by the GetUpgradeMultiplier method
        ApplyAllQubitUpgrades();
    }
    
    private void ApplyZeroQubitUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔧 Applying Zero Qubit upgrades");
        
        // Find all Zero Qubits and notify them
        ZeroQubit[] zeroQubits = FindObjectsOfType<ZeroQubit>();
        if (showDebugLogs)
            Debug.Log($"Found {zeroQubits.Length} Zero Qubits to upgrade");
        
        foreach (ZeroQubit zeroQubit in zeroQubits)
        {
            if (zeroQubit != null)
            {
                // ZeroQubit inherits from Qubit, so call the base method
                Qubit qubitComponent = zeroQubit.GetComponent<Qubit>();
                if (qubitComponent != null)
                {
                    qubitComponent.ApplyGlobalUpgrades();
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log("✅ Applied Zero Qubit upgrades");
    }
    
    private void ApplyOneQubitUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔧 Applying One Qubit upgrades");
        
        // Find all One Qubits and notify them
        OneQubit[] oneQubits = FindObjectsOfType<OneQubit>();
        if (showDebugLogs)
            Debug.Log($"Found {oneQubits.Length} One Qubits to upgrade");
        
        foreach (OneQubit oneQubit in oneQubits)
        {
            if (oneQubit != null)
            {
                // OneQubit should have a Qubit component too
                Qubit qubitComponent = oneQubit.GetComponent<Qubit>();
                if (qubitComponent != null)
                {
                    qubitComponent.ApplyGlobalUpgrades();
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log("✅ Applied One Qubit upgrades");
    }
    
    private void ApplyAllQubitUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔧 Applying upgrades to all qubits (core effect)");
        
        // Find all qubits and notify them that core upgrades affect them too
        Qubit[] allQubits = FindObjectsOfType<Qubit>();
        if (showDebugLogs)
            Debug.Log($"Found {allQubits.Length} total qubits to notify of core upgrade");
        
        foreach (Qubit qubit in allQubits)
        {
            if (qubit != null)
            {
                qubit.ApplyGlobalUpgrades();
            }
        }
        
        if (showDebugLogs)
            Debug.Log("✅ Applied core upgrades to all qubits");
    }
    
    // Get upgrade info for UI display
    public UpgradeInfo GetUpgradeInfo(string upgradeType)
    {
        if (!upgradeLevels.ContainsKey(upgradeType))
            return null;
        
        return new UpgradeInfo
        {
            upgradeType = upgradeType,
            currentLevel = GetUpgradeLevel(upgradeType),
            nextCost = GetUpgradeCost(upgradeType),
            currentMultiplier = GetUpgradeMultiplier(upgradeType),
            nextMultiplier = GetNextLevelMultiplier(upgradeType),
            canAfford = CanAffordUpgrade(upgradeType)
        };
    }
    
    private float GetNextLevelMultiplier(string upgradeType)
    {
        if (!upgradeLevels.ContainsKey(upgradeType))
            return 1f;
        
        // Temporarily increment level to calculate next multiplier
        upgradeLevels[upgradeType]++;
        float nextMultiplier = GetUpgradeMultiplier(upgradeType);
        upgradeLevels[upgradeType]--; // Restore original level
        
        return nextMultiplier;
    }
    
    public bool CanAffordUpgrade(string upgradeType)
    {
        if (resourceManager == null)
            return false;
        
        int cost = GetUpgradeCost(upgradeType);
        int currentResources = resourceManager.GetCurrentInformation();
        
        return currentResources >= cost;
    }
    
    // Get all upgrade types for UI iteration
    public string[] GetAllUpgradeTypes()
    {
        return new string[] { "core", "zeroQubit", "oneQubit" };
    }
    
    // Reset all upgrades (for testing/debugging)
    public void ResetAllUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔄 Resetting all upgrades");
        
        // Use GetAllUpgradeTypes() instead of Keys.ToArray()
        foreach (string upgradeType in GetAllUpgradeTypes())
        {
            upgradeLevels[upgradeType] = 0;
        }
        
        // Notify all systems of the reset
        foreach (string upgradeType in GetAllUpgradeTypes())
        {
            OnUpgradeChanged?.Invoke(upgradeType, 0);
        }
        
        // Reapply (now zero) upgrades to all objects
        ApplyUpgradesToObjects("core");
        ApplyUpgradesToObjects("zeroQubit");
        ApplyUpgradesToObjects("oneQubit");
        
        if (showDebugLogs)
            Debug.Log("✅ All upgrades reset");
    }
    
    // Manual upgrade application (for testing)
    public void ForceApplyAllUpgrades()
    {
        if (showDebugLogs)
            Debug.Log("🔧 Force applying all upgrades");
        
        ApplyUpgradesToObjects("core");
        ApplyUpgradesToObjects("zeroQubit");
        ApplyUpgradesToObjects("oneQubit");
    }
    
    // Get upgrade summary for debugging
    public string GetUpgradeSummary()
    {
        System.Text.StringBuilder summary = new System.Text.StringBuilder();
        summary.AppendLine("=== UPGRADE SUMMARY ===");
        
        foreach (string upgradeType in GetAllUpgradeTypes())
        {
            int level = GetUpgradeLevel(upgradeType);
            float multiplier = GetUpgradeMultiplier(upgradeType);
            int cost = GetUpgradeCost(upgradeType);
            bool canAfford = CanAffordUpgrade(upgradeType);
            
            summary.AppendLine($"{upgradeType}: Level {level} | {multiplier:F2}x | Next: {cost} | Can afford: {canAfford}");
        }
        
        return summary.ToString();
    }
    
    // FIXED: Method to manually test events
    [ContextMenu("Debug: Test Core Upgrade Event")]
    private void DebugTestCoreUpgradeEvent()
    {
        Debug.Log($"🧪 Testing core upgrade event manually");
        Debug.Log($"🧪 Current subscribers: {(OnUpgradeChanged?.GetInvocationList()?.Length ?? 0)}");
        
        // Fire the event manually
        OnUpgradeChanged?.Invoke("core", GetUpgradeLevel("core") + 1);
        
        Debug.Log($"🧪 Test event fired");
    }
    
    // Debug methods for testing
    [ContextMenu("Debug: Add 100 Information")]
    private void DebugAdd100Information()
    {
        if (resourceManager != null)
        {
            resourceManager.AddInformation(100);
            Debug.Log("Added 100 Information");
        }
    }
    
    [ContextMenu("Debug: Add 1000 Information")]
    private void DebugAdd1000Information()
    {
        if (resourceManager != null)
        {
            resourceManager.AddInformation(1000);
            Debug.Log("Added 1000 Information");
        }
    }
    
    [ContextMenu("Debug: Upgrade Core")]
    private void DebugUpgradeCore()
    {
        TryUpgrade("core");
    }
    
    [ContextMenu("Debug: Upgrade Zero Qubit")]
    private void DebugUpgradeZeroQubit()
    {
        TryUpgrade("zeroQubit");
    }
    
    [ContextMenu("Debug: Upgrade One Qubit")]
    private void DebugUpgradeOneQubit()
    {
        TryUpgrade("oneQubit");
    }
    
    [ContextMenu("Debug: Show All Levels")]
    private void DebugShowAllLevels()
    {
        Debug.Log(GetUpgradeSummary());
    }
    
    [ContextMenu("Debug: Reset All Upgrades")]
    private void DebugResetAllUpgrades()
    {
        ResetAllUpgrades();
    }
    
    [ContextMenu("Debug: Force Apply All Upgrades")]
    private void DebugForceApplyAllUpgrades()
    {
        ForceApplyAllUpgrades();
    }
}

// Helper class for upgrade information
[System.Serializable]
public class UpgradeInfo
{
    public string upgradeType;
    public int currentLevel;
    public int nextCost;
    public float currentMultiplier;
    public float nextMultiplier;
    public bool canAfford;
    
    public float GetUpgradeBenefit()
    {
        return nextMultiplier - currentMultiplier;
    }
    
    public string GetDisplayText()
    {
        return $"Level {currentLevel} → {currentLevel + 1}\n" +
               $"{currentMultiplier:F1}x → {nextMultiplier:F1}x\n" +
               $"Cost: {nextCost}";
    }
}