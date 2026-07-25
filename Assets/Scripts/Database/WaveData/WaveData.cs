using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData")]
public class WaveData : ScriptableObject
{
    [SerializeField][InspectorUtilities.DisplayWithoutEdit]private string _waveDataName;
    [SerializeField]private int _initialDelay = 10;
    [SerializeField]private bool _autoStart = false;
    [SerializeField]private float _difficultyMultiplier = 1.0f;

    public string WaveDataName => _waveDataName;
    public int InitialDelay => _initialDelay;
    public bool AutoStart => _autoStart;
    public float DifficultyMultiplier => _difficultyMultiplier;

    [System.Serializable]
    public class WaveSpawnEntry
    {
        [SerializeField]private EnemyData _enemyData;
        [SerializeField]private int _count = 9;
        [SerializeField]private SpawnPattern _spawnPattern;
        [SerializeField][Tooltip("Aside from Automatically generated Spawners around the edge of the Grid, you can also select bespoke spawner explicitly placed in the scene.")]
        private List<BespokeSpawner> _bespokeSpawners;

        public EnemyData EnemyData => _enemyData;
        public int Count => _count;
        public SpawnPattern SpawnPattern => _spawnPattern;
        
        // Modified to handle empty lists more gracefully
        public List<BespokeSpawner> BespokeSpawners
        {
            get
            {
                if (_bespokeSpawners == null)
                {
                    _bespokeSpawners = new List<BespokeSpawner>();
                }

                // Only log warnings if the list contains entries that might be problematic
                if (_bespokeSpawners.Count > 0)
                {
                    bool hasNullEntries = false;
                    foreach (BespokeSpawner bespokeSpawner in _bespokeSpawners)
                    {
                        if (bespokeSpawner == null)
                        {
                            hasNullEntries = true;
                            break;
                        }
                    }
                    
                    if (hasNullEntries)
                    {
                        Debug.LogError($"One or more bespoke spawner is null for a WaveSpawnEntry");
                    }
                }
                
                return _bespokeSpawners;
            }
        }

        public bool IsWaveSpawnEntryValid()
        {
            if (this == null)
            {
                Debug.LogError($"WaveSpawnEntry is null");
                return false;
            }

            if (_enemyData == null)
            {
                Debug.LogError($"EnemyData is null for WaveSpawnEntry");
                return false;
            }
            
            if (_spawnPattern == null)
            {
                Debug.LogError($"SpawnPattern is null for WaveSpawnEntry");
                return false;
            }

            if (_count <= 0)
            {
                Debug.LogError($"Count is less than or equal to 0 for WaveSpawnEntry");
                return false;
            }
            
            return true;
        }
    }

    [SerializeField]
    private List<WaveSpawnEntry> _waveSpawnEntries = new List<WaveSpawnEntry>();

    public List<WaveSpawnEntry> WaveSpawnEntries
    {
        get => _waveSpawnEntries;
    }

    private void OnValidate()
    {
        this._waveDataName = name;
    }
}