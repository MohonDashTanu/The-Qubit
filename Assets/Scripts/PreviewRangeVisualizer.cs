using UnityEngine;

/// <summary>
/// Component to manage range visualization for preview objects.
/// Attach this to objects that need to show attack or generation ranges during preview.
/// </summary>
public class PreviewRangeVisualizer : MonoBehaviour
{
    [Header("Range Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private Transform rangeParent;
    [SerializeField] private float radius = 5f;
    [SerializeField] private Color rangeColor = new Color(1f, 1f, 0.3f, 0.3f);
    [SerializeField] private bool isGeneration = false; // True for generation radius, false for attack range
    
    [Header("Preview Settings")]
    [SerializeField] private bool showInPreviewMode = true;
    [SerializeField] private float previewAlpha = 0.4f;
    
    // Reference to created range object
    private GameObject rangeIndicator;
    private SpriteRenderer rangeRenderer;
    
    private void Awake()
    {
        // Find a parent for the range if not set
        if (rangeParent == null)
        {
            rangeParent = transform;
        }
        
        // Create range indicator if not already present
        CreateRangeIndicator();
    }
    
    private void Start()
    {
        // Update the range visualization
        UpdateRangeSize();
    }
    
    private void CreateRangeIndicator()
    {
        // Check if we already have a range indicator
        Transform existingRange = rangeParent.Find(isGeneration ? "GenerationRange" : "AttackRange");
        if (existingRange != null)
        {
            rangeIndicator = existingRange.gameObject;
            rangeRenderer = rangeIndicator.GetComponent<SpriteRenderer>();
            return;
        }
        
        // Create a new range indicator
        if (rangeIndicatorPrefab != null)
        {
            rangeIndicator = Instantiate(rangeIndicatorPrefab, rangeParent);
        }
        else
        {
            // Create a simple circle if no prefab is provided
            rangeIndicator = new GameObject(isGeneration ? "GenerationRange" : "AttackRange");
            rangeIndicator.transform.SetParent(rangeParent);
            rangeIndicator.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            rangeRenderer = rangeIndicator.AddComponent<SpriteRenderer>();
            
            // Create a circular sprite
            Texture2D texture = CreateCircleTexture(256, 128);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
            rangeRenderer.sprite = sprite;
        }
        
        // Configure the renderer
        if (rangeRenderer == null)
        {
            rangeRenderer = rangeIndicator.GetComponent<SpriteRenderer>();
        }
        
        if (rangeRenderer != null)
        {
            // Use different colors based on range type
            Color baseColor = isGeneration ? new Color(0.3f, 0.7f, 1f, previewAlpha) : new Color(1f, 1f, 0.3f, previewAlpha);
            rangeRenderer.color = baseColor;
            
            // Set sorting layer to be behind the qubit
            rangeRenderer.sortingLayerName = "Object";
            rangeRenderer.sortingOrder = -1;
        }
        
        // Position at zero
        rangeIndicator.transform.localPosition = Vector3.zero;
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
    
    public void UpdateRangeSize()
    {
        if (rangeIndicator == null)
        {
            CreateRangeIndicator();
        }
        
        // Calculate scale to match the desired radius
        // The sprite is normally a 1x1 unit circle, so to make it match the radius exactly,
        // we multiply by 2 (to convert radius to diameter) and apply a calibration factor
        float calibrationFactor = 0.37f; // This value was determined experimentally
        float scale = radius * 2f * calibrationFactor;
        
        // Apply the scale
        rangeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
    }
    
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        UpdateRangeSize();
    }
    
    public void SetColor(Color newColor)
    {
        if (rangeRenderer != null)
        {
            rangeRenderer.color = newColor;
        }
    }
    
    public void SetVisible(bool visible)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(visible);
        }
    }
    
    public void SetPreviewMode(bool isPreview)
    {
        if (rangeIndicator != null)
        {
            // In preview mode, only show if showInPreviewMode is true
            // In normal mode, hide by default (will be shown by other systems when needed)
            rangeIndicator.SetActive(isPreview ? showInPreviewMode : false);
        }
    }
    
    /// <summary>
    /// Set to placement preview mode (green color for valid placement)
    /// </summary>
    public void SetPlacementPreview(bool valid)
    {
        if (rangeRenderer != null)
        {
            Color color = valid ? 
                new Color(0f, 1f, 0f, previewAlpha) : // Green for valid
                new Color(1f, 0f, 0f, previewAlpha);  // Red for invalid
                
            rangeRenderer.color = color;
        }
        
        // Always show during placement preview
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }
    }
    
    /// <summary>
    /// Restore normal color mode
    /// </summary>
    public void SetNormalMode()
    {
        if (rangeRenderer != null)
        {
            // Use different colors based on range type
            Color color = isGeneration ? 
                new Color(0.3f, 0.7f, 1f, previewAlpha) : // Blue for generation
                new Color(1f, 1f, 0.3f, previewAlpha);    // Yellow for attack
                
            rangeRenderer.color = color;
        }
    }
    
    /// <summary>
    /// Get reference to the range indicator GameObject
    /// </summary>
    public GameObject GetRangeIndicator()
    {
        return rangeIndicator;
    }
    
    /// <summary>
    /// Get the current radius value
    /// </summary>
    public float GetRadius()
    {
        return radius;
    }
    
    /// <summary>
    /// Set whether to show in preview mode
    /// </summary>
    public void SetShowInPreviewMode(bool show)
    {
        showInPreviewMode = show;
        
        // Update visibility if currently in a preview
        Qubit qubit = GetComponent<Qubit>();
        if (qubit != null)
        {
            // Try to get preview state through reflection
            System.Reflection.FieldInfo field = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (field != null)
            {
                bool isPreview = (bool)field.GetValue(qubit);
                if (isPreview && rangeIndicator != null)
                {
                    rangeIndicator.SetActive(showInPreviewMode);
                }
            }
        }
    }
    
    /// <summary>
    /// Set the preview alpha value
    /// </summary>
    public void SetPreviewAlpha(float alpha)
    {
        previewAlpha = Mathf.Clamp01(alpha);
        
        // Update color if renderer exists
        if (rangeRenderer != null)
        {
            Color color = rangeRenderer.color;
            color.a = previewAlpha;
            rangeRenderer.color = color;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw a wire sphere to show the range in the editor
        Gizmos.color = isGeneration ? Color.blue : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}