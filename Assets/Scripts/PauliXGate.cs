using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PauliXGate : MonoBehaviour
{
    [Header("Pauli-X Gate Settings")]
    [SerializeField] private float baseRadius = 4f;
    [SerializeField] private float radiusPerLevel = 1f;
    [SerializeField] private int baseMaxQubits = 5;
    [SerializeField] private int maxQubitsPerLevel = 2;
    [SerializeField] private float activationDuration = 1.5f; // Short activation only
    
    [Header("Transformation Prefabs")]
    [SerializeField] private GameObject zeroQubitPrefab; // Assign your ZeroQubit prefab
    [SerializeField] private GameObject oneQubitPrefab;  // Assign your OneQubit prefab
    
    [Header("Visual Effects")]
    [SerializeField] private Color flipColor = new Color(0.2f, 0.5f, 1f, 0.3f); // Blue color for X gate
    [SerializeField] private Color rangeColor = new Color(0.2f, 0.5f, 1f, 0.2f);
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    [SerializeField] private float calibrationFactor = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip flipSound;
    [SerializeField] private AudioClip gateHumSound;
    
    // Runtime variables
    private int gateLevel = 1;
    private float currentRadius;
    private int currentMaxQubits;
    private List<Qubit> processedQubits = new List<Qubit>();
    private GameObject rangeIndicator;
    private SpriteRenderer rangeRenderer;
    private AudioSource audioSource;
    private bool isActive = false;
    private bool isInPreviewMode = false;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        CreateRangeVisualization();
    }
    
    private void CreateRangeVisualization()
    {
        Transform existingRange = transform.Find("GateRange");
        
        if (existingRange != null)
        {
            rangeIndicator = existingRange.gameObject;
            rangeRenderer = rangeIndicator.GetComponent<SpriteRenderer>();
            return;
        }
        
        rangeIndicator = new GameObject("GateRange");
        rangeIndicator.transform.SetParent(transform);
        rangeIndicator.transform.localPosition = Vector3.zero;
        
        rangeRenderer = rangeIndicator.AddComponent<SpriteRenderer>();
        
        Texture2D texture = CreateCircleTexture(256, 128);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
        rangeRenderer.sprite = sprite;
        
        rangeRenderer.color = rangeColor;
        rangeRenderer.sortingLayerName = "Object";
        rangeRenderer.sortingOrder = -1;
        
        rangeIndicator.SetActive(false);
    }
    
    private Texture2D CreateCircleTexture(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2, size / 2);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                
                if (distance < radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else if (distance < radius + 1)
                {
                    float t = distance - radius;
                    colors[y * size + x] = new Color(1, 1, 1, 1 - t);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return texture;
    }
    
    public void SetPreviewMode(bool isPreview)
    {
        isInPreviewMode = isPreview;
        
        if (isPreview)
        {
            currentRadius = baseRadius + (radiusPerLevel * (gateLevel - 1));
        }
        
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(isPreview);
            UpdateRangeVisualization();
        }
    }
    
    public void SetPreviewLevel(int level)
    {
        gateLevel = level;
        
        if (isInPreviewMode)
        {
            currentRadius = baseRadius + (radiusPerLevel * (level - 1));
            UpdateRangeVisualization();
        }
    }
    
    public void SetPlacementValidity(bool isValid)
    {
        if (!isInPreviewMode || rangeRenderer == null)
            return;
            
        rangeRenderer.color = isValid ? 
            new Color(0f, 1f, 0f, 0.3f) :
            new Color(1f, 0f, 0f, 0.3f);
    }
    
    private void UpdateRangeVisualization()
    {
        if (rangeIndicator == null || rangeRenderer == null)
            return;
            
        float radiusToVisualize = isInPreviewMode ? 
            (baseRadius + (radiusPerLevel * (gateLevel - 1))) :
            currentRadius;
            
        float scale = radiusToVisualize * 2f * calibrationFactor;
        
        rangeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
        
        if (isInPreviewMode)
        {
            rangeRenderer.color = new Color(0f, 1f, 0f, 0.3f);
        }
        else
        {
            rangeRenderer.color = rangeColor;
        }
    }
    
    /// <summary>
    /// Activate the Pauli-X gate and apply bit flip effects immediately
    /// </summary>
    public void ActivateGate(Vector3 position, int level)
    {
        gateLevel = level;
        transform.position = position;
        
        currentRadius = baseRadius + (radiusPerLevel * (level - 1));
        currentMaxQubits = baseMaxQubits + (maxQubitsPerLevel * (level - 1));
        
        Debug.Log($"🔄 Pauli-X Gate Level {level} activated! Radius: {currentRadius}, Max Qubits: {currentMaxQubits}");
        
        isInPreviewMode = false;
        isActive = true;
        
        // Show range indicator only during activation
        UpdateRangeVisualization();
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }
        
        // Apply bit flip effects immediately
        ApplyBitFlipEffects();
        
        // Play activation sound
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Start SHORT activation sequence
        StartCoroutine(ShortActivationSequence());
    }
    
    /// <summary>
    /// Short activation effect then immediately destroy
    /// </summary>
    private IEnumerator ShortActivationSequence()
    {
        // Brief visual effect
        StartCoroutine(PulseRangeVisualization());
        
        // Play hum sound during activation
        if (gateHumSound != null && audioSource != null)
        {
            audioSource.clip = gateHumSound;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
        
        // Wait for short duration
        yield return new WaitForSeconds(activationDuration);
        
        Debug.Log("🔄 Pauli-X Gate transformations complete");
        DeactivateGate();
    }
    
    /// <summary>
    /// Apply bit flip effects to qubits in range - transforms Zero ↔ One
    /// </summary>
    private void ApplyBitFlipEffects()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentRadius);
        List<Qubit> validQubits = new List<Qubit>();
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Qubit"))
            {
                Qubit qubit = collider.GetComponent<Qubit>();
                if (qubit != null && CanApplyBitFlip(qubit))
                {
                    validQubits.Add(qubit);
                }
            }
        }
        
        // Sort by distance and take only the max allowed
        validQubits.Sort((a, b) => 
        {
            float distA = Vector2.Distance(transform.position, a.transform.position);
            float distB = Vector2.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        int qubitsToAffect = Mathf.Min(validQubits.Count, currentMaxQubits);
        
        for (int i = 0; i < qubitsToAffect; i++)
        {
            ProcessQubit(validQubits[i]);
        }
        
        Debug.Log($"🎯 Bit-flipped {qubitsToAffect} qubits out of {validQubits.Count} in range");
    }
    
    /// <summary>
    /// Check if we can apply bit flip to this qubit
    /// </summary>
    private bool CanApplyBitFlip(Qubit qubit)
    {
        // Can't affect preview qubits
        bool isInPreview = false;
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (previewField != null)
        {
            isInPreview = (bool)previewField.GetValue(qubit);
        }
        
        if (isInPreview) return false;
        
        // Can't affect qubits that have already been processed
        if (processedQubits.Contains(qubit)) return false;
        
        // Can affect SuperpositionQubits - they become random type
        SuperpositionQubit superQubit = qubit.GetComponent<SuperpositionQubit>();
        if (superQubit != null) return true;
        
        // Can affect ZeroQubits and OneQubits
        ZeroQubit zeroQubit = qubit.GetComponent<ZeroQubit>();
        OneQubit oneQubit = qubit.GetComponent<OneQubit>();
        
        return (zeroQubit != null || oneQubit != null);
    }
    
    /// <summary>
    /// Process a single qubit for bit flip transformation
    /// </summary>
    private void ProcessQubit(Qubit qubit)
    {
        if (qubit == null) return;
        
        // Add to processed list to prevent multiple applications
        processedQubits.Add(qubit);
        
        // Check what type of qubit this is
        ZeroQubit zeroQubit = qubit.GetComponent<ZeroQubit>();
        OneQubit oneQubit = qubit.GetComponent<OneQubit>();
        SuperpositionQubit superQubit = qubit.GetComponent<SuperpositionQubit>();
        
        if (zeroQubit != null)
        {
            // Transform Zero Qubit → One Qubit
            TransformQubit(qubit, oneQubitPrefab, "ZeroQubit", "OneQubit");
        }
        else if (oneQubit != null)
        {
            // Transform One Qubit → Zero Qubit
            TransformQubit(qubit, zeroQubitPrefab, "OneQubit", "ZeroQubit");
        }
        else if (superQubit != null)
        {
            // SuperpositionQubit becomes random type (50/50 chance)
            GameObject targetPrefab = Random.Range(0f, 1f) < 0.5f ? zeroQubitPrefab : oneQubitPrefab;
            string targetType = targetPrefab == zeroQubitPrefab ? "ZeroQubit" : "OneQubit";
            TransformQubit(qubit, targetPrefab, "SuperpositionQubit", targetType);
        }
        else
        {
            Debug.LogWarning($"Unknown qubit type for bit flip: {qubit.name}");
        }
    }
    
    /// <summary>
    /// Transform one qubit type to another
    /// </summary>
    private void TransformQubit(Qubit originalQubit, GameObject targetPrefab, string fromType, string toType)
    {
        if (targetPrefab == null)
        {
            Debug.LogError($"❌ No {toType} prefab assigned!");
            return;
        }
        
        Vector3 position = originalQubit.transform.position;
        Quaternion rotation = originalQubit.transform.rotation;
        int currentHealth = originalQubit.GetCurrentHealth();
        
        Debug.Log($"🔄 BIT FLIP! {fromType} → {toType} at {position}");
        
        // Play flip sound
        if (flipSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(flipSound);
        }
        
        // Store any superposition effects before destruction
        SuperpositionEffect existingEffect = originalQubit.GetComponent<SuperpositionEffect>();
        
        // STEP 1: Create new qubit
        GameObject newQubit = Instantiate(targetPrefab, position, rotation);
        newQubit.tag = "Qubit";
        
        // STEP 2: Configure the new qubit
        Qubit newQubitComponent = newQubit.GetComponent<Qubit>();
        if (newQubitComponent != null)
        {
            newQubitComponent.SetPreviewMode(false);
            newQubitComponent.SetGridPosition(position);
            
            // Preserve health percentage
            if (newQubitComponent.QubitData != null && originalQubit.QubitData != null)
            {
                float healthPercentage = (float)currentHealth / originalQubit.QubitData.maxHealth;
                int newHealth = Mathf.RoundToInt(newQubitComponent.QubitData.maxHealth * healthPercentage);
                
                // Use reflection to set health
                System.Reflection.FieldInfo healthField = typeof(Qubit).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (healthField != null)
                {
                    healthField.SetValue(newQubitComponent, Mathf.Max(1, newHealth));
                }
            }
        }
        
        // STEP 3: Transfer superposition effects
        if (existingEffect != null)
        {
            SuperpositionEffect newEffect = newQubit.AddComponent<SuperpositionEffect>();
            newEffect.Initialize(existingEffect.GetEffectType(), existingEffect.GetBoostAmount(), 
                existingEffect.IsPermanent(), existingEffect.GetGateLevel());
            Debug.Log($"✨ Transferred superposition effect to new {toType}");
        }
        
        // STEP 4: Handle grid management
        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.FreeCell(position);
            gridManager.OccupyCell(position, newQubit);
        }
        
        // STEP 5: CRITICAL - Use ReplaceQubit instead of destroy/add cycle
        QubitManager qubitManager = QubitManager.Instance;
        if (qubitManager != null)
        {
            // This maintains the count and transfers entanglements!
            qubitManager.ReplaceQubit(originalQubit.gameObject, newQubit);
        }
        else
        {
            Debug.LogError("❌ QubitManager not found during qubit transformation!");
        }
        
        // STEP 6: Destroy original qubit (safe now that it's been replaced)
        Destroy(originalQubit.gameObject);
        
        // STEP 7: Show transformation effect
        StartCoroutine(ShowBitFlipEffect(position));
        
        Debug.Log($"🎯 Successfully transformed {fromType} → {toType}. Count maintained!");
    }
    
    /// <summary>
    /// Show bit flip transformation effect
    /// </summary>
    private IEnumerator ShowBitFlipEffect(Vector3 position)
    {
        // Create a bit flip effect - blue spinning effect
        GameObject effectObj = new GameObject("BitFlipEffect");
        effectObj.transform.position = position;
        
        SpriteRenderer effectRenderer = effectObj.AddComponent<SpriteRenderer>();
        
        // Create a spinning square texture for the X gate effect
        Texture2D flipTexture = CreateSquareTexture(64, flipColor);
        Sprite flipSprite = Sprite.Create(flipTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 100f);
        effectRenderer.sprite = flipSprite;
        
        effectRenderer.color = flipColor;
        effectRenderer.sortingOrder = 10;
        
        // Animate the effect - spinning and scaling
        float duration = 0.8f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            
            // Spin the effect
            float rotation = progress * 720f; // Two full rotations
            effectObj.transform.rotation = Quaternion.Euler(0, 0, rotation);
            
            // Scale pulse
            float scale = 1f + Mathf.Sin(progress * Mathf.PI * 4) * 0.3f;
            effectObj.transform.localScale = Vector3.one * scale;
            
            // Fade out
            float alpha = Mathf.Lerp(1f, 0f, progress);
            Color currentColor = flipColor;
            currentColor.a = alpha;
            effectRenderer.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(effectObj);
    }
    
    /// <summary>
    /// Create a square texture for the X gate effect
    /// </summary>
    private Texture2D CreateSquareTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Create an X pattern
                bool isOnX = (Mathf.Abs(x - y) < 3) || (Mathf.Abs(x - (size - 1 - y)) < 3);
                
                if (isOnX)
                {
                    colors[y * size + x] = Color.white;
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// Brief pulse effect during activation only
    /// </summary>
    private IEnumerator PulseRangeVisualization()
    {
        if (rangeIndicator == null) yield break;
        
        float elapsed = 0f;
        Vector3 originalScale = rangeIndicator.transform.localScale;
        Color originalColor = rangeRenderer.color;
        
        while (elapsed < activationDuration && rangeIndicator != null)
        {
            float progress = elapsed / activationDuration;
            float pulseValue = Mathf.Sin(progress * Mathf.PI * 3); // 3 pulses
            
            float scaleMultiplier = 1f + (pulseValue * 0.15f);
            rangeIndicator.transform.localScale = originalScale * scaleMultiplier;
            
            Color currentColor = originalColor;
            currentColor.a = originalColor.a * (0.7f + pulseValue * 0.3f);
            rangeRenderer.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    private void DeactivateGate()
    {
        isActive = false;
        
        // Stop audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Hide range visualization
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
        
        // Transformations are instant and permanent
        Destroy(gameObject);
    }
    
    public float GetCurrentRadius()
    {
        return currentRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        float radius = Application.isPlaying ? currentRadius : baseRadius;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        if (Application.isPlaying && isActive)
        {
            Gizmos.color = Color.cyan;
            foreach (Qubit qubit in processedQubits)
            {
                if (qubit != null)
                {
                    Gizmos.DrawLine(transform.position, qubit.transform.position);
                }
            }
        }
    }
}