using UnityEngine;
using System.Reflection;

// Inspector helper to debug and fix Qubit generation issues
public class QubitDebugger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Qubit targetQubit;
    
    [Header("Runtime Information")]
    [SerializeField] private string qubitType = "Unknown";
    [SerializeField] private string qubitDataName = "None";
    [SerializeField] private bool canGenerate = false;
    [SerializeField] private float informationPerSecond = 0f;
    [SerializeField] private float generationTimer = 0f;
    [SerializeField] private float generationInterval = 0f;
    [SerializeField] private bool hasResourceManager = false;
    
    [Header("Actions")]
    [SerializeField] private bool refreshInfo = false;
    [SerializeField] private bool fixGeneration = false;
    [SerializeField] private bool forceGenerateOnce = false;
    [SerializeField] private int forcedGenerationAmount = 10;
    
    // Cached reflection fields
    private FieldInfo generationTimerField;
    private FieldInfo resourceManagerField;
    private FieldInfo qubitDataField;
    
    private void Start()
    {
        if (targetQubit == null)
            targetQubit = GetComponent<Qubit>();
            
        if (targetQubit == null)
        {
            Debug.LogError("QubitDebugger: No target Qubit assigned or found on this GameObject!");
            enabled = false;
            return;
        }
        
        // Cache reflection fields
        qubitDataField = typeof(Qubit).GetField("qubitData", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
        resourceManagerField = typeof(Qubit).GetField("resourceManager", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
        generationTimerField = typeof(Qubit).GetField("generationTimer", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
        // Initial refresh
        RefreshInfo();
    }
    
    private void Update()
    {
        if (refreshInfo)
        {
            refreshInfo = false;
            RefreshInfo();
        }
        
        if (fixGeneration)
        {
            fixGeneration = false;
            FixGeneration();
        }
        
        if (forceGenerateOnce)
        {
            forceGenerateOnce = false;
            ForceGenerate();
        }
        
        // Update generation timer in real-time
        if (generationTimerField != null && targetQubit != null)
        {
            generationTimer = (float)generationTimerField.GetValue(targetQubit);
        }
    }
    
    private void RefreshInfo()
    {
        if (targetQubit == null)
            return;
            
        qubitType = targetQubit.GetType().Name;
        
        // Get QubitData
        if (qubitDataField != null)
        {
            QubitData data = (QubitData)qubitDataField.GetValue(targetQubit);
            if (data != null)
            {
                qubitDataName = data.name;
                canGenerate = data.canGenerate;
                informationPerSecond = data.informationPerSecond;
            }
            else
            {
                qubitDataName = "NULL";
            }
        }
        
        // Get ResourceManager
        if (resourceManagerField != null)
        {
            hasResourceManager = resourceManagerField.GetValue(targetQubit) != null;
        }
        
        // Get generation timer
        if (generationTimerField != null)
        {
            generationTimer = (float)generationTimerField.GetValue(targetQubit);
        }
        
        // Try to find generation interval
        FieldInfo generationIntervalField = typeof(Qubit).GetField("generationInterval", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
        if (generationIntervalField != null)
        {
            generationInterval = (float)generationIntervalField.GetValue(targetQubit);
        }
        else
        {
            generationInterval = 1.0f; // Default assumption
        }
        
        Debug.Log($"QubitDebugger: Refreshed info for {targetQubit.name} - " +
                 $"Type={qubitType}, Data={qubitDataName}, CanGenerate={canGenerate}, " +
                 $"InfoPerSec={informationPerSecond}, Timer={generationTimer:F2}, Interval={generationInterval:F2}, " +
                 $"HasRM={hasResourceManager}");
    }
    
    private void FixGeneration()
    {
        if (targetQubit == null)
            return;
            
        Debug.Log($"QubitDebugger: Attempting to fix generation for {targetQubit.name}");
        
        // Get QubitData
        if (qubitDataField != null)
        {
            QubitData data = (QubitData)qubitDataField.GetValue(targetQubit);
            if (data != null)
            {
                // Set canGenerate to true
                FieldInfo canGenerateField = typeof(QubitData).GetField("canGenerate");
                if (canGenerateField != null)
                {
                    canGenerateField.SetValue(data, true);
                    Debug.Log($"QubitDebugger: Set canGenerate to true for {data.name}");
                }
                
                // Set a reasonable informationPerSecond
                if (data.informationPerSecond <= 0)
                {
                    FieldInfo infoPerSecField = typeof(QubitData).GetField("informationPerSecond");
                    if (infoPerSecField != null)
                    {
                        infoPerSecField.SetValue(data, 10f);
                        Debug.Log($"QubitDebugger: Set informationPerSecond to 10 for {data.name}");
                    }
                }
            }
            else
            {
                Debug.LogError("QubitDebugger: QubitData is null!");
            }
        }
        
        // Reset generation timer
        if (generationTimerField != null)
        {
            generationTimerField.SetValue(targetQubit, 0f);
            Debug.Log("QubitDebugger: Reset generation timer to 0");
        }
        
        // Ensure ResourceManager is assigned
        if (resourceManagerField != null && resourceManagerField.GetValue(targetQubit) == null)
        {
            ResourceManager manager = ResourceManager.Instance;
            if (manager != null)
            {
                resourceManagerField.SetValue(targetQubit, manager);
                Debug.Log("QubitDebugger: Assigned ResourceManager");
            }
            else
            {
                Debug.LogError("QubitDebugger: Could not find ResourceManager.Instance!");
            }
        }
        
        // Refresh info after fixes
        RefreshInfo();
    }
    
    private void ForceGenerate()
    {
        if (targetQubit == null)
            return;
            
        Debug.Log($"QubitDebugger: Forcing resource generation for {targetQubit.name}");
        
        // Get ResourceManager
        ResourceManager manager = null;
        if (resourceManagerField != null)
        {
            manager = (ResourceManager)resourceManagerField.GetValue(targetQubit);
        }
        
        // If not found, try to get singleton
        if (manager == null)
        {
            manager = ResourceManager.Instance;
            
            // If found, assign it to the qubit
            if (manager != null && resourceManagerField != null)
            {
                resourceManagerField.SetValue(targetQubit, manager);
                Debug.Log("QubitDebugger: Assigned ResourceManager");
            }
        }
        
        // Generate resources directly
        if (manager != null)
        {
            manager.AddInformation(forcedGenerationAmount);
            Debug.Log($"QubitDebugger: Added {forcedGenerationAmount} information directly");
        }
        else
        {
            Debug.LogError("QubitDebugger: Could not find ResourceManager to add resources!");
        }
    }
}