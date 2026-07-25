using UnityEngine;

/// <summary>
/// Automatically sets up all qubits in the scene with proper click detection
/// Add this to your GameManager or run it manually
/// </summary>
public class QubitClickerSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupAllQubitsInScene();
        }
    }
    
    [ContextMenu("Setup All Qubits in Scene")]
    public void SetupAllQubitsInScene()
    {
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        //if (enableDebugLogs)
            //Debug.Log($"Setting up {allQubits.Length} qubits for clicking...");
        
        int setupCount = 0;
        int skippedCount = 0;
        
        foreach (GameObject qubit in allQubits)
        {
            if (qubit == null) continue;
            
            // Skip preview qubits
            if (IsPreviewQubit(qubit))
            {
                skippedCount++;
                if (enableDebugLogs)
                    //Debug.Log($"Skipped preview qubit: {qubit.name}");
                continue;
            }
            
            // Check if already has UniversalQubitClicker
            UniversalQubitClicker existingClicker = qubit.GetComponent<UniversalQubitClicker>();
            if (existingClicker == null)
            {
                // Add the clicker
                UniversalQubitClicker newClicker = qubit.AddComponent<UniversalQubitClicker>();
                
                // Configure it
                var enableLogsField = typeof(UniversalQubitClicker).GetField("enableDebugLogs", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enableLogsField != null)
                {
                    enableLogsField.SetValue(newClicker, enableDebugLogs);
                }
                
                setupCount++;
                
                //if (enableDebugLogs)
                    //Debug.Log($"✅ Added UniversalQubitClicker to {qubit.name}");
            }
            else
            {
                //if (enableDebugLogs)
                    //Debug.Log($"⏭️ {qubit.name} already has UniversalQubitClicker");
            }
            
            // Ensure it has SpriteRangeDisplay
            SpriteRangeDisplay rangeDisplay = qubit.GetComponent<SpriteRangeDisplay>();
            if (rangeDisplay == null)
            {
                rangeDisplay = qubit.AddComponent<SpriteRangeDisplay>();
                //if (enableDebugLogs)
                    //Debug.Log($"✅ Added SpriteRangeDisplay to {qubit.name}");
            }
            
            // Ensure proper collider setup
            EnsureProperCollider(qubit);
        }
        
        if (enableDebugLogs)
        {
            //Debug.Log($"=== SETUP COMPLETE ===");
            //Debug.Log($"✅ Setup: {setupCount} qubits");
            //Debug.Log($"⏭️ Skipped: {skippedCount} preview qubits");
            //Debug.Log($"🎯 Total processed: {setupCount + skippedCount} qubits");
        }
    }
    
    private bool IsPreviewQubit(GameObject qubit)
    {
        // Check preview tag
        if (qubit.CompareTag("PreviewQubit"))
            return true;
        
        // Check name
        if (qubit.name.Contains("_PREVIEW") || qubit.name.Contains("Preview"))
            return true;
        
        // Check if Qubit component is in preview mode
        Qubit qubitComponent = qubit.GetComponent<Qubit>();
        if (qubitComponent != null)
        {
            System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (previewField != null)
            {
                bool isInPreview = (bool)previewField.GetValue(qubitComponent);
                if (isInPreview)
                    return true;
            }
        }
        
        return false;
    }
    
    private void EnsureProperCollider(GameObject qubit)
    {
        Collider2D collider2D = qubit.GetComponent<Collider2D>();
        
        if (collider2D == null)
        {
            // Add a circle collider for clicking
            CircleCollider2D newCollider = qubit.AddComponent<CircleCollider2D>();
            newCollider.radius = 0.5f;
            newCollider.isTrigger = false; // Important for OnMouseDown
            
            //if (enableDebugLogs)
                //Debug.Log($"✅ Added CircleCollider2D to {qubit.name}");
        }
        else
        {
            // Make sure existing collider is set up for clicking
            if (collider2D.isTrigger)
            {
                // If it's a trigger, we need a separate non-trigger collider for clicking
                CircleCollider2D clickCollider = qubit.AddComponent<CircleCollider2D>();
                clickCollider.radius = 0.3f; // Smaller radius to not interfere
                clickCollider.isTrigger = false;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ Added separate click collider to {qubit.name} (existing was trigger)");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"✅ {qubit.name} already has proper click collider");
            }
        }
    }
    
    [ContextMenu("Test All Qubit Clicks")]
    public void TestAllQubitClicks()
    {
        UniversalQubitClicker[] allClickers = FindObjectsOfType<UniversalQubitClicker>();
        
        Debug.Log($"Testing {allClickers.Length} qubit clickers...");
        
        foreach (var clicker in allClickers)
        {
            if (clicker != null)
            {
                clicker.ToggleRange();
                Debug.Log($"Tested click on {clicker.gameObject.name}");
            }
        }
    }
    
    [ContextMenu("Hide All Ranges")]
    public void HideAllRanges()
    {
        UniversalQubitClicker[] allClickers = FindObjectsOfType<UniversalQubitClicker>();
        
        foreach (var clicker in allClickers)
        {
            if (clicker != null)
            {
                clicker.HideRange();
            }
        }
        
        Debug.Log($"Hidden ranges for {allClickers.Length} qubits");
    }
    
    [ContextMenu("Show All Ranges")]
    public void ShowAllRanges()
    {
        UniversalQubitClicker[] allClickers = FindObjectsOfType<UniversalQubitClicker>();
        
        foreach (var clicker in allClickers)
        {
            if (clicker != null)
            {
                clicker.ShowRange();
            }
        }
        
        Debug.Log($"Shown ranges for {allClickers.Length} qubits");
    }
    
    [ContextMenu("Debug All Qubits")]
    public void DebugAllQubits()
    {
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        Debug.Log($"=== DEBUGGING {allQubits.Length} QUBITS ===");
        
        foreach (GameObject qubit in allQubits)
        {
            if (qubit == null) continue;
            
            Debug.Log($"\n--- {qubit.name} ---");
            Debug.Log($"Tag: {qubit.tag}");
            Debug.Log($"Layer: {LayerMask.LayerToName(qubit.layer)}");
            Debug.Log($"IsPreview: {IsPreviewQubit(qubit)}");
            
            // Check components
            Debug.Log($"Components:");
            Debug.Log($"  Qubit: {qubit.GetComponent<Qubit>() != null}");
            Debug.Log($"  OneQubit: {qubit.GetComponent<OneQubit>() != null}");
            Debug.Log($"  ZeroQubit: {qubit.GetComponent<ZeroQubit>() != null}");
            Debug.Log($"  SuperpositionQubit: {qubit.GetComponent<SuperpositionQubit>() != null}");
            Debug.Log($"  UniversalQubitClicker: {qubit.GetComponent<UniversalQubitClicker>() != null}");
            Debug.Log($"  SpriteRangeDisplay: {qubit.GetComponent<SpriteRangeDisplay>() != null}");
            
            // Check colliders
            Collider2D[] colliders = qubit.GetComponentsInChildren<Collider2D>();
            Debug.Log($"  Collider2D count: {colliders.Length}");
            foreach (var col in colliders)
            {
                Debug.Log($"    - {col.name}: enabled={col.enabled}, trigger={col.isTrigger}");
            }
        }
    }
}