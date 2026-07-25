using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class QubitSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;
    [SerializeField] private int slotIconSize = 100; // Default size for the icon, can be adjusted in the inspector

    private QubitData qubitData;
    private QubitManager qubitManager;

    public int SlotIconSize
    {
        get => slotIconSize;
    }

    // Public method that can be called directly from the UI Button's OnClick event
    public void TriggerQubitSelection()
    {
        //Debug.Log("TriggerQubitSelection called");
        
        if (qubitData == null)
        {
            //Debug.LogError("QubitSlot: qubitData is null!");
            return;
        }
        
        if (qubitManager == null)
        {
            //Debug.LogWarning("QubitSlot: qubitManager is null, trying to find instance");
            qubitManager = QubitManager.Instance;
            
            if (qubitManager == null)
            {
                //Debug.LogError("QubitSlot: Could not find QubitManager instance!");
                return;
            }
        }
        
        //Debug.Log($"QubitSlot: Triggering selection for {qubitData.qubitName}");
        qubitManager.SelectQubitForPlacement(qubitData);
    }
    
    private void Awake()
    {
        // Try to find the icon image and button if not assigned
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
            }
        }
        
        if (iconButton == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconButton = iconTransform.GetComponent<Button>();
                
                // Set up button click event if it's not already set
                if (iconButton != null)
                {
                    iconButton.onClick.RemoveAllListeners();
                    iconButton.onClick.AddListener(TriggerQubitSelection);
                    //Debug.Log("QubitSlot: Added button click listener");
                }
            }
        }
    }
    
    private void Start()
    {
        // Ensure we have a reference to the QubitManager
        if (qubitManager == null)
        {
            qubitManager = QubitManager.Instance;
            
            if (qubitManager == null)
            {
                //Debug.LogError("Could not find QubitManager instance!");
            }
        }
    }
    
    // Initialize the slot with the manager reference
    public void Initialize(QubitManager manager)
    {
        qubitManager = manager;
        UpdateVisuals();
        
        // Set up button click event if it's not already set
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(TriggerQubitSelection);
            //Debug.Log("QubitSlot: Added button click listener in Initialize");
        }
    }
    
    // Assign a qubit to this slot
    public void AssignQubit(QubitData data)
    {
        qubitData = data;
        UpdateVisuals();
        
        if (data != null)
        {
            //Debug.Log($"QubitSlot: Assigned qubit {data.qubitName}");
        }
    }
    
    // Update the visual elements
    private void UpdateVisuals()
    {
        if (qubitData != null)
        {
            // Set the icon
            if (iconImage != null && qubitData.qubitIcon != null)
            {
                iconImage.sprite = qubitData.qubitIcon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                // Use a default sprite if the qubit doesn't have an icon
                iconImage.gameObject.SetActive(true);
                //Debug.LogWarning($"QubitSlot: Qubit {qubitData.qubitName} has no icon!");
            }
        }
        else
        {
            // Hide elements if no qubit is assigned
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }
    }
    
    // Get the assigned qubit data
    public QubitData GetQubitData()
    {
        return qubitData;
    }
    
    // Handle click events for the entire slot area (backup click detection)
    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log("QubitSlot: OnPointerClick detected");
        TriggerQubitSelection();
    }
}