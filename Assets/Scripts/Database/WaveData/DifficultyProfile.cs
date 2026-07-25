using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Wave/DifficultyProfile")]
public class DifficultyProfile : ScriptableObject
{
    [InspectorUtilities.DisplayWithoutEdit]public string _difficultyName;
    [SerializeField]private float _healthMultiplier = 1.0f;
    [SerializeField]private float _speedMultiplier = 1.0f;
    [SerializeField]private float _rewardMultiplier = 1.0f;

    public float HealthMultiplier => _healthMultiplier;
    public float SpeedMultiplier => _speedMultiplier;
    public float RewardMultiplier => _rewardMultiplier;

    private void OnValidate()
    {
        this._difficultyName = name;
    }

    // We do not have ScalingCurve yet, what does this mean?
}
