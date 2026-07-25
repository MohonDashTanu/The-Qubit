using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuantumCollapseManager : MonoBehaviour
{
    [Header("Collapse Settings")]
    [SerializeField] private float baseStability = 0.9f; // 90% stability (10% collapse chance)
    [SerializeField] private float stabilityDecayPerExtraQubit = 0.1f; // -10% per extra qubit
    [SerializeField] private float minStability = 0.1f; // Never go below 10% stability (90% collapse max)
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI qubitCountText; // Shows "99/99" 
    [SerializeField] private TextMeshProUGUI riskText; // Shows "Risk: 80%"
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject collapseEffectPrefab; // Particle effect for collapse
    [SerializeField] private AudioClip collapseSound; // Sound effect
    [SerializeField] private Color safeColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool forceCollapseForTesting = false;
    
    // Runtime variables
    private QubitManager qubitManager;
    private AudioSource audioSource;
    private bool isInDangerZone = false;
    
    // Statistics
    private int totalCollapses = 0;
    private int qubitsLostToCollapse = 0;
    
    // Singleton for easy access
    public static QuantumCollapseManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        // Find QubitManager
        qubitManager = QubitManager.Instance;
        if (qubitManager == null)
        {
            Debug.LogError("QuantumCollapseManager: Could not find QubitManager!");
            enabled = false;
            return;
        }
        
        // Subscribe to qubit count changes
        QubitManager.OnQubitCountChanged += OnQubitCountChanged;
        
        // Initialize UI  
        UpdateUI();
        
        if (showDebugLogs)
            Debug.Log("✅ QuantumCollapseManager initialized");
    }
    
    private void Update()
    {
        // Handle danger zone status - FIXED: Only enter danger zone when OVER limit
        if (qubitManager != null && qubitManager.GetCurrentQubitCount() > qubitManager.GetMaxQubitCount())
        {
            if (!isInDangerZone)
            {
                EnterDangerZone();
            }
        }
        else
        {
            if (isInDangerZone)
            {
                ExitDangerZone();
            }
        }
        
        // Debug testing
        if (forceCollapseForTesting)
        {
            forceCollapseForTesting = false;
            TriggerQuantumCollapse();
        }
    }
    
    private void OnQubitCountChanged(int currentCount, int maxCount)
    {
        if (showDebugLogs)
            Debug.Log($"🔢 Qubit count changed: {currentCount}/{maxCount}");
        
        // FIXED: Check for collapse only when OVER limit, not AT limit
        if (currentCount > maxCount)
        {
            CheckForCollapse();
        }
        
        UpdateUI();
    }
    
    private void CheckForCollapse()
    {
        if (qubitManager == null) return;
        
        float riskLevel = qubitManager.GetRiskLevel();
        float collapseChance = riskLevel; // Risk level IS the collapse chance (0-0.9)
        
        // Roll for collapse ONCE when qubit is placed OVER the limit
        float roll = Random.Range(0f, 1f);
        
        if (showDebugLogs)
            Debug.Log($"🎲 Collapse check on qubit placement: Roll={roll:F3}, Chance={collapseChance:F3}");
        
        if (roll < collapseChance)
        {
            // COLLAPSE TRIGGERED!
            TriggerQuantumCollapse();
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"✅ Survived collapse check! System remains stable.");
        }
    }
    
    public void TriggerQuantumCollapse()
    {
        if (qubitManager == null) return;
        
        int qubitsDestroyed = qubitManager.GetCurrentQubitCount();
        
        if (showDebugLogs)
            Debug.Log($"🌀 QUANTUM COLLAPSE TRIGGERED! Destroying {qubitsDestroyed} qubits");
        
        // Update statistics
        totalCollapses++;
        qubitsLostToCollapse += qubitsDestroyed;
        
        // Visual/Audio feedback
        StartCoroutine(CollapseSequence());
        
        // Trigger the actual collapse in QubitManager
        qubitManager.TriggerQuantumCollapse();
        
        // Force UI update
        UpdateUI();
    }
    
    private IEnumerator CollapseSequence()
    {
        // Show collapse effect
        if (collapseEffectPrefab != null)
        {
            GameObject effect = Instantiate(collapseEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f); // Clean up after 5 seconds
        }
        
        // Play collapse sound
        if (collapseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collapseSound);
        }
        
        // Brief pause for dramatic effect
        yield return new WaitForSeconds(1f);
        
        if (showDebugLogs)
            Debug.Log("✅ Collapse sequence completed");
    }
    
    private void UpdateUI()
    {
        if (qubitManager == null) return;
        
        int currentCount = qubitManager.GetCurrentQubitCount();
        int maxCount = qubitManager.GetMaxQubitCount();
        float riskLevel = qubitManager.GetRiskLevel();
        
        // Update qubit count text - Simple format: "99/99"
        if (qubitCountText != null)
        {
            qubitCountText.text = $"{currentCount}/{maxCount}";
            
            // FIXED: Color coding based on proper thresholds
            if (currentCount > maxCount)
            {
                qubitCountText.color = dangerColor; // Red when OVER limit
            }
            else if (currentCount == maxCount)
            {
                qubitCountText.color = warningColor; // Yellow when AT limit (warning)
            }
            else
            {
                qubitCountText.color = safeColor; // White when under limit
            }
        }
        
        // FIXED: Update risk text - Only show risk when actually at risk
        if (riskText != null)
        {
            if (currentCount > maxCount)
            {
                // Only show risk percentage when OVER the limit
                int riskPercent = Mathf.RoundToInt(riskLevel * 100);
                riskText.text = $"Risk: {riskPercent}%";
                riskText.color = dangerColor;
                riskText.gameObject.SetActive(true);
            }
            else if (currentCount == maxCount)
            {
                // Show warning when AT the limit
                int riskPercent = Mathf.RoundToInt(riskLevel * 100);
                riskText.text = $"Risk: {riskPercent}%";
                riskText.color = warningColor;
                riskText.gameObject.SetActive(true);
            }
            else
            {
                // Hide risk text when safe
                riskText.gameObject.SetActive(false);
            }
        }
    }
    
    private void EnterDangerZone()
    {
        isInDangerZone = true;
        
        if (showDebugLogs)
            Debug.Log("⚠️ Entered quantum instability danger zone!");
    }
    
    private void ExitDangerZone()
    {
        isInDangerZone = false;
        
        if (showDebugLogs)
            Debug.Log("✅ Exited danger zone - system stable");
    }
    
    public float GetCurrentStability()
    {
        if (qubitManager == null) return 1f;
        
        int currentCount = qubitManager.GetCurrentQubitCount();
        int maxCount = qubitManager.GetMaxQubitCount();
        
        // FIXED: Full stability when under OR at the limit
        if (currentCount <= maxCount)
            return 1f; // Full stability when not over the limit
        
        // Calculate stability only when OVER the limit
        int riskQubits = currentCount - maxCount; // Actual extra qubits
        float stability = baseStability - (riskQubits * stabilityDecayPerExtraQubit);
        
        return Mathf.Max(stability, minStability);
    }
    
    public int GetTotalCollapses()
    {
        return totalCollapses;
    }
    
    public int GetQubitsLostToCollapse()
    {
        return qubitsLostToCollapse;
    }
    
    public bool IsInDangerZone()
    {
        return isInDangerZone;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (qubitManager != null)
        {
            QubitManager.OnQubitCountChanged -= OnQubitCountChanged;
        }
    }
    
    // Debug methods for testing
    [ContextMenu("Debug: Force Quantum Collapse")]
    private void DebugForceCollapse()
    {
        TriggerQuantumCollapse();
    }
    
    [ContextMenu("Debug: Show Collapse Statistics")]
    private void DebugShowStats()
    {
        Debug.Log($"=== COLLAPSE STATISTICS ===\n" +
                  $"Total Collapses: {totalCollapses}\n" +
                  $"Qubits Lost: {qubitsLostToCollapse}\n" +
                  $"Current Stability: {GetCurrentStability():F2}\n" +
                  $"In Danger Zone: {isInDangerZone}");
    }
    
    [ContextMenu("Debug: Reset Statistics")]
    private void DebugResetStats()
    {
        totalCollapses = 0;
        qubitsLostToCollapse = 0;
        Debug.Log("Collapse statistics reset");
    }
}