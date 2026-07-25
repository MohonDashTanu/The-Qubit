using UnityEngine;

// This component helps integrate the GridManager with QubitManager and other systems
// Attach this to your GameManager or main scene controller
public class GridIntegrationHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private QubitManager qubitManager;
    
    [Header("Qubit Integration")]
    [SerializeField] private bool autoFreeGridOnQubitDestroy = true;
    
    private void Awake()
    {
        // Find references if not assigned
        if (gridManager == null)
        {
            gridManager = Object.FindAnyObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridIntegrationHelper: Could not find GridManager!");
            }
        }
        
        if (qubitManager == null)
        {
            qubitManager = Object.FindAnyObjectByType<QubitManager>();
            if (qubitManager == null)
            {
                Debug.LogError("GridIntegrationHelper: Could not find QubitManager!");
            }
        }
    }
    
    private void Start()
    {
        // Subscribe to events
        if (autoFreeGridOnQubitDestroy)
        {
            // Listen for qubit destroyed events
            // Note: You'll need to add an event in your Qubit class if it doesn't exist
            // Example: public static event System.Action<Vector3> OnQubitDestroyed;
            
            // Qubit.OnQubitDestroyed += HandleQubitDestroyed;
        }
    }
    
    // Method to handle qubit destruction
    private void HandleQubitDestroyed(Vector3 position)
    {
        if (gridManager != null)
        {
            gridManager.FreeCell(position);
        }
    }
    
    // Helper method to manually place a qubit at a specific grid position
    public bool PlaceQubitAtGridPosition(QubitData qubitData, int x, int y)
    {
        if (gridManager == null || qubitManager == null || qubitData == null)
        {
            Debug.LogError("Cannot place qubit - missing references!");
            return false;
        }
        
        // Get the world position for the grid coordinates
        Vector3 worldPosition = gridManager.GetWorldPosition(x, y);
        
        // Check if placement is valid
        if (!gridManager.IsValidPlacement(worldPosition))
        {
            Debug.LogWarning($"Cannot place qubit at grid position ({x}, {y}) - position is invalid or occupied!");
            return false;
        }
        
        // Check resources
        ResourceManager resourceManager = ResourceManager.Instance;
        if (resourceManager != null && qubitData.qubitCost > 0)
        {
            int currentInfo = resourceManager.GetCurrentInformation();
            if (currentInfo < qubitData.qubitCost)
            {
                Debug.Log($"Not enough information to place qubit! Need {qubitData.qubitCost}, have {currentInfo}");
                return false;
            }
            
            // Use the resources
            resourceManager.UseInformation(qubitData.qubitCost);
        }
        
        // Create the actual qubit
        GameObject placedQubit = null;
        if (qubitData.qubitPrefab != null)
        {
            placedQubit = Instantiate(qubitData.qubitPrefab, worldPosition, Quaternion.identity);
            
            // Set the qubit tag
            placedQubit.tag = "Qubit";
            
            // Mark the cell as occupied
            gridManager.OccupyCell(worldPosition, placedQubit);
            
            Debug.Log($"Successfully placed {qubitData.qubitName} at grid position ({x}, {y})");
            return true;
        }
        else
        {
            Debug.LogError($"Cannot place qubit - {qubitData.qubitName} has no prefab assigned!");
            return false;
        }
    }
    
    // Helper method to get a grid cell at a specific world position
    public GridCell GetCellAtWorldPosition(Vector3 worldPosition)
    {
        if (gridManager == null)
        {
            Debug.LogError("Cannot get cell - GridManager is null!");
            return null;
        }
        
        return gridManager.GetCellAtPosition(worldPosition);
    }
    
    // Helper method to convert screen position to grid position
    public Vector2Int ScreenToGridPosition(Vector2 screenPosition)
    {
        if (gridManager == null)
        {
            Debug.LogError("Cannot convert position - GridManager is null!");
            return new Vector2Int(-1, -1);
        }
        
        // Convert screen to world position
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0));
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            return gridManager.GetGridPosition(worldPosition);
        }
        
        return new Vector2Int(-1, -1);
    }
    
    // Toggle grid visibility
    public void ToggleGridVisibility(bool show)
    {
        if (gridManager != null)
        {
            gridManager.ToggleGridVisibility(show);
        }
    }

    // Clear all objects from the grid
    public void ClearGrid()
    {
        if (gridManager == null) return;
        
        // Get grid dimensions
        Vector2Int dimensions = gridManager.GetGridDimensions();
        
        // Loop through all cells
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                Vector3 worldPos = gridManager.GetWorldPosition(x, y);
                GridCell cell = gridManager.GetCellAtPosition(worldPos);
                
                if (cell != null && !cell.IsAvailable && cell.Occupier != null)
                {
                    // Destroy the occupier
                    Destroy(cell.Occupier);
                    
                    // Free the cell
                    gridManager.FreeCell(worldPos);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (autoFreeGridOnQubitDestroy)
        {
            // Qubit.OnQubitDestroyed -= HandleQubitDestroyed;
        }
    }
}