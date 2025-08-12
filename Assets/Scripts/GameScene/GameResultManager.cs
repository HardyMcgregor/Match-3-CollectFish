using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameResultManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel;
    public Text resultText;
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
    public float panelDelay = 1f;

    private bool gameEnded = false;

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (replayButton != null)
            replayButton.onClick.AddListener(ReplayGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

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
        return collectionManager != null && collectionManager.IsCollectionInProgress();
    }

    void CheckGameResult()
    {
        if (gameEnded) return;

        if (AllBoardsCompleted())
        {
            ShowResult(true);
            return;
        }

        if (collectionBar != null && collectionBar.IsFull())
        {
            if (HasFishOnActiveBoard())
            {
                ShowResult(false);
                return;
            }
        }
    }

    bool AllBoardsCompleted()
    {
        if (collectionManager == null) return false;

        string[] boardOrder = { "Board_3", "Board_2", "Board_1" };

        foreach (string boardName in boardOrder)
        {
            int remainingFish = collectionManager.GetRemainingFishCount(boardName);
            if (remainingFish > 0)
            {
                return false;
            }
        }

        return true;
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
        StartCoroutine(ShowResultWithDelay(isWin));
    }

    System.Collections.IEnumerator ShowResultWithDelay(bool isWin)
    {
        yield return new WaitForSeconds(panelDelay);

        if (resultText != null)
        {
            resultText.text = isWin ? winMessage : loseMessage;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    public void ReplayGame()
    {
        StopAllCoroutines();
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

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

    public void ResetGameState()
    {
        gameEnded = false;
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void OnDisable()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(ReplayGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }
}