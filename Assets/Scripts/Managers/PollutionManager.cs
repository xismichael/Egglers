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

        public List<Vector2Int> dirtyTiles = new List<Vector2Int>();
        public List<PollutionSource> pollutionSources = new List<PollutionSource>();
        
        float spreadTimer = 0f;
        public float spreadInterval = 0.5f; // spread every 0.5 seconds

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            HandleSpreadTick();
        }

        void HandleSpreadTick()
        {
            spreadTimer += Time.deltaTime;
            
            if (spreadTimer >= spreadInterval)
            {
                spreadTimer = 0f;
                SpreadAllDirtyTiles();
            }
        }

        void SpreadAllDirtyTiles()
        {
            List<Vector2Int> positionsToSpread = new List<Vector2Int>(dirtyTiles);
            dirtyTiles.Clear();
            
            foreach (var pos in positionsToSpread)
            {
                // Check if tile still exists at this position
                if (grid[pos.x, pos.y] is PollutionTile tile)
                {
                    tile.Spread();
                }
            }
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

        public void AddPollutionToPosition(Vector2Int pos, float spreadRate, float strength, float resistance)
        {
            // Create new PollutionTile at this position
            PollutionTile newTile = new PollutionTile(pos);
            newTile.pollutionSpreadRate = spreadRate;
            newTile.pollutionStrength = strength;
            newTile.pollutionResistance = resistance;

            // Add to grid
            grid[pos.x, pos.y] = newTile;
        }

        public void AddPollutionToTile(PollutionTile tile, float spreadRate, float strength, float resistance)
        {
            // Add pollution to existing tile
            tile.pollutionSpreadRate += spreadRate;
            tile.pollutionStrength += strength;
            tile.pollutionResistance += resistance;
        }

        public void MarkTileDirty(Vector2Int pos)
        {
            if (!dirtyTiles.Contains(pos))
            {
                dirtyTiles.Add(pos);
            }
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
            dirtyTiles.Clear();
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
            
            // Reset timer
            spreadTimer = 0f;
            
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
