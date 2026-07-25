using UnityEngine;

[CreateAssetMenu(fileName = "New Qubit Data", menuName = "Quantum/Qubit Data")]
public class QubitData : ScriptableObject
{
    [Header("Basic Info")]
    public string qubitName;
    public Sprite qubitIcon;
    
    [Header("Prefab Reference")]
    public GameObject qubitPrefab;
        
    [Header("Economy")]
    public int qubitCost;
    
    [Header("Health")]
    public int maxHealth = 50;
    
    [Header("Capabilities")]
    public bool canAttack = false;
    public bool canGenerate = false;
    
    [Header("Combat Stats")]
    [Tooltip("Only used if canAttack is true")]
    public int attackPower = 0;
    public float attackRange = 0f;
    public float attackSpeed = 0f; // Attacks per second
    
    [Header("Projectile Settings")]
    [Tooltip("Speed of projectiles fired by this qubit (units per second). Only used if canAttack is true.")]
    public float projectileSpeed = 10f; // NEW: Projectile speed
    
    [Header("Generation Stats")]
    [Tooltip("Only used if canGenerate is true")]
    public float informationPerSecond = 0f;
    
    private void OnValidate()
    {
        // Ensure projectile speed has a reasonable minimum if the qubit can attack
        if (canAttack && projectileSpeed <= 0)
        {
            projectileSpeed = 10f; // Set a reasonable default
        }
    }
}