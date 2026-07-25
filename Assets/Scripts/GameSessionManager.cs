using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simplified version - Only tracks total currency earned this run
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpgradeManager upgradeManager; // Your main menu upgrade manager
    
    [Header("Session Currency Tracking")]
    [SerializeField] private int totalEarnedThisRun = 0;
    
    [Header("UI Display (Optional)")]
    [SerializeField] private TextMeshProUGUI totalEarnedText; // Shows "Run Total: X"
    
    [Header("Events")]
    public System.Action<int> OnTotalEarnedChanged;
    
    // Singleton for easy access
    public static GameSessionManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist between scenes if needed
    }
    
    private void Start()
    {
        // Initialize currency tracking
        totalEarnedThisRun = 0;
        UpdateUI();
        
        Debug.Log("GameSessionManager started - tracking currency earnings for this run");
    }
    
    /// <summary>
    /// Add currency earned during this game session
    /// </summary>
    public void AddSessionCurrency(int amount)
    {
        if (amount <= 0) return;
        
        totalEarnedThisRun += amount;
        
        OnTotalEarnedChanged?.Invoke(totalEarnedThisRun);
        UpdateUI();
        
        Debug.Log($"Earned {amount} currency. Total this run: {totalEarnedThisRun}");
    }
    
    /// <summary>
    /// Transfer all earned currency to the main upgrade system
    /// Call this when the game ends or player returns to main menu
    /// </summary>
    public void TransferCurrencyToUpgradeSystem()
    {
        if (upgradeManager == null)
        {
            Debug.LogError("No UpgradeManager assigned! Cannot transfer currency.");
            return;
        }
        
        if (totalEarnedThisRun > 0)
        {
            upgradeManager.AddCurrency(totalEarnedThisRun);
            Debug.Log($"Transferred {totalEarnedThisRun} currency to upgrade system");
            
            // Reset for next run
            totalEarnedThisRun = 0;
            UpdateUI();
        }
        else
        {
            Debug.Log("No currency to transfer");
        }
    }
    
    /// <summary>
    /// Get total currency earned this session
    /// </summary>
    public int GetTotalEarnedThisRun()
    {
        return totalEarnedThisRun;
    }
    
    /// <summary>
    /// Reset earnings (for new runs)
    /// </summary>
    public void ResetEarnings()
    {
        totalEarnedThisRun = 0;
        UpdateUI();
        Debug.Log("Session earnings reset for new run");
    }
    
    /// <summary>
    /// Update UI display
    /// </summary>
    private void UpdateUI()
    {
        if (totalEarnedText != null)
        {
            totalEarnedText.text = $"{totalEarnedThisRun}";
        }
    }
    
    /// <summary>
    /// Called when game ends - automatically transfer currency
    /// </summary>
    public void OnGameOver()
    {
        Debug.Log($"Game Over! Transferring {totalEarnedThisRun} currency to upgrade system");
        TransferCurrencyToUpgradeSystem();
    }
    
    /// <summary>
    /// Called when returning to main menu
    /// </summary>
    public void OnReturnToMainMenu()
    {
        TransferCurrencyToUpgradeSystem();
    }
}