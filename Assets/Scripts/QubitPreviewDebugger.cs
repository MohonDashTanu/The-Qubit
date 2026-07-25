using UnityEngine;

/// <summary>
/// Debug utility to find and fix qubits stuck in preview mode
/// Attach to any GameObject in your scene or use the static methods
/// </summary>
public class QubitPreviewDebugger : MonoBehaviour
{
    [Header("Debug Actions")]
    [SerializeField] private bool findStuckQubits = false;
    [SerializeField] private bool fixAllStuckQubits = false;
    [SerializeField] private bool analyzeSpecificQubit = false;
    [SerializeField] private GameObject targetQubit; // Drag a specific qubit here to analyze
    
    private void Update()
    {
        if (findStuckQubits)
        {
            findStuckQubits = false;
            FindStuckPreviewQubits();
        }
        
        if (fixAllStuckQubits)
        {
            fixAllStuckQubits = false;
            FixAllStuckPreviewQubits();
        }
        
        if (analyzeSpecificQubit && targetQubit != null)
        {
            analyzeSpecificQubit = false;
            AnalyzeQubitState(targetQubit);
        }
    }
    
    /// <summary>
    /// Find all qubits that appear to be stuck in preview mode
    /// </summary>
    [ContextMenu("Find Stuck Preview Qubits")]
    public static void FindStuckPreviewQubits()
    {
        Debug.Log("=== SEARCHING FOR STUCK PREVIEW QUBITS ===");
        
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        int stuckCount = 0;
        
        foreach (GameObject qubitObj in allQubits)
        {
            if (qubitObj == null) continue;
            
            bool isStuck = IsQubitStuckInPreview(qubitObj);
            if (isStuck)
            {
                stuckCount++;
                Debug.LogError($"🔴 STUCK QUBIT FOUND: {qubitObj.name}", qubitObj);
                AnalyzeQubitState(qubitObj);
            }
        }
        
        if (stuckCount == 0)
        {
            Debug.Log("✅ No stuck preview qubits found!");
        }
        else
        {
            Debug.LogError($"🔴 Found {stuckCount} qubits stuck in preview mode!");
        }
    }
    
    /// <summary>
    /// Fix all qubits that are stuck in preview mode
    /// </summary>
    [ContextMenu("Fix All Stuck Preview Qubits")]
    public static void FixAllStuckPreviewQubits()
    {
        Debug.Log("=== FIXING ALL STUCK PREVIEW QUBITS ===");
        
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        int fixedCount = 0;
        
        foreach (GameObject qubitObj in allQubits)
        {
            if (qubitObj == null) continue;
            
            bool wasStuck = IsQubitStuckInPreview(qubitObj);
            if (wasStuck)
            {
                Debug.Log($"🔧 Fixing stuck qubit: {qubitObj.name}");
                ForceFixQubitPreviewMode(qubitObj);
                fixedCount++;
            }
        }
        
        if (fixedCount == 0)
        {
            Debug.Log("✅ No qubits needed fixing!");
        }
        else
        {
            Debug.Log($"🔧 Fixed {fixedCount} stuck preview qubits!");
        }
    }
    
    /// <summary>
    /// Check if a qubit appears to be stuck in preview mode
    /// </summary>
    public static bool IsQubitStuckInPreview(GameObject qubitObj)
    {
        if (qubitObj == null) return false;
        
        // Check if it has the right tag but shows preview characteristics
        if (!qubitObj.CompareTag("Qubit")) return false;
        
        Qubit qubitComponent = qubitObj.GetComponent<Qubit>();
        if (qubitComponent == null) return false;
        
        // Use reflection to check preview mode
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (previewField != null)
        {
            bool isInPreview = (bool)previewField.GetValue(qubitComponent);
            if (isInPreview)
            {
                return true; // Definitely stuck in preview
            }
        }
        
        // Check for other preview indicators that might indicate a stuck state
        
        // Check if colliders are disabled (preview qubits have disabled colliders)
        Collider2D[] colliders = qubitObj.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            bool allDisabled = true;
            foreach (var collider in colliders)
            {
                if (collider.enabled)
                {
                    allDisabled = false;
                    break;
                }
            }
            if (allDisabled)
            {
                return true; // Colliders disabled = likely stuck in preview
            }
        }
        
        // Check for semi-transparent sprites
        SpriteRenderer sr = qubitObj.GetComponent<SpriteRenderer>();
        if (sr != null && sr.color.a < 0.9f)
        {
            return true; // Semi-transparent = likely stuck in preview
        }
        
