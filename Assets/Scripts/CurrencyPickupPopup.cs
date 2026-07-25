using UnityEngine;
using TMPro;

public class CurrencyPickupPopup : MonoBehaviour 
{
    // Create a Currency Pickup Popup
    public static CurrencyPickupPopup Create(Vector3 position, int currencyAmount, TMP_FontAsset font = null) 
    {
        // Create the popup GameObject
        GameObject popupObject = new GameObject("CurrencyPickup", typeof(CurrencyPickupPopup));
        popupObject.transform.position = position;

        CurrencyPickupPopup popup = popupObject.GetComponent<CurrencyPickupPopup>();
        popup.Setup(currencyAmount, font);

        return popup;
    }

    private static int sortingOrder;

    [Header("Popup Settings")]
    private const float DISAPPEAR_TIMER_MAX = 1.5f;
    private const float MOVE_SPEED = 1f; // Reduced from 60f
    private const float SCALE_SPEED = 1f;
    private const float DISAPPEAR_SPEED = 3f;

    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;

    private void Awake() 
    {
        // Add TextMeshPro component
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Set sorting order
        sortingOrder++;
        textMesh.sortingOrder = sortingOrder;
    }

    public void Setup(int currencyAmount, TMP_FontAsset font = null) 
    {
        // Set text
        textMesh.SetText($"+{currencyAmount}");
        
        // Set font if provided
        if (font != null)
        {
            textMesh.font = font;
        }
        
        // Set appearance
        textMesh.fontSize = 4f; // Good size for world space
        textColor = Color.yellow;
        textMesh.color = textColor;
        
        // Set timer
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Set movement (upward and slightly random horizontal)
        float randomX = Random.Range(-0.5f, 0.5f);
        moveVector = new Vector3(randomX, 1f) * MOVE_SPEED;
        
        // Start small
        transform.localScale = Vector3.one * 0.5f;
    }

    private void Update() 
    {
        // Move the popup
        transform.position += moveVector * Time.deltaTime;
        
        // Slow down movement over time (reduced deceleration)
        moveVector -= moveVector * 4f * Time.deltaTime;

        // Scale animation
        if (disappearTimer > DISAPPEAR_TIMER_MAX * 0.5f) 
        {
            // First half - grow
            float increaseScaleAmount = SCALE_SPEED;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
            
            // Cap the scale
            if (transform.localScale.x > 1.2f)
            {
                transform.localScale = Vector3.one * 1.2f;
            }
        } 
        else 
        {
            // Second half - shrink slightly
            float decreaseScaleAmount = SCALE_SPEED * 0.5f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        // Timer countdown
        disappearTimer -= Time.deltaTime;
        
        if (disappearTimer < 0) 
        {
            // Start fading out
            textColor.a -= DISAPPEAR_SPEED * Time.deltaTime;
            textMesh.color = textColor;
            
            if (textColor.a < 0) 
            {
                Destroy(gameObject);
            }
        }
    }
}