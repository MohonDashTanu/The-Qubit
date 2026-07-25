using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Simplified Game Over Manager - only tracks total earnings for transfer
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI currencyEarnedText; // Shows "Currency Earned: X"
    [SerializeField] private TextMeshProUGUI transferStatusText; // Shows transfer confirmation
    
    [Header("Scene Management")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip gameOverSound;
    
    private bool gameEnded = false;
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Hide game over panel initially
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Setup button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }
    
    /// <summary>
    /// Trigger game over sequence
    /// </summary>
    public void TriggerGameOver()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        
        Debug.Log("Game Over triggered!");
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Play game over sound
        if (gameOverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Handle currency transfer and UI updates
        HandleCurrencyTransfer();
    }
    
    /// <summary>
    /// Handle currency transfer and UI updates - simplified version
    /// </summary>
    private void HandleCurrencyTransfer()
    {
        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager == null)
        {
            Debug.LogWarning("No GameSessionManager found!");
            ShowCurrencyError();
            return;
        }
        
        // Get total currency earned this run
        int totalEarned = sessionManager.GetTotalEarnedThisRun();
        
        // Update currency earned display
        if (currencyEarnedText != null)
        {
            currencyEarnedText.text = $"Currency Earned: {totalEarned}";
        }
        
        // Transfer currency to upgrade system
        sessionManager.OnGameOver();
        
        // Show transfer confirmation
        if (transferStatusText != null)
        {
            if (totalEarned > 0)
            {
                transferStatusText.text = $"{totalEarned} Currency Added to Upgrades!";
                transferStatusText.color = Color.green;
            }
            else
            {
                transferStatusText.text = "No Currency Earned This Run";
                transferStatusText.color = Color.gray;
            }
        }
        
        Debug.Log($"Game Over - {totalEarned} currency transferred to upgrade system");
    }
    
    /// <summary>
    /// Show error message if session manager not found
    /// </summary>
    private void ShowCurrencyError()
    {
        if (currencyEarnedText != null)
        {
            currencyEarnedText.text = "Currency Earned: Error";
        }
        
        if (transferStatusText != null)
        {
            transferStatusText.text = "Error: Could not transfer currency";
            transferStatusText.color = Color.red;
        }
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        
        // Resume time scale
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Restart current level (optional)
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("Restarting level...");
        
        // Resume time scale
        Time.timeScale = 1f;
        
        // Reset earnings for new run
        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager != null)
        {
            sessionManager.ResetEarnings();
        }
        
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Get currency earned this run (for external UI elements)
    /// </summary>
    public int GetCurrencyEarnedThisRun()
    {
        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager != null)
        {
            return sessionManager.GetTotalEarnedThisRun();
        }
        return 0;
    }
}