using UnityEngine;

namespace QuantumTD.Upgrades
{
    /// <summary>
    /// Interface for any object that can be upgraded
    /// </summary>
    public interface IUpgradeable
    {
        /// <summary>
        /// Apply an upgrade of specific type
        /// </summary>
        /// <param name="upgradeType">Type of upgrade to apply</param>
        /// <returns>True if upgrade was successful</returns>
        bool ApplyUpgrade(UpgradeType upgradeType);

        /// <summary>
        /// Get the current level of a specific upgrade type
        /// </summary>
        /// <param name="upgradeType">Type of upgrade to check</param>
        /// <returns>Current level of the upgrade</returns>
        int GetUpgradeLevel(UpgradeType upgradeType);

        /// <summary>
        /// Get the maximum level for a specific upgrade type
        /// </summary>
        /// <param name="upgradeType">Type of upgrade to check</param>
        /// <returns>Maximum possible level for this upgrade</returns>
        int GetMaxUpgradeLevel(UpgradeType upgradeType);

        /// <summary>
        /// Check if a specific upgrade type can be applied
        /// </summary>
        /// <param name="upgradeType">Type of upgrade to check</param>
        /// <returns>True if upgrade can be applied</returns>
        bool CanUpgrade(UpgradeType upgradeType);
    }

    /// <summary>
    /// Enum defining all possible upgrade types
    /// </summary>
    public enum UpgradeType
    {
        Power,
        Range,
        Speed,
        Generation
    }
}