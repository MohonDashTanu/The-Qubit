using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "WaveDatabase", menuName = "Wave/WaveDatabase")]
public class WaveDatabase : ScriptableObject
{
    [SerializeField][InspectorUtilities.DisplayWithoutEdit]private string _waveDataBaseName;
    [SerializeField]private DifficultyProfile _difficultyProfile;
    [SerializeField]private List<WaveSequenceEntry> _waveSequenceEntries = new List<WaveSequenceEntry>();

#if UNITY_EDITOR
    [SerializeField]
    [TextArea(15, 20)]
    private string _description;
#endif

    public string waveDataBaseName => _waveDataBaseName;

    public DifficultyProfile DifficultyProfile => _difficultyProfile;

    public List<WaveSequenceEntry> WaveSequenceEntries => _waveSequenceEntries;

    private void OnValidate()
    {
        this._waveDataBaseName = name;

        foreach (WaveSequenceEntry waveSequenceEntry in _waveSequenceEntries)
        {
            waveSequenceEntry.SequenceIndex = _waveSequenceEntries.IndexOf(waveSequenceEntry);
        }
    }

}
