using UnityEngine;
using System.Collections;
using static QubitManager;

/// <summary>
/// Superposition Qubit - Rare hybrid that can both attack AND generate resources
/// Created by transforming One Qubits or Zero Qubits with Hadamard gates
/// </summary>
public class SuperpositionQubit : Qubit
{
    [Header("Superposition Qubit Specific")]
    [SerializeField] private GameObject superpositionEffectPrefab;
    [SerializeField] private float resourceGenerationInterval = 1f;
    [SerializeField] private Color superpositionGlow = new Color(0.5f, 1f, 0.5f, 0.8f);
    
    [Header("Enhanced Stats")]
    [SerializeField] private int bonusAttackPower = 10; // Extra damage for being special
    [SerializeField] private float bonusAttackRange = 1f; // Extra range
    [SerializeField] private float bonusGenerationRate = 1f; // Extra generation
    
    // Superposition-specific variables
    private float resourceTimer = 0f;
    private bool isAlreadySuperpositioned = true; // Born in superposition
    
    protected override void Awake()
    {
        base.Awake();
        
        Debug.Log($"SuperpositionQubit.Awake: {gameObject.name}");
        
        // Mark as already superpositioned to prevent further enhancement
        isAlreadySuperpositioned = true;
        
        // Apply visual effects immediately
        ApplySuperpositionVisuals();
    }
    
    protected override void Start()
    {
        base.Start();
        
        // CRITICAL: Ensure this is NOT in preview mode
        SetPreviewMode(false);
        
        // Verify and fix QubitData capabilities
        ValidateAndFixQubitData();
        
        Debug.Log($"SuperpositionQubit initialized - Attack: {GetEffectiveAttackPower()}, Range: {GetEffectiveAttackRange():F1}, Generation: {GetEffectiveGenerationRate():F1}/s");
    }
    
    /// <summary>
    /// Validate and fix QubitData to ensure proper dual functionality
    /// </summary>
    private void ValidateAndFixQubitData()
    {
        if (qubitData == null)
        {
            Debug.LogError($"SuperpositionQubit {gameObject.name}: No QubitData assigned!");
            return;
        }
        
        // Force enable both capabilities
        qubitData.canAttack = true;
        qubitData.canGenerate = true;
        
        // Ensure reasonable stats - auto-fix if they're zero or too low
        if (qubitData.attackPower <= 0)
        {
            qubitData.attackPower = 25; // Higher than normal qubits
            Debug.Log($"Fixed attackPower: set to {qubitData.attackPower}");
        }
        
        if (qubitData.attackRange <= 0)
        {
            qubitData.attackRange = 6f; // Longer range than normal
            Debug.Log($"Fixed attackRange: set to {qubitData.attackRange}");
        }
        
        if (qubitData.attackSpeed <= 0)
        {
            qubitData.attackSpeed = 1.5f; // Faster than normal
            Debug.Log($"Fixed attackSpeed: set to {qubitData.attackSpeed}");
        }
        
        if (qubitData.informationPerSecond <= 0)
        {
            qubitData.informationPerSecond = 3f; // Good generation rate
            Debug.Log($"Fixed informationPerSecond: set to {qubitData.informationPerSecond}");
        }
        
        if (qubitData.maxHealth <= 0)
        {
            qubitData.maxHealth = 75; // More health than normal
            currentHealth = qubitData.maxHealth;
            Debug.Log($"Fixed maxHealth: set to {qubitData.maxHealth}");
        }
        
        Debug.Log($"SuperpositionQubit stats validated - canAttack: {qubitData.canAttack}, canGenerate: {qubitData.canGenerate}");
    }
    
    protected override void Update()
    {
        // Skip all actions if in preview mode
        if (isInPreviewMode)
            return;
            
        // Call base Update for attack logic (inherited from Qubit)
        base.Update();
        
        // Handle independent resource generation
        resourceTimer += Time.deltaTime;
        if (resourceTimer >= resourceGenerationInterval)
        {
            GenerateSuperpositionResources();
            resourceTimer = 0f;
        }
    }
    
