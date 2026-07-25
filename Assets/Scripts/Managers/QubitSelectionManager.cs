using UnityEngine;

public class QubitSelectionManager : MonoBehaviour
{
    // Singleton instance
    public static QubitSelectionManager Instance { get; private set; }
    
    // Keep track of the currently selected qubit
    private SpriteRangeDisplay currentlySelectedQubit;
    
    // For placement preview
    private QubitManager qubitManager;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Find QubitManager - replaced FindObjectOfType with FindAnyObjectByType
        qubitManager = Object.FindAnyObjectByType<QubitManager>();
    }
    
    // Called when a qubit is clicked
    public void SelectQubit(SpriteRangeDisplay selected, bool isSelected)
    {
        Debug.Log($"QubitSelectionManager: SelectQubit called for {selected.gameObject.name}, isSelected={isSelected}");
        
        // If a qubit was clicked to be selected
        if (isSelected)
        {
            // Deselect previous qubit if there was one
            if (currentlySelectedQubit != null && currentlySelectedQubit != selected)
            {
                currentlySelectedQubit.ToggleRangeDisplay(false);
                Debug.Log($"Deselected previous qubit: {currentlySelectedQubit.gameObject.name}");
            }
            
            // Set new selection
            currentlySelectedQubit = selected;
        }
        else if (selected == currentlySelectedQubit)
        {
            // If the current selection was deselected
            currentlySelectedQubit = null;
        }
    }
    
    // Clear all selections (e.g., when clicking elsewhere)
    public void ClearSelection()
    {
        if (currentlySelectedQubit != null)
        {
            currentlySelectedQubit.ToggleRangeDisplay(false);
            currentlySelectedQubit = null;
            Debug.Log("All qubit selections cleared");
        }
    }
    
    private void Update()
    {
        // Handle clicking outside of qubits to deselect
        if (Input.GetMouseButtonDown(0))
        {
            // Check if we clicked on a qubit
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            
            // If we didn't hit a qubit, clear selection
            if (hit.collider == null || !hit.collider.CompareTag("Qubit"))
            {
                ClearSelection();
            }
        }
    }
    
    // This method is called by QubitManager when a qubit is placed
    public void OnQubitPlaced(GameObject placedQubit)
    {
        // Add range display component to newly placed qubit
        if (placedQubit != null && !placedQubit.GetComponent<SpriteRangeDisplay>())
        {
            SpriteRangeDisplay rangeDisplay = placedQubit.AddComponent<SpriteRangeDisplay>();
            Debug.Log($"Added SpriteRangeDisplay to newly placed qubit: {placedQubit.name}");
        }
    }
}