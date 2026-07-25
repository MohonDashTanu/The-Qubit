// GateData.cs - Complete fixed version with CNOT gate
using UnityEngine;

[CreateAssetMenu(fileName = "New Gate Data", menuName = "Quantum/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Basic Info")]
    public string gateName;
    public Sprite gateIcon;
    public GateType gateType;
    
    [Header("Prefab Reference")]
    public GameObject gatePrefab;
    
    [Header("Base Properties")]
    public float baseRadius = 3f;
    public float radiusPerLevel = 1f;
    public int baseMaxTargets = 3;
    public int maxTargetsPerLevel = 2;
    public float duration = 15f;
    
    [Header("Progression")]
    public int maxLevel = 5;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string quantumExplanation;
}

// FIXED: Complete enum for gate types with all quantum gates including CNOT
public enum GateType
{
    None,
    Hadamard,
    PauliX,
    PauliY,    // ADDED: Missing PauliY gate
    PauliZ,
    CNOT,      // CNOT gate for strategic entanglement
    Swap,      // ADDED: Missing Swap gate
    Toffoli
}