using System.Threading.Tasks;
using UnityEngine;

public interface IGameplayBuff
{
    bool OnApplyGameplayBuff();

    bool OnRemoveGameplayBuff();
}
