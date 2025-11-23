using UnityEngine;
using Egglers;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GridManager : MonoBehaviour
{
    // ---------------- Singleton ----------------
    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GridManager: Another instance exists! Destroying this one.");
#if UNITY_EDITOR
            DestroyImmediate(this.gameObject);
#else
            Destroy(this.gameObject);
#endif
            return;
        }
        Instance = this;
    }

    // ---------------- References ----------------

    [SerializeField] private PlantBitManager plantBitManager;
    [SerializeField] private PollutionManager pollutionManager;
    [SerializeField] private GameManager gameManager;

    // ---------------- Grid Settings ----------------
    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float tileSize = 1f;
    public float spacing = 0.05f;

    [Header("References")]
    public GameObject tilePrefab; // assign in inspector

    public GameObject[,] tiles;

    // ---------------- Grid Generation ----------------
#if UNITY_EDITOR
    [ContextMenu("Generate Grid")]
#endif
    public void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("GridManager: Tile prefab is not assigned!");
            return;
        }

        ClearGrid();

        tiles = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float posX = x * (tileSize + spacing);
                float posZ = y * (tileSize + spacing);
                Vector3 position = new Vector3(posX, 0, posZ);

#if UNITY_EDITOR
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, transform);
                tile.transform.localPosition = position;
#else
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
#endif
                tile.name = $"Tile ({x},{y})";
                tiles[x, y] = tile;

                GridVisualTile visualTile = tile.GetComponent<GridVisualTile>();
                if (visualTile != null)
                {
                    visualTile.coords = new Vector2Int(x, y);
                    visualTile.plantBitManager = plantBitManager;
                    visualTile.pollutionManager = pollutionManager;
                    visualTile.gameManager = gameManager;
                }

                TileActionBinder binder = tile.GetComponent<TileActionBinder>();
                if (binder != null)
                {
                    binder.plantBitManager = plantBitManager;
                    binder.pollutionManager = pollutionManager;
                    binder.gameManager = gameManager;
                }
            }
        }

        // Center the grid
        float totalWidth = (width - 1) * (tileSize + spacing);
        float totalHeight = (height - 1) * (tileSize + spacing);
        transform.localPosition = new Vector3(-totalWidth / 2f, 0, -totalHeight / 2f);
    }



#if UNITY_EDITOR
    [ContextMenu("Clear Grid")]
#endif
    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    // Optional: accessor for a tile
    public GameObject GetTile(int x, int y)
    {
        if (tiles == null || x < 0 || y < 0 || x >= width || y >= height)
            return null;
        return tiles[x, y];
    }
}
