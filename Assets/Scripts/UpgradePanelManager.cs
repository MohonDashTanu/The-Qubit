using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradePanelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private Transform upgradeSlotParent;
    [SerializeField] private GameObject upgradeSlotPrefab;
    [SerializeField] private Button refundButton;
    
    [Header("Upgrade Data")]
    [SerializeField] private List<UpgradeData> availableUpgrades;
    
    private List<UpgradeSlotUI> upgradeSlots = new List<UpgradeSlotUI>();
    
    private void Start()
    {
        SetupUpgradeSlots();
        
        if (refundButton)
        {
            refundButton.onClick.AddListener(RefundAll);
        }
    }
    
    private void SetupUpgradeSlots()
    {
        // Clear existing slots
        foreach (var slot in upgradeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        upgradeSlots.Clear();
        
        // Create new slots
        foreach (var upgradeData in availableUpgrades)
        {
            if (upgradeData == null) continue;
            
            GameObject slotObject = Instantiate(upgradeSlotPrefab, upgradeSlotParent);
            UpgradeSlotUI slotUI = slotObject.GetComponent<UpgradeSlotUI>();
            
            if (slotUI != null)
            {
                slotUI.Initialize(upgradeData, upgradeManager);
                upgradeSlots.Add(slotUI);
            }
        }
    }
    
    private void RefundAll()
    {
        upgradeManager.RefundAllUpgrades();
    }
}