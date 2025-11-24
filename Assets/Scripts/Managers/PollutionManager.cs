using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Central manager for the pollution system.
    /// Handles pollution grid management, source/tile lifecycle, and coordination with other systems.
    /// 
    /// Dual Grid System:
    /// - PollutionManager.grid: Stores actual pollution entities (sources and tiles)
    /// - GridSystem: Stores tile states and entity references (synchronized with PollutionManager.grid)
    /// </summary>
    public class PollutionManager : MonoBehaviour
    {
        // === Singleton Pattern ===
        public static PollutionManager Instance { get; private set; }

        // === Pollution Grid ===
        public object[,] grid;      // 2D array storing PollutionSource and PollutionTile objects
        public int gridWidth;
        public int gridHeight;

        // === Cross-System References ===
        public GridSystem gridSystem;       // Reference to the main grid system (for tile state queries)
        public PlantManager plantManager;   // Reference to plant system (for killing plants)

        // === Pollution Entity Tracking ===
        public List<PollutionSource> pollutionSources = new List<PollutionSource>();  // All sources (including dormant)
        public List<PollutionSource> activeSources = new List<PollutionSource>();     // Active sources only
        public Dictionary<Vector2Int, PollutionTile> pollutedTiles = new Dictionary<Vector2Int, PollutionTile>(); // All pollution tiles

        void Awake()
        {
            // Singleton initialization
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
        public void CreateGrid(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            grid = new object[width, height];
        }

        /// <summary>
        /// Returns all 4-directional neighbors of a position (up, down, left, right).
        /// Only returns positions that are within grid bounds.
        /// </summary>
        public List<Vector2Int> GetAdjacentPositions(Vector2Int pos)
        {
            List<Vector2Int> adjacentPositions = new List<Vector2Int>();

            // 4-directional movement vectors
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1),  // Down
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(1, 0)    // Right
            };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = pos + dir;

                // Only add if within bounds
                if (neighbor.x >= 0 && neighbor.x < gridWidth &&
                    neighbor.y >= 0 && neighbor.y < gridHeight)
                {
                    adjacentPositions.Add(neighbor);
                }
            }

            return adjacentPositions;
        }

        /// <summary>
        /// Creates a new pollution tile at the specified position with the given stats.
        /// Synchronizes with GridSystem to keep both grids consistent.
        /// </summary>
        /// <returns>The newly created tile</returns>
        public PollutionTile AddPollutionToPosition(Vector2Int pos, float spreadRate, float strength, float resistance)
        {
            // Create and configure new tile
            PollutionTile newTile = new PollutionTile(pos);
            newTile.pollutionSpreadRate = spreadRate;
            newTile.pollutionStrength = strength;
            newTile.pollutionResistance = resistance;
            newTile.isFrozen = false; // New tiles start unfrozen

            // Add to PollutionManager grid
            grid[pos.x, pos.y] = newTile;

            // Track in dictionary for quick lookup
            pollutedTiles[pos] = newTile;

            // Synchronize with GridSystem
            if (gridSystem != null)
            {
                gridSystem.SetTileState(pos, TileState.Pollution);
                gridSystem.SetEntity(pos, newTile);
            }
            GridEvents.PollutionUpdated(pos);
            return newTile;
        }

        /// <summary>
        /// Utility method for UI/testing: Gets existing tile or creates a new one with default values.
        /// </summary>
        public PollutionTile GetOrCreateTile(Vector2Int pos)
        {
            // Bounds check
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
            {
                Debug.LogWarning($"Position {pos} is out of bounds!");
                return null;
            }

            // Return existing tile if present
            object existing = grid[pos.x, pos.y];
            if (existing is PollutionTile existingTile)
            {
                return existingTile;
            }

            // Create new tile with default values (for testing)
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
        /// Legacy method for creating pollution sources (simple version without GameManager parameters).
        /// </summary>
        public void AddPollutionSource(Vector2Int pos, float spreadRate, float strength, float resistance, float pulseRate, float dormantDuration)
        {
            // Create source with default HP
            PollutionSource source = new PollutionSource(pos, spreadRate, strength, resistance, pulseRate, dormantDuration);

            // Add to grid
            grid[pos.x, pos.y] = source;

            // Track in lists
            pollutionSources.Add(source);
            activeSources.Add(source);
        }

        /// <summary>
        /// Creates a pollution source (called by GameManager during game initialization).
        /// 
        /// Note: pollutionType and tier parameters are legacy and ignored. 
        /// The source stats are configured directly from emissionRate.
        /// </summary>
        public void CreateSource(Vector2Int pos, PollutionType pollutionType, SourceTier tier, float hp, float emissionRate, float tickInterval, float dormantDuration)
        {
            // Create source (type/tier parameters ignored, using emissionRate for all stats)
            PollutionSource source = new PollutionSource(
                pos,
                emissionRate,           // SpreadRate
                emissionRate,           // Strength
                emissionRate * 0.5f,    // Resistance (half of emission rate)
                tickInterval,           // Pulse rate (how often it emits)
                dormantDuration,        // Dormant period before activation
                hp                      // Hit points
            );

            // Add to PollutionManager grid
            grid[pos.x, pos.y] = source;

            // Track in lists
            pollutionSources.Add(source);
            activeSources.Add(source);

            // Synchronize with GridSystem
            if (gridSystem != null)
            {
                gridSystem.SetTileState(pos, TileState.PollutionSource);
                gridSystem.SetEntity(pos, source);
            }
            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Starts all source pulse coroutines.
        /// Each source pulses independently at its own rate after its own dormant period.
        /// Called by GameManager after sources are created.
        /// </summary>
        public void StartAllSourcePulses()
        {
            foreach (PollutionSource source in pollutionSources)
            {
                StartCoroutine(SourcePulseCoroutine(source));
            }
        }

        /// <summary>
        /// Resets the entire pollution system to initial state.
        /// Stops all coroutines, clears all entities, and nulls the grid.
        /// Called by GameManager during game restart.
        /// </summary>
        public void ResetPollutionSystem()
        {
            // Stop all pulse coroutines
            StopAllCoroutines();

            // Clear tracking structures
            pollutionSources.Clear();
            activeSources.Clear();
            pollutedTiles.Clear();

            // Clear the grid
            if (grid != null)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        grid[x, y] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a pollution entity (tile or source) at the specified position.
        /// Handles cleanup of bidirectional connections and GridSystem synchronization.
        /// </summary>
        public void RemovePollutionAt(int x, int y)
        {
            // Bounds check
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];
            Vector2Int pos = new Vector2Int(x, y);

            // --- Remove Pollution Tile ---
            if (gridObject is PollutionTile tile)
            {
                // Clean up bidirectional source connections
                foreach (PollutionSource source in tile.connectedSources)
                {
                    source.connectedTiles.Remove(tile);
                }
                tile.connectedSources.Clear();

                // Remove from PollutionManager grid
                grid[x, y] = null;

                // Remove from tracking dictionary
                pollutedTiles.Remove(pos);

                // Synchronize with GridSystem
                if (gridSystem != null)
                {
                    gridSystem.SetTileState(pos, TileState.Empty);
                    gridSystem.RemoveEntity(pos);
                }
            }
            // --- Remove Pollution Source ---
            else if (gridObject is PollutionSource source)
            {
                // Remove from tracking lists
                pollutionSources.Remove(source);
                activeSources.Remove(source);

                // Remove from PollutionManager grid
                grid[x, y] = null;

                // Synchronize with GridSystem
                if (gridSystem != null)
                {
                    gridSystem.SetTileState(pos, TileState.Empty);
                    gridSystem.RemoveEntity(pos);
                }
            }
            GridEvents.PollutionUpdated(pos);
        }

        /// <summary>
        /// Convenience method: Removes pollution at a Vector2Int position.
        /// </summary>
        public void RemovePollutionTile(Vector2Int pos)
        {
            RemovePollutionAt(pos.x, pos.y);
        }

        /// <summary>
        /// Reduces pollution by a percentage (0-100).
        /// Legacy method - not currently used (DamagePollutionAt is preferred).
        /// </summary>
        public void ReducePollutionAt(int x, int y, float percentage)
        {
            // Bounds check
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionTile tile)
            {
                // Calculate reduction factor
                float reductionFactor = 1f - (percentage / 100f);
                tile.pollutionSpreadRate *= reductionFactor;
                tile.pollutionStrength *= reductionFactor;
                tile.pollutionResistance *= reductionFactor;

                // Remove if too weak
                if (tile.GetTotalPollution() < 0.1f)
                {
                    RemovePollutionAt(x, y);
                }
                else
                {
                    tile.isFrozen = false; // Unfreeze for continued operation
                }
            }
        }

        /// <summary>
        /// Damages a pollution tile by a fixed amount (reduces all stats proportionally).
        /// Freezes the tile during damage application to prevent race conditions.
        /// Called by plants when extracting resources from adjacent pollution.
        /// </summary>
        public void DamagePollutionAt(Vector2Int pos, float damage)
        {
            // Bounds check
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[pos.x, pos.y];

            if (gridObject is PollutionTile tile)
            {
                // Freeze to prevent spreading/receiving while being modified
                tile.isFrozen = true;

                // Calculate proportional reduction
                float totalPollution = tile.GetTotalPollution();
                if (totalPollution > 0)
                {
                    float ratio = damage / totalPollution;
                    float reduction = Mathf.Min(ratio, 1.0f); // Cap at 100%

                    tile.pollutionSpreadRate *= (1f - reduction);
                    tile.pollutionStrength *= (1f - reduction);
                    tile.pollutionResistance *= (1f - reduction);
                }

                // Remove if too weak
                if (tile.GetTotalPollution() < 0.1f)
                {
                    RemovePollutionAt(pos.x, pos.y);
                }
                else
                {
                    tile.isFrozen = false; // Unfreeze for continued operation
                }
                GridEvents.PollutionUpdated(pos);
            }
        }

        /// <summary>
        /// Removes a pollution source and cleans up all bidirectional tile connections.
        /// Synchronizes with GridSystem.
        /// Called by PlantManager when a source is destroyed.
        /// </summary>
        public void RemoveSourceAt(int x, int y)
        {
            // Bounds check
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];
            Vector2Int pos = new Vector2Int(x, y);

            if (gridObject is PollutionSource source)
            {
                // Clean up bidirectional tile connections
                foreach (PollutionTile tile in source.connectedTiles)
                {
                    tile.connectedSources.Remove(source);
                }
                source.connectedTiles.Clear();

                // Remove from tracking lists
                pollutionSources.Remove(source);
                activeSources.Remove(source);

                // Remove from PollutionManager grid
                grid[x, y] = null;

                // Synchronize with GridSystem
                if (gridSystem != null)
                {
                    gridSystem.SetTileState(pos, TileState.Empty);
                    gridSystem.RemoveEntity(pos);
                }
            }
        }

        /// <summary>
        /// Returns the total pollution level at a position.
        /// Works for both tiles and sources.
        /// Returns 0 if position is empty or out of bounds.
        /// </summary>
        public float GetPollutionLevelAt(Vector2Int pos)
        {
            // Bounds check
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
            {
                return 0f;
            }

            object gridObject = grid[pos.x, pos.y];

            if (gridObject is PollutionTile tile)
            {
                return tile.GetTotalPollution();
            }
            else if (gridObject is PollutionSource source)
            {
                return source.GetTotalPollution();
            }

            return 0f;
        }

        /// <summary>
        /// Freezes a pollution tile at the specified position.
        /// Frozen tiles cannot spread or receive pollution (used for thread safety).
        /// </summary>
        public void FreezeTileAt(int x, int y)
        {
            // Bounds check
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionTile tile)
            {
                tile.isFrozen = true;
                GridEvents.PollutionUpdated(tile.position);
            }
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
        System.Collections.IEnumerator SourcePulseCoroutine(PollutionSource source)
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
    }
}
