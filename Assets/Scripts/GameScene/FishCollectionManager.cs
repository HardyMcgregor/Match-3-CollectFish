using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class FishCollectionManager : MonoBehaviour
{
    [Header("References")]
    public MultiBoardGenerator boardGenerator;
    public CollectionBar collectionBar;
    public GameResultManager gameResultManager; // Add reference to the result manager

    [Header("Animation Settings")]
    public float moveAnimationDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Board Order (Smallest to Largest Z-layer)")]
    public string[] boardOrder = { "Board_3", "Board_2", "Board_1" };

    private Dictionary<string, int> boardFishCount = new Dictionary<string, int>();
    private string currentActiveBoard = "Board_3";
    private bool isAnimating = false;

    void Start()
    {
        InitializeBoardCounts();
        SetupInitialBoardState();

        // Auto-find GameResultManager if not assigned
        if (gameResultManager == null)
            gameResultManager = FindObjectOfType<GameResultManager>();
    }

    void InitializeBoardCounts()
    {
        foreach (string boardName in boardOrder)
        {
            BoardConfig config = boardGenerator.GetBoardConfig(boardName);
            if (config != null)
            {
                boardFishCount[boardName] = config.width * config.height;
            }
        }
    }

    void SetupInitialBoardState()
    {
        foreach (string boardName in boardOrder)
        {
            bool shouldShow = (boardName == currentActiveBoard);
            boardGenerator.SetBoardActive(boardName, shouldShow);
        }

        Debug.Log($"Started with {currentActiveBoard} active. Fish count: {boardFishCount[currentActiveBoard]}");
    }

    public void OnFishClicked(FishController fish, string boardName)
    {
        Debug.Log($"OnFishClicked called for {fish.name} on {boardName}");

        // Prevent clicking during any animations (movement or destruction)
        if (boardName != currentActiveBoard || isAnimating || collectionBar.IsAnimating())
        {
            Debug.Log("Cannot collect fish - animation in progress or wrong board");
            return;
        }

        // Check if game has ended
        if (gameResultManager != null && gameResultManager.IsGameEnded())
        {
            Debug.Log("Cannot collect fish - game has ended");
            return;
        }

        if (collectionBar.CanAddFish())
        {
            StartCoroutine(MoveFishToCollection(fish, boardName));
        }
        else
        {
            Debug.Log("Collection bar is full!");
            // The GameResultManager will automatically detect this lose condition
        }
    }

    System.Collections.IEnumerator MoveFishToCollection(FishController fish, string boardName)
    {
        isAnimating = true;

        // Get the target slot
        Transform targetSlot = collectionBar.collectionSlots[collectionBar.GetCollectedCount()];

        // Store original world positions for smooth movement
        Vector3 startWorldPos = fish.transform.position;
        Vector3 targetWorldPos = targetSlot.position;

        float elapsed = 0f;
        while (elapsed < moveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveAnimationDuration);

            // Move directly from start position to target slot position in world space
            fish.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            yield return null;
        }

        // Ensure final position is exact
        fish.transform.position = targetWorldPos;

        // Add to collection bar using existing method (pass the target slot)
        collectionBar.AddFish(fish, targetSlot);

        boardFishCount[boardName]--;

        // Wait for any destruction animations to complete before proceeding
        while (collectionBar.IsAnimating())
        {
            yield return null;
        }

        if (boardFishCount[boardName] <= 0)
            yield return StartCoroutine(HandleBoardComplete(boardName));

        isAnimating = false;
    }

    System.Collections.IEnumerator HandleBoardComplete(string completedBoard)
    {
        Debug.Log($"{completedBoard} completed! Moving to next board.");

        boardGenerator.SetBoardActive(completedBoard, false);
        yield return new WaitForSeconds(0.3f);

        string nextBoard = GetNextBoard(completedBoard);
        if (nextBoard != null)
        {
            currentActiveBoard = nextBoard;
            foreach (string boardName in boardOrder)
            {
                bool shouldShow = (boardName == currentActiveBoard);
                boardGenerator.SetBoardActive(boardName, shouldShow);
            }
            Debug.Log($"Activated {currentActiveBoard}. Fish count: {boardFishCount[currentActiveBoard]}");
        }
        else
        {
            Debug.Log("All boards completed! Game finished!");
            OnGameComplete();
        }
    }

    string GetNextBoard(string currentBoard)
    {
        int currentIndex = System.Array.IndexOf(boardOrder, currentBoard);
        if (currentIndex >= 0 && currentIndex < boardOrder.Length - 1)
        {
            return boardOrder[currentIndex + 1];
        }
        return null;
    }

    void OnGameComplete()
    {
        Debug.Log("Congratulations! All fish collected!");

        // The GameResultManager will automatically detect the win condition
        // No need to manually trigger it here since it monitors the game state
    }

    public string GetCurrentActiveBoard()
    {
        return currentActiveBoard;
    }

    public int GetRemainingFishCount(string boardName)
    {
        return boardFishCount.ContainsKey(boardName) ? boardFishCount[boardName] : 0;
    }

    public bool IsCollectionInProgress()
    {
        return isAnimating || collectionBar.IsAnimating();
    }
}