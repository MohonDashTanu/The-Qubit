using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private string enemyLayerName = "Enemy";
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("Physics Settings")]
    // Removed the unused enemyRadius field
    [SerializeField] private PhysicsMaterial2D enemyPhysicsMaterial;
    
    // Runtime variables
    private List<Enemy> activeEnemies = new List<Enemy>();
    
    // Singleton instance
    public static EnemyManager Instance { get; private set; }
    
    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Make sure the enemy layer exists
        CheckOrCreateEnemyLayer();
        
        // Create physics material if none assigned
        if (enemyPhysicsMaterial == null)
        {
            CreateEnemyPhysicsMaterial();
        }
    }
    
    private void CheckOrCreateEnemyLayer()
    {
        // Try to find enemy layer
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        
        if (enemyLayer == -1)
        {
           // Debug.LogWarning($"Enemy layer '{enemyLayerName}' not found! Enemies will use default layer.");
        }
        else
        {
            enemyLayerMask = 1 << enemyLayer;
          //  Debug.Log($"Using '{enemyLayerName}' (layer {enemyLayer}) for enemies.");
        }
    }
    
    private void CreateEnemyPhysicsMaterial()
    {
        enemyPhysicsMaterial = new PhysicsMaterial2D("EnemyMaterial");
        enemyPhysicsMaterial.friction = 0.2f;
        enemyPhysicsMaterial.bounciness = 0.5f;
        
       // Debug.Log("Created enemy physics material");
    }
    
    /// <summary>
    /// Register an enemy with the manager
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || activeEnemies.Contains(enemy))
            return;
            
        activeEnemies.Add(enemy);
        
        // Set enemy layer
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        if (enemyLayer != -1)
        {
            enemy.gameObject.layer = enemyLayer;
        }
        
        // Configure rigidbody if present
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            if (enemyPhysicsMaterial != null)
            {
                // Apply physics material to all colliders
                Collider2D[] colliders = enemy.GetComponents<Collider2D>();
                foreach (Collider2D collider in colliders)
                {
                    collider.sharedMaterial = enemyPhysicsMaterial;
                }
            }
        }
        
        if (showDebugInfo)
        {
           // Debug.Log($"Registered enemy: {enemy.name}");
        }
    }
    
    /// <summary>
    /// Unregister an enemy from the manager
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;
            
        activeEnemies.Remove(enemy);
        
        if (showDebugInfo)
        {
           // Debug.Log($"Unregistered enemy: {enemy.name}");
        }
    }
    
    /// <summary>
    /// Get a count of active enemies
    /// </summary>
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// Get the physics material for enemies
    /// </summary>
    public PhysicsMaterial2D GetEnemyPhysicsMaterial()
    {
        return enemyPhysicsMaterial;
    }
    
    /// <summary>
    /// Get the layer mask for enemies
    /// </summary>
    public LayerMask GetEnemyLayerMask()
    {
        return enemyLayerMask;
    }
    
    /// <summary>
    /// Clean up the active enemies list
    /// </summary>
    private void Update()
    {
        // Remove null entries
        activeEnemies.RemoveAll(e => e == null);
    }
}