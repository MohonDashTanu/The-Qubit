using NUnit.Framework;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

[System.Serializable]
public class WaveSequenceEntry
{
    [SerializeField] private WaveData _waveData;

    private WaveRuntimeData _waveRuntimeData;

    private int _sequenceIndex;

    public WaveData WaveData => _waveData;

    public int SequenceIndex
    {
        get => _sequenceIndex;
        set => _sequenceIndex = value;
    }

    public WaveRuntimeData WaveRuntimeData
    {
        get => _waveRuntimeData;
        set => _waveRuntimeData = value;
    }

    public bool IsWaveSequenceEntryValid()
    {
        if (_waveData == null)
        {
            Debug.LogError($"WaveData is null for WaveSequenceEntry at index {_sequenceIndex}");
            return false;
        }
        if (_waveRuntimeData == null)
        {
            Debug.LogError($"WaveRuntimeData is null for WaveSequenceEntry at index {_sequenceIndex}");
            return false;
        }
        return true;
    }
}
