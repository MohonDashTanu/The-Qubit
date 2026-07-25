using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnPattern", menuName = "Spawning/SpawnPattern")]
public class SpawnPattern : ScriptableObject
{
    [SerializeField][InspectorUtilities.DisplayWithoutEdit]private string _SpawnPattenName;
    [SerializeField]private float _spawnInterval = 2.0f;

    //dont know what their design purpose are, need to confirm with Hermanto
    [SerializeField]private int _burstCount = 5;
    [SerializeField]private float _delayBetweenBurst = 20.0f;
    [SerializeField]private bool _staggered = false;

    public string SpawnPatternName => _SpawnPattenName;
    public float SpawnInterval => _spawnInterval;
    public int BurstCount => _burstCount;
    public float DelayBetweenBurst => _delayBetweenBurst;
    public bool Staggered => _staggered;

    private void OnValidate()
    {
        this._SpawnPattenName = name;
    }
}
