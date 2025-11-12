using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class Plant
    {
        // Position on grid
        public Vector2Int position;

        // Natural components (exponential scaling)
        public int leafAmount;
        public int rootAmount;
        public int fruitAmount;

        // Grafted components (linear scaling)
        public int graftedLeafAmount;
        public int graftedRootAmount;
        public int graftedFruitAmount;

        // Capacity and hierarchy
        public int maxComponentAmount;
        public Plant parentPlant;
        public List<Plant> children = new List<Plant>();

        // State
        public PlantPhase phase;
        public bool hasAutoSprouted;
        public bool isHeart;

        // Cooldowns
        public float graftingCooldown;

        // Bud growth tracking
        public int growthProgress;
        public int growthDuration;
        public float resourcePerTick;
        public float sproutGrowthCost;

        // Derived stats (calculated)
        public float attackDamage;
        public float resourceExtractionRate;
        public float resourceStorage;

        // References to managers (set externally)
        public PlantManager plantManager;
        public GridSystem gridSystem;

        // Configuration values (set from config)
        public float leafMultiplier = 1.0f;
        public float rootMultiplier = 1.0f;
        public float fruitMultiplier = 1.0f;

        public int TotalNaturalComponents => leafAmount + rootAmount + fruitAmount;
        public int TotalGraftedComponents => graftedLeafAmount + graftedRootAmount + graftedFruitAmount;
        public int TotalComponents => TotalNaturalComponents + TotalGraftedComponents;

        public Plant()
        {
            phase = PlantPhase.Bud;
            hasAutoSprouted = false;
            isHeart = false;
            graftingCooldown = 0f;
            growthProgress = 0;
        }

        public void RecalculateStats()
        {
            attackDamage = CalculateStat(leafAmount, graftedLeafAmount, leafMultiplier);
            resourceExtractionRate = CalculateStat(rootAmount, graftedRootAmount, rootMultiplier);
            resourceStorage = CalculateStat(fruitAmount, graftedFruitAmount, fruitMultiplier);
        }

        private float CalculateStat(int natural, int grafted, float baseMultiplier)
        {
            // Exponential for natural components (power of 1.3)
            float naturalPower = Mathf.Pow(natural, 1.3f) * baseMultiplier;

            // Linear for grafted components
            float graftedPower = grafted * 1.0f;

            return naturalPower + graftedPower;
        }

        public void TransitionToGrownPhase()
        {
            phase = PlantPhase.Grown;
            RecalculateStats();

            // One-time auto-sprout
            if (!hasAutoSprouted)
            {
                hasAutoSprouted = true;
                AttemptAutoSprout();
            }
        }

        private void AttemptAutoSprout()
        {
            List<Vector2Int> neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);
            
            foreach (Vector2Int neighborPos in neighbors)
            {
                if (IsValidSproutTarget(neighborPos))
                {
                    plantManager.CreateSprout(this, neighborPos);
                }
            }
        }

        public bool IsValidSproutTarget(Vector2Int target)
        {
            TileState state = gridSystem.GetTileState(target);

            // Empty tiles always valid
            if (state == TileState.Empty)
            {
                return true;
            }

            // Polluted tiles: need ATD > pollution attackDamage
            if (state == TileState.Pollution)
            {
                PollutionTile tile = gridSystem.GetEntity<PollutionTile>(target);
                if (tile != null)
                {
                    return attackDamage > tile.attackDamage;
                }
            }

            return false;
        }

        public bool IsValidManualSprout(Vector2Int target)
        {
            // Must be adjacent
            if (!gridSystem.AreAdjacent(position, target))
            {
                return false;
            }

            TileState state = gridSystem.GetTileState(target);

            // Cannot have plant, source, or heart
            if (state == TileState.Plant || state == TileState.PollutionSource || state == TileState.Heart)
            {
                return false;
            }

            // Empty tiles always valid
            if (state == TileState.Empty)
            {
                return true;
            }

            // Polluted tiles: need ATD > pollution
            if (state == TileState.Pollution)
            {
                PollutionTile tile = gridSystem.GetEntity<PollutionTile>(target);
                if (tile != null)
                {
                    return attackDamage > tile.attackDamage;
                }
            }

            return false;
        }

        public void CheckOverwhelm()
        {
            List<Vector2Int> neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);

            foreach (Vector2Int neighborPos in neighbors)
            {
                if (gridSystem.GetTileState(neighborPos) == TileState.Pollution)
                {
                    PollutionTile tile = gridSystem.GetEntity<PollutionTile>(neighborPos);
                    if (tile != null)
                    {
                        float effectiveATD;

                        if (phase == PlantPhase.Bud)
                        {
                            // Buds use parent's CURRENT ATD (dynamic)
                            if (parentPlant == null)
                            {
                                continue; // Safety check
                            }
                            effectiveATD = parentPlant.attackDamage;
                        }
                        else
                        {
                            // Grown plants use own ATD
                            effectiveATD = attackDamage;
                        }

                        // Acidic pollution requires more leaves to resist
                        if (tile.dominantType == PollutionType.Acidic)
                        {
                            effectiveATD *= 0.67f;
                        }

                        // Pollution overwhelms if ATD <= pollution attack
                        if (effectiveATD <= tile.attackDamage)
                        {
                            plantManager.DeletePlant(this, fromPollution: true);
                            return;
                        }
                    }
                }
            }
        }

        public void ExtractFromAdjacentPollution()
        {
            List<Vector2Int> neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);

            foreach (Vector2Int neighborPos in neighbors)
            {
                if (gridSystem.GetTileState(neighborPos) == TileState.Pollution)
                {
                    PollutionTile tile = gridSystem.GetEntity<PollutionTile>(neighborPos);
                    if (tile != null && attackDamage > tile.attackDamage)
                    {
                        // Extract resources
                        float resourceGain = tile.totalPollutionLevel * 0.1f * resourceExtractionRate;

                        // Apply type modifier
                        resourceGain *= tile.GetExtractionMultiplier();

                        plantManager.AddResource(resourceGain);

                        // Reduce pollution
                        float pollutionReduction = (attackDamage - tile.attackDamage) * 0.05f;
                        tile.TakeDamage(pollutionReduction);

                        // Check if tile should be removed
                        if (tile.ShouldBeRemoved())
                        {
                            plantManager.RemovePollutionTile(neighborPos);
                        }
                    }
                }
            }
        }
    }
}

