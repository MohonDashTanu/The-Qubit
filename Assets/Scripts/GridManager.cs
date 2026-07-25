using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Properties")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 100;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 originPosition = Vector3.zero;
    [SerializeField] private bool centerGridOnOrigin = true;
    
    [Header("Visual Properties")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Color validCellColor = new Color(0.5f, 1f, 0.5f, 0.3f);
    [SerializeField] private Color invalidCellColor = new Color(1f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color occupiedCellColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
    [SerializeField] private bool showGrid = true;
    
    // The grid data structure
    private GridCell[,] grid;
    
    // List to hold all grid cell visualizers
    private List<GameObject> gridVisualizers = new List<GameObject>();
    
    // Centered origin position (calculated if centerGridOnOrigin is true)
    private Vector3 effectiveOriginPosition;
    
    // Singleton instance
    public static GridManager Instance { get; private set; }
    
    // Statistics
    private int totalCellsOccupied = 0;
    private int totalCellsFreed = 0;
    
    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Calculate the effective origin position (centered if requested)
        CalculateEffectiveOrigin();
        
        // Initialize grid
        InitializeGrid();
    }
    
    private void Start()
    {
        // Create visual representation of the grid
        if (showGrid)
        {
            CreateGridVisual();
        }
        
        // Auto-check Quantum Core compatibility
        Invoke("DebugQuantumCoreCompatibility", 1f); // Delay to ensure QuantumCore is initialized
    }
    
    // Calculate the effective origin based on centering option
    private void CalculateEffectiveOrigin()
    {
        if (centerGridOnOrigin)
        {
            // Calculate the total size of the grid
            float totalWidth = width * cellSize;
            float totalHeight = height * cellSize;
            
            // Set origin to center the grid around the GameObject's position
            effectiveOriginPosition = originPosition + new Vector3(-totalWidth/2, -totalHeight/2, 0);
        }
        else
        {
            // Use the direct origin position
            effectiveOriginPosition = originPosition;
        }
    }

    // Public method to center the grid at runtime
    public void CenterGrid()
    {
        // Calculate the total size of the grid
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;
        
        // Update the effective origin position
        effectiveOriginPosition = originPosition + new Vector3(-totalWidth/2, -totalHeight/2, 0);
        centerGridOnOrigin = true;
        
        // Update all cell positions
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    // Update the cell's position
                    grid[x, y].WorldPosition = GetWorldPosition(x, y);
                    
                    // Update visual object if it exists
                    if (grid[x, y].VisualObject != null)
                    {
                        grid[x, y].VisualObject.transform.position = grid[x, y].WorldPosition;
                    }
                }
            }
        }
    }

    public void ResetGrid()
    {
        // First destroy all qubits on the grid
        GameObject[] qubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        foreach (GameObject qubit in qubits)
        {
            if (qubit != null)
            {
                Destroy(qubit);
            }
        }
        
        // Then make sure all cells are properly freed
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Set the grid cell to available
                grid[x, y].IsAvailable = true;
                grid[x, y].Occupier = null;
                
                // Update the visual if it exists
                if (grid[x, y].VisualObject != null)
                {
                    UpdateCellVisual(grid[x, y].VisualObject, true);
                    
                    // Also update the GridCellVisual component if it exists
                    GridCellVisual visualComponent = grid[x, y].VisualObject.GetComponent<GridCellVisual>();
                    if (visualComponent != null)
                    {
                        visualComponent.SetOccupied(false);
                    }
                }
            }
        }
        
        // Reset statistics
        totalCellsOccupied = 0;
    }

    // You can call this from a button
    public void ResetGridButton()
    {
        ResetGrid();
    }
    
    private void InitializeGrid()
    {
        grid = new GridCell[width, height];
        
        // Initialize all cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = GetWorldPosition(x, y);
                grid[x, y] = new GridCell(worldPos, x, y, true);
            }
        }
    }
    
    private void CreateGridVisual()
    {
        // Clear any existing visualizers
        ClearGridVisual();
        
        // Create new visualizers
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = grid[x, y];
                
                if (cellPrefab != null)
                {
                    // Create the cell visual at the cell's world position (which is now the center)
                    GameObject cellObject = Instantiate(cellPrefab, cell.WorldPosition, Quaternion.identity, transform);
                    cellObject.name = $"Cell_{x}_{y}";
                    
                    // Set the scale to match cell size
                    cellObject.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                    
                    // Store reference to visualizer
                    gridVisualizers.Add(cellObject);
                    
                    // Set initial color based on availability
                    UpdateCellVisual(cellObject, cell.IsAvailable);
                    
                    // Link the cell data with the visualizer
                    cell.VisualObject = cellObject;
                }
            }
        }
    }
    
    private void ClearGridVisual()
    {
        foreach (GameObject visualizer in gridVisualizers)
        {
            if (visualizer != null)
            {
                Destroy(visualizer);
            }
        }
        gridVisualizers.Clear();
    }
    
    private void UpdateCellVisual(GameObject cellObject, bool isAvailable)
    {
        if (cellObject == null) return;
        
        // Get the renderer component
        Renderer renderer = cellObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Set color based on availability
            renderer.material.color = isAvailable ? validCellColor : occupiedCellColor;
        }
        
        // Also update the GridCellVisual component if it exists
        GridCellVisual visualComponent = cellObject.GetComponent<GridCellVisual>();
        if (visualComponent != null)
        {
            visualComponent.SetOccupied(!isAvailable);
        }
    }
    
    // Convert grid coordinates to world position (returns cell CENTER, not corner)
    public Vector3 GetWorldPosition(int x, int y)
    {
        // Calculate the position of the cell CENTER, not the bottom-left corner
        float centerX = (x * cellSize) + (cellSize * 0.5f);
        float centerY = (y * cellSize) + (cellSize * 0.5f);
        return new Vector3(centerX, centerY, 0) + effectiveOriginPosition;
    }
    
    // Convert world position to grid coordinates
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // Adjust world position relative to origin
        Vector3 relativePosition = worldPosition - effectiveOriginPosition;
        
        // Calculate grid position (dividing by cellSize directly)
        int x = Mathf.FloorToInt(relativePosition.x / cellSize);
        int y = Mathf.FloorToInt(relativePosition.y / cellSize);
        
        // Clamp to grid bounds
        int clampedX = Mathf.Clamp(x, 0, width - 1);
        int clampedY = Mathf.Clamp(y, 0, height - 1);
        
        return new Vector2Int(clampedX, clampedY);
    }
    
    // Get the closest cell's world position for a given world position
    public Vector3 GetSnappedPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        return GetWorldPosition(gridPos.x, gridPos.y);
    }
    
    // Check if a position is valid for placement
    public bool IsValidPlacement(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        
        // Check if the ORIGINAL position (before clamping) is within grid bounds
        Vector3 relativePosition = worldPosition - effectiveOriginPosition;
        int originalX = Mathf.FloorToInt(relativePosition.x / cellSize);
        int originalY = Mathf.FloorToInt(relativePosition.y / cellSize);
        
        // Check if the position is within grid bounds BEFORE clamping
        if (originalX < 0 || originalX >= width || originalY < 0 || originalY >= height)
        {
            return false;
        }
        
        // Check if the cell exists
        if (grid == null)
        {
            return false;
        }
        
        // Check cell availability
        try {
            GridCell cell = grid[gridPos.x, gridPos.y];
            if (cell == null)
            {
                return false;
            }
            
            return cell.IsAvailable;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
    
    // Mark a cell as occupied
    public bool OccupyCell(Vector3 worldPosition, GameObject occupier)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        
        // Validate position
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
        {
            return false;
        }
        
        GridCell cell = grid[gridPos.x, gridPos.y];
        
        // Check if cell is available
        if (!cell.IsAvailable)
        {
            return false;
        }
        
        // Occupy the cell
        cell.IsAvailable = false;
        cell.Occupier = occupier;
        totalCellsOccupied++;
        
        // Update visual if it exists
        if (cell.VisualObject != null)
        {
            UpdateCellVisual(cell.VisualObject, false);
            
            // Also update the GridCellVisual component if it exists
            GridCellVisual visualComponent = cell.VisualObject.GetComponent<GridCellVisual>();
            if (visualComponent != null)
            {
                visualComponent.SetOccupied(true);
            }
        }
        
        return true;
    }
    
    // Free a cell
    public void FreeCell(Vector3 worldPosition)
    {
        // First, snap the position to ensure we're freeing the exact grid cell
        Vector3 snappedPosition = GetSnappedPosition(worldPosition);
        Vector2Int gridPos = GetGridPosition(snappedPosition);
        
        // Validate position is within grid bounds
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
        {
            return;
        }
        
        GridCell cell = grid[gridPos.x, gridPos.y];
        
        // Only count as freed if it was actually occupied
        bool wasOccupied = !cell.IsAvailable;
        if (wasOccupied)
        {
            totalCellsFreed++;
        }
        
        // Free the cell
        cell.IsAvailable = true;
        cell.Occupier = null;
        
        // Update visual if it exists
        if (cell.VisualObject != null)
        {
            UpdateCellVisual(cell.VisualObject, true);
            
            // Also update the GridCellVisual component if it exists
            GridCellVisual visualComponent = cell.VisualObject.GetComponent<GridCellVisual>();
            if (visualComponent != null)
            {
                visualComponent.SetOccupied(false);
            }
        }
    }
    
    // Method specifically for handling quantum collapse cleanup
    public void HandleQuantumCollapse()
    {
        // Find all qubits and free their grid cells
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        foreach (GameObject qubit in allQubits)
        {
            if (qubit != null)
            {
                Vector3 position = qubit.transform.position;
                Vector2Int gridPos = GetGridPosition(position);
                
                // Check if this cell is actually occupied before freeing
                if (gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height)
                {
                    GridCell cell = grid[gridPos.x, gridPos.y];
                    if (cell != null && !cell.IsAvailable)
                    {
                        FreeCell(position);
                    }
                }
            }
        }
    }
    
    // Get the cell at a position
    public GridCell GetCellAtPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        
        // Validate position
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
        {
            return null;
        }
        
        return grid[gridPos.x, gridPos.y];
    }
    
    // Check if a world position is within the grid bounds
    public bool IsWithinGridBounds(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - effectiveOriginPosition;
        int x = Mathf.FloorToInt(relativePosition.x / cellSize);
        int y = Mathf.FloorToInt(relativePosition.y / cellSize);
        
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    // Get grid dimensions
    public Vector2Int GetGridDimensions()
    {
        return new Vector2Int(width, height);
    }
    
    // Get cell size
    public float GetCellSize()
    {
        return cellSize;
    }
    
    // Get total grid area in world units
    public Vector2 GetGridWorldSize()
    {
        return new Vector2(width * cellSize, height * cellSize);
    }
    
    // Get the effective origin position
    public Vector3 GetEffectiveOrigin()
    {
        return effectiveOriginPosition;
    }
    
    // Toggle grid visibility
    public void ToggleGridVisibility(bool show)
    {
        showGrid = show;
        
        foreach (GameObject visualizer in gridVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.SetActive(show);
            }
        }
    }
    
    // Method to check if Quantum Core building range is compatible with grid
    public void DebugQuantumCoreCompatibility()
    {
        QuantumCore core = QuantumCore.Instance;
        if (core == null)
        {
            return;
        }
        
        //float buildingRange = core.GetBuildingRange();
        Vector3 corePosition = core.transform.position;
        
        // Calculate how much of the grid the core can actually reach
        Vector3 gridCenter = effectiveOriginPosition + new Vector3((width * cellSize) / 2f, (height * cellSize) / 2f, 0);
        
        // Calculate grid corners
        Vector3 gridMin = effectiveOriginPosition;
        Vector3 gridMax = effectiveOriginPosition + new Vector3(width * cellSize, height * cellSize, 0);
        
        // Check if core can reach all corners
        float distanceToCorner1 = Vector3.Distance(corePosition, gridMin);
        float distanceToCorner2 = Vector3.Distance(corePosition, gridMax);
        float distanceToCorner3 = Vector3.Distance(corePosition, new Vector3(gridMin.x, gridMax.y, 0));
        float distanceToCorner4 = Vector3.Distance(corePosition, new Vector3(gridMax.x, gridMin.y, 0));
        
        float maxCornerDistance = Mathf.Max(distanceToCorner1, distanceToCorner2, distanceToCorner3, distanceToCorner4);
    }
    
    // Get grid statistics
    public GridStatistics GetGridStatistics()
    {
        int occupiedCells = 0;
        int availableCells = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].IsAvailable)
                    availableCells++;
                else
                    occupiedCells++;
            }
        }
        
        return new GridStatistics
        {
            totalCells = width * height,
            occupiedCells = occupiedCells,
            availableCells = availableCells,
            totalCellsOccupied = totalCellsOccupied,
            totalCellsFreed = totalCellsFreed,
            gridDimensions = new Vector2Int(width, height),
            cellSize = cellSize,
            effectiveOrigin = effectiveOriginPosition
        };
    }
    
    // Find all occupied cells and their occupiers
    public List<OccupiedCellInfo> GetAllOccupiedCells()
    {
        List<OccupiedCellInfo> occupiedCells = new List<OccupiedCellInfo>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = grid[x, y];
                if (!cell.IsAvailable && cell.Occupier != null)
                {
                    occupiedCells.Add(new OccupiedCellInfo
                    {
                        gridPosition = new Vector2Int(x, y),
                        worldPosition = cell.WorldPosition,
                        occupier = cell.Occupier
                    });
                }
            }
        }
        
        return occupiedCells;
    }
    
    // Validate grid integrity
    public bool ValidateGridIntegrity()
    {
        bool isValid = true;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = grid[x, y];
                
                // Check for null cells
                if (cell == null)
                {
                    isValid = false;
                    continue;
                }
                
                // Check for inconsistent occupancy
                if (!cell.IsAvailable && cell.Occupier == null)
                {
                    // Auto-fix this issue
                    cell.IsAvailable = true;
                }
                
                // Check for destroyed occupiers
                if (cell.Occupier != null && cell.Occupier == null)
                {
                    // Auto-fix this issue
                    cell.IsAvailable = true;
                    cell.Occupier = null;
                }
            }
        }
        
        return isValid;
    }
    
    // Draw gizmos to show the grid in the editor
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // Calculate the effective origin for gizmos
            Vector3 gizmoOrigin = originPosition;
            if (centerGridOnOrigin)
            {
                float totalWidth = width * cellSize;
                float totalHeight = height * cellSize;
                gizmoOrigin += new Vector3(-totalWidth/2, -totalHeight/2, 0);
            }
            
            // Draw grid in the editor for easier setup
            Gizmos.color = Color.gray;
            
            // Draw grid lines
            for (int x = 0; x <= width; x++)
            {
                Vector3 startPos = gizmoOrigin + new Vector3(x * cellSize, 0, 0);
                Vector3 endPos = gizmoOrigin + new Vector3(x * cellSize, height * cellSize, 0);
                Gizmos.DrawLine(startPos, endPos);
            }
            
            for (int y = 0; y <= height; y++)
            {
                Vector3 startPos = gizmoOrigin + new Vector3(0, y * cellSize, 0);
                Vector3 endPos = gizmoOrigin + new Vector3(width * cellSize, y * cellSize, 0);
                Gizmos.DrawLine(startPos, endPos);
            }
            
            // Draw cell centers
            Gizmos.color = Color.yellow;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Calculate cell center for visualization
                    float centerX = (x * cellSize) + (cellSize * 0.5f);
                    float centerY = (y * cellSize) + (cellSize * 0.5f);
                    Vector3 centerPos = gizmoOrigin + new Vector3(centerX, centerY, 0);
                    Gizmos.DrawSphere(centerPos, 0.05f);
                }
            }
        }
    }
}

// Helper classes for grid information
[System.Serializable]
public class GridStatistics
{
    public int totalCells;
    public int occupiedCells;
    public int availableCells;
    public int totalCellsOccupied;
    public int totalCellsFreed;
    public Vector2Int gridDimensions;
    public float cellSize;
    public Vector3 effectiveOrigin;
}

[System.Serializable]
public class OccupiedCellInfo
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public GameObject occupier;
}