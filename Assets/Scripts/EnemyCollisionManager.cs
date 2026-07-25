using UnityEngine;
using System.Collections.Generic;

public class EnemyCollisionManager : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float avoidanceRadius = 0.5f;
    [SerializeField] private float avoidanceForce = 1.0f;
    [SerializeField] private LayerMask enemyLayerMask;
    
    // Static instance for easy access
    public static EnemyCollisionManager Instance { get; private set; }
    
    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Default to Enemy layer if not set
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }
    }
    
    /// <summary>
    /// Calculate avoidance vector for an enemy based on nearby enemies
    /// </summary>
    public Vector2 CalculateAvoidanceVector(Vector2 position)
    {
        Vector2 avoidanceVector = Vector2.zero;
        
        // Find all nearby enemies
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, avoidanceRadius, enemyLayerMask);
        
        // For each nearby enemy, calculate avoidance contribution
        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            // Skip self
            if (enemyCollider.gameObject == gameObject || enemyCollider.isTrigger)
                continue;
                
            // Calculate direction away from other enemy
            Vector2 enemyPos = enemyCollider.transform.position;
            Vector2 awayDir = (position - enemyPos).normalized;
            
            // Calculate force based on proximity (closer = stronger)
            float distance = Vector2.Distance(position, enemyPos);
            
            // Avoid division by zero
            if (distance < 0.01f)
                distance = 0.01f;
                
            float forceMagnitude = avoidanceForce * (1f / distance);
            
            // Add to avoidance vector
            avoidanceVector += awayDir * forceMagnitude;
        }
        
        return avoidanceVector;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize the avoidance radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
}