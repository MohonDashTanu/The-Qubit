using UnityEngine;
using System.Collections;

/// <summary>
/// Component that makes qubits "confused" with chaotic behavior
/// Created by Pauli-Y gates - adds unpredictable but potentially powerful effects
/// </summary>
public class ConfusedQubit : MonoBehaviour
{
    [Header("Confusion Settings")]
    [SerializeField] private float attackSpeedMultiplier = 3f; // 3x faster attacks for OneQubits
    [SerializeField] private float minGenerationInterval = 0.3f; // Minimum time between generations
    [SerializeField] private float maxGenerationInterval = 3f; // Maximum time between generations
    [SerializeField] private float burstChance = 0.3f; // 30% chance for resource burst
    [SerializeField] private int burstMultiplier = 3; // 3x resources during burst

    [Header("Visual Effects")]
    [SerializeField] private float spinSpeed = 180f; // Degrees per second
    [SerializeField] private float colorCycleSpeed = 2f; // How fast colors change
    [SerializeField] private bool showConfusionParticles = true;

    // Component references
    private Qubit qubitComponent;
    private OneQubit oneQubitComponent;
    private ZeroQubit zeroQubitComponent;
    private SpriteRenderer spriteRenderer;

    // Confusion state
    private bool isConfused = false;
    private float nextGenerationTime = 0f;
    private float confusionEndTime = 0f;
    private float originalAttackSpeed = 1f;
    private Color originalColor = Color.white;
    private Vector3 originalSpriteScale = Vector3.one;
    private Quaternion originalSpriteRotation = Quaternion.identity;
    private bool wasOriginallyOneQubit = false;
    private bool wasOriginallyZeroQubit = false;

    // Visual effects
    private float colorTime = 0f;
    private float spinRotation = 0f;

    private void Awake()
    {
        // Get component references
        qubitComponent = GetComponent<Qubit>();
        oneQubitComponent = GetComponent<OneQubit>();
        zeroQubitComponent = GetComponent<ZeroQubit>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Store original values
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalSpriteScale = spriteRenderer.transform.localScale; // Store original sprite scale
            originalSpriteRotation = spriteRenderer.transform.localRotation; // Store original rotation
        }

        // Determine what type of qubit this was originally
        wasOriginallyOneQubit = oneQubitComponent != null;
        wasOriginallyZeroQubit = zeroQubitComponent != null;

        // Store original attack speed if it's a OneQubit
        if (wasOriginallyOneQubit && qubitComponent != null && qubitComponent.QubitData != null)
        {
            originalAttackSpeed = qubitComponent.QubitData.attackSpeed;
        }

        // Set initial confusion state
        SetConfusionState(true);

