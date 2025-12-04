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

        [Header("Graft Buffer Display")]
        [SerializeField] private TMP_Text graftBufferText;

        [Header("Pollution Sources Display")]
        [SerializeField] private TMP_Text pollutionSourcesText;
        [SerializeField] private int pollutionBarLength = 10;
        
        private int totalPollutionSources = 0;

        [Header("Game State Display")]
        [SerializeField] private TMP_Text gameStateText;

        protected override void InnerAwake()
        {
            base.InnerAwake();
        }

        protected override void InnerUpdate()
        {
            base.InnerUpdate();
            
            // Update HUD every frame while active
            if (isActive)
            {
                UpdateHUD();
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
            UpdateGraftBufferDisplay();
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

        private void UpdateGraftBufferDisplay()
        {
            PlantBitManager plantManager = PlantBitManager.Instance;
            GraftBuffer buffer = plantManager.graftBuffer;

            if (graftBufferText != null)
            {
                if (buffer.hasContent)
                {
                    graftBufferText.text = $"Buffer: L:{buffer.leafCount} R:{buffer.rootCount} F:{buffer.fruitCount}";
                }
                else
                {
                    graftBufferText.text = "Buffer: Empty";
                }
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
                
                string bar = CreateProgressBar(currentSources, totalPollutionSources, pollutionBarLength);
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
    }
}
