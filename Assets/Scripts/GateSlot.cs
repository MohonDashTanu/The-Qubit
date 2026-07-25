// GateSlot.cs - Configured for your exact hierarchy structure
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GateSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References - Auto-Assigned")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.yellow;
    
    // Runtime variables
    private GateData gateData;
    private QuantumGateManager gateManager;
    private int currentQuantity = 0;
    private int currentLevel = 1;
    
    private void Awake()
    {
        //Debug.Log($"🎮 GateSlot.Awake on {gameObject.name}");
        
        // Auto-find components based on your exact hierarchy:
        // Gate Slot (root)
        // ├── Background (Image)
        // ├── Icon (Image + Button)
        // ├── Quantity (TextMeshPro)
        // └── Level (TextMeshPro)
        
        // Find Background
        Transform backgroundTransform = transform.Find("Background");
        if (backgroundTransform != null)
        {
            backgroundImage = backgroundTransform.GetComponent<Image>();
            //Debug.Log("✅ Found Background Image");
        }
        
        // Find Icon (has both Image and Button)
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
            button = iconTransform.GetComponent<Button>();
            //Debug.Log($"✅ Found Icon - Image: {iconImage != null}, Button: {button != null}");
        }
        
        // Find Quantity text
        Transform quantityTransform = transform.Find("Quantity");
        if (quantityTransform != null)
        {
            quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();
            //Debug.Log($"✅ Found Quantity Text: {quantityText != null}");
        }
        
        // Find Level text
        Transform levelTransform = transform.Find("Level");
        if (levelTransform != null)
        {
            levelText = levelTransform.GetComponent<TextMeshProUGUI>();
            //Debug.Log($"✅ Found Level Text: {levelText != null}");
        }
        
        // Set up button click if found
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(TriggerGateSelection);
            //Debug.Log("✅ Added button click listener");
        }
        else
        {
            //Debug.LogWarning("⚠️ No Button component found on Icon!");
        }
        
        // Log what we found
        //Debug.Log($"Component Summary - Icon: {iconImage != null}, Button: {button != null}, " +
                 //$"Quantity: {quantityText != null}, Level: {levelText != null}, Background: {backgroundImage != null}");
    }
    
    private void Start()
    {
        // Find gate manager
        gateManager = QuantumGateManager.Instance;
        
        if (gateManager == null)
        {
            //Debug.LogError("GateSlot: Could not find QuantumGateManager!");
            return;
        }
        
        // Subscribe to gate events
        gateManager.OnGateQuantityChanged += OnGateQuantityChanged;
        
        // Initial update
        UpdateUI();
        
        //Debug.Log("✅ GateSlot initialization complete");
    }
    
    // Initialize the slot with gate data (called by GateSlotManager)
    public void Initialize(QuantumGateManager manager, GateData data, int quantity, int level)
    {
        gateManager = manager;
        gateData = data;
        currentQuantity = quantity;
        currentLevel = level;
        
        //Debug.Log($"🎮 GateSlot.Initialize called for {data.gateName} with quantity: {quantity}, level: {level}");
        
        // Set icon
        if (iconImage != null && data.gateIcon != null)
        {
            iconImage.sprite = data.gateIcon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            //Debug.Log($"✅ Set icon for {data.gateName}");
        }
        else
        {
            //Debug.LogWarning($"⚠️ Could not set icon - IconImage: {iconImage != null}, GateIcon: {data.gateIcon != null}");
        }
        
        // Update UI with current values
        UpdateUI();
    }
    
    // Main selection method
    public void TriggerGateSelection()
    {
        //Debug.Log($"🔥 TriggerGateSelection called for {(gateData != null ? gateData.gateName : "Unknown")}");
        
        if (gateManager == null)
        {
            //Debug.LogError("❌ GateManager is null!");
            return;
        }
        
        if (gateData == null)
        {
            //Debug.LogError("❌ GateData is null!");
            return;
        }
        
        // Check if we have any gates remaining
        if (currentQuantity <= 0)
        {
            //Debug.Log($"❌ No {gateData.gateName} gates remaining!");
            return;
        }
        
        // Try to select gate for placement
        bool success = gateManager.SelectGateForPlacement(gateData.gateType);
        //Debug.Log($"🎯 Gate selection {gateData.gateName} result: {(success ? "SUCCESS" : "FAILED")}");
        
        if (success)
        {
            UpdateUI(); // Update visual state
        }
    }
    
    private void OnGateQuantityChanged(GateType changedGateType, int newQuantity)
    {
        if (gateData != null && changedGateType == gateData.gateType)
        {
            currentQuantity = newQuantity;
            UpdateUI();
        }
    }
    
    public void UpdateUI()
    {
        //Debug.Log($"🔄 UpdateUI called for {(gateData != null ? gateData.gateName : "Unknown")}");
        
        if (gateManager == null || gateData == null)
        {
            //Debug.LogWarning("Cannot update UI - manager or data not set");
            return;
        }
        
        // Get current values from manager
        currentQuantity = gateManager.GetGateQuantity(gateData.gateType);
        currentLevel = gateManager.GetGateLevel(gateData.gateType);
        
        // Update quantity text
        if (quantityText != null)
        {
            quantityText.text = currentQuantity.ToString();
            
            // Color code based on quantity
            if (currentQuantity <= 0)
            {
                quantityText.color = Color.red;
            }
            else if (currentQuantity <= 1)
            {
                quantityText.color = Color.yellow;
            }
            else
            {
                quantityText.color = Color.white;
            }
        }
        else
        {
            //Debug.LogWarning("⚠️ Quantity text is null - cannot update");
        }
        
        // Update level text
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
        }
        else
        {
            //Debug.LogWarning("⚠️ Level text is null - cannot update");
        }
        
        // Update button interactability
        bool canUse = currentQuantity > 0;
        if (button != null)
        {
            button.interactable = canUse;
            
            // Update button image color if it has one
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canUse ? normalColor : disabledColor;
            }
            
           // Debug.Log($"🔘 Button interactable: {button.interactable}");
        }
        
        // Update icon color
        if (iconImage != null)
        {
            Color targetColor;
            
            if (currentQuantity <= 0)
            {
                targetColor = disabledColor;
            }
            else if (gateManager.GetSelectedGateType() == gateData.gateType && gateManager.IsInPlacementMode())
            {
                targetColor = selectedColor;
            }
            else
            {
                targetColor = normalColor;
            }
            
            iconImage.color = targetColor;
        }
        
        // Update background if needed
        if (backgroundImage != null)
        {
            // You can add background color changes here if desired
            backgroundImage.color = canUse ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
        
        //Debug.Log($"✅ UI Updated - Gate: {gateData.gateName}, Quantity: {currentQuantity}, Level: {currentLevel}, Can use: {canUse}");
    }
    
    // Update selection state periodically
    private void Update()
    {
        if (gateManager != null && gateData != null)
        {
            // Check if selection state changed
            bool isSelected = gateManager.GetSelectedGateType() == gateData.gateType && gateManager.IsInPlacementMode();
            
            // Update visual if needed
            if (iconImage != null)
            {
                bool wasSelected = iconImage.color == selectedColor;
                if (wasSelected != isSelected)
                {
                    UpdateUI();
                }
            }
        }
    }
    
    // Get the assigned gate data
    public GateData GetGateData()
    {
        return gateData;
    }
    
    // Get current quantity
    public int GetCurrentQuantity()
    {
        return currentQuantity;
    }
    
    // IPointerClickHandler implementation (backup click detection)
    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log($"🖱️ OnPointerClick detected for {(gateData != null ? gateData.gateName : "Unknown")}");
        TriggerGateSelection();
    }
    
    private void OnMouseDown()
    {
        //Debug.Log($"🖱️ OnMouseDown detected for {(gateData != null ? gateData.gateName : "Unknown")}");
        TriggerGateSelection();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (gateManager != null)
        {
            gateManager.OnGateQuantityChanged -= OnGateQuantityChanged;
        }
    }
}