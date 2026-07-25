// QuantumGateManager.cs - Updated to work with GateInventory system and CNOT gate
using UnityEngine;
using System.Collections.Generic;

public class QuantumGateManager : MonoBehaviour
{
    [Header("Gate Prefabs")]
    [SerializeField] private GameObject hadamardGatePrefab;
    [SerializeField] private GameObject pauliXGatePrefab;
    [SerializeField] private GameObject pauliYGatePrefab; // Pauli-Y Gate prefab
    [SerializeField] private GameObject cnotGatePrefab;   // CNOT Gate prefab
    // Add more gate prefabs as needed
    
    [Header("Placement Settings")]
    [SerializeField] private LayerMask placementLayerMask = -1;
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    
    [Header("References")]
    [SerializeField] private GateInventory gateInventory; // Reference to inventory
    
    // Runtime variables - now loaded from inventory
    private Dictionary<GateType, int> gateQuantities = new Dictionary<GateType, int>();
    private Dictionary<GateType, int> gateLevels = new Dictionary<GateType, int>();
    private Dictionary<GateType, bool> gateUnlocked = new Dictionary<GateType, bool>();
    private Dictionary<GateType, GateData> gateDataLookup = new Dictionary<GateType, GateData>();
    
    // Placement state
    private GateType selectedGateType = GateType.None;
    private bool isInPlacementMode = false;
    private GameObject placementPreview;
    private Camera mainCamera;
    
    // Events
    public delegate void GateQuantityChangedEvent(GateType gateType, int newQuantity);
    public event GateQuantityChangedEvent OnGateQuantityChanged;
    
    public delegate void GateUsedEvent(GateType gateType, Vector3 position, int level);
    public event GateUsedEvent OnGateUsed;
    
    // Singleton
    public static QuantumGateManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        mainCamera = Camera.main;
        
