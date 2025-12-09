using TMPro;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Grafting mode for UI
    /// </summary>
    public enum GraftMode
    {
        ApplyGraft,
        RemoveGraft
    }

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
        [SerializeField] private TMP_Text plantInfoMaintenanceText;
        [SerializeField] private TMP_Text plantInfoMaxComponentsText;
        [SerializeField] private TMP_Text plantInfoInfectionText;
        [SerializeField] private TMP_Text plantInfoSpreadTimerLabelText;
        [SerializeField] private UnityEngine.UI.Button nipButton;
        [SerializeField] private UnityEngine.UI.Button sproutButton;

        [Header("Grafting UI")]
        [SerializeField] private TMP_Text graftBufferText;
        [SerializeField] private TMP_Text graftModeText;
        [SerializeField] private UnityEngine.UI.Button graftModeToggleButton;
        [SerializeField] private UnityEngine.UI.Slider leafSlider;
        [SerializeField] private UnityEngine.UI.Slider rootSlider;
        [SerializeField] private UnityEngine.UI.Slider fruitSlider;
        [SerializeField] private TMP_Text leafValueText;
        [SerializeField] private TMP_Text rootValueText;
        [SerializeField] private TMP_Text fruitValueText;
        [SerializeField] private UnityEngine.UI.Button graftApplyButton;

        [Header("Log Message")]
        [SerializeField] private TMP_Text logMessageText;

        private GameObject currentHoveredTile = null;
        private GraftMode currentGraftMode = GraftMode.ApplyGraft;
        private GameObject lastFocusedTileForGrafting = null; // Track when plant selection changes

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

            if (graftModeToggleButton != null)
            {
                graftModeToggleButton.onClick.AddListener(OnGraftModeToggleClicked);
            }

            if (graftApplyButton != null)
            {
                graftApplyButton.onClick.AddListener(OnGraftApplyClicked);
            }

            // Add slider listeners to update max values dynamically
            if (leafSlider != null)
            {
                leafSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (rootSlider != null)
            {
                rootSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (fruitSlider != null)
            {
                fruitSlider.onValueChanged.AddListener(OnSliderValueChanged);
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
            UpdateGraftBufferDisplay();
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
                plantLeafText.text += $" | Extraction: {plant.extractionRate:F1}/tick";
            }

            if (plantRootText != null)
            {
                plantRootText.text = $"Root: {plant.rootCount}";
                if (plant.graftedRootCount > 0)
                    plantRootText.text += $" (+{plant.graftedRootCount})";
                plantRootText.text += $" | Atk: {plant.attackDamage:F1}";
            }

            if (plantFruitText != null)
            {
                plantFruitText.text = $"Fruit: {plant.fruitCount}";
                if (plant.graftedFruitCount > 0)
                    plantFruitText.text += $" (+{plant.graftedFruitCount})";
                plantFruitText.text += $" | Storage: {plant.energyStorage:F1}";
            }
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

            if (plantInfoInfectionText != null)
            {
                if (plant.isInfected && plant.phase != PlantBitPhase.FullyInfected)
                {
                    float infectionPercent = PollutionManager.Instance.CheckPlantInfection(plant);
                    string bar = CreateProgressBar((int)(infectionPercent * 100), 100, 10);
                    plantInfoInfectionText.text = $"{bar} {(infectionPercent * 100):F0}%";
                }
                else if (plant.phase == PlantBitPhase.FullyInfected)
                {
                    string bar = CreateProgressBar(100, 100, 10);
                    plantInfoInfectionText.text = $"{bar} 100%";
                }
                else
                {
                    string bar = CreateProgressBar(0, 100, 10);
                    plantInfoInfectionText.text = $"{bar} 0%";
                }
            }

            // Update spread timer for fully infected plants
            if (plant.phase == PlantBitPhase.FullyInfected && plant.infectionCoroutine != null)
            {
                // Calculate time elapsed and remaining
                float elapsed = Time.time - plant.infectionSpreadStartTime;
                float remaining = Mathf.Max(0, plant.infectionSpreadDuration - elapsed);
                
                // Label with countdown
                if (plantInfoSpreadTimerLabelText != null)
                {
                    plantInfoSpreadTimerLabelText.text = $"Spreads in {remaining:F1}s";
                }
            }
            else
            {
                // Hide the spread timer if not fully infected or not spreading
                if (plantInfoSpreadTimerLabelText != null)
                {
                    plantInfoSpreadTimerLabelText.text = "";
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

            // Only update graft scrollbars if the focused tile changed (don't reset every frame!)
            if (GameManager.Instance.focusedTile != lastFocusedTileForGrafting)
            {
                lastFocusedTileForGrafting = GameManager.Instance.focusedTile;
                UpdateGraftScrollbars(resetValues: true);
            }
            else
            {
                // Keep scrollbar max values updated even if tile hasn't changed
                // (buffer or plant state might have changed)
                UpdateGraftScrollbars(resetValues: false);
            }

            // Update graft UI
            UpdateGraftModeButtonText();
            UpdateGraftModeIndicator();
            UpdateGraftBufferDisplay();
            UpdateGraftPreview(plant);
        }

        private void OnNipButtonClicked()
        {
            Debug.Log("[HUD] OnNipButtonClicked called");
            
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null) return;

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null) return;

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null) return;
            
            // Nip the plant (checks isHeart internally)
            bool success = plant.Nip();
            
            if (success)
            {
                ClosePlantInfo();
                SoundManager.Instance.PlayButtonClick();
            }
        }

        private void OnSproutButtonClicked()
        {
            Debug.Log("[HUD] OnSproutButtonClicked called");
            
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null)
            {
                ShowLogMessage("No plant selected");
                return;
            }

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null)
            {
                ShowLogMessage("Invalid tile");
                return;
            }

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null)
            {
                ShowLogMessage("No plant on this tile");
                return;
            }
            
            if (plant.phase != PlantBitPhase.Grown)
            {
                ShowLogMessage("Plant must be grown to sprout");
                SoundManager.Instance.PlayError();
                return;
            }
            
            if (plant.isInfected)
            {
                ShowLogMessage("Cannot sprout infected plant");
                SoundManager.Instance.PlayError();
                return;
            }

            float currentEnergy = PlantBitManager.Instance.currentEnergy;
            float cost = plant.sproutCost;

            if (currentEnergy < cost)
            {
                ShowLogMessage($"Not enough energy! Current: {currentEnergy:F0} Required: {cost:F0}");
                SoundManager.Instance.PlayError();
                return;
            }

            bool success = plant.AttemptSprout(true);
            
            if (success)
            {
                ShowLogMessage($"Sprouted! Cost: {cost:F0} energy");
                SoundManager.Instance.PlayButtonClick();
            }
            else
            {
                ShowLogMessage("No available spots to sprout (neighbors blocked or polluted)");
                SoundManager.Instance.PlayError();
            }
        }

        public void ShowLogMessage(string message)
        {
            if (logMessageText != null)
            {
                logMessageText.text = message;
            }
        }

        private void OnGraftModeToggleClicked()
        {
            // Toggle between Apply and Remove modes
            currentGraftMode = currentGraftMode == GraftMode.ApplyGraft
                ? GraftMode.RemoveGraft
                : GraftMode.ApplyGraft;

            // Update scrollbars for the new mode (and reset values)
            UpdateGraftScrollbars(resetValues: true);

            // Update button text to reflect current mode
            UpdateGraftModeButtonText();

            SoundManager.Instance.PlayButtonClick();
        }

        private void OnGraftApplyClicked()
        {
            Debug.Log("[HUD] OnGraftApplyClicked called");
            
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null)
            {
                ShowLogMessage("No plant selected");
                return;
            }

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null)
            {
                ShowLogMessage("Invalid tile");
                return;
            }

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null)
            {
                ShowLogMessage("No plant on this tile");
                return;
            }

            // Check if plant is a bud (can't graft buds)
            if (plant.phase == PlantBitPhase.Bud)
            {
                ShowLogMessage("Cannot graft a bud");
                SoundManager.Instance.PlayError();
                return;
            }

            // Check if plant is fully infected (can't graft fully infected)
            if (plant.phase == PlantBitPhase.FullyInfected)
            {
                ShowLogMessage("Cannot graft a fully infected plant");
                SoundManager.Instance.PlayError();
                return;
            }

            // Get slider values (already integers due to wholeNumbers = true)
            int leafAmount = leafSlider != null ? (int)leafSlider.value : 0;
            int rootAmount = rootSlider != null ? (int)rootSlider.value : 0;
            int fruitAmount = fruitSlider != null ? (int)fruitSlider.value : 0;
            
            Debug.Log($"[HUD] Slider values - Leaf: {leafAmount}, Root: {rootAmount}, Fruit: {fruitAmount}");

            int totalAmount = leafAmount + rootAmount + fruitAmount;
            
            Debug.Log($"[HUD] Total amount: {totalAmount}, Current mode: {currentGraftMode}");

            if (totalAmount == 0)
            {
                ShowLogMessage("Select components to graft");
                return;
            }

            if (currentGraftMode == GraftMode.ApplyGraft)
            {
                // Check if buffer has enough components
                GraftBuffer buffer = PlantBitManager.Instance.graftBuffer;
                
                Debug.Log($"[HUD] Buffer contents - Leaf: {buffer.leafCount}, Root: {buffer.rootCount}, Fruit: {buffer.fruitCount}");

                if (!buffer.hasContent)
                {
                    ShowLogMessage("Buffer is empty! Use Remove mode to harvest components first");
                    return;
                }

                if (leafAmount > buffer.leafCount || rootAmount > buffer.rootCount || fruitAmount > buffer.fruitCount)
                {
                    ShowLogMessage("Not enough components in buffer");
                    Debug.Log($"[HUD] Not enough in buffer. Requested L{leafAmount} R{rootAmount} F{fruitAmount}");
                    return;
                }

                // Check if plant has room for components
                int componentsLeft = plant.maxComponentCount - plant.TotalComponents;
                Debug.Log($"[HUD] Plant components: {plant.TotalComponents}/{plant.maxComponentCount}, Space left: {componentsLeft}");
                
                if (totalAmount > componentsLeft)
                {
                    ShowLogMessage($"Plant can only hold {componentsLeft} more components");
                    return;
                }

                // Apply the graft
                bool success = plant.ApplyGraft(leafAmount, rootAmount, fruitAmount);
                if (success)
                {
                    ShowLogMessage($"Applied graft: L{leafAmount} R{rootAmount} F{fruitAmount}");
                    Debug.Log($"[HUD] Applied graft to plant at {plant.position}: L{leafAmount} R{rootAmount} F{fruitAmount}");
                    GridEvents.PlantUpdated(plant.position);
                }
                else
                {
                    ShowLogMessage("Failed to apply graft - not enough energy!");
                    return;
                }
            }
            else // RemoveGraft
            {
                Debug.Log($"[HUD] Remove mode - Plant natural components: L{plant.leafCount} R{plant.rootCount} F{plant.fruitCount}");
                
                // Check if plant has enough natural components to remove
                if (leafAmount > plant.leafCount || rootAmount > plant.rootCount || fruitAmount > plant.fruitCount)
                {
                    ShowLogMessage("Not enough natural components to remove");
                    return;
                }

                // Remove the graft (adds to buffer)
                bool success = plant.RemoveGraft(leafAmount, rootAmount, fruitAmount);
                if (success)
                {
                    ShowLogMessage($"Removed components: L{leafAmount} R{rootAmount} F{fruitAmount}");
                    Debug.Log($"[HUD] Removed components from plant at {plant.position}: L{leafAmount} R{rootAmount} F{fruitAmount}");
                    GridEvents.PlantUpdated(plant.position);
                }
                else
                {
                    ShowLogMessage("Failed to remove components - not enough energy!");
                    return;
                }
            }

            // Reset sliders after action
            if (leafSlider != null) leafSlider.value = 0;
            if (rootSlider != null) rootSlider.value = 0;
            if (fruitSlider != null) fruitSlider.value = 0;

            // Update the scrollbars to reflect new state
            // After Remove, the buffer contents changed, so we need to fully refresh the scrollbar ranges
            UpdateGraftScrollbars(resetValues: false);

            SoundManager.Instance.PlayButtonClick();
        }

        private void UpdateGraftScrollbars(bool resetValues = false)
        {
            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null) return;

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null) return;

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null) return;

            // Only reset slider values when explicitly requested (mode change, plant change)
            if (resetValues)
            {
                if (leafSlider != null) leafSlider.value = 0;
                if (rootSlider != null) rootSlider.value = 0;
                if (fruitSlider != null) fruitSlider.value = 0;
            }

            // Can't graft buds or fully infected plants
            if (plant.phase == PlantBitPhase.Bud || plant.phase == PlantBitPhase.FullyInfected)
            {
                SetSliderRange(leafSlider, 0);
                SetSliderRange(rootSlider, 0);
                SetSliderRange(fruitSlider, 0);

                // Disable sliders
                if (leafSlider != null) leafSlider.interactable = false;
                if (rootSlider != null) rootSlider.interactable = false;
                if (fruitSlider != null) fruitSlider.interactable = false;
                if (graftApplyButton != null) graftApplyButton.interactable = false;
                return;
            }

            // Enable sliders for grown plants
            if (leafSlider != null) leafSlider.interactable = true;
            if (rootSlider != null) rootSlider.interactable = true;
            if (fruitSlider != null) fruitSlider.interactable = true;
            if (graftApplyButton != null) graftApplyButton.interactable = true;

            // Dynamic logic based on mode
            if (currentGraftMode == GraftMode.RemoveGraft)
            {
                // Remove mode: max = natural component count
                SetSliderRange(leafSlider, plant.leafCount);
                SetSliderRange(rootSlider, plant.rootCount);
                SetSliderRange(fruitSlider, plant.fruitCount);
            }
            else // ApplyGraft
            {
                // Apply mode: max = min(buffer amount, components left)
                int componentsLeft = plant.maxComponentCount - plant.TotalComponents;
                GraftBuffer buffer = PlantBitManager.Instance.graftBuffer;

                int leafMax = Mathf.Min(buffer.leafCount, componentsLeft);
                int rootMax = Mathf.Min(buffer.rootCount, componentsLeft);
                int fruitMax = Mathf.Min(buffer.fruitCount, componentsLeft);

                // Log warning if buffer is empty
                if (buffer.TotalComponents == 0 && resetValues)
                {
                    ShowLogMessage("Graft buffer is empty! Use Remove mode first");
                }

                SetSliderRange(leafSlider, leafMax);
                SetSliderRange(rootSlider, rootMax);
                SetSliderRange(fruitSlider, fruitMax);
            }
        }

        private void SetSliderRange(UnityEngine.UI.Slider slider, int maxValue)
        {
            if (slider == null) return;

            slider.minValue = 0;
            slider.maxValue = maxValue;
            slider.wholeNumbers = true;
        }

        private void UpdateGraftModeButtonText()
        {
            if (graftModeToggleButton == null) return;

            // Try to find TMP_Text first (preferred), then fall back to legacy Text
            TMP_Text tmpText = graftModeToggleButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = currentGraftMode == GraftMode.ApplyGraft ? "Apply" : "Remove";
                return;
            }

            // Fallback to legacy Text component
            UnityEngine.UI.Text legacyText = graftModeToggleButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (legacyText != null)
            {
                legacyText.text = currentGraftMode == GraftMode.ApplyGraft ? "Apply" : "Remove";
            }
        }

        private void OnSliderValueChanged(float value)
        {
            // Only need to update in ApplyGraft mode (interdependent limits)
            if (currentGraftMode != GraftMode.ApplyGraft) return;

            if (GameManager.Instance == null || GameManager.Instance.focusedTile == null) return;

            GridVisualTile visualTile = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
            if (visualTile == null) return;

            PlantBit plant = GameManager.Instance.gameGrid.GetEntity<PlantBit>(visualTile.coords);
            if (plant == null) return;

            // Temporarily remove listeners to prevent feedback loop
            if (leafSlider != null) leafSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            if (rootSlider != null) rootSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            if (fruitSlider != null) fruitSlider.onValueChanged.RemoveListener(OnSliderValueChanged);

            // Get current slider values (already integers)
            int leafAmount = (int)(leafSlider != null ? leafSlider.value : 0);
            int rootAmount = (int)(rootSlider != null ? rootSlider.value : 0);
            int fruitAmount = (int)(fruitSlider != null ? fruitSlider.value : 0);

            // Calculate how many components are left after current selections
            int componentsLeft = plant.maxComponentCount - plant.TotalComponents;
            int remainingSlots = componentsLeft - (leafAmount + rootAmount + fruitAmount);

            // Update max values for each slider based on buffer and remaining slots
            GraftBuffer buffer = PlantBitManager.Instance.graftBuffer;

            int leafMax = Mathf.Min(buffer.leafCount, leafAmount + remainingSlots);
            int rootMax = Mathf.Min(buffer.rootCount, rootAmount + remainingSlots);
            int fruitMax = Mathf.Min(buffer.fruitCount, fruitAmount + remainingSlots);

            // Update slider ranges (this will clamp values automatically)
            SetSliderRange(leafSlider, leafMax);
            SetSliderRange(rootSlider, rootMax);
            SetSliderRange(fruitSlider, fruitMax);

            // Values are already clamped by Unity when we changed the range
            // No need to manually restore them

            // Re-add listeners
            if (leafSlider != null) leafSlider.onValueChanged.AddListener(OnSliderValueChanged);
            if (rootSlider != null) rootSlider.onValueChanged.AddListener(OnSliderValueChanged);
            if (fruitSlider != null) fruitSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void UpdateGraftModeIndicator()
        {
            if (graftModeText == null) return;

            string modeString = currentGraftMode == GraftMode.ApplyGraft ? "APPLY" : "REMOVE";
            graftModeText.text = $"MODE: {modeString}";
        }

        private void UpdateGraftBufferDisplay()
        {
            if (graftBufferText == null || PlantBitManager.Instance == null) return;

            GraftBuffer buffer = PlantBitManager.Instance.graftBuffer;
            graftBufferText.text = $"Buffer: L{buffer.leafCount} R{buffer.rootCount} F{buffer.fruitCount}";
        }

        private void UpdateGraftPreview(PlantBit plant)
        {
            if (plant == null) return;

            // Get current slider values
            int leafAmount = leafSlider != null ? (int)leafSlider.value : 0;
            int rootAmount = rootSlider != null ? (int)rootSlider.value : 0;
            int fruitAmount = fruitSlider != null ? (int)fruitSlider.value : 0;

            // Update slider value displays
            if (leafValueText != null)
            {
                leafValueText.text = leafAmount.ToString();
            }

            if (rootValueText != null)
            {
                rootValueText.text = rootAmount.ToString();
            }

            if (fruitValueText != null)
            {
                fruitValueText.text = fruitAmount.ToString();
            }

            // Calculate preview stats based on mode
            float previewExtraction, previewAttack, previewStorage, previewMaintenance;

            if (currentGraftMode == GraftMode.ApplyGraft)
            {
                // Apply mode: add grafted components
                int newGraftedLeaf = plant.graftedLeafCount + leafAmount;
                int newGraftedRoot = plant.graftedRootCount + rootAmount;
                int newGraftedFruit = plant.graftedFruitCount + fruitAmount;

                previewExtraction = plant.CalculateStat(plant.leafCount, newGraftedLeaf, plant.data.leafMultiplier);
                previewAttack = plant.CalculateStat(plant.rootCount, newGraftedRoot, plant.data.rootMultiplier);
                previewStorage = plant.CalculateStat(plant.fruitCount, newGraftedFruit, plant.data.fruitMultiplier);
                
                // Calculate new maintenance cost
                int newTotalComponents = plant.leafCount + newGraftedLeaf + plant.rootCount + newGraftedRoot + plant.fruitCount + newGraftedFruit;
                previewMaintenance = plant.CalculateMaintenanceCostForComponents(newTotalComponents);
            }
            else // Remove mode
            {
                // Remove mode: subtract natural components
                int newLeafCount = Mathf.Max(0, plant.leafCount - leafAmount);
                int newRootCount = Mathf.Max(0, plant.rootCount - rootAmount);
                int newFruitCount = Mathf.Max(0, plant.fruitCount - fruitAmount);

                previewExtraction = plant.CalculateStat(newLeafCount, plant.graftedLeafCount, plant.data.leafMultiplier);
                previewAttack = plant.CalculateStat(newRootCount, plant.graftedRootCount, plant.data.rootMultiplier);
                previewStorage = plant.CalculateStat(newFruitCount, plant.graftedFruitCount, plant.data.fruitMultiplier);
                
                // Calculate new maintenance cost
                int newTotalComponents = newLeafCount + plant.graftedLeafCount + newRootCount + plant.graftedRootCount + newFruitCount + plant.graftedFruitCount;
                previewMaintenance = plant.CalculateMaintenanceCostForComponents(newTotalComponents);
            }

            // Update the existing plant info texts with current → preview (delta)
            if (plantInfoExtractionText != null)
            {
                float delta = previewExtraction - plant.extractionRate;
                string deltaStr = delta > 0 ? $"<color=green>+{delta:F1}</color>" : delta < 0 ? $"<color=red>{delta:F1}</color>" : "±0.0";
                plantInfoExtractionText.text = $"Extract: {plant.extractionRate:F1} → {previewExtraction:F1} ({deltaStr})";
            }

            if (plantInfoAttackText != null)
            {
                float delta = previewAttack - plant.attackDamage;
                string deltaStr = delta > 0 ? $"<color=green>+{delta:F1}</color>" : delta < 0 ? $"<color=red>{delta:F1}</color>" : "±0.0";
                plantInfoAttackText.text = $"Atk: {plant.attackDamage:F1} → {previewAttack:F1} ({deltaStr})";
            }

            if (plantInfoStorageText != null)
            {
                float delta = previewStorage - plant.energyStorage;
                string deltaStr = delta > 0 ? $"<color=green>+{delta:F1}</color>" : delta < 0 ? $"<color=red>{delta:F1}</color>" : "±0.0";
                plantInfoStorageText.text = $"Storage: {plant.energyStorage:F1} → {previewStorage:F1} ({deltaStr})";
            }

            if (plantInfoMaintenanceText != null)
            {
                float delta = previewMaintenance - plant.maintenanceCost;
                string deltaStr = delta > 0 ? $"<color=red>+{delta:F1}</color>" : delta < 0 ? $"<color=green>{delta:F1}</color>" : "±0.0";
                plantInfoMaintenanceText.text = $"Maint: {plant.maintenanceCost:F1} → {previewMaintenance:F1} ({deltaStr})";
            }

            // Update component text based on mode
            if (plantInfoMaxComponentsText != null)
            {
                if (currentGraftMode == GraftMode.ApplyGraft)
                {
                    // Apply mode: show slots available
                    int componentsLeft = plant.maxComponentCount - plant.TotalComponents;
                    plantInfoMaxComponentsText.text = $"Slots Available: {componentsLeft}";
                }
                else // Remove mode
                {
                    // Remove mode: show what's in the buffer
                    GraftBuffer buffer = PlantBitManager.Instance.graftBuffer;
                    plantInfoMaxComponentsText.text = $"Buffer: L{buffer.leafCount} R{buffer.rootCount} F{buffer.fruitCount}";
                }
            }
        }
    }
}
