using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WaveData;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject quantumCore;
    [SerializeField] private EnemyDatabase enemyDatabase;
    
    [Header("Spawning")]
    [SerializeField] private Transform spawnPointsParent; // Parent object containing spawn points
    [SerializeField] private Transform[] spawnPoints; // Can be assigned directly or found via parent
    
    [Header("Wave Database")]
    [SerializeField] private WaveDatabase waveDatabase;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDetailedDebugLogs = true;
    [SerializeField] private EnemyData fallbackEnemyData; // Assign a simple enemy in the inspector
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    // Runtime variables
    private List<Transform> availableSpawnPoints = new List<Transform>();
    private int currentWave = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool waveInProgress = false;
    
    // FIXED: Add tracking for spawned enemies count
    private int totalEnemiesSpawnedThisWave = 0;
    private int totalEnemiesRequiredThisWave = 0;
    
    // Events
    public delegate void WaveEvent(int waveNumber);
    public event WaveEvent OnWaveStart;
    public event WaveEvent OnWaveComplete;

    private void Awake()
    {
        // Check if the wave database is assigned
        if (waveDatabase == null)
        {
            //Debug.LogError("WaveManager: Wave Database not assigned!");
            return;
        }
        // Check if the enemy database is assigned
        if (enemyDatabase == null)
        {
            //Debug.LogError("WaveManager: Enemy Database not assigned!");
            return;
        }
    }

    private void Start()
    {
        //Debug.Log("WaveManager starting...");
        
        // Validate core components
        if (quantumCore == null)
        {
            //Debug.LogError("WaveManager: quantumCore reference is missing! Finding one in scene...");
            quantumCore = GameObject.FindGameObjectWithTag("QuantumCore");
            
            if (quantumCore == null)
            {
                //Debug.LogError("WaveManager: No QuantumCore found in scene! Enemies will have no target.");
            }
            else
            {
                //Debug.Log($"WaveManager: Found QuantumCore: {quantumCore.name}");
            }
        }

        // Initialize spawn points
        InitializeSpawnPoints();
        
        // Initialize the wave sequence entries runtime data
        InitializeWaveSequenceEntries();
        
        // Start the first wave after a delay
        StartCoroutine(StartNewWave(waveDatabase.WaveSequenceEntries[currentWave]));
    }
    
    private void Update()
    {
        // Clean up the active enemies list (remove destroyed enemies)
        CleanupEnemyList();

        // FIXED: Wave ends when all enemies are SPAWNED, not when all are dead
        if (waveInProgress && HasWaveCompletedSpawning(waveDatabase.WaveSequenceEntries[currentWave]))
        {
            waveInProgress = false;
            
            // Notify listeners that the wave is complete
            if (OnWaveComplete != null)
            {
                OnWaveComplete(currentWave);
            }

            //Debug.Log($"Wave {currentWave + 1} completed! All {totalEnemiesSpawnedThisWave} enemies spawned. Next wave starting soon...");

            if (currentWave >= waveDatabase.WaveSequenceEntries.Count - 1)
            {
                //Debug.Log("WaveManager: All waves completed!");
                return;
            }

            // Increment wave counter
            currentWave++;
            
            // FIXED: Reset counters for new wave
            totalEnemiesSpawnedThisWave = 0;
            totalEnemiesRequiredThisWave = 0;
            
            // Start new wave
            StartCoroutine(StartNewWave(waveDatabase.WaveSequenceEntries[currentWave]));
        }
        
        // Update UI
        UpdateUI();
    }

    private void InitializeSpawnPoints()
    {
        availableSpawnPoints.Clear();
        
        // First check if spawn points are directly assigned
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform sp in spawnPoints)
            {
                if (sp != null)
                {
                    availableSpawnPoints.Add(sp);
                }
            }
            //Debug.Log($"WaveManager: Using {availableSpawnPoints.Count} directly assigned spawn points");
        }
        // Otherwise check if there's a parent container
        else if (spawnPointsParent != null)
        {
            // Get all child transforms
            foreach (Transform child in spawnPointsParent)
            {
                if (child != null)
                {
                    availableSpawnPoints.Add(child);
                }
            }
            //Debug.Log($"WaveManager: Found {availableSpawnPoints.Count} spawn points from parent object");
        }
        
        // Fallback: Create default spawn points if none found
        if (availableSpawnPoints.Count == 0)
        {
            //Debug.LogWarning("WaveManager: No spawn points found! Creating default spawn points.");
            
            Vector3[] defaultPositions = new Vector3[]
            {
                new Vector3(-10, 5, 0),
                new Vector3(-10, -5, 0),
                new Vector3(10, 5, 0),
                new Vector3(10, -5, 0)
            };
            
            GameObject spawnerParent = new GameObject("DefaultSpawners");
            
            foreach (Vector3 pos in defaultPositions)
            {
                GameObject spawner = new GameObject($"DefaultSpawner_{pos.x}_{pos.y}");
                spawner.transform.position = pos;
                spawner.transform.SetParent(spawnerParent.transform);
                availableSpawnPoints.Add(spawner.transform);
            }
            
            //Debug.LogWarning($"WaveManager: Created {defaultPositions.Length} default spawn points");
        }
        
        //Debug.Log($"WaveManager: Successfully initialized {availableSpawnPoints.Count} spawn points");
    }

    private void InitializeWaveSequenceEntries()
    {
        foreach (var waveSequenceEntry in waveDatabase.WaveSequenceEntries)
        {
            if (waveSequenceEntry.WaveRuntimeData == null)
            {
                waveSequenceEntry.WaveRuntimeData = new WaveRuntimeData(0, new List<int>(new int[waveSequenceEntry.WaveData.WaveSpawnEntries.Count]));
            }
        }
        
        //Debug.Log($"WaveManager: Initialized {waveDatabase.WaveSequenceEntries.Count} wave sequence entries");
    }

    public IEnumerator StartNewWave(WaveSequenceEntry waveSequenceEntry)
    {
        yield return new WaitForSeconds(waveSequenceEntry.WaveData.InitialDelay);

        if (waveInProgress)
        {
            //Debug.LogWarning("WaveManager: Wave already in progress, cannot start a new one.");
            yield break;
        }

        // Mark wave as in progress
        waveInProgress = true;

        // FIXED: Calculate total enemies for this wave and reset counters
        totalEnemiesRequiredThisWave = waveSequenceEntry.WaveData.WaveSpawnEntries.Sum(w => w.Count);
        totalEnemiesSpawnedThisWave = 0;

        // Notify listeners that the wave started
        if (OnWaveStart != null)
        {
            OnWaveStart(currentWave);
        }

        //Debug.Log($"Starting Wave {currentWave + 1} with {totalEnemiesRequiredThisWave} enemies to spawn.");

        // Start spawning enemies for this wave
        StartCoroutine(SpawnEnemiesRoutine(waveSequenceEntry));
    }
    
    private IEnumerator SpawnEnemiesRoutine(WaveSequenceEntry waveSequenceEntry)
    {
        while (!HasWaveCompletedSpawning(waveSequenceEntry))
        {
            // Move try-catch outside of the yield
            try
            {
                var waveSpawnEntries = waveSequenceEntry.WaveData.WaveSpawnEntries;
                int activeIndex = waveSequenceEntry.WaveRuntimeData.WaveSpawnEntryActiveIndex;
                
                // Safety check
                if (activeIndex >= waveSpawnEntries.Count)
                {
                    //Debug.LogError($"Active index {activeIndex} is out of range for wave entries count {waveSpawnEntries.Count}");
                    yield break;
                }
                
                // Check if entry is valid
                if (!waveSpawnEntries[activeIndex].IsWaveSpawnEntryValid())
                {
                    //Debug.LogError($"WaveManager: WaveSpawnEntry {activeIndex} is not valid!");
                    yield break;
                }
                
                // Get spawners for this entry
                List<Transform> spawnersToUse = GetSpawnersForWaveEntry(waveSpawnEntries[activeIndex]);
                
                if (spawnersToUse.Count == 0)
                {
                    //Debug.LogError("No spawners available for this wave - skipping spawn");
                }
                else
                {
                    // Spawn an enemy
                    SpawnEnemy(waveSequenceEntry.WaveData, waveSpawnEntries[activeIndex], spawnersToUse);
                    
                    // FIXED: Increment our overall spawned counter
                    totalEnemiesSpawnedThisWave++;
                    
                    // Increment spawned count
                    waveSequenceEntry.WaveRuntimeData.IncrementSpawnedCount(activeIndex);
                    
                    //Debug.Log($"Spawned enemy {totalEnemiesSpawnedThisWave}/{totalEnemiesRequiredThisWave} for Wave {currentWave + 1}");
                    
                    // Check if we've spawned all enemies for this entry
                    if (waveSequenceEntry.WaveRuntimeData.WaveSpawnEntrySpawnedCount[activeIndex] >= waveSpawnEntries[activeIndex].Count)
                    {
                        //Debug.Log($"Completed spawning all enemies for entry {activeIndex}");
                        
                        // Move to next entry
                        if (activeIndex < waveSpawnEntries.Count - 1)
                        {
                            waveSequenceEntry.WaveRuntimeData.IncrementActiveIndex();
                            //Debug.Log($"Moving to next wave spawn entry: {waveSequenceEntry.WaveRuntimeData.WaveSpawnEntryActiveIndex}");
                        }
                    }
                }
                
                // Calculate spawn interval (outside try block but before yield)
                float spawnInterval = waveSpawnEntries[activeIndex].SpawnPattern.SpawnInterval;
            }
            catch (System.Exception ex)
            {
                //Debug.LogError($"Error in SpawnEnemiesRoutine: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }
            
            // Yield outside the try-catch block
            float safeSpawnInterval = 1.0f; // Default interval in case of exception
            
            try
            {
                safeSpawnInterval = waveSequenceEntry.WaveData.WaveSpawnEntries[waveSequenceEntry.WaveRuntimeData.WaveSpawnEntryActiveIndex].SpawnPattern.SpawnInterval;
            }
            catch
            {
                //Debug.LogWarning("Couldn't get spawn interval, using default value");
            }
            
            yield return new WaitForSeconds(safeSpawnInterval);
        }
        
        //Debug.Log($"Completed spawning all {totalEnemiesSpawnedThisWave} enemies for Wave {currentWave + 1}");
    }
    
    private List<Transform> GetSpawnersForWaveEntry(WaveSpawnEntry waveSpawnEntry)
    {
        // Skip any warnings or processing for empty BespokeSpawners lists
        if (waveSpawnEntry.BespokeSpawners == null || waveSpawnEntry.BespokeSpawners.Count == 0)
        {
            return new List<Transform>(availableSpawnPoints);
        }
        
        List<Transform> spawnersToUse = new List<Transform>();
        bool foundValidBespokeSpawner = false;
        
        // Add all valid bespoke spawners
        foreach (var bespokeSpawner in waveSpawnEntry.BespokeSpawners)
        {
            if (bespokeSpawner != null && bespokeSpawner.BespokeSpawnerTransform != null)
            {
                spawnersToUse.Add(bespokeSpawner.BespokeSpawnerTransform);
                foundValidBespokeSpawner = true;
            }
        }
        
        // If we found any valid bespoke spawners, use only those
        if (foundValidBespokeSpawner)
        {
            if (showDetailedDebugLogs)
            {
                //Debug.Log($"Using {spawnersToUse.Count} bespoke spawners for this wave entry");
            }
            return spawnersToUse;
        }
        
        // If no valid bespoke spawners were found, use the general spawners
        return new List<Transform>(availableSpawnPoints);
    }

    private void SpawnEnemy(WaveData waveData, WaveSpawnEntry waveSpawnEntry, List<Transform> spawners)
    {
        if (spawners == null || spawners.Count == 0)
        {
            //Debug.LogError("WaveManager: No spawners available for enemy spawning!");
            return;
        }
       
        // Get a random spawner
        Transform selectedSpawner = spawners[Random.Range(0, spawners.Count)];
        Vector3 spawnerPosition = selectedSpawner.position;
        
        EnemyData enemyData = waveSpawnEntry.EnemyData;

        // Check if the enemy data is valid, use fallback if needed
        if (enemyData == null)
        {
            //Debug.LogWarning("WaveManager: Enemy data is null! Using fallback enemy if available.");
            enemyData = fallbackEnemyData;
            
            if (enemyData == null)
            {
                //Debug.LogError("WaveManager: No fallback enemy data available!");
                return;
            }
        }

        // Check if the enemy prefab is assigned
        if (enemyData.enemyPrefab == null)
        {
            //Debug.LogError($"WaveManager: Enemy prefab for {enemyData.enemyName} is not assigned!");
            return;
        }

        // Debug showing spawning enemy type
        if (showDetailedDebugLogs)
        {
            //Debug.Log($"WaveManager: Spawning Enemy {enemyData.enemyName} at {selectedSpawner.name}");
        }

        // Spawn the enemy at the selected spawn point position
        GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnerPosition, Quaternion.identity);
        
        // Initialize the enemy
        Enemy enemyComponent = enemy.GetComponent<Enemy>();

        // Check if the enemy component is present
        if (enemyComponent == null)
        {
            //Debug.LogError($"WaveManager: Enemy prefab does not have Enemy component!");
            Destroy(enemy); // Clean up to avoid further errors
            return;
        }
        else
        {
            enemyComponent.Initialize(enemyData, quantumCore.transform);
            
            // Apply difficulty modifiers from wave data
            var waveDifficultyModifier = waveData.DifficultyMultiplier;
            var waveSpawnEntryHealthModifier = waveDifficultyModifier * waveDatabase.DifficultyProfile.HealthMultiplier;
            var waveSpawnEntrySpeedModifier = waveDifficultyModifier * waveDatabase.DifficultyProfile.SpeedMultiplier;
            enemyComponent.ApplyDifficultyModifier(waveSpawnEntryHealthModifier, waveSpawnEntrySpeedModifier);
            
            if (showDetailedDebugLogs)
            {
                //Debug.Log($"WaveManager: Spawned {enemyData.enemyName} with Health: {enemyComponent.CurrentHealth}, Speed: {enemyComponent.CachedMoveSpeed}");
            }
        }

        // Add to active enemies list
        activeEnemies.Add(enemy);
    }

    private void CleanupEnemyList()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }
    
    // FIXED: Updated UI method
    private void UpdateUI()
    {
        if (waveText != null)
        {
            // FIXED: Display wave number starting from 1
            waveText.text = $"Wave: {currentWave + 1}";
        }
        
        if (enemyCountText != null)
        {
            // FIXED: Show spawned count vs total required, not alive vs total
            enemyCountText.text = $"Spawned: {totalEnemiesSpawnedThisWave}/{totalEnemiesRequiredThisWave} | Alive: {activeEnemies.Count}";
        }
    }
    
    // Get current wave number (1-based for display)
    public int GetCurrentWave()
    {
        return currentWave + 1; // FIXED: Return 1-based wave number
    }
    
    // Get number of active enemies
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    // FIXED: Get spawned enemies count
    public int GetSpawnedEnemyCount()
    {
        return totalEnemiesSpawnedThisWave;
    }
    
    // FIXED: Get total enemies required for current wave
    public int GetTotalEnemiesRequired()
    {
        return totalEnemiesRequiredThisWave;
    }

    public bool HasWaveCompletedSpawning(WaveSequenceEntry waveSequenceEntry)
    {
        // Check if the wave sequence entry is valid
        if (waveSequenceEntry == null || waveSequenceEntry.WaveRuntimeData == null)
        {
            //Debug.LogError("WaveManager: Wave sequence entry or its runtime data is null!");
            return false;
        }

        var activeWaveSpawnEntryIndex = waveSequenceEntry.WaveRuntimeData.WaveSpawnEntryActiveIndex;
        var waveSpawnEntries = waveSequenceEntry.WaveData.WaveSpawnEntries;
        var waveSpawnEntrySpawnedCount = waveSequenceEntry.WaveRuntimeData.WaveSpawnEntrySpawnedCount;

        // Better logging to understand wave state
        if (showDetailedDebugLogs)
        {
            //Debug.Log($"Wave {waveSequenceEntry.SequenceIndex + 1} status: " +
                    //$"Active index: {activeWaveSpawnEntryIndex}, " +
                    //$"Total entries: {waveSpawnEntries.Count}, " +
                    //$"Current spawned: {(activeWaveSpawnEntryIndex < waveSpawnEntrySpawnedCount.Count ? waveSpawnEntrySpawnedCount[activeWaveSpawnEntryIndex] : 0)}, " +
                    //$"Required: {(activeWaveSpawnEntryIndex < waveSpawnEntries.Count ? waveSpawnEntries[activeWaveSpawnEntryIndex].Count : 0)}");
        }

        // Check if we've reached the last spawn entry
        if (activeWaveSpawnEntryIndex != waveSpawnEntries.Count - 1)
        {
            if (showDetailedDebugLogs)
            {
                //Debug.Log($"Wave {waveSequenceEntry.SequenceIndex + 1} is not complete - still on entry {activeWaveSpawnEntryIndex} out of {waveSpawnEntries.Count - 1}");
            }
            return false;
        }

        // Check if the last entry is valid
        if (activeWaveSpawnEntryIndex < waveSpawnEntries.Count && 
            !waveSpawnEntries[activeWaveSpawnEntryIndex].IsWaveSpawnEntryValid())
        {
            //Debug.LogError($"WaveManager: WaveSpawnEntry {activeWaveSpawnEntryIndex} is not valid!");
            return false;
        }

        // Check if we've spawned all enemies in the last spawn entry
        if (activeWaveSpawnEntryIndex < waveSpawnEntries.Count && 
            activeWaveSpawnEntryIndex < waveSpawnEntrySpawnedCount.Count &&
            waveSpawnEntrySpawnedCount[activeWaveSpawnEntryIndex] < waveSpawnEntries[activeWaveSpawnEntryIndex].Count)
        {
            if (showDetailedDebugLogs)
            {
                int remaining = waveSpawnEntries[activeWaveSpawnEntryIndex].Count - waveSpawnEntrySpawnedCount[activeWaveSpawnEntryIndex];
                //Debug.Log($"Wave {waveSequenceEntry.SequenceIndex + 1} is not complete - {remaining} enemies remaining to spawn");
            }
            return false;
        }

        //Debug.Log($"Wave {waveSequenceEntry.SequenceIndex + 1} is complete - all enemies spawned");
        return true;
    }
    
    // Helper method to spawn a specific enemy type for testing
    public void SpawnTestEnemy(string enemyName)
    {
        if (enemyDatabase == null || availableSpawnPoints.Count == 0)
        {
            //Debug.LogError("Cannot spawn test enemy - missing references!");
            return;
        }
        
        EnemyData enemy = enemyDatabase.GetEnemyByName(enemyName);
        if (enemy == null || enemy.enemyPrefab == null)
        {
            //Debug.LogError($"Could not find enemy '{enemyName}' or it has no prefab!");
            return;
        }
        
        Transform spawner = availableSpawnPoints[0]; // Use first spawner
        GameObject spawnedEnemy = Instantiate(enemy.enemyPrefab, spawner.position, Quaternion.identity);
        
        Enemy enemyComponent = spawnedEnemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.Initialize(enemy, quantumCore.transform);
            activeEnemies.Add(spawnedEnemy);
            //Debug.Log($"Test enemy '{enemyName}' spawned at {spawner.position}");
        }
    }
    
    // Draw spawn points in the editor for easy visualization
    private void OnDrawGizmos()
    {
        // Draw spawn points if available in the editor
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform spawn in spawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawSphere(spawn.position, 0.5f);
                    Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 2f);
                }
            }
        }
        
        // Draw spawn points from parent if available
        if (spawnPointsParent != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform child in spawnPointsParent)
            {
                Gizmos.DrawSphere(child.position, 0.5f);
                Gizmos.DrawLine(child.position, child.position + child.forward * 2f);
            }
        }
    }
}