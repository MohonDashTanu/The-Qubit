using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumTD.Upgrades
{
    /// <summary>
    /// Scriptable Object to store all upgrade data centrally
    /// </summary>
    [CreateAssetMenu(fileName = "New Upgrade Database", menuName = "Quantum/Upgrade Database")]
    public class UpgradeDatabase : ScriptableObject
    {
        [SerializeField] private List<UpgradeTypeData> upgradeTypes = new List<UpgradeTypeData>();
        
        private Dictionary<UpgradeType, UpgradeTypeData> upgradeLookup;

        private void OnEnable()
        {
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            upgradeLookup = new Dictionary<UpgradeType, UpgradeTypeData>();
            
            foreach (var upgradeType in upgradeTypes)
            {
                upgradeLookup[upgradeType.Type] = upgradeType;
            }
        }

        /// <summary>
        /// Get data for a specific upgrade type
        /// </summary>
        public UpgradeTypeData GetUpgradeData(UpgradeType type)
        {
            if (upgradeLookup == null)
            {
                InitializeLookup();
            }

            if (upgradeLookup.TryGetValue(type, out UpgradeTypeData data))
            {
                return data;
            }

            Debug.LogWarning($"No data found for upgrade type: {type}");
            return null;
        }

        /// <summary>
        /// Get all upgrade types
        /// </summary>
        public List<UpgradeTypeData> GetAllUpgradeTypes()
        {
            return upgradeTypes;
        }

        /// <summary>
        /// Get upgrade value at a specific level
        /// </summary>
        public float GetUpgradeValue(UpgradeType type, int level)
        {
            var data = GetUpgradeData(type);
            if (data == null) return 0f;

            return data.BaseValue + (data.ValuePerLevel * level);
        }

        /// <summary>
        /// Get upgrade cost at a specific level
        /// </summary>
        public int GetUpgradeCost(UpgradeType type, int level)
        {
            var data = GetUpgradeData(type);
            if (data == null) return 0;
            
            return data.BaseCost + Mathf.FloorToInt(data.CostIncrease * level);
        }
    }

    /// <summary>
    /// Data structure for each upgrade type
    /// </summary>
    [Serializable]
    public class UpgradeTypeData
    {
        public UpgradeType Type;
        public string DisplayName;
        public Sprite Icon;
        public string Description;
        
        [Header("Value Settings")]
        public float BaseValue;
        public float ValuePerLevel;
        public int MaxLevel = 10;
        
        [Header("Cost Settings")]
        public int BaseCost = 10;
        public float CostIncrease = 5f;

        [Header("UI Settings")]
        public Color ButtonColor = Color.white;
    }
}