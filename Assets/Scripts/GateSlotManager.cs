// GateSlotManager.cs - Manages gate UI slots (similar to UpgradeSlotManager)
using System.Collections.Generic;
using UnityEngine;

public class GateSlotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gateSlotPrefab;
    [SerializeField] private Transform gateSlotParent;
    [SerializeField] private QuantumGateManager quantumGateManager;
    [SerializeField] private GateInventory gateInventory; // The scriptable object
    
    private List<GameObject> activeSlots = new List<GameObject>();
    
    // Singleton
    public static GateSlotManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        //Debug.Log("🎮 GateSlotManager Awake");
    }
    
    private void Start()
    {
        //Debug.Log("🎮 GateSlotManager Start");
        
        // Find QuantumGateManager if not assigned
        if (quantumGateManager == null)
        {
            quantumGateManager = QuantumGateManager.Instance;
            if (quantumGateManager == null)
            {
                //Debug.LogError("GateSlotManager: Could not find QuantumGateManager!");
                return;
            }
        }
        
        // Find GateInventory if not assigned
        if (gateInventory == null)
        {
            gateInventory = Resources.Load<GateInventory>("GateInventory");
            if (gateInventory == null)
            {
                //Debug.LogError("GateSlotManager: Could not find GateInventory in Resources!");
                return;
            }
        }
        
        // Initialize slots based on inventory
        InitializeGateSlots();
        
        // Subscribe to gate events
        if (quantumGateManager != null)
        {
            //Debug.Log("✅ Subscribing to QuantumGateManager events");
            quantumGateManager.OnGateQuantityChanged += OnGateQuantityChanged;
        }
        
        // Subscribe to wave events for unlocking
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnWaveStart += OnWaveStarted;
        }
    }
    
    private void InitializeGateSlots()
    {
        //Debug.Log($"🔧 Initializing gate slots from inventory");
        
        if (gateInventory == null)
        {
            //Debug.LogError("No gate inventory assigned!");
            return;
        }
        
        // Clear existing slots
        foreach (GameObject slot in activeSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        activeSlots.Clear();
        
        // Get gates that should be loaded for this run
        List<GateInventoryEntry> runGates = gateInventory.GetRunGates();
        
        //Debug.Log($"📦 Found {runGates.Count} gates to load for this run");
        
        // Create slots for each gate with run quantity > 0
        foreach (GateInventoryEntry entry in runGates)
        {
            if (entry.gateData != null)
            {
                CreateGateSlot(entry);
            }
        }
        
        // If no gates available, log for debugging
        if (runGates.Count == 0)
        {
            //Debug.Log("📝 No gates available for this run. Check inventory settings.");
        }
    }
    
    private void CreateGateSlot(GateInventoryEntry gateEntry)
    {
        if (gateSlotPrefab == null || gateSlotParent == null)
        {
            //Debug.LogError("GateSlotManager: Missing prefab or parent references!");
            return;
        }

        // Create the slot
        GameObject slotObject = Instantiate(gateSlotPrefab, gateSlotParent);
        slotObject.name = $"GateSlot_{gateEntry.gateData.gateName}";
        
        // Add to active slots list
        activeSlots.Add(slotObject);
        
        // Get the GateSlot component
        GateSlot slot = slotObject.GetComponent<GateSlot>();
        if (slot != null)
        {
            // Initialize with gate data, quantity, and level
            int runQuantity = gateEntry.GetRunQuantity();
            slot.Initialize(quantumGateManager, gateEntry.gateData, runQuantity, gateEntry.currentLevel);
            
            //Debug.Log($"✅ Created slot for {gateEntry.gateData.gateName} with {runQuantity} quantity");
        }
        else
        {
            //Debug.LogError($"GateSlotManager: No GateSlot component found on instantiated prefab!");
        }
    }
    
    public void UpdateAllSlots()
    {
        //Debug.Log("🔄 Updating all gate slots");
        
        // Find all GateSlot components in active slots
        foreach (GameObject slotObject in activeSlots)
        {
            if (slotObject != null)
            {
                GateSlot slot = slotObject.GetComponent<GateSlot>();
                if (slot != null)
                {
                    slot.UpdateUI();
                }
            }
        }
    }
    
    private void OnGateQuantityChanged(GateType gateType, int newQuantity)
    {
        //Debug.Log($"📢 Gate quantity changed event received: {gateType} -> {newQuantity}");
        UpdateAllSlots();
    }
    
    private void OnWaveStarted(int waveNumber)
    {
        // Check for gate unlocks
        if (gateInventory != null)
        {
            gateInventory.CheckWaveUnlocks(waveNumber);
            
            // Rebuild slots if new gates were unlocked
            List<GateInventoryEntry> runGates = gateInventory.GetRunGates();
            if (runGates.Count > activeSlots.Count)
            {
                //Debug.Log($"🔓 New gates unlocked at wave {waveNumber}! Rebuilding slots...");
                InitializeGateSlots();
            }
        }
    }
    
    // Public method to refresh slots (useful after shop purchases or upgrades)
    public void RefreshGateSlots()
    {
        //Debug.Log("🔄 Refreshing gate slots from inventory");
        InitializeGateSlots();
    }
    
    // Load gates into the manager from inventory (called at run start)
    public void LoadGatesFromInventory()
    {
        if (gateInventory == null || quantumGateManager == null)
        {
            //Debug.LogError("Cannot load gates - missing inventory or manager!");
            return;
        }
        
        List<GateInventoryEntry> runGates = gateInventory.GetRunGates();
        
        //Debug.Log($"📥 Loading {runGates.Count} gate types into manager");
        
        // Load each gate type into the manager
        foreach (GateInventoryEntry entry in runGates)
        {
            if (entry.gateData != null)
            {
                // Set quantities and levels in the manager
                quantumGateManager.SetGateQuantity(entry.gateData.gateType, entry.GetRunQuantity());
                quantumGateManager.SetGateLevel(entry.gateData.gateType, entry.currentLevel);
                
                // Mark as unlocked if it is
                if (entry.unlocked)
                {
                    quantumGateManager.UnlockGate(entry.gateData.gateType);
                }
                
                //Debug.Log($"📦 Loaded {entry.gateData.gateName}: Qty={entry.GetRunQuantity()}, Level={entry.currentLevel}");
            }
        }
        
        // Update UI after loading
        UpdateAllSlots();
    }
    
    // Get gate inventory (for external access)
    public GateInventory GetGateInventory()
    {
        return gateInventory;
    }
    
    // Set gate inventory (useful for testing or save/load)
    public void SetGateInventory(GateInventory newInventory)
    {
        gateInventory = newInventory;
        RefreshGateSlots();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (quantumGateManager != null)
        {
            quantumGateManager.OnGateQuantityChanged -= OnGateQuantityChanged;
        }
        
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnWaveStart -= OnWaveStarted;
        }
    }
    
    // Debug methods for testing
    [ContextMenu("Debug: Refresh Slots")]
    private void DebugRefreshSlots()
    {
        RefreshGateSlots();
    }
    
    [ContextMenu("Debug: Load Gates from Inventory")]
    private void DebugLoadGates()
    {
        LoadGatesFromInventory();
    }
    
    [ContextMenu("Debug: Show Inventory Summary")]
    private void DebugShowInventory()
    {
        if (gateInventory != null)
        {
            //Debug.Log(gateInventory.GetInventorySummary());
        }
    }
}