// ResourceDisruptorEnemy.cs - Updated to use new movement system
using UnityEngine;

public class ResourceDisruptorEnemy : Enemy
{
    [Header("Disruptor Settings")]
    [SerializeField] private float targetUpdateInterval = 2f;
    [SerializeField] private float qubitDetectionRange = 12f;
    [SerializeField] private Color disruptorColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private GameObject disruptionEffectPrefab;
    
    // Keep track of current resource qubit target
    private Transform currentResourceQubitTarget;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
    
    public override void Initialize(EnemyData data, Transform coreTarget)
    {
        base.Initialize(data, coreTarget);
        
        try
        {
            // Apply disruptor color tint
            if (spriteRenderer != null)
            {
                spriteRenderer.color = disruptorColor;
            }
            
            // Start looking for qubits
            InvokeRepeating("UpdateTarget", 0.1f, targetUpdateInterval);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in ResourceDisruptor Initialize: {e.Message}");
        }
    }
    
    protected override void Update()
    {
        try
        {
            if (enemyData == null)
                return;
                
            ValidateObstacleReference();
            
            // FIXED: If currently attacking, let base class handle it
            if (isAttacking && currentObstacle != null)
            {
                base.Update(); // This will stop movement and handle attacking
                return;
            }
            
            // FIXED: Only calculate movement if not attacking
            if (!isAttacking)
            {
                CalculateDisruptorMovementDirection();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in ResourceDisruptor Update: {e.Message}");
            isAttacking = false;
            currentObstacle = null;
            SetMovementDirection(Vector2.zero);
        }
    }
    
    // FIXED: Calculate movement direction based on disruptor priorities
    private void CalculateDisruptorMovementDirection()
    {
        Vector2 targetDirection = Vector2.zero;
        
        // Priority 1: Move toward resource qubit if we have one
        if (currentResourceQubitTarget != null)
        {
            targetDirection = (currentResourceQubitTarget.position - transform.position).normalized;
        }
        // Priority 2: Move toward core if no resource target
        else if (target != null)
        {
            targetDirection = (target.position - transform.position).normalized;
        }
        
        // Set the movement direction (base class will apply it)
        SetMovementDirection(targetDirection);
    }
    
    // Find and update the current target (resource qubit or core)
    private void UpdateTarget()
    {
        try
        {
            if (this == null || !gameObject.activeInHierarchy)
                return;
                
            // Default to core if no qubits found
            currentResourceQubitTarget = null;
            
            // Find resource-generating qubits in range
            Transform resourceQubit = FindResourceGeneratingQubit();
            
            if (resourceQubit != null)
            {
                currentResourceQubitTarget = resourceQubit;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in ResourceDisruptor UpdateTarget: {e.Message}");
            currentResourceQubitTarget = null;
        }
    }
    
    // Find the nearest resource-generating qubit
    private Transform FindResourceGeneratingQubit()
    {
        try
        {
            GameObject[] qubits = GameObject.FindGameObjectsWithTag("Qubit");
            
            Transform nearestResourceQubit = null;
            float minDistance = qubitDetectionRange;
            
            foreach (GameObject qubitObject in qubits)
            {
                if (qubitObject == null)
                    continue;
                
                // Skip preview objects
                if (IsPreviewObject(qubitObject))
                    continue;
                    
                // Check if this qubit generates resources
                Qubit qubit = qubitObject.GetComponent<Qubit>();
                if (qubit != null && qubit.QubitData != null && qubit.QubitData.canGenerate)
                {
                    float distance = Vector2.Distance(transform.position, qubitObject.transform.position);
                    if (distance < minDistance)
                    {
                        nearestResourceQubit = qubitObject.transform;
                        minDistance = distance;
                    }
                }
            }
            
            return nearestResourceQubit;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in FindResourceGeneratingQubit: {e.Message}");
            return null;
        }
    }
    
    // Override the attack method to add disruptive effects
    protected override void AttackObstacle()
    {
        try
        {
            // First call the base implementation
            base.AttackObstacle();

            // Add disruptive visual effect if we have one and we're attacking
            if (!isAttacking || currentObstacle == null)
                return;

            if (disruptionEffectPrefab != null && Random.value < 0.1f) // 10% chance per attack frame
            {
                Instantiate(disruptionEffectPrefab, currentObstacle.transform.position, Quaternion.identity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error in ResourceDisruptor AttackObstacle: {e.Message}");
        }
    }
    
    // Override the obstacle destroyed handler
    protected override void HandleObstacleDestroyed()
    {
        try
        {
            base.HandleObstacleDestroyed();
            
            // Immediately look for a new target
            UpdateTarget();
        }
        catch (System.Exception)
        {
            // Silently ignore any errors
        }
    }
    
    // Visualize the detection range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = disruptorColor;
        Gizmos.DrawWireSphere(transform.position, qubitDetectionRange);
    }
    
    // Additional safety when destroyed
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Cancel the repeating invoke
        CancelInvoke("UpdateTarget");
        
        // Clear target references
        currentResourceQubitTarget = null;
    }
}