using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QubitEntry
{
    public QubitData qubitData;
    public bool unlocked = false;
}

[CreateAssetMenu(fileName = "New Qubit Database", menuName = "Quantum/Qubit Database")]
public class QubitDatabase : ScriptableObject
{
    [SerializeField]
    private List<QubitEntry> qubits = new List<QubitEntry>();

    // Get all qubits in the database
    public List<QubitEntry> GetAllQubits()
    {
        return qubits;
    }
    
    // Get all unlocked qubits in the database
    public List<QubitData> GetAllUnlockedQubits()
    {
        List<QubitData> unlockedQubits = new List<QubitData>();
        foreach (QubitEntry entry in qubits)
        {
            if (entry.unlocked)
            {
                unlockedQubits.Add(entry.qubitData);
            }
        }
        return unlockedQubits;
    }

    // Get a qubit by its name
    public QubitData GetQubitByName(string name)
    {
        QubitEntry entry = qubits.Find(q => q.qubitData.qubitName == name);
        return entry?.qubitData;
    }

    // Get a qubit by its index
    public QubitData GetQubitByIndex(int index)
    {
        if (index >= 0 && index < qubits.Count)
        {
            return qubits[index].qubitData;
        }
        return null;
    }
    
    // Check if a qubit is unlocked
    public bool IsQubitUnlocked(string name)
    {
        QubitEntry entry = qubits.Find(q => q.qubitData.qubitName == name);
        return entry != null && entry.unlocked;
    }
    
    // Unlock a qubit by name
    public bool UnlockQubit(string name)
    {
        QubitEntry entry = qubits.Find(q => q.qubitData.qubitName == name);
        if (entry != null && !entry.unlocked)
        {
            entry.unlocked = true;
            return true;
        }
        return false;
    }
}