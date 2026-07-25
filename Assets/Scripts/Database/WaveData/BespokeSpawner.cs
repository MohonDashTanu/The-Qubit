using UnityEngine;

[CreateAssetMenu(fileName = "Spawner", menuName = "Spawning/Spawner")]
public class BespokeSpawner : ScriptableObject
{
    [SerializeField][InspectorUtilities.DisplayWithoutEdit]private string _spawnerName;
    [SerializeField]private bool _active = false;
    [SerializeField]private Transform _bespokeSpawnerTransform;

    public string SpawnName => _spawnerName;
    public Transform BespokeSpawnerTransform => _bespokeSpawnerTransform;

    public bool Active
    {
        get { return _active; }
        set { _active = value; }
    }
}
