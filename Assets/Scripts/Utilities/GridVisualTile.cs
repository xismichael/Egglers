using UnityEngine;
using TMPro;

namespace Egglers
{
    /// <summary>
    /// Handles visual representation of a tile: plants, billboard, and pollution text.
    /// Listens to grid events and updates accordingly.
    /// Uses the unified GridSystem for all entity access.
    /// </summary>
    public class GridVisualTile : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] public GameManager gameManager;
        [SerializeField] public PlantBitManager plantBitManager;
        [SerializeField] public PollutionManager pollutionManager;

        [Header("Plant Objects")]
        public GameObject plant1;
        public GameObject plant2;
        public GameObject plant3;

        [Header("Billboard / Pollution")]
        public TMP_Text tmp;
        [SerializeField] private PollutionParticles pollutionParticles;

        private const float SpreadNormalization = 75f;
        private const float StrengthNormalization = 75f;
        private const float ResistanceNormalization = 75f;


        [Header("Grid Info")]
        public Vector2Int coords;

        private void OnEnable()
        {
            GridEvents.OnPlantUpdated += HandlePlantUpdate;
            GridEvents.OnPollutionUpdated += HandlePollutionUpdate;
        }

        private void OnDisable()
        {
            GridEvents.OnPlantUpdated -= HandlePlantUpdate;
            GridEvents.OnPollutionUpdated -= HandlePollutionUpdate;
        }

        #region Grid Event Handlers

        private void HandlePlantUpdate(Vector2Int pos)
        {
            if (pos != coords) return;

            PlantBit bit = gameManager.gameGrid.GetEntity<PlantBit>(pos);
            UpdatePlantVisuals(bit);
        }

        private void HandlePollutionUpdate(Vector2Int pos)
        {
            if (pos != coords) return;

            PollutionTile tile = gameManager.gameGrid.GetEntity<PollutionTile>(coords);
            PollutionSource source = gameManager.gameGrid.GetEntity<PollutionSource>(coords);

            float level = 0f;
            if (tile != null) level = tile.GetTotalPollution();
            else if (source != null) level = source.GetTotalPollution();

            if (tmp != null)
                tmp.text = level.ToString("0.00");

            float spreadRate = 0f;
            float strength = 0f;
            float resistance = 0f;

            if (tile != null)
            {
                spreadRate = tile.pollutionSpreadRate;
                strength = tile.pollutionStrength;
                resistance = tile.pollutionResistance;
            }
            else if (source != null)
            {
                spreadRate = source.pollutionSpreadRate;
                strength = source.pollutionStrength;
                resistance = source.pollutionResistance;
            }

            if (pollutionParticles != null)
            {
                if (level <= 0.01f)
                {
                    pollutionParticles.DisableVisuals();
                }
                else
                {
                    pollutionParticles.EnableVisuals();

                    float spreadIntensity = Mathf.Clamp01(spreadRate / SpreadNormalization);
                    float strengthIntensity = Mathf.Clamp01(strength / StrengthNormalization);
                    float resistanceIntensity = Mathf.Clamp01(resistance / ResistanceNormalization);

                    pollutionParticles.SetResistanceIntensity(resistanceIntensity);
                    pollutionParticles.SetAttackIntensity(strengthIntensity);
                    pollutionParticles.SetSpreadIntensity(spreadIntensity);
                }
            }
        }

        #endregion

        public void RefreshActions()
        {
            TileActions tileActions = GetComponent<TileActions>();
            if (tileActions == null || tileActions.actions == null) return;

            Vector2Int pos = coords;

            bool hasPollution =
                PollutionManager.Instance.gameGrid.GetEntity<PollutionTile>(pos) != null ||
                PollutionManager.Instance.gameGrid.GetEntity<PollutionSource>(pos) != null;

            bool hasHeart =
                PlantBitManager.Instance.gameGrid.GetEntity<PlantBit>(pos)?.isHeart == true;

            foreach (var action in tileActions.actions)
            {
                switch (action.actionType)
                {
                    case TileActionType.PlaceHeart:
                        // Disable if pollution OR heart already exists
                        action.enabled = !hasPollution && !hasHeart;
                        break;

                    default:
                        action.enabled = true;
                        break;
                }
            }
        }

        #region Plant & Billboard Methods

        /// <summary>
        /// Sets only the specified plant active (by index 0-2), disables others.
        /// </summary>
        public void SetActivePlantByIndex(int index)
        {
            if (plant1 != null) plant1.SetActive(index == 0);
            if (plant2 != null) plant2.SetActive(index == 1);
            if (plant3 != null) plant3.SetActive(index == 2);
        }

        /// <summary>
        /// Sets billboard or TMP text for the tile.
        /// </summary>
        public void SetBillboardText(string text)
        {
            if (tmp != null)
            {
                tmp.text = text;
            }
        }

        /// <summary>
        /// Updates plant visuals (sprites, scale, etc.) if needed.
        /// Customize to match your plant prefabs or animation logic.
        /// </summary>
        private void UpdatePlantVisuals(PlantBit bit)
        {
            if (bit == null)
            {
                SetActivePlantByIndex(-1); // disable all
                return;
            }

            // Example: enable plant1 for heart, plant2 for normal, plant3 for special
            if (bit.isHeart) SetActivePlantByIndex(2);
            else if (bit.phase == PlantBitPhase.Grown) SetActivePlantByIndex(1);
            else if (bit.phase == PlantBitPhase.Bud) SetActivePlantByIndex(0);
            else SetActivePlantByIndex(-1);
        }

        #endregion
    }
}
