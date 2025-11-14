using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PlantPollutionGame
{
    public class PollutionVisualizer : MonoBehaviour
    {
        [Header("References")]
        public GameObject tilePrefab; // 50x50 prefab with Text child
        public Transform gridParent; // Parent transform (Canvas)
        
        [Header("Grid Settings")]
        public int gridWidth = 15;
        public int gridHeight = 15;
        public float tileSize = 50f;
        public float tileSpacing = 2f;
        
        [Header("Test Pollution Source Settings")]
        public float testSpreadRate = 10f;
        public float testStrength = 15f;
        public float testResistance = 5f;
        public float testPulseRate = 4f;
        public float testDormantDuration = 0f;
        
        // Store references to text components
        private TextMeshProUGUI[,] tileTexts;
        private bool isVisualsCreated = false;
        
        public void InitializeGridVisuals()
        {
            // Clear old visuals if they exist
            if (isVisualsCreated)
            {
                ClearGridVisuals();
            }
            
            // Create grid in PollutionManager
            PollutionManager.Instance.CreateGrid(gridWidth, gridHeight);
            
            // Initialize text array
            tileTexts = new TextMeshProUGUI[gridWidth, gridHeight];
            
            float totalSize = tileSize + tileSpacing;
            
            // Create visual tiles
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Instantiate prefab
                    GameObject tile = Instantiate(tilePrefab, gridParent);
                    
                    // Position it
                    RectTransform rectTransform = tile.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(x * totalSize, y * totalSize);
                    
                    // Get text component (assumes it's on the prefab or a child)
                    TextMeshProUGUI textComponent = tile.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        tileTexts[x, y] = textComponent;
                        textComponent.text = "0";
                    }
                    else
                    {
                        Debug.LogWarning($"No TextMeshProUGUI found on tile prefab at position ({x}, {y})");
                    }
                }
            }
            
            isVisualsCreated = true;
            Debug.Log($"Grid visuals created: {gridWidth}x{gridHeight}");
        }
        
        public void UpdateGridVisuals()
        {
            if (!isVisualsCreated)
            {
                Debug.LogWarning("Visuals not created yet. Call InitializeGridVisuals first.");
                return;
            }
            
            // Update text for each tile
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (tileTexts[x, y] != null)
                    {
                        float pollution = GetPollutionAt(x, y);
                        tileTexts[x, y].text = pollution.ToString("F1");
                    }
                }
            }
        }
        
        private float GetPollutionAt(int x, int y)
        {
            Vector2Int pos = new Vector2Int(x, y);
            object gridObject = PollutionManager.Instance.grid[x, y];
            
            if (gridObject is PollutionTile tile)
            {
                return tile.GetTotalPollution();
            }
            else if (gridObject is PollutionSource source)
            {
                return source.GetTotalPollution();
            }
            
            return 0f;
        }
        
        public void AddTestPollutionSource(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                Debug.LogWarning($"Position ({x}, {y}) is out of bounds!");
                return;
            }
            
            // Add source to PollutionManager
            Vector2Int position = new Vector2Int(x, y);
            PollutionManager.Instance.AddPollutionSource(
                position,
                testSpreadRate,
                testStrength,
                testResistance,
                testPulseRate,
                testDormantDuration
            );
            
            Debug.Log($"Pollution source added at ({x}, {y})");
        }
        
        public void AddDefaultSources()
        {
            // Add two test sources at predetermined positions (for 15x15 grid)
            AddTestPollutionSource(3, 3);
            AddTestPollutionSource(11, 11);
            Debug.Log("Default pollution sources added!");
        }
        
        public void StartAllSources()
        {
            PollutionManager.Instance.StartAllSourcePulses();
            Debug.Log("All pollution sources started!");
        }
        
        public void ResetEverything()
        {
            // Reset pollution system
            PollutionManager.Instance.ResetPollutionSystem();
            
            // Clear visuals
            ClearGridVisuals();
            
            Debug.Log("Everything reset!");
        }
        
        public void ClearGridVisuals()
        {
            if (gridParent != null)
            {
                // Destroy all children
                foreach (Transform child in gridParent)
                {
                    Destroy(child.gameObject);
                }
            }
            
            tileTexts = null;
            isVisualsCreated = false;
            Debug.Log("Grid visuals cleared");
        }
        
        void Update()
        {
            // Auto-update visuals every frame if created
            if (isVisualsCreated)
            {
                UpdateGridVisuals();
            }
        }
    }
}

