using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager for quantum circuits and quantum operations.
/// Handles the creation, manipulation, and visualization of quantum circuits.
/// </summary>
public class QuantumCircuitManager : MonoBehaviour
{
    [Header("Circuit Settings")]
    [SerializeField] private int maxQubits = 8;
    [SerializeField] private float qubitSpacing = 1.5f;
    [SerializeField] private float gateSpacing = 1.5f;
    
    [Header("Visualization")]
    [SerializeField] private GameObject qubitLinePrefab;
    [SerializeField] private GameObject gateConnectionPrefab;
    [SerializeField] private bool showProbabilityAmplitudes = true;
    
    [Header("Gate Prefabs")]
    [SerializeField] private GameObject hadamardGatePrefab;
    [SerializeField] private GameObject pauliXGatePrefab;
    [SerializeField] private GameObject pauliYGatePrefab;
    [SerializeField] private GameObject pauliZGatePrefab;
    [SerializeField] private GameObject cnotGatePrefab;
    [SerializeField] private GameObject swapGatePrefab;
    
    // Runtime data
    private List<GameObject> qubitLines = new List<GameObject>();
    private List<GameObject> gates = new List<GameObject>();
    private Dictionary<int, List<GameObject>> gatesByQubit = new Dictionary<int, List<GameObject>>();
    
    // Qubit state information (for educational visualization)
    private List<Complex> qubitStates = new List<Complex>();
    
