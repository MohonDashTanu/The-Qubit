using UnityEngine;
using System.Collections;

// Fixed ZeroQubit - STOPS range display pulsing
public class ZeroQubit : Qubit
{
    [Header("Zero Qubit Specific Settings")]
    [SerializeField] private GameObject pulseEffectPrefab;
    [SerializeField] private float pulseInterval = 1f;
    [SerializeField] private float resourceGenerationInterval = 1f;
    
    // Additional internal variables
    private float pulseTimer = 0f;
    private float resourceTimer = 0f;
    
    // Debug variables
    private bool showDebugLogs = false;

    protected override void Awake()
    {
        base.Awake();
        
        // Additional initialization specific to ZeroQubit
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (qubitData != null)
        {
            //Debug.Log($"ZeroQubit using YOUR stats: attackRange={qubitData.attackRange}, attackPower={qubitData.attackPower}, informationPerSecond={qubitData.informationPerSecond}");
        }
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Initialize resource manager if needed
        if (resourceManager == null)
            resourceManager = ResourceManager.Instance;
    }
    
    protected override void Update()
    {
        // Skip all update actions if in preview mode
        if (isInPreviewMode)
            return;
            
        // FIXED: Call base Update for attack logic, but be more careful about combat
        base.Update();
        
        // Handle independent resource generation (uses YOUR informationPerSecond from QubitData)
        resourceTimer += Time.deltaTime;
        if (resourceTimer >= resourceGenerationInterval)
        {
            GenerateResourceIndependent();
            resourceTimer = 0f;
        }
        
        // FIXED: Only pulse if NOT in combat with an enemy
        if (!IsInCombatWithEnemy())
        {
            pulseTimer += Time.deltaTime;
            if (pulseTimer >= pulseInterval)
            {
                CreatePulseEffectOnQubitOnly();
                pulseTimer = 0f;
            }
        }
    }
    
    private bool IsInCombatWithEnemy()
    {
        // Check if any enemies are currently colliding with this qubit
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.6f);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.CompareTag("Enemy"))
            {
                // Check if the enemy is attacking this specific qubit
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Use reflection to check if this enemy is attacking us
                    System.Reflection.FieldInfo obstacleField = typeof(Enemy).GetField("currentObstacle", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        
                    if (obstacleField != null)
                    {
                        GameObject currentObstacle = (GameObject)obstacleField.GetValue(enemy);
                        if (currentObstacle == this.gameObject)
                        {
                            return true; // This enemy is attacking us
                        }
                    }
                }
            }
        }
        
        return false; // No enemies attacking us
    }

    
    // Independent resource generation method - uses YOUR informationPerSecond value
    private void GenerateResourceIndependent()
    {
        // Skip if in preview mode
        if (isInPreviewMode)
            return;

        // Find resource manager if needed
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;

            if (resourceManager == null)
            {
                return;
            }
        }

        // Safety check on qubit data
        if (qubitData == null)
        {
            return;
        }

        // Generate resources using YOUR informationPerSecond value from QubitData
        int baseAmount = Mathf.RoundToInt(qubitData.informationPerSecond);

        // Use base generation amount for now - global upgrades will be applied later
        int generatedAmount = baseAmount;

        // Add information to the resource pool
        resourceManager.AddInformation(generatedAmount);
    }
    
    // Override to use our independent generation method
    protected override void GenerateResource()
    {
        // Skip if in preview mode
        if (isInPreviewMode)
            return;
            
        // Use our independent method instead of the base implementation
        GenerateResourceIndependent();
    }
    
    // FIXED: Create pulse effect ONLY on the qubit sprite itself, NOT the range display
    private void CreatePulseEffectOnQubitOnly()
    {
        // Skip if in preview mode
        if (isInPreviewMode)
            return;
            
        // SAFETY CHECK: Don't pulse if in combat
        if (IsInCombatWithEnemy())
            return;
            
        if (pulseEffectPrefab != null)
        {
            GameObject pulse = Instantiate(pulseEffectPrefab, transform.position, Quaternion.identity);
            pulse.transform.SetParent(transform);
        }
        else
        {
            // FIXED: Only pulse the qubit's main sprite, not any range displays
            StartCoroutine(PulseQubitSpriteOnly());
        }
    }
        
    // FIXED: Simple pulse animation that ONLY affects the qubit's main sprite
    private IEnumerator PulseQubitSpriteOnly()
    {
        // Skip if in preview mode
        if (isInPreviewMode)
            yield break;
        
        // CRITICAL: Only pulse the MAIN SPRITE RENDERER, not child objects
        SpriteRenderer mainSprite = GetComponent<SpriteRenderer>();
        if (mainSprite == null)
        {
            // If no sprite renderer on this object, find the main qubit sprite (not range displays)
            SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in allRenderers)
            {
                // Skip range displays - they should NOT pulse
                if (renderer.gameObject.name.Contains("Range"))
                    continue;
                    
                // Use the first non-range sprite renderer
                mainSprite = renderer;
                break;
            }
        }
        
        if (mainSprite == null)
            yield break;
            
        // Store original values
        Vector3 originalScale = mainSprite.transform.localScale;
        Color originalColor = mainSprite.color;
        
        Vector3 targetScale = originalScale * 1.2f;
        Color targetColor = originalColor;
        targetColor.a = 0.8f; // Slightly fade during pulse
        
        // Scale up and fade
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            mainSprite.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            mainSprite.color = Color.Lerp(originalColor, targetColor, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale down and restore
        elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            mainSprite.transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            mainSprite.color = Color.Lerp(targetColor, originalColor, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we're back to original
        mainSprite.transform.localScale = originalScale;
        mainSprite.color = originalColor;
    }
    
    // Public method to apply global upgrades - called by global system
    public void ApplyGlobalUpgrades()
    {
        // TODO: Implement when global system is ready
        // This method will be called when global upgrades change
        // No need to store individual levels anymore
    }
    
    // Public method to get current generation rate - useful for UI display
    public float GetCurrentGenerationRate()
    {
        if (qubitData == null)
            return 0f;
            
        float baseRate = qubitData.informationPerSecond;
        
        // TODO: Apply global upgrade multipliers when global system is implemented
        // return baseRate * GlobalUpgradeSystem.Instance.GetGenerationMultiplier();
        
        return baseRate;
    }
}