
using UnityEngine;

// Fixed ZeroQubitData - ONLY sets behavior flags, YOU define all the stats
[CreateAssetMenu(fileName = "ZeroQubit Data", menuName = "Quantum/Zero Qubit Data")]
public class ZeroQubitData : QubitData
{
    [Header("Zero Qubit Settings")]
    public GameObject pulseEffectPrefab;

    private void OnEnable()
    {
        //Debug.Log($"ZeroQubitData.OnEnable() for {name}");
        
        // ONLY set the behavior capabilities - YOU define all the actual stats
        canGenerate = true;  // Zero Qubits can generate resources
        canAttack = true;    // Zero Qubits can defend themselves
        
        // DON'T override any stats - let YOU define them in the inspector:
        // - attackRange (you set this - could be 1, 0.5, 2, whatever you want)
        // - attackPower (you set this - could be 15, 10, 20, whatever you want) 
        // - attackSpeed (you set this - could be 1, 0.5, 2, whatever you want)
        // - informationPerSecond (you set this - could be 2, 1, 3, whatever you want)
        // - maxHealth (you set this)
        // - qubitCost (you set this)
        
        //Debug.Log($"ZeroQubitData initialized: canGenerate={canGenerate}, canAttack={canAttack}");
        //Debug.Log($"Stats defined by you: attackRange={attackRange}, attackPower={attackPower}, attackSpeed={attackSpeed}, informationPerSecond={informationPerSecond}");
    }
    
    private void OnValidate()
    {   
        //Debug.Log($"ZeroQubitData.OnValidate() for {name}");
        
        // ONLY ensure the behavior flags are correct - don't touch your custom stats
        canGenerate = true;  // Always true for Zero Qubit family
        canAttack = true;    // Always true for Zero Qubit family
        
        // NO stat overrides - your custom values are preserved
        //Debug.Log($"ZeroQubitData validated: canGenerate={canGenerate}, canAttack={canAttack}");
        //Debug.Log($"Your custom stats preserved: attackRange={attackRange}, attackPower={attackPower}, informationPerSecond={informationPerSecond}");
    }
}