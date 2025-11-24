using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Central manager for the pollution system.
    /// Handles pollution tile/source lifecycle and coordination with GridSystem.
    /// </summary>
    public class PollutionManager : MonoBehaviour
    {
        // === Singleton ===
        public static PollutionManager Instance { get; private set; }

        // === Grid reference ===
        public GridSystem gameGrid;

        public PlantBitManager plantManager;

        // === Entity tracking ===
        public List<PollutionSource> pollutionSources = new();
        public List<PollutionSource> activeSources = new();
        public Dictionary<Vector2Int, PollutionTile> pollutedTiles = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        /// <summary>
        /// Initializes the pollution grid with the specified dimensions.
        /// Called by GameManager during game initialization.
        /// </summary>
        public void Initialize(GridSystem sharedGrid)
        {
            gameGrid = sharedGrid;
        }
        /// <summary>
        /// Returns all 4-directional neighbors of a position.
        /// </summary>
        public List<Vector2Int> GetAdjacentPositions(Vector2Int pos)
        {
            return gameGrid.GetNeighbors(pos, includeDiagonal: false);
        }

        /// <summary>
        /// Adds a pollution tile at a position or updates an existing one.
        /// </summary>
        public PollutionTile AddPollutionToPosition(Vector2Int pos, float spreadRate, float strength, float resistance)
        {
            PollutionTile tile = gameGrid.GetEntity<PollutionTile>(pos);
            if (tile != null)
            {
                tile.pollutionSpreadRate += spreadRate;
                tile.pollutionStrength += strength;
                tile.pollutionResistance += resistance;
            }
            else
            {
                tile = new PollutionTile(pos)
                {
                    pollutionSpreadRate = spreadRate,
                    pollutionStrength = strength,
                    pollutionResistance = resistance,
                    isFrozen = false
                };

                gameGrid.SetEntity(tile);
                // gameGrid.SetTileState(pos, TileState.Pollution);
                pollutedTiles[pos] = tile;
            }

            GridEvents.PollutionUpdated(pos);
            return tile;
        }

        /// <summary>
        /// Gets an existing tile or creates one with default values.
        /// </summary>
        public PollutionTile GetOrCreateTile(Vector2Int pos)
        {
            return AddPollutionToPosition(pos, 10f, 10f, 5f);
        }

        /// <summary>
        /// Adds pollution stats to an existing tile (used during spreading).
        /// Does not create a new tile - the tile must already exist.
        /// </summary>
        public void AddPollutionToTile(PollutionTile tile, float spreadRate, float strength, float resistance)
        {
            tile.pollutionSpreadRate += spreadRate;
            tile.pollutionStrength += strength;
            tile.pollutionResistance += resistance;

            GridEvents.PollutionUpdated(tile.position);
        }
        /// <summary>
        /// Creates a pollution source at a position.
        /// </summary>
        public void CreateSource(Vector2Int pos, PollutionType pollutionType, SourceTier tier, float hp, float emissionRate, float pulseRate, float dormantDuration)
        {
            var source = new PollutionSource(pos, emissionRate, emissionRate, emissionRate * 0.5f, pulseRate, dormantDuration, hp);

            gameGrid.SetEntity(source);

            pollutionSources.Add(source);
            activeSources.Add(source);

            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Starts pulse coroutines for all sources.
        /// </summary>
        public void StartAllSourcePulses()
        {
            foreach (var source in pollutionSources)
            {
                StartCoroutine(SourcePulseCoroutine(source));
            }
        }

        /// <summary>
        /// Removes a pollution tile or source at a position.
        /// Cleans up connections and tracking lists.
        /// </summary>
        public void RemovePollutionAt(Vector2Int pos)
        {
            // Remove PollutionTile if it exists
            PollutionTile tile = gameGrid.GetEntity<PollutionTile>(pos);
            if (tile != null)
            {
                // Disconnect from connected sources
                foreach (var source in tile.connectedSources)
                {
                    source.connectedTiles.Remove(tile);
                }
                tile.connectedSources.Clear();

                pollutedTiles.Remove(pos);

                // Remove from grid by type
                gameGrid.RemoveEntity(tile);
            }

            // Remove PollutionSource if it exists
            PollutionSource sourceEntity = gameGrid.GetEntity<PollutionSource>(pos);
            if (sourceEntity != null)
            {
                // Disconnect from connected tiles
                foreach (var connectedTile in sourceEntity.connectedTiles)
                {
                    connectedTile.connectedSources.Remove(sourceEntity);
                }
                sourceEntity.connectedTiles.Clear();

                pollutionSources.Remove(sourceEntity);
                activeSources.Remove(sourceEntity);

                // Remove from grid by type
                gameGrid.RemoveEntity(sourceEntity);
            }

            // Notify the system
            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Damages a pollution tile proportionally to the specified amount.
        /// </summary>
        public void DamagePollutionAt(Vector2Int pos, float damage)
        {
            var tile = gameGrid.GetEntity<PollutionTile>(pos);
            if (tile == null) return;

            tile.isFrozen = true;
            float total = tile.GetTotalPollution();
            if (total > 0f)
            {
                float ratio = Mathf.Min(damage / total, 1f);
                tile.pollutionSpreadRate *= 1f - ratio;
                tile.pollutionStrength *= 1f - ratio;
                tile.pollutionResistance *= 1f - ratio;
            }

            if (tile.GetTotalPollution() < 0.1f)
                RemovePollutionAt(pos);
            else
            {
                tile.isFrozen = false;
            }
            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Returns the total pollution at a position (tile or source).
        /// </summary>
        public float GetPollutionLevelAt(Vector2Int pos)
        {
            var tile = gameGrid.GetEntity<PollutionTile>(pos);
            if (tile != null) return tile.GetTotalPollution();

            var source = gameGrid.GetEntity<PollutionSource>(pos);
            return source != null ? source.GetTotalPollution() : 0f;
        }

        /// <summary>
        /// Freezes a pollution tile at a position.
        /// </summary>
        public void FreezeTileAt(Vector2Int pos)
        {
            var tile = gameGrid.GetEntity<PollutionTile>(pos);
            if (tile == null) return;

            tile.isFrozen = true;
            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Coroutine that handles a single source's pulse cycle.
        /// 
        /// Lifecycle:
        /// 1. Wait for dormant period (source inactive)
        /// 2. Pulse repeatedly at source's individual rate (source active)
        /// 3. Stop if source is destroyed
        /// 
        /// Each source runs its own coroutine independently.
        /// </summary>
        private IEnumerator SourcePulseCoroutine(PollutionSource source)
        {
            // Phase 1: Dormant period
            while (source.timeSinceCreation < source.dormantDuration)
            {
                source.timeSinceCreation += Time.deltaTime;
                yield return null; // Wait one frame
            }

            // Phase 2: Active pulsing
            while (pollutionSources.Contains(source))
            {
                source.Pulse();
                yield return new WaitForSeconds(source.pulseRate);
            }
        }

        /// <summary>
        /// Resets the pollution system.
        /// </summary>
        public void ResetPollutionSystem()
        {
            StopAllCoroutines();
            foreach (var pos in new List<Vector2Int>(pollutedTiles.Keys))
                RemovePollutionAt(pos);

            foreach (var source in new List<PollutionSource>(pollutionSources))
                RemovePollutionAt(source.position);

            pollutedTiles.Clear();
            pollutionSources.Clear();
            activeSources.Clear();
        }
    }
}