    /// <summary>
    /// Generate resources - this is our main resource generation method
    /// </summary>
    private void GenerateSuperpositionResources()
    {
        // Skip if in preview mode
        if (isInPreviewMode)
            return;
            
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null) 
            {
                Debug.LogWarning("SuperpositionQubit: ResourceManager not found!");
                return;
            }
        }
        
        if (qubitData == null) return;
        
        // Use the effective generation rate (includes global upgrades)
        float effectiveRate = GetEffectiveGenerationRate();
        int generatedAmount = Mathf.RoundToInt(effectiveRate);
        
        if (generatedAmount > 0)
        {
            resourceManager.AddInformation(generatedAmount);
            
            // Show generation effect
            ShowResourceGenerationEffect();
            
            // Debug log occasionally to verify it's working
            if (Time.time % 5f < 0.1f)
            {
                Debug.Log($"💫 SuperpositionQubit generated {generatedAmount} information (Rate: {effectiveRate:F1}/s)");
            }
        }
    }
    
    /// <summary>
    /// Override the base GenerateResource method to use our custom implementation
    /// </summary>
    protected override void GenerateResource()
    {
        // Use our superposition-specific generation method
        GenerateSuperpositionResources();
    }
    
    /// <summary>
    /// Visual effect when generating resources
    /// </summary>
    private void ShowResourceGenerationEffect()
    {
        if (superpositionEffectPrefab != null)
        {
            GameObject effect = Instantiate(superpositionEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Apply permanent superposition visual effects
    /// </summary>
    private void ApplySuperpositionVisuals()
    {
        if (spriteRenderer != null)
        {
            // Apply the superposition glow color
            spriteRenderer.color = superpositionGlow;
            
            // Start continuous shimmer effect
            StartCoroutine(ContinuousShimmerEffect());
        }
    }
    
    /// <summary>
    /// Continuous shimmer effect for superposition qubits
    /// </summary>
    private IEnumerator ContinuousShimmerEffect()
    {
        Color baseColor = superpositionGlow;
        
        while (this != null && !isInPreviewMode)
        {
            float time = Time.time * 3f; // Slower than temporary superposition
            float shimmer = Mathf.Sin(time) * 0.2f + 0.8f; // Subtle shimmer
            
            if (spriteRenderer != null)
            {
                Color currentColor = baseColor;
                currentColor.a = baseColor.a * shimmer;
                spriteRenderer.color = currentColor;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Check if this qubit can be affected by superposition effects
    /// </summary>
    public bool CanBeSuperpositioned()
    {
        return false; // Superposition qubits cannot be enhanced further
    }
    
    /// <summary>
    /// Override the upgrade type to benefit from both zero and one qubit upgrades
    /// For attack capabilities, use oneQubit upgrades
    /// </summary>
    protected override string GetUpgradeType()
    {
        // Use oneQubit upgrades for attack capabilities
        return "oneQubit";
    }
    
    /// <summary>
    /// Override generation rate to use zero qubit multipliers + bonus
    /// </summary>
    protected override float GetEffectiveGenerationRate()
    {
        float baseRate = qubitData.informationPerSecond + bonusGenerationRate;
        
        // Apply global upgrades for zero qubit type (better for resource generation)
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            float multiplier = upgradeManager.GetUpgradeMultiplier("zeroQubit");
            baseRate *= multiplier;
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                baseRate *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }
        return baseRate;
    }
    
    /// <summary>
    /// Override attack power to include bonus damage
    /// </summary>
    protected override int GetEffectiveAttackPower()
    {
        float basePower = qubitData.attackPower + bonusAttackPower;
        
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            float multiplier = upgradeManager.GetUpgradeMultiplier("oneQubit");
            basePower *= multiplier;
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                basePower *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }

        return Mathf.RoundToInt(basePower);
    }
    
    /// <summary>
    /// Override attack speed with global upgrades
    /// </summary>
    protected override float GetEffectiveAttackSpeed()
    {
        float baseSpeed = qubitData.attackSpeed;
        
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            float multiplier = upgradeManager.GetUpgradeMultiplier("oneQubit");
            baseSpeed *= multiplier;
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                baseSpeed *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }

        return baseSpeed;
    }
    
    /// <summary>
    /// Override attack range to include bonus range
    /// </summary>
    protected override float GetEffectiveAttackRange()
    {
        float baseRange = qubitData.attackRange + bonusAttackRange;
        
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            float multiplier = upgradeManager.GetUpgradeMultiplier("oneQubit");
            baseRange *= multiplier;
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                baseRange *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }

        return baseRange;
    }
    
    /// <summary>
    /// Override die method to show special destruction effect
    /// </summary>
    protected override void Die()
    {
        Debug.Log("💫 Superposition Qubit destroyed - high value target eliminated!");
        
        // Show special destruction effect
        if (superpositionEffectPrefab != null)
        {
            GameObject deathEffect = Instantiate(superpositionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(deathEffect, 3f);
        }
        
        // Call base die method
        base.Die();
    }
    
    /// <summary>
    /// Public methods for external systems
    /// </summary>
    public float GetCurrentGenerationRate()
    {
        return GetEffectiveGenerationRate();
    }
    
    public int GetCurrentAttackPower()
    {
        return GetEffectiveAttackPower();
    }
    
    public float GetCurrentAttackRange()
    {
        return GetEffectiveAttackRange();
    }
    
    public float GetCurrentAttackSpeed()
    {
        return GetEffectiveAttackSpeed();
    }
    
    /// <summary>
    /// Get a summary of this qubit's capabilities for UI display
    /// </summary>
    public string GetCapabilitySummary()
    {
        return $"Dual Capability Qubit\n" +
               $"Attack: {GetCurrentAttackPower()} damage\n" +
               $"Range: {GetCurrentAttackRange():F1}\n" +
               $"Speed: {GetCurrentAttackSpeed():F1} attacks/s\n" +
               $"Generation: {GetCurrentGenerationRate():F1} info/s";
    }
    
    /// <summary>
    /// Draw gizmos to show both attack and generation capabilities
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        if (qubitData != null)
        {
            // Draw attack range in yellow
            Gizmos.color = Color.yellow;
            float attackRange = Application.isPlaying ? GetEffectiveAttackRange() : qubitData.attackRange + bonusAttackRange;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw generation indicator in blue
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            
            // Draw superposition indicator in green
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw the qubit itself in cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
    
    /// <summary>
    /// Debug method to manually test resource generation
    /// </summary>
    [ContextMenu("Test Generate Resources")]
    private void TestGenerateResources()
    {
        GenerateSuperpositionResources();
    }
    
    /// <summary>
    /// Debug method to show current stats
    /// </summary>
    [ContextMenu("Show Current Stats")]
    private void ShowCurrentStats()
    {
        Debug.Log(GetCapabilitySummary());
    }
}