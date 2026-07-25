using UnityEngine;

/// <summary>
/// Manages the display of attack and generation ranges during preview mode.
/// This component ensures that range indicators are properly shown during placement preview.
/// </summary>
public class PreviewRangeDisplayManager : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private GameObject rangePrefab; // Reference to a circle sprite
    [SerializeField] private bool alwaysShowInPreview = true;
    
    [Header("Range Colors")]
    [SerializeField] private Color validPlacementColor = new Color(0f, 1f, 0f, 0.3f); // Green
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.3f); // Red
    [SerializeField] private Color attackRangeColor = new Color(1f, 1f, 0.3f, 0.3f); // Yellow
    [SerializeField] private Color generationRangeColor = new Color(0.3f, 0.7f, 1f, 0.3f); // Blue
    
    [Header("Configuration")]
    [SerializeField] private float calibrationFactor = 0.37f; // Scale factor to match visual size to actual range
    
    // References to attached components
    private Qubit qubitComponent;
    private bool isInPreviewMode = false;
    
    // Cached range values
    private float attackRange = 5f;
    private float generationRange = 3f;
    
    // References to range display objects
    private GameObject attackRangeObject;
    private GameObject generationRangeObject;
    
    private void Awake()
    {
        // Get the qubit component
        qubitComponent = GetComponent<Qubit>();
        
        // Get preview mode state
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
        
        // Initialize range display objects
        InitializeRangeDisplays();
    }
    
    private void Start()
    {
        // Get range values from qubit
        UpdateRangeValues();
        
        // Update range visualizations
        UpdateRangeDisplays();
    }
    
    private void InitializeRangeDisplays()
    {
        // Check qubit type to determine which ranges to show
        bool showAttackRange = false;
        bool showGenerationRange = false;
        
        if (qubitComponent != null && qubitComponent.QubitData != null)
        {
            // Check qubit capabilities
            showAttackRange = qubitComponent.QubitData.canAttack;
            showGenerationRange = qubitComponent.QubitData.canGenerate;
        }
        
        // Also check for specific qubit types
        OneQubit oneQubit = GetComponent<OneQubit>();
        ZeroQubit zeroQubit = GetComponent<ZeroQubit>();
        
        if (oneQubit != null)
        {
            showAttackRange = true;
        }
        
        if (zeroQubit != null)
        {
            showGenerationRange = true;
        }
        
        // Create range displays as needed
        if (showAttackRange)
        {
            CreateRangeDisplay(ref attackRangeObject, "AttackRange", attackRangeColor);
        }
        
        if (showGenerationRange)
        {
            CreateRangeDisplay(ref generationRangeObject, "GenerationRange", generationRangeColor);
        }
    }
    
    private void CreateRangeDisplay(ref GameObject rangeObject, string name, Color color)
    {
        // Check if the object already exists
        Transform existingTransform = transform.Find(name);
        if (existingTransform != null)
        {
            rangeObject = existingTransform.gameObject;
            
            // Update the color
            SpriteRenderer renderer = rangeObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
            
            return;
        }
        
        // Create a new range object
        if (rangePrefab != null)
        {
            // Instantiate from prefab
            rangeObject = Instantiate(rangePrefab, transform);
            rangeObject.name = name;
        }
        else
        {
            // Create a new GameObject with SpriteRenderer
            rangeObject = new GameObject(name);
            rangeObject.transform.SetParent(transform);
            
            // Add a sprite renderer
            SpriteRenderer renderer = rangeObject.AddComponent<SpriteRenderer>();
            
            // Create a circular sprite
            Texture2D texture = CreateCircleTexture(256, 128);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
            renderer.sprite = sprite;
        }
        
        // Configure the range object
        rangeObject.transform.localPosition = Vector3.zero;
        
        // Set the color
        SpriteRenderer spriteRenderer = rangeObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = -1; // Behind the qubit
        }
        
        // Set initial visibility
        rangeObject.SetActive(isInPreviewMode || (alwaysShowInPreview && name == "AttackRange"));
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
    
    private void UpdateRangeValues()
    {
        // Get attack range from OneQubit component
        OneQubit oneQubit = GetComponent<OneQubit>();
        if (oneQubit != null)
        {
            // Try to use the public GetAttackRange method if available
            System.Reflection.MethodInfo method = typeof(OneQubit).GetMethod("GetAttackRange");
            if (method != null)
            {
                attackRange = (float)method.Invoke(oneQubit, null);
            }
            else
            {
                // Try to access the field directly
                System.Reflection.FieldInfo field = typeof(OneQubit).GetField("attackRange", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (field != null)
                {
                    attackRange = (float)field.GetValue(oneQubit);
                }
            }
        }
        else if (qubitComponent != null && qubitComponent.QubitData != null)
        {
            attackRange = qubitComponent.QubitData.attackRange;
        }
        
        // Get generation range from ZeroQubit component
        ZeroQubit zeroQubit = GetComponent<ZeroQubit>();
        if (zeroQubit != null)
        {
            // Try to access the generationRadius field
            System.Reflection.FieldInfo field = typeof(ZeroQubit).GetField("generationRadius", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public);
            
            if (field != null)
            {
                generationRange = (float)field.GetValue(zeroQubit);
            }
        }
        
        // COMMENTED OUT: Individual upgrade stats - replaced by global system
        /*
        // Apply upgrades if available
        if (qubitComponent != null)
        {
            float rangeUpgrade = qubitComponent.getUpgradeStat("range");
            attackRange += rangeUpgrade;
            
            // Generation range is typically not affected by upgrades
        }
        */
        
        // TODO: Apply global upgrade multipliers when global system is implemented
        // attackRange *= GlobalUpgradeSystem.Instance.GetRangeMultiplier();
    }
    
    private void UpdateRangeDisplays()
    {
        // Update attack range display
        if (attackRangeObject != null)
        {
            // Calculate scale to match the desired radius
            float scale = attackRange * 2f * calibrationFactor;
            attackRangeObject.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Set color based on preview mode
            SpriteRenderer renderer = attackRangeObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = isInPreviewMode ? validPlacementColor : attackRangeColor;
            }
            
            // Show in preview mode or if explicitly enabled
            attackRangeObject.SetActive(isInPreviewMode || alwaysShowInPreview);
        }
        
        // Update generation range display
        if (generationRangeObject != null)
        {
            // Calculate scale to match the desired radius
            float scale = generationRange * 2f * calibrationFactor;
            generationRangeObject.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Set color based on preview mode
            SpriteRenderer renderer = generationRangeObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = isInPreviewMode ? validPlacementColor : generationRangeColor;
            }
            
            // Show in preview mode or if explicitly enabled
            generationRangeObject.SetActive(isInPreviewMode || alwaysShowInPreview);
        }
    }
    
    /// <summary>
    /// Set the preview mode state
    /// </summary>
    public void SetPreviewMode(bool isPreview)
    {
        isInPreviewMode = isPreview;
        
        // Update range displays
        UpdateRangeDisplays();
    }
    
    /// <summary>
    /// Set the placement validity indicator
    /// </summary>
    public void SetPlacementValidity(bool isValid)
    {
        if (!isInPreviewMode)
            return;
            
        Color color = isValid ? validPlacementColor : invalidPlacementColor;
        
        // Update attack range color
        if (attackRangeObject != null)
        {
            SpriteRenderer renderer = attackRangeObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
        
        // Update generation range color
        if (generationRangeObject != null)
        {
            SpriteRenderer renderer = generationRangeObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// Toggle visibility of range displays
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (attackRangeObject != null)
        {
            attackRangeObject.SetActive(visible);
        }
        
        if (generationRangeObject != null)
        {
            generationRangeObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Set whether to always show ranges in preview mode
    /// </summary>
    public void SetAlwaysShowInPreview(bool alwaysShow)
    {
        alwaysShowInPreview = alwaysShow;
        
        // Update visibility if we're in preview mode
        if (isInPreviewMode)
        {
            UpdateRangeDisplays();
        }
    }
    
    /// <summary>
    /// Get the attack range value
    /// </summary>
    public float GetAttackRange()
    {
        return attackRange;
    }
    
    /// <summary>
    /// Get the generation range value
    /// </summary>
    public float GetGenerationRange()
    {
        return generationRange;
    }
    
    /// <summary>
    /// Set custom attack range
    /// </summary>
    public void SetAttackRange(float range)
    {
        attackRange = range;
        UpdateRangeDisplays();
    }
    
    /// <summary>
    /// Set custom generation range
    /// </summary>
    public void SetGenerationRange(float range)
    {
        generationRange = range;
        UpdateRangeDisplays();
    }
    
    /// <summary>
    /// Apply range upgrades - COMMENTED OUT for global system
    /// </summary>
    public void ApplyRangeUpgrades()
    {
        // COMMENTED OUT: Individual upgrade stats - replaced by global system
        /*
        if (qubitComponent != null)
        {
            float rangeUpgrade = qubitComponent.getUpgradeStat("range");
            attackRange += rangeUpgrade;
            UpdateRangeDisplays();
        }
        */
        
        // TODO: Apply global upgrade multipliers when global system is implemented
        // UpdateRangeValues();
        // UpdateRangeDisplays();
    }
    
    /// <summary>
    /// Update all ranges with current values from qubit
    /// </summary>
    public void RefreshRangeValues()
    {
        UpdateRangeValues();
        UpdateRangeDisplays();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
            return;
            
        // Draw range gizmos for easier setup
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, generationRange);
    }
}