using UnityEngine;

/// <summary>
/// Component that tracks permanent superposition effects applied to qubits
/// Prevents stacking and provides bonuses to the qubit
/// </summary>
public class SuperpositionEffect : MonoBehaviour
{
    public enum EffectType
    {
        SpeedBoost,     // For One Qubits
        ResourceBoost   // For Zero Qubits
    }
    
    [Header("Effect Details")]
    [SerializeField] private EffectType effectType;
    [SerializeField] private float boostAmount;
    [SerializeField] private bool isPermanent;
    [SerializeField] private int gateLevel; // Level of gate that applied this effect
    
    [Header("Runtime Info")]
    [SerializeField] private bool isInitialized = false;
    
    // Cache the qubit component
    private Qubit qubit;
    private ZeroQubit zeroQubit;
    private OneQubit oneQubit;
    
    public EffectType GetEffectType() => effectType;
    public float GetBoostAmount() => boostAmount;
    public bool IsPermanent() => isPermanent;
    public int GetGateLevel() => gateLevel;
    
    /// <summary>
    /// Initialize the superposition effect - FIXED VERSION
    /// </summary>
    public void Initialize(EffectType type, float boost, bool permanent, int level = 1)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"SuperpositionEffect already initialized on {gameObject.name}!");
            return;
        }
        
        effectType = type;
        boostAmount = boost;
        isPermanent = permanent;
        gateLevel = level;
        isInitialized = true;
        
        // Cache components
        qubit = GetComponent<Qubit>();
        zeroQubit = GetComponent<ZeroQubit>();
        oneQubit = GetComponent<OneQubit>();
        
        Debug.Log($"✨ SuperpositionEffect initialized: {type} boost of {boost * 100:F1}% on {gameObject.name}");
        
        // Apply the effect immediately
        ApplyEffect();
    }
    
    /// <summary>
    /// Overload for backward compatibility
    /// </summary>
    public void Initialize(EffectType type, float boost, bool permanent)
    {
        Initialize(type, boost, permanent, 1);
    }
    
    /// <summary>
    /// Apply the superposition effect to the qubit
    /// </summary>
    private void ApplyEffect()
    {
        if (!isInitialized) return;
        
        switch (effectType)
        {
            case EffectType.SpeedBoost:
                ApplySpeedBoost();
                break;
                
            case EffectType.ResourceBoost:
                ApplyResourceBoost();
                break;
        }
    }
    
    /// <summary>
    /// Apply speed boost to attack speed
    /// This modifies the qubit's effective attack speed calculation
    /// </summary>
    private void ApplySpeedBoost()
    {
        // The boost will be applied in the GetEffectiveAttackSpeed method
        // which we'll override in the qubit classes
        Debug.Log($"⚡ Speed boost of {boostAmount * 100:F1}% applied to {gameObject.name}");
    }
    
    /// <summary>
    /// Apply resource boost to generation rate
    /// This modifies the qubit's effective generation rate calculation
    /// </summary>
    private void ApplyResourceBoost()
    {
        // The boost will be applied in the GetEffectiveGenerationRate method
        // which we'll override in the qubit classes  
        Debug.Log($"💎 Resource boost of {boostAmount * 100:F1}% applied to {gameObject.name}");
    }
    
    /// <summary>
    /// Get the speed multiplier for this qubit (1.0 = no boost)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        if (effectType == EffectType.SpeedBoost && isInitialized)
        {
            return 1f + boostAmount;
        }
        return 1f;
    }
    
    /// <summary>
    /// Get the resource multiplier for this qubit (1.0 = no boost)
    /// </summary>
    public float GetResourceMultiplier()
    {
        if (effectType == EffectType.ResourceBoost && isInitialized)
        {
            return 1f + boostAmount;
        }
        return 1f;
    }
    
    /// <summary>
    /// Check if this qubit can receive another superposition effect
    /// </summary>
    public bool CanReceiveAnotherEffect()
    {
        // Qubits with permanent effects cannot receive more
        return !isPermanent;
    }
    
    /// <summary>
    /// Get a description of this effect for UI display
    /// </summary>
    public string GetEffectDescription()
    {
        string effectName = effectType == EffectType.SpeedBoost ? "Speed" : "Resource Generation";
        string boostPercent = (boostAmount * 100f).ToString("F1");
        string permanency = isPermanent ? "Permanent" : "Temporary";
        
        return $"{permanency} {effectName} Boost: +{boostPercent}%";
    }
    
    /// <summary>
    /// Visual indicator in inspector
    /// </summary>
    private void OnValidate()
    {
        if (isInitialized)
        {
            string baseName = gameObject.name.Replace("(SuperPos)", "").Replace("(Clone)", "");
            gameObject.name = baseName + "(SuperPos)";
        }
    }
}