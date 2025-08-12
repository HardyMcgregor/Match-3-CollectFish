using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameResultManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel; // The panel that contains everything
    public Text resultText; // The text component that shows "YOU WIN" or "YOU LOSE"
    public Button replayButton;
    public Button quitButton;

    [Header("Game References")]
    public FishCollectionManager collectionManager;
    public CollectionBar collectionBar;
    public MultiBoardGenerator boardGenerator;

    [Header("Result Messages")]
    public string winMessage = "YOU WIN!";
    public string loseMessage = "YOU LOSE!";

    [Header("Settings")]
    public float panelDelay = 1f; // Delay before showing the panel

    private bool gameEnded = false;

    void Start()
    {
        // Initially hide the result panel
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Setup button listeners
        if (replayButton != null)
            replayButton.onClick.AddListener(ReplayGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Auto-find references if not assigned
        AutoFindReferences();
    }

    void AutoFindReferences()
    {
        if (collectionManager == null)
            collectionManager = FindObjectOfType<FishCollectionManager>();

        if (collectionBar == null)
            collectionBar = FindObjectOfType<CollectionBar>();

        if (boardGenerator == null)
            boardGenerator = FindObjectOfType<MultiBoardGenerator>();

        // Warn if critical references are missing
        if (collectionManager == null)
            Debug.LogWarning("GameResultManager: FishCollectionManager not found!");
        if (collectionBar == null)
            Debug.LogWarning("GameResultManager: CollectionBar not found!");
    }

    void Update()
    {
        if (!gameEnded && !IsGameInProgress())
        {
            CheckGameResult();
        }
    }

    bool IsGameInProgress()
    {
        // Don't check results while animations are playing
        return collectionManager != null && collectionManager.IsCollectionInProgress();
    }

    void CheckGameResult()
    {
        if (gameEnded) return;

        // Check for win condition: all boards completed
        if (AllBoardsCompleted())
        {
            ShowResult(true); // Win
            return;
        }

        // Check for lose condition: collection bar is full but there are still fish on active board
        if (collectionBar != null && collectionBar.IsFull())
        {
            if (HasFishOnActiveBoard())
            {
                ShowResult(false); // Lose
                return;
            }
        }
    }

    bool AllBoardsCompleted()
    {
        if (collectionManager == null) return false;

        // Check if all boards have been completed (no fish remaining on any board)
        string[] boardOrder = { "Board_3", "Board_2", "Board_1" }; // Same order as in FishCollectionManager

        foreach (string boardName in boardOrder)
        {
            int remainingFish = collectionManager.GetRemainingFishCount(boardName);
            if (remainingFish > 0)
            {
                return false; // Still has fish on this board
            }
        }

        return true; // All boards are empty
    }

    bool HasFishOnActiveBoard()
    {
        if (collectionManager == null) return false;

        string activeBoard = collectionManager.GetCurrentActiveBoard();
        int remainingFish = collectionManager.GetRemainingFishCount(activeBoard);

        return remainingFish > 0;
    }

    void ShowResult(bool isWin)
    {
        if (gameEnded) return;

        gameEnded = true;

        // Use coroutine to add delay before showing result
        StartCoroutine(ShowResultWithDelay(isWin));
    }

    System.Collections.IEnumerator ShowResultWithDelay(bool isWin)
    {
        // Wait for any ongoing animations to finish
        yield return new WaitForSeconds(panelDelay);

        // Set the result text
        if (resultText != null)
        {
            resultText.text = isWin ? winMessage : loseMessage;
        }

        // Show the result panel
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        Debug.Log(isWin ? "Game Won!" : "Game Lost!");
    }

    public void ReplayGame()
    {
        Debug.Log("Restarting game...");

        // Stop all coroutines to prevent conflicts
        StopAllCoroutines();

        // Reload the current scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        // In Unity Editor, stop playing
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // In build, quit the application
        Application.Quit();
#endif
    }

    // Public methods that can be called from other scripts if needed
    public void ForceWin()
    {
        ShowResult(true);
    }

    public void ForceLose()
    {
        ShowResult(false);
    }

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    // Reset game state (useful if you want to restart without reloading scene)
    public void ResetGameState()
    {
        gameEnded = false;
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void OnDisable()
    {
        // Clean up button listeners
        if (replayButton != null)
            replayButton.onClick.RemoveListener(ReplayGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }
}