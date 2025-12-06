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

        /// <summary>
        /// Checks infection status of a plant and returns infection progress (0-1).
        /// Handles plant curing, full infection, and returns progress percentage.
        /// Call this every tick for infected plants.
        /// </summary>
        public float CheckPlantInfection(PlantBit plant)
        {
            PollutionTile tile = gameGrid.GetEntity<PollutionTile>(plant.position);
            
            // No pollution at plant position → cure
            if (tile == null)
            {
                plant.isInfected = false;
                GridEvents.PlantUpdated(plant.position);
                return 0f; // 0% infected
            }
            
            float pollutionStrength = tile.pollutionStrength;
            float plantAttack = plant.attackDamage;
            
            // Plant wins → remove pollution and cure
            if (plantAttack > pollutionStrength)
            {
                RemovePollutionAt(tile.position.x, tile.position.y);
                plant.isInfected = false;
                GridEvents.PlantUpdated(plant.position);
                return 0f; // Cured!
            }
            
            // Calculate infection threshold
            float maxThreshold = plantAttack * (1 + (0.5f - 0.5f * tile.pollutionResistance / 75f));
            
            // Pollution wins → fully infect
            if (pollutionStrength > maxThreshold)
            {
                plant.phase = PlantBitPhase.FullyInfected;
                tile.isFrozen = false; // Unfreeze - plant is dead, pollution can spread
                GridEvents.PlantKilledByPollution(plant.position);
                plant.InfectionSpread(tile.GetInfectionRate());
                GridEvents.PlantUpdated(plant.position);
                return 1f; // 100% infected
            }
            
            // In-between → return infection progress
            float infectionProgress = (pollutionStrength - plantAttack) / (maxThreshold - plantAttack);
            return Mathf.Clamp01(infectionProgress); // Returns 0-1 (0%-100%)
        }
    }
}
