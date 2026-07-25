using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyEntry
{
    public EnemyData enemyData;
    public bool unlocked = true;
    public int waveUnlockLevel = 0;  // At which wave this enemy starts appearing
}

[CreateAssetMenu(fileName = "New Enemy Database", menuName = "Quantum/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    [SerializeField]
    private List<EnemyEntry> enemies = new List<EnemyEntry>();
    
    // Get all enemies in the database
    public List<EnemyEntry> GetAllEnemies()
    {
        return enemies;
    }
    
    // Get all unlocked enemies
    public List<EnemyData> GetAllUnlockedEnemies()
    {
        List<EnemyData> unlockedEnemies = new List<EnemyData>();
        foreach (EnemyEntry entry in enemies)
        {
            if (entry.unlocked)
            {
                unlockedEnemies.Add(entry.enemyData);
            }
        }
        return unlockedEnemies;
    }
    
    // Get enemies unlocked at specific wave level
    public List<EnemyData> GetEnemiesForWave(int waveLevel)
    {
        List<EnemyData> availableEnemies = new List<EnemyData>();
        
        // DEBUG: Log the total number of entries in the database
        Debug.Log($"EnemyDatabase contains {enemies.Count} total entries");
        
        foreach (EnemyEntry entry in enemies)
        {
            // DEBUG: Log each entry's details
            if (entry.enemyData != null)
            {
                Debug.Log($"Checking enemy: {entry.enemyData.enemyName}, Unlocked: {entry.unlocked}, Wave Level: {entry.waveUnlockLevel}");
            }
            else
            {
                Debug.Log("Found NULL enemy data entry in database");
                continue;
            }
            
            if (entry.unlocked && entry.waveUnlockLevel <= waveLevel)
            {
                availableEnemies.Add(entry.enemyData);
                Debug.Log($"Added {entry.enemyData.enemyName} to available enemies for wave {waveLevel}");
            }
            else
            {
                if (!entry.unlocked)
                    Debug.Log($"Skipped {entry.enemyData.enemyName} because it's not unlocked");
                if (entry.waveUnlockLevel > waveLevel)
                    Debug.Log($"Skipped {entry.enemyData.enemyName} because its wave level {entry.waveUnlockLevel} > current wave {waveLevel}");
            }
        }
        
        Debug.Log($"Returning {availableEnemies.Count} available enemies for wave {waveLevel}");
        return availableEnemies;
    }
    
    // Get an enemy by name
    public EnemyData GetEnemyByName(string name)
    {
        EnemyEntry entry = enemies.Find(e => e.enemyData.enemyName == name);
        return entry?.enemyData;
    }
    
    // Get an enemy by index
    public EnemyData GetEnemyByIndex(int index)
    {
        if (index >= 0 && index < enemies.Count)
        {
            return enemies[index].enemyData;
        }
        return null;
    }
    
    // Check if an enemy is unlocked
    public bool IsEnemyUnlocked(string name)
    {
        EnemyEntry entry = enemies.Find(e => e.enemyData.enemyName == name);
        return entry != null && entry.unlocked;
    }
    
    // Unlock an enemy by name
    public bool UnlockEnemy(string name)
    {
        EnemyEntry entry = enemies.Find(e => e.enemyData.enemyName == name);
        if (entry != null && !entry.unlocked)
        {
            entry.unlocked = true;
            return true;
        }
        return false;
    }
}