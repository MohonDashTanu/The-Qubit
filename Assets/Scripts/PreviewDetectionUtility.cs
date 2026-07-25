using UnityEngine;

/// <summary>
/// Centralized utility for detecting if a GameObject is a preview object
/// Used by enemies to avoid attacking preview qubits
/// </summary>
public static class PreviewDetectionUtility
{
    /// <summary>
    /// Comprehensive check to determine if a GameObject is a preview object
    /// </summary>
    /// <param name="obj">The GameObject to check</param>
    /// <returns>True if the object is a preview, false if it's a real game object</returns>
    public static bool IsPreviewObject(GameObject obj)
    {
        if (obj == null) return true; // Treat null as preview to be safe
        
        // Check 1: Preview tag
        if (obj.CompareTag("PreviewQubit"))
        {
            return true;
        }
        
        // Check 2: Name contains preview indicators
        string name = obj.name.ToLower();
        if (name.Contains("_preview") || name.Contains("preview") || 
            (name.Contains("(clone)") && name.Contains("preview")))
        {
            return true;
        }
        
        // Check 3: Preview layer
        if (obj.layer == LayerMask.NameToLayer("Preview"))
        {
            return true;
        }
        
        // Check 4: Qubit component in preview mode
        Qubit qubitComponent = obj.GetComponent<Qubit>();
        if (qubitComponent != null)
        {
            if (IsQubitInPreviewMode(qubitComponent))
            {
                return true;
            }
        }

        // Check 4.5: Exclude confused qubits (they're real, just visually different)
        ConfusedQubit confusedComponent = obj.GetComponent<ConfusedQubit>();
        if (confusedComponent != null)
        {
            // Confused qubits are real qubits, not previews
            return false;
        }
        
        // Check 5: All colliders disabled (strong indicator of preview)
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
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
                return true;
            }
        }
        
        // Check 6: Semi-transparent sprite (preview objects are often semi-transparent)
        // BUT exclude confused qubits which change colors
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.color.a < 0.9f)
        {
            // Check if this is a confused qubit (they change colors but aren't previews)
            ConfusedQubit confusedCheck = obj.GetComponent<ConfusedQubit>();
            if (confusedCheck == null) // Only treat as preview if NOT confused
            {
                return true;
            }
        }
        
        // Check 7: Parent has preview indicators
        if (obj.transform.parent != null)
        {
            string parentName = obj.transform.parent.name.ToLower();
            if (parentName.Contains("preview") || parentName.Contains("_preview"))
            {
                return true;
            }
        }
        
        // Check 8: PreviewMarker component
        if (obj.GetComponent<PreviewMarker>() != null)
        {
            return true;
        }
        
        // If none of the preview indicators are found, it's a real object
        return false;
    }
    
    /// <summary>
    /// Check if a Qubit component is in preview mode using reflection
    /// </summary>
    /// <param name="qubit">The Qubit component to check</param>
    /// <returns>True if in preview mode</returns>
    public static bool IsQubitInPreviewMode(Qubit qubit)
    {
        if (qubit == null) return true; // Treat null as preview to be safe
        
        // Use reflection to access the protected isInPreviewMode field
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (previewField != null)
        {
            return (bool)previewField.GetValue(qubit);
        }
        
        return false; // If we can't determine, assume it's real
    }
    
    /// <summary>
    /// Filter a list of GameObjects to remove all preview objects
    /// </summary>
    /// <param name="objects">Array of GameObjects to filter</param>
    /// <returns>Array containing only real (non-preview) objects</returns>
    public static GameObject[] FilterOutPreviewObjects(GameObject[] objects)
    {
        if (objects == null) return new GameObject[0];
        
        System.Collections.Generic.List<GameObject> realObjects = new System.Collections.Generic.List<GameObject>();
        
        foreach (GameObject obj in objects)
        {
            if (!IsPreviewObject(obj))
            {
                realObjects.Add(obj);
            }
        }
        
        return realObjects.ToArray();
    }
    
    /// <summary>
    /// Find all real qubits in the scene (excludes preview qubits)
    /// </summary>
    /// <returns>Array of real qubit GameObjects</returns>
    public static GameObject[] FindRealQubits()
    {
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        return FilterOutPreviewObjects(allQubits);
    }
    
    /// <summary>
    /// Debug method to log why an object is considered a preview
    /// </summary>
    /// <param name="obj">The GameObject to analyze</param>
    public static void DebugPreviewStatus(GameObject obj)
    {
        if (obj == null)
        {
            Debug.Log("Object is null - treated as preview");
            return;
        }
        
        Debug.Log($"=== Preview Analysis for {obj.name} ===");
        
        if (obj.CompareTag("PreviewQubit"))
        {
            Debug.Log("✓ Has PreviewQubit tag");
        }
        
        if (obj.name.ToLower().Contains("preview"))
        {
            Debug.Log("✓ Name contains 'preview'");
        }
        
        if (obj.layer == LayerMask.NameToLayer("Preview"))
        {
            Debug.Log("✓ On Preview layer");
        }
        
        Qubit qubit = obj.GetComponent<Qubit>();
        if (qubit != null && IsQubitInPreviewMode(qubit))
        {
            Debug.Log("✓ Qubit component is in preview mode");
        }
        
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
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
                Debug.Log("✓ All colliders are disabled");
            }
        }
        
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null && sr.color.a < 0.9f)
        {
            Debug.Log($"✓ Semi-transparent sprite (alpha: {sr.color.a})");
        }
        
        bool isPreview = IsPreviewObject(obj);
        Debug.Log($"Final result: {(isPreview ? "PREVIEW" : "REAL")} object");
    }
}