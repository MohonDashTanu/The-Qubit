using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CursorSettings
{
    [Tooltip("Horizontal offset to adjust cursor position (positive values move right)")]
    public float xOffset = 0f;
    
    [Tooltip("Vertical offset to adjust cursor position (positive values move up)")]
    public float yOffset = 0f;
    
    [Tooltip("Debug mode to show visual feedback of cursor alignment")]
    public bool showDebugCursor = false;
}

public class GridPlacementAdjuster : MonoBehaviour
{
    [Header("Cursor Adjustment")]
    [SerializeField] private CursorSettings cursorSettings = new CursorSettings();
    
    [Header("Debug Visualization")]
    [SerializeField] private GameObject cursorDebugVisual;
    
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Hide debug cursor visual by default
        if (cursorDebugVisual != null)
        {
            cursorDebugVisual.SetActive(cursorSettings.showDebugCursor);
        }
    }
    
    private void Update()
    {
        // Toggle debug cursor with F2 key
        if (cursorDebugVisual != null && Input.GetKeyDown(KeyCode.F2))
        {
            cursorSettings.showDebugCursor = !cursorSettings.showDebugCursor;
            cursorDebugVisual.SetActive(cursorSettings.showDebugCursor);
        }
        
        // Update debug cursor position
        if (cursorSettings.showDebugCursor && cursorDebugVisual != null)
        {
            Vector3 adjustedMousePos = GetAdjustedMousePosition();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(adjustedMousePos);
            worldPos.z = 0;
            cursorDebugVisual.transform.position = worldPos;
        }
    }
    
    // The main function that other components will use
    public Vector3 GetAdjustedMousePosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.x += cursorSettings.xOffset;
        mousePos.y += cursorSettings.yOffset;
        return mousePos;
    }
    
    // For use in the inspector to test different values
    public void SetOffsets(float x, float y)
    {
        cursorSettings.xOffset = x;
        cursorSettings.yOffset = y;
        //Debug.Log($"Cursor offsets set to X: {x}, Y: {y}");
    }
}