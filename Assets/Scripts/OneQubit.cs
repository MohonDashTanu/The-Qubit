using UnityEngine;

public class OneQubit : MonoBehaviour
{
    [Header("Combat Properties")]
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private GameObject projectilePrefab;
    
    // Visual indicator for debugging
    [SerializeField] private bool showAttackRange = false;
    
    // Internal variables
    private float attackTimer = 0f;
    
    // Reference to the base Qubit component
    private Qubit qubitComponent;
    
    private void Start()
    {
        // Get the Qubit component
        qubitComponent = GetComponent<Qubit>();
        
        // Make sure the object has the Qubit tag
        if (tag != "Qubit")
        {
            tag = "Qubit";
        }
        
        // If we have QubitData, use its values instead of our serialized ones
        if (qubitComponent != null && qubitComponent.QubitData != null)
        {
            attackRange = qubitComponent.QubitData.attackRange;
            attackDamage = qubitComponent.QubitData.attackPower;
            attackRate = qubitComponent.QubitData.attackSpeed;
            
            Debug.Log($"OneQubit using QubitData: Range={attackRange}, Damage={attackDamage}, Rate={attackRate}");
        }
        
        // Debug visualization
        if (showAttackRange)
        {
            Debug.Log($"OneQubit initialized with range: {attackRange}");
        }
    }
    
    private void Update()
    {
        // Skip all actions if in preview mode
        if (IsInPreviewMode())
            return;
            
        // Let the base Qubit class handle combat - don't duplicate logic
        // The OneQubit script mainly exists to identify this as a combat qubit type
        
        // Optional: You can add OneQubit-specific behaviors here if needed
    }
    
    private bool IsInPreviewMode()
    {
        if (qubitComponent == null)
            return false;
            
        // Use reflection to check preview mode from base Qubit
        System.Reflection.FieldInfo previewField = typeof(Qubit).GetField("isInPreviewMode", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (previewField != null)
        {
            return (bool)previewField.GetValue(qubitComponent);
        }
        
        return false;
    }
    
    // Public method to get attack range (used by other systems)
    public float GetAttackRange()
    {
        // Use QubitData if available, otherwise fall back to serialized value
        if (qubitComponent != null && qubitComponent.QubitData != null)
        {
            return qubitComponent.QubitData.attackRange;
        }
        return attackRange;
    }
    
    // Public setter for attack range
    public void SetAttackRange(float newRange)
    {
        if (newRange > 0)
        {
            attackRange = newRange;
        }
    }
    
    // Visualize the attack range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        float currentRange = GetAttackRange();
        Gizmos.DrawWireSphere(transform.position, currentRange);
    }
}