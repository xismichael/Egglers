using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public class GameManager : MonoBehaviour
    {
        public GridSystem gameGrid;

        [Header("Managers")]
        public PlantBitManager plantManager;
        public PollutionManager pollutionManager;

        [Header("Game State")]
        public GameState gameState = GameState.HeartPlacement;

        [Header("Configuration")]
        public int gridWidth = 10;
        public int gridHeight = 10;

        [Header("Heart Start Components")]
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
            [Header("Pollution stats")]
            public float spreadRate;
            public float strength;
            public float resistance;
            public float pulseRate;
            public float dormantDuration;
        }
        void Awake()
        {
            // Ensure the GridSystem exists
            if (gameGrid == null)
            {
                gameGrid = new GridSystem();
            }

            // Ensure the PollutionManager has a reference to the grid
            if (pollutionManager != null)
            {
                pollutionManager.Initialize(gameGrid);
                pollutionManager.plantManager = plantManager;
            }
            // Ensure the PollutionManager has a reference to the grid
            if (plantManager != null)
            {
                plantManager.Initialize(gameGrid);
                plantManager.pollutionManager = pollutionManager;
            }
        }
        void Start()
        {
            gameGrid.Initialize(gridWidth, gridHeight);
            InitializeGame();
        }

        public void InitializeGame()
        {
            // Place pollution sources (predetermined positions)
            foreach (SourceSetup sourceSetup in sourcesSetup)
            {
                pollutionManager.AddPollutionSource(
                    sourceSetup.position,
                    sourceSetup.spreadRate,
                    sourceSetup.strength,
                    sourceSetup.resistance,
                    sourceSetup.pulseRate,
                    sourceSetup.dormantDuration
                );
            }

            // Start heart placement mode
            gameState = GameState.HeartPlacement;
            Debug.Log("Place the Heart to begin the game!");
        }

        public bool IsValidHeartPlacement(Vector2Int pos)
        {
            // Check the unified grid for a plant at this position
            PlantBit existingPlant = gameGrid.GetEntity<PlantBit>(pos);
            return existingPlant == null;
        }

        public void OnPlayerPlacesHeart(Vector2Int pos)
        {
            if (!IsValidHeartPlacement(pos))
            {
                Debug.LogWarning("Invalid Heart placement position");
                return;
            }


            plantManager.InitializeHeart(pos);
            gameState = GameState.Playing;

            StartGameLoops();
            // Start pollution source coroutines (each source pulses at its own rate)
            pollutionManager.StartAllSourcePulses();
            Debug.Log("Game started!");
        }

        private void StartGameLoops()
        {
            if (plantTickCoroutine != null)
            {
                StopCoroutine(plantTickCoroutine);
            }
            // Debug.Log("Game LOOPING!");

            plantTickCoroutine = StartCoroutine(PlantTickCoroutine());
        }

        private IEnumerator PlantTickCoroutine()
        {
            while (gameState == GameState.Playing)
            {
                // Debug.Log("[Tick] PlantTickCoroutine running");

                plantManager.UpdatePlants(plantManager.heart);
                CheckWinCondition();
                yield return new WaitForSeconds(plantTickRate);
            }
        }

        public void CheckWinCondition()
        {
            Debug.Log("CheckWinCondition: " + pollutionManager.pollutionSources.Count);
            if (pollutionManager.pollutionSources.Count == 0)
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
                Debug.Log("Victory! All pollution sources destroyed!");
            }
        }

        public void TriggerLoss()
        {
            if (gameState != GameState.Lost)
            {
                gameState = GameState.Lost;
                StopGameLoops();
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

        // Player actions
        public void PlayerManualSprout(Vector2Int parentPos, Vector2Int targetPos)
        {
            if (gameState != GameState.Playing) return;

            PlantBit parent = gameGrid.GetEntity<PlantBit>(parentPos);
            if (parent != null)
            {
                plantManager.CreateSprout(parent, targetPos);
            }
        }

        public void PlayerPrune(Vector2Int pos)
        {
            if (gameState != GameState.Playing) return;

            PlantBit plant = gameGrid.GetEntity<PlantBit>(pos);
            if (plant != null && plant != plantManager.heart)
            {
                plantManager.KillPlantBit(plant);
            }
        }

        public void PlayerApplyGrafts(Vector2Int pos)
        {
            if (gameState != GameState.Playing) return;
            plantManager.ApplyGraftAtPosition(pos);
        }

        public void PlayerRemoveGrafts(Vector2Int pos, int leaf, int root, int fruit)
        {
            if (gameState != GameState.Playing) return;
            plantManager.RemoveGraftAtPosition(pos, leaf, root, fruit);
        }
    }
}
