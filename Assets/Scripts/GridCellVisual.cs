using UnityEngine;

// This script is attached to the grid cell prefab
public class GridCellVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // Cell colors for different states
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color highlightedColor = new Color(0.5f, 1f, 0.5f, 0.4f);
    [SerializeField] private Color occupiedColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.5f, 0.5f, 0.3f);
    
    // Cell state
    private bool isOccupied = false;
    private bool isHighlighted = false;
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Set initial color
        SetDefaultColor();
    }
    
    // Set the cell to its default color
    public void SetDefaultColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isOccupied ? occupiedColor : defaultColor;
        }
    }
    
    // Highlight the cell when hovering
    public void Highlight(bool isValid = true)
    {
        if (spriteRenderer != null)
        {
            isHighlighted = true;
            spriteRenderer.color = isValid ? highlightedColor : invalidColor;
        }
    }
    
    // Remove highlighting
    public void RemoveHighlight()
    {
        isHighlighted = false;
        SetDefaultColor();
    }
    
    // Mark the cell as occupied
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        
        // If not highlighted, update the color
        if (!isHighlighted)
        {
            SetDefaultColor();
        }
    }
    
    // Set a custom color for the cell
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    // Mouse events for interaction
    private void OnMouseEnter()
    {
        Highlight();
    }
    
    private void OnMouseExit()
    {
        RemoveHighlight();
    }
}