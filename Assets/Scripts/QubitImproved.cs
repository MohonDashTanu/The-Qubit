using UnityEngine;
using System.Collections;
using QuantumTD.Upgrades;
using System.Threading.Tasks;

namespace QuantumTD.Qubits
{
    /// <summary>
    /// Improved Qubit class that works with the new upgrade system
    /// This can be used as a reference for updating the existing Qubit class
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class QubitImproved : MonoBehaviour, IDamageable, IBuffable
    {
        // Static event for qubit destruction
        public static event System.Action<Vector3> OnQubitDestroyed;
        
        [Header("Qubit Configuration")]
        [SerializeField] protected QubitData qubitData;
        
        [Header("Attack Properties")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected Collider2D attackRangeCollider;
        
        [Header("Runtime State")]
        [SerializeField] protected int currentHealth; // Serialized to view in inspector

        // Internal variables
        protected float attackTimer = 0f;
        protected float generationTimer = 0f;
        protected float entanglementTimer = 0f;
        protected ResourceManager resourceManager;
        protected SpriteRenderer spriteRenderer;
        protected Rigidbody2D rb;
        protected Collider2D mainCollider;

        public bool isEntangled => qubitBuffContainer.GetBuff<EntanglementBuff>() != null;
        public GameplayBuffContainer qubitBuffContainer = new GameplayBuffContainer();

        // Upgrade component reference
        protected QubitUpgradeComponent upgradeComponent;
        
        // Grid system reference
        protected GridManager gridManager;
        
        // Store our grid cell position for cleanup
        protected Vector3 gridPosition;
        
        // Entanglement related variables
        protected float entanglementRate = 0.1f;

        protected Qubit[] entangledQubits = new Qubit[170];
        
        protected virtual void Awake()
        {
            // Get components
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            mainCollider = GetComponent<Collider2D>();
            upgradeComponent = GetComponent<QubitUpgradeComponent>();
            
            // If upgrade component doesn't exist, add it
            if (upgradeComponent == null)
            {
                upgradeComponent = gameObject.AddComponent<QubitUpgradeComponent>();
            }
            
            // Configure rigidbody for static objects
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                rb.gravityScale = 0;
            }
            
            // Make sure the main collider is NOT a trigger
            if (mainCollider != null && mainCollider.isTrigger)
            {
                //Debug.LogWarning("Main qubit collider was set as trigger - disabling trigger to allow physical collisions");
                mainCollider.isTrigger = false;
            }
            
            // Make sure the attack range collider IS a trigger
            if (attackRangeCollider != null && !attackRangeCollider.isTrigger)
            {
                //Debug.LogWarning("Attack range collider should be a trigger - fixing");
                attackRangeCollider.isTrigger = true;
            }
            
            // Store our position for grid cleanup
            gridPosition = transform.position;
            
            // Try to find grid manager
            gridManager = FindObjectOfType<GridManager>();

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
        }
        
        protected virtual void Update()
        {
            if (qubitData == null)
                return;
            
            // Combat logic - for ranged attacks via projectiles
            if (qubitData.canAttack)
            {
                attackTimer += Time.deltaTime;

                float attackSpeed = qubitData.attackSpeed + GetUpgradedStat(UpgradeType.Speed);
                //Apply Gameplay Buffs based on Gameplay Buff type
                IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
                if (gameplayBuff != null)
                {
                    if (gameplayBuff is EntanglementBuff entanglementBuff)
                    {
                        // Apply the entanglement buff multiplier
                        attackSpeed *= entanglementBuff.EntanglementBuffMultiplier;
                    }
                }

                float attackInterval = 1f / (attackSpeed);
                
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
                float generationInterval = 1f / (1f + GetUpgradedStat(UpgradeType.Generation));
                
                if (generationTimer >= generationInterval)
                {
                    GenerateResource();
                    generationTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// Get the value of an upgraded stat
        /// </summary>
        protected virtual float GetUpgradedStat(UpgradeType upgradeType)
        {
            if (upgradeComponent != null)
            {
                return upgradeComponent.GetUpgradeValue(upgradeType);
            }
            
            return 0f; // Default fallback
        }
        
        #region Attack Logic
        
        /// <summary>
        /// Try to perform a ranged attack against nearby enemies
        /// </summary>
        protected virtual void TryRangedAttack()
        {
            if (!qubitData.canAttack)
                return;
            
            // Find the nearest enemy within range
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
        
        /// <summary>
        /// Find an enemy within attack range
        /// </summary>
        protected virtual GameObject FindEnemyInAttackRange()
        {
            if (qubitData == null || !qubitData.canAttack)
                return null;
            
            // Calculate attack range with upgrades
            float range = qubitData.attackRange + GetUpgradedStat(UpgradeType.Range);
            
            // Use Physics2D.OverlapCircleAll for better 2D collision detection
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
        
        /// <summary>
        /// Check if an enemy is in direct contact with the qubit
        /// </summary>
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
        
        /// <summary>
        /// Fire a projectile at a target enemy
        /// </summary>
        protected virtual void FireProjectile(GameObject enemy)
        {
            if (projectilePrefab == null || qubitData == null)
                return;
            
            // Calculate direction to enemy
            Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            
            if (projectileComponent != null)
            {
                // Calculate attack power with upgrades
                int attackPower = qubitData.attackPower + Mathf.RoundToInt(GetUpgradedStat(UpgradeType.Power));

                //Apply Gameplay Buffs based on Gameplay Buff type
                IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
                if (gameplayBuff != null)
                {
                    if (gameplayBuff is EntanglementBuff entanglementBuff)
                    {
                        // Apply the entanglement buff multiplier
                        attackPower = Mathf.RoundToInt(attackPower * entanglementBuff.EntanglementBuffMultiplier);
                    }
                }


                projectileComponent.Initialize(directionToEnemy, attackPower);
            }
        }
        
        #endregion
        
        #region Resource Generation
        
        /// <summary>
        /// Generate resources based on qubit capabilities
        /// </summary>
        protected virtual void GenerateResource()
        {
            if (resourceManager == null || !qubitData.canGenerate)
                return;
                
            // Calculate generation amount with upgrades
            float generationMultiplier = 1f + GetUpgradedStat(UpgradeType.Generation);
            int generatedAmount = Mathf.RoundToInt(qubitData.informationPerSecond * generationMultiplier);
            
            resourceManager.AddInformation(generatedAmount);
        }
        
        #endregion
        
        #region Combat & Damage Handling
        
        /// <summary>
        /// Handle collision with enemies
        /// </summary>
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            // Only handle direct collisions with the qubit's main body
            if (collision.gameObject.CompareTag("Enemy"))
            {
                //Debug.Log($"Qubit {gameObject.name} collided with enemy {collision.gameObject.name}");
                
                // Initial damage to the enemy
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Calculate attack power with upgrades
                    int attackPower = qubitData.attackPower + Mathf.RoundToInt(GetUpgradedStat(UpgradeType.Power));

                    //Apply Gameplay Buffs based on Gameplay Buff type
                    IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
                    if (gameplayBuff != null)
                    {
                        if (gameplayBuff is EntanglementBuff entanglementBuff)
                        {
                            // Apply the entanglement buff multiplier
                            attackPower = Mathf.RoundToInt(attackPower * entanglementBuff.EntanglementBuffMultiplier);
                        }
                    }

                    // Deal initial damage to the enemy
                    enemy.TakeDamage(attackPower);
                    
                    // Start a coroutine to deal continuous damage
                    StartCoroutine(DealContinuousDamage(enemy));
                }
            }
        }
        
        /// <summary>
        /// Deal continuous damage to an enemy while in contact
        /// </summary>
        protected virtual IEnumerator DealContinuousDamage(Enemy enemy)
        {
            if (enemy == null || qubitData == null) yield break;
            
            float damageInterval = 1f; // Deal damage every second
            
            while (enemy != null && currentHealth > 0)
            {
                yield return new WaitForSeconds(damageInterval);
                
                if (enemy != null)
                {
                    // Calculate attack power with upgrades
                    int attackPower = qubitData.attackPower + Mathf.RoundToInt(GetUpgradedStat(UpgradeType.Power));

                    //Apply Gameplay Buffs based on Gameplay Buff type
                    IGameplayBuff gameplayBuff = qubitBuffContainer.GetBuff<EntanglementBuff>();
                    if (gameplayBuff != null)
                    {
                        if (gameplayBuff is EntanglementBuff entanglementBuff)
                        {
                            // Apply the entanglement buff multiplier
                            attackPower = Mathf.RoundToInt(attackPower * entanglementBuff.EntanglementBuffMultiplier);
                        }
                    }

                    // Deal damage to the enemy
                    enemy.TakeDamage(attackPower);
                    //Debug.Log($"Qubit dealing continuous damage to enemy: {attackPower}");
                }
            }
        }
        
        /// <summary>
        /// Implementation of IDamageable interface to take damage
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (qubitData == null) return;
            
            currentHealth -= damage;
            //Debug.Log($"{qubitData.qubitName} took {damage} damage. Health: {currentHealth}/{qubitData.maxHealth}");

            QubitManager qubitManager = QubitManager.Instance;
            if (qubitManager != null)
            {
                qubitManager.OnQubitDamaged(gameObject);
                //Debug.Log($"Notified QubitManager of qubit destruction at {position}");
            }

            this.ClearAllBuffs(); // Clear buffs on damage

            // Visual feedback
            StartCoroutine(FlashDamage());
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Visual feedback when taking damage
        /// </summary>
        protected virtual IEnumerator FlashDamage()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = originalColor;
            }
        }
        
        /// <summary>
        /// Handle death of the qubit
        /// </summary>
        protected virtual void Die()
        {
            if (qubitData != null)
            {
                //Debug.Log($"{qubitData.qubitName} was destroyed!");
            }
            
            // Store position before destruction for grid cleanup
            Vector3 position = transform.position;
            
            // Trigger the OnQubitDestroyed event
            OnQubitDestroyed?.Invoke(position);
            
            // Free the grid cell using stored grid position
            if (gridManager == null)
            {
                // Try to find grid manager if not already cached
                gridManager = FindObjectOfType<GridManager>();
            }
            
            if (gridManager != null)
            {
                //Debug.Log($"Freeing grid cell at {position} due to qubit destruction");
                gridManager.FreeCell(position);
                
                // Also try with the stored grid position for reliability
                if (gridPosition != Vector3.zero && Vector3.Distance(position, gridPosition) > 0.1f)
                {
                    //Debug.Log($"Also freeing grid cell at stored position {gridPosition}");
                    gridManager.FreeCell(gridPosition);
                }
            }
            
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Entanglement Logic
        
        public virtual Vector3 GetGridPosition()
        {
            return gridPosition;
        }
        
        public virtual (string resultMessage, bool result) TryAddEntangledQubit(Qubit targetQubit)
        {
            if (targetQubit == null)
                return ("Target qubit is null.", false);
                
            if (IsEntangledWith(targetQubit))
                return ("Already entangled with target qubit.", false);
                
            for (int i = 0; i < entangledQubits.Length; i++)
            {
                if (entangledQubits[i] == targetQubit)
                {
                    continue;
                }
                
                if (entangledQubits[i] == null)
                {
                    entangledQubits[i] = targetQubit;
                    return ($"{this.GetGridPosition()} Successfully added {targetQubit.GetGridPosition()} as entangled qubit.", true);
                }
            }
            
            return ($"{this.GetGridPosition()} Already entangled with all qubits.", false);
        }
        
        public virtual bool IsEntangledWith(Qubit targetQubit)
        {
            if (targetQubit == null)
                return false;
                
            for (int i = 0; i < entangledQubits.Length; i++)
            {
                if (entangledQubits[i] == targetQubit)
                {
                    return true;
                }
            }
            
            return false;
        }

        #endregion

        #region Gameplay Buff System
        public void AddBuff(IGameplayBuff buff)
        {
            if (qubitData == null) return;
            // Add the buff to the container
            qubitBuffContainer.AddBuff(buff);
            // Apply the buff immediately?
        }

        public void RemoveBuff(IGameplayBuff buff)
        {
            if (qubitData == null) return;
            // Remove the buff from the container
            qubitBuffContainer.RemoveBuff(buff);
        }

        public void ClearAllBuffs()
        {
            if (qubitData == null) return;
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
                    Debug.Log($" Entanglement buff applied to {gameObject.name}");
                }
            }
            else
            {
                // Handle other buffs if needed
                //Debug.Log($"Gameplay buff added: {gameplayBuff.GetType().Name}");
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the qubit is placed on the grid
        /// </summary>
        public void SetGridPosition(Vector3 position)
        {
            this.gridPosition = position;
        }
        
        /// <summary>
        /// Get current health
        /// </summary>
        public int GetCurrentHealth()
        {
            return currentHealth;
        }
        
        /// <summary>
        /// Get health percentage (0-1)
        /// </summary>
        public float GetHealthPercentage()
        {
            if (qubitData == null || qubitData.maxHealth <= 0)
                return 0f;
                
            return (float)currentHealth / qubitData.maxHealth;
        }
        
        /// <summary>
        /// Property for external access to qubit data
        /// </summary>
        public QubitData QubitData => qubitData;
        
        /// <summary>
        /// The following methods are for backward compatibility with the old upgrade system
        /// These can be removed once the full transition is complete
        /// </summary>
        
        public void lvlup(string type, int max)
        {
            if (upgradeComponent != null)
            {
                UpgradeType upgradeType;
                
                switch (type)
                {
                    case "power":
                        upgradeType = UpgradeType.Power;
                        break;
                    case "range":
                        upgradeType = UpgradeType.Range;
                        break;
                    case "speed":
                        upgradeType = UpgradeType.Speed;
                        break;
                    case "gen":
                        upgradeType = UpgradeType.Generation;
                        break;
                    default:
                        //Debug.LogError("Wrong level up type");
                        return;
                }
                
                upgradeComponent.ApplyUpgrade(upgradeType);
            }
        }
        
        public int getLvl(string type)
        {
            if (upgradeComponent != null)
            {
                UpgradeType upgradeType;
                
                switch (type)
                {
                    case "power":
                        upgradeType = UpgradeType.Power;
                        break;
                    case "range":
                        upgradeType = UpgradeType.Range;
                        break;
                    case "speed":
                        upgradeType = UpgradeType.Speed;
                        break;
                    case "gen":
                        upgradeType = UpgradeType.Generation;
                        break;
                    default:
                        //Debug.LogError("Wrong level type");
                        return 1;
                }
                
                return upgradeComponent.GetUpgradeLevel(upgradeType);
            }
            
            return 1;
        }
        
        public float getUpgradeStat(string type)
        {
            if (upgradeComponent != null)
            {
                UpgradeType upgradeType;
                
                switch (type)
                {
                    case "power":
                        upgradeType = UpgradeType.Power;
                        break;
                    case "range":
                        upgradeType = UpgradeType.Range;
                        break;
                    case "speed":
                        upgradeType = UpgradeType.Speed;
                        break;
                    case "gen":
                        upgradeType = UpgradeType.Generation;
                        break;
                    default:
                        //Debug.LogError("Wrong upgrade stat type");
                        return 0f;
                }
                
                return upgradeComponent.GetUpgradeValue(upgradeType);
            }
            
            return 0f;
        }
        
        #endregion
        
        #region Editor Visualization
        
        /// <summary>
        /// Draw gizmos to visualize attack range
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (qubitData != null && qubitData.canAttack)
            {
                Gizmos.color = Color.yellow;
                
                // Calculate attack range with upgrades (if available at edit time)
                float range = qubitData.attackRange;
                if (Application.isPlaying && upgradeComponent != null)
                {
                    range += GetUpgradedStat(UpgradeType.Range);
                }
                
                Gizmos.DrawWireSphere(transform.position, range);
            }
        }
        
        #endregion
    }
}