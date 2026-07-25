using UnityEngine;

/// <summary>
/// Applies main menu upgrades to game objects when they spawn
/// </summary>
public class UpgradeApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpgradeManager upgradeManager;
    
    // Singleton for easy access
    public static UpgradeApplier Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // Find upgrade manager if not assigned
        if (upgradeManager == null)
        {
            // Try to load from Resources or find existing one
            upgradeManager = Resources.Load<UpgradeManager>("MainUpgradeManager");
        }
        
        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeApplier: No UpgradeManager found! Upgrades won't be applied.");
        }
        else
        {
            Debug.Log($"UpgradeApplier initialized with upgrade manager: {upgradeManager.name}");
        }
    }
    
    /// <summary>
    /// Apply upgrades to QuantumCore
    /// </summary>
    public void ApplyUpgradesToCore(QuantumCore core)
    {
        if (upgradeManager == null || core == null) return;
        
        // Get upgrade multipliers
        float attackMultiplier = GetAttackMultiplier();
        float healthMultiplier = GetHealthMultiplier();
        float rangeMultiplier = GetRangeMultiplier();
        
        // Apply to core (you'll need to add these methods to QuantumCore)
        core.ApplyUpgradeMultipliers(attackMultiplier, healthMultiplier, rangeMultiplier);
        
        Debug.Log($"Applied upgrades to QuantumCore: Attack x{attackMultiplier:F2}, Health x{healthMultiplier:F2}, Range x{rangeMultiplier:F2}");
    }
    
    /// <summary>
    /// Apply upgrades to a Qubit when it spawns
    /// </summary>
    public void ApplyUpgradesToQubit(Qubit qubit)
    {
        if (upgradeManager == null || qubit == null) return;
        
        // Get upgrade multipliers
        float attackMultiplier = GetAttackMultiplier();
        float healthMultiplier = GetHealthMultiplier();
        float rangeMultiplier = GetRangeMultiplier();
        
        // Apply to qubit (you'll need to add this method to Qubit)
        qubit.ApplyUpgradeMultipliers(attackMultiplier, healthMultiplier, rangeMultiplier);
        
        Debug.Log($"Applied upgrades to {qubit.name}: Attack x{attackMultiplier:F2}, Health x{healthMultiplier:F2}, Range x{rangeMultiplier:F2}");
    }
    
    /// <summary>
    /// Get attack upgrade multiplier
    /// </summary>
    public float GetAttackMultiplier()
    {
        if (upgradeManager == null) return 1f;
        
        var attackUpgrade = upgradeManager.GetUpgradeProgressByType(UIUpgradeType.Attack);
        if (attackUpgrade != null)
        {
            // Convert upgrade effect to multiplier (effect is additive bonus)
            float effect = attackUpgrade.GetCurrentEffect();
            return 1f + (effect / 100f); // Convert percentage to multiplier
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Get health upgrade multiplier
    /// </summary>
    public float GetHealthMultiplier()
    {
        if (upgradeManager == null) return 1f;
        
        var healthUpgrade = upgradeManager.GetUpgradeProgressByType(UIUpgradeType.Health);
        if (healthUpgrade != null)
        {
            float effect = healthUpgrade.GetCurrentEffect();
            return 1f + (effect / 100f);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Get range upgrade multiplier
    /// </summary>
    public float GetRangeMultiplier()
    {
        if (upgradeManager == null) return 1f;
        
        var rangeUpgrade = upgradeManager.GetUpgradeProgressByType(UIUpgradeType.Range);
        if (rangeUpgrade != null)
        {
            float effect = rangeUpgrade.GetCurrentEffect();
            return 1f + (effect / 100f);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Get speed upgrade multiplier
    /// </summary>
    public float GetSpeedMultiplier()
    {
        if (upgradeManager == null) return 1f;
        
        var speedUpgrade = upgradeManager.GetUpgradeProgressByType(UIUpgradeType.Speed);
        if (speedUpgrade != null)
        {
            float effect = speedUpgrade.GetCurrentEffect();
            return 1f + (effect / 100f);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Get generation upgrade multiplier
    /// </summary>
    public float GetGenerationMultiplier()
    {
        if (upgradeManager == null) return 1f;
        
        var genUpgrade = upgradeManager.GetUpgradeProgressByType(UIUpgradeType.Generation);
        if (genUpgrade != null)
        {
            float effect = genUpgrade.GetCurrentEffect();
            return 1f + (effect / 100f);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Apply all upgrades to all existing objects in scene
    /// </summary>
    public void ApplyUpgradesToAllObjects()
    {
        // Apply to QuantumCore
        QuantumCore core = FindObjectOfType<QuantumCore>();
        if (core != null)
        {
            ApplyUpgradesToCore(core);
        }
        
        // Apply to all Qubits
        Qubit[] allQubits = FindObjectsOfType<Qubit>();
        foreach (Qubit qubit in allQubits)
        {
            ApplyUpgradesToQubit(qubit);
        }
        
        Debug.Log($"Applied upgrades to all objects: 1 core, {allQubits.Length} qubits");
    }
}