        // Check for preview indicators in name
        if (qubitObj.name.Contains("_PREVIEW") || qubitObj.name.Contains("Preview"))
        {
            return true; // Name indicates preview
        }
        
        return false; // Appears to be functioning normally
    }
    
    /// <summary>
    /// Analyze and log the state of a specific qubit
    /// </summary>
    public static void AnalyzeQubitState(GameObject qubitObj)
    {
        if (qubitObj == null)
        {
            Debug.Log("Cannot analyze null qubit");
            return;
        }
        
        Debug.Log($"=== ANALYZING QUBIT: {qubitObj.name} ===");
        
        // Basic info
        Debug.Log($"Tag: {qubitObj.tag}");
        Debug.Log($"Layer: {qubitObj.layer} ({LayerMask.LayerToName(qubitObj.layer)})");
        Debug.Log($"Active: {qubitObj.activeInHierarchy}");
        
        // Check Qubit component
        Qubit qubitComponent = qubitObj.GetComponent<Qubit>();
        if (qubitComponent != null)
        {
            // Use reflection to check preview mode
            System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (previewField != null)
            {
                bool isInPreview = (bool)previewField.GetValue(qubitComponent);
                Debug.Log($"isInPreviewMode: {isInPreview}");
            }
            
            Debug.Log($"QubitData: {(qubitComponent.QubitData != null ? qubitComponent.QubitData.name : "NULL")}");
        }
        else
        {
            Debug.Log("❌ No Qubit component found!");
        }
        
        // Check other qubit types
        OneQubit oneQubit = qubitObj.GetComponent<OneQubit>();
        ZeroQubit zeroQubit = qubitObj.GetComponent<ZeroQubit>();
        Debug.Log($"OneQubit: {oneQubit != null}");
        Debug.Log($"ZeroQubit: {zeroQubit != null}");
        
        // Check colliders
        Collider2D[] colliders = qubitObj.GetComponentsInChildren<Collider2D>();
        Debug.Log($"Collider2D count: {colliders.Length}");
        foreach (var collider in colliders)
        {
            Debug.Log($"  - {collider.gameObject.name}: enabled={collider.enabled}, trigger={collider.isTrigger}");
        }
        
        // Check sprite renderer
        SpriteRenderer sr = qubitObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"SpriteRenderer alpha: {sr.color.a}");
        }
        
        // Check range display
        SpriteRangeDisplay rangeDisplay = qubitObj.GetComponent<SpriteRangeDisplay>();
        Debug.Log($"SpriteRangeDisplay: {rangeDisplay != null}");
        
        Debug.Log("=== ANALYSIS COMPLETE ===");
    }
    
    /// <summary>
    /// Force fix a qubit that's stuck in preview mode
    /// </summary>
    public static void ForceFixQubitPreviewMode(GameObject qubitObj)
    {
        if (qubitObj == null)
        {
            Debug.LogError("Cannot fix null qubit");
            return;
        }
        
        Debug.Log($"🔧 Force fixing qubit: {qubitObj.name}");
        
        // Step 1: Fix all Qubit components
        Qubit[] qubits = qubitObj.GetComponentsInChildren<Qubit>();
        foreach (Qubit qubit in qubits)
        {
            qubit.SetPreviewMode(false);
            Debug.Log($"Set preview mode FALSE for: {qubit.gameObject.name}");
        }
        
        // Step 2: Fix tag
        qubitObj.tag = "Qubit";
        
        // Step 3: Fix layer
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer == -1) defaultLayer = 0;
        SetLayerRecursively(qubitObj, defaultLayer);
        
        // Step 4: Fix name
        string cleanName = qubitObj.name.Replace("_PREVIEW", "").Replace("(Clone)", "").Replace("Preview", "");
        qubitObj.name = cleanName;
        
        // Step 5: Enable all colliders
        Collider2D[] colliders = qubitObj.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
        
        // Step 6: Fix sprite opacity
        SpriteRenderer[] renderers = qubitObj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains("Range"))
                continue;
                
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
        
        // Step 7: Fix range display
        SpriteRangeDisplay rangeDisplay = qubitObj.GetComponent<SpriteRangeDisplay>();
        if (rangeDisplay != null)
        {
            rangeDisplay.SetPreviewMode(false);
        }
        
        Debug.Log($"✅ Fixed qubit: {qubitObj.name}");
    }
    
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}