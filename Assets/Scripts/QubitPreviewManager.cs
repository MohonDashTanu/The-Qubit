using UnityEngine;

/// <summary>
/// Manages qubit placement preview state and ensures attack/generation ranges are visible during preview.
/// Attach this to qubit prefabs to ensure proper preview behavior.
/// </summary>
public class QubitPreviewManager : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private bool isInPreviewMode = false;
    [SerializeField] private float previewAlpha = 0.7f;
    [SerializeField] private bool forceShowRanges = true; // Always show ranges in preview mode
    
    [Header("Range Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private Color validPlacementColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.3f);
    
    // Component references
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private Qubit qubit;
    
    // Range references
    private GameObject attackRangeIndicator;
    private GameObject generationRangeIndicator;
    
    private void Awake()
    {
        // Cache components
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();
        qubit = GetComponent<Qubit>();
        
        // Find existing range indicators
        attackRangeIndicator = transform.Find("AttackRange")?.gameObject;
        generationRangeIndicator = transform.Find("GenerationRange")?.gameObject;
        
        // Check if this is called during preview
        if (isInPreviewMode)
        {
            InitializePreviewMode();
        }
    }
    
    /// <summary>
    /// Set preview mode state and configure the object accordingly
    /// </summary>
    public void SetPreviewMode(bool isPreview)
    {
        isInPreviewMode = isPreview;
        
        if (isPreview)
        {
            InitializePreviewMode();
        }
        else
        {
            ExitPreviewMode();
        }
    }
    
    /// <summary>
    /// Initialize preview mode settings
    /// </summary>
    private void InitializePreviewMode()
    {
        // Tell the Qubit component it's in preview mode
        if (qubit != null)
        {
            // Use reflection to access the protected SetPreviewMode method
            System.Reflection.MethodInfo method = qubit.GetType().GetMethod("SetPreviewMode");
            if (method != null)
            {
                method.Invoke(qubit, new object[] { true });
            }
        }
        
        // Make all renderers semi-transparent
        foreach (SpriteRenderer renderer in renderers)
        {
            // Skip range indicators - handled separately
            if (IsRangeIndicator(renderer.gameObject))
                continue;
                
            Color color = renderer.color;
            color.a = previewAlpha;
            renderer.color = color;
        }
        
        // Disable all colliders in preview
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Make sure range indicators exist and are visible
        EnsureRangeIndicatorsExist();
        
        // Set range indicators to valid placement color
        SetPlacementValidity(true);
    }
    
    /// <summary>
    /// Exit preview mode and restore normal state
    /// </summary>
    private void ExitPreviewMode()
    {
        // Tell the Qubit component it's not in preview mode
        if (qubit != null)
        {
            // Use reflection to access the protected SetPreviewMode method
            System.Reflection.MethodInfo method = qubit.GetType().GetMethod("SetPreviewMode");
            if (method != null)
            {
                method.Invoke(qubit, new object[] { false });
            }
        }
        
        // Restore normal opacity
        foreach (SpriteRenderer renderer in renderers)
        {
            // Skip range indicators - handled separately
            if (IsRangeIndicator(renderer.gameObject))
                continue;
                
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
        
        // Enable all colliders
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
        
        // Hide range indicators by default
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.SetActive(false);
        }
        
        if (generationRangeIndicator != null)
        {
            generationRangeIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Ensure range indicators exist and are properly configured
    /// </summary>
    private void EnsureRangeIndicatorsExist()
    {
        // Check if we need attack range
        bool needsAttackRange = false;
        bool needsGenerationRange = false;
        
        // Check qubit capabilities
        if (qubit != null && qubit.QubitData != null)
        {
            needsAttackRange = qubit.QubitData.canAttack;
            needsGenerationRange = qubit.QubitData.canGenerate;
        }
        
        // Also check for specific qubit types
        if (GetComponent<OneQubit>() != null)
        {
            needsAttackRange = true;
        }
        
        if (GetComponent<ZeroQubit>() != null)
        {
            needsGenerationRange = true;
        }
        
        // Create attack range if needed
        if (needsAttackRange && attackRangeIndicator == null)
        {
            CreateRangeIndicator("AttackRange", new Color(1f, 1f, 0.3f, 0.3f), out attackRangeIndicator);
            
            // Get range value - use base range only
            float attackRange = 5f; // Default
            if (qubit != null && qubit.QubitData != null)
            {
                attackRange = qubit.QubitData.attackRange;
            }
            
            // Set scale
            SetRangeScale(attackRangeIndicator, attackRange);
        }
        
        // Create generation range if needed
        if (needsGenerationRange && generationRangeIndicator == null)
        {
            CreateRangeIndicator("GenerationRange", new Color(0.3f, 0.7f, 1f, 0.3f), out generationRangeIndicator);
            
            // Use a default generation range for Zero Qubit
            float generationRange = 3f;
            
            // Set scale
            SetRangeScale(generationRangeIndicator, generationRange);
        }
        
        // Make sure they're visible in preview mode
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.SetActive(isInPreviewMode && forceShowRanges);
        }
        
        if (generationRangeIndicator != null)
        {
            generationRangeIndicator.SetActive(isInPreviewMode && forceShowRanges);
        }
    }
    
    /// <summary>
    /// Create a range indicator with the specified color
    /// </summary>
    private void CreateRangeIndicator(string name, Color color, out GameObject indicator)
    {
        // Check if it already exists
        Transform existingIndicator = transform.Find(name);
        if (existingIndicator != null)
        {
            indicator = existingIndicator.gameObject;
            
            // Make sure it has a sprite renderer
            SpriteRenderer renderer = indicator.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = indicator.AddComponent<SpriteRenderer>();
            }
            
            // Set the color
            renderer.color = color;
            
            return;
        }
        
        // Create from prefab if available
        if (rangeIndicatorPrefab != null)
        {
            indicator = Instantiate(rangeIndicatorPrefab, transform);
            indicator.name = name;
        }
        else
        {
            // Create a basic circle
            indicator = new GameObject(name);
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
            
            // Create a circular sprite
            Texture2D texture = CreateCircleTexture(256, 128);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
            renderer.sprite = sprite;
            
            // Set the color
            renderer.color = color;
            
            // Set sorting order to be behind the qubit
            renderer.sortingLayerName = "Object";
            renderer.sortingOrder = -1;
        }
    }
    
    /// <summary>
    /// Create a circular texture for range indicators
    /// </summary>
    private Texture2D CreateCircleTexture(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2, size / 2);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                
                if (distance < radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else if (distance < radius + 1)
                {
                    // Smooth edge
                    float t = distance - radius;
                    colors[y * size + x] = new Color(1, 1, 1, 1 - t);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// Check if an object is a range indicator
    /// </summary>
    private bool IsRangeIndicator(GameObject obj)
    {
        return obj.name == "AttackRange" || obj.name == "GenerationRange";
    }
    
    /// <summary>
    /// Set the scale of a range indicator to match the desired radius
    /// </summary>
    private void SetRangeScale(GameObject indicator, float radius)
    {
        if (indicator == null)
            return;
            
        // Apply calibration factor to match visual scale with actual range
        float calibrationFactor = 0.37f; // Value determined experimentally
        float scale = radius * 2f * calibrationFactor;
        
        indicator.transform.localScale = new Vector3(scale, scale, 1f);
    }
    
    /// <summary>
    /// Set the placement validity indicator color
    /// </summary>
    public void SetPlacementValidity(bool isValid)
    {
        Color color = isValid ? validPlacementColor : invalidPlacementColor;
        
        // Set color for attack range
        if (attackRangeIndicator != null)
        {
            SpriteRenderer renderer = attackRangeIndicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
        
        // Set color for generation range
        if (generationRangeIndicator != null)
        {
            SpriteRenderer renderer = generationRangeIndicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// Update the position for preview mode
    /// </summary>
    public void UpdatePreviewPosition(Vector3 position)
    {
        if (!isInPreviewMode)
            return;
            
        transform.position = position;
    }
    
    /// <summary>
    /// Force enable range indicators for preview
    /// </summary>
    public void EnableRangeIndicators(bool enable)
    {
        forceShowRanges = enable;
        
        if (isInPreviewMode)
        {
            // Update visibility based on new setting
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.SetActive(enable);
            }
            
            if (generationRangeIndicator != null)
            {
                generationRangeIndicator.SetActive(enable);
            }
        }
    }
    
    /// <summary>
    /// Update attack range value and scale
    /// </summary>
    public void UpdateAttackRange(float range)
    {
        if (attackRangeIndicator != null)
        {
            SetRangeScale(attackRangeIndicator, range);
        }
    }
    
    /// <summary>
    /// Update generation range value and scale
    /// </summary>
    public void UpdateGenerationRange(float range)
    {
        if (generationRangeIndicator != null)
        {
            SetRangeScale(generationRangeIndicator, range);
        }
    }
    
    /// <summary>
    /// Apply upgrades to ranges if applicable - COMMENTED OUT for global system
    /// </summary>
    public void ApplyRangeUpgrades()
    {
        // COMMENTED OUT: Individual upgrade stats - replaced by global system
        /*
        if (qubit != null && attackRangeIndicator != null)
        {
            float rangeUpgrade = qubit.getUpgradeStat("range");
            
            // Get base range
            float baseRange = 5f;
            if (qubit.QubitData != null)
            {
                baseRange = qubit.QubitData.attackRange;
            }
            
            // Apply upgrade
            float upgradedRange = baseRange + rangeUpgrade;
            
            // Update scale
            SetRangeScale(attackRangeIndicator, upgradedRange);
        }
        */
        
        // TODO: Apply global upgrade multipliers when global system is implemented
        // UpdateAttackRange(baseRange * GlobalUpgradeSystem.Instance.GetRangeMultiplier());
    }
    
    /// <summary>
    /// Get the current preview mode state
    /// </summary>
    public bool IsInPreviewMode()
    {
        return isInPreviewMode;
    }
    
    /// <summary>
    /// Set the preview alpha value for transparency
    /// </summary>
    public void SetPreviewAlpha(float alpha)
    {
        previewAlpha = Mathf.Clamp01(alpha);
        
        if (isInPreviewMode)
        {
            // Update all renderers
            foreach (SpriteRenderer renderer in renderers)
            {
                // Skip range indicators
                if (IsRangeIndicator(renderer.gameObject))
                    continue;
                    
                Color color = renderer.color;
                color.a = previewAlpha;
                renderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// Refresh range displays with current values - for global system updates
    /// </summary>
    public void RefreshRangeDisplays()
    {
        if (qubit != null && qubit.QubitData != null)
        {
            // Update attack range with base value
            if (attackRangeIndicator != null)
            {
                float baseRange = qubit.QubitData.attackRange;
                // TODO: Apply global upgrades when implemented
                // baseRange *= GlobalUpgradeSystem.Instance.GetRangeMultiplier();
                SetRangeScale(attackRangeIndicator, baseRange);
            }
            
            // Generation range typically doesn't change with upgrades
            if (generationRangeIndicator != null)
            {
                float generationRange = 3f; // Default for Zero Qubit
                SetRangeScale(generationRangeIndicator, generationRange);
            }
        }
    }
}