using UnityEngine;

// Add this script to disable the attack range visualization during preview
[RequireComponent(typeof(SpriteRenderer))]
public class PreviewRangeDisabler : MonoBehaviour
{
    [SerializeField] private bool checkParentPreview = true;
    private SpriteRenderer rangeRenderer;
    
    private void Awake()
    {
        rangeRenderer = GetComponent<SpriteRenderer>();
        
        // Check if we're a preview or child of a preview
        bool isPreview = IsPreviewObject();
        
        if (isPreview)
        {
            // Disable the range visualization in preview
            rangeRenderer.enabled = false;
            
            // Also disable any additional effects
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }
        }
    }
    
    private bool IsPreviewObject()
    {
        // Check self for preview hints
        if (gameObject.layer == LayerMask.NameToLayer("Preview"))
            return true;
            
        // Check transparency
        if (rangeRenderer != null && rangeRenderer.color.a < 0.9f)
            return true;
            
        // Check name
        if (gameObject.name.Contains("Preview"))
            return true;
            
        // Check if parent is a preview (if enabled)
        if (checkParentPreview && transform.parent != null)
        {
            // Check parent's transparency
            SpriteRenderer parentRenderer = transform.parent.GetComponent<SpriteRenderer>();
            if (parentRenderer != null && parentRenderer.color.a < 0.9f)
                return true;
                
            // Check parent's components
            Qubit parentQubit = transform.parent.GetComponent<Qubit>();
            if (parentQubit != null)
            {
                // Try to check isInPreviewMode using reflection
                System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                if (previewField != null)
                {
                    bool parentIsPreview = (bool)previewField.GetValue(parentQubit);
                    return parentIsPreview;
                }
                
                // If reflection fails, check parent name
                if (transform.parent.name.Contains("(Clone)") || transform.parent.name.Contains("Preview"))
                    return true;
            }
        }
        
        return false;
    }
}