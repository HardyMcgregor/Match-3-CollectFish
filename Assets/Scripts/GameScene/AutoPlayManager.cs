using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlayManager : MonoBehaviour
{
    [Header("References")]
    public FishCollectionManager collectionManager;
    public CollectionBar collectionBar;
    public MultiBoardGenerator boardGenerator;
    public GameResultManager gameResultManager;

    [Header("Auto-Play Settings")]
    public float fishCollectionDelay = 0.8f; // Delay between collecting fish
    public float boardSwitchDelay = 1.5f; // Extra delay when switching boards

    private bool isAutoPlaying = false;
    private bool isAutoWin = false;
    private Coroutine autoPlayCoroutine;

    void Start()
    {
        // Check if we should start auto-play
        if (GameStartManager.isAutoPlay)
        {
            isAutoPlaying = true;
            isAutoWin = GameStartManager.isAutoWin;

            Debug.Log($"Auto-Play Mode: {(isAutoWin ? "AUTO-WIN" : "AUTO-LOSE")}");

            // Start auto-play after a short delay to let everything initialize
            StartCoroutine(StartAutoPlayDelayed());
        }

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

        if (gameResultManager == null)
            gameResultManager = FindObjectOfType<GameResultManager>();
    }

    IEnumerator StartAutoPlayDelayed()
    {
        yield return new WaitForSeconds(1f); // Wait for initialization

        if (isAutoWin)
        {
            autoPlayCoroutine = StartCoroutine(AutoWinRoutine());
        }
        else
        {
            autoPlayCoroutine = StartCoroutine(AutoLoseRoutine());
        }
    }

    IEnumerator AutoWinRoutine()
    {
        Debug.Log("Starting Auto-Win routine...");

        while (!gameResultManager.IsGameEnded())
        {
            // Wait for any ongoing animations to finish
            yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());

            // Get the current active board
            string activeBoard = collectionManager.GetCurrentActiveBoard();

            // Find a fish to collect on the current board
            FishController fishToCollect = FindBestFishForWin(activeBoard);

            if (fishToCollect != null)
            {
                Debug.Log($"Auto-collecting {fishToCollect.name} for WIN strategy");

                // Simulate clicking the fish
                collectionManager.OnFishClicked(fishToCollect, activeBoard);

                // Wait for collection animation
                yield return new WaitForSeconds(fishCollectionDelay);
            }
            else
            {
                Debug.LogWarning("No suitable fish found for auto-win!");
                yield return new WaitForSeconds(0.5f);
            }

            // Check if we completed a board
            if (collectionManager.GetRemainingFishCount(activeBoard) <= 0)
            {
                Debug.Log($"Board {activeBoard} completed in auto-win mode");
                yield return new WaitForSeconds(boardSwitchDelay);
            }
        }

        Debug.Log("Auto-Win routine completed!");
    }

    IEnumerator AutoLoseRoutine()
    {
        Debug.Log("Starting Auto-Lose routine...");

        while (!gameResultManager.IsGameEnded())
        {
            // Wait for any ongoing animations to finish
            yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());

            // Check if collection bar is almost full
            if (collectionBar.GetCollectedCount() >= collectionBar.maxSlots - 1)
            {
                // Collection bar is almost full, now we need to make sure we can't complete it
                Debug.Log("Collection bar almost full - triggering lose condition");

                // Try to collect one more fish that won't create a match of 3
                string activeBoard = collectionManager.GetCurrentActiveBoard();
                FishController fishToCollect = FindWorstFishForLose(activeBoard);

                if (fishToCollect != null)
                {
                    Debug.Log($"Auto-collecting {fishToCollect.name} to trigger LOSE condition");
                    collectionManager.OnFishClicked(fishToCollect, activeBoard);
                    yield return new WaitForSeconds(fishCollectionDelay);
                }

                break; // This should trigger the lose condition
            }
            else
            {
                // Collect fish normally but avoid creating matches when possible
                string activeBoard = collectionManager.GetCurrentActiveBoard();
                FishController fishToCollect = FindFishForLoseStrategy(activeBoard);

                if (fishToCollect != null)
                {
                    Debug.Log($"Auto-collecting {fishToCollect.name} for LOSE strategy");
                    collectionManager.OnFishClicked(fishToCollect, activeBoard);
                    yield return new WaitForSeconds(fishCollectionDelay);
                }
                else
                {
                    Debug.LogWarning("No suitable fish found for auto-lose!");
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        Debug.Log("Auto-Lose routine completed!");
    }

    FishController FindBestFishForWin(string boardName)
    {
        // For auto-win, prioritize fish that will create matches when possible
        // This helps clear the collection bar and progress through the game

        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);

        if (availableFish.Count == 0) return null;

        // Simple strategy: just pick the first available fish
        // The match-3 system will handle clearing matches automatically
        return availableFish[0];
    }

    FishController FindWorstFishForLose(string boardName)
    {
        // For the final fish that will cause a loss, pick any fish
        // since the collection bar will be full

        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);

        if (availableFish.Count == 0) return null;

        return availableFish[0];
    }

    FishController FindFishForLoseStrategy(string boardName)
    {
        // For auto-lose, try to avoid creating matches when the collection bar is getting full
        // But still collect fish to fill up the bar

        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);

        if (availableFish.Count == 0) return null;

        // If collection bar has space, just pick any fish
        // The key is to eventually fill it up without clearing all boards
        return availableFish[Random.Range(0, availableFish.Count)];
    }

    List<FishController> GetAvailableFishOnBoard(string boardName)
    {
        List<FishController> availableFish = new List<FishController>();

        // Get board configuration
        BoardConfig config = boardGenerator.GetBoardConfig(boardName);
        if (config == null) return availableFish;

        // Search through all positions on the board
        for (int x = 0; x < config.width; x++)
        {
            for (int y = 0; y < config.height; y++)
            {
                GameObject fishObj = boardGenerator.GetFish(boardName, x, y);
                if (fishObj != null)
                {
                    FishController fishController = fishObj.GetComponent<FishController>();
                    if (fishController != null)
                    {
                        availableFish.Add(fishController);
                    }
                }
            }
        }

        return availableFish;
    }

    // Public method to stop auto-play if needed
    public void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
        isAutoPlaying = false;
        Debug.Log("Auto-play stopped");
    }

    // Check if auto-play is currently running
    public bool IsAutoPlaying()
    {
        return isAutoPlaying && !gameResultManager.IsGameEnded();
    }

    void OnDisable()
    {
        StopAutoPlay();
    }
}