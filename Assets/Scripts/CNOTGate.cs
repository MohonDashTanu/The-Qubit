using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CNOTGate : MonoBehaviour
{
    [Header("CNOT Gate Settings")]
    [SerializeField] private float baseRadius = 6f;
    [SerializeField] private float radiusPerLevel = 1.5f;
    [SerializeField] private int baseMaxPairs = 1; // How many qubit pairs to entangle
    [SerializeField] private int maxPairsPerLevel = 1;
    [SerializeField] private float activationDuration = 3f; // Longer for strategic effect
    
    [Header("Entanglement Requirements")]
    [SerializeField] private float maxEntanglementDistance = 8f; // Max distance between qubits to entangle
    [SerializeField] private bool requireAdjacentQubits = false; // Set true if you want stricter requirements
    
    [Header("Visual Effects")]
    [SerializeField] private Color entanglementColor = new Color(0f, 1f, 1f, 0.4f); // Cyan for CNOT
    [SerializeField] private Color rangeColor = new Color(0f, 1f, 1f, 0.2f);
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    [SerializeField] private float calibrationFactor = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip entanglementSound;
    [SerializeField] private AudioClip gateHumSound;
    
    // Runtime variables
    private int gateLevel = 1;
    private float currentRadius;
    private int currentMaxPairs;
    private List<(Qubit, Qubit)> processedPairs = new List<(Qubit, Qubit)>();
    private GameObject rangeIndicator;
    private SpriteRenderer rangeRenderer;
    private AudioSource audioSource;
    private bool isActive = false;
    private bool isInPreviewMode = false;
    private QubitManager qubitManager;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        qubitManager = QubitManager.Instance;
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
    /// Activate the CNOT gate and create strategic entanglements immediately
    /// </summary>
    public void ActivateGate(Vector3 position, int level)
    {
        gateLevel = level;
        transform.position = position;
        
        currentRadius = baseRadius + (radiusPerLevel * (level - 1));
        currentMaxPairs = baseMaxPairs + (maxPairsPerLevel * (level - 1));
        
        Debug.Log($"🔗 CNOT Gate Level {level} activated! Radius: {currentRadius}, Max Pairs: {currentMaxPairs}");
        
        isInPreviewMode = false;
        isActive = true;
        
        // Show range indicator only during activation
        UpdateRangeVisualization();
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }
        
        // Apply entanglement effects immediately
        CreateStrategicEntanglements();
        
        // Play activation sound
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Start activation sequence
        StartCoroutine(ActivationSequence());
    }
    
    /// <summary>
    /// Activation effect then immediately destroy
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        // Visual effect during activation
        StartCoroutine(PulseRangeVisualization());
        
        // Play hum sound during activation
        if (gateHumSound != null && audioSource != null)
        {
            audioSource.clip = gateHumSound;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
        
        // Wait for activation duration
        yield return new WaitForSeconds(activationDuration);
        
        Debug.Log("🔗 CNOT Gate entanglements created - bonds are permanent");
        DeactivateGate();
    }
    
    /// <summary>
    /// Create strategic entanglements between qubits in range - MAIN FUNCTIONALITY
    /// </summary>
    private void CreateStrategicEntanglements()
    {
        if (qubitManager == null)
        {
            Debug.LogError("❌ QubitManager not found! CNOT gate cannot create entanglements.");
            return;
        }
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentRadius);
        List<Qubit> validQubits = new List<Qubit>();
        
        // Find all valid qubits in range
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Qubit"))
            {
                Qubit qubit = collider.GetComponent<Qubit>();
                if (qubit != null && CanEntangleQubit(qubit))
                {
                    validQubits.Add(qubit);
                }
            }
        }
        
        Debug.Log($"🎯 Found {validQubits.Count} valid qubits for entanglement");
        
        if (validQubits.Count < 2)
        {
            Debug.Log("❌ Need at least 2 qubits to create entanglement!");
            return;
        }
        
        // Create entanglement pairs strategically
        List<(Qubit, Qubit)> pairsToEntangle = SelectOptimalPairs(validQubits);
        
        int successfulEntanglements = 0;
        foreach (var (qubit1, qubit2) in pairsToEntangle)
        {
            if (CreateEntanglementPair(qubit1, qubit2))
            {
                processedPairs.Add((qubit1, qubit2));
                successfulEntanglements++;
                
                // Show entanglement effect
                StartCoroutine(ShowEntanglementEffect(qubit1.transform.position, qubit2.transform.position));
            }
        }
        
        Debug.Log($"✨ CNOT Gate successfully entangled {successfulEntanglements} qubit pairs!");
    }
    
    /// <summary>
    /// Check if a qubit can be entangled by this gate
    /// </summary>
    private bool CanEntangleQubit(Qubit qubit)
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
        
        // Any qubit type can be entangled
        return true;
    }
    
    /// <summary>
    /// Select optimal pairs for entanglement based on distance and strategy
    /// </summary>
    private List<(Qubit, Qubit)> SelectOptimalPairs(List<Qubit> validQubits)
    {
        List<(Qubit, Qubit)> selectedPairs = new List<(Qubit, Qubit)>();
        List<Qubit> availableQubits = new List<Qubit>(validQubits);
        
        // Create pairs up to the maximum allowed
        for (int pairCount = 0; pairCount < currentMaxPairs && availableQubits.Count >= 2; pairCount++)
        {
            (Qubit qubit1, Qubit qubit2) = FindBestPair(availableQubits);
            
            if (qubit1 != null && qubit2 != null)
            {
                selectedPairs.Add((qubit1, qubit2));
                availableQubits.Remove(qubit1);
                availableQubits.Remove(qubit2);
                
                Debug.Log($"📌 Selected pair: {qubit1.gameObject.name} ↔ {qubit2.gameObject.name}");
            }
        }
        
        return selectedPairs;
    }
    
    /// <summary>
    /// Find the best pair of qubits to entangle based on various criteria
    /// </summary>
    private (Qubit, Qubit) FindBestPair(List<Qubit> availableQubits)
    {
        Qubit bestQubit1 = null;
        Qubit bestQubit2 = null;
        float bestScore = float.MinValue;
        
        for (int i = 0; i < availableQubits.Count; i++)
        {
            for (int j = i + 1; j < availableQubits.Count; j++)
            {
                Qubit qubit1 = availableQubits[i];
                Qubit qubit2 = availableQubits[j];
                
                // Check if they're already entangled
                if (AreAlreadyEntangled(qubit1, qubit2))
                    continue;
                
                float distance = Vector3.Distance(qubit1.transform.position, qubit2.transform.position);
                
                // Skip if too far apart
                if (distance > maxEntanglementDistance)
                    continue;
                
                // Calculate pairing score (lower distance = better)
                float score = CalculatePairingScore(qubit1, qubit2, distance);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestQubit1 = qubit1;
                    bestQubit2 = qubit2;
                }
            }
        }
        
        return (bestQubit1, bestQubit2);
    }
    
    /// <summary>
    /// Calculate how good a pairing would be (higher = better)
    /// </summary>
    private float CalculatePairingScore(Qubit qubit1, Qubit qubit2, float distance)
    {
        float score = 0f;
        
        // Prefer closer qubits (inverse distance)
        score += (maxEntanglementDistance - distance) * 10f;
        
        // Bonus for different qubit types (strategic diversity)
        bool isQubit1Zero = qubit1.GetComponent<ZeroQubit>() != null;
        bool isQubit2Zero = qubit2.GetComponent<ZeroQubit>() != null;
        if (isQubit1Zero != isQubit2Zero) // Different types
        {
            score += 50f;
        }
        
        // Bonus for healthy qubits (avoid entangling damaged ones)
        float healthBonus = (qubit1.GetHealthPercentage() + qubit2.GetHealthPercentage()) * 25f;
        score += healthBonus;
        
        // Small random factor to avoid deterministic patterns
        score += Random.Range(-5f, 5f);
        
        return score;
    }
    
    /// <summary>
    /// Check if two qubits are already entangled using QubitManager
    /// </summary>
    private bool AreAlreadyEntangled(Qubit qubit1, Qubit qubit2)
    {
        if (qubitManager == null || qubitManager.Entanglements == null)
            return false;
        
        foreach (var entanglement in qubitManager.Entanglements)
        {
            if (entanglement.QubitSource == null || entanglement.QubitTarget == null)
                continue;
                
            if ((entanglement.QubitSource == qubit1 && entanglement.QubitTarget == qubit2) ||
                (entanglement.QubitSource == qubit2 && entanglement.QubitTarget == qubit1))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Create an entanglement pair using QubitManager's system
    /// </summary>
    private bool CreateEntanglementPair(Qubit qubit1, Qubit qubit2)
    {
        if (qubitManager == null)
            return false;
        
        // Use reflection to access the TryEntanglement method
        System.Reflection.MethodInfo tryEntanglementMethod = typeof(QubitManager).GetMethod("TryEntanglement", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (tryEntanglementMethod != null)
        {
            bool success = (bool)tryEntanglementMethod.Invoke(qubitManager, new object[] { qubit1, qubit2 });
            
            if (success)
            {
                Debug.Log($"🔗 Successfully entangled {qubit1.gameObject.name} ↔ {qubit2.gameObject.name}");
                
                // Play entanglement sound
                if (entanglementSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(entanglementSound);
                }
                
                return true;
            }
        }
        
        Debug.LogWarning($"❌ Failed to entangle {qubit1.gameObject.name} ↔ {qubit2.gameObject.name}");
        return false;
    }
    
    /// <summary>
    /// Show visual effect when entanglement is created
    /// </summary>
    private IEnumerator ShowEntanglementEffect(Vector3 pos1, Vector3 pos2)
    {
        // Create a lightning-like effect between the two positions
        GameObject effectObj = new GameObject("EntanglementEffect");
        Vector3 midpoint = (pos1 + pos2) / 2f;
        effectObj.transform.position = midpoint;
        
        LineRenderer lineRenderer = effectObj.AddComponent<LineRenderer>();
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = entanglementColor;
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.sortingOrder = 10;
        
        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);
        
        // Animate the effect
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            
            // Pulsing effect
            float pulse = Mathf.Sin(progress * Mathf.PI * 6) * 0.3f + 0.7f;
            lineRenderer.startWidth = 0.1f * pulse;
            lineRenderer.endWidth = 0.1f * pulse;
            
            // Color fade
            Color currentColor = entanglementColor;
            currentColor.a = Mathf.Lerp(1f, 0f, progress);
            lineMaterial.color = currentColor;
            
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
            float pulseValue = Mathf.Sin(progress * Mathf.PI * 6); // 6 pulses
            
            float scaleMultiplier = 1f + (pulseValue * 0.1f);
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
        
        // Entanglements are permanent and managed by QubitManager
        Destroy(gameObject);
    }
    
    public float GetCurrentRadius()
    {
        return currentRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float radius = Application.isPlaying ? currentRadius : baseRadius;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        if (Application.isPlaying && isActive)
        {
            Gizmos.color = Color.yellow;
            foreach (var (qubit1, qubit2) in processedPairs)
            {
                if (qubit1 != null && qubit2 != null)
                {
                    Gizmos.DrawLine(qubit1.transform.position, qubit2.transform.position);
                }
            }
        }
    }
}