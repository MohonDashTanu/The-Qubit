using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject upgradePanel;
    
    [Header("Buttons")]
    [SerializeField] private Button newRunButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button backButton; // Back button in upgrade panel
    
    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "GameScene"; // Name of your main game scene
    
    private void Start()
    {
        // Initialize panels
        ShowMainMenu();
        
        // Setup button listeners
        if (newRunButton != null)
            newRunButton.onClick.AddListener(StartNewRun);
            
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(ShowUpgradePanel);
            
        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);
            
        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);
    }
    
    /// <summary>
    /// Show the main menu and hide upgrade panel
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }
    
    /// <summary>
    /// Show the upgrade panel and hide main menu
    /// </summary>
    public void ShowUpgradePanel()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
    }
    
    /// <summary>
    /// Start a new game run
    /// </summary>
    public void StartNewRun()
    {
        Debug.Log("Starting new run...");
        
        // Load the main game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogWarning("Game scene name not set! Please assign it in the inspector.");
        }
    }
    
    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Toggle between main menu and upgrade panel
    /// </summary>
    public void ToggleUpgradePanel()
    {
        if (upgradePanel != null)
        {
            bool isUpgradePanelActive = upgradePanel.activeSelf;
            
            if (isUpgradePanelActive)
            {
                ShowMainMenu();
            }
            else
            {
                ShowUpgradePanel();
            }
        }
    }
}