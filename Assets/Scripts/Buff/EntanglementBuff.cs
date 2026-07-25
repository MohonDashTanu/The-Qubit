using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections;


public class EntanglementBuff : IGameplayBuff
{
    private Qubit _target;
    private float engtanglementBuffMultiplier = 1.1f; 
    public float EntanglementBuffMultiplier { get; private set; } = 1.1f; 

    public EntanglementBuff(Qubit target)
    {
        if (target == null)
        {
            throw new System.ArgumentNullException(nameof(target), "Target cannot be null.");
        }
        _target = target;
    }

    public bool OnApplyGameplayBuff()
    {
        throw new System.NotImplementedException();
    }

    public bool OnRemoveGameplayBuff()
    {
        throw new System.NotImplementedException();
    }
}
