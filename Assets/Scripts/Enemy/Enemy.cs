// Enemy.cs - Complete updated version with currency drops
using UnityEngine;
using System.Collections;
using TMPro; // Added for floating text effects

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Components")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Combat State")]
    protected bool isInCombat = false;
    
    [Header("Runtime References")]
    protected EnemyData enemyData;
    protected Transform target;
    [SerializeField] protected int currentHealth;
    protected float attackTimer = 0f;
    protected bool isAttacking = false;
    protected GameObject currentObstacle = null;
    
    // Component references
    protected Rigidbody2D rb;
    protected Collider2D mainCollider;
    
    // FIXED: Movement system like PlayerMovement
    [Header("Movement System")]
    [SerializeField] protected bool useRigidbodyMovement = true; // Toggle between physics and transform movement
    protected Vector2 currentMovementDirection = Vector2.zero;

    [Header("Currency Drop Settings")]
    [SerializeField] private TMP_FontAsset currencyTextFont; // Assign your font in inspector
    [SerializeField] private float currencyTextSize = 3f;
    [SerializeField] private Color currencyTextColor = Color.yellow;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;
    
    // Cached values
    protected float cachedMoveSpeed = 2f;
    protected int cachedDamageAmount = 10;
    protected float cachedAttackCooldown = 1f;
    protected bool cachedCanAttack = true;

    public int CurrentHealth => currentHealth;
    public float CachedMoveSpeed => cachedMoveSpeed;

    protected virtual void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // FIXED: Configure rigidbody like PlayerMovement
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = 0; // No gravity for top-down movement
            rb.linearDamping = 0; // No drag for consistent movement
            rb.angularDamping = 0; // No angular drag
        }
        else if (useRigidbodyMovement)
        {
            // Add rigidbody if we want to use physics movement but don't have one
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0;
            rb.angularDamping = 0;
        }
        
        // Ensure collider is not a trigger
        if (mainCollider != null && mainCollider.isTrigger)
        {
            mainCollider.isTrigger = false;
        }
        
        // Make sure we're using the Enemy tag
        if (tag != "Enemy")
        {
            tag = "Enemy";
        }
    }

    public void ApplyDifficultyModifier(float healthModifier, float speedModifier)
    {
        this.currentHealth = (int)((float)currentHealth * healthModifier);
        this.cachedMoveSpeed = this.cachedMoveSpeed * speedModifier;
    }

    // Initialize with enemy data and target
    public virtual void Initialize(EnemyData data, Transform coreTarget)
    {
        if (data == null)
        {
            Debug.LogWarning($"Null enemy data passed to {gameObject.name}!");
            return;
        }
        
        this.enemyData = data;
        this.currentHealth = data.health;
        this.target = coreTarget;
        
        // Cache values
        this.cachedMoveSpeed = data.moveSpeed;
        this.cachedDamageAmount = data.damageAmount;
        this.cachedAttackCooldown = data.attackCooldown;
        this.cachedCanAttack = data.canAttack;
        
        // Set sprite if available
        if (spriteRenderer != null && data.enemyIcon != null)
        {
            spriteRenderer.sprite = data.enemyIcon;
        }
    }
    
    protected virtual void Start()
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"Enemy {gameObject.name} initialized without data!");
            Destroy(gameObject);
        }
    }
    
    protected virtual void Update()
    {
        try
        {
            // Check if our obstacle reference is still valid
            ValidateObstacleReference();
            
            // FIXED: If currently attacking, STOP ALL MOVEMENT
            if (isAttacking && currentObstacle != null)
            {
                // STOP movement completely during combat
                SetMovementDirection(Vector2.zero);
                AttackObstacle();
                return;
            }
            
            // FIXED: Only move if NOT attacking
            if (!isAttacking && target != null)
            {
                CalculateAndSetMovementDirection();
            }
            else
            {
                // No target or attacking - stop moving
                SetMovementDirection(Vector2.zero);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Exception in Enemy Update: {e.Message}");
            
            // Try to recover
            isAttacking = false;
            currentObstacle = null;
            SetMovementDirection(Vector2.zero);
        }
    }
    
    // FIXED: Use FixedUpdate for physics-based movement like good practice
    protected virtual void FixedUpdate()
    {
        try
        {
            // Apply movement in FixedUpdate for physics consistency
            ApplyMovement();
            
            // Additional safety check
            ValidateObstacleReference();
        }
        catch (System.Exception)
        {
            // Silently handle any physics errors
        }
    }
    
    // FIXED: Calculate movement direction toward target (like PlayerMovement input)
    protected virtual void CalculateAndSetMovementDirection()
    {
        if (target == null)
        {
            SetMovementDirection(Vector2.zero);
            return;
        }
        
        // Calculate direction to target (normalized automatically)
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        
        // Set the movement direction
        SetMovementDirection(directionToTarget);
    }
    
    // FIXED: Set movement direction (like PlayerMovement setting movement vector)
    protected virtual void SetMovementDirection(Vector2 direction)
    {
        // Normalize if magnitude > 1 (like PlayerMovement)
        if (direction.magnitude > 1)
        {
            direction = direction.normalized;
        }
        
        currentMovementDirection = direction;
    }
    
    // FIXED: Apply movement using the same logic as PlayerMovement
    protected virtual void ApplyMovement()
    {
        if (currentMovementDirection == Vector2.zero)
        {
            // Stop movement completely
            if (useRigidbodyMovement && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }
        
        // Get current move speed
        float moveSpeed = (enemyData != null) ? enemyData.moveSpeed : cachedMoveSpeed;
        
        // Apply movement using the same method as PlayerMovement
        if (useRigidbodyMovement && rb != null)
        {
            // Physics-based movement (like PlayerMovement with rigidbody)
            rb.linearVelocity = currentMovementDirection * moveSpeed;
        }
        else
        {
            // Transform-based movement (like PlayerMovement without rigidbody)
            transform.Translate(currentMovementDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }
    
    // Validate that our obstacle reference is still valid
    protected virtual void ValidateObstacleReference()
    {
        if (isAttacking && (currentObstacle == null || !currentObstacle.activeInHierarchy))
        {
            // The obstacle is no longer valid
            isAttacking = false;
            currentObstacle = null;
            
            try
            {
                HandleObstacleDestroyed();
            }
            catch (System.Exception)
            {
                // Silently ignore any errors
            }
        }
    }
    
    protected virtual void HandleObstacleDestroyed()
    {
        // Base implementation - derived classes can override
    }
    
    // Attack the current obstacle
    protected virtual void AttackObstacle()
    {
        try
        {
            // Double-check that the obstacle still exists
            if (currentObstacle == null || !currentObstacle.activeInHierarchy)
            {
                isAttacking = false;
                currentObstacle = null;
                return;
            }
            
            // Check for preview objects
            if (IsPreviewObject(currentObstacle))
            {
                Debug.LogWarning($"Enemy {gameObject.name} was attacking a preview object! Stopping attack.");
                isAttacking = false;
                currentObstacle = null;
                return;
            }
            
            // Decrement attack timer
            attackTimer -= Time.deltaTime;
            
            // Use cached values if enemyData is null
            bool canAttack = (enemyData != null) ? enemyData.canAttack : cachedCanAttack;
            float attackCooldown = (enemyData != null) ? enemyData.attackCooldown : cachedAttackCooldown;
            int damageAmount = (enemyData != null) ? enemyData.damageAmount : cachedDamageAmount;
            
            // Check if ready to attack
            if (attackTimer <= 0f && canAttack)
            {
                if (currentObstacle != null && currentObstacle.activeInHierarchy)
                {
                    IDamageable damageable = currentObstacle.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        try
                        {
                            damageable.TakeDamage(damageAmount);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"Error while damaging obstacle: {ex.Message}");
                            isAttacking = false;
                            currentObstacle = null;
                            return;
                        }
                    }
                }
                else
                {
                    isAttacking = false;
                    currentObstacle = null;
                    return;
                }
                
                // Reset attack timer
                attackTimer = attackCooldown;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Exception in AttackObstacle: {ex.Message}");
            isAttacking = false;
            currentObstacle = null;
        }
    }
    
    // Comprehensive preview object detection
    protected virtual bool IsPreviewObject(GameObject obj)
    {
        if (obj == null) return true;
        
        // Check 1: Preview tag
        if (obj.CompareTag("PreviewQubit"))
        {
            return true;
        }
        
        // Check 2: Name contains preview indicators
        string name = obj.name.ToLower();
        if (name.Contains("_preview") || name.Contains("preview") || 
            (name.Contains("(clone)") && name.Contains("preview")))
        {
            return true;
        }
        
        // Check 3: Preview layer
        if (obj.layer == LayerMask.NameToLayer("Preview"))
        {
            return true;
        }
        
        // Check 4: Qubit component in preview mode
        Qubit qubitComponent = obj.GetComponent<Qubit>();
        if (qubitComponent != null)
        {
            System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (previewField != null)
            {
                bool isInPreview = (bool)previewField.GetValue(qubitComponent);
                if (isInPreview)
                {
                    return true;
                }
            }
        }
        
        // Check 5: All colliders disabled
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            bool allDisabled = true;
            foreach (var collider in colliders)
            {
                if (collider.enabled)
                {
                    allDisabled = false;
                    break;
                }
            }
            if (allDisabled)
            {
                return true;
            }
        }
        
        // Check 6: Semi-transparent sprite
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.color.a < 0.9f)
        {
            return true;
        }
        
        // Check 7: Parent has preview indicators
        if (obj.transform.parent != null)
        {
            string parentName = obj.transform.parent.name.ToLower();
            if (parentName.Contains("preview") || parentName.Contains("_preview"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // FIXED: Handle collision - stop movement immediately when hitting obstacle
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        try
        {
            if (collision == null || collision.gameObject == null)
                return;
                
            // Skip all preview objects
            if (IsPreviewObject(collision.gameObject))
            {
                return;
            }
                
            // If collided with a real qubit
            if (collision.gameObject.CompareTag("Qubit"))
            {
                // IMMEDIATELY STOP ALL MOVEMENT and start attacking
                isAttacking = true;
                currentObstacle = collision.gameObject;
                attackTimer = 0f; // Attack immediately
                
                // STOP movement immediately
                SetMovementDirection(Vector2.zero);
                
                // FREEZE physics to prevent sliding/jittering
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
            // If collided with the quantum core
            else if (collision.gameObject.CompareTag("QuantumCore"))
            {
                try
                {
                    IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        int damageAmount = (enemyData != null) ? enemyData.damageAmount : cachedDamageAmount;
                        damageable.TakeDamage(damageAmount);
                    }
                }
                catch (System.Exception)
                {
                    // Silently ignore any errors
                }
                
                // Enemy did its job, destroy it
                Destroy(gameObject);
            }
        }
        catch (System.Exception)
        {
            // Silently ignore any errors in collision handling
        }
    }
    
    // Handle when obstacle is destroyed or we're no longer in contact
    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        try
        {
            if (collision == null || collision.gameObject == null)
                return;
                
            // Check if this collision exit is from our current obstacle
            if (currentObstacle != null && collision.gameObject == currentObstacle)
            {
                isAttacking = false;
                currentObstacle = null;
                
                // Resume movement immediately when combat ends
                // The Update loop will recalculate movement direction
            }
        }
        catch (System.Exception)
        {
            // Silently ignore any errors
        }
    }
    
    // IDamageable implementation
    public virtual void TakeDamage(int damage)
    {
        try
        {
            currentHealth -= damage;
            
            // Visual feedback
            StartCoroutine(FlashRed());
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        catch (System.Exception)
        {
            // Silently ignore any errors
        }
    }
    
    // Visual feedback when taking damage
    protected virtual IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
    
    // Die and give rewards - UPDATED VERSION with currency drops
    protected virtual void Die()
    {
        try
        {
            // Give old information reward (existing system)
            if (enemyData != null)
            {
                ResourceManager resourceManager = FindObjectOfType<ResourceManager>();
                if (resourceManager != null)
                {
                    resourceManager.AddInformation(enemyData.informationReward);
                }
            }
            
            // NEW: Give main menu currency
            DropMainMenuCurrency();
        }
        catch (System.Exception)
        {
            // Silently ignore any errors
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// NEW METHOD: Handle main menu currency drops
    /// </summary>
    private void DropMainMenuCurrency()
    {
        if (enemyData == null) return;
        
        // Calculate currency drop
        int currencyDrop = enemyData.CalculateCurrencyDrop();
        
        if (currencyDrop > 0)
        {
            // Add to session currency
            GameSessionManager sessionManager = GameSessionManager.Instance;
            if (sessionManager != null)
            {
                sessionManager.AddSessionCurrency(currencyDrop);
                Debug.Log($"{enemyData.enemyName} dropped {currencyDrop} currency!");
                
                // Optional: Show currency pickup effect
                ShowCurrencyPickupEffect(currencyDrop);
            }
            else
            {
                Debug.LogWarning("No GameSessionManager found! Currency drop lost.");
            }
        }
    }
    
    /// <summary>
    /// Optional: Visual effect for currency pickup
    /// </summary>
    private void ShowCurrencyPickupEffect(int amount)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
        CurrencyPickupPopup.Create(spawnPosition, amount, currencyTextFont);
    }


    
    /// <summary>
    /// Animate the floating currency text
    /// </summary>
    private IEnumerator AnimateFloatingText(GameObject textObject)
    {
        if (textObject == null) yield break;
        
        float duration = 2f; // Longer duration for better visibility
        float elapsed = 0f;
        Vector3 startPos = textObject.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.5f; // Less distance for better control
        
        TextMeshPro textMesh = textObject.GetComponent<TextMeshPro>();
        if (textMesh == null) 
        {
            Destroy(textObject);
            yield break;
        }
        
        Color startColor = textMesh.color;
        
        // Animation loop
        while (elapsed < duration && textObject != null)
        {
            float progress = elapsed / duration;
            
            // Smooth easing curve
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Move upward with easing
            textObject.transform.position = Vector3.Lerp(startPos, endPos, easedProgress);
            
            // Fade out in the last 50% of animation
            float alpha = 1f;
            if (progress > 0.5f)
            {
                float fadeProgress = (progress - 0.5f) / 0.5f; // 0 to 1 for the fade part
                alpha = 1f - fadeProgress;
            }
            
            // Apply color with new alpha
            Color newColor = startColor;
            newColor.a = alpha;
            textMesh.color = newColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Make sure to destroy the object at the end
        if (textObject != null)
        {
            Destroy(textObject);
        }
    }
        
    // Extra safety on destroy
    protected virtual void OnDestroy()
    {
        // Clear any references
        currentObstacle = null;
        target = null;
        currentMovementDirection = Vector2.zero;
    }
    
    // Public method to get current movement for debugging
    public Vector2 GetCurrentMovementDirection()
    {
        return currentMovementDirection;
    }
    
    // Public method to check if enemy is moving
    public bool IsMoving()
    {
        return currentMovementDirection.magnitude > 0.1f && !isAttacking;
    }
}