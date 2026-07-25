using UnityEngine;
using System.Collections.Generic;

public class SpawnerManager : MonoBehaviour 
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject spawnerList; // Empty GameObject to hold spawners
    
    private void Start() 
    {
        CreateSpawners();
    }
    
    private void CreateSpawners() 
    {
        Vector2[] positions = GenerateSpawnerPositions();
        
        for (int i = 0; i < positions.Length; i++) 
        {
            GameObject spawner = new GameObject($"Spawner_{i:00}");
            spawner.transform.SetParent(spawnerList.transform);
            spawner.transform.position = new Vector3(positions[i].x, positions[i].y, 0);
            
            // Add EnemySpawner component if you have one
            // spawner.AddComponent<EnemySpawner>();
        }
    }
    
    private Vector2[] GenerateSpawnerPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        
        // Top Edge (y = 20)
        for (int x = 20; x >= -20; x -= 2)
        {
            positions.Add(new Vector2(x, 20));
        }
        
        // Right Edge (x = 20) - excluding corner already added
        for (int y = 18; y >= -20; y -= 2)
        {
            positions.Add(new Vector2(20, y));
        }
        
        // Bottom Edge (y = -20) - excluding corner already added
        for (int x = 18; x >= -20; x -= 2)
        {
            positions.Add(new Vector2(x, -20));
        }
        
        // Left Edge (x = -20) - excluding corners already added
        for (int y = -18; y <= 18; y += 2)
        {
            positions.Add(new Vector2(-20, y));
        }
        
        return positions.ToArray();
    }
}