        Debug.Log($"🌀 ConfusedQubit initialized - OneQubit: {wasOriginallyOneQubit}, ZeroQubit: {wasOriginallyZeroQubit}");
        Debug.Log($"🎯 Original sprite scale: {originalSpriteScale}");
    }

    private void Update()
    {
        if (!isConfused)
            return;
            
        // Skip all effects if in preview mode
        if (IsInPreviewMode())
            return;
            
        // Check if confusion should expire
        if (Time.time >= confusionEndTime)
        {
            Debug.Log($"⏰ Confusion expired on {gameObject.name} at time {Time.time:F1}");
            RemoveConfusion();
            return;
        }
            
        // Update visual effects
        UpdateConfusionVisuals();
        
        // Handle confused ZeroQubit generation
        if (wasOriginallyZeroQubit)
        {
            HandleConfusedGeneration();
        }
    }

    /// <summary>
    /// Set the confusion state of this qubit
    /// </summary>
    public void SetConfusionState(bool confused)
    {
        isConfused = confused;

        if (confused)
        {
            StartConfusion();
        }
        else
        {
            EndConfusion();
        }
    }

    /// <summary>
    /// Start the confusion effects
    /// </summary>
    private void StartConfusion()
    {
        Debug.Log($"🌀 Starting confusion on {gameObject.name}");

        // Set next generation time for ZeroQubits
        if (wasOriginallyZeroQubit)
        {
            SetRandomGenerationTime();
        }

        // Show confusion particle effect
        if (showConfusionParticles)
        {
            StartCoroutine(ShowConfusionEffect());
        }
    }

    /// <summary>
    /// End the confusion effects and restore normal behavior
    /// </summary>
    private void EndConfusion()
    {
        Debug.Log($"🌀 Ending confusion on {gameObject.name}");

        // Restore original everything
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.transform.localRotation = originalSpriteRotation;
            spriteRenderer.transform.localScale = originalSpriteScale;

            Debug.Log($"🎯 Restored sprite scale to: {originalSpriteScale}");
        }
    }

    /// <summary>
    /// Update the spinning and color-changing visual effects - FIXED VERSION
    /// </summary>
    private void UpdateConfusionVisuals()
    {
        // FIXED: Only spin and color, don't scale during normal updates
        if (spriteRenderer != null)
        {
            // Spinning effect - apply to sprite renderer's transform only
            spinRotation += spinSpeed * Time.deltaTime;
            if (spinRotation >= 360f) spinRotation -= 360f;

            Quaternion targetRotation = originalSpriteRotation * Quaternion.Euler(0, 0, spinRotation);
            spriteRenderer.transform.localRotation = targetRotation;

            // Rainbow color cycling - FIXED: Keep alpha at 1.0 to avoid transparency detection
            colorTime += Time.deltaTime * colorCycleSpeed;

            // Create rainbow effect using HSV
            float hue = (colorTime % 1f); // Cycle through all hues
            Color rainbowColor = Color.HSVToRGB(hue, 0.8f, 1f); // High saturation, full brightness
            rainbowColor.a = 1f; // CRITICAL: Keep full opacity so enemies can detect it

            spriteRenderer.color = rainbowColor;

            // ENSURE scale stays at original - no scaling during normal updates
            spriteRenderer.transform.localScale = originalSpriteScale;
        }
    }

    /// <summary>
    /// Handle random resource generation for confused ZeroQubits
    /// </summary>
    private void HandleConfusedGeneration()
    {
        if (Time.time >= nextGenerationTime)
        {
            GenerateConfusedResources();
            SetRandomGenerationTime();
        }
    }

    /// <summary>
    /// Set a random time for the next resource generation
    /// </summary>
    private void SetRandomGenerationTime()
    {
        float randomInterval = Random.Range(minGenerationInterval, maxGenerationInterval);
        nextGenerationTime = Time.time + randomInterval;

        //Debug.Log($"🎲 Next confused generation in {randomInterval:F1} seconds");
    }

    /// <summary>
    /// Generate resources with random bursts
    /// </summary>
    private void GenerateConfusedResources()
    {
        ResourceManager resourceManager = ResourceManager.Instance;
        if (resourceManager == null || qubitComponent == null || qubitComponent.QubitData == null)
            return;

        // Calculate base generation amount
        float baseRate = qubitComponent.QubitData.informationPerSecond;
        int baseAmount = Mathf.RoundToInt(baseRate);

        // Check for burst generation
        bool isBurst = Random.Range(0f, 1f) < burstChance;
        int generatedAmount = isBurst ? (baseAmount * burstMultiplier) : baseAmount;

        // Generate the resources
        resourceManager.AddInformation(generatedAmount);

        // Visual feedback for bursts
        if (isBurst)
        {
            Debug.Log($"💥 CONFUSED BURST! Generated {generatedAmount} information!");
            StartCoroutine(ShowBurstEffect());
        }
        else
        {
            //Debug.Log($"🌀 Confused generation: {generatedAmount} information");
        }
    }

    /// <summary>
    /// Override the base qubit's attack behavior for confused OneQubits
    /// This method should be called by reflection from the base Qubit class
    /// </summary>
    public float GetConfusedAttackSpeed()
    {
        if (!isConfused || !wasOriginallyOneQubit)
            return originalAttackSpeed;

        return originalAttackSpeed * attackSpeedMultiplier;
    }

    /// <summary>
    /// Get a random attack direction for confused OneQubits
    /// This method should be called when the qubit tries to attack
    /// </summary>
    public Vector2 GetRandomAttackDirection()
    {
        if (!isConfused || !wasOriginallyOneQubit)
            return Vector2.right; // Default direction

        // Generate completely random direction
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
    }

    /// <summary>
    /// Check if this qubit should use confused attack behavior
    /// </summary>
    public bool ShouldUseConfusedAttack()
    {
        return isConfused && wasOriginallyOneQubit;
    }

    /// <summary>
    /// Check if this qubit should use confused generation behavior
    /// </summary>
    public bool ShouldUseConfusedGeneration()
    {
        return isConfused && wasOriginallyZeroQubit;
    }

    /// <summary>
    /// Show burst effect when generating extra resources - FIXED VERSION
    /// </summary>
    private IEnumerator ShowBurstEffect()
    {
        if (spriteRenderer == null) yield break;

        // Use the stored original scale
        Vector3 burstScale = originalSpriteScale * 1.5f;

        // Scale up
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            spriteRenderer.transform.localScale = Vector3.Lerp(originalSpriteScale, burstScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            spriteRenderer.transform.localScale = Vector3.Lerp(burstScale, originalSpriteScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ENSURE we're back to exactly the original scale
        spriteRenderer.transform.localScale = originalSpriteScale;
        Debug.Log($"🎯 Burst effect complete - scale reset to: {spriteRenderer.transform.localScale}");
    }

    /// <summary>
    /// Show initial confusion effect - FIXED VERSION
    /// </summary>
    private IEnumerator ShowConfusionEffect()
    {
        // FIXED: Scale only the sprite renderer using stored original scale
        if (spriteRenderer == null) yield break;

        float effectDuration = 1f;
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            float progress = elapsed / effectDuration;
            float pulse = Mathf.Sin(progress * Mathf.PI * 4) * 0.2f + 1f; // 4 pulses
            spriteRenderer.transform.localScale = originalSpriteScale * pulse;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ENSURE we're back to exactly the original scale
        spriteRenderer.transform.localScale = originalSpriteScale;
        Debug.Log($"🎯 Confusion effect complete - scale reset to: {spriteRenderer.transform.localScale}");
    }

    /// <summary>
    /// Check if in preview mode using reflection
    /// </summary>
    private bool IsInPreviewMode()
    {
        if (qubitComponent == null) return false;

        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (previewField != null)
        {
            return (bool)previewField.GetValue(qubitComponent);
        }

        return false;
    }

    /// <summary>
    /// Get confusion status for external systems
    /// </summary>
    public bool IsConfused()
    {
        return isConfused;
    }

    /// <summary>
    /// Get info about this confused qubit for UI display
    /// </summary>
    public string GetConfusionInfo()
    {
        if (!isConfused) return "";

        if (wasOriginallyOneQubit)
        {
            return $"Confused OneQubit: {attackSpeedMultiplier}x attack speed, random directions";
        }
        else if (wasOriginallyZeroQubit)
        {
            return $"Confused ZeroQubit: Random generation intervals, {burstChance * 100:F0}% burst chance";
        }

        return "Confused Qubit: Chaotic behavior";
    }

    /// <summary>
    /// Remove confusion (called when effect should end)
    /// </summary>
    public void RemoveConfusion()
    {
        SetConfusionState(false);

        // Destroy this component
        Destroy(this);
    }

    /// <summary>
    /// Debug method to toggle confusion for testing
    /// </summary>
    [ContextMenu("Toggle Confusion")]
    private void DebugToggleConfusion()
    {
        SetConfusionState(!isConfused);
    }

    private void OnDestroy()
    {
        // FIXED: Restore original state when component is destroyed
        if (spriteRenderer != null && !IsInPreviewMode())
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.transform.localRotation = originalSpriteRotation;
            spriteRenderer.transform.localScale = originalSpriteScale;

            Debug.Log($"🎯 OnDestroy - restored sprite scale to: {originalSpriteScale}");
        }
    }
    
    public void SetConfusionState(bool confused, float duration = 20f)
    {
        isConfused = confused;
        
        if (confused)
        {
            // Set when confusion should end
            confusionEndTime = Time.time + duration;
            Debug.Log($"🌀 Confusion will expire at time {confusionEndTime:F1} (in {duration} seconds)");
            StartConfusion();
        }
        else
        {
            EndConfusion();
        }
    }
}