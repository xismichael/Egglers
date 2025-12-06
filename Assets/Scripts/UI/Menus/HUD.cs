using TMPro;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// HUD displays key game stats: energy, graft buffer, and pollution sources
    /// </summary>
    public class HUD : GameMenu
    {
        [Header("Energy Display")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private int energyBarLength = 10;

        [Header("Pollution Sources Display")]
        [SerializeField] private TMP_Text pollutionSourcesText;

        private int totalPollutionSources = 0;

        [Header("Game State Display")]
        [SerializeField] private TMP_Text gameStateText;

        [Header("Tile Hover Info - Plant")]
        [SerializeField] private TMP_Text plantPhaseText;
        [SerializeField] private TMP_Text plantLeafText;
        [SerializeField] private TMP_Text plantRootText;
        [SerializeField] private TMP_Text plantFruitText;

        [Header("Tile Hover Info - Pollution")]
        [SerializeField] private TMP_Text pollutionTypeText;
        [SerializeField] private TMP_Text pollutionSpreadText;
        [SerializeField] private TMP_Text pollutionStrengthText;
        [SerializeField] private TMP_Text pollutionResistanceText;

        [Header("Plant Info Panel")]
        [SerializeField] private GameObject plantInfo;
        [SerializeField] private TMP_Text plantInfoExtractionText;
        [SerializeField] private TMP_Text plantInfoAttackText;
        [SerializeField] private TMP_Text plantInfoStorageText;
        [SerializeField] private TMP_Text plantInfoMaxComponentsText;
        [SerializeField] private TMP_Text plantInfoInfectionText;
        [SerializeField] private UnityEngine.UI.Button nipButton;
        [SerializeField] private UnityEngine.UI.Button sproutButton;

        [Header("Log Message")]
        [SerializeField] private TMP_Text logMessageText;

        private GameObject currentHoveredTile = null;

        protected override void InnerAwake()
        {
            base.InnerAwake();
            if (plantInfo != null)
            {
                plantInfo.SetActive(false);
            }

            if (nipButton != null)
            {
                nipButton.onClick.AddListener(OnNipButtonClicked);
            }

            if (sproutButton != null)
            {
                sproutButton.onClick.AddListener(OnSproutButtonClicked);
            }
        }

        protected override void InnerUpdate()
        {
            base.InnerUpdate();

            if (isActive)
            {
                UpdateHUD();
                CheckHoverState();

                if (plantInfo != null && plantInfo.activeSelf)
                {
                    UpdatePlantInfo();
                }
            }
        }

        /// <summary>
        /// Checks if the player is still hovering over a tile, clears hover info if not
        /// </summary>
        private void CheckHoverState()
        {
            // Check if we have a hovered tile
            if (currentHoveredTile == null)
            {
                // No tile being hovered, make sure info is cleared
                ClearTileHoverInfo();
            }
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            base.OpenMenu();

            InitializePollutionSourceCount();
            UpdateHUD();
        }

        private void InitializePollutionSourceCount()
        {
            // Capture the initial pollution source count at game start
            if (PollutionManager.Instance != null && totalPollutionSources == 0)
            {
                totalPollutionSources = PollutionManager.Instance.pollutionSources.Count;
            }
        }

        public override void RefreshMenu()
        {
            base.RefreshMenu();
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (GameManager.Instance == null || PlantBitManager.Instance == null) return;

            UpdateEnergyDisplay();
            UpdatePollutionSourcesDisplay();
            UpdateGameStateDisplay();
        }

        private void UpdateEnergyDisplay()
        {
            PlantBitManager plantManager = PlantBitManager.Instance;

            if (energyText != null)
            {
                string bar = CreateProgressBar((int)plantManager.currentEnergy, (int)plantManager.maxEnergy, energyBarLength);
                energyText.text = $"Energy: {bar} {plantManager.currentEnergy:F0}/{plantManager.maxEnergy:F0}";
            }
        }

        private void UpdatePollutionSourcesDisplay()
        {
            PollutionManager pollutionManager = PollutionManager.Instance;

            if (pollutionSourcesText != null && pollutionManager != null)
            {
                int currentSources = pollutionManager.pollutionSources.Count;

                // Update total if we haven't captured it yet
                if (totalPollutionSources == 0 || currentSources > totalPollutionSources)
                {
                    totalPollutionSources = currentSources;
                }

                // Multiply by 2 so each source takes up 2 blocks in the bar
                int barCurrent = currentSources * 2;
                int barMax = totalPollutionSources * 2;

                // Bar length is exactly the max number of blocks needed
                string bar = CreateProgressBar(barCurrent, barMax, barMax);
                pollutionSourcesText.text = $"Pollution Sources: {bar} {currentSources}/{totalPollutionSources}";
            }
        }

        /// <summary>
        /// Creates a text-based progress bar using block characters
        /// </summary>
        private string CreateProgressBar(int current, int max, int barLength)
        {
            if (max <= 0) return "[          ]";

            // Calculate how many blocks should be filled (remaining sources = filled blocks)
            float ratio = (float)current / max;
            int filledBlocks = Mathf.RoundToInt(ratio * barLength);
            int emptyBlocks = barLength - filledBlocks;

            // Build the bar string
            string filled = new string('█', filledBlocks);
            string empty = new string('░', emptyBlocks);

            return $"[{filled}{empty}]";
        }

        private void UpdateGameStateDisplay()
        {
            GameManager gameManager = GameManager.Instance;

            if (gameStateText != null)
            {
                string stateDisplay = gameManager.gameState switch
                {
                    GameState.HeartPlacement => "Place Your Heart",
                    GameState.Playing => "Playing",
                    GameState.Won => "Victory!",
                    GameState.Lost => "Defeat",
                    _ => ""
                };

                gameStateText.text = stateDisplay;
            }
        }

        /// <summary>
        /// Updates the hover info display based on the hovered tile GameObject
        /// Call this when the player hovers over a tile
        /// </summary>
        public void UpdateTileHoverInfo(GameObject tileObject)
        {
            currentHoveredTile = tileObject;

            if (tileObject == null)
            {
                ClearTileHoverInfo();
                return;
            }

            // Get the GridVisualTile component to access coordinates
            GridVisualTile visualTile = tileObject.GetComponent<GridVisualTile>();
            if (visualTile == null || GameManager.Instance == null)
            {
                ClearTileHoverInfo();
                return;
            }

            Vector2Int coords = visualTile.coords;

            bool hasPlant = false;
            bool hasPollution = false;

            // Check for plant at this position
            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(coords);
            if (plant != null)
            {
                UpdatePlantHoverInfo(plant);
                hasPlant = true;
            }
            else
            {
                ClearPlantHoverInfo();
            }

            // Check for pollution tile
            PollutionTile pollutionTile = GameManager.Instance.gameGrid.GetEntity<PollutionTile>(coords);
            if (pollutionTile != null)
            {
                UpdatePollutionHoverInfo(pollutionTile, false);
                hasPollution = true;
            }
            else
            {
                // Check for pollution source
                PollutionSource pollutionSource = GameManager.Instance.gameGrid.GetEntity<PollutionSource>(coords);
                if (pollutionSource != null)
                {
                    UpdatePollutionHoverInfo(pollutionSource);
                    hasPollution = true;
                }
                else
                {
                    ClearPollutionHoverInfo();
                }
            }

            // If nothing on this tile, clear everything
            if (!hasPlant && !hasPollution)
            {
                ClearTileHoverInfo();
            }
        }

        /// <summary>
        /// Call this when the player stops hovering over a tile
        /// </summary>
        public void OnTileHoverExit()
        {
            currentHoveredTile = null;
            ClearTileHoverInfo();
        }

        private void UpdatePlantHoverInfo(PlantBit plant)
        {
            if (plantPhaseText != null)
            {
                string phaseStr = plant.phase == PlantBitPhase.Bud ? "Bud" :
                                  plant.phase == PlantBitPhase.Grown ? "Grown" :
                                  plant.phase == PlantBitPhase.FullyInfected ? "FullyInfected" : "Unknown";
                if (plant.isHeart) phaseStr = "Heart";
                if (plant.isInfected) phaseStr += " (Infected)";
                plantPhaseText.text = $"Phase: {phaseStr}";
            }

            if (plantLeafText != null)
            {
                plantLeafText.text = $"Leaf: {plant.leafCount}";
                if (plant.graftedLeafCount > 0)
                    plantLeafText.text += $" (+{plant.graftedLeafCount})";
                plantLeafText.text += $" | Energy: {plant.extractionRate:F1}";
            }

            if (plantRootText != null)
            {
                plantRootText.text = $"Root: {plant.rootCount}";
                if (plant.graftedRootCount > 0)
                    plantRootText.text += $" (+{plant.graftedRootCount})";
                plantRootText.text += $" | Attack: {plant.attackDamage:F1}";
            }

            if (plantFruitText != null)
            {
                plantFruitText.text = $"Fruit: {plant.fruitCount}";
                if (plant.graftedFruitCount > 0)
                    plantFruitText.text += $" (+{plant.graftedFruitCount})";
                plantFruitText.text += $" | Storage: {plant.energyStorage:F1}";
            }

            // Log detailed debug info to console
            Debug.Log($"[HUD] Plant at {plant.position} | " +
                      $"Phase: {plant.phase} | " +
                      $"IsHeart: {plant.isHeart} | " +
                      $"IsInfected: {plant.isInfected} | " +
                      $"L:{plant.leafCount}+{plant.graftedLeafCount} " +
                      $"R:{plant.rootCount}+{plant.graftedRootCount} " +
                      $"F:{plant.fruitCount}+{plant.graftedFruitCount} | " +
                      $"Attack:{plant.attackDamage:F2} Energy:{plant.extractionRate:F2} Storage:{plant.energyStorage:F2} | " +
                      $"SproutCost:{plant.sproutCost:F2} MaintenanceCost:{plant.maintenanceCost:F2} | " +
                      $"MaxComponents:{plant.maxComponentCount}");
        }

        private void UpdatePollutionHoverInfo(PollutionTile pollution, bool isSource)
        {
            if (pollutionTypeText != null)
            {
                pollutionTypeText.text = isSource ? "Pollution Source" : "Pollution";
            }

            if (pollutionSpreadText != null)
            {
                pollutionSpreadText.text = $"Spread: {pollution.pollutionSpreadRate:F1}";
            }

            if (pollutionStrengthText != null)
            {
                pollutionStrengthText.text = $"Strength: {pollution.pollutionStrength:F1}";
            }

            if (pollutionResistanceText != null)
            {
                pollutionResistanceText.text = $"Resistance: {pollution.pollutionResistance:F1}";
            }
        }

        private void UpdatePollutionHoverInfo(PollutionSource source)
        {
            if (pollutionTypeText != null)
            {
                pollutionTypeText.text = "Pollution Source";
            }

            if (pollutionSpreadText != null)
            {
                pollutionSpreadText.text = $"Spread: {source.pollutionSpreadRate:F1}";
            }

            if (pollutionStrengthText != null)
            {
                pollutionStrengthText.text = $"Strength: {source.pollutionStrength:F1}";
            }

            if (pollutionResistanceText != null)
            {
                pollutionResistanceText.text = $"Resistance: {source.pollutionResistance:F1}";
            }
        }

        private void ClearPlantHoverInfo()
        {
            if (plantPhaseText != null) plantPhaseText.text = "";
            if (plantLeafText != null) plantLeafText.text = "";
            if (plantRootText != null) plantRootText.text = "";
            if (plantFruitText != null) plantFruitText.text = "";
        }

        private void ClearPollutionHoverInfo()
        {
            if (pollutionTypeText != null) pollutionTypeText.text = "";
            if (pollutionSpreadText != null) pollutionSpreadText.text = "";
            if (pollutionStrengthText != null) pollutionStrengthText.text = "";
            if (pollutionResistanceText != null) pollutionResistanceText.text = "";
        }

        /// <summary>
        /// Clears all tile hover info (call when not hovering over any tile)
        /// </summary>
        public void ClearTileHoverInfo()
        {
            ClearPlantHoverInfo();
            ClearPollutionHoverInfo();
        }

        public void OpenPlantInfo()
        {
            if (plantInfo != null)
            {
                plantInfo.SetActive(true);
                UpdatePlantInfo();
            }
        }

        public void ClosePlantInfo()
        {
            if (plantInfo != null)
            {
                plantInfo.SetActive(false);
            }
        }

        private void UpdatePlantInfo()
        {
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null)
            {
                ClosePlantInfo();
                return;
            }

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null)
            {
                ClosePlantInfo();
                return;
            }

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null)
            {
                ClosePlantInfo();
                return;
            }

            if (plantInfoExtractionText != null)
            {
                plantInfoExtractionText.text = $"Extraction: {plant.extractionRate:F1}";
            }

            if (plantInfoAttackText != null)
            {
                plantInfoAttackText.text = $"Attack: {plant.attackDamage:F1}";
            }

            if (plantInfoStorageText != null)
            {
                plantInfoStorageText.text = $"Storage: {plant.energyStorage:F1}";
            }

            if (plantInfoMaxComponentsText != null)
            {
                plantInfoMaxComponentsText.text = $"Max Components: {plant.TotalComponents}/{plant.maxComponentCount}";
            }

            if (plantInfoInfectionText != null)
            {
                if (plant.isInfected && plant.phase != PlantBitPhase.FullyInfected)
                {
                    float infectionPercent = PollutionManager.Instance.CheckPlantInfection(plant);
                    string bar = CreateProgressBar((int)(infectionPercent * 100), 100, 10);
                    plantInfoInfectionText.text = $"Infection: {bar} {(infectionPercent * 100):F0}%";
                }
                else if (plant.phase == PlantBitPhase.FullyInfected)
                {
                    plantInfoInfectionText.text = "Infection: FULLY INFECTED";
                }
                else
                {
                    string bar = CreateProgressBar(0, 100, 10);
                    plantInfoInfectionText.text = $"Infection: {bar} 0%";
                }
            }

            if (nipButton != null)
            {
                nipButton.interactable = !plant.isHeart;
            }

            if (sproutButton != null)
            {
                sproutButton.interactable = plant.phase == PlantBitPhase.Grown && !plant.isInfected;
            }
        }

        private void OnNipButtonClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null) return;

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null) return;

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant != null && !plant.isHeart)
            {
                PlantBitManager.Instance.NipPlantBit(plant);
                float refund = plant.sproutCost * 0.25f;
                ShowLogMessage($"Plant nipped! Refunded {refund:F1} energy");
                ClosePlantInfo();
            }
        }

        private void OnSproutButtonClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null) return;

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null) return;

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant != null && plant.phase == PlantBitPhase.Grown && !plant.isInfected)
            {
                float currentEnergy = PlantBitManager.Instance.currentEnergy;
                float cost = plant.sproutCost;

                if (currentEnergy < cost)
                {
                    ShowLogMessage($"Not enough energy! Current: {currentEnergy:F0} Required: {cost:F0}");
                    return;
                }

                plant.AttemptSprout(true);
                ShowLogMessage($"Sprouted! Cost: {cost:F0} energy");
            }
        }

        public void ShowLogMessage(string message)
        {
            if (logMessageText != null)
            {
                logMessageText.text = message;
            }
        }
    }
}
