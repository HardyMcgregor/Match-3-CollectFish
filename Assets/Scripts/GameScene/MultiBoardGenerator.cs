using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoardConfig
{
    public string boardName;
    public Transform boardTransform;
    public int width;
    public int height;
    public float cellSize = 90f; // Size of each cell in Unity units
    public bool isActive = true; // Whether this board should be generated
    [Range(1, 10)]
    public int maxFishTypes = 3; // Maximum number of fish types for this board
}

public class MultiBoardGenerator : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject cellPrefab;
    public GameObject[] fishPrefabs; // Array of Fish1, Fish2, Fish3, etc.

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

        // Validate fish type configurations
        ValidateFishTypeConfigurations();

        GenerateAllBoards();
    }

    void AutoAssignBoardTransforms()
    {
        for (int i = 0; i < boardConfigs.Length; i++)
        {
            if (boardConfigs[i].boardTransform == null)
            {
                // Try to find the board by name in the scene
                GameObject foundBoard = GameObject.Find(boardConfigs[i].boardName);
                if (foundBoard != null)
                {
                    boardConfigs[i].boardTransform = foundBoard.transform;
                    Debug.Log($"Auto-assigned {boardConfigs[i].boardName}");
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
            // Ensure maxFishTypes is within valid range
            if (config.maxFishTypes <= 0)
            {
                config.maxFishTypes = fishPrefabs.Length; // Use all available fish types
                Debug.LogWarning($"{config.boardName} had invalid maxFishTypes, set to {config.maxFishTypes}");
            }
            else if (config.maxFishTypes > fishPrefabs.Length)
            {
                config.maxFishTypes = fishPrefabs.Length;
                Debug.LogWarning($"{config.boardName} maxFishTypes exceeded available prefabs, clamped to {config.maxFishTypes}");
            }

            Debug.Log($"{config.boardName} configured for {config.maxFishTypes} fish types");
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
        Debug.Log($"Generating {config.boardName} with {config.width}x{config.height} cells using {config.maxFishTypes} fish types");

        // Initialize arrays for this board
        GameObject[,] cells = new GameObject[config.width, config.height];
        GameObject[,] fishes = new GameObject[config.width, config.height];

        // Store references
        allCells[config.boardName] = cells;
        allFishes[config.boardName] = fishes;

        // Create cells first
        CreateCells(config, cells);

        // Then populate with fish using limited types if specified
        PopulateFish(config, cells, fishes);
    }

    void CreateCells(BoardConfig config, GameObject[,] cells)
    {
        // Calculate starting position to center the board within its 720x720 area
        Vector3 startPos = new Vector3(
            -(config.width - 1) * config.cellSize * 0.5f,
            -(config.height - 1) * config.cellSize * 0.5f,
            0f
        );

        for (int x = 0; x < config.width; x++)
        {
            for (int y = 0; y < config.height; y++)
            {
                // Calculate position for this cell
                Vector3 cellPosition = startPos + new Vector3(x * config.cellSize, y * config.cellSize, 0f);

                // Instantiate cell
                GameObject newCell = Instantiate(cellPrefab, config.boardTransform);
                newCell.transform.localPosition = cellPosition;
                newCell.name = $"{config.boardName}_Cell_{x}_{y}";

                // Store reference
                cells[x, y] = newCell;

                // Add CellController component if it doesn't exist
                if (newCell.GetComponent<CellController>() == null)
                {
                    newCell.AddComponent<CellController>();
                }

                // Set cell coordinates and board info
                CellController cellController = newCell.GetComponent<CellController>();
                cellController.SetCoordinates(x, y);
                cellController.SetBoardName(config.boardName);
            }
        }
    }

    void PopulateFish(BoardConfig config, GameObject[,] cells, GameObject[,] fishes)
    {
        // Determine available fish prefabs for this board
        GameObject[] availableFishPrefabs = GetAvailableFishPrefabs(config);

        if (availableFishPrefabs.Length == 0)
        {
            Debug.LogError($"No fish prefabs available for {config.boardName}!");
            return;
        }

        Debug.Log($"{config.boardName} using fish types: {string.Join(", ", System.Array.ConvertAll(availableFishPrefabs, f => f.name))}");

        for (int x = 0; x < config.width; x++)
        {
            for (int y = 0; y < config.height; y++)
            {
                // Get random fish prefab from available types
                GameObject randomFishPrefab = availableFishPrefabs[Random.Range(0, availableFishPrefabs.Length)];

                // Instantiate fish as child of the cell
                GameObject newFish = Instantiate(randomFishPrefab, cells[x, y].transform);
                newFish.transform.localPosition = Vector3.zero;
                newFish.name = $"{config.boardName}_Fish_{x}_{y}";

                // Store reference
                fishes[x, y] = newFish;

                // Ensure FishController is set up
                FishController fishController = newFish.GetComponent<FishController>();
                if (fishController == null)
                {
                    fishController = newFish.AddComponent<FishController>();
                }
                fishController.SetCoordinates(x, y);
                fishController.SetFishType(GetFishTypeFromPrefab(randomFishPrefab));
                fishController.SetBoardName(config.boardName);

                // Initialize the existing FishClickHandler on the prefab
                FishClickHandler clickHandler = newFish.GetComponent<FishClickHandler>();
                if (clickHandler != null)
                {
                    clickHandler.Initialize(
                        FindObjectOfType<FishCollectionManager>(),
                        fishController,
                        config.boardName
                    );
                }

                Debug.Log($"Spawned {randomFishPrefab.name} at {x},{y} on {config.boardName}");
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

        // If maxFishTypes is 0 or greater than available prefabs, use all
        if (config.maxFishTypes <= 0 || config.maxFishTypes >= fishPrefabs.Length)
        {
            return fishPrefabs;
        }

        // Return only the first N fish types
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
        // Extract fish type from prefab name (Fish1, Fish2, etc.)
        string prefabName = fishPrefab.name;
        if (prefabName.StartsWith("Fish") && prefabName.Length > 4)
        {
            string numberPart = prefabName.Substring(4);
            if (int.TryParse(numberPart, out int fishType))
            {
                return fishType;
            }
        }
        return 1; // Default to type 1 if parsing fails
    }

    // Public methods for accessing board data
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

    // Get board configuration by name
    public BoardConfig GetBoardConfig(string boardName)
    {
        foreach (BoardConfig config in boardConfigs)
        {
            if (config.boardName == boardName)
                return config;
        }
        return null;
    }

    // Enable/disable specific boards
    public void SetBoardActive(string boardName, bool active)
    {
        BoardConfig config = GetBoardConfig(boardName);
        if (config != null && config.boardTransform != null)
        {
            config.boardTransform.gameObject.SetActive(active);
        }
    }

    // Get fish types available for a specific board (useful for debugging/UI)
    public int GetFishTypesForBoard(string boardName)
    {
        BoardConfig config = GetBoardConfig(boardName);
        return config != null ? config.maxFishTypes : fishPrefabs.Length;
    }
}

// Enhanced cell controller
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

// Enhanced fish controller
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