using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;


public class Qubit : MonoBehaviour, IDamageable, IBuffable
{
    // Static event for qubit destruction - useful for external systems to listen for
    public static event System.Action<Vector3> OnQubitDestroyed;
    public event System.Action OnQubitDamaged;

    [Header("Health System")]
    [SerializeField] protected int maxHealth = 50; // ADD THIS LINE - it was missing!

    [Header("Qubit Configuration")]
    [SerializeField] protected QubitData qubitData;

    [Header("Attack Properties")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Collider2D attackRangeCollider;

    [Header("Resource Generation")]
    [SerializeField] protected float generationInterval = 1f;

    [Header("Runtime State")]
    [SerializeField] protected int currentHealth; // Serialized to view in inspector

    // Preview mode tracking - IMPORTANT: Not serialized to avoid conflicts with derived classes
    protected bool isInPreviewMode = false;

    // Internal variables
    protected float attackTimer = 0f;
    protected float generationTimer = 0f;
    protected float entanglementTimer = 0f;
    protected float entanglementRate = 0.1f;
    public bool isEntangled => qubitBuffContainer.GetBuff<EntanglementBuff>() != null;
    public GameplayBuffContainer qubitBuffContainer = new GameplayBuffContainer();

    protected ResourceManager resourceManager;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Collider2D mainCollider; // The qubit's main collision body

    // Grid system reference
    protected GridManager gridManager;

    // Store our grid cell position for cleanup
    protected Vector3 gridPosition;

    // Method to check for preview mode

    private void ApplyMainMenuUpgrades()
    {
        UpgradeApplier upgradeApplier = UpgradeApplier.Instance;
        if (upgradeApplier != null)
        {
            // Get multipliers from main menu upgrade system
            upgradeAttackMultiplier = upgradeApplier.GetAttackMultiplier();
            upgradeHealthMultiplier = upgradeApplier.GetHealthMultiplier();
            upgradeRangeMultiplier = upgradeApplier.GetRangeMultiplier();
            
            Debug.Log($"Qubit {name} main menu upgrades applied: Attack x{upgradeAttackMultiplier:F2}, Health {currentHealth}/{maxHealth}, Range x{upgradeRangeMultiplier:F2}");

            // Apply health upgrade immediately
            if (qubitData != null)
            {
                int baseMaxHealth = qubitData.maxHealth;
                int newMaxHealth = Mathf.RoundToInt(baseMaxHealth * upgradeHealthMultiplier);
                int healthDifference = newMaxHealth - maxHealth;

                maxHealth = newMaxHealth;
                currentHealth += healthDifference; // Also increase current health

                //Debug.Log($"Qubit {name} main menu upgrades applied: Attack x{upgradeAttackMultiplier:F2}, Health {currentHealth}/{maxHealth}, Range x{upgradeRangeMultiplier:F2}, Speed x{upgradeSpeedMultiplier:F2}, Generation x{upgradeGenerationMultiplier:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"UpgradeApplier not found - {name} will use base stats only");
        }
    }
    protected virtual void CheckPreviewState()
    {
        //Debug.Log($"CheckPreviewState called on {gameObject.name} - Current isInPreviewMode: {isInPreviewMode}");

        // IMPORTANT: If SetPreviewMode was already called explicitly, don't override it
        if (isInPreviewMode)
        {
            //Debug.Log("isInPreviewMode is already True - keeping it as True");
            return; // Don't change it if it's already set to true
        }

        // Check if this is a temporary preview object based on various clues

        // 1. Check if the object has a "Preview" layer
        if (gameObject.layer == LayerMask.NameToLayer("Preview"))
        {
            //Debug.Log("Detected preview via Preview layer");
            isInPreviewMode = true;
            return;
        }

        // 2. Check parent's name for hints like "Preview"
        if (transform.parent != null && transform.parent.name.Contains("Preview"))
        {
            //Debug.Log($"Detected preview via parent name: {transform.parent.name}");
            isInPreviewMode = true;
            return;
        }

        // 3. Check if object name contains "_PREVIEW" (set by QubitManager)
        if (gameObject.name.Contains("_PREVIEW"))
        {
            //Debug.Log("Detected preview via _PREVIEW in name");
            isInPreviewMode = true;
            return;
        }

        // 4. Check transparency - previews are often semi-transparent
        if (spriteRenderer != null && spriteRenderer.color.a < 0.9f)
        {
            //Debug.Log($"Detected preview via transparency: {spriteRenderer.color.a}");
            isInPreviewMode = true;
            return;
        }

        // 5. Check if the object has a temporary name (often has "(Clone)" suffix)
        if (gameObject.name.Contains("(Clone)") && !gameObject.scene.IsValid())
        {
            //Debug.Log("Detected preview via Clone name and invalid scene");
            isInPreviewMode = true;
            return;
        }

        // 6. Check for placement indicator
        Transform placementIndicator = transform.Find("PlacementIndicator");
        if (placementIndicator != null && placementIndicator.gameObject.activeSelf)
        {
            //Debug.Log("Detected preview via placement indicator");
            isInPreviewMode = true;
            return;
        }

        // 7. Check if all colliders are disabled (preview objects have disabled colliders)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
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
                //Debug.Log("Detected preview via all colliders disabled");
                isInPreviewMode = true;
                return;
            }
        }

        //Debug.Log("No preview indicators found - keeping isInPreviewMode as False");
        // Not in preview mode
        isInPreviewMode = false;
    }

    // Method to manually set preview state
    public virtual void SetPreviewMode(bool isPreview)
    {
        //Debug.Log($"SetPreviewMode called on {gameObject.name}: from {isInPreviewMode} to {isPreview}");
        isInPreviewMode = isPreview;
        //Debug.Log($"isInPreviewMode is now: {isInPreviewMode}");
    }

    public bool HasSuperpositionEffect()
    {
        return GetComponent<SuperpositionEffect>() != null;
    }

    protected virtual void Awake()
    {
        //Debug.Log($"Qubit Awake: {gameObject.name} - Initial isInPreviewMode: {isInPreviewMode}");

        // Get components
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();

        // Configure rigidbody for static objects
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.gravityScale = 0;
        }

        // Make sure the main collider is NOT a trigger (for physical collisions)
        if (mainCollider != null && mainCollider.isTrigger)
        {
            mainCollider.isTrigger = false;
        }

        // Make sure the attack range collider IS a trigger (for detection only)
        if (attackRangeCollider != null && !attackRangeCollider.isTrigger)
        {
            attackRangeCollider.isTrigger = true;
        }

        // Store our position for grid cleanup
        gridPosition = transform.position;

        // Try to find grid manager
        gridManager = FindObjectOfType<GridManager>();

        // ONLY check preview state if it wasn't already set explicitly
        // This prevents overriding SetPreviewMode(true) calls
        if (!isInPreviewMode)
        {
            //Debug.Log("Preview mode not set explicitly - checking preview state");
            CheckPreviewState();
        }
        else
        {
            //Debug.Log("Preview mode already set to True - skipping CheckPreviewState");
        }
        //Debug.Log($"Qubit Awake complete: {gameObject.name} - Final isInPreviewMode: {isInPreviewMode}");

        qubitBuffContainer.OnGameplayBuffAdded += HandleGameplayBuffAddedEvent;
    }

    protected virtual void Start()
    {
        // Make sure the object has the Qubit tag
        if (tag != "Qubit")
        {
            tag = "Qubit";
        }

        // Find resource manager
        resourceManager = ResourceManager.Instance;

        // Initialize health
        if (qubitData != null)
        {
            currentHealth = qubitData.maxHealth;
        }

        // Check preview state again in case it changed after Awake
        CheckPreviewState();
        ApplyMainMenuUpgrades();
    }

    protected virtual void Update()
    {
        // DEBUG LOG - Track preview mode for Clone objects
        if (gameObject.name.Contains("Clone"))
        {
            //Debug.Log($"Qubit Update: {gameObject.name} - isInPreviewMode: {isInPreviewMode}");
        }

        if (isInPreviewMode)
            return;

        if (qubitData == null)
            return;

        // Combat logic - for ranged attacks via projectiles
        if (qubitData.canAttack)
        {
            attackTimer += Time.deltaTime;

            // Use base stats - global upgrades will be applied via multipliers later
            float attackInterval = 1f / GetEffectiveAttackSpeed();

            if (attackTimer >= attackInterval)
            {
                TryRangedAttack();
                attackTimer = 0f;
            }
        }

        // Resource generation logic
        if (qubitData.canGenerate)
        {
            generationTimer += Time.deltaTime;

            // Use base generation interval with global multipliers
            if (generationTimer >= generationInterval)
            {
                GenerateResource();
                generationTimer = 0f;
            }
        }
    }

    #region Global Upgrade Integration

    // Get effective attack speed with global upgrades
    protected virtual float GetEffectiveAttackSpeed()
    {
        float baseSpeed = qubitData.attackSpeed;

        // Apply global upgrades based on qubit type
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            string upgradeType = GetUpgradeType();
            float primaryMultiplier = upgradeManager.GetUpgradeMultiplier(upgradeType);
            
            // BALANCED: Reduce the impact of the multiplier on attack speed
            float speedBonus = (primaryMultiplier - 1f) * 0.1f; // Only 40% of the multiplier effect
            baseSpeed *= (1f + speedBonus);
        }

        // Apply superposition speed boost if present (unchanged)
        SuperpositionEffect superpositionEffect = GetComponent<SuperpositionEffect>();
        if (superpositionEffect != null)
        {
            float superpositionMultiplier = superpositionEffect.GetSpeedMultiplier();
            baseSpeed *= superpositionMultiplier;
        }

        // Apply confusion speed boost if present (unchanged)
        ConfusedQubit confusedComponent = GetComponent<ConfusedQubit>();
        if (confusedComponent != null && confusedComponent.ShouldUseConfusedAttack())
        {
            float confusedSpeedMultiplier = confusedComponent.GetConfusedAttackSpeed() / qubitData.attackSpeed;
            baseSpeed *= confusedSpeedMultiplier;
        }

        return baseSpeed;
    }

    public virtual void ApplyUpgradeMultipliers(float attackMult, float healthMult, float rangeMult, float speedMult = 1f, float generationMult = 1f)
    {
        upgradeAttackMultiplier = attackMult;
        upgradeHealthMultiplier = healthMult;
        upgradeRangeMultiplier = rangeMult;

        // Apply health upgrade
        if (qubitData != null)
        {
            int baseMaxHealth = qubitData.maxHealth;
            int newMaxHealth = Mathf.RoundToInt(baseMaxHealth * healthMult);
            int healthDifference = newMaxHealth - maxHealth;
            
            maxHealth = newMaxHealth;
            currentHealth += healthDifference; // Also increase current health
            
            Debug.Log($"Qubit {name} upgrades applied: Attack x{attackMult:F2}, Health {currentHealth}/{maxHealth}, Range x{rangeMult:F2}, Speed x{speedMult:F2}, Generation x{generationMult:F2}");
        }
        else
        {
            // Fallback if no QubitData
            int newMaxHealth = Mathf.RoundToInt(50 * healthMult); // 50 is default
            int healthDifference = newMaxHealth - maxHealth;
            
            maxHealth = newMaxHealth;
            currentHealth += healthDifference;
        }
    }

    [Header("Main Menu Upgrades")]
    [SerializeField] protected float upgradeAttackMultiplier = 1f;
    [SerializeField] protected float upgradeHealthMultiplier = 1f;
    [SerializeField] protected float upgradeRangeMultiplier = 1f;
    // Get effective attack power with global upgrades
    protected virtual int GetEffectiveAttackPower()
    {
        float basePower = qubitData.attackPower;

        // Apply global upgrades based on qubit type
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            string upgradeType = GetUpgradeType();
            float multiplier = upgradeManager.GetUpgradeMultiplier(upgradeType);

            basePower *= multiplier;
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                basePower *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }

        return Mathf.RoundToInt(basePower);
    }
    public virtual int GetUpgradedAttackDamage()
    {
        if (qubitData != null)
        {
            float basePower = qubitData.attackPower * upgradeAttackMultiplier;
            
            // Apply global upgrades
            GlobalUpgradeManager globalUpgradeManager = GlobalUpgradeManager.Instance;
            if (globalUpgradeManager != null)
            {
                string upgradeType = GetUpgradeType();
                float globalMultiplier = globalUpgradeManager.GetUpgradeMultiplier(upgradeType);
                basePower *= globalMultiplier;
            }
            
            // Apply buff multipliers
            IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                basePower *= entanglementBuff.EntanglementBuffMultiplier;
            }
            
            return Mathf.RoundToInt(basePower);
        }
        return Mathf.RoundToInt(10 * upgradeAttackMultiplier); // Default fallback
    }

    public virtual float GetUpgradedAttackRange()
    {
        if (qubitData != null)
        {
            float baseRange = qubitData.attackRange * upgradeRangeMultiplier;
            
            // Apply global upgrades
            GlobalUpgradeManager globalUpgradeManager = GlobalUpgradeManager.Instance;
            if (globalUpgradeManager != null)
            {
                string upgradeType = GetUpgradeType();
                float globalMultiplier = globalUpgradeManager.GetUpgradeMultiplier(upgradeType);
                baseRange *= globalMultiplier;
            }
            
            // Apply buff multipliers
            IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                baseRange *= entanglementBuff.EntanglementBuffMultiplier;
            }
            
            return baseRange;
        }
        return 5f * upgradeRangeMultiplier; // Default fallback
    }

    public virtual float GetUpgradedGenerationRate()
    {
        if (QubitData != null)
        {
            // You might want a generation multiplier for this
            return QubitData.informationPerSecond;
        }
        return 0f;
    }

    public bool CanBeSuperpositioned()
    {
        // Superposition qubits cannot be enhanced further
        if (GetComponent<SuperpositionQubit>() != null)
            return false;

        // Qubits with permanent superposition effects cannot be enhanced again
        SuperpositionEffect existingEffect = GetComponent<SuperpositionEffect>();
        if (existingEffect != null && existingEffect.IsPermanent())
            return false;

        return true;
    }

    public string GetSuperpositionInfo()
    {
        SuperpositionEffect effect = GetComponent<SuperpositionEffect>();
        if (effect != null)
        {
            return effect.GetEffectDescription();
        }

        // Check if this is a superposition qubit
        if (GetComponent<SuperpositionQubit>() != null)
        {
            return "Superposition Qubit: Dual Attack & Generation Capabilities";
        }

        return "";
    }

    // Get effective attack range with global upgrades
    protected virtual float GetEffectiveAttackRange()
    {
        float baseRange = qubitData.attackRange;

        // Apply global upgrades based on qubit type
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            string upgradeType = GetUpgradeType();
            float multiplier = upgradeManager.GetUpgradeMultiplier(upgradeType);
            return baseRange * multiplier;
        }

        return baseRange;
    }

    

    // Get effective generation rate with global upgrades
    protected virtual float GetEffectiveGenerationRate()
    {
        float baseRate = qubitData.informationPerSecond;

        // Apply global upgrades based on qubit type
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            string upgradeType = GetUpgradeType();
            float globalMultiplier = upgradeManager.GetUpgradeMultiplier(upgradeType);
            baseRate *= globalMultiplier;
        }

        // Apply superposition resource boost if present
        SuperpositionEffect superpositionEffect = GetComponent<SuperpositionEffect>();
        if (superpositionEffect != null)
        {
            float superpositionMultiplier = superpositionEffect.GetResourceMultiplier();
            baseRate *= superpositionMultiplier;

            // Debug log occasionally to show the effect is working
            if (Time.time % 5f < 0.1f)
            {
                //Debug.Log($"💎 {gameObject.name} generation rate: {baseRate:F2} (includes superposition boost)");
            }
        }

        //Apply Gameplay Buffs based on Gameplay Buff type
        IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
        if (gameplayBuff != null)
        {
            if (gameplayBuff is EntanglementBuff entanglementBuff)
            {
                // Apply the entanglement buff multiplier
                baseRate *= entanglementBuff.EntanglementBuffMultiplier;
            }
        }

        return baseRate;
    }

    // Get effective projectile speed with global upgrades
    protected virtual float GetEffectiveProjectileSpeed()
    {
        float baseSpeed = 10f; // Default fallback

        // Try to get from QubitData if available
        if (qubitData != null)
        {
            System.Reflection.FieldInfo speedField = typeof(QubitData).GetField("projectileSpeed");
            if (speedField != null)
            {
                baseSpeed = (float)speedField.GetValue(qubitData);
            }
        }

        // Apply global upgrades based on qubit type
        GlobalUpgradeManager upgradeManager = GlobalUpgradeManager.Instance;
        if (upgradeManager != null)
        {
            string upgradeType = GetUpgradeType();
            float multiplier = upgradeManager.GetUpgradeMultiplier(upgradeType);

            // BALANCED: Reduce projectile speed scaling
            float speedBonus = (multiplier - 1f) * 0.1f; // Only 25% of multiplier effect on projectile speed
            
            // Also get a SMALLER boost from core level for all qubits
            int coreLevel = upgradeManager.GetUpgradeLevel("core");
            float coreBonus = coreLevel * 0.1f; // REDUCED from 0.5f to 0.2f speed per core level

            return (baseSpeed + coreBonus) * (1f + speedBonus);
        }

        return baseSpeed;
    }

    // Determine which upgrade type affects this qubit
    protected virtual string GetUpgradeType()
    {
        // Check if this is a Zero Qubit (resource generation)
        if (GetComponent<ZeroQubit>() != null)
        {
            return "zeroQubit";
        }

        // Check if this is a One Qubit (attack)
        if (GetComponent<OneQubit>() != null)
        {
            return "oneQubit";
        }

        // Default to oneQubit for unknown types
        return "oneQubit";
    }

    #endregion

    #region Grid and Position Management

    public virtual Vector3 GetGridPosition()
    {
        return gridPosition;
    }

    // Called when the qubit is placed on the grid - stores the grid position
    public void SetGridPosition(Vector3 position)
    {
        this.gridPosition = position;
        //Debug.Log($"Qubit {gameObject.name} grid position set to {position}");
    }

    #endregion

    #region Entanglement System
    #endregion

    #region Gameplay Buff System
    public void AddBuff(IGameplayBuff buff)
    {
        if (qubitData == null || buff == null || isInPreviewMode)
            return;
        // Add the buff to the container
        qubitBuffContainer.AddBuff(buff);
        // Apply the buff immediately?
    }

    public void RemoveBuff(IGameplayBuff buff)
    {
        if (qubitData == null || buff == null || isInPreviewMode)
            return;
        // Remove the buff from the container
        qubitBuffContainer.RemoveBuff(buff);
    }

    public void ClearAllBuffs()
    {
        if (qubitData == null || isInPreviewMode)
            return;
        // Clear all buffs in the container
        qubitBuffContainer.Clear();
    }

    public void HandleGameplayBuffAddedEvent(IGameplayBuff gameplayBuff)
    {
        if (gameplayBuff is EntanglementBuff)
        {
            // Handle entanglement buff application
            EntanglementBuff entanglementBuff = gameplayBuff as EntanglementBuff;
            if (entanglementBuff != null)
            {
                Debug.Log($"🔗 Entanglement buff applied to {gameObject.name}");
            }
        }
        else
        {
            // Handle other buffs if needed
            //Debug.Log($"Gameplay buff added: {gameplayBuff.GetType().Name}");
        }
    }

    #endregion

    #region Combat System

        // RANGED ATTACK functionality - uses the attack range to find distant enemies
    protected virtual void TryRangedAttack()
    {
        // Skip attacks if in preview mode
        if (isInPreviewMode)
            return;

        if (!qubitData.canAttack)
            return;

        // Find the nearest enemy within range that's NOT already in direct contact
        GameObject nearestEnemy = FindEnemyInAttackRange();

        if (nearestEnemy != null)
        {
            // Only attack if the enemy is not already in direct collision with us
            if (!IsEnemyInContact(nearestEnemy))
            {
                FireProjectile(nearestEnemy);
            }
        }
    }

    // Find an enemy within the attack range - for ranged attacks ONLY
    protected virtual GameObject FindEnemyInAttackRange()
    {
        if (qubitData == null || !qubitData.canAttack)
            return null;

        // Use effective range with global upgrades
        float range = GetEffectiveAttackRange();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, range);

        GameObject nearest = null;
        float minDistance = range;

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);

                if (distance <= range && distance < minDistance)
                {
                    nearest = collider.gameObject;
                    minDistance = distance;
                }
            }
        }

        return nearest;
    }

    // Check if an enemy is in direct contact with us
    protected virtual bool IsEnemyInContact(GameObject enemy)
    {
        if (enemy == null || mainCollider == null)
            return false;

        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider == null)
            return false;

        // Check if colliders are touching
        return mainCollider.IsTouching(enemyCollider);
    }

    // Fire a projectile at a ranged enemy - UPDATED WITH PROJECTILE SPEED
    protected virtual void FireProjectile(GameObject enemy)
    {
        // Skip projectile creation if in preview mode
        if (isInPreviewMode)
            return;

        if (projectilePrefab == null || qubitData == null)
            return;

        Vector2 directionToEnemy;

        // Check if this is a confused qubit
        ConfusedQubit confusedComponent = GetComponent<ConfusedQubit>();
        if (confusedComponent != null && confusedComponent.ShouldUseConfusedAttack())
        {
            // Use random direction instead of targeting enemy
            directionToEnemy = confusedComponent.GetRandomAttackDirection();
            //Debug.Log($"🌀 Confused {gameObject.name} shooting randomly!");
        }
        else
        {
            // Normal targeting behavior
            directionToEnemy = (enemy.transform.position - transform.position).normalized;
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectileComponent = projectile.GetComponent<Projectile>();

        if (projectileComponent != null)
        {
            // Use effective attack power and projectile speed with global upgrades
            int attackPower = GetEffectiveAttackPower();
            float projectileSpeed = GetEffectiveProjectileSpeed();

            // Use the enhanced Initialize method with speed parameter
            projectileComponent.Initialize(directionToEnemy, attackPower, projectileSpeed);

            //Debug.Log($"Qubit {gameObject.name} fired projectile with {attackPower} damage, speed: {projectileSpeed:F2}");
        }
    }

    // CONTACT COMBAT - when enemies physically touch the qubit
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        // Skip collision handling if in preview mode
        if (isInPreviewMode)
            return;

        // Only handle direct collisions with the qubit's main body
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //Debug.Log($"Qubit {gameObject.name} collided with enemy {collision.gameObject.name}");

            // Initial damage to the enemy
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Deal initial damage to the enemy using effective attack power
                int attackPower = GetEffectiveAttackPower();
                enemy.TakeDamage(attackPower);

                // Start a coroutine to deal continuous damage
                StartCoroutine(DealContinuousDamage(enemy));
            }
        }
    }

    // Continuously deal damage to an enemy while in contact
    protected virtual IEnumerator DealContinuousDamage(Enemy enemy)
    {
        // Return early if in preview mode
        if (isInPreviewMode) yield break;

        if (enemy == null || qubitData == null) yield break;

        float damageInterval = 1f; // Deal damage every second

        while (enemy != null && currentHealth > 0)
        {
            yield return new WaitForSeconds(damageInterval);

            // Check again if we entered preview mode
            if (isInPreviewMode) yield break;

            if (enemy != null)
            {
                // Deal damage to the enemy using effective attack power
                int attackPower = GetEffectiveAttackPower();
                enemy.TakeDamage(attackPower);
                //Debug.Log($"Qubit {gameObject.name} dealing continuous damage: {attackPower}");
            }
        }
    }

    #endregion

    #region Resource Generation

    // Resource generation functionality
    protected virtual void GenerateResource()
    {
        // Skip resource generation if in preview mode
        if (isInPreviewMode)
            return;

        if (resourceManager == null || !qubitData.canGenerate)
            return;

        // Check if this is a confused qubit that handles its own generation
        ConfusedQubit confusedComponent = GetComponent<ConfusedQubit>();
        if (confusedComponent != null && confusedComponent.ShouldUseConfusedGeneration())
        {
            // Confused ZeroQubits handle their own generation timing and bursts
            // Don't use the normal generation - let ConfusedQubit handle it
            return;
        }

        // Normal generation behavior
        float effectiveRate = GetEffectiveGenerationRate();
        int generatedAmount = Mathf.RoundToInt(effectiveRate);
        resourceManager.AddInformation(generatedAmount);

        // Log occasionally to avoid spam
        if (Time.time % 3f < 0.1f) // Log every ~3 seconds
        {
            //Debug.Log($"Qubit {gameObject.name} generated {generatedAmount} information (Rate: {effectiveRate:F1}/s)");
        }
    }

    #endregion

    #region Health and Damage System

    // IDamageable implementation
    public virtual void TakeDamage(int damage)
    {
        // Skip damage handling if in preview mode
        if (isInPreviewMode)
            return;

        if (qubitData == null) return;

        // Check if this qubit is part of an entanglement network
        List<Qubit> entanglementNetwork = GetEntanglementNetwork();

        if (entanglementNetwork.Count > 1)
        {
            // Distribute damage across the entire entanglement network
            DistributeDamageAcrossNetwork(damage, entanglementNetwork);
        }
        else
        {
            // No entanglement - take damage normally (use original logic)
            ApplyDirectDamageToSelf(damage);
        }
    }

    private List<Qubit> GetEntanglementNetwork()
    {
        List<Qubit> network = new List<Qubit>();

        // Always include ourselves
        network.Add(this);

        // Get the QubitManager to access entanglement data
        QubitManager qubitManager = QubitManager.Instance;
        if (qubitManager == null)
            return network;

        // Find all qubits directly entangled with this one
        HashSet<Qubit> processedQubits = new HashSet<Qubit>();
        Queue<Qubit> qubitsToProcess = new Queue<Qubit>();

        qubitsToProcess.Enqueue(this);
        processedQubits.Add(this);

        // Use breadth-first search to find the entire connected network
        while (qubitsToProcess.Count > 0)
        {
            Qubit currentQubit = qubitsToProcess.Dequeue();

            // Check all entanglements for connections to this qubit
            foreach (var entanglement in qubitManager.Entanglements)
            {
                if (entanglement.QubitSource == null || entanglement.QubitTarget == null)
                    continue;

                Qubit connectedQubit = null;

                // If current qubit is the source, the target is connected
                if (entanglement.QubitSource == currentQubit)
                    connectedQubit = entanglement.QubitTarget;
                // If current qubit is the target, the source is connected
                else if (entanglement.QubitTarget == currentQubit)
                    connectedQubit = entanglement.QubitSource;

                // Add newly discovered qubit to the network
                if (connectedQubit != null && !processedQubits.Contains(connectedQubit))
                {
                    network.Add(connectedQubit);
                    processedQubits.Add(connectedQubit);
                    qubitsToProcess.Enqueue(connectedQubit);
                }
            }
        }

        return network;
    }

    private void DistributeDamageAcrossNetwork(int totalDamage, List<Qubit> network)
    {
        if (network.Count == 0) return;

        // Calculate damage per qubit (integer division)
        int damagePerQubit = totalDamage / network.Count;
        int remainderDamage = totalDamage % network.Count;

        Debug.Log($"🔗 Quantum Entanglement: Distributing {totalDamage} damage across {network.Count} qubits ({damagePerQubit} each + {remainderDamage} remainder)");

        // Apply damage to each qubit in the network
        for (int i = 0; i < network.Count; i++)
        {
            Qubit qubit = network[i];
            if (qubit == null || qubit.gameObject == null) continue;

            // First few qubits get the remainder damage
            int actualDamage = damagePerQubit + (i < remainderDamage ? 1 : 0);

            // Apply damage directly (bypass TakeDamage to prevent infinite recursion)
            qubit.ApplyDirectDamageToSelf(actualDamage);

            Debug.Log($"⚡ {qubit.gameObject.name} took {actualDamage} distributed damage. Health: {qubit.currentHealth}/{qubit.qubitData.maxHealth}");
        }
    }

    private void ApplyDirectDamageToSelf(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"💥 {gameObject.name} took {damage} direct damage. Health: {currentHealth}/{qubitData.maxHealth}");

        // Notify listeners about the damage
        OnQubitDamaged?.Invoke();

        QubitManager qubitManager = QubitManager.Instance;
        if (qubitManager != null)
        {
            qubitManager.OnQubitDamaged(gameObject);
            //Debug.Log($"Notified QubitManager of qubit destruction at {position}");
        }

        this.ClearAllBuffs(); // Clear buffs on damage

        // Visual feedback (original logic)
        StartCoroutine(FlashDamage());

        // Check for death (original logic)
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ApplyDirectDamage(int damage)
    {
        currentHealth -= damage;
        //Debug.Log($"💥 {gameObject.name} took {damage} direct damage. Health: {currentHealth}/{qubitData.maxHealth}");

        // Visual feedback
        StartCoroutine(FlashDamage());

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual IEnumerator FlashDamage()
    {
        // Skip visual effects if in preview mode
        if (isInPreviewMode) yield break;

        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    protected virtual void Die()
    {
        // Skip death if in preview mode
        if (isInPreviewMode)
            return;

        if (qubitData != null)
        {
            //Debug.Log($"{qubitData.qubitName} was destroyed!");
        }

        // Store position before destruction for grid cleanup
        Vector3 position = transform.position;

        // Trigger the OnQubitDestroyed event
        OnQubitDestroyed?.Invoke(position);

        // Notify QubitManager for count tracking
        QubitManager qubitManager = QubitManager.Instance;
        if (qubitManager != null)
        {
            qubitManager.OnQubitDestroyed(gameObject);
            //Debug.Log($"Notified QubitManager of qubit destruction at {position}");
        }

        // Free the grid cell using stored grid position
        if (gridManager == null)
        {
            // Try to find grid manager if not already cached
            gridManager = FindObjectOfType<GridManager>();
        }

        if (gridManager != null)
        {
            gridManager.FreeCell(position);

            // Also try with the stored grid position for reliability
            if (gridPosition != Vector3.zero && Vector3.Distance(position, gridPosition) > 0.1f)
            {
                gridManager.FreeCell(gridPosition);
            }
        }

        Destroy(gameObject);
    }

    // Called when object is destroyed by any means
    protected virtual void OnDestroy()
    {
        // Only execute if we're being destroyed during gameplay, not scene unload
        if (gameObject.scene.isLoaded && !isInPreviewMode)
        {
            // Notify QubitManager for count tracking
            QubitManager qubitManager = QubitManager.Instance;
            if (qubitManager != null)
            {
                qubitManager.OnQubitDestroyed(gameObject);
            }

            // Free the grid cell again as a safety backup
            if (gridManager == null)
            {
                // Try to find grid manager if not already cached
                gridManager = FindObjectOfType<GridManager>();
            }

            if (gridManager != null)
            {
                // Use both the stored position and current position to be extra safe
                gridManager.FreeCell(transform.position);

                // Free using the stored position too if it's different
                if (gridPosition != Vector3.zero && Vector3.Distance(transform.position, gridPosition) > 0.1f)
                {
                    gridManager.FreeCell(gridPosition);
                }
            }
        }
    }

    #endregion

    #region Public Properties and Methods

    // Get current health
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Get health percentage (0-1)
    public float GetHealthPercentage()
    {
        if (qubitData == null || qubitData.maxHealth <= 0)
            return 0f;

        return (float)currentHealth / qubitData.maxHealth;
    }

    // Property for external access
    public QubitData QubitData => qubitData;

    // Get effective stats for UI display
    public float GetEffectiveAttackRangeForUI()
    {
        return GetEffectiveAttackRange();
    }

    public int GetEffectiveAttackPowerForUI()
    {
        return GetEffectiveAttackPower();
    }

    public float GetEffectiveAttackSpeedForUI()
    {
        return GetEffectiveAttackSpeed();
    }

    public float GetEffectiveGenerationRateForUI()
    {
        return GetEffectiveGenerationRate();
    }

    public float GetEffectiveProjectileSpeedForUI()
    {
        return GetEffectiveProjectileSpeed();
    }

    // Method to manually apply global upgrades (called by GlobalUpgradeManager)
    public virtual void ApplyGlobalUpgrades()
    {
        // This method is called when global upgrades change
        // No need to store individual levels anymore - everything is calculated dynamically
        //Debug.Log($"Global upgrades applied to {gameObject.name}");
    }

    #endregion

    #region Editor Visualization

    // Visualize attack range in the editor
    protected virtual void OnDrawGizmosSelected()
    {
        if (qubitData != null && qubitData.canAttack)
        {
            Gizmos.color = Color.yellow;
            // Use effective range if in play mode, base range if in edit mode
            float range = Application.isPlaying ? GetEffectiveAttackRange() : qubitData.attackRange;
            Gizmos.DrawWireSphere(transform.position, range);

            // Draw a smaller circle to show the qubit itself
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        if (qubitData != null && qubitData.canGenerate)
        {
            Gizmos.color = Color.blue;
            // Draw a small sphere to indicate this qubit generates resources
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
    }

    public string GetConfusionInfo()
    {
        ConfusedQubit confusedComponent = GetComponent<ConfusedQubit>();
        if (confusedComponent != null)
        {
            return confusedComponent.GetConfusionInfo();
        }
        return "";
    }
    
    public bool IsConfused()
    {
        ConfusedQubit confusedComponent = GetComponent<ConfusedQubit>();
        return confusedComponent != null && confusedComponent.IsConfused();
    }
    #endregion
}