using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoardConfig
{
    public string boardName;
    public Transform boardTransform;
    public int width;
    public int height;
    public float cellSize = 90f;
    public bool isActive = true;
    [Range(1, 10)]
    public int maxFishTypes = 3;
}

public class MultiBoardGenerator : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject cellPrefab;
    public GameObject[] fishPrefabs;

    [Header("Board Configurations")]
    public BoardConfig[] boardConfigs = new BoardConfig[]
    {
        new BoardConfig { boardName = "Board_1", width = 8, height = 8, cellSize = 90f, isActive = true, maxFishTypes = 5 },
        new BoardConfig { boardName = "Board_2", width = 6, height = 6, cellSize = 90f, isActive = true, maxFishTypes = 4 },
        new BoardConfig { boardName = "Board_3", width = 3, height = 3, cellSize = 90f, isActive = true, maxFishTypes = 2 },
    };

    [Header("Auto-Find Boards")]
    public bool autoFindBoards = true;

    private Dictionary<string, GameObject[,]> allCells = new Dictionary<string, GameObject[,]>();
    private Dictionary<string, GameObject[,]> allFishes = new Dictionary<string, GameObject[,]>();

    void Start()
    {
        if (autoFindBoards)
        {
            AutoAssignBoardTransforms();
        }

        ValidateFishTypeConfigurations();
        GenerateAllBoards();
    }

    void AutoAssignBoardTransforms()
    {
        for (int i = 0; i < boardConfigs.Length; i++)
        {
            if (boardConfigs[i].boardTransform == null)
            {
                GameObject foundBoard = GameObject.Find(boardConfigs[i].boardName);
                if (foundBoard != null)
                {
                    boardConfigs[i].boardTransform = foundBoard.transform;
                }
                else
                {
                    Debug.LogWarning($"Could not find {boardConfigs[i].boardName} in the scene!");
                    boardConfigs[i].isActive = false;
                }
            }
        }
    }

    void ValidateFishTypeConfigurations()
    {
        foreach (BoardConfig config in boardConfigs)
        {
            if (config.maxFishTypes <= 0)
            {
                config.maxFishTypes = fishPrefabs.Length;
            }
            else if (config.maxFishTypes > fishPrefabs.Length)
            {
                config.maxFishTypes = fishPrefabs.Length;
            }
        }
    }

    void GenerateAllBoards()
    {
        foreach (BoardConfig config in boardConfigs)
        {
            if (config.isActive && config.boardTransform != null)
            {
                GenerateBoard(config);
            }
        }
    }

    void GenerateBoard(BoardConfig config)
    {
        GameObject[,] cells = new GameObject[config.width, config.height];
        GameObject[,] fishes = new GameObject[config.width, config.height];

        allCells[config.boardName] = cells;
        allFishes[config.boardName] = fishes;

        CreateCells(config, cells);
        PopulateFish(config, cells, fishes);
    }

    void CreateCells(BoardConfig config, GameObject[,] cells)
    {
        Vector3 startPos = new Vector3(
            -(config.width - 1) * config.cellSize * 0.5f,
            -(config.height - 1) * config.cellSize * 0.5f,
            0f
        );

        for (int x = 0; x < config.width; x++)
        {
            for (int y = 0; y < config.height; y++)
            {
                Vector3 cellPosition = startPos + new Vector3(x * config.cellSize, y * config.cellSize, 0f);

                GameObject newCell = Instantiate(cellPrefab, config.boardTransform);
                newCell.transform.localPosition = cellPosition;
                newCell.name = $"{config.boardName}_Cell_{x}_{y}";

                cells[x, y] = newCell;

                if (newCell.GetComponent<CellController>() == null)
                {
                    newCell.AddComponent<CellController>();
                }

                CellController cellController = newCell.GetComponent<CellController>();
                cellController.SetCoordinates(x, y);
                cellController.SetBoardName(config.boardName);
            }
        }
    }

    void PopulateFish(BoardConfig config, GameObject[,] cells, GameObject[,] fishes)
    {
        GameObject[] availableFishPrefabs = GetAvailableFishPrefabs(config);

        if (availableFishPrefabs.Length == 0)
        {
            Debug.LogError($"No fish prefabs available for {config.boardName}!");
            return;
        }

        for (int x = 0; x < config.width; x++)
        {
            for (int y = 0; y < config.height; y++)
            {
                GameObject randomFishPrefab = availableFishPrefabs[Random.Range(0, availableFishPrefabs.Length)];

                GameObject newFish = Instantiate(randomFishPrefab, cells[x, y].transform);
                newFish.transform.localPosition = Vector3.zero;
                newFish.name = $"{config.boardName}_Fish_{x}_{y}";

                fishes[x, y] = newFish;

                FishController fishController = newFish.GetComponent<FishController>();
                if (fishController == null)
                {
                    fishController = newFish.AddComponent<FishController>();
                }
                fishController.SetCoordinates(x, y);
                fishController.SetFishType(GetFishTypeFromPrefab(randomFishPrefab));
                fishController.SetBoardName(config.boardName);

                FishClickHandler clickHandler = newFish.GetComponent<FishClickHandler>();
                if (clickHandler != null)
                {
                    clickHandler.Initialize(
                        FindObjectOfType<FishCollectionManager>(),
                        fishController,
                        config.boardName
                    );
                }
            }
        }
    }

    GameObject[] GetAvailableFishPrefabs(BoardConfig config)
    {
        if (fishPrefabs.Length == 0)
        {
            Debug.LogError("No fish prefabs assigned!");
            return new GameObject[0];
        }

        if (config.maxFishTypes <= 0 || config.maxFishTypes >= fishPrefabs.Length)
        {
            return fishPrefabs;
        }

        GameObject[] limitedFish = new GameObject[config.maxFishTypes];
        for (int i = 0; i < config.maxFishTypes; i++)
        {
            limitedFish[i] = fishPrefabs[i];
        }

        return limitedFish;
    }

    GameObject GetRandomFishPrefab()
    {
        if (fishPrefabs.Length == 0)
        {
            Debug.LogError("No fish prefabs assigned!");
            return null;
        }

        int randomIndex = Random.Range(0, fishPrefabs.Length);
        return fishPrefabs[randomIndex];
    }

    int GetFishTypeFromPrefab(GameObject fishPrefab)
    {
        string prefabName = fishPrefab.name;
        if (prefabName.StartsWith("Fish") && prefabName.Length > 4)
        {
            string numberPart = prefabName.Substring(4);
            if (int.TryParse(numberPart, out int fishType))
            {
                return fishType;
            }
        }
        return 1;
    }

    public GameObject GetCell(string boardName, int x, int y)
    {
        if (allCells.ContainsKey(boardName))
        {
            GameObject[,] cells = allCells[boardName];
            if (IsValidPosition(cells, x, y))
                return cells[x, y];
        }
        return null;
    }

    public GameObject GetFish(string boardName, int x, int y)
    {
        if (allFishes.ContainsKey(boardName))
        {
            GameObject[,] fishes = allFishes[boardName];
            if (IsValidPosition(fishes, x, y))
                return fishes[x, y];
        }
        return null;
    }

    public void SetFish(string boardName, int x, int y, GameObject fish)
    {
        if (allFishes.ContainsKey(boardName))
        {
            GameObject[,] fishes = allFishes[boardName];
            if (IsValidPosition(fishes, x, y))
                fishes[x, y] = fish;
        }
    }

    bool IsValidPosition(GameObject[,] array, int x, int y)
    {
        return x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);
    }

    public BoardConfig GetBoardConfig(string boardName)
    {
        foreach (BoardConfig config in boardConfigs)
        {
            if (config.boardName == boardName)
                return config;
        }
        return null;
    }

    public void SetBoardActive(string boardName, bool active)
    {
        BoardConfig config = GetBoardConfig(boardName);
        if (config != null && config.boardTransform != null)
        {
            config.boardTransform.gameObject.SetActive(active);
        }
    }

    public int GetFishTypesForBoard(string boardName)
    {
        BoardConfig config = GetBoardConfig(boardName);
        return config != null ? config.maxFishTypes : fishPrefabs.Length;
    }
}

public class CellController : MonoBehaviour
{
    public int x, y;
    public string boardName;

    public void SetCoordinates(int newX, int newY)
    {
        x = newX;
        y = newY;
    }

    public void SetBoardName(string name)
    {
        boardName = name;
    }
}

public class FishController : MonoBehaviour
{
    public int x, y;
    public int fishType;
    public string boardName;

    public void SetCoordinates(int newX, int newY)
    {
        x = newX;
        y = newY;
    }

    public void SetFishType(int type)
    {
        fishType = type;
    }

    public void SetBoardName(string name)
    {
        boardName = name;
    }
}