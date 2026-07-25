using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private Button upgradeButton;
    
    [Header("Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Color maxLevelColor = Color.gray;
    
    private UpgradeData upgradeData;
    private UpgradeManager upgradeManager;
    private UpgradeProgress progress;
    
    public void Initialize(UpgradeData data, UpgradeManager manager)
    {
        upgradeData = data;
        upgradeManager = manager;
        
        // Initialize upgrade progress if it doesn't exist
        manager.InitializeUpgrade(data);
        progress = manager.GetUpgradeProgress(data);
        
        // Setup UI
        if (iconImage && data.icon) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.upgradeName;
        
        // Setup button
        if (upgradeButton) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        
        // Subscribe to events
        manager.OnCurrencyChanged += OnCurrencyChanged;
        manager.OnUpgradeChanged += OnUpgradeChanged;
        
        // Initial update
        UpdateUI();
    }
    
    private void OnUpgradeClicked()
    {
        upgradeManager.TryUpgrade(upgradeData);
    }
    
    private void OnCurrencyChanged(int newAmount)
    {
        UpdateUI();
    }
    
    private void OnUpgradeChanged(UpgradeProgress changedUpgrade)
    {
        if (changedUpgrade.upgradeData == upgradeData)
        {
            UpdateUI();
        }
    }
    
    private void UpdateUI()
    {
        if (progress == null) return;
        
        // Update level
        if (levelText) levelText.text = $"LVL {progress.currentLevel}";
        
        // Update cost and button state
        bool isMaxLevel = progress.currentLevel >= upgradeData.maxLevel;
        bool canAfford = progress.CanUpgrade(upgradeManager.currentCurrency);
        
        if (isMaxLevel)
        {
            if (costText) costText.text = "MAX";
            if (upgradeButton) upgradeButton.interactable = false;
            SetTextColor(maxLevelColor);
        }
        else
        {
            int cost = progress.GetNextLevelCost();
            if (costText) costText.text = cost.ToString();
            if (upgradeButton) upgradeButton.interactable = canAfford;
            SetTextColor(canAfford ? affordableColor : unaffordableColor);
        }
        
        // Update effect description
        if (effectText)
        {
            float currentEffect = progress.GetCurrentEffect();
            effectText.text = $"Effect: +{currentEffect:F1}";
        }
    }
    
    private void SetTextColor(Color color)
    {
        if (costText) costText.color = color;
        if (nameText) nameText.color = color;
    }
    
    private void OnDestroy()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnCurrencyChanged -= OnCurrencyChanged;
            upgradeManager.OnUpgradeChanged -= OnUpgradeChanged;
        }
    }
}