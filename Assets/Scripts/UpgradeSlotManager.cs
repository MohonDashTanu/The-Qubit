using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeSlotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradeSlotPrefab;
    [SerializeField] private Transform upgradeSlotParent;
    [SerializeField] private GlobalUpgradeManager globalUpgradeManager;
    
    [Header("Upgrade Slots Configuration")]
    [SerializeField] private List<UpgradeSlotConfig> upgradeSlots = new List<UpgradeSlotConfig>();
    
    [System.Serializable]
    public class UpgradeSlotConfig
    {
        public string upgradeType = "core";
        public string title = "Core Upgrades";
        public Sprite icon; // Default/Level 0 icon
        public List<Sprite> levelIcons = new List<Sprite>(); // Icons for each level
    }
    
    private List<GameObject> activeSlots = new List<GameObject>();
    
    // Singleton
    public static UpgradeSlotManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        //Debug.Log("🎮 UpgradeSlotManager Awake");
    }
    
    private void Start()
    {
        //Debug.Log("🎮 UpgradeSlotManager Start");
        
        // Find GlobalUpgradeManager if not assigned
        if (globalUpgradeManager == null)
        {
            globalUpgradeManager = GlobalUpgradeManager.Instance;
            if (globalUpgradeManager == null)
            {
                //Debug.LogError("UpgradeSlotManager: Could not find GlobalUpgradeManager!");
                return;
            }
        }
        
        // Initialize slots
        InitializeUpgradeSlots();
        
        // Subscribe to upgrade events
        if (globalUpgradeManager != null)
        {
            //Debug.Log("✅ Subscribing to GlobalUpgradeManager events");
            GlobalUpgradeManager.OnUpgradeChanged += OnUpgradeChanged;
        }
    }
    
    private void InitializeUpgradeSlots()
    {
        //Debug.Log($"🔧 Initializing {upgradeSlots.Count} upgrade slots");
        
        // Clear existing slots
        foreach (GameObject slot in activeSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        activeSlots.Clear();
        
        // Create default slots if none configured
        if (upgradeSlots.Count == 0)
        {
            //Debug.Log("📝 No upgrade slots configured, creating defaults");
            upgradeSlots.Add(new UpgradeSlotConfig { upgradeType = "core", title = "Core Upgrades" });
            upgradeSlots.Add(new UpgradeSlotConfig { upgradeType = "zeroQubit", title = "Zero Qubit Upgrades" });
            upgradeSlots.Add(new UpgradeSlotConfig { upgradeType = "oneQubit", title = "One Qubit Upgrades" });
        }
        
        // Create slots
        foreach (var config in upgradeSlots)
        {
            CreateUpgradeSlot(config);
        }
    }
    
    private void CreateUpgradeSlot(UpgradeSlotConfig config)
    {
        if (upgradeSlotPrefab == null || upgradeSlotParent == null)
        {
            //Debug.LogError("UpgradeSlotManager: Missing prefab or parent references!");
            return;
        }

        // Create the slot
        GameObject slotObject = Instantiate(upgradeSlotPrefab, upgradeSlotParent);
        slotObject.name = $"UpgradeSlot_{config.upgradeType}";
        
        // Add to active slots list
        activeSlots.Add(slotObject);
        
        // Get the UpgradeSlot component
        UpgradeSlot slot = slotObject.GetComponent<UpgradeSlot>();
        if (slot != null)
        {
            // Initialize with all required parameters including icon
            slot.Initialize(globalUpgradeManager, config.upgradeType, config.title, config.icon);
            
            // The title and icon are now set in the Initialize method,
            // so we don't need to set them separately here
        }
        else
        {
            //Debug.LogError($"UpgradeSlotManager: No UpgradeSlot component found on instantiated prefab!");
        }
    }
    
    public void UpdateAllSlots()
    {
        //Debug.Log("🔄 Updating all upgrade slots");
        
        // Find all UpgradeSlot components in active slots
        foreach (GameObject slotObject in activeSlots)
        {
            if (slotObject != null)
            {
                UpgradeSlot slot = slotObject.GetComponent<UpgradeSlot>();
                if (slot != null)
                {
                    slot.UpdateUI();
                }
            }
        }
    }
    
    // Get the appropriate icon based on level
    public Sprite GetIconForLevel(string upgradeType, int level)
    {
        var config = upgradeSlots.Find(s => s.upgradeType == upgradeType);
        if (config == null) return null;
        
        // If we have level icons configured
        if (config.levelIcons != null && config.levelIcons.Count > 0)
        {
            // Use the last icon if level exceeds the number of icons
            int iconIndex = Mathf.Min(level, config.levelIcons.Count - 1);
            
            // Return the icon at this index if it exists
            if (config.levelIcons[iconIndex] != null)
            {
                return config.levelIcons[iconIndex];
            }
        }
        
        // Otherwise return the default icon
        return config.icon;
    }
    
    private void OnUpgradeChanged(string upgradeType, int newLevel)
    {
        //Debug.Log($"📢 Upgrade changed event received: {upgradeType} -> Level {newLevel}");
        UpdateAllSlots();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (globalUpgradeManager != null)
        {
            GlobalUpgradeManager.OnUpgradeChanged -= OnUpgradeChanged;
        }
    }
    
    // Public method to trigger upgrade (called by UpgradeSlot)
    public void TriggerUpgrade(string upgradeType)
    {
        //Debug.Log($"🎯 UpgradeSlotManager: Triggering upgrade for {upgradeType}");
        
        if (globalUpgradeManager != null)
        {
            bool success = globalUpgradeManager.TryUpgrade(upgradeType);
            //Debug.Log($"Upgrade {upgradeType} result: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success)
            {
                UpdateAllSlots();
            }
        }
        else
        {
            //Debug.LogError("GlobalUpgradeManager is null!");
        }
    }
}