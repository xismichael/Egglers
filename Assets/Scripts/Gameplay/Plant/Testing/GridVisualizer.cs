using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Egglers
{
    public class GridVisualizer : MonoBehaviour
    {
        public static GridVisualizer Instance { get; private set; }

        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private PlantBitManager manager;

        [SerializeField] private Button tickButton;
        [SerializeField] private Button removeGraftButton;
        [SerializeField] private Button applyGraftButton;

        [SerializeField] private Slider graftLeafSlider;
        [SerializeField] private Slider graftRootSlider;
        [SerializeField] private Slider graftFruitSlider;
        
        
        [SerializeField] private TMP_Text graftLeafText;
        [SerializeField] private TMP_Text graftRootText;
        [SerializeField] private TMP_Text graftFruitText;
        


        [SerializeField] private TMP_Text descriptionText;

        private GridLayoutGroup gridLayoutGroup;
        private GameObject[,] gridArray;

        private PlantBit displayPlantBit = null;

        void Awake()
        {

            // If there is an instance, and it's not me, delete myself.
    
            if (Instance != null && Instance != this) 
            { 
                Destroy(this); 
            } 
            else 
            { 
                Instance = this; 
            }

            tickButton.onClick.AddListener(OnTickClicked);
            removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
            applyGraftButton.onClick.AddListener(OnApplyGraftClicked);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();

            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = manager.gridWidth;

            gridArray = new GameObject[manager.gridWidth, manager.gridHeight];

            for (int x = 0; x < manager.gridWidth; x++)
            {
                for (int y = 0; y < manager.gridHeight; y++)
                {
                    gridArray[x, y] = Instantiate(tilePrefab, transform);
                    gridArray[x, y].GetComponent<VisualTile>().position = new Vector2Int(x, y);
                }
            }
        }

        private void OnTickClicked()
        {
            manager.UpdatePlants(manager.heart);
            UpdateGrid();
        }

        private void OnRemoveGraftClicked()
        {
            if (displayPlantBit == null) return;
            manager.RemoveGraftAtPosition(displayPlantBit.position, (int) graftLeafSlider.value, (int) graftRootSlider.value, (int) graftFruitSlider.value);
        }

        private void OnApplyGraftClicked()
        {
            if (displayPlantBit == null) return;
            manager.ApplyGraftAtPosition(displayPlantBit.position);
        }

        void UpdateGrid()
        {
            for (int x = 0; x < manager.gridWidth; x++)
            {
                for (int y = 0; y < manager.gridHeight; y++)
                {
                    GameTile tile = manager.gameGrid.GetTileAtPosition(new Vector2Int(x, y));
                    if (tile == null) continue;

                    string text = "-";

                    PlantBit plantBit = tile.GetPlantBit();

                    if (plantBit != null)
                    {
                        if (plantBit.phase == PlantBitPhase.Bud) text = "+";
                        else if (plantBit.phase == PlantBitPhase.Grown) text = "( )";
                    }

                    gridArray[x, y].GetComponentInChildren<TMP_Text>().text = text;

                }
            }

            ShowPlantBitInfo();
        }

        void Update()
        {
            graftLeafText.text = $"Leaf: {graftLeafSlider.value}";
            graftRootText.text = $"Root: {graftRootSlider.value}";
            graftFruitText.text = $"Fruit: {graftFruitSlider.value}";
        }

        void OnDestroy()
        {
            tickButton.onClick.RemoveListener(OnTickClicked);
            removeGraftButton.onClick.RemoveListener(OnRemoveGraftClicked);
            applyGraftButton.onClick.RemoveListener(OnApplyGraftClicked);
        }

        public void SetDisplayPlantBit(Vector2Int pos)
        {

            Debug.Log("Attempting to Set bit");
            GameTile tile = manager.gameGrid.GetTileAtPosition(pos);
            if (tile == null) return;

            PlantBit plantBit = tile.GetPlantBit();
            if (plantBit == null) return;

            displayPlantBit = plantBit;

            ShowPlantBitInfo();
        }

        public void ShowPlantBitInfo()
        {
            if (displayPlantBit == null) return;

            Debug.Log("Showing plant bit");
            descriptionText.text = $"Position: {displayPlantBit.position}\nTotal Components: {displayPlantBit.TotalComponents}\n" +
                $"Leafs: {displayPlantBit.leafCount + displayPlantBit.graftedLeafCount}\nRoots: {displayPlantBit.rootCount + displayPlantBit.graftedRootCount}\nFruits: {displayPlantBit.fruitCount + displayPlantBit.graftedFruitCount}\n" +
                $"Attack Dmg: {displayPlantBit.attackDamage}\nExtraction Rate: {displayPlantBit.extractionRate}\nStorage: {displayPlantBit.energyStorage}\n";
        }
    }
}

