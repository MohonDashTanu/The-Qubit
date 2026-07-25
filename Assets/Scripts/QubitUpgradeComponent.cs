using UnityEngine;
using QuantumTD.Upgrades;

namespace QuantumTD.Qubits
{
    /// <summary>
    /// Component that handles qubit upgrades
    /// </summary>
    public class QubitUpgradeComponent : MonoBehaviour, IUpgradeable
    {
        [Header("References")]
        [SerializeField] private UpgradeDatabase upgradeDatabase;
        [SerializeField] private Qubit qubit;

        [Header("Upgrade Levels")]
        [SerializeField] private int powerLevel = 1;
        [SerializeField] private int rangeLevel = 1;
        [SerializeField] private int speedLevel = 1;
        [SerializeField] private int generationLevel = 1;
        
        [Header("Max Upgrade Levels")]
        [SerializeField] private int maxUpgradeLevel = 10;

        private ResourceManager resourceManager;

        private void Awake()
        {
            // Auto-fill references if not set
            if (qubit == null)
            {
                qubit = GetComponent<Qubit>();
            }
            
            if (upgradeDatabase == null)
            {
                upgradeDatabase = Resources.Load<UpgradeDatabase>("UpgradeDatabase");
                if (upgradeDatabase == null)
                {
                    //Debug.LogWarning("No UpgradeDatabase found in Resources folder!");
                }
            }
        }

        private void Start()
        {
            resourceManager = ResourceManager.Instance;
        }

        /// <summary>
        /// Apply an upgrade of specific type
        /// </summary>
        public bool ApplyUpgrade(UpgradeType upgradeType)
        {
            if (!CanUpgrade(upgradeType))
            {
                return false;
            }

            // Check if we have enough resources
            if (resourceManager != null)
            {
                int currentLevel = GetUpgradeLevel(upgradeType);
                int cost = upgradeDatabase.GetUpgradeCost(upgradeType, currentLevel);
                
                if (resourceManager.GetCurrentInformation() < cost)
                {
                    //Debug.Log($"Not enough information to upgrade! Need {cost}");
                    return false;
                }
                
                // Use resources
                resourceManager.UseInformation(cost);
            }

            // Apply the upgrade
            switch (upgradeType)
            {
                case UpgradeType.Power:
                    powerLevel++;
                    break;
                case UpgradeType.Range:
                    rangeLevel++;
                    break;
                case UpgradeType.Speed:
                    speedLevel++;
                    break;
                case UpgradeType.Generation:
                    generationLevel++;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Check if the specific upgrade can be applied
        /// </summary>
        public bool CanUpgrade(UpgradeType upgradeType)
        {
            int currentLevel = GetUpgradeLevel(upgradeType);
            int maxLevel = GetMaxUpgradeLevel(upgradeType);
            
            return currentLevel < maxLevel;
        }

        /// <summary>
        /// Get the current level of an upgrade type
        /// </summary>
        public int GetUpgradeLevel(UpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case UpgradeType.Power:
                    return powerLevel;
                case UpgradeType.Range:
                    return rangeLevel;
                case UpgradeType.Speed:
                    return speedLevel;
                case UpgradeType.Generation:
                    return generationLevel;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Get maximum level for an upgrade type
        /// </summary>
        public int GetMaxUpgradeLevel(UpgradeType upgradeType)
        {
            if (upgradeDatabase != null)
            {
                var data = upgradeDatabase.GetUpgradeData(upgradeType);
                if (data != null)
                {
                    return data.MaxLevel;
                }
            }
            
            return maxUpgradeLevel;
        }

        /// <summary>
        /// Get the current value of an upgrade
        /// </summary>
        public float GetUpgradeValue(UpgradeType upgradeType)
        {
            if (upgradeDatabase == null)
            {
                // Fallback to old hardcoded values if database not available
                switch (upgradeType)
                {
                    case UpgradeType.Power:
                        return 1f * (powerLevel - 1);
                    case UpgradeType.Range:
                        return 1f * (rangeLevel - 1);
                    case UpgradeType.Speed:
                        return 0.2f * (speedLevel - 1);
                    case UpgradeType.Generation:
                        return 0.5f * (generationLevel - 1);
                    default:
                        return 0f;
                }
            }

            int level = GetUpgradeLevel(upgradeType) - 1; // Subtract 1 as level 1 is the base level
            return upgradeDatabase.GetUpgradeValue(upgradeType, level);
        }

        /// <summary>
        /// Get the cost for the next upgrade
        /// </summary>
        public int GetUpgradeCost(UpgradeType upgradeType)
        {
            if (upgradeDatabase == null)
            {
                // Fallback to a basic cost calculation
                return 10 + (GetUpgradeLevel(upgradeType) * 5);
            }

            return upgradeDatabase.GetUpgradeCost(upgradeType, GetUpgradeLevel(upgradeType));
        }
    }
}