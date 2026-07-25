using UnityEngine;

// Represents a single cell in the grid
// Represents a single cell in the grid
public class GridCell
{
    // Grid coordinates
    public int X { get; private set; }
    public int Y { get; private set; }
    
    // World position - modified to allow updates from GridManager
    public Vector3 WorldPosition { get; set; }
    
    // Availability flag
    public bool IsAvailable { get; set; }
    
    // The object occupying this cell (if any)
    public GameObject Occupier { get; set; }
    
    // Reference to the visual representation (if any)
    public GameObject VisualObject { get; set; }
    
    // Constructor
    public GridCell(Vector3 worldPosition, int x, int y, bool isAvailable)
    {
        WorldPosition = worldPosition;
        X = x;
        Y = y;
        IsAvailable = isAvailable;
        Occupier = null;
        VisualObject = null;
    }
    
    // Check if this cell is adjacent to another cell
    public bool IsAdjacentTo(GridCell other)
    {
        // Check if the other cell is adjacent (horizontally, vertically, or diagonally)
        int xDiff = Mathf.Abs(X - other.X);
        int yDiff = Mathf.Abs(Y - other.Y);
        
        // Return true if the cells are adjacent (including diagonals)
        return xDiff <= 1 && yDiff <= 1 && !(xDiff == 0 && yDiff == 0);
    }
    
    // Check if the cell is adjacent to any occupied cells
    public bool IsAdjacentToOccupied(GridCell[,] grid)
    {
        // Get grid dimensions
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        // Check all surrounding cells
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                // Skip checking this cell
                if (xOffset == 0 && yOffset == 0) continue;
                
                // Calculate neighbor coordinates
                int nx = X + xOffset;
                int ny = Y + yOffset;
                
                // Check if neighbor is within grid bounds
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    // Check if neighbor is occupied
                    if (!grid[nx, ny].IsAvailable)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
}