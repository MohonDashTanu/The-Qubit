using UnityEngine;
using TMPro;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private UpgradeManager upgradeManager;
    
    private void Start()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnCurrencyChanged += UpdateDisplay;
            UpdateDisplay(upgradeManager.currentCurrency);
        }
    }
    
    private void UpdateDisplay(int amount)
    {
        if (currencyText) currencyText.text = amount.ToString();
    }
    
    private void OnDestroy()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnCurrencyChanged -= UpdateDisplay;
        }
    }
}