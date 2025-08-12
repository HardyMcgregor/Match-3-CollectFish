using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class FishCollectionManager : MonoBehaviour
{
    [Header("References")]
    public MultiBoardGenerator boardGenerator;
    public CollectionBar collectionBar;
    public GameResultManager gameResultManager;

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
    }

    public void OnFishClicked(FishController fish, string boardName)
    {
        if (boardName != currentActiveBoard || isAnimating || collectionBar.IsAnimating())
        {
            return;
        }

        if (gameResultManager != null && gameResultManager.IsGameEnded())
        {
            return;
        }

        if (collectionBar.CanAddFish())
        {
            StartCoroutine(MoveFishToCollection(fish, boardName));
        }
    }

    System.Collections.IEnumerator MoveFishToCollection(FishController fish, string boardName)
    {
        isAnimating = true;

        int fx = fish.x;
        int fy = fish.y;
        if (boardGenerator != null)
        {
            boardGenerator.SetFish(boardName, fx, fy, null);
        }

        FishClickHandler click = fish.GetComponent<FishClickHandler>();
        if (click != null)
            click.enabled = false;

        fish.SetBoardName("Collected");

        Transform targetSlot = collectionBar.collectionSlots[collectionBar.GetCollectedCount()];

        Vector3 startWorldPos = fish.transform.position;
        Vector3 targetWorldPos = targetSlot.position;

        float elapsed = 0f;
        while (elapsed < moveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveAnimationDuration);

            fish.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            yield return null;
        }

        fish.transform.position = targetWorldPos;

        collectionBar.AddFish(fish, targetSlot);

        boardFishCount[boardName]--;

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
        }
        else
        {
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
        // GameResultManager will automatically detect the win condition
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