using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Universal click detector that works with ALL qubit types
/// Add this to ALL qubit prefabs (OneQubit, ZeroQubit, SuperpositionQubit)
/// </summary>
public class UniversalQubitClicker : MonoBehaviour, IPointerClickHandler
{
    [Header("Click Detection Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private float clickRadius = 0.5f;
    
    // Component references
    private SpriteRangeDisplay rangeDisplay;
    private Qubit qubitComponent;
    private Collider2D clickCollider;
    
    // State tracking
    private bool isRangeVisible = false;
    
    private void Awake()
    {
        // Get components
        rangeDisplay = GetComponent<SpriteRangeDisplay>();
        qubitComponent = GetComponent<Qubit>();
        clickCollider = GetComponent<Collider2D>();
        
        // If no SpriteRangeDisplay, add one
        if (rangeDisplay == null)
        {
            rangeDisplay = gameObject.AddComponent<SpriteRangeDisplay>();
            if (enableDebugLogs)
                Debug.Log($"Added SpriteRangeDisplay to {gameObject.name}");
        }
        
        // Ensure we have a proper collider for clicking
        EnsureClickableCollider();
        
        if (enableDebugLogs)
            Debug.Log($"UniversalQubitClicker initialized on {gameObject.name}");
    }
    
    private void EnsureClickableCollider()
    {
        // Check if we have any collider
        if (clickCollider == null)
        {
            // No collider found, add a circle collider
            CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
            newCollider.radius = clickRadius;
            newCollider.isTrigger = false; // Important for OnMouseDown
            clickCollider = newCollider;
            
            if (enableDebugLogs)
                Debug.Log($"Added CircleCollider2D to {gameObject.name}");
        }
        else
        {
            // Make sure existing collider is set up for clicking
            clickCollider.isTrigger = false; // Important for OnMouseDown
            
            if (enableDebugLogs)
                Debug.Log($"Using existing collider on {gameObject.name}");
        }
    }
    
    private void Update()
    {
        // Manual click detection as backup
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (IsMouseOverThisQubit(mouseWorldPos))
            {
                if (enableDebugLogs)
                    Debug.Log($"Manual click detection on {gameObject.name}");
                HandleQubitClick();
            }
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0; // Ensure z is 0 for 2D
        return worldPos;
    }
    
    private bool IsMouseOverThisQubit(Vector3 mouseWorldPos)
    {
        // Method 1: Use collider if available
        if (clickCollider != null && clickCollider.enabled)
        {
            return clickCollider.bounds.Contains(mouseWorldPos);
        }
        
        // Method 2: Distance check
        float distance = Vector2.Distance(transform.position, mouseWorldPos);
        return distance <= clickRadius;
    }
    
    private void HandleQubitClick()
    {
        // Skip if this is a preview qubit
        if (IsInPreviewMode())
        {
            if (enableDebugLogs)
                Debug.Log($"Skipping click on preview qubit: {gameObject.name}");
            return;
        }
        
        // Toggle range display
        isRangeVisible = !isRangeVisible;
        
        if (rangeDisplay != null)
        {
            rangeDisplay.ToggleRangeDisplay(isRangeVisible);
            
            if (enableDebugLogs)
                Debug.Log($"Toggled range display for {gameObject.name}: {isRangeVisible}");
        }
        
        // Notify selection manager
        QubitSelectionManager selectionManager = QubitSelectionManager.Instance;
        if (selectionManager != null)
        {
            selectionManager.SelectQubit(rangeDisplay, isRangeVisible);
        }
    }
    
    private bool IsInPreviewMode()
    {
        if (qubitComponent == null)
            return false;
            
        // Use reflection to check preview mode
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (previewField != null)
        {
            return (bool)previewField.GetValue(qubitComponent);
        }
        
        return false;
    }
    
    // Multiple click detection methods for maximum compatibility
    
    // Method 1: OnMouseDown (3D physics)
    private void OnMouseDown()
    {
        if (enableDebugLogs)
            Debug.Log($"OnMouseDown on {gameObject.name}");
        HandleQubitClick();
    }
    
    // Method 2: IPointerClickHandler (UI system)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"OnPointerClick on {gameObject.name}");
        HandleQubitClick();
    }
    
    // Method 3: OnMouseUpAsButton (alternative)
    private void OnMouseUpAsButton()
    {
        if (enableDebugLogs)
            Debug.Log($"OnMouseUpAsButton on {gameObject.name}");
        HandleQubitClick();
    }
    
    // Public methods for external control
    public void ShowRange()
    {
        isRangeVisible = true;
        if (rangeDisplay != null)
        {
            rangeDisplay.ToggleRangeDisplay(true);
        }
    }
    
    public void HideRange()
    {
        isRangeVisible = false;
        if (rangeDisplay != null)
        {
            rangeDisplay.ToggleRangeDisplay(false);
        }
    }
    
    public void ToggleRange()
    {
        HandleQubitClick();
    }
    
    public bool IsRangeVisible()
    {
        return isRangeVisible;
    }
    
    // Debug methods
    [ContextMenu("Test Click")]
    private void TestClick()
    {
        Debug.Log($"Manual test click on {gameObject.name}");
        HandleQubitClick();
    }
    
    [ContextMenu("Debug Components")]
    private void DebugComponents()
    {
        Debug.Log($"=== COMPONENT DEBUG for {gameObject.name} ===");
        Debug.Log($"Qubit Component: {qubitComponent != null}");
        Debug.Log($"SpriteRangeDisplay: {rangeDisplay != null}");
        Debug.Log($"Collider2D: {clickCollider != null} (enabled: {(clickCollider != null ? clickCollider.enabled : false)})");
        Debug.Log($"IsInPreviewMode: {IsInPreviewMode()}");
        Debug.Log($"Tag: {tag}");
        Debug.Log($"Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        // Check for other qubit types
        OneQubit oneQubit = GetComponent<OneQubit>();
        ZeroQubit zeroQubit = GetComponent<ZeroQubit>();
        SuperpositionQubit superQubit = GetComponent<SuperpositionQubit>();
        
        Debug.Log($"OneQubit: {oneQubit != null}");
        Debug.Log($"ZeroQubit: {zeroQubit != null}");
        Debug.Log($"SuperpositionQubit: {superQubit != null}");
    }
}