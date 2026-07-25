using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private float baseSpeed = 10f;
    [SerializeField] private float lifetime = 5f;
    
    private int damage;
    private Vector2 direction;
    private float actualSpeed; // The speed this projectile will actually use
    
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    // Enhanced initialization that accepts custom speed
    public void Initialize(Vector2 targetDirection, int damage, float? customSpeed = null)
    {
        this.damage = damage;
        this.direction = targetDirection.normalized;
        
        // Use custom speed if provided, otherwise use base speed
        this.actualSpeed = customSpeed ?? baseSpeed;
        
        // Set rotation to face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        //Debug.Log($"Projectile initialized with direction: {direction}, speed: {actualSpeed}, damage: {damage}");
    }
    
    private void Update()
    {
        // Use the actual speed for movement
        Vector2 velocity = direction * actualSpeed;
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            
            Destroy(gameObject);
        }
    }
}