using UnityEngine;

public class QubitHunterEnemy : Enemy
{
    [Header("Hunter Settings")]
    [SerializeField] private float qubitDetectionRange = 15f;
    [SerializeField] private Color hunterColor = new Color(1f, 0.5f, 0f);
    
    // Keep track of current qubit target
    private Transform currentQubitTarget;
    
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
        
        // Apply hunter color tint
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hunterColor;
        }
    }
    
    protected override void Update()
    {
        // FIXED: If currently attacking, let base class handle it
        if (isAttacking && currentObstacle != null)
        {
            base.Update(); // This will stop movement and handle attacking
            return;
        }
        
        // FIXED: Only find targets and calculate movement if NOT attacking
        if (!isAttacking)
        {
            // Look for the nearest qubit
            FindNearestQubit();
            
            // Calculate movement direction toward preferred target
            CalculateHunterMovementDirection();
        }
        
        // Let base class handle the rest (including movement application)
        try
        {
            ValidateObstacleReference();
            
            if (!isAttacking)
            {
                // Movement direction is already set by CalculateHunterMovementDirection
                // Base class will apply it in FixedUpdate
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Exception in QubitHunter Update: {e.Message}");
            isAttacking = false;
            currentObstacle = null;
            SetMovementDirection(Vector2.zero);
        }
    }
    
    // FIXED: Calculate movement direction based on hunter priorities
    private void CalculateHunterMovementDirection()
    {
        Vector2 targetDirection = Vector2.zero;
        
        // Priority 1: Move toward qubit if we have one
        if (currentQubitTarget != null)
        {
            targetDirection = (currentQubitTarget.position - transform.position).normalized;
        }
        // Priority 2: Move toward core if no qubit target
        else if (target != null)
        {
            targetDirection = (target.position - transform.position).normalized;
        }
        
        // Set the movement direction (base class will apply it)
        SetMovementDirection(targetDirection);
    }
    
    // Find the nearest qubit with comprehensive preview detection
    private void FindNearestQubit()
    {
        // Clear current target
        currentQubitTarget = null;
        
        GameObject[] qubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        float closestDistance = qubitDetectionRange;
        
        foreach (GameObject qubit in qubits)
        {
            if (qubit == null) continue;
            
            // COMPREHENSIVE PREVIEW DETECTION
            if (IsPreviewObject(qubit))
            {
                continue; // Skip all preview objects
            }
            
            // If we get here, it's a real qubit
            float distance = Vector2.Distance(transform.position, qubit.transform.position);
            
            if (distance < closestDistance)
            {
                currentQubitTarget = qubit.transform;
                closestDistance = distance;
            }
        }
    }
    
    // When we stop colliding with our obstacle - look for new target
    protected override void OnCollisionExit2D(Collision2D collision)
    {
        base.OnCollisionExit2D(collision);
        
        // Immediately look for a new target when we stop attacking
        if (!isAttacking)
        {
            FindNearestQubit();
        }
    }
    
    // Visualize the detection range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = hunterColor;
        Gizmos.DrawWireSphere(transform.position, qubitDetectionRange);
    }
}