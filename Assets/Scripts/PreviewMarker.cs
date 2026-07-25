using UnityEngine;

// Component to explicitly mark preview objects
// Add this to your preview prefabs in the QubitManager
public class PreviewMarker : MonoBehaviour
{
    [Header("Auto-Configuration")]
    [SerializeField] private bool applyToChildren = true;
    [SerializeField] private bool disableAllEffects = true;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool setTransparency = true;
    [SerializeField] private float previewAlpha = 0.7f;
    
    private void Awake()
    {
        // Mark self as preview
        MarkAsPreview(gameObject);
        
        // Mark all children as preview if enabled
        if (applyToChildren)
        {
            foreach (Transform child in transform)
            {
                MarkAsPreview(child.gameObject);
            }
        }
    }
    
    private void MarkAsPreview(GameObject obj)
    {
        // Find all renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Set transparency if enabled
            if (setTransparency)
            {
                // For sprite renderers
                if (renderer is SpriteRenderer spriteRenderer)
                {
                    Color color = spriteRenderer.color;
                    color.a = previewAlpha;
                    spriteRenderer.color = color;
                }
                // For mesh renderers, update their materials
                else
                {
                    foreach (Material material in renderer.materials)
                    {
                        if (material.HasProperty("_Color"))
                        {
                            Color color = material.color;
                            color.a = previewAlpha;
                            material.color = color;
                            
                            // Make sure transparency is enabled
                            material.SetFloat("_Mode", 3); // Transparent mode
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            material.SetInt("_ZWrite", 0);
                            material.renderQueue = 3000;
                            material.DisableKeyword("_ALPHATEST_ON");
                            material.EnableKeyword("_ALPHABLEND_ON");
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        }
                    }
                }
            }
        }
        
        // Disable colliders if enabled
        if (disableColliders)
        {
            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = false;
            }
            
            Collider[] colliders3D = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders3D)
            {
                collider.enabled = false;
            }
        }
        
        // Disable effects if enabled
        if (disableAllEffects)
        {
            // Disable particle systems
            ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }
            
            // Disable trail renderers
            TrailRenderer[] trails = obj.GetComponentsInChildren<TrailRenderer>();
            foreach (TrailRenderer trail in trails)
            {
                trail.enabled = false;
            }
            
            // Disable line renderers
            LineRenderer[] lines = obj.GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer line in lines)
            {
                line.enabled = false;
            }
        }
        
        // Tell Qubit components they're in preview mode
        Qubit qubit = obj.GetComponent<Qubit>();
        if (qubit != null)
        {
            qubit.SetPreviewMode(true);
        }
    }
}