    // Singleton pattern
    public static QuantumCircuitManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        InitializeCircuit();
    }
    
    private void InitializeCircuit()
    {
        // Create qubit lines
        for (int i = 0; i < maxQubits; i++)
        {
            CreateQubitLine(i);
            
            // Initialize qubit states to |0⟩
            qubitStates.Add(new Complex(1, 0));
            
            // Initialize gate lists for each qubit
            gatesByQubit[i] = new List<GameObject>();
        }
    }
    
    private void CreateQubitLine(int qubitIndex)
    {
        if (qubitLinePrefab == null)
        {
            //Debug.LogWarning("QuantumCircuitManager: qubitLinePrefab is not assigned!");
            return;
        }
        
        // Calculate position
        Vector3 position = new Vector3(0, -qubitIndex * qubitSpacing, 0);
        
        // Create the qubit line
        GameObject qubitLine = Instantiate(qubitLinePrefab, position, Quaternion.identity, transform);
        qubitLine.name = $"Qubit_{qubitIndex}";
        
        // Add to the list
        qubitLines.Add(qubitLine);
    }
    
    // Add a gate to the circuit
    public GameObject AddGate(GateType gateType, int qubitIndex, int position = -1)
    {
        // Validate inputs
        if (qubitIndex < 0 || qubitIndex >= maxQubits)
        {
            //Debug.LogError($"QuantumCircuitManager: Invalid qubit index {qubitIndex}!");
            return null;
        }
        
        // Get the appropriate gate prefab
        GameObject gatePrefab = GetGatePrefab(gateType);
        if (gatePrefab == null)
        {
            //Debug.LogError($"QuantumCircuitManager: No prefab assigned for gate type {gateType}!");
            return null;
        }
        
        // Calculate position
        int gatePosition = position;
        if (gatePosition < 0)
        {
            // Add to the end of the circuit
            gatePosition = gatesByQubit[qubitIndex].Count;
        }
        
        Vector3 gatePos = new Vector3(gatePosition * gateSpacing, -qubitIndex * qubitSpacing, 0);
        
        // Create the gate
        GameObject gate = Instantiate(gatePrefab, gatePos, Quaternion.identity, transform);
        gate.name = $"{gateType}_Q{qubitIndex}_P{gatePosition}";
        
        // Add to the lists
        gates.Add(gate);
        gatesByQubit[qubitIndex].Add(gate);
        
        // Apply the gate operation to update the quantum state
        ApplyGateOperation(gateType, qubitIndex);
        
        return gate;
    }
    
    // Add a two-qubit gate to the circuit
    public GameObject AddTwoQubitGate(GateType gateType, int controlQubit, int targetQubit, int position = -1)
    {
        // Validate inputs
        if (controlQubit < 0 || controlQubit >= maxQubits || targetQubit < 0 || targetQubit >= maxQubits)
        {
            //Debug.LogError($"QuantumCircuitManager: Invalid qubit indices {controlQubit}, {targetQubit}!");
            return null;
        }
        
        if (controlQubit == targetQubit)
        {
            //Debug.LogError("QuantumCircuitManager: Control and target qubits must be different!");
            return null;
        }
        
        // Get the appropriate gate prefab
        GameObject gatePrefab = GetGatePrefab(gateType);
        if (gatePrefab == null)
        {
            //Debug.LogError($"QuantumCircuitManager: No prefab assigned for gate type {gateType}!");
            return null;
        }
        
        // Calculate position
        int gatePosition = position;
        if (gatePosition < 0)
        {
            // Add to the end of the circuit, considering both qubits
            int controlQubitGateCount = gatesByQubit[controlQubit].Count;
            int targetQubitGateCount = gatesByQubit[targetQubit].Count;
            gatePosition = Mathf.Max(controlQubitGateCount, targetQubitGateCount);
        }
        
        // Calculate the main gate position (on the control qubit)
        Vector3 controlGatePos = new Vector3(gatePosition * gateSpacing, -controlQubit * qubitSpacing, 0);
        
        // Create the main gate
        GameObject gate = Instantiate(gatePrefab, controlGatePos, Quaternion.identity, transform);
        gate.name = $"{gateType}_C{controlQubit}_T{targetQubit}_P{gatePosition}";
        
        // Create the connection line
        if (gateConnectionPrefab != null)
        {
            // Calculate the start and end positions of the connection
            Vector3 startPos = controlGatePos;
            Vector3 endPos = new Vector3(gatePosition * gateSpacing, -targetQubit * qubitSpacing, 0);
            
            // Create the connection
            GameObject connection = Instantiate(gateConnectionPrefab, transform);
            connection.name = $"Connection_{gateType}_C{controlQubit}_T{targetQubit}";
            
            // Configure the connection
            LineRenderer lineRenderer = connection.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
            }
            
            // Associate the connection with the gate
            connection.transform.SetParent(gate.transform);
        }
        
        // Add to the lists
        gates.Add(gate);
        gatesByQubit[controlQubit].Add(gate);
        gatesByQubit[targetQubit].Add(gate);
        
        // Apply the gate operation to update the quantum state
        ApplyTwoQubitGateOperation(gateType, controlQubit, targetQubit);
        
        return gate;
    }
    
    // Get the appropriate gate prefab based on gate type
    private GameObject GetGatePrefab(GateType gateType)
    {
        switch (gateType)
        {
            case GateType.Hadamard:
                return hadamardGatePrefab;
            case GateType.PauliX:
                return pauliXGatePrefab;
            case GateType.PauliY:
                return pauliYGatePrefab;
            case GateType.PauliZ:
                return pauliZGatePrefab;
            case GateType.CNOT:
                return cnotGatePrefab;
            case GateType.Swap:
                return swapGatePrefab;
            default:
                //Debug.LogError($"QuantumCircuitManager: Unknown gate type {gateType}!");
                return null;
        }
    }
    
    // Apply a single-qubit gate operation
    private void ApplyGateOperation(GateType gateType, int qubitIndex)
    {
        // For educational purposes only - simplified quantum operations
        // In a real quantum system, operations are applied to the entire state vector
        
        Complex qubitState = qubitStates[qubitIndex];
        
        switch (gateType)
        {
            case GateType.Hadamard:
                // |0⟩ -> (|0⟩ + |1⟩)/√2
                // |1⟩ -> (|0⟩ - |1⟩)/√2
                if (IsInState0(qubitState))
                {
                    // Set to superposition
                    qubitStates[qubitIndex] = new Complex(1 / Mathf.Sqrt(2), 0);
                }
                else if (IsInState1(qubitState))
                {
                    // Set to superposition with negative amplitude for |1⟩
                    qubitStates[qubitIndex] = new Complex(1 / Mathf.Sqrt(2), 0);
                }
                else
                {
                    // Already in superposition - result depends on the specific state
                    // For educational simplification, we'll just toggle
                    qubitStates[qubitIndex] = new Complex(qubitState.real * -1, qubitState.imaginary);
                }
                break;
                
            case GateType.PauliX:
                // X gate: |0⟩ -> |1⟩, |1⟩ -> |0⟩ (bit flip)
                if (IsInState0(qubitState))
                {
                    qubitStates[qubitIndex] = new Complex(0, 0, 1, 0); // |1⟩
                }
                else if (IsInState1(qubitState))
                {
                    qubitStates[qubitIndex] = new Complex(1, 0, 0, 0); // |0⟩
                }
                else
                {
                    // Superposition: invert the amplitudes
                    // For educational purposes, we'll just invert the real part
                    qubitStates[qubitIndex] = new Complex(qubitState.real * -1, qubitState.imaginary);
                }
                break;
                
            case GateType.PauliY:
                // Y gate: |0⟩ -> i|1⟩, |1⟩ -> -i|0⟩
                if (IsInState0(qubitState))
                {
                    qubitStates[qubitIndex] = new Complex(0, 0, 0, 1); // i|1⟩
                }
                else if (IsInState1(qubitState))
                {
                    qubitStates[qubitIndex] = new Complex(0, -1, 0, 0); // -i|0⟩
                }
                else
                {
                    // For educational purposes, we'll just add an imaginary component
                    qubitStates[qubitIndex] = new Complex(qubitState.imaginary, qubitState.real);
                }
                break;
                
            case GateType.PauliZ:
                // Z gate: |0⟩ -> |0⟩, |1⟩ -> -|1⟩ (phase flip)
                if (IsInState1(qubitState))
                {
                    qubitStates[qubitIndex] = new Complex(0, 0, -1, 0); // -|1⟩
                }
                // |0⟩ stays the same
                break;
                
            default:
                //Debug.LogWarning($"QuantumCircuitManager: Operation for gate type {gateType} not implemented!");
                break;
        }
        
        // Update visualization
        UpdateStateVisualization();
    }
    
    // Apply a two-qubit gate operation
    private void ApplyTwoQubitGateOperation(GateType gateType, int controlQubit, int targetQubit)
    {
        // For educational purposes only - simplified quantum operations
        
        Complex controlState = qubitStates[controlQubit];
        Complex targetState = qubitStates[targetQubit];
        
        switch (gateType)
        {
            case GateType.CNOT:
                // If control qubit is |1⟩, apply X gate to target
                if (IsInState1(controlState))
                {
                    ApplyGateOperation(GateType.PauliX, targetQubit);
                }
                break;
                
            case GateType.Swap:
                // Swap the states of the two qubits
                qubitStates[controlQubit] = targetState;
                qubitStates[targetQubit] = controlState;
                break;
                
            default:
                //Debug.LogWarning($"QuantumCircuitManager: Operation for two-qubit gate type {gateType} not implemented!");
                break;
        }
        
        // Update visualization
        UpdateStateVisualization();
    }
    
    // Helper methods to check qubit states
    private bool IsInState0(Complex state)
    {
        // Check if qubit is in |0⟩ state
        return Mathf.Approximately(state.real, 1) && Mathf.Approximately(state.imaginary, 0);
    }
    
    private bool IsInState1(Complex state)
    {
        // Check if qubit is in |1⟩ state
        // Fixed: Properly check the nullable Vector4
        if (!state.HasValue())
            return false;
            
        Vector4 value = state.value.Value; // Properly handle the nullable Vector4
        return Mathf.Approximately(state.real, 0) && 
               Mathf.Approximately(state.imaginary, 0) && 
               Mathf.Approximately(value.x, 0) && 
               Mathf.Approximately(value.y, 0) && 
               Mathf.Approximately(value.z, 1);
    }
    
    // Update the visual representation of qubit states
    private void UpdateStateVisualization()
    {
        if (!showProbabilityAmplitudes)
            return;
            
        for (int i = 0; i < qubitStates.Count; i++)
        {
            if (i >= qubitLines.Count)
                continue;
                
            GameObject qubitLine = qubitLines[i];
            
            // Find or create state visualization
            Transform stateLabel = qubitLine.transform.Find("StateLabel");
            TMPro.TextMeshPro textComponent = null;
            
            if (stateLabel == null)
            {
                // Create a new state label
                GameObject label = new GameObject("StateLabel");
                label.transform.SetParent(qubitLine.transform);
                label.transform.localPosition = new Vector3(-2f, 0, 0); // Position at the start of the line
                
                textComponent = label.AddComponent<TMPro.TextMeshPro>();
                textComponent.fontSize = 4;
                textComponent.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                textComponent.color = Color.white;
            }
            else
            {
                textComponent = stateLabel.GetComponent<TMPro.TextMeshPro>();
            }
            
            if (textComponent != null)
            {
                Complex state = qubitStates[i];
                
                // Format the state visualization
                string stateText;
                if (IsInState0(state))
                {
                    stateText = "|0⟩";
                }
                else if (IsInState1(state))
                {
                    stateText = "|1⟩";
                }
                else
                {
                    // Show superposition
                    stateText = FormatComplexState(state);
                }
                
                textComponent.text = stateText;
            }
        }
    }
    
    // Format a complex state for display
    private string FormatComplexState(Complex state)
    {
        // For educational purposes, we'll use a simplified representation
        // In a real system, we would show the full state vector including amplitudes
        
        if (state.HasValue())
        {
            // Custom state vector - simplified for educational display
            return $"α|0⟩ + β|1⟩";
        }
        else
        {
            // Just real and imaginary parts
            float real = state.real;
            float imag = state.imaginary;
            
            if (Mathf.Approximately(real, 1/Mathf.Sqrt(2)) && Mathf.Approximately(imag, 0))
            {
                return $"1/√2|0⟩ + 1/√2|1⟩";
            }
            else
            {
                return $"α|0⟩ + β|1⟩";
            }
        }
    }
    
    // Reset the circuit to its initial state
    public void ResetCircuit()
    {
        // Clear all gates
        foreach (GameObject gate in gates)
        {
            Destroy(gate);
        }
        gates.Clear();
        
        // Clear gate lists by qubit
        foreach (var list in gatesByQubit.Values)
        {
            list.Clear();
        }
        
        // Reset qubit states to |0⟩
        for (int i = 0; i < qubitStates.Count; i++)
        {
            qubitStates[i] = new Complex(1, 0);
        }
        
        // Update visualization
        UpdateStateVisualization();
    }
    
    // Get a copy of the current circuit state
    public List<Complex> GetCircuitState()
    {
        List<Complex> stateCopy = new List<Complex>();
        foreach (var state in qubitStates)
        {
            stateCopy.Add(state);
        }
        return stateCopy;
    }
    
    // Get the state of a specific qubit
    public Complex GetQubitState(int qubitIndex)
    {
        if (qubitIndex < 0 || qubitIndex >= qubitStates.Count)
        {
            //Debug.LogError($"QuantumCircuitManager: Invalid qubit index {qubitIndex}!");
            return new Complex(0, 0);
        }
        
        return qubitStates[qubitIndex];
    }
    
    // Set the state of a specific qubit (for educational purposes)
    public void SetQubitState(int qubitIndex, Complex state)
    {
        if (qubitIndex < 0 || qubitIndex >= qubitStates.Count)
        {
            //Debug.LogError($"QuantumCircuitManager: Invalid qubit index {qubitIndex}!");
            return;
        }
        
        qubitStates[qubitIndex] = state;
        UpdateStateVisualization();
    }
    
    // Set the maximum number of qubits
    public void SetMaxQubits(int newMaxQubits)
    {
        if (newMaxQubits < 1)
        {
            //Debug.LogError("QuantumCircuitManager: Max qubits must be at least 1!");
            return;
        }
        
        // If reducing the number of qubits, remove excess qubits
        if (newMaxQubits < maxQubits)
        {
            for (int i = newMaxQubits; i < maxQubits; i++)
            {
                if (i < qubitLines.Count)
                {
                    Destroy(qubitLines[i]);
                }
                
                if (i < qubitStates.Count)
                {
                    qubitStates.RemoveAt(i);
                }
                
                if (gatesByQubit.ContainsKey(i))
                {
                    foreach (var gate in gatesByQubit[i])
                    {
                        if (gates.Contains(gate))
                        {
                            gates.Remove(gate);
                        }
                        Destroy(gate);
                    }
                    
                    gatesByQubit.Remove(i);
                }
            }
            
            // Trim the lists
            if (qubitLines.Count > newMaxQubits)
            {
                qubitLines.RemoveRange(newMaxQubits, qubitLines.Count - newMaxQubits);
            }
        }
        // If increasing the number of qubits, add new qubits
        else if (newMaxQubits > maxQubits)
        {
            for (int i = maxQubits; i < newMaxQubits; i++)
            {
                CreateQubitLine(i);
                qubitStates.Add(new Complex(1, 0));
                gatesByQubit[i] = new List<GameObject>();
            }
        }
        
        maxQubits = newMaxQubits;
        UpdateStateVisualization();
    }
    
    // Helper structure for complex numbers in quantum computing
    public struct Complex
    {
        public float real;
        public float imaginary;
        public Vector4? value; // For representing specific states like |0⟩ and |1⟩
        
        public Complex(float real, float imaginary)
        {
            this.real = real;
            this.imaginary = imaginary;
            this.value = null;
        }
        
        public Complex(float real, float imaginary, float valueX, float valueY)
        {
            this.real = real;
            this.imaginary = imaginary;
            this.value = new Vector4(valueX, valueY, 0, 0);
        }
        
        public Complex(float real, float imaginary, float valueX, float valueY, float valueZ)
        {
            this.real = real;
            this.imaginary = imaginary;
            this.value = new Vector4(valueX, valueY, valueZ, 0);
        }
        
        public bool HasValue()
        {
            return value.HasValue;
        }
        
        // Addition operator
        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.imaginary + b.imaginary);
        }
        
        // Subtraction operator
        public static Complex operator -(Complex a, Complex b)
        {
            return new Complex(a.real - b.real, a.imaginary - b.imaginary);
        }
        
        // Multiplication operator
        public static Complex operator *(Complex a, Complex b)
        {
            float resultReal = a.real * b.real - a.imaginary * b.imaginary;
            float resultImaginary = a.real * b.imaginary + a.imaginary * b.real;
            return new Complex(resultReal, resultImaginary);
        }
        
        // Scalar multiplication
        public static Complex operator *(Complex a, float scalar)
        {
            return new Complex(a.real * scalar, a.imaginary * scalar);
        }
        
        // String representation
        public override string ToString()
        {
            if (HasValue())
            {
                Vector4 v = value.Value; // Properly access the Vector4 value
                if (Mathf.Approximately(v.z, 1f) && Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0f))
                {
                    return "|1⟩"; // Basis state |1⟩
                }
                else if (Mathf.Approximately(v.x, 1f) && Mathf.Approximately(v.y, 0f) && Mathf.Approximately(v.z, 0f))
                {
                    return "|0⟩"; // Basis state |0⟩
                }
                else
                {
                    return $"({v.x}, {v.y}, {v.z})";
                }
            }
            
            string realPart = real != 0 ? $"{real}" : "";
            string imagPart = "";
            
            if (imaginary != 0)
            {
                string sign = imaginary > 0 ? "+" : "-";
                if (real != 0)
                {
                    imagPart = $"{sign}{Mathf.Abs(imaginary)}i";
                }
                else
                {
                    imagPart = $"{imaginary}i";
                }
            }
            
            if (real == 0 && imaginary == 0)
            {
                return "0";
            }
            
            return realPart + imagPart;
        }
    }
}
    