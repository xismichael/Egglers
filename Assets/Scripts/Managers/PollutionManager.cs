using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public class PollutionManager : MonoBehaviour
    {
        public static PollutionManager Instance { get; private set; }
        //plantManager reference
        public PlantBitManager plantManager;

        public List<PollutionSource> pollutionSources = new List<PollutionSource>();
        //gameGrid reference
        public GridSystem gameGrid;

        int gridWidth;
        int gridHeight;


        public void Initialize(GridSystem sharedGrid)
        {
            gameGrid = sharedGrid;
        }

        public void InitializeGridSize(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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

            PollutionTile newTile = gameGrid.GetOrCreatePollutionTile(pos, spreadRate, strength, resistance);
            GridEvents.PollutionUpdated(pos);

            return newTile;
        }

        public void AddPollutionToTile(PollutionTile tile, float spreadRate, float strength, float resistance)
        {
            // Add pollution to existing tile
            tile.pollutionSpreadRate += spreadRate;
            tile.pollutionStrength += strength;
            tile.pollutionResistance += resistance;
            GridEvents.PollutionUpdated(tile.position);
        }

        public void AddPollutionSource(Vector2Int pos, float spreadRate, float strength, float resistance, float pulseRate, float dormantDuration)
        {
            // Create new pollution source
            PollutionSource source = new PollutionSource(pos, spreadRate, strength, resistance, pulseRate, dormantDuration);

            // Add to sources list
            pollutionSources.Add(source);

            // Add to grid
            gameGrid.SetEntity(source);
            GridEvents.PollutionUpdated(pos);
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


            //add further cleanup logic later

        }

        // Helper functions for plant manager to interact with pollution
        public void RemovePollutionAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            PollutionTile pollutionTile = gameGrid.GetEntity<PollutionTile>(new Vector2Int(x, y));

            if (pollutionTile != null)
            {
                // Disconnect from all sources
                foreach (PollutionSource source in pollutionTile.connectedSources)
                {
                    source.connectedTiles.Remove(pollutionTile);
                }
                pollutionTile.connectedSources.Clear();

                gameGrid.RemoveEntity(pollutionTile);
                GridEvents.PollutionUpdated(new Vector2Int(x, y));
            }

            //check pollution source at position
            PollutionSource pollutionSource = gameGrid.GetEntity<PollutionSource>(new Vector2Int(x, y));
            if (pollutionSource != null)
            {
                RemoveSource(pollutionSource);
            }


        }

        public void ReducePollutionAt(int x, int y, float percentage)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            PollutionTile pollutionTile = gameGrid.GetEntity<PollutionTile>(new Vector2Int(x, y));
            if (pollutionTile == null) return;

            // Freeze tile to prevent it from spreading/receiving while being reduced
            pollutionTile.isFrozen = true;

            // Reduce by percentage
            float reductionFactor = 1f - (percentage / 100f);
            pollutionTile.pollutionSpreadRate *= reductionFactor;
            pollutionTile.pollutionStrength *= reductionFactor;
            pollutionTile.pollutionResistance *= reductionFactor;

            // Remove if pollution is too low
            if (pollutionTile.GetTotalPollution() < 0.1f)
            {
                RemovePollutionAt(x, y);
            }
            else
            {
                GridEvents.PollutionUpdated(new Vector2Int(x, y));
            }

        }

        public void RemoveSourceAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            PollutionSource pollutionSource = gameGrid.GetEntity<PollutionSource>(new Vector2Int(x, y));
            if (pollutionSource == null) return;
            RemoveSource(pollutionSource);
        }

        public void RemoveSource(PollutionSource pollutionSource)
        {
            if (pollutionSource == null) return;

            pollutionSources.Remove(pollutionSource);

            Vector2Int pos = pollutionSource.position;
            //remove all connected tiles from source
            foreach (PollutionTile tile in pollutionSource.connectedTiles)
            {
                tile.connectedSources.Remove(pollutionSource);
            }
            pollutionSource.connectedTiles.Clear();
            gameGrid.RemoveEntity(pollutionSource);
            GridEvents.PollutionUpdated(pos);

        }

        public void FreezeTileAt(int x, int y)
        {
            // Check bounds
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return;
            }

            PollutionTile pollutionTile = gameGrid.GetEntity<PollutionTile>(new Vector2Int(x, y));
            if (pollutionTile == null) return;
            pollutionTile.isFrozen = true;
        }

        System.Collections.IEnumerator SourcePulseCoroutine(PollutionSource source)
        {
            GridEvents.PollutionUpdated(source.position);
            // Wait for dormant period
            while (source.timeSinceCreation < source.dormantDuration)
            {
                source.timeSinceCreation += Time.deltaTime;
                yield return null;
            }
            GridEvents.PollutionUpdated(source.position);

            // Continuously pulse at the source's rate
            while (pollutionSources.Contains(source))
            {
                source.Pulse();
                yield return new WaitForSeconds(source.pulseRate);
            }
        }

        public float GetPollutionLevelAt(Vector2Int pos)
        {
            PollutionTile pollutionTile = gameGrid.GetEntity<PollutionTile>(pos);
            PollutionSource pollutionSource = gameGrid.GetEntity<PollutionSource>(pos);
            if (pollutionTile == null && pollutionSource == null) return 0f;
            if (pollutionTile != null) return pollutionTile.GetTotalPollution();
            return pollutionSource.GetTotalPollution();
        }
    }
}
