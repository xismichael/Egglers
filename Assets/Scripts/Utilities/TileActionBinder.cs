using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Binds actions from TileActions to actual game callbacks.
    /// Delegates all visual updates to GridVisualTile.
    /// </summary>
    public class TileActionBinder : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] public PlantBitManager plantBitManager;
        [SerializeField] public PollutionManager pollutionManager;
        [SerializeField] public GameManager gameManager;

        private TileActions tileActions;

        void Awake()
        {
            tileActions = GetComponent<TileActions>();
            if (tileActions?.actions == null) return;

            foreach (var action in tileActions.actions)
            {
                action.callback = GetCallbackForType(action.actionType);
                if (action.callback == null)
                    Debug.LogWarning($"Unhandled tile action type {action.actionType} on {name}");
            }
        }

        // --------------------------
        // Map enum to actual callback
        // --------------------------
        private System.Action<GameObject> GetCallbackForType(TileActionType type)
        {
            return type switch
            {
                TileActionType.Plant1 => tile => SetActivePlant(tile, 0),
                TileActionType.Plant2 => tile => SetActivePlant(tile, 1),
                TileActionType.Plant3 => tile => SetActivePlant(tile, 2),
                TileActionType.Billboard => tile => SetBillboard(tile, "Hello World!"),
                // TileActionType.SpawnPollution => SpawnPollution,
                TileActionType.PlaceHeart => PlaceHeart,
                _ => null
            };
        }

        // --------------------------
        // Action callbacks
        // --------------------------
        private void SetActivePlant(GameObject tile, int plantIndex)
        {
            GridVisualTile visual = tile.GetComponent<GridVisualTile>();
            if (visual == null) return;

            visual.SetActivePlantByIndex(plantIndex);
        }

        private void SetBillboard(GameObject tile, string text)
        {
            GridVisualTile visual = tile.GetComponent<GridVisualTile>();
            if (visual == null) return;

            visual.SetBillboardText(text);
        }

        // private void SpawnPollution(GameObject tile)
        // {
        //     if (pollutionManager == null)
        //     {
        //         Debug.LogWarning("No PollutionManager assigned.");
        //         return;
        //     }

        //     GridVisualTile visual = tile.GetComponent<GridVisualTile>();
        //     if (visual == null || plantBitManager == null) return;

        //     Vector2Int pos = visual.coords;

        //     // Use the unified grid to set a PollutionTile
        //     if (!plantBitManager.gameGrid.HasEntity(pos))
        //     {
        //         PollutionTile newTile = new PollutionTile(pos);
        //         plantBitManager.gameGrid.SetEntity(pos, newTile);
        //         plantBitManager.gameGrid.SetTileState(pos, TileState.Pollution);
        //         GridEvents.PollutionUpdated(pos);
        //     }
        // }

        private void PlaceHeart(GameObject tile)
        {
            GridVisualTile visual = tile.GetComponent<GridVisualTile>();
            if (visual == null || gameManager == null) return;

            Vector2Int pos = visual.coords;

            if (!gameManager.IsValidHeartPlacement(pos))
            {
                Debug.Log("Invalid heart placement.");
                return;
            }

            gameManager.OnPlayerPlacesHeart(pos);
        }
    }
}
