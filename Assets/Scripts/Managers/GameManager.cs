using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Managers")]
        public PlantManager plantManager;
        public PollutionManager pollutionManager;
        public GridManager gridManager;
        public GridSystem gridSystem;

        [Header("Game State")]
        public GameState gameState = GameState.HeartPlacement;

        [Header("Configuration")]
        public int gridWidth = 10;
        public int gridHeight = 10;
        public int heartStartLeaf = 3;
        public int heartStartRoot = 3;
        public int heartStartFruit = 3;

        [Header("Tick Rates")]
        public float plantTickRate = 0.5f;

        [Header("Pollution Sources Setup")]
        public List<SourceSetup> sourcesSetup = new List<SourceSetup>();

        // UI Events
        public System.Action OnGameWon;
        public System.Action OnGameLost;
        public System.Action<string> OnErrorMessage;

        private Coroutine plantTickCoroutine;

        [System.Serializable]
        public class SourceSetup
        {
            public Vector2Int position;
            public PollutionType pollutionType;
            public SourceTier tier;
            public float hp;
            public float emissionRate;
            public float tickInterval;
            public float dormantDuration;
        }
        void Start()
        {
            GridManager.Instance.GenerateGrid();
            InitializeGame();
        }
        void Awake()
        {
            // Initialize grid system
            if (gridSystem == null)
            {
                gridSystem = new GridSystem();
            }

            gridSystem.Initialize(gridWidth, gridHeight);

            // Set references
            if (plantManager != null)
            {
                plantManager.gridSystem = gridSystem;
                plantManager.pollutionManager = pollutionManager;
            }

            if (pollutionManager != null)
            {
                pollutionManager.gridSystem = gridSystem;
                pollutionManager.plantManager = plantManager;
                pollutionManager.CreateGrid(gridWidth, gridHeight); // Initialize pollution grid
            }
        }
        void Update()
        {
            // for (int x = 0; x < GridManager.Instance.width; x++)
            // {
            //     for (int y = 0; y < GridManager.Instance.height; y++)
            //     {
            //         GameObject tile = GridManager.Instance.GetTile(x, y);
            //         if (tile != null)
            //         {
            //             TileActions data = tile.GetComponent<TileActions>();
            //             data?.InvokeAction("billboard");
            //         }
            //     }
            // }
        }
        public void InitializeGame()
        {
            // Place pollution sources (predetermined positions)
            foreach (SourceSetup sourceSetup in sourcesSetup)
            {
                pollutionManager.CreateSource(
                    sourceSetup.position,
                    sourceSetup.pollutionType,
                    sourceSetup.tier,
                    sourceSetup.hp,
                    sourceSetup.emissionRate,
                    sourceSetup.tickInterval,
                    sourceSetup.dormantDuration
                );
            }
            
            // Start pollution source coroutines (each source pulses at its own rate)
            pollutionManager.StartAllSourcePulses();

            // Start heart placement mode
            gameState = GameState.HeartPlacement;
            Debug.Log("Place the Heart to begin the game!");
        }

        public bool IsValidHeartPlacement(Vector2Int pos)
        {
            return gridSystem.IsInBounds(pos) &&
                   gridSystem.GetTileState(pos) == TileState.Empty;
        }

        public void OnPlayerPlacesHeart(Vector2Int pos)
        {
            if (!IsValidHeartPlacement(pos))
            {
                OnErrorMessage?.Invoke("Cannot place Heart here!");
                Debug.LogWarning("Invalid Heart placement position");
                return;
            }

            // Initialize Heart
            plantManager.InitializeHeart(pos, heartStartLeaf, heartStartRoot, heartStartFruit);

            // Start game loops
            StartGameLoops();

            gameState = GameState.Playing;
            Debug.Log("Game started!");
        }

        private void StartGameLoops()
        {
            // Start plant tick coroutine
            if (plantTickCoroutine != null)
            {
                StopCoroutine(plantTickCoroutine);
            }
            plantTickCoroutine = StartCoroutine(PlantTickCoroutine());
        }

        private IEnumerator PlantTickCoroutine()
        {
            while (gameState == GameState.Playing)
            {
                plantManager.UpdatePlants();
                CheckWinCondition();

                yield return new WaitForSeconds(plantTickRate);
            }
        }

        public void CheckWinCondition()
        {
            if (pollutionManager.activeSources.Count == 0)
            {
                TriggerWin();
            }
        }

        public void TriggerWin()
        {
            if (gameState != GameState.Won)
            {
                gameState = GameState.Won;
                StopGameLoops();
                OnGameWon?.Invoke();
                Debug.Log("Victory! All pollution sources destroyed!");
            }
        }

        public void TriggerLoss()
        {
            if (gameState != GameState.Lost)
            {
                gameState = GameState.Lost;
                StopGameLoops();
                OnGameLost?.Invoke();
                Debug.Log("Defeat! The Heart has been overwhelmed by pollution!");
            }
        }

        private void StopGameLoops()
        {
            if (plantTickCoroutine != null)
            {
                StopCoroutine(plantTickCoroutine);
                plantTickCoroutine = null;
            }
        }

        public void PauseGame()
        {
            if (gameState == GameState.Playing)
            {
                gameState = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (gameState == GameState.Paused)
            {
                gameState = GameState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void RestartGame()
        {
            // Stop coroutines
            StopGameLoops();

            // Clear all data
            plantManager.allPlants.Clear();
            plantManager.heartPlant = null;
            
            // Clear pollution data (uses ResetPollutionSystem which clears grid, lists, and dictionaries)
            pollutionManager.ResetPollutionSystem();

            // Reinitialize grids
            gridSystem.Initialize(gridWidth, gridHeight);
            pollutionManager.CreateGrid(gridWidth, gridHeight);
            
            InitializeGame();
        }

        // Public methods for manual player actions
        public void PlayerManualSprout(Vector2Int plantPos, Vector2Int targetPos)
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            Plant plant = gridSystem.GetEntity<Plant>(plantPos);
            if (plant != null)
            {
                plantManager.ManualSprout(plant, targetPos);
            }
        }

        public void PlayerPrune(Vector2Int plantPos)
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            Plant plant = gridSystem.GetEntity<Plant>(plantPos);
            if (plant != null && !plant.isHeart)
            {
                plantManager.DeletePlant(plant, fromPollution: false);
            }
        }

        public void PlayerRemoveGrafts(Vector2Int plantPos, int leaf, int root, int fruit)
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            Plant plant = gridSystem.GetEntity<Plant>(plantPos);
            if (plant != null)
            {
                plantManager.RemoveGrafts(plant, leaf, root, fruit);
            }
        }

        public void PlayerApplyGrafts(Vector2Int plantPos)
        {
            if (gameState != GameState.Playing)
            {
                return;
            }

            Plant plant = gridSystem.GetEntity<Plant>(plantPos);
            if (plant != null)
            {
                plantManager.ApplyGrafts(plant);
            }
        }
    }
}

