using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameplayBuffContainer
{
    private Dictionary<Type, IGameplayBuff> buffs = new Dictionary<Type, IGameplayBuff>();
    public event System.Action<IGameplayBuff> OnGameplayBuffAdded;

    public void AddBuff(IGameplayBuff gameplayBuff)
    {
        Type buffType = gameplayBuff.GetType();
        if (!buffs.ContainsKey(buffType))
        {
            buffs.Add(buffType, gameplayBuff);
            Debug.Log($"Buff of type {buffType} added to the container.");
            OnGameplayBuffAdded.Invoke(gameplayBuff);
        }
        else
        {
            Debug.Log($"Buff of type {buffType} already exists in the container. Cannot add again.");
        }
    }
    public IGameplayBuff GetBuff<T>() where T : class, IGameplayBuff
    {
        buffs.TryGetValue(typeof(T), out var buff);
        return buff;
    }

    public void RemoveBuff(IGameplayBuff gameplayBuff)
    {
        Type buffType = gameplayBuff?.GetType();
        if (buffs.ContainsKey(buffType))
        {
            buffs.Remove(buffType);
        }
        else
        {
            Debug.LogWarning($"Buff of type {buffType} does not exist in the container. Cannot remove.");
        }
    }

    public void Clear()
    {
        buffs.Clear();
    }
}
