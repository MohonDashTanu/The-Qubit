using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages qubit selection, placement, and general qubit operations.
/// Controls qubit preview mode and ensures ranges are visible during placement.
/// Includes quantum collapse risk/reward system and entanglement visualization.
/// Players can now build anywhere within the grid bounds.
/// </summary>
public class QubitManager : MonoBehaviour
{
    [System.Serializable]
    public class Entanglement
    {
        public Qubit QubitSource;
        public Qubit QubitTarget;
        public Mesh lineMesh;

        public void InitMesh()
        {
            lineMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            lineMesh.uv = new Vector2[]
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1)
            };
            lineMesh.triangles = new int[]
            {
                0, 2, 1, 2, 3, 1
            };
            lineMesh.RecalculateNormals();
        }

        public Entanglement(Qubit source, Qubit target)
        {
            QubitSource = source;
            QubitTarget = target;
            lineMesh = new Mesh();
            InitMesh();
        }
    }

    [Header("Global Upgrade System")]
    [SerializeField] private GlobalUpgradeManager globalUpgradeManager;

    [Header("Qubit Limits & Collapse System")]
    [SerializeField] private int baseMaxQubits = 5;
    private int currentMaxQubits;
    private int currentQubitCount = 0;

    [Header("UI References")]
    [SerializeField] private Text qubitCountText;

    [Header("References")]
    [SerializeField] private QubitDatabase qubitDatabase;
    [SerializeField] private Transform qubitSlotParent;
    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject qubitSlotPrefab;
    [SerializeField] private GridManager gridManager;

    [Header("Preview Settings")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    [SerializeField] private LayerMask placementLayerMask = -1;
    [SerializeField] private bool disablePreviewEffects = true;
    [SerializeField] private bool disablePreviewRanges = false;
    [SerializeField] private float previewAlpha = 0.7f;

    [Header("Range Display")]
    [SerializeField] private GameObject rangeCirclePrefab;

    [Header("UI Feedback")]
    [SerializeField] private GameObject insufficientResourcesMessage;
    [SerializeField] private float messageDisplayTime = 2f;

    [Header("Entanglement Settings")]
    [SerializeField] private float entanglementRate = 0.1f;
    [SerializeField] private bool enableEntanglement = true;

    // Runtime variables
    private QubitData selectedQubit;
    private GameObject previewObject;
    private bool isPlacementMode = false;
    private Camera mainCamera;
    private float entanglementTimer = 0f;

    // Entanglement warning suppression
    private bool insufficientQubitsLogged = false;
    private float lastInsufficientQubitsLog = 0f;

    // Entanglement visualization
    private List<Entanglement> entanglements = new List<Entanglement>();

    // Track the active slots
    private List<GameObject> activeSlots = new List<GameObject>();

    // Track the active qubit game objects
    private List<GameObject> activeQubits = new List<GameObject>();

    // Coroutine for showing messages
    private Coroutine messageCoroutine;

    // Singleton instance for easy access
    public static QubitManager Instance { get; private set; }

    // Event for quantum collapse system
    public static event System.Action<int, int> OnQubitCountChanged; // (current, max)

    // Public property for entanglement access
    public List<Entanglement> Entanglements => entanglements;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;

        if (insufficientResourcesMessage != null)
        {
            insufficientResourcesMessage.SetActive(false);
        }
    }

    private void Start()
    {
        if (gridManager == null)
        {
            gridManager = Object.FindAnyObjectByType<GridManager>();
        }
        
        if (globalUpgradeManager == null)
        {
            globalUpgradeManager = GlobalUpgradeManager.Instance;
        }
        
        UpdateMaxQubits();
        
        if (globalUpgradeManager != null)
        {
            GlobalUpgradeManager.OnUpgradeChanged += OnUpgradeChanged;
        }

        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();

        if (qubitDatabase != null)
        {
            List<QubitData> unlockedQubits = qubitDatabase.GetAllUnlockedQubits();

            if (unlockedQubits.Count == 0)
            {
                UnlockDefaultQubits();
            }
        }

        InitializeQubitSlots();
    }

    private void UnlockDefaultQubits()
    {
        if (qubitDatabase != null)
        {
            List<QubitEntry> allQubits = qubitDatabase.GetAllQubits();
            if (allQubits.Count > 0)
            {
                if (allQubits[0].qubitData != null)
                {
                    string qubitName = allQubits[0].qubitData.qubitName;
                    qubitDatabase.UnlockQubit(qubitName);
                }

                if (allQubits.Count > 1 && allQubits[1].qubitData != null)
                {
                    string qubitName = allQubits[1].qubitData.qubitName;
                    qubitDatabase.UnlockQubit(qubitName);
                }
            }
        }
    }

    private void Update()
    {
        if (isPlacementMode && selectedQubit != null)
        {
            UpdatePreviewPosition();

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceQubit();
            }
        }

        if (enableEntanglement && activeQubits.Count >= 2)
        {
            UpdateEntanglement();
        }
    }

    #region Qubit Count and Collapse System

    private void UpdateMaxQubits()
    {
        if (globalUpgradeManager == null)
        {
            currentMaxQubits = baseMaxQubits;
        }
        else
        {
            int coreLevel = globalUpgradeManager.GetUpgradeLevel("core");
            currentMaxQubits = baseMaxQubits + (coreLevel * 2);
        }
        
        UpdateQubitCountUI();
        OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);
    }

    private void UpdateQubitCountUI()
    {
        if (qubitCountText != null)
        {
            if (currentQubitCount > currentMaxQubits)
            {
                qubitCountText.color = Color.red;
                qubitCountText.text = $"Qubits: {currentQubitCount}/{currentMaxQubits} (RISK!)";
            }
            else if (currentQubitCount == currentMaxQubits)
            {
                qubitCountText.color = Color.yellow;
                qubitCountText.text = $"Qubits: {currentQubitCount}/{currentMaxQubits} (MAX)";
            }
            else
            {
                qubitCountText.color = Color.white;
                qubitCountText.text = $"Qubits: {currentQubitCount}/{currentMaxQubits}";
            }
        }
    }

    private void OnUpgradeChanged(string upgradeType, int newLevel)
    {
        if (upgradeType == "core")
        {
            UpdateMaxQubits();
        }
    }

    public void OnQubitDamaged(GameObject qubit)
    {
        if (activeQubits.Contains(qubit))
        {
            RemoveEntanglementsForQubit(qubit);
        }
    }

    public void OnQubitDestroyed(GameObject qubit)
    {
        if (activeQubits.Contains(qubit))
        {
            activeQubits.Remove(qubit);
            currentQubitCount--;
            UpdateQubitCountUI();
            
            RemoveEntanglementsForQubit(qubit);
            OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);
        }
    }

    public void TriggerQuantumCollapse()
    {
        entanglements.Clear();
        
        if (gridManager != null)
        {
            gridManager.HandleQuantumCollapse();
        }
        
        GameObject[] allQubits = GameObject.FindGameObjectsWithTag("Qubit");
        
        foreach (GameObject qubit in allQubits)
        {
            if (qubit != null)
            {
                if (activeQubits.Contains(qubit))
                {
                    activeQubits.Remove(qubit);
                }
                
                Destroy(qubit);
            }
        }
        
        currentQubitCount = 0;
        UpdateQubitCountUI();
        OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);
    }

    public int GetCurrentQubitCount()
    {
        return currentQubitCount;
    }

    public int GetMaxQubitCount()
    {
        return currentMaxQubits;
    }

    public bool IsOverLimit()
    {
        return currentQubitCount > currentMaxQubits;
    }

    public float GetRiskLevel()
    {
        if (currentQubitCount < currentMaxQubits)
            return 0f;
            
        int riskQubits = (currentQubitCount - currentMaxQubits) + 1;
        
        float baseStability = 1f;
        float stabilityDecayPerExtraQubit = 0.1f;
        float minStability = 0.1f;
        
        float stability = baseStability - (riskQubits * stabilityDecayPerExtraQubit);
        stability = Mathf.Max(stability, minStability);
        
        return 1f - stability;
    }

    #endregion

    #region Entanglement System

    private (Qubit candidateAlpha, Qubit candidateBeta, bool isSuccess) SelectEntanglementCandidates(List<GameObject> qubits)
    {
        qubits.RemoveAll(q => q == null);

        if (qubits.Count < 2)
        {
            if (!insufficientQubitsLogged || Time.time - lastInsufficientQubitsLog > 10f)
            {
                insufficientQubitsLogged = true;
                lastInsufficientQubitsLog = Time.time;
            }
            return (null, null, false);
        }

        insufficientQubitsLogged = false;

        Qubit outputCandidateAlpha = null;
        Qubit outputCandidateBeta = null;

        for (int i = 0; i < qubits.Count; i++)
        {
            int candidateAlphaIndex = Random.Range(0, qubits.Count);
            int candidateBetaIndex = Random.Range(0, qubits.Count);

            while (candidateBetaIndex == candidateAlphaIndex)
            {
                candidateBetaIndex = Random.Range(0, qubits.Count);
            }
            
            outputCandidateAlpha = qubits[candidateAlphaIndex].GetComponent<Qubit>();
            outputCandidateBeta = qubits[candidateBetaIndex].GetComponent<Qubit>();

            if (outputCandidateAlpha == null || outputCandidateBeta == null)
            {
                continue;
            }

            var distance = Vector3.Distance(outputCandidateAlpha.GetGridPosition(), outputCandidateBeta.GetGridPosition());

            if (distance > 1)
            {
                continue;
            }
            
            return (outputCandidateAlpha, outputCandidateBeta, true);
        }

        return (null, null, false);
    }

    public List<Entanglement> GetAllEntanglements()
    {
        return new List<Entanglement>(entanglements);
    }

    public int GetEntanglementNetworkSize(Qubit qubit)
    {
        if (qubit == null) return 1;
        
        HashSet<Qubit> network = new HashSet<Qubit>();
        Queue<Qubit> qubitsToProcess = new Queue<Qubit>();
        
        qubitsToProcess.Enqueue(qubit);
        network.Add(qubit);
        
        while (qubitsToProcess.Count > 0)
        {
            Qubit currentQubit = qubitsToProcess.Dequeue();
            
            foreach (var entanglement in entanglements)
            {
                if (entanglement.QubitSource == null || entanglement.QubitTarget == null)
                    continue;
                    
                Qubit connectedQubit = null;
                
                if (entanglement.QubitSource == currentQubit)
                    connectedQubit = entanglement.QubitTarget;
                else if (entanglement.QubitTarget == currentQubit)
                    connectedQubit = entanglement.QubitSource;
                
                if (connectedQubit != null && !network.Contains(connectedQubit))
                {
                    network.Add(connectedQubit);
                    qubitsToProcess.Enqueue(connectedQubit);
                }
            }
        }
        
        return network.Count;
    }

    private List<Qubit> GetEntanglementNetworkForQubit(Qubit qubit)
    {
        List<Qubit> network = new List<Qubit>();
        HashSet<Qubit> processedQubits = new HashSet<Qubit>();
        Queue<Qubit> qubitsToProcess = new Queue<Qubit>();
        
        qubitsToProcess.Enqueue(qubit);
        processedQubits.Add(qubit);
        network.Add(qubit);
        
        while (qubitsToProcess.Count > 0)
        {
            Qubit currentQubit = qubitsToProcess.Dequeue();
            
            foreach (var entanglement in entanglements)
            {
                if (entanglement.QubitSource == null || entanglement.QubitTarget == null)
                    continue;
                    
                Qubit connectedQubit = null;
                
                if (entanglement.QubitSource == currentQubit)
                    connectedQubit = entanglement.QubitTarget;
                else if (entanglement.QubitTarget == currentQubit)
                    connectedQubit = entanglement.QubitSource;
                
                if (connectedQubit != null && !processedQubits.Contains(connectedQubit))
                {
                    network.Add(connectedQubit);
                    processedQubits.Add(connectedQubit);
                    qubitsToProcess.Enqueue(connectedQubit);
                }
            }
        }
        
        return network;
    }

    private void UpdateEntanglement()
    {
        entanglements.RemoveAll(e => e.QubitSource == null || e.QubitTarget == null ||
                                      e.QubitSource.gameObject == null || e.QubitTarget.gameObject == null);

        entanglementTimer += Time.deltaTime;
        float entanglementInterval = 1f / entanglementRate;

        if (entanglementTimer >= entanglementInterval)
        {
            var result = SelectEntanglementCandidates(activeQubits);
            if (result.isSuccess)
            {
                if (TryEntanglement(result.candidateAlpha, result.candidateBeta))
                {
                    entanglementTimer = 0f;
                }
                else
                {
                    entanglementTimer = entanglementInterval;
                }
            }
            else
            {
                entanglementTimer = entanglementInterval;
            }
        }
    }

    private bool AreEntangled(Qubit alpha, Qubit beta)
    {
        if (alpha == null || beta == null)
        {
            return false;
        }

        if (alpha.gameObject == beta.gameObject)
        {
            return false;
        }

        foreach (var entanglement in entanglements)
        {
            if (entanglement.QubitSource == null || entanglement.QubitTarget == null)
            {
                continue;
            }

            if ((entanglement.QubitSource.gameObject == alpha.gameObject && entanglement.QubitTarget.gameObject == beta.gameObject) ||
                (entanglement.QubitSource.gameObject == beta.gameObject && entanglement.QubitTarget.gameObject == alpha.gameObject))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryEntanglement(Qubit qubitAlpha, Qubit qubitBeta)
    {
        if (!AreEntangled(qubitAlpha, qubitBeta))
        {
            entanglements.Add(new Entanglement(qubitAlpha, qubitBeta));
            qubitAlpha.AddBuff(new EntanglementBuff(qubitAlpha));
            qubitBeta.AddBuff(new EntanglementBuff(qubitBeta));
            return true;
        }
        return false;
    }

    private void RemoveEntanglementsForQubit(GameObject qubit)
    {
        if (qubit == null) return;

        entanglements.RemoveAll(e => 
            (e.QubitSource != null && e.QubitSource.gameObject == qubit) ||
            (e.QubitTarget != null && e.QubitTarget.gameObject == qubit));
    }

    #endregion

    #region Slot Management

    private void InitializeQubitSlots()
    {
        if (qubitDatabase == null || qubitSlotParent == null || qubitSlotPrefab == null)
        {
            return;
        }

        ClearExistingSlots();

        List<QubitData> unlockedQubits = qubitDatabase.GetAllUnlockedQubits();

        foreach (QubitData qubit in unlockedQubits)
        {
            CreateQubitSlot(qubit);
        }
    }

    private GameObject CreateQubitSlot(QubitData qubitData)
    {
        if (qubitData == null)
        {
            return null;
        }

        GameObject newSlot = Instantiate(qubitSlotPrefab, qubitSlotParent);
        activeSlots.Add(newSlot);

        Transform iconTransform = newSlot.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = qubitData.qubitIcon;
                iconImage.gameObject.SetActive(true);
            }
        }

        Transform iconBackground = newSlot.transform.Find("Background");
        if (iconBackground != null)
        {
            GridLayoutGroup gridLayout = iconBackground.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                if (qubitData.qubitIcon != null)
                {
                    QubitSlot tempSlotComponent = newSlot.GetComponent<QubitSlot>();
                    float slotIconSize = tempSlotComponent != null ? tempSlotComponent.SlotIconSize : 66f;
                    gridLayout.cellSize = new Vector2(qubitData.qubitIcon.rect.width, qubitData.qubitIcon.rect.height).normalized * slotIconSize;
                }
                else
                {
                    gridLayout.cellSize = new Vector2(66, 66);
                }
            }
        }

        QubitSlot slotComponent = newSlot.GetComponent<QubitSlot>();
        if (slotComponent != null)
        {
            slotComponent.Initialize(this);
            slotComponent.AssignQubit(qubitData);
        }

        return newSlot;
    }

    private void ClearExistingSlots()
    {
        foreach (GameObject slot in activeSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }

        activeSlots.Clear();
    }

    #endregion

    #region Placement System

    public void SelectQubitForPlacement(QubitData qubitData)
    {
        if (qubitData == null)
        {
            return;
        }

        if (isPlacementMode && selectedQubit == qubitData)
        {
            CancelPlacement();
            return;
        }

        if (isPlacementMode)
        {
            CancelPlacement();
        }

        selectedQubit = qubitData;
        isPlacementMode = true;

        CreatePreviewObject();
    }

    private void CreatePreviewObject()
    {
        if (selectedQubit == null) return;

        if (selectedQubit.qubitPrefab != null)
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }

            if (previewPrefab != null)
            {
                previewObject = Instantiate(previewPrefab);
            }
            else
            {
                previewObject = Instantiate(selectedQubit.qubitPrefab);
            }

            ConfigurePreviewObject(previewObject);

            previewObject.SetActive(true);

            ZeroQubit zeroQubit = previewObject.GetComponent<ZeroQubit>();
            bool isZeroQubit = (zeroQubit != null);

            SpriteRangeDisplay previewRangeDisplay = previewObject.AddComponent<SpriteRangeDisplay>();

            if (rangeCirclePrefab != null)
            {
                System.Reflection.FieldInfo field = typeof(SpriteRangeDisplay).GetField("rangeSpritePrefab",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                if (field != null)
                {
                    field.SetValue(previewRangeDisplay, rangeCirclePrefab);
                }
            }

            previewRangeDisplay.SetPreviewMode(true);
            previewRangeDisplay.EnablePreviewDisplay(true);
            previewRangeDisplay.SetPlacementPreview();

            UpdatePreviewPosition();
        }
    }

    private GameObject ConfigurePreviewObject(GameObject previewObject)
    {
        if (previewObject == null)
        {
            return null;
        }

        Qubit[] qubits = previewObject.GetComponentsInChildren<Qubit>();

        foreach (Qubit qubit in qubits)
        {
            qubit.SetPreviewMode(true);
        }

        ZeroQubit[] zeroQubits = previewObject.GetComponentsInChildren<ZeroQubit>();

        foreach (ZeroQubit zeroQubit in zeroQubits)
        {
            zeroQubit.SetPreviewMode(true);
        }

        OneQubit[] oneQubits = previewObject.GetComponentsInChildren<OneQubit>();

        foreach (OneQubit oneQubit in oneQubits)
        {
            Qubit qubitComponent = oneQubit.GetComponent<Qubit>();
            if (qubitComponent != null)
            {
                qubitComponent.SetPreviewMode(true);
            }
        }

        string originalTag = previewObject.tag;
        previewObject.tag = "PreviewQubit";

        int previewLayer = LayerMask.NameToLayer("Preview");
        if (previewLayer != -1)
        {
            SetLayerRecursively(previewObject, previewLayer);
        }

        string originalName = previewObject.name;
        previewObject.name = originalName + "_PREVIEW";

        Collider2D[] colliders = previewObject.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Collider[] colliders3D = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders3D)
        {
            collider.enabled = false;
        }

        if (disablePreviewEffects)
        {
            ParticleSystem[] particles = previewObject.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }

            TrailRenderer[] trails = previewObject.GetComponentsInChildren<TrailRenderer>();
            foreach (TrailRenderer trail in trails)
            {
                trail.enabled = false;
            }

            LineRenderer[] lines = previewObject.GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer line in lines)
            {
                line.enabled = false;
            }
        }

        MonoBehaviour[] components = previewObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != null)
            {
                component.StopAllCoroutines();
            }
        }

        return previewObject;
    }

    private bool GetPreviewModeReflection(Qubit qubit)
    {
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (previewField != null)
        {
            return (bool)previewField.GetValue(qubit);
        }
        return false;
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void UpdatePreviewPosition()
    {
        if (previewObject == null || mainCamera == null)
        {
            return;
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        Vector3 targetPosition;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayerMask))
        {
            targetPosition = hit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            float distance;

            if (plane.Raycast(ray, out distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
            else
            {
                return;
            }
        }

        if (gridManager != null)
        {
            targetPosition = gridManager.GetSnappedPosition(targetPosition);
        }

        previewObject.transform.position = targetPosition;

        bool isValid = IsValidPlacement(targetPosition);

        UpdatePreviewColor(isValid);

        UpdatePreviewRanges(isValid);
    }

    private void UpdatePreviewColor(bool isValid)
    {
        if (previewObject == null) return;
        
        SpriteRenderer[] renderers = previewObject.GetComponentsInChildren<SpriteRenderer>();
        
        Color targetColor = isValid ? validPlacementColor : invalidPlacementColor;
        
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains("Range"))
                continue;
                
            Color newColor = targetColor;
            newColor.a = previewAlpha;
            renderer.color = newColor;
        }
        
        SpriteRangeDisplay rangeDisplay = previewObject.GetComponent<SpriteRangeDisplay>();
        if (rangeDisplay != null)
        {
            rangeDisplay.SetPlacementValidity(isValid);
        }
    }
    
    private void UpdatePreviewRanges(bool isValid)
    {
        // No range color changes during preview
    }

    private bool IsValidPlacement(Vector3 position)
    {
        // Get caller information using stack trace
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
        string caller = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
        string callerClass = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
        
        // RESTORED: Check QuantumCore building range first
        QuantumCore quantumCore = QuantumCore.Instance;
        // if (quantumCore != null)
        // {
        //     float distanceFromCore = Vector2.Distance(position, quantumCore.transform.position);
        //     float coreBuildingRange = quantumCore.GetBuildingRange();
            
        //     if (distanceFromCore > coreBuildingRange)
        //     {
        //         Debug.Log($"❌ IsValidPlacement FAILED at QuantumCore building range check | Position: {position} | Called by: {callerClass}.{caller}() | Distance: {distanceFromCore:F2} > Range: {coreBuildingRange:F2}");
        //         return false;
        //     }
        //     else
        //     {
        //         Debug.Log($"✅ IsValidPlacement PASSED QuantumCore building range check | Position: {position} | Distance: {distanceFromCore:F2} <= Range: {coreBuildingRange:F2}");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning($"⚠️ IsValidPlacement: QuantumCore not found - skipping building range check | Position: {position} | Called by: {callerClass}.{caller}()");
        // }
        
        // Check GridManager validation
        if (gridManager != null)
        {
            bool isGridValid = gridManager.IsValidPlacement(position);

            if (!isGridValid)
            {
                Debug.Log($"❌ IsValidPlacement FAILED at GridManager check | Position: {position} | Called by: {callerClass}.{caller}() | Reason: GridManager.IsValidPlacement returned false");
                return false;
            }
            else
            {
                Debug.Log($"✅ IsValidPlacement PASSED GridManager check | Position: {position} | Called by: {callerClass}.{caller}()");
            }
        }

        // Final validation passed
        Debug.Log($"✅ IsValidPlacement PASSED ALL CHECKS | Position: {position} | Called by: {callerClass}.{caller}()");
        return true;
    }

    public void ReplaceQubit(GameObject oldQubit, GameObject newQubit)
    {
        if (oldQubit == null || newQubit == null)
        {
            return;
        }

        int index = activeQubits.IndexOf(oldQubit);
        if (index >= 0)
        {
            activeQubits[index] = newQubit;
            UpdateQubitCountUI();
            OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);
            TransferEntanglements(oldQubit, newQubit);
        }
        else
        {
            activeQubits.Add(newQubit);
            currentQubitCount++;
            UpdateQubitCountUI();
            OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);
        }
    }

    private void TransferEntanglements(GameObject oldQubit, GameObject newQubit)
    {
        if (entanglements == null || entanglements.Count == 0) return;
        
        Qubit oldQubitComponent = oldQubit.GetComponent<Qubit>();
        Qubit newQubitComponent = newQubit.GetComponent<Qubit>();
        
        if (oldQubitComponent == null || newQubitComponent == null) return;
        
        for (int i = 0; i < entanglements.Count; i++)
        {
            var entanglement = entanglements[i];
            
            if (entanglement.QubitSource == oldQubitComponent)
            {
                entanglement.QubitSource = newQubitComponent;
            }
            else if (entanglement.QubitTarget == oldQubitComponent)
            {
                entanglement.QubitTarget = newQubitComponent;
            }
        }
    }

    private Bounds GetGridBounds()
    {
        if (grid == null)
        {
            return new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        }

        Renderer renderer = grid.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        Collider collider = grid.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }

        Vector3 size = new Vector3(10, 10, 1);
        return new Bounds(grid.transform.position, size);
    }

    private void TryPlaceQubit()
    {
        if (previewObject == null || selectedQubit == null) return;

        Vector3 placementPosition = previewObject.transform.position;

        if (gridManager != null)
        {
            placementPosition = gridManager.GetSnappedPosition(placementPosition);
        }

        if (IsValidPlacement(placementPosition))
        {
            ResourceManager resourceManager = ResourceManager.Instance;
            if (resourceManager != null && selectedQubit.qubitCost > 0)
            {
                int currentInfo = resourceManager.GetCurrentInformation();
                if (currentInfo < selectedQubit.qubitCost)
                {
                    ShowInsufficientResourcesMessage();
                    return;
                }

                resourceManager.UseInformation(selectedQubit.qubitCost);
            }

            GameObject placedQubit = Instantiate(selectedQubit.qubitPrefab, placementPosition, Quaternion.identity);

            ResetPlacedQubitFromPreview(placedQubit);

            Qubit qubitComponent = placedQubit.GetComponent<Qubit>();
            if (qubitComponent != null)
            {
                qubitComponent.SetGridPosition(placementPosition);
            }

            if (grid != null)
            {
                placedQubit.transform.SetParent(grid.transform);
            }

            placedQubit.tag = "Qubit";

            if (gridManager != null)
            {
                gridManager.OccupyCell(placementPosition, placedQubit);
            }

            activeQubits.Add(placedQubit);

            currentQubitCount++;
            UpdateQubitCountUI();
            
            OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);

            CancelPlacement();
        }
    }

    private void ResetPlacedQubitFromPreview(GameObject placedQubit)
    {
        Qubit[] qubits = placedQubit.GetComponentsInChildren<Qubit>();
        
        foreach (Qubit qubit in qubits)
        {
            qubit.SetPreviewMode(false);
        }
        
        ZeroQubit[] zeroQubits = placedQubit.GetComponentsInChildren<ZeroQubit>();
        OneQubit[] oneQubits = placedQubit.GetComponentsInChildren<OneQubit>();
        
        foreach (ZeroQubit zeroQubit in zeroQubits)
        {
            Qubit qubitComp = zeroQubit.GetComponent<Qubit>();
            if (qubitComp != null)
            {
                qubitComp.SetPreviewMode(false);
            }
        }
        
        foreach (OneQubit oneQubit in oneQubits)
        {
            Qubit qubitComp = oneQubit.GetComponent<Qubit>();
            if (qubitComp != null)
            {
                qubitComp.SetPreviewMode(false);
            }
        }
        
        placedQubit.tag = "Qubit";
        
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer == -1) defaultLayer = 0;
        SetLayerRecursively(placedQubit, defaultLayer);
        
        string cleanName = placedQubit.name.Replace("_PREVIEW", "").Replace("(Clone)", "");
        placedQubit.name = cleanName;
        
        Collider2D[] colliders = placedQubit.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
        
        Collider[] colliders3D = placedQubit.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders3D)
        {
            collider.enabled = true;
        }
        
        SpriteRenderer[] renderers = placedQubit.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains("Range"))
                continue;
                
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
        
        MonoBehaviour[] monoBehaviours = placedQubit.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour mb in monoBehaviours)
        {
            if (mb != null)
            {
                mb.enabled = true;
            }
        }
        
        SpriteRangeDisplay existingRangeDisplay = placedQubit.GetComponent<SpriteRangeDisplay>();
        if (existingRangeDisplay == null)
        {
            SpriteRangeDisplay rangeDisplay = placedQubit.AddComponent<SpriteRangeDisplay>();
            
            if (rangeCirclePrefab != null)
            {
                System.Reflection.FieldInfo field = typeof(SpriteRangeDisplay).GetField("rangeSpritePrefab",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                if (field != null)
                {
                    field.SetValue(rangeDisplay, rangeCirclePrefab);
                }
            }
            
            rangeDisplay.SetPreviewMode(false);
        }
        else
        {
            existingRangeDisplay.SetPreviewMode(false);
        }
        
        QubitSelectionManager selectionManager = QubitSelectionManager.Instance;
        if (selectionManager != null)
        {
            selectionManager.OnQubitPlaced(placedQubit);
        }
    }

    private void ShowInsufficientResourcesMessage()
    {
        if (insufficientResourcesMessage == null) return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(ShowMessageTemporarily(insufficientResourcesMessage, messageDisplayTime));
    }

    private IEnumerator ShowMessageTemporarily(GameObject message, float duration)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(duration);
        message.SetActive(false);
        messageCoroutine = null;
    }

    public void CancelPlacement()
    {
        isPlacementMode = false;
        selectedQubit = null;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    #endregion

    #region Manual Placement Methods

    public bool PlaceQubitAt(QubitData qubitData, Vector3 worldPosition)
    {
        if (qubitData == null || qubitData.qubitPrefab == null)
        {
            return false;
        }

        if (gridManager != null)
        {
            worldPosition = gridManager.GetSnappedPosition(worldPosition);
        }

        if (!IsValidPlacement(worldPosition))
        {
            return false;
        }

        ResourceManager resourceManager = ResourceManager.Instance;
        if (resourceManager != null && qubitData.qubitCost > 0)
        {
            int currentInfo = resourceManager.GetCurrentInformation();
            if (currentInfo < qubitData.qubitCost)
            {
                return false;
            }

            resourceManager.UseInformation(qubitData.qubitCost);
        }

        GameObject placedQubit = Instantiate(qubitData.qubitPrefab, worldPosition, Quaternion.identity);

        Qubit qubitComponent = placedQubit.GetComponent<Qubit>();
        if (qubitComponent != null)
        {
            qubitComponent.SetPreviewMode(false);
        }

        SpriteRangeDisplay rangeDisplay = placedQubit.AddComponent<SpriteRangeDisplay>();

        if (rangeCirclePrefab != null)
        {
            System.Reflection.FieldInfo field = typeof(SpriteRangeDisplay).GetField("rangeSpritePrefab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                field.SetValue(rangeDisplay, rangeCirclePrefab);
            }
        }

        if (grid != null)
        {
            placedQubit.transform.SetParent(grid.transform);
        }

        placedQubit.tag = "Qubit";

        if (gridManager != null)
        {
            gridManager.OccupyCell(worldPosition, placedQubit);
        }

        activeQubits.Add(placedQubit);
        currentQubitCount++;
        UpdateQubitCountUI();
        
        OnQubitCountChanged?.Invoke(currentQubitCount, currentMaxQubits);

        return true;
    }

    #endregion

    #region Public Utility Methods

    public void RefreshQubitSlots()
    {
        InitializeQubitSlots();
    }

    public QubitData GetQubitByName(string name)
    {
        if (qubitDatabase == null)
            return null;
            
        return qubitDatabase.GetQubitByName(name);
    }
    
    public List<QubitData> GetAllUnlockedQubits()
    {
        if (qubitDatabase == null)
            return new List<QubitData>();
            
        return qubitDatabase.GetAllUnlockedQubits();
    }
    
    public bool UnlockQubit(string name)
    {
        if (qubitDatabase == null)
            return false;
            
        bool result = qubitDatabase.UnlockQubit(name);
        
        if (result)
        {
            RefreshQubitSlots();
        }
        
        return result;
    }

    public QubitDatabase GetQubitDatabase()
    {
        return qubitDatabase;
    }

    public Transform GetQubitSlotParent()
    {
        return qubitSlotParent;
    }

    public GameObject GetGrid()
    {
        return grid;
    }

    public GameObject GetQubitSlotPrefab()
    {
        return qubitSlotPrefab;
    }

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public void SetGridManager(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (globalUpgradeManager != null)
        {
            GlobalUpgradeManager.OnUpgradeChanged -= OnUpgradeChanged;
        }
    }

    #endregion
}