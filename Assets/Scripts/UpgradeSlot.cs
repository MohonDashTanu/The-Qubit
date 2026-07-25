using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UpgradeSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;

    [Header("Upgrade Configuration")]
    private string upgradeType;
    private GlobalUpgradeManager upgradeManager;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color insufficientResourcesColor = Color.red;

    private ResourceManager resourceManager;

    private void Awake()
    {
        //Debug.Log($"🎮 UpgradeSlot Awake: {gameObject.name}");

        // Find button on this object or children
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                button = GetComponentInChildren<Button>();
            }
        }

        // Add click listener
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(TriggerUpgrade);
            //Debug.Log("✅ Button click listener added");
        }
        else
        {
            //Debug.LogWarning("⚠️ No Button component found!");
        }
    }

    private void Start()
    {
        resourceManager = ResourceManager.Instance;

        // Initial UI update
        UpdateUI();
    }

    public void Initialize(GlobalUpgradeManager manager, string type, string title, Sprite icon)
    {
        upgradeManager = manager;
        upgradeType = type;

        //Debug.Log($"🎮 UpgradeSlot.Initialize called for {type} with icon: {(icon != null ? icon.name : "NULL")}");

        // Find all TextMeshProUGUI components in children
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            //Debug.Log($"Found Text: {text.name} on {text.gameObject.name}");

            // Try to identify by name
            if (text.gameObject.name.ToLower().Contains("title"))
            {
                titleText = text;
                text.text = title;
            }
            else if (text.gameObject.name.ToLower().Contains("level"))
            {
                levelText = text;
            }
            else if (text.gameObject.name.ToLower().Contains("cost"))
            {
                costText = text;
            }
        }

        // Find Image components for icon - MORE FLEXIBLE APPROACH
        Image[] images = GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            //Debug.Log($"Found Image: {img.name} on {img.gameObject.name}");

            // Skip background images
            if (img.gameObject.name.ToLower().Contains("background"))
                continue;

            // Use the first non-background image as icon, or specifically look for "icon"
            if ((img.gameObject.name.ToLower().Contains("icon") || iconImage == null) && icon != null)
            {
                iconImage = img;
                iconImage.sprite = icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
                //Debug.Log($"✅ Set icon on {img.gameObject.name}");

                // If we found one with "icon" in the name, stop looking
                if (img.gameObject.name.ToLower().Contains("icon"))
                    break;
            }
        }

        // Update UI with current values
        UpdateUI();
    }

    public void TriggerUpgrade()
    {
        //Debug.Log($"🔥 TriggerUpgrade called for {upgradeType}");

        if (upgradeManager == null)
        {
            //Debug.LogError("❌ UpgradeManager is null!");
            return;
        }

        if (string.IsNullOrEmpty(upgradeType))
        {
            //Debug.LogError("❌ UpgradeType is not set!");
            return;
        }

        bool success = upgradeManager.TryUpgrade(upgradeType);
        //Debug.Log($"🎯 Upgrade {upgradeType} result: {(success ? "SUCCESS" : "FAILED")}");

        if (success)
        {
            UpdateUI();

            // Update all other slots
            UpgradeSlotManager slotManager = UpgradeSlotManager.Instance;
            if (slotManager != null)
            {
                slotManager.UpdateAllSlots();
            }
        }
    }

    public void UpdateUI()
    {
        //Debug.Log($"🔄 UpdateUI called for {upgradeType}");

        if (upgradeManager == null || string.IsNullOrEmpty(upgradeType))
        {
            //Debug.LogWarning("Cannot update UI - manager or type not set");
            return;
        }

        // Get current level
        int currentLevel = upgradeManager.GetUpgradeLevel(upgradeType);

        // Update level text - just show the number
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
        }

        // UPDATE ICON BASED ON LEVEL
        if (iconImage != null)
        {
            UpgradeSlotManager slotManager = UpgradeSlotManager.Instance;
            if (slotManager != null)
            {
                Sprite newIcon = slotManager.GetIconForLevel(upgradeType, currentLevel);
                if (newIcon != null)
                {
                    iconImage.sprite = newIcon;
                    //Debug.Log($"🎨 Updated icon for {upgradeType} level {currentLevel}");
                }
            }
        }

        // Get cost and resource info
        int cost = upgradeManager.GetUpgradeCost(upgradeType);
        bool hasEnoughResources = true;

        if (resourceManager != null)
        {
            int currentResources = resourceManager.GetCurrentInformation();
            hasEnoughResources = currentResources >= cost;
            //Debug.Log($"💰 Resources check - Cost: {cost}, Have: {currentResources}, Enough: {hasEnoughResources}");
        }

        // Update cost text
        if (costText != null)
        {
            costText.text = cost.ToString();
            costText.color = hasEnoughResources ? Color.white : insufficientResourcesColor;
        }

        // Update button interactability
        if (button != null)
        {
            button.interactable = hasEnoughResources;

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasEnoughResources ? defaultColor : insufficientResourcesColor;
            }

            //Debug.Log($"🔘 Button interactable: {button.interactable}");
        }

        //Debug.Log($"✅ UI Updated - Type: {upgradeType}, Level: {currentLevel}, Can afford: {hasEnoughResources}");
    }

    // IPointerClickHandler implementation
    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log($"🖱️ OnPointerClick detected for {upgradeType}");
        TriggerUpgrade();
    }

    private void OnMouseDown()
    {
        //Debug.Log($"🖱️ OnMouseDown detected for {upgradeType}");
        TriggerUpgrade();
    }
    private void OnEnable()
    {
        // Subscribe to resource changes if ResourceManager has events
        StartCoroutine(CheckResourcesPeriodically());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private System.Collections.IEnumerator CheckResourcesPeriodically()
    {
        int lastKnownResources = -1;
        
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check every half second
            
            if (resourceManager != null)
            {
                int currentResources = resourceManager.GetCurrentInformation();
                if (currentResources != lastKnownResources)
                {
                    lastKnownResources = currentResources;
                    UpdateUI(); // Update when resources change
                }
            }
        }
    }
}