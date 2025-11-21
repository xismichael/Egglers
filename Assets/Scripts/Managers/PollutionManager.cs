using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class PollutionManager : MonoBehaviour
    {
        public static PollutionManager Instance { get; private set; }

        public object[,] grid;
        public int gridWidth;
        public int gridHeight;

        public List<PollutionSource> pollutionSources = new List<PollutionSource>();

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void CreateGrid(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            grid = new object[width, height];
        }

        public List<Vector2Int> GetAdjacentPositions(Vector2Int pos)
        {
            List<Vector2Int> adjacentPositions = new List<Vector2Int>();

            // 4-directional: up, down, left, right
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = pos + dir;
                
                // Check if in bounds
                if (neighbor.x >= 0 && neighbor.x < gridWidth && 
                    neighbor.y >= 0 && neighbor.y < gridHeight)
                {
                    adjacentPositions.Add(neighbor);
                }
            }

            return adjacentPositions;
        }

        public PollutionTile AddPollutionToPosition(Vector2Int pos, float spreadRate, float strength, float resistance)
        {
            // Create new PollutionTile at this position
            PollutionTile newTile = new PollutionTile(pos);
            newTile.pollutionSpreadRate = spreadRate;
            newTile.pollutionStrength = strength;
            newTile.pollutionResistance = resistance;
            newTile.isFrozen = false; // Tiles start unfrozen

            // Add to grid
            grid[pos.x, pos.y] = newTile;
            
            return newTile;
        }

        public void AddPollutionToTile(PollutionTile tile, float spreadRate, float strength, float resistance)
        {
            // Add pollution to existing tile
            tile.pollutionSpreadRate += spreadRate;
            tile.pollutionStrength += strength;
            tile.pollutionResistance += resistance;
        }

        public void AddPollutionSource(Vector2Int pos, float spreadRate, float strength, float resistance, float pulseRate, float dormantDuration)
        {
            // Create new pollution source
            PollutionSource source = new PollutionSource(pos, spreadRate, strength, resistance, pulseRate, dormantDuration);
            
            // Add to grid
            grid[pos.x, pos.y] = source;
            
            // Add to sources list
            pollutionSources.Add(source);
        }

        public void StartAllSourcePulses()
        {
            foreach (PollutionSource source in pollutionSources)
            {
                StartCoroutine(SourcePulseCoroutine(source));
            }
        }

        public void ResetPollutionSystem()
        {
            // Stop all coroutines
            StopAllCoroutines();
            
            // Clear all data structures
            pollutionSources.Clear();
            
            // Clear grid
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

        // Helper functions for plant manager to interact with pollution
        public void RemovePollutionAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionTile tile)
            {
                // Disconnect from all sources
                foreach (PollutionSource source in tile.connectedSources)
                {
                    source.connectedTiles.Remove(tile);
                }
                tile.connectedSources.Clear();
                
                grid[x, y] = null;
            }
            else if (gridObject is PollutionSource source)
            {
                pollutionSources.Remove(source);
                grid[x, y] = null;
            }
        }

        public void ReducePollutionAt(int x, int y, float percentage)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionTile tile)
            {
                // Freeze tile to prevent it from spreading/receiving while being reduced
                tile.isFrozen = true;
                
                // Reduce by percentage
                float reductionFactor = 1f - (percentage / 100f);
                tile.pollutionSpreadRate *= reductionFactor;
                tile.pollutionStrength *= reductionFactor;
                tile.pollutionResistance *= reductionFactor;

                // Remove if pollution is too low
                if (tile.GetTotalPollution() < 0.1f)
                {
                    RemovePollutionAt(x, y);
                }
            }
        }

        public void RemoveSourceAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionSource source)
            {
                pollutionSources.Remove(source);
                grid[x, y] = null;
            }
        }

        public void FreezeTileAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            object gridObject = grid[x, y];

            if (gridObject is PollutionTile tile)
            {
                tile.isFrozen = true;
            }
        }

        System.Collections.IEnumerator SourcePulseCoroutine(PollutionSource source)
        {
            // Wait for dormant period
            while (source.timeSinceCreation < source.dormantDuration)
            {
                source.timeSinceCreation += Time.deltaTime;
                yield return null;
            }
            
            // Continuously pulse at the source's rate
            while (pollutionSources.Contains(source))
            {
                source.Pulse();
                yield return new WaitForSeconds(source.pulseRate);
            }
        }
    }
}
