using UnityEngine;
using TMPro;

namespace Egglers
{
    /// <summary>
    /// Handles visual representation of a tile: plants, billboard, and pollution text.
    /// Listens to grid events and updates accordingly.
    /// </summary>
    public class GridVisualTile : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] public PlantBitManager plantBitManager;
        [SerializeField] public PollutionManager pollutionManager;
        [SerializeField] public GameManager gameManager;

        [Header("Plant Objects")]
        public GameObject plant1;
        public GameObject plant2;
        public GameObject plant3;

        [Header("Billboard / Pollution")]
        public TMP_Text tmp;

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

            PlantBit bit = plantBitManager?.gameGrid.GetTileAtPosition(pos)?.GetPlantBit();
            UpdatePlantVisuals(bit);
        }

        private void HandlePollutionUpdate(Vector2Int pos)
        {
            if (pos != coords) return;

            UpdatePollutionVisuals();
        }

        #endregion

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
        /// Updates the pollution display from the manager.
        /// </summary>
        public void UpdatePollutionVisuals()
        {
            if (tmp == null || pollutionManager == null) return;

            float level = pollutionManager.GetPollutionLevelAt(coords);
            tmp.text = level.ToString("0.00");
        }

        /// <summary>
        /// Updates plant visuals (sprites, scale, etc.) if needed.
        /// </summary>
        private void UpdatePlantVisuals(PlantBit bit)
        {
            // TODO: implement custom plant visual update
        }

        #endregion
    }
}
