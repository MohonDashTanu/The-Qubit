// GateInventory.cs - Main inventory scriptable object
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Gate Inventory", menuName = "Quantum/Gate Inventory")]
public class GateInventory : ScriptableObject
{
    [Header("Gate Collection")]
    [SerializeField] private List<GateInventoryEntry> gates = new List<GateInventoryEntry>();
    
    [Header("Debug")]
    public bool resetOnPlay = false; // For testing
    
    private void OnEnable()
    {
        if (resetOnPlay && Application.isPlaying)
        {
            ResetForTesting();
        }
    }
    
    // Get all gates
    public List<GateInventoryEntry> GetAllGates()
    {
        return gates;
    }
    
    // Get gates that should be loaded in a run (unlocked with quantity > 0)
    public List<GateInventoryEntry> GetRunGates()
    {
        List<GateInventoryEntry> runGates = new List<GateInventoryEntry>();
        
        foreach (GateInventoryEntry entry in gates)
        {
            if (entry.unlocked && entry.GetRunQuantity() > 0)
            {
                runGates.Add(entry);
            }
        }
        
        return runGates;
    }
    
    // Get specific gate entry by type
    public GateInventoryEntry GetGateEntry(GateType gateType)
    {
        return gates.Find(g => g.gateData != null && g.gateData.gateType == gateType);
    }
    
    // Get gate data by type
    public GateData GetGateData(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        return entry?.gateData;
    }
    
    // Check if gate is unlocked
    public bool IsGateUnlocked(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        return entry != null && entry.unlocked;
    }
    
    // Get run quantity for a gate type
    public int GetRunQuantity(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        return entry?.GetRunQuantity() ?? 0;
    }
    
    // Get gate level
    public int GetGateLevel(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        return entry?.currentLevel ?? 1;
    }
    
    // Unlock gate (called when wave requirements are met)
    public bool UnlockGate(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        if (entry != null && !entry.unlocked)
        {
            entry.unlocked = true;
            Debug.Log($"🔓 Unlocked {gateType} gate!");
            return true;
        }
        return false;
    }
    
    // Add owned quantity (from shop purchases)
    public void AddOwnedQuantity(GateType gateType, int amount)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        if (entry != null)
        {
            entry.ownedQuantity += amount;
            Debug.Log($"📦 Added {amount} {gateType} gates. Total owned: {entry.ownedQuantity}");
        }
    }
    
    // Set owned quantity
    public void SetOwnedQuantity(GateType gateType, int quantity)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        if (entry != null)
        {
            entry.ownedQuantity = quantity;
        }
    }
    
    // Upgrade gate level
    public bool UpgradeGate(GateType gateType)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        if (entry != null && entry.gateData != null)
        {
            if (entry.currentLevel < entry.gateData.maxLevel)
            {
                entry.currentLevel++;
                Debug.Log($"⬆️ Upgraded {gateType} to level {entry.currentLevel}");
                return true;
            }
        }
        return false;
    }
    
    // Set max per run (for balancing)
    public void SetMaxPerRun(GateType gateType, int maxPerRun)
    {
        GateInventoryEntry entry = GetGateEntry(gateType);
        if (entry != null)
        {
            entry.maxPerRun = maxPerRun;
        }
    }
    
    // Check unlock conditions based on wave
    public void CheckWaveUnlocks(int currentWave)
    {
        foreach (GateInventoryEntry entry in gates)
        {
            if (!entry.unlocked && currentWave >= entry.unlockWave)
            {
                UnlockGate(entry.gateData.gateType);
            }
        }
    }
    
    // Reset for testing
    private void ResetForTesting()
    {
        foreach (GateInventoryEntry entry in gates)
        {
            entry.unlocked = false;
            entry.ownedQuantity = 0;
            entry.currentLevel = 1;
        }
        
        // Give some gates for testing
        SetOwnedQuantity(GateType.Hadamard, 5);
        UnlockGate(GateType.Hadamard);
        
        Debug.Log("🔄 Reset gate inventory for testing");
    }
    
    // Get summary for debugging
    public string GetInventorySummary()
    {
        System.Text.StringBuilder summary = new System.Text.StringBuilder();
        summary.AppendLine("=== GATE INVENTORY ===");
        
        foreach (GateInventoryEntry entry in gates)
        {
            if (entry.gateData != null)
            {
                summary.AppendLine($"{entry.gateData.gateName}: " +
                    $"Owned={entry.ownedQuantity}, " +
                    $"Max/Run={entry.maxPerRun}, " +
                    $"RunQty={entry.GetRunQuantity()}, " +
                    $"Level={entry.currentLevel}, " +
                    $"Unlocked={entry.unlocked}");
            }
        }
        
        return summary.ToString();
    }
    
    // Context menu for testing
    [ContextMenu("Debug: Show Inventory")]
    private void DebugShowInventory()
    {
        Debug.Log(GetInventorySummary());
    }
    
    [ContextMenu("Debug: Give Test Gates")]
    private void DebugGiveTestGates()
    {
        SetOwnedQuantity(GateType.Hadamard, 10);
        UnlockGate(GateType.Hadamard);
        Debug.Log("Given test gates");
    }
}