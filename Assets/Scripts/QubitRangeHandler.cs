using UnityEngine;

/// <summary>
/// Handles the range visualization for qubits, ensuring proper display both in normal and preview modes.
/// This standardized component provides consistent range display across all qubit types.
/// </summary>
public class QubitRangeHandler : MonoBehaviour
{
    [Header("Range Configuration")]
    [SerializeField] private bool isAttackRange = true; // False for generation range (Zero Qubit)
    [SerializeField] private float rangeRadius = 5f; // Default range
    [SerializeField] private GameObject rangePrefab; // Optional custom range prefab
    
    [Header("Visual Settings")]
    [SerializeField] private Color attackRangeColor = new Color(1f, 1f, 0.3f, 0.2f); // Yellow tint
    [SerializeField] private Color generationRangeColor = new Color(0.3f, 0.7f, 1f, 0.2f); // Blue tint
    [SerializeField] private Color placementValidColor = new Color(0f, 1f, 0f, 0.2f); // Green tint
    [SerializeField] private Color placementInvalidColor = new Color(1f, 0f, 0f, 0.2f); // Red tint
    [SerializeField] private float previewAlpha = 0.4f; // Alpha value in preview mode
    
    // Component references
    private GameObject rangeObject;
    private SpriteRenderer rangeRenderer;
    private Qubit qubitComponent;
    
    // State tracking
    private bool isInPreviewMode = false;
    private bool isSelected = false;
    
    private void Awake()
    {
        qubitComponent = GetComponent<Qubit>();
        
        // Determine if we're in preview mode
        if (qubitComponent != null)
        {
            // Use reflection to access the protected isInPreviewMode field
            System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (previewField != null)
            {
                isInPreviewMode = (bool)previewField.GetValue(qubitComponent);
            }
        }
        
        // Check qubit type to determine range type
        ZeroQubit zeroQubit = GetComponent<ZeroQubit>();
        if (zeroQubit != null)
        {
            isAttackRange = false; // Zero Qubit uses generation range
        }
        
        // Create/find range visualization
        CreateRangeVisualization();
    }
    
    private void Start()
    {
        // Get range value from qubit components
        UpdateRangeFromQubit();
        
        // Update the range visualization
        UpdateRangeVisualization();
    }
    
    private void CreateRangeVisualization()
    {
        // Check if range already exists
        string rangeName = isAttackRange ? "AttackRange" : "GenerationRange";
        Transform existingRange = transform.Find(rangeName);
        
        if (existingRange != null)
        {
            rangeObject = existingRange.gameObject;
            rangeRenderer = rangeObject.GetComponent<SpriteRenderer>();
            
            if (rangeRenderer == null)
            {
                rangeRenderer = rangeObject.AddComponent<SpriteRenderer>();
            }
            
            return;
        }
        
        // Create new range object
        if (rangePrefab != null)
        {
            rangeObject = Instantiate(rangePrefab, transform);
        }
        else
        {
            // Create a simple circle if no prefab is provided
            rangeObject = new GameObject(rangeName);
            rangeObject.transform.SetParent(transform);
            rangeObject.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            rangeRenderer = rangeObject.AddComponent<SpriteRenderer>();
            
            // Create a circular sprite
            Texture2D texture = CreateCircleTexture(256, 128);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
            rangeRenderer.sprite = sprite;
        }
        
        // Ensure we have a renderer reference
        if (rangeRenderer == null)
        {
            rangeRenderer = rangeObject.GetComponent<SpriteRenderer>();
        }
        
        // Configure the renderer
        if (rangeRenderer != null)
        {
            // Use different colors based on range type
            Color baseColor = isAttackRange ? attackRangeColor : generationRangeColor;
            rangeRenderer.color = baseColor;
            
            // Set sorting layer to be behind the qubit
            rangeRenderer.sortingLayerName = "Object";
            rangeRenderer.sortingOrder = -1;
        }
        
        // Position at zero
        rangeObject.transform.localPosition = Vector3.zero;
    }
    
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
    /// Get the current range from qubit components
    /// </summary>
    private void UpdateRangeFromQubit()
    {
        // Get range from different qubit types
        if (isAttackRange)
        {
            // Get attack range
            OneQubit oneQubit = GetComponent<OneQubit>();
            if (oneQubit != null)
            {
                // Try to get the range through the GetAttackRange method
                System.Reflection.MethodInfo method = typeof(OneQubit).GetMethod("GetAttackRange");
                if (method != null)
                {
                    rangeRadius = (float)method.Invoke(oneQubit, null);
                }
                else
                {
                    // Try to access the field directly
                    System.Reflection.FieldInfo field = typeof(OneQubit).GetField("attackRange", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public);
                    
                    if (field != null)
                    {
                        rangeRadius = (float)field.GetValue(oneQubit);
                    }
                }
            }
            else if (qubitComponent != null && qubitComponent.QubitData != null)
            {
                rangeRadius = qubitComponent.QubitData.attackRange;
            }
        }
        else
        {
            // Get generation range
            ZeroQubit zeroQubit = GetComponent<ZeroQubit>();
            if (zeroQubit != null)
            {
                // Try to access the generation radius field
                System.Reflection.FieldInfo field = typeof(ZeroQubit).GetField("generationRadius", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (field != null)
                {
                    rangeRadius = (float)field.GetValue(zeroQubit);
                }
                else
                {
                    // Default to 3 for Zero Qubit if field not found
                    rangeRadius = 3f;
                }
            }
        }
        
        // COMMENTED OUT: Individual upgrade modifications - replaced by global system
        /*
        // Check for upgrade modifications
        if (qubitComponent != null)
        {
            // Get upgrade stats that affect range
            if (isAttackRange)
            {
                float rangeUpgrade = qubitComponent.getUpgradeStat("range");
                rangeRadius += rangeUpgrade;
            }
            else
            {
                // Generation range isn't typically affected by upgrades,
                // but we could add similar logic if needed
            }
        }
        */
        
        // TODO: Apply global upgrade multipliers when global system is implemented
        // if (isAttackRange)
        // {
        //     rangeRadius *= GlobalUpgradeSystem.Instance.GetRangeMultiplier();
        // }
    }
    
    /// <summary>
    /// Update the visual representation of the range
    /// </summary>
    private void UpdateRangeVisualization()
    {
        if (rangeObject == null)
            return;
            
        // Calculate scale to match the desired radius
        float calibrationFactor = 0.37f; // This value was determined experimentally
        float scale = rangeRadius * 2f * calibrationFactor;
        
        // Apply the scale
        rangeObject.transform.localScale = new Vector3(scale, scale, 1f);
        
        // Ensure proper color
        if (rangeRenderer != null)
        {
            // Use different colors based on range type and mode
            Color targetColor;
            
            if (isInPreviewMode)
            {
                // Use valid placement color in preview mode (green)
                targetColor = placementValidColor;
            }
            else
            {
                // Use normal colors
                targetColor = isAttackRange ? attackRangeColor : generationRangeColor;
            }
            
            // Apply color
            rangeRenderer.color = targetColor;
        }
        
        // Set visibility based on mode
        rangeObject.SetActive(isInPreviewMode || isSelected);
    }
    
    /// <summary>
    /// Select this qubit and show its range
    /// </summary>
    public void Select(bool selected)
    {
        isSelected = selected;
        
        if (rangeObject != null)
        {
            rangeObject.SetActive(isInPreviewMode || isSelected);
        }
    }
    
    /// <summary>
    /// Set the preview mode state
    /// </summary>
    public void SetPreviewMode(bool isPreview)
    {
        isInPreviewMode = isPreview;
        
        // Always show in preview mode
        if (rangeObject != null)
        {
            rangeObject.SetActive(isPreview || isSelected);
            
            // Update color for preview mode
            if (rangeRenderer != null)
            {
                if (isPreview)
                {
                    // Use placement color
                    rangeRenderer.color = placementValidColor;
                }
                else
                {
                    // Use normal colors
                    rangeRenderer.color = isAttackRange ? attackRangeColor : generationRangeColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Update the range value
    /// </summary>
    public void SetRange(float newRange)
    {
        rangeRadius = newRange;
        UpdateRangeVisualization();
    }
    
    /// <summary>
    /// Set the placement validity indicator
    /// </summary>
    public void SetPlacementValidity(bool isValid)
    {
        if (!isInPreviewMode || rangeRenderer == null)
            return;
            
        rangeRenderer.color = isValid ? placementValidColor : placementInvalidColor;
    }
    
    /// <summary>
    /// Toggle visibility of the range
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (rangeObject != null)
        {
            rangeObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Set the range color
    /// </summary>
    public void SetRangeColor(Color color)
    {
        if (rangeRenderer != null)
        {
            rangeRenderer.color = color;
        }
    }
    
    /// <summary>
    /// Force visibility in preview mode
    /// </summary>
    public void ForceShowInPreview(bool force)
    {
        if (isInPreviewMode && rangeObject != null)
        {
            rangeObject.SetActive(force);
        }
    }
    
    /// <summary>
    /// Get the current range radius
    /// </summary>
    public float GetRangeRadius()
    {
        return rangeRadius;
    }
    
    /// <summary>
    /// Get whether this is an attack range
    /// </summary>
    public bool IsAttackRange()
    {
        return isAttackRange;
    }
    
    /// <summary>
    /// Set whether this is an attack range
    /// </summary>
    public void SetIsAttackRange(bool isAttack)
    {
        if (isAttackRange != isAttack)
        {
            isAttackRange = isAttack;
            
            // Update name
            if (rangeObject != null)
            {
                rangeObject.name = isAttackRange ? "AttackRange" : "GenerationRange";
            }
            
            // Update color
            if (rangeRenderer != null && !isInPreviewMode)
            {
                rangeRenderer.color = isAttackRange ? attackRangeColor : generationRangeColor;
            }
        }
    }
    
    /// <summary>
    /// Refresh range values from qubit components - for global system updates
    /// </summary>
    public void RefreshRangeValues()
    {
        UpdateRangeFromQubit();
        UpdateRangeVisualization();
    }
    
    /// <summary>
    /// Apply global upgrades - called by global upgrade system
    /// </summary>
    public void ApplyGlobalUpgrades()
    {
        // TODO: Implement when global system is ready
        // UpdateRangeFromQubit();
        // UpdateRangeVisualization();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Don't draw in play mode to avoid confusion with actual range visualization
        if (Application.isPlaying)
            return;
            
        // Draw a wire sphere to show the range in the editor
        Gizmos.color = isAttackRange ? Color.yellow : Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangeRadius);
    }
}