using UnityEngine;

[CreateAssetMenu(fileName = "Spawner", menuName = "Scriptable Objects/Spawner")]
public class Spawner : ScriptableObject
{
    [SerializeField]
    private Vector3 _position;
    [SerializeField]
    private bool _active;

    public Vector3 Position
    {
        get { return _position; }
        set { _position = value; }
    }

    public bool Active
    {
        get { return _active; }
        set { _active = value; }
    }
}
