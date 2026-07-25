using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Quantum/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public Sprite enemyIcon;
    
    [Header("Prefab Reference")]
    public GameObject enemyPrefab;
    
    [Header("Stats")]
    public int health = 100;
    public float moveSpeed = 2f;
    public int damageAmount = 10;
    
    [Header("Attack Properties")]
    public bool canAttack = true;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    
    [Header("In-Game Rewards (old system)")]
    public int informationReward = 10;
    
    [Header("Main Menu Currency Drops")]
    [Tooltip("Minimum currency dropped when killed")]
    public int minCurrencyDrop = 1;
    [Tooltip("Maximum currency dropped when killed")]
    public int maxCurrencyDrop = 5;
    [Tooltip("Chance to drop currency (0-100%)")]
    [Range(0f, 100f)]
    public float currencyDropChance = 50f;
    [Tooltip("Bonus currency for special enemies")]
    public int bonusCurrencyDrop = 0;
    
    [Header("Special Drop Settings")]
    [Tooltip("Is this a rare/boss enemy with guaranteed drops?")]
    public bool guaranteedDrop = false;
    [Tooltip("Multiplier for currency drops (for boss enemies)")]
    public float currencyMultiplier = 1f;
    
    /// <summary>
    /// Calculate how much currency this enemy should drop
    /// </summary>
    public int CalculateCurrencyDrop()
    {
        // Check if we should drop currency at all
        if (!guaranteedDrop && Random.Range(0f, 100f) > currencyDropChance)
        {
            return 0; // No drop
        }
        
        // Calculate base drop amount
        int baseDrop = Random.Range(minCurrencyDrop, maxCurrencyDrop + 1);
        
        // Add bonus currency
        int totalDrop = baseDrop + bonusCurrencyDrop;
        
        // Apply multiplier
        totalDrop = Mathf.RoundToInt(totalDrop * currencyMultiplier);
        
        return Mathf.Max(0, totalDrop);
    }
    
    /// <summary>
    /// Get drop chance as a readable string for UI
    /// </summary>
    public string GetDropChanceText()
    {
        if (guaranteedDrop)
            return "Guaranteed";
        else
            return $"{currencyDropChance:F0}%";
    }
}