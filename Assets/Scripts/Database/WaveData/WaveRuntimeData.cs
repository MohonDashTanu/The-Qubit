using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class WaveRuntimeData
{
    private List<int> _waveSpawnEntrySpawnedCount = new List<int>();

    private int _waveSpawnEntryActiveIndex = 0;

    public List<int> WaveSpawnEntrySpawnedCount
    {
        get => _waveSpawnEntrySpawnedCount;
        set => _waveSpawnEntrySpawnedCount = value;
    }

    public int WaveSpawnEntryActiveIndex
    {
        get => _waveSpawnEntryActiveIndex;
        set => _waveSpawnEntryActiveIndex = value;
    }

    public void IncrementSpawnedCount(int index)
    {
        if (index < 0 || index >= _waveSpawnEntrySpawnedCount.Count)
        {
            Debug.LogError($"Index {index} is out of range for WaveSpawnEntrySpawnedCount");
            return;
        }
        _waveSpawnEntrySpawnedCount[index]++;
    }

    public void IncrementActiveIndex()
    {
        if (_waveSpawnEntryActiveIndex < _waveSpawnEntrySpawnedCount.Count - 1)
        {
            _waveSpawnEntryActiveIndex++;
        }
        else
        {
            Debug.LogWarning("Active index is already at the last entry");
        }
    }

    public WaveRuntimeData(int activeIndex, List<int> waveSpawnEntrySpawnedCount)
    {
        if (waveSpawnEntrySpawnedCount == null)
        {
            throw new System.ArgumentNullException(nameof(waveSpawnEntrySpawnedCount), "waveSpawnEntrySpawnedCount cannot be null");
        }

        // Constructor for WaveRuntimeData
        _waveSpawnEntryActiveIndex = activeIndex;
        _waveSpawnEntrySpawnedCount = waveSpawnEntrySpawnedCount;
    }
}
