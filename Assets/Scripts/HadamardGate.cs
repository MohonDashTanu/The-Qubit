using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HadamardGate : MonoBehaviour
{
    [Header("Hadamard Gate Settings")]
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float radiusPerLevel = 1f;
    [SerializeField] private int baseMaxQubits = 3;
    [SerializeField] private int maxQubitsPerLevel = 2;
    [SerializeField] private float activationDuration = 2f; // Short activation effect only
    
    [Header("Superposition Effects")]
    [SerializeField] private float baseZeroQubitBoost = 0.5f; // 50% base boost
    [SerializeField] private float maxZeroQubitBoost = 1.0f; // 100% max boost  
    [SerializeField] private float baseOneQubitSpeedBoost = 0.1f; // 10% base boost
    [SerializeField] private float maxOneQubitSpeedBoost = 0.2f; // 20% max boost
    [SerializeField] private float baseTransformChance = 0.05f; // 5% base chance
    [SerializeField] private float transformChancePerLevel = 0.01f; // +1% per level
    
    [Header("Superposition Qubit")]
    [SerializeField] private GameObject superpositionQubitPrefab; // Assign your asset here
    
    [Header("Visual Effects")]
    [SerializeField] private Color superpositionColor = new Color(0.5f, 1f, 0.5f, 0.3f);
    [SerializeField] private Color rangeColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    [SerializeField] private float calibrationFactor = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip transformationSound;
    [SerializeField] private AudioClip superpositionHumSound;
    
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
    /// Activate the Hadamard gate and apply effects immediately
    /// </summary>
    public void ActivateGate(Vector3 position, int level)
    {
        gateLevel = level;
        transform.position = position;
        
        currentRadius = baseRadius + (radiusPerLevel * (level - 1));
        currentMaxQubits = baseMaxQubits + (maxQubitsPerLevel * (level - 1));
        
        Debug.Log($"🌀 Hadamard Gate Level {level} activated! Radius: {currentRadius}, Max Qubits: {currentMaxQubits}");
        
        isInPreviewMode = false;
        isActive = true;
        
        // Show range indicator only during activation
        UpdateRangeVisualization();
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }
        
        // Apply effects immediately
        ApplySuperpositionEffects();
        
        // Play activation sound
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Start SHORT activation sequence - no long duration
        StartCoroutine(ShortActivationSequence());
    }
    
    /// <summary>
    /// Short activation effect then immediately destroy
    /// </summary>
    private IEnumerator ShortActivationSequence()
    {
        // Brief visual effect during activation
        StartCoroutine(PulseRangeVisualization());
        
        // Play hum sound during activation
        if (superpositionHumSound != null && audioSource != null)
        {
            audioSource.clip = superpositionHumSound;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
        
        // Wait for short activation duration
        yield return new WaitForSeconds(activationDuration);
        
        Debug.Log("✨ Hadamard Gate activation complete - effects applied permanently");
        DeactivateGate();
    }
    
    /// <summary>
    /// Apply superposition effects to qubits in range
    /// </summary>
    private void ApplySuperpositionEffects()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentRadius);
        List<Qubit> validQubits = new List<Qubit>();
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Qubit"))
            {
                Qubit qubit = collider.GetComponent<Qubit>();
                if (qubit != null && CanApplySuperposition(qubit))
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
        
        Debug.Log($"🎯 Processed {qubitsToAffect} qubits out of {validQubits.Count} in range");
    }
    
    /// <summary>
    /// Check if we can apply superposition to this qubit
    /// </summary>
    private bool CanApplySuperposition(Qubit qubit)
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
        
        // Can't affect superposition qubits
        SuperpositionQubit superQubit = qubit.GetComponent<SuperpositionQubit>();
        if (superQubit != null) return false;
        
        // Check if qubit has superposition resistance flag
        if (HasSuperpositionEffect(qubit)) return false;
        
        return true;
    }
    
    /// <summary>
    /// Process a single qubit for superposition effects - FIXED VERSION
    /// </summary>
    private void ProcessQubit(Qubit qubit)
    {
        if (qubit == null) return;
        
        // Add to processed list to prevent multiple applications
        processedQubits.Add(qubit);
        
        // Check what type of qubit this is - FIXED: Check for ANY qubit type
        OneQubit oneQubit = qubit.GetComponent<OneQubit>();
        ZeroQubit zeroQubit = qubit.GetComponent<ZeroQubit>();
        
        // Roll for transformation first - applies to BOTH types
        float transformChance = baseTransformChance + (transformChancePerLevel * (gateLevel - 1));
        float roll = Random.Range(0f, 1f);
        
        Debug.Log($"🎲 Transformation roll for {qubit.name}: {roll:F3} vs {transformChance:F3}");
        
        if (roll < transformChance)
        {
            // TRANSFORMATION! - Works for both OneQubit AND ZeroQubit
            TransformToSuperpositionQubit(qubit);
        }
        else
        {
            // Apply appropriate boost based on qubit type
            if (oneQubit != null)
            {
                ApplySpeedBoostToOneQubit(qubit);
            }
            else if (zeroQubit != null)
            {
                ApplyResourceBoostToZeroQubit(qubit);
            }
            else
            {
                // Generic qubit - treat as One Qubit
                ApplySpeedBoostToOneQubit(qubit);
            }
        }
    }
    
    /// <summary>
    /// Transform ANY Qubit into Superposition Qubit - FIXED VERSION
    /// </summary>
    private void TransformToSuperpositionQubit(Qubit originalQubit)
    {
        if (superpositionQubitPrefab == null)
        {
            Debug.LogError("❌ No Superposition Qubit prefab assigned!");
            // Apply fallback boost instead
            OneQubit oneQubit = originalQubit.GetComponent<OneQubit>();
            if (oneQubit != null)
            {
                ApplySpeedBoostToOneQubit(originalQubit);
            }
            else
            {
                ApplyResourceBoostToZeroQubit(originalQubit);
            }
            return;
        }
        
        Vector3 position = originalQubit.transform.position;
        Quaternion rotation = originalQubit.transform.rotation;
        
        Debug.Log($"✨ TRANSFORMATION! {originalQubit.name} → Superposition Qubit!");
        
        // Play transformation sound
        if (transformationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transformationSound);
        }
        
        // Create superposition qubit
        GameObject newSuperQubit = Instantiate(superpositionQubitPrefab, position, rotation);
        newSuperQubit.tag = "Qubit";
        
        // Configure the new qubit
        Qubit newQubitComponent = newSuperQubit.GetComponent<Qubit>();
        if (newQubitComponent != null)
        {
            newQubitComponent.SetPreviewMode(false);
            newQubitComponent.SetGridPosition(position);
        }
        
        // Handle grid management
        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.FreeCell(position);
            gridManager.OccupyCell(position, newSuperQubit);
        }
        
        // CRITICAL: Use ReplaceQubit to maintain count
        QubitManager qubitManager = QubitManager.Instance;
        if (qubitManager != null)
        {
            qubitManager.ReplaceQubit(originalQubit.gameObject, newSuperQubit);
        }
        
        // Destroy original
        Destroy(originalQubit.gameObject);
        
        // Show transformation effect
        StartCoroutine(ShowTransformationEffect(position));
    }
    
    /// <summary>
    /// Apply permanent speed boost to One Qubit
    /// </summary>
    private void ApplySpeedBoostToOneQubit(Qubit qubit)
    {
        // Calculate speed boost based on gate level
        float speedBoost = Mathf.Lerp(baseOneQubitSpeedBoost, maxOneQubitSpeedBoost, 
            (float)(gateLevel - 1) / 9f); // Assuming max level 10
        
        // Apply permanent superposition effect flag
        SuperpositionEffect effect = qubit.gameObject.AddComponent<SuperpositionEffect>();
        effect.Initialize(SuperpositionEffect.EffectType.SpeedBoost, speedBoost, true, gateLevel);
        
        Debug.Log($"⚡ Applied {speedBoost * 100:F1}% permanent speed boost to {qubit.name}");
        
        // Start visual shimmer effect
        StartCoroutine(ApplyPermanentShimmer(qubit, superpositionColor));
    }
    
    /// <summary>
    /// Apply permanent resource boost to Zero Qubit
    /// </summary>
    private void ApplyResourceBoostToZeroQubit(Qubit qubit)
    {
        // Calculate resource boost based on gate level
        float resourceBoost = Mathf.Lerp(baseZeroQubitBoost, maxZeroQubitBoost, 
            (float)(gateLevel - 1) / 9f); // Assuming max level 10
        
        // Apply permanent superposition effect flag
        SuperpositionEffect effect = qubit.gameObject.AddComponent<SuperpositionEffect>();
        effect.Initialize(SuperpositionEffect.EffectType.ResourceBoost, resourceBoost, true, gateLevel);
        
        Debug.Log($"💎 Applied {resourceBoost * 100:F1}% permanent resource boost to {qubit.name}");
        
        // Start visual shimmer effect
        StartCoroutine(ApplyPermanentShimmer(qubit, new Color(0.3f, 0.7f, 1f, 0.8f))); // Blue for resource
    }
    
    /// <summary>
    /// Check if qubit already has superposition effects
    /// </summary>
    private bool HasSuperpositionEffect(Qubit qubit)
    {
        return qubit.GetComponent<SuperpositionEffect>() != null;
    }
    
    /// <summary>
    /// Apply permanent shimmer effect to enhanced qubits
    /// </summary>
    private IEnumerator ApplyPermanentShimmer(Qubit qubit, Color shimmerColor)
    {
        SpriteRenderer qubitRenderer = qubit.GetComponent<SpriteRenderer>();
        if (qubitRenderer == null)
            qubitRenderer = qubit.GetComponentInChildren<SpriteRenderer>();
            
        if (qubitRenderer == null) yield break;
        
        Color baseColor = qubitRenderer.color;
        
        while (qubit != null && !isInPreviewMode)
        {
            // Check if qubit is in preview mode using reflection
            bool isInPreview = false;
            System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (previewField != null)
            {
                isInPreview = (bool)previewField.GetValue(qubit);
            }
            
            if (isInPreview) break;
            
            float time = Time.time * 2f;
            float shimmer = Mathf.Sin(time) * 0.3f + 0.7f;
            Color blendedColor = Color.Lerp(baseColor, shimmerColor, 0.4f);
            blendedColor.a = baseColor.a * shimmer;
            
            qubitRenderer.color = blendedColor;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Show transformation effect
    /// </summary>
    private IEnumerator ShowTransformationEffect(Vector3 position)
    {
        // Create a bright flash effect
        GameObject effectObj = new GameObject("TransformationEffect");
        effectObj.transform.position = position;
        
        SpriteRenderer effectRenderer = effectObj.AddComponent<SpriteRenderer>();
        
        // Create a bright circle texture
        Texture2D flashTexture = CreateCircleTexture(128, 64);
        Sprite flashSprite = Sprite.Create(flashTexture, new Rect(0, 0, 128, 128), Vector2.one * 0.5f, 100f);
        effectRenderer.sprite = flashSprite;
        
        effectRenderer.color = Color.white;
        effectRenderer.sortingOrder = 10;
        
        // Animate the effect
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(0.5f, 3f, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            effectObj.transform.localScale = Vector3.one * scale;
            effectRenderer.color = new Color(1f, 1f, 1f, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(effectObj);
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
            float pulseValue = Mathf.Sin(progress * Mathf.PI * 4); // 4 pulses during activation
            
            float scaleMultiplier = 1f + (pulseValue * 0.2f);
            rangeIndicator.transform.localScale = originalScale * scaleMultiplier;
            
            Color currentColor = originalColor;
            currentColor.a = originalColor.a * (0.8f + pulseValue * 0.2f);
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
        
        // Hide range visualization immediately
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
        
        // Effects are permanent - just destroy the gate object
        Destroy(gameObject);
    }
    
    public float GetCurrentRadius()
    {
        return currentRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float radius = Application.isPlaying ? currentRadius : baseRadius;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        if (Application.isPlaying && isActive)
        {
            Gizmos.color = Color.yellow;
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