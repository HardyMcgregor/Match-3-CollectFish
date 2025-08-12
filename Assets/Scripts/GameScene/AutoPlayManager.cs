using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoPlayManager : MonoBehaviour
{
    [Header("References")]
    public FishCollectionManager collectionManager;
    public CollectionBar collectionBar;
    public MultiBoardGenerator boardGenerator;
    public GameResultManager gameResultManager;

    [Header("Auto-Play Settings")]
    public float fishCollectionDelay = 0.8f;
    public float boardSwitchDelay = 1.5f;
    public float reloadDelay = 2.0f;

    private bool isAutoPlaying = false;
    private bool isAutoWin = false;
    private Coroutine autoPlayCoroutine;

    void Start()
    {
        AutoFindReferences();

        if (GameStartManager.isAutoPlay)
        {
            isAutoPlaying = true;
            isAutoWin = GameStartManager.isAutoWin;
            StartCoroutine(StartAutoPlayDelayed());
        }
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
        yield return new WaitForSeconds(1f);

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
        while (!gameResultManager.IsGameEnded())
        {
            yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());

            string activeBoard = collectionManager.GetCurrentActiveBoard();
            FishController fishToCollect = FindBestFishForWin(activeBoard);

            if (fishToCollect != null)
            {
                collectionManager.OnFishClicked(fishToCollect, activeBoard);
                yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());
                yield return new WaitForSeconds(fishCollectionDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (collectionManager.GetRemainingFishCount(activeBoard) <= 0)
            {
                yield return new WaitForSeconds(boardSwitchDelay);
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (isAutoWin && !DidPlayerWin())
        {
            yield return new WaitForSeconds(reloadDelay);
            ReloadGameScene();
        }
        else
        {
            isAutoPlaying = false;
        }
    }

    IEnumerator AutoLoseRoutine()
    {
        while (!gameResultManager.IsGameEnded())
        {
            yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());

            string activeBoard = collectionManager.GetCurrentActiveBoard();

            if (collectionBar.GetCollectedCount() >= collectionBar.maxSlots - 1)
            {
                FishController fishToCollect = FindWorstFishForLose(activeBoard);

                if (fishToCollect != null)
                {
                    collectionManager.OnFishClicked(fishToCollect, activeBoard);
                    yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());
                    yield return new WaitForSeconds(fishCollectionDelay);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                FishController fishToCollect = FindFishForLoseStrategy(activeBoard);

                if (fishToCollect != null)
                {
                    collectionManager.OnFishClicked(fishToCollect, activeBoard);
                    yield return new WaitUntil(() => !collectionManager.IsCollectionInProgress());
                    yield return new WaitForSeconds(fishCollectionDelay);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        isAutoPlaying = false;
    }

    FishController FindBestFishForWin(string boardName)
    {
        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);
        if (availableFish.Count == 0) return null;

        FishController bestMatch = FindFishThatCreatesMatch(availableFish);
        if (bestMatch != null) return bestMatch;

        FishController preferredType = FindFishOfExistingType(availableFish);
        if (preferredType != null) return preferredType;

        if (collectionBar.GetCollectedCount() >= collectionBar.maxSlots - 2)
        {
            FishController safeFish = FindSafestFish(availableFish);
            if (safeFish != null) return safeFish;
        }

        return availableFish[Random.Range(0, availableFish.Count)];
    }

    FishController FindFishThatCreatesMatch(List<FishController> availableFish)
    {
        var fishTypeCounts = new Dictionary<int, int>();

        foreach (var fish in availableFish)
        {
            if (!fishTypeCounts.ContainsKey(fish.fishType))
                fishTypeCounts[fish.fishType] = 0;
            fishTypeCounts[fish.fishType]++;
        }

        var sortedFish = availableFish.OrderByDescending(f => fishTypeCounts[f.fishType]).ToList();
        return sortedFish.FirstOrDefault();
    }

    FishController FindFishOfExistingType(List<FishController> availableFish)
    {
        return null;
    }

    FishController FindSafestFish(List<FishController> availableFish)
    {
        return availableFish[Random.Range(0, availableFish.Count)];
    }

    FishController FindWorstFishForLose(string boardName)
    {
        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);
        if (availableFish.Count == 0) return null;
        return availableFish[0];
    }

    FishController FindFishForLoseStrategy(string boardName)
    {
        List<FishController> availableFish = GetAvailableFishOnBoard(boardName);
        if (availableFish.Count == 0) return null;
        return availableFish[Random.Range(0, availableFish.Count)];
    }

    List<FishController> GetAvailableFishOnBoard(string boardName)
    {
        List<FishController> availableFish = new List<FishController>();
        BoardConfig config = boardGenerator.GetBoardConfig(boardName);
        if (config == null) return availableFish;

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

    bool DidPlayerWin()
    {
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

    void ReloadGameScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
        isAutoPlaying = false;
    }

    public bool IsAutoPlaying()
    {
        return isAutoPlaying && !gameResultManager.IsGameEnded();
    }

    void OnDisable()
    {
        StopAutoPlay();
    }
}