        // Find gate inventory if not assigned
        if (gateInventory == null)
        {
            gateInventory = Resources.Load<GateInventory>("GateInventory");
        }
    }
    
    private void Start()
    {
        // Load gates from inventory
        LoadGatesFromInventory();
        
        //Debug.Log("✅ QuantumGateManager initialized with inventory system");
    }
    
    private void LoadGatesFromInventory()
    {
        if (gateInventory == null)
        {
            //Debug.LogError("QuantumGateManager: No gate inventory assigned!");
            return;
        }
        
        // Clear existing data
        gateQuantities.Clear();
        gateLevels.Clear();
        gateUnlocked.Clear();
        gateDataLookup.Clear();
        
        // Load all gates from inventory
        List<GateInventoryEntry> allGates = gateInventory.GetAllGates();
        
        foreach (GateInventoryEntry entry in allGates)
        {
            if (entry.gateData != null)
            {
                GateType gateType = entry.gateData.gateType;
                
                // Load quantities (use run quantity, not owned quantity)
                gateQuantities[gateType] = entry.GetRunQuantity();
                gateLevels[gateType] = entry.currentLevel;
                gateUnlocked[gateType] = entry.unlocked;
                gateDataLookup[gateType] = entry.gateData;
                
                //Debug.Log($"📦 Loaded {entry.gateData.gateName}: " +
                         //$"RunQty={entry.GetRunQuantity()} (Owned={entry.ownedQuantity}, Max/Run={entry.maxPerRun}), " +
                         //$"Level={entry.currentLevel}, Unlocked={entry.unlocked}");
            }
        }
        
        //Debug.Log($"🚪 Loaded {gateDataLookup.Count} gate types from inventory");
    }
    
    private void Update()
    {
        // Handle gate placement
        if (isInPlacementMode)
        {
            UpdatePlacementPreview();
            
            // Cancel with right click or escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelGatePlacement();
            }
            
            // Place with left click
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceGate();
            }
        }
    }
    
    /// <summary>
    /// Try to select a gate for placement
    /// </summary>
    public bool SelectGateForPlacement(GateType gateType)
    {
        // Check if gate is unlocked
        if (!IsGateUnlocked(gateType))
        {
            //Debug.Log($"❌ Gate {gateType} is not unlocked yet!");
            return false;
        }
        
        // Check if we have any of this gate type
        if (GetGateQuantity(gateType) <= 0)
        {
            //Debug.Log($"❌ No {gateType} gates remaining!");
            return false;
        }
        
        // Cancel any existing placement
        if (isInPlacementMode)
        {
            CancelGatePlacement();
        }
        
        // Enter placement mode
        selectedGateType = gateType;
        isInPlacementMode = true;
        
        CreatePlacementPreview();
        
        //Debug.Log($"🎯 Selected {gateType} gate for placement (Level {GetGateLevel(gateType)})");
        return true;
    }

    private void CreatePlacementPreview()
    {
        // Create a preview object to show placement range
        placementPreview = new GameObject($"{selectedGateType}GatePreview");

        // Get the gate data to know which prefab to use
        GateData gateData = GetGateData(selectedGateType);
        GameObject gatePrefab = null;

        // Get prefab from data first, then fallback to hardcoded
        if (gateData != null && gateData.gatePrefab != null)
        {
            gatePrefab = gateData.gatePrefab;
        }
        else
        {
            gatePrefab = GetHardcodedGatePrefab(selectedGateType);
        }

        if (gatePrefab != null)
        {
            // Instantiate the actual gate prefab for preview
            placementPreview = Instantiate(gatePrefab);
            placementPreview.name = $"{selectedGateType}GatePreview";

            // Set preview mode based on gate type
            if (selectedGateType == GateType.Hadamard)
            {
                HadamardGate hadamardGate = placementPreview.GetComponent<HadamardGate>();
                if (hadamardGate != null)
                {
                    // Set the correct level for preview
                    int currentLevel = GetGateLevel(selectedGateType);
                    hadamardGate.SetPreviewLevel(currentLevel);
                    hadamardGate.SetPreviewMode(true);

                    //Debug.Log($"✅ Created Hadamard preview at level {currentLevel}");
                }
            }
            else if (selectedGateType == GateType.PauliX)
            {
                PauliXGate pauliXGate = placementPreview.GetComponent<PauliXGate>();
                if (pauliXGate != null)
                {
                    // Set the correct level for preview
                    int currentLevel = GetGateLevel(selectedGateType);
                    pauliXGate.SetPreviewLevel(currentLevel);
                    pauliXGate.SetPreviewMode(true);

                    //Debug.Log($"✅ Created Pauli-X preview at level {currentLevel}");
                }
            }
            else if (selectedGateType == GateType.PauliY)
            {
                PauliYGate pauliYGate = placementPreview.GetComponent<PauliYGate>();
                if (pauliYGate != null)
                {
                    // Set the correct level for preview
                    int currentLevel = GetGateLevel(selectedGateType);
                    pauliYGate.SetPreviewLevel(currentLevel);
                    pauliYGate.SetPreviewMode(true);

                    //Debug.Log($"✅ Created Pauli-Y preview at level {currentLevel}");
                }
            }
            else if (selectedGateType == GateType.CNOT)
            {
                CNOTGate cnotGate = placementPreview.GetComponent<CNOTGate>();
                if (cnotGate != null)
                {
                    // Set the correct level for preview
                    int currentLevel = GetGateLevel(selectedGateType);
                    cnotGate.SetPreviewLevel(currentLevel);
                    cnotGate.SetPreviewMode(true);

                    //Debug.Log($"✅ Created CNOT preview at level {currentLevel}");
                }
            }
            // Add more gate types here as you implement them

            // Disable any non-visual components for preview
            // Remove or disable audio, colliders, etc.
            AudioSource audioSource = placementPreview.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.enabled = false;
            }

            Collider2D[] colliders = placementPreview.GetComponentsInChildren<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }
        else
        {
            //Debug.LogError($"No prefab found for gate type: {selectedGateType}");
            // Fallback to simple circle if no prefab
            CreateSimplePreview();
        }
    }

    private void CreateSimplePreview()
    {
        // Fallback method - create a simple circle preview
        placementPreview = new GameObject($"{selectedGateType}GatePreview");
        
        // Add visual indicator
        SpriteRenderer previewRenderer = placementPreview.AddComponent<SpriteRenderer>();
        
        // Create preview texture
        Texture2D previewTexture = CreatePreviewTexture(256);
        Sprite previewSprite = Sprite.Create(previewTexture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100f);
        previewRenderer.sprite = previewSprite;
        previewRenderer.color = validPlacementColor;
        previewRenderer.sortingOrder = 10; // On top
        
        // Scale to show gate range
        float radius = GetGateRadius(selectedGateType);
        float scale = radius * 2f * 0.37f; // Same calibration factor
        placementPreview.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Texture2D CreatePreviewTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2, size / 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                // Create dashed circle for preview
                float angle = Mathf.Atan2(y - center.y, x - center.x);
                float dashPattern = Mathf.Sin(angle * 8f) > 0 ? 1f : 0.3f;

                if (distance < size / 2 - 2 && distance > size / 2 - 8)
                {
                    colors[y * size + x] = new Color(1f, 1f, 1f, 0.8f * dashPattern);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
    
    private void UpdatePlacementPreview()
    {
        if (placementPreview == null || mainCamera == null)
            return;
            
        // Get mouse world position
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        
        Vector3 targetPosition;
        
        // Raycast for placement position
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayerMask))
        {
            targetPosition = hit.point;
        }
        else
        {
            // Place on z=0 plane
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
            else
            {
                return;
            }
        }
        
        // Update preview position
        placementPreview.transform.position = targetPosition;
        
        // Check if placement is valid
        bool isValidPlacement = IsValidGatePlacement(targetPosition);
        
        // Update preview validity indicator based on gate type
        if (selectedGateType == GateType.Hadamard)
        {
            HadamardGate hadamardGate = placementPreview.GetComponent<HadamardGate>();
            if (hadamardGate != null)
            {
                hadamardGate.SetPlacementValidity(isValidPlacement);
            }
        }
        else if (selectedGateType == GateType.PauliX)
        {
            PauliXGate pauliXGate = placementPreview.GetComponent<PauliXGate>();
            if (pauliXGate != null)
            {
                pauliXGate.SetPlacementValidity(isValidPlacement);
            }
        }
        else if (selectedGateType == GateType.PauliY)
        {
            PauliYGate pauliYGate = placementPreview.GetComponent<PauliYGate>();
            if (pauliYGate != null)
            {
                pauliYGate.SetPlacementValidity(isValidPlacement);
            }
        }
        else if (selectedGateType == GateType.CNOT)
        {
            CNOTGate cnotGate = placementPreview.GetComponent<CNOTGate>();
            if (cnotGate != null)
            {
                cnotGate.SetPlacementValidity(isValidPlacement);
            }
        }
        
        // Fallback: Update preview color for simple previews
        SpriteRenderer previewRenderer = placementPreview.GetComponent<SpriteRenderer>();
        if (previewRenderer != null)
        {
            previewRenderer.color = isValidPlacement ? validPlacementColor : invalidPlacementColor;
        }
    }
    
    private bool IsValidGatePlacement(Vector3 position)
    {
        // Check if there are any qubits in range (gates need targets)
        float radius = GetGateRadius(selectedGateType);
        Collider2D[] qubitsInRange = Physics2D.OverlapCircleAll(position, radius);
        
        bool hasValidTargets = false;
        int qubitCount = 0;
        
        foreach (Collider2D collider in qubitsInRange)
        {
            if (collider.CompareTag("Qubit"))
            {
                hasValidTargets = true;
                qubitCount++;
            }
        }
        
        // Special validation for CNOT gate - needs at least 2 qubits
        if (selectedGateType == GateType.CNOT)
        {
            return qubitCount >= 2;
        }
        
        // Must have at least one qubit in range for other gates
        return hasValidTargets;
    }
    
    private void TryPlaceGate()
    {
        if (placementPreview == null)
            return;
            
        Vector3 placementPosition = placementPreview.transform.position;
        
        if (IsValidGatePlacement(placementPosition))
        {
            // Use the gate
            UseGate(selectedGateType, placementPosition);
            
            // Exit placement mode
            CancelGatePlacement();
        }
        else
        {
            if (selectedGateType == GateType.CNOT)
            {
                //Debug.Log("❌ Invalid CNOT placement - need at least 2 qubits in range!");
            }
            else
            {
                //Debug.Log("❌ Invalid gate placement - no qubits in range!");
            }
        }
    }
    
    private void UseGate(GateType gateType, Vector3 position)
    {
        // Decrease quantity
        if (gateQuantities.ContainsKey(gateType) && gateQuantities[gateType] > 0)
        {
            gateQuantities[gateType]--;
            
            // Fire event
            OnGateQuantityChanged?.Invoke(gateType, gateQuantities[gateType]);
            
            // Create the actual gate effect
            CreateGateEffect(gateType, position, GetGateLevel(gateType));
            
            // Fire gate used event
            OnGateUsed?.Invoke(gateType, position, GetGateLevel(gateType));
            
            //Debug.Log($"✨ Used {gateType} gate at {position}! Remaining: {gateQuantities[gateType]}");
        }
    }
    
    private void CreateGateEffect(GateType gateType, Vector3 position, int level)
    {
        GateData gateData = GetGateData(gateType);
        GameObject gatePrefab = null;
        
        // Get prefab from data first, then fallback to hardcoded
        if (gateData != null && gateData.gatePrefab != null)
        {
            gatePrefab = gateData.gatePrefab;
        }
        else
        {
            gatePrefab = GetHardcodedGatePrefab(gateType);
        }
        
        if (gatePrefab != null)
        {
            GameObject gateInstance = Instantiate(gatePrefab, position, Quaternion.identity);
            
            // Initialize the gate based on type
            switch (gateType)
            {
                case GateType.Hadamard:
                    HadamardGate hadamardGate = gateInstance.GetComponent<HadamardGate>();
                    if (hadamardGate != null)
                    {
                        hadamardGate.ActivateGate(position, level);
                    }
                    break;
                    
                case GateType.PauliX:
                    PauliXGate pauliXGate = gateInstance.GetComponent<PauliXGate>();
                    if (pauliXGate != null)
                    {
                        pauliXGate.ActivateGate(position, level);
                    }
                    break;
                    
                case GateType.PauliY:
                    PauliYGate pauliYGate = gateInstance.GetComponent<PauliYGate>();
                    if (pauliYGate != null)
                    {
                        pauliYGate.ActivateGate(position, level);
                    }
                    break;
                    
                case GateType.CNOT:
                    CNOTGate cnotGate = gateInstance.GetComponent<CNOTGate>();
                    if (cnotGate != null)
                    {
                        cnotGate.ActivateGate(position, level);
                    }
                    break;
                    
                // Add other gate types here as you implement them
                case GateType.PauliZ:
                    // TODO: Implement PauliZGate
                    //Debug.LogWarning("PauliZ gate not yet implemented!");
                    break;
                    
                case GateType.Swap:
                    // TODO: Implement SwapGate
                    //Debug.LogWarning("Swap gate not yet implemented!");
                    break;
                    
                case GateType.Toffoli:
                    // TODO: Implement ToffoliGate
                    //Debug.LogWarning("Toffoli gate not yet implemented!");
                    break;
                    
                default:
                    //Debug.LogError($"Unknown gate type: {gateType}");
                    break;
            }
        }
        else
        {
            //Debug.LogError($"No prefab found for gate type: {gateType}");
        }
    }
    
    private GameObject GetHardcodedGatePrefab(GateType gateType)
    {
        switch (gateType)
        {
            case GateType.Hadamard:
                return hadamardGatePrefab;
            case GateType.PauliX:
                return pauliXGatePrefab;
            case GateType.PauliY:
                return pauliYGatePrefab;
            case GateType.CNOT:
                return cnotGatePrefab;
            // Add more hardcoded prefabs here as needed
            default:
                return null;
        }
    }
    
    private float GetGateRadius(GateType gateType)
    {
        GateData gateData = GetGateData(gateType);
        if (gateData != null)
        {
            GateInventoryEntry entry = gateInventory?.GetGateEntry(gateType);
            return entry?.GetRadius() ?? gateData.baseRadius;
        }
        
        // Fallback values
        switch (gateType)
        {
            case GateType.Hadamard:
                return 3f + (1f * (GetGateLevel(gateType) - 1));
            case GateType.PauliX:
                return 4f + (1f * (GetGateLevel(gateType) - 1)); // Slightly larger than Hadamard
            case GateType.PauliY:
                return 4f + (1f * (GetGateLevel(gateType) - 1)); // Same as Pauli-X
            case GateType.CNOT:
                return 6f + (1.5f * (GetGateLevel(gateType) - 1)); // Larger radius for strategic entanglement
            default:
                return 3f;
        }
    }
    
    private void CancelGatePlacement()
    {
        isInPlacementMode = false;
        selectedGateType = GateType.None;
        
        if (placementPreview != null)
        {
            Destroy(placementPreview);
            placementPreview = null;
        }
        
        //Debug.Log("🔄 Gate placement cancelled");
    }
    
    // Public getters - now get from loaded data
    public int GetGateQuantity(GateType gateType)
    {
        return gateQuantities.ContainsKey(gateType) ? gateQuantities[gateType] : 0;
    }
    
    public int GetGateLevel(GateType gateType)
    {
        return gateLevels.ContainsKey(gateType) ? gateLevels[gateType] : 1;
    }
    
    public bool IsGateUnlocked(GateType gateType)
    {
        return gateUnlocked.ContainsKey(gateType) ? gateUnlocked[gateType] : false;
    }
    
    public GateData GetGateData(GateType gateType)
    {
        return gateDataLookup.ContainsKey(gateType) ? gateDataLookup[gateType] : null;
    }
    
    public bool IsInPlacementMode()
    {
        return isInPlacementMode;
    }
    
    public GateType GetSelectedGateType()
    {
        return selectedGateType;
    }
    
    // Public setters (used by GateSlotManager for loading)
    public void SetGateQuantity(GateType gateType, int quantity)
    {
        gateQuantities[gateType] = quantity;
        OnGateQuantityChanged?.Invoke(gateType, quantity);
    }
    
    public void SetGateLevel(GateType gateType, int level)
    {
        gateLevels[gateType] = level;
        //Debug.Log($"📈 {gateType} gate set to level {level}");
    }
    
    public void UnlockGate(GateType gateType)
    {
        gateUnlocked[gateType] = true;
        //Debug.Log($"🔓 Unlocked {gateType} gate");
    }
    
    // Refresh from inventory (useful when inventory changes)
    public void RefreshFromInventory()
    {
        LoadGatesFromInventory();
    }
    
    private void OnDestroy()
    {
        // Clean up placement preview if it exists
        if (placementPreview != null)
        {
            Destroy(placementPreview);
        }
    }
    
    // Debug methods
    [ContextMenu("Debug: Show Gate Status")]
    private void DebugShowGateStatus()
    {
        //Debug.Log("=== GATE MANAGER STATUS ===");
        foreach (var kvp in gateQuantities)
        {
            GateType gateType = kvp.Key;
            int quantity = kvp.Value;
            int level = GetGateLevel(gateType);
            bool unlocked = IsGateUnlocked(gateType);
            
            //Debug.Log($"{gateType}: Quantity={quantity}, Level={level}, Unlocked={unlocked}");
        }
    }
    
    [ContextMenu("Debug: Refresh from Inventory")]
    private void DebugRefreshFromInventory()
    {
        RefreshFromInventory();
    }
}