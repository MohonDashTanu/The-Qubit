using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PauliYGate : MonoBehaviour
{
    [Header("Pauli-Y Gate Settings")]
    [SerializeField] private float baseRadius = 4f;
    [SerializeField] private float radiusPerLevel = 1f;
    [SerializeField] private int baseMaxQubits = 4;
    [SerializeField] private int maxQubitsPerLevel = 2;
    [SerializeField] private float activationDuration = 2f; // Short activation only
    [SerializeField] private float confusionDuration = 20f; // How long qubits stay confused

    [Header("Visual Effects")]
    [SerializeField] private Color confusionColor = new Color(1f, 0.5f, 1f, 0.3f); // Purple/magenta for Y gate
    [SerializeField] private Color rangeColor = new Color(1f, 0.5f, 1f, 0.2f);
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    [SerializeField] private float calibrationFactor = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip confusionSound;
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
    /// Activate the Pauli-Y gate and apply confusion effects immediately
    /// </summary>
    public void ActivateGate(Vector3 position, int level)
    {
        gateLevel = level;
        transform.position = position;

        currentRadius = baseRadius + (radiusPerLevel * (level - 1));
        currentMaxQubits = baseMaxQubits + (maxQubitsPerLevel * (level - 1));

        Debug.Log($"🌀 Pauli-Y Gate Level {level} activated! Radius: {currentRadius}, Max Qubits: {currentMaxQubits}");

        isInPreviewMode = false;
        isActive = true;

        // Show range indicator only during activation
        UpdateRangeVisualization();
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(true);
        }

        // Apply confusion effects immediately
        ApplyConfusionEffects();

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

        Debug.Log("🌀 Pauli-Y Gate confusion applied - effects continue independently");
        DeactivateGate();
    }

    /// <summary>
    /// Apply confusion effects to qubits in range
    /// </summary>
    private void ApplyConfusionEffects()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentRadius);
        List<Qubit> validQubits = new List<Qubit>();

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Qubit"))
            {
                Qubit qubit = collider.GetComponent<Qubit>();
                if (qubit != null && CanApplyConfusion(qubit))
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

        Debug.Log($"🌀 Confused {qubitsToAffect} qubits out of {validQubits.Count} in range");
    }

    /// <summary>
    /// Check if we can apply confusion to this qubit
    /// </summary>
    private bool CanApplyConfusion(Qubit qubit)
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

        // Can affect any type of qubit (including SuperpositionQubits)
        ZeroQubit zeroQubit = qubit.GetComponent<ZeroQubit>();
        OneQubit oneQubit = qubit.GetComponent<OneQubit>();
        SuperpositionQubit superQubit = qubit.GetComponent<SuperpositionQubit>();

        // Check if already confused
        ConfusedQubit existingConfusion = qubit.GetComponent<ConfusedQubit>();
        if (existingConfusion != null) return false; // Already confused

        return (zeroQubit != null || oneQubit != null || superQubit != null);
    }

    /// <summary>
    /// Process a single qubit for confusion effects
    /// </summary>
    private void ProcessQubit(Qubit qubit)
    {
        if (qubit == null) return;

        // Add to processed list to prevent multiple applications
        processedQubits.Add(qubit);

        // Apply confusion effect by adding ConfusedQubit component
        ApplyConfusionToQubit(qubit);
    }

    /// <summary>
    /// Apply confusion effect to a qubit by adding ConfusedQubit component - FIXED VERSION
    /// </summary>
    private void ApplyConfusionToQubit(Qubit qubit)
    {
        Debug.Log($"🌀 CONFUSION! Applying chaos to {qubit.name}");

        // Play confusion sound
        if (confusionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(confusionSound);
        }

        // Add ConfusedQubit component
        ConfusedQubit confusedComponent = qubit.gameObject.AddComponent<ConfusedQubit>();

        // Start confusion effect immediately with duration - FIXED: Pass duration parameter
        confusedComponent.SetConfusionState(true, confusionDuration);

        // Show confusion effect
        StartCoroutine(ShowConfusionEffect(qubit.transform.position));

        // NOTE: No longer need RemoveConfusionAfterDuration coroutine
        // The ConfusedQubit now handles its own duration internally
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
            float pulseValue = Mathf.Sin(progress * Mathf.PI * 5); // 5 pulses for chaos effect

            float scaleMultiplier = 1f + (pulseValue * 0.25f);
            rangeIndicator.transform.localScale = originalScale * scaleMultiplier;

            Color currentColor = originalColor;
            currentColor.a = originalColor.a * (0.6f + pulseValue * 0.4f);
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

        // Confusion effects continue on their own via ConfusedQubit component
        Destroy(gameObject);
    }

    public float GetCurrentRadius()
    {
        return currentRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
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

    /// <summary>
    /// Show swirling confusion effect when applying confusion to a qubit
    /// </summary>
    private IEnumerator ShowConfusionEffect(Vector3 position)
    {
        // Create a swirling confusion effect
        GameObject effectObj = new GameObject("ConfusionEffect");
        effectObj.transform.position = position;

        SpriteRenderer effectRenderer = effectObj.AddComponent<SpriteRenderer>();

        // Create a swirling spiral texture
        Texture2D confusionTexture = CreateSpiralTexture(64, confusionColor);
        Sprite confusionSprite = Sprite.Create(confusionTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 100f);
        effectRenderer.sprite = confusionSprite;

        effectRenderer.color = confusionColor;
        effectRenderer.sortingOrder = 10;

        // Animate the effect - swirling and color changing
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            // Spin the effect rapidly
            float rotation = progress * 1080f; // Three full rotations
            effectObj.transform.rotation = Quaternion.Euler(0, 0, rotation);

            // Scale and pulse
            float scale = 0.5f + Mathf.Sin(progress * Mathf.PI * 6) * 0.3f; // 6 pulses
            effectObj.transform.localScale = Vector3.one * scale;

            // Color cycling effect
            float hue = (progress * 3f) % 1f; // Cycle through colors 3 times
            Color currentColor = Color.HSVToRGB(hue, 0.8f, 1f);
            currentColor.a = Mathf.Lerp(1f, 0f, progress); // Fade out
            effectRenderer.color = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(effectObj);
    }

    /// <summary>
    /// Create a spiral texture for the confusion effect
    /// </summary>
    private Texture2D CreateSpiralTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2, size / 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 fromCenter = pos - center;

                float distance = fromCenter.magnitude;
                float angle = Mathf.Atan2(fromCenter.y, fromCenter.x);

                // Create spiral pattern
                float spiral = Mathf.Sin(angle * 3 + distance * 0.5f);

                if (distance < size / 2 && spiral > 0.3f)
                {
                    float intensity = 1f - (distance / (size / 2));
                    colors[y * size + x] = new Color(1f, 1f, 1f, intensity);
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
}