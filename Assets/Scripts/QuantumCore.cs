using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class QuantumCore : MonoBehaviour, IDamageable
{
    [Header("Core Properties")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private SpriteRenderer coreRenderer;

    [Header("Attack Properties")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float baseAttackRange = 8f;
    [SerializeField] private float attackRangePerLevel = 2f; // How much attack range increases per level
    [SerializeField] private float maxAttackRange = 20f; // Maximum attack range cap
    [SerializeField] private float baseAttackSpeed = 0.5f; // Attacks per second
    [SerializeField] private int baseAttackDamage = 20;

    [Header("Projectile Properties")]
    [SerializeField] private float baseProjectileSpeed = 1f;
    [SerializeField] private float projectileSpeedPerLevel = 0.3f; // REDUCED from 1f to 0.3f

    [Header("Resource Generation")]
    [SerializeField] private float baseInfoPerSecond = 2f;
    private float currentInfoPerSecond;
    private float infoGenerationTimer = 0f;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;

    [Header("Visual")]
    [SerializeField] private GameObject attackRangeIndicator; // Visual for attack range

    [Header("Main Menu Upgrades")]
    [SerializeField] private float upgradeAttackMultiplier = 1f;
    [SerializeField] private float upgradeHealthMultiplier = 1f;
    [SerializeField] private float upgradeRangeMultiplier = 1f;

    [SerializeField] private int currentHealth; // Serialized to view in inspector

    // Component references
    private Rigidbody2D rb;
    private CircleCollider2D coreCollider;
    private GlobalUpgradeManager upgradeManager;

    // Attack variables
    private float attackTimer = 0f;
    private float currentAttackRange;
    private float currentAttackSpeed;
    private int currentAttackDamage;
    private float currentProjectileSpeed;

    // FIXED: Add flag to prevent multiple subscriptions
    private bool subscribedToUpgrades = false;

    // Singleton for easy access
    public static QuantumCore Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Get components
        rb = GetComponent<Rigidbody2D>();
        coreCollider = GetComponent<CircleCollider2D>();

        // Configure rigidbody (kinematic since the core doesn't move)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.gravityScale = 0;
        }

        // Ensure the collider is not a trigger (for physical collisions)
        if (coreCollider != null && coreCollider.isTrigger)
        {
            coreCollider.isTrigger = false;
        }

        // FIXED: Try to find upgrade manager early and subscribe to events
        TryFindUpgradeManager();
    }

    private void Start()
    {
        // Ensure this object has the right tag
        gameObject.tag = "QuantumCore";

        // Initialize health
        currentHealth = maxHealth;

        // FIXED: Try again to find upgrade manager if not found in Awake
        if (upgradeManager == null)
        {
            TryFindUpgradeManager();
        }

        // Initialize main menu upgrades
        InitializeMainMenuUpgrades();

        // Initialize UI
        UpdateHealthUI();

        // Update stats based on upgrades
        UpdateCoreStats();

        // Create range indicators if needed
        CreateRangeIndicators();

        Debug.Log($"Quantum Core initialized with {currentHealth} health");
        Debug.Log($"Base stats - Attack Range: {baseAttackRange} (Max: {maxAttackRange}), Speed: {baseAttackSpeed}, Damage: {baseAttackDamage}, Info/s: {baseInfoPerSecond}, Projectile Speed: {baseProjectileSpeed}");
    }

    // FIXED: New method to find and subscribe to upgrade manager
    private void TryFindUpgradeManager()
    {
        if (upgradeManager == null)
        {
            upgradeManager = GlobalUpgradeManager.Instance;

            if (upgradeManager == null)
            {
                // Try to find it in the scene if Instance is null
                upgradeManager = FindObjectOfType<GlobalUpgradeManager>();
            }
        }

        // FIXED: Subscribe to upgrade events only once
        if (upgradeManager != null && !subscribedToUpgrades)
        {
            GlobalUpgradeManager.OnUpgradeChanged += OnUpgradeChanged;
            subscribedToUpgrades = true;
            Debug.Log("✅ QuantumCore subscribed to upgrade events");
        }
        else if (upgradeManager == null)
        {
            Debug.LogWarning("⚠️ GlobalUpgradeManager not found - QuantumCore will use base stats only");
        }
    }

    // FIXED: Update method to periodically check for upgrade manager
    private void Update()
    {
        // FIXED: Continuously try to find upgrade manager if not found
        if (upgradeManager == null && !subscribedToUpgrades)
        {
            TryFindUpgradeManager();
        }

        // Attack logic
        attackTimer += Time.deltaTime;
        if (attackTimer >= 1f / currentAttackSpeed)
        {
            TryAttack();
            attackTimer = 0f;
        }

        // Information generation
        infoGenerationTimer += Time.deltaTime;
        if (infoGenerationTimer >= 1f)
        {
            GenerateInformation();
            infoGenerationTimer = 0f;
        }
    }

    /// <summary>
    /// Initialize main menu upgrades when the core starts
    /// </summary>
    private void InitializeMainMenuUpgrades()
    {
        // Apply main menu upgrades when core starts
        UpgradeApplier upgradeApplier = UpgradeApplier.Instance;
        if (upgradeApplier != null)
        {
            upgradeApplier.ApplyUpgradesToCore(this);
            Debug.Log("✅ Applied main menu upgrades to QuantumCore on start");
        }
        else
        {
            Debug.LogWarning("⚠️ UpgradeApplier not found - QuantumCore will use base stats only");
        }
    }

    /// <summary>
    /// Apply main menu upgrade multipliers to the QuantumCore
    /// Called by UpgradeApplier when the game scene loads
    /// </summary>
    public void ApplyUpgradeMultipliers(float attackMult, float healthMult, float rangeMult)
    {
        upgradeAttackMultiplier = attackMult;
        upgradeHealthMultiplier = healthMult;
        upgradeRangeMultiplier = rangeMult;
        
        // Apply health upgrade (increase max health and current health)
        int originalMaxHealth = 100; // Assuming 100 is base health
        int newMaxHealth = Mathf.RoundToInt(originalMaxHealth * healthMult);
        int healthDifference = newMaxHealth - maxHealth;
        maxHealth = newMaxHealth;
        currentHealth += healthDifference; // Also increase current health
        
        Debug.Log($"QuantumCore main menu upgrades applied: Attack x{attackMult:F2}, Health x{healthMult:F2} ({maxHealth}), Range x{rangeMult:F2}");
        
        // Update stats that depend on upgrades
        UpdateCoreStats();
        UpdateHealthUI();
    }

    private void UpdateCoreStats()
    {
        Debug.Log("🔧 UpdateCoreStats called with main menu upgrades");

        if (upgradeManager == null)
        {
            // Use base stats with main menu upgrades only
            currentAttackRange = baseAttackRange * upgradeRangeMultiplier;
            currentAttackSpeed = baseAttackSpeed;
            currentAttackDamage = Mathf.RoundToInt(baseAttackDamage * upgradeAttackMultiplier);
            currentInfoPerSecond = baseInfoPerSecond;
            currentProjectileSpeed = baseProjectileSpeed;

            Debug.LogWarning("No GlobalUpgradeManager found - using base stats with main menu upgrades only");
            return;
        }

        // Get core upgrade level and multiplier from GlobalUpgradeManager (in-game upgrades)
        int coreLevel = upgradeManager.GetUpgradeLevel("core");
        float coreMultiplier = upgradeManager.GetUpgradeMultiplier("core");

        Debug.Log($"🎯 Core Level: {coreLevel}, Multiplier: {coreMultiplier:F2}");

        // COMBINED UPGRADE SCALING (Global + Main Menu):

        // Attack Range: Main menu range upgrade + Global upgrades
        float baseRangeWithMainMenu = baseAttackRange * upgradeRangeMultiplier;
        currentAttackRange = baseRangeWithMainMenu + (coreLevel * attackRangePerLevel);
        
        // Cap attack range if enabled
        if (maxAttackRange > 0)
        {
            currentAttackRange = Mathf.Min(currentAttackRange, maxAttackRange);
        }

        // Attack Speed: Global upgrades only (main menu doesn't affect speed directly)
        float attackSpeedBonus = (coreMultiplier - 1f) * 0.3f;
        currentAttackSpeed = baseAttackSpeed * (1f + attackSpeedBonus);

        // Damage: Main menu attack upgrade + Global upgrades
        float baseDamageWithMainMenu = baseAttackDamage * upgradeAttackMultiplier;
        currentAttackDamage = Mathf.RoundToInt(baseDamageWithMainMenu * coreMultiplier);

        // Resource generation: Global upgrades only
        currentInfoPerSecond = baseInfoPerSecond * coreMultiplier;

        // Projectile Speed: Global upgrades only
        float projectileSpeedFromLevels = coreLevel * projectileSpeedPerLevel;
        float projectileSpeedMultiplierBonus = (coreMultiplier - 1f) * 0.2f;
        currentProjectileSpeed = (baseProjectileSpeed + projectileSpeedFromLevels) * (1f + projectileSpeedMultiplierBonus);

        Debug.Log($"COMBINED Core Stats (Global + Main Menu):");
        Debug.Log($"Attack Range: {currentAttackRange} (base {baseAttackRange} x {upgradeRangeMultiplier:F2} main menu + {coreLevel * attackRangePerLevel} global)");
        Debug.Log($"Damage: {currentAttackDamage} (base {baseAttackDamage} x {upgradeAttackMultiplier:F2} main menu x {coreMultiplier:F2} global)");
        Debug.Log($"Health: {maxHealth} (base with {upgradeHealthMultiplier:F2}x main menu multiplier)");

        // Update range indicators
        UpdateRangeIndicators();
    }

    private void GenerateInformation()
    {
        ResourceManager resourceManager = ResourceManager.Instance;
        if (resourceManager != null && currentInfoPerSecond > 0)
        {
            int infoToAdd = Mathf.RoundToInt(currentInfoPerSecond);
            resourceManager.AddInformation(infoToAdd);

            // Only log occasionally to avoid spam
            if (Time.time % 5f < 1f) // Log every ~5 seconds
            {
                Debug.Log($"Core generated {infoToAdd} information (Rate: {currentInfoPerSecond:F1}/s)");
            }
        }
        else if (resourceManager == null)
        {
            Debug.LogWarning("QuantumCore: ResourceManager not found - cannot generate information!");
        }
    }

    private void TryAttack()
    {
        // Find the nearest enemy within ATTACK range
        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            Attack(nearestEnemy);
        }
    }

    private GameObject FindNearestEnemy()
    {
        // Use ATTACK range for combat
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentAttackRange);

        GameObject nearest = null;
        float minDistance = currentAttackRange + 1f;

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    nearest = collider.gameObject;
                    minDistance = distance;
                }
            }
        }

        return nearest;
    }

    private void Attack(GameObject enemy)
    {
        if (projectilePrefab != null)
        {
            // Calculate direction to enemy
            Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectileComponent = projectile.GetComponent<Projectile>();

            if (projectileComponent != null)
            {
                // Pass the balanced projectile speed
                projectileComponent.Initialize(directionToEnemy, currentAttackDamage, currentProjectileSpeed);
                Debug.Log($"Core firing projectile with damage: {currentAttackDamage}, speed: {currentProjectileSpeed:F2}");
            }
            else
            {
                Debug.LogWarning("Projectile prefab doesn't have Projectile component!");
            }
        }
        else
        {
            Debug.LogWarning("No projectile prefab assigned to QuantumCore!");
        }
    }

    private void CreateRangeIndicators()
    {
        // Create attack range indicator ONLY
        if (attackRangeIndicator == null)
        {
            attackRangeIndicator = new GameObject("CoreAttackRangeIndicator");
            attackRangeIndicator.transform.SetParent(transform);
            attackRangeIndicator.transform.localPosition = Vector3.zero;

            SpriteRenderer attackRenderer = attackRangeIndicator.AddComponent<SpriteRenderer>();

            // Create a circle texture for attack range
            Texture2D attackCircleTexture = CreateCircleTexture(256, 128);
            Sprite attackCircleSprite = Sprite.Create(attackCircleTexture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);

            attackRenderer.sprite = attackCircleSprite;
            attackRenderer.color = new Color(1f, 0.3f, 0.3f, 0.15f); // Red, very transparent for attack range
            attackRenderer.sortingOrder = -1; // Behind other elements

            Debug.Log("Created QuantumCore attack range indicator");
        }

        UpdateRangeIndicators();
    }

    private void UpdateRangeIndicators()
    {
        // Update attack range indicator ONLY
        if (attackRangeIndicator != null)
        {
            float attackScale = currentAttackRange * 2f * 0.37f; // Calibration factor
            attackRangeIndicator.transform.localScale = new Vector3(attackScale, attackScale, 1f);

            Debug.Log($"Updated core attack range indicator scale to {attackScale} (range: {currentAttackRange})");
        }
    }

    private Texture2D CreateCircleTexture(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2, size / 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance < radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else if (distance < radius + 1)
                {
                    float t = distance - radius;
                    colors[y * size + x] = new Color(1, 1, 1, 1 - t);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    // Handle direct collision with enemies
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"Quantum Core collided with enemy: {collision.gameObject.name}");

            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy == null)
            {
                // If enemy doesn't have Enemy component, take some default damage
                TakeDamage(10);
            }
            // If enemy has Enemy component, it should handle its own attack logic
        }
    }

    // Implementation of IDamageable interface
    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Visual feedback
        StartCoroutine(FlashDamage());

        // Update UI
        UpdateHealthUI();

        Debug.Log($"Quantum Core took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // Check for game over
        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    private IEnumerator FlashDamage()
    {
        if (coreRenderer != null)
        {
            Color originalColor = coreRenderer.color;
            coreRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            coreRenderer.color = originalColor;
        }
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    private void GameOver()
    {
        Debug.Log("🚨 GAME OVER - Quantum Core destroyed! 🚨");

        // Find and trigger the GameOverManager
        GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
            Debug.Log("✅ GameOverManager triggered successfully");
        }
        else
        {
            Debug.LogError("❌ GameOverManager not found! Make sure it exists in the scene.");
            
            // Fallback - pause game manually
            Time.timeScale = 0;
            
            // Hide the core
            if (coreRenderer != null)
            {
                coreRenderer.enabled = false;
            }
        }
    }

    // FIXED: Called when upgrades change - now with better logging
    public void OnUpgradeChanged(string upgradeType, int newLevel)
    {
        Debug.Log($"🎯 QuantumCore: Received upgrade change notification - {upgradeType} to level {newLevel}");

        // Always update stats when any upgrade changes (core upgrades affect everything)
        UpdateCoreStats();
    }

    // Get current attack range (used for combat)
    public float GetAttackRange()
    {
        return currentAttackRange;
    }

    // Get current attack damage
    public int GetAttackDamage()
    {
        return currentAttackDamage;
    }

    // Get current attack speed
    public float GetAttackSpeed()
    {
        return currentAttackSpeed;
    }

    // Get current info generation rate
    public float GetInfoGenerationRate()
    {
        return currentInfoPerSecond;
    }

    // Get current projectile speed
    public float GetProjectileSpeed()
    {
        return currentProjectileSpeed;
    }

    // Return current health percentage (0-1)
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    // Get current health
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Get max health
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    // Manual health setting (for testing/debugging)
    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    // Heal the core
    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateHealthUI();
    }

    // Force stats update (useful for testing)
    public void ForceStatsUpdate()
    {
        UpdateCoreStats();
    }

    // Toggle range indicator visibility
    public void SetAttackRangeIndicatorVisible(bool visible)
    {
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.SetActive(visible);
        }
    }

    // Toggle range indicators
    public void SetRangeIndicatorsVisible(bool visible)
    {
        SetAttackRangeIndicatorVisible(visible);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range in red
        Gizmos.color = Color.red;
        float gizmoAttackRange = currentAttackRange > 0 ? currentAttackRange : baseAttackRange;
        Gizmos.DrawWireSphere(transform.position, gizmoAttackRange);

        // Draw a smaller circle to show the core itself
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Show attack range cap indicator
        if (maxAttackRange > 0)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Faint red for max attack range
            Gizmos.DrawWireSphere(transform.position, maxAttackRange);
        }
    }

    // FIXED: Proper cleanup to prevent memory leaks
    private void OnDestroy()
    {
        if (subscribedToUpgrades)
        {
            GlobalUpgradeManager.OnUpgradeChanged -= OnUpgradeChanged;
            subscribedToUpgrades = false;
        }
    }

    // Debug methods for testing
    [ContextMenu("Test: Deal 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10);
    }

    [ContextMenu("Test: Heal to Full")]
    private void TestHeal()
    {
        SetHealth(maxHealth);
    }

    [ContextMenu("Test: Force Stats Update")]
    private void TestStatsUpdate()
    {
        ForceStatsUpdate();
    }

    [ContextMenu("Test: Toggle Attack Range Indicator")]
    private void TestToggleAttackRange()
    {
        if (attackRangeIndicator != null)
        {
            SetAttackRangeIndicatorVisible(!attackRangeIndicator.activeSelf);
        }
    }

    [ContextMenu("Debug: Show Attack Range Status")]
    private void DebugShowAttackRangeStatus()
    {
        Debug.Log($"🎯 Attack Range Status:");
        Debug.Log($"Current Attack Range: {currentAttackRange}");
        Debug.Log($"Base Attack Range: {baseAttackRange}");
        Debug.Log($"Attack Range Per Level: {attackRangePerLevel}");
        Debug.Log($"Max Attack Range: {maxAttackRange}");
        Debug.Log($"Attack Range Capped: {currentAttackRange >= maxAttackRange}");

        if (upgradeManager != null)
        {
            int coreLevel = upgradeManager.GetUpgradeLevel("core");
            float uncappedRange = baseAttackRange + (coreLevel * attackRangePerLevel);
            Debug.Log($"Core Level: {coreLevel}");
            Debug.Log($"Uncapped Attack Range: {uncappedRange}");
            Debug.Log($"Range Reduction Due to Cap: {Mathf.Max(0, uncappedRange - maxAttackRange)}");
        }
    }
}