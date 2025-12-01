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
        [SerializeField] public GameManager gameManager;
        [SerializeField] public PlantBitManager plantBitManager;
        [SerializeField] public PollutionManager pollutionManager;

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
                TileActionType.PlaceHeart => PlaceHeart,
                TileActionType.Debug => DebugTile,
                TileActionType.NipBud => NipBud,
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

        private void DebugTile(GameObject tile)
        {
            if (tile == null) return;

            GridVisualTile visual = tile.GetComponent<GridVisualTile>();
            if (visual == null)
            {
                Debug.Log("[DebugTile] No GridVisualTile component found.");
                return;
            }

            Vector2Int pos = visual.coords;
            Debug.Log($"[DebugTile] Tile at {pos}");

            // Plant data
            PlantBit plant = plantBitManager?.gameGrid.GetEntity<PlantBit>(pos);
            if (plant != null)
            {
                Debug.Log($"  PlantBit: Heart={plant.isHeart}, Phase={plant.phase}, " +
                          $"Leaf={plant.leafCount}+{plant.graftedLeafCount}, " +
                          $"Root={plant.rootCount}+{plant.graftedRootCount}, " +
                          $"Fruit={plant.fruitCount}+{plant.graftedFruitCount}, " +
                          $"EnergyStorage={plant.energyStorage}, Attack={plant.attackDamage}");
            }
            else
            {
                Debug.Log("  No PlantBit on this tile.");
            }

            // PollutionTile data
            PollutionTile pollution = pollutionManager?.gameGrid.GetEntity<PollutionTile>(pos);
            if (pollution != null)
            {
                Debug.Log($"  PollutionTile: SpreadRate={pollution.pollutionSpreadRate}, " +
                          $"Strength={pollution.pollutionStrength}, Resistance={pollution.pollutionResistance}, " +
                          $"Total={pollution.GetTotalPollution()}, ConnectedSources={pollution.connectedSources.Count}");
            }
            else
            {
                Debug.Log("  No PollutionTile on this tile.");
            }

            // PollutionSource data
            PollutionSource source = pollutionManager?.gameGrid.GetEntity<PollutionSource>(pos);
            if (source != null)
            {
                Debug.Log($"  PollutionSource: Total={source.GetTotalPollution()}, " +
                          $"SpreadRate={source.pollutionSpreadRate}, Strength={source.pollutionStrength}, " +
                          $"Resistance={source.pollutionResistance}, PulseRate={source.pulseRate}, " +
                          $"Dormant={source.dormantDuration}, Active={source.IsActive}, " +
                          $"ConnectedTiles={source.connectedTiles.Count}");
            }
            else
            {
                Debug.Log("  No PollutionSource on this tile.");
            }

            // Tile state
            if (plantBitManager != null)
            {
                TileState state = plantBitManager.gameGrid.GetTileState(pos);
                Debug.Log($"  TileState: {state}");
            }
        }

        private void NipBud(GameObject tile)
        {
            if (tile == null || plantBitManager == null) return;

            GridVisualTile visual = tile.GetComponent<GridVisualTile>();
            if (visual == null) return;

            PlantBit plant = plantBitManager.gameGrid.GetEntity<PlantBit>(visual.coords);
            if (plant == null)
            {
                Debug.Log("[NipBud] No plant on this tile.");
                return;
            }

            if (plant.isHeart)
            {
                Debug.LogWarning("[NipBud] Cannot nip the heart!");
                return;
            }

            float refund = plant.sproutCost * 0.5f;

            Debug.Log($"[NipBud] Killing plant at {plant.position}, refunding {refund} energy");

            plantBitManager.KillPlantBit(plant);
            plantBitManager.AddEnergy(refund);

            GridEvents.PlantUpdated(plant.position);
        }


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

            // // Refresh context menu to gray out PlaceHeart
            // TileContextMenu menu = FindFirstObjectByType<TileContextMenu>();
            // menu?.RefreshContextMenu();
        }

    }
}
