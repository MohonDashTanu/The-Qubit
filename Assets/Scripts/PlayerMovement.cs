using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useRigidbody = false;
    
    private Rigidbody2D rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // If no rigidbody and useRigidbody is true, add one
        if (rb == null && useRigidbody)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // No gravity for top-down
        }
    }
    
    private void Update()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Arrow Keys
        float vertical = Input.GetAxis("Vertical");     // W/S or Arrow Keys
        
        Vector2 movement = new Vector2(horizontal, vertical);
        
        // Normalize diagonal movement
        if (movement.magnitude > 1)
        {
            movement = movement.normalized;
        }
        
        // Move the player
        if (useRigidbody && rb != null)
        {
            // Physics-based movement
            rb.linearVelocity = movement * moveSpeed;
        }
        else
        {
            // Transform-based movement
            transform.Translate(movement * moveSpeed * Time.deltaTime);
        }
    }
}