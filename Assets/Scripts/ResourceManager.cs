using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private int startingInformation = 50;
    
    [Header("UI References")]
    
    [SerializeField] private TMPro.TextMeshProUGUI informationText;
    
    [Header("Resource Generation")]
    [SerializeField] private float baseInformationRate = 1f;
    
    // Runtime variables
    private int currentInformation;
    private float informationModifier = 1f;
    
    // Singleton pattern
    public static ResourceManager Instance { get; private set; }
    
    // Events
    public delegate void ResourceChangedEvent(int amount);
    public event ResourceChangedEvent OnInformationChanged;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Initialize resources
        currentInformation = startingInformation;
    }
    
    private void Start()
    {
        // Initial UI update
        UpdateResourceDisplay();
        
        // Start passive resource generation
        InvokeRepeating("GenerateResources", 1f, 1f);
    }
    
    // Update the UI displays
    private void UpdateResourceDisplay()
    {
        if (informationText != null)
        {
            informationText.text = $"Information: {currentInformation}";
        }
    }
    
    // Generate passive resources
    private void GenerateResources()
    {
        // Generate information based on current rate and modifiers
        float informationGain = baseInformationRate * informationModifier;
        if (informationGain > 0)
        {
            AddInformation(Mathf.FloorToInt(informationGain));
        }
    }
    
    // Add information
    public void AddInformation(int amount)
    {
        if (amount <= 0)
            return;
            
        int oldValue = currentInformation;
        currentInformation += amount;
        
        // Update UI
        UpdateResourceDisplay();
        
        // Trigger event
        if (OnInformationChanged != null)
            OnInformationChanged(currentInformation - oldValue);
    }
    
    // Use information if we have enough
    public bool UseInformation(int amount)
    {
        if (amount <= 0 || currentInformation < amount)
            return false;
            
        int oldValue = currentInformation;
        currentInformation -= amount;
        
        // Update UI
        UpdateResourceDisplay();
        
        // Trigger event
        if (OnInformationChanged != null)
            OnInformationChanged(currentInformation - oldValue);
            
        return true;
    }
    
    // Set resource generation modifier
    public void SetInformationModifier(float modifier)
    {
        informationModifier = Mathf.Max(0, modifier);
    }
    
    // Get current resource amount
    public int GetCurrentInformation()
    {
        return currentInformation;
    }
}