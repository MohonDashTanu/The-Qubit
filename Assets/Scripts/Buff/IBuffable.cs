using System.Threading.Tasks;
using UnityEngine;

public interface IBuffable
{
    void AddBuff(IGameplayBuff buff);
    void RemoveBuff(IGameplayBuff buff);
    void ClearAllBuffs();
   
}
