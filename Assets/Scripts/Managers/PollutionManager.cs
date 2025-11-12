using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class PollutionManager : MonoBehaviour
    {
        // Grid reference
        public GridSystem gridSystem;
        public PlantManager plantManager;

        // Pollution tracking
        public Dictionary<Vector2Int, PollutionTile> pollutedTiles = new Dictionary<Vector2Int, PollutionTile>();
        public List<PollutionSource> activeSources = new List<PollutionSource>();

        // Configuration
        [Header("Pollution Configuration")]
        public float baseSpreadRate = 1.0f;
        public float pollutionTickRate = 7.0f; // 5-10 seconds

        // Game time tracking for source awakening
        private float gameTime = 0f;

        private void Update()
        {
            gameTime += Time.deltaTime;
        }

        public PollutionSource CreateSource(Vector2Int position, PollutionType type, SourceTier tier, float hp, float emission, float tickInterval, float dormantDuration = 0f)
        {
            PollutionSource source = new PollutionSource(position, type, tier, hp, emission, tickInterval, dormantDuration);
            source.pollutionManager = this;
            source.gridSystem = gridSystem;

            activeSources.Add(source);
            gridSystem.SetTileState(position, TileState.PollutionSource);
            gridSystem.SetEntity(position, source);

            return source;
        }

        public void RemoveSource(PollutionSource source)
        {
            if (activeSources.Contains(source))
            {
                activeSources.Remove(source);
                gridSystem.SetTileState(source.position, TileState.Empty);
                gridSystem.RemoveEntity(source.position);
            }
        }

        public PollutionTile GetOrCreateTile(Vector2Int pos)
        {
            if (pollutedTiles.ContainsKey(pos))
            {
                return pollutedTiles[pos];
            }

            // Create new tile
            PollutionTile newTile = new PollutionTile(pos);
            newTile.hopsFromSource = int.MaxValue; // Init to max
            newTile.baseSpreadRate = baseSpreadRate;

            gridSystem.SetTileState(pos, TileState.Pollution);
            gridSystem.SetEntity(pos, newTile);
            pollutedTiles[pos] = newTile;

            return newTile;
        }

        public void RemovePollutionTile(Vector2Int pos)
        {
            if (pollutedTiles.ContainsKey(pos))
            {
                pollutedTiles.Remove(pos);
                gridSystem.SetTileState(pos, TileState.Empty);
                gridSystem.RemoveEntity(pos);
            }
        }

        public float GetPollutionLevelAt(Vector2Int pos)
        {
            if (pollutedTiles.TryGetValue(pos, out PollutionTile tile))
            {
                return tile.totalPollutionLevel;
            }
            return 0f;
        }

        public void UpdatePollutionSpread()
        {
            // Check source awakening
            foreach (PollutionSource source in activeSources)
            {
                source.CheckAwakening(gameTime);
            }

            // 1. Sources emit to adjacent tiles only
            foreach (PollutionSource source in activeSources)
            {
                source.OnTick();
            }

            // 2. Tiles spread to neighbors
            List<SpreadOperation> spreads = new List<SpreadOperation>();

            // Make a copy to avoid modifying collection during iteration
            List<PollutionTile> tilesToProcess = new List<PollutionTile>(pollutedTiles.Values);

            foreach (PollutionTile tile in tilesToProcess)
            {
                tile.RecalculateStats();

                List<Vector2Int> neighbors = gridSystem.GetNeighbors(tile.position, includeDiagonal: false);

                foreach (Vector2Int neighborPos in neighbors)
                {
                    TileState state = gridSystem.GetTileState(neighborPos);

                    // Can spread to empty or pollution tiles
                    if (state == TileState.Empty || state == TileState.Pollution)
                    {
                        // Distance-based decay
                        float decay = 1.0f / (1.0f + tile.hopsFromSource * 0.2f);
                        float spreadAmount = tile.spreadSpeed * decay;

                        // Get or prepare neighbor tile
                        PollutionTile neighbor = GetOrCreateTile(neighborPos);

                        // Fountain rule: only spread if neighbor has LESS total pollution
                        if (neighbor.totalPollutionLevel < tile.totalPollutionLevel)
                        {
                            SpreadOperation spread = new SpreadOperation();
                            spread.targetPosition = neighborPos;
                            spread.pollutionType = tile.dominantType;
                            spread.amount = spreadAmount;
                            spread.hops = tile.hopsFromSource + 1;

                            spreads.Add(spread);
                        }
                    }
                }
            }

            // 3. Apply all spreads (allows blending from multiple sources)
            foreach (SpreadOperation spread in spreads)
            {
                if (pollutedTiles.TryGetValue(spread.targetPosition, out PollutionTile targetTile))
                {
                    targetTile.AddPollution(spread.pollutionType, spread.amount);
                    targetTile.hopsFromSource = Mathf.Min(targetTile.hopsFromSource, spread.hops);
                }
            }

            // 4. Check for tiles that should be removed
            List<Vector2Int> tilesToRemove = new List<Vector2Int>();
            foreach (var kvp in pollutedTiles)
            {
                if (kvp.Value.ShouldBeRemoved())
                {
                    tilesToRemove.Add(kvp.Key);
                }
            }

            foreach (Vector2Int pos in tilesToRemove)
            {
                RemovePollutionTile(pos);
            }
        }

        public void CheckHeartOverwhelm()
        {
            if (plantManager == null || plantManager.heartPlant == null)
            {
                return;
            }

            Plant heart = plantManager.heartPlant;
            List<Vector2Int> neighbors = gridSystem.GetNeighbors(heart.position, includeDiagonal: false);

            foreach (Vector2Int neighborPos in neighbors)
            {
                if (gridSystem.GetTileState(neighborPos) == TileState.Pollution)
                {
                    PollutionTile tile = gridSystem.GetEntity<PollutionTile>(neighborPos);
                    if (tile != null)
                    {
                        float effectiveATD = heart.attackDamage;

                        // Acidic modifier
                        if (tile.dominantType == PollutionType.Acidic)
                        {
                            effectiveATD *= 0.67f;
                        }

                        // Loss condition: ANY adjacent pollution stronger than Heart
                        if (effectiveATD <= tile.attackDamage)
                        {
                            // Trigger loss condition
                            GameManager gameManager = FindObjectOfType<GameManager>();
                            if (gameManager != null)
                            {
                                gameManager.TriggerLoss();
                            }
                            return;
                        }
                    }
                }
            }
        }
    }
}

