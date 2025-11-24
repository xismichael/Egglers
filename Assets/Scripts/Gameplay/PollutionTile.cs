using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Represents a pollution tile on the grid.
    /// Pollution tiles have three stats: SpreadRate, Strength, and Resistance.
    /// They spread to adjacent tiles, can kill weak plants, and are reduced by stronger plants.
    /// </summary>
    public class PollutionTile
    {
        // === Grid Position ===
        public Vector2Int position;

        // === Core Pollution Stats ===
        public float pollutionSpreadRate;   // How fast pollution spreads (contributes to total pollution)
        public float pollutionStrength;     // Offensive power - determines if pollution can kill plants
        public float pollutionResistance;   // Defensive power - how hard it is for plants to extract from this tile
        
        // === State Management ===
        public bool isFrozen; // When true, tile cannot spread or receive pollution (used to prevent race conditions)
        
        // === Network Tracking ===
        // Tracks which sources contribute to this tile (pollution spreads from sources through connected tiles)
        public HashSet<PollutionSource> connectedSources = new HashSet<PollutionSource>();

        // === Derived Properties ===
        // Attack damage equals strength (used for combat calculations against plants)
        public float attackDamage => pollutionStrength;
        
        // Total pollution level (sum of all three stats)
        public float totalPollutionLevel => pollutionSpreadRate + pollutionStrength + pollutionResistance;
        
        /// <summary>
        /// Dynamically calculates pollution type based on stat ratios.
        /// Acidic: Strength >= 50% of total (aggressive, reduces plant defense)
        /// Sludge: Resistance >= 50% of total (persistent, hard to extract)
        /// Toxic: Neither dominates (balanced)
        /// </summary>
        public PollutionType dominantType
        {
            get
            {
                float totalPollution = GetTotalPollution();
                if (totalPollution == 0) return PollutionType.Toxic;
                
                if (pollutionStrength >= totalPollution * 0.5f)
                {
                    return PollutionType.Acidic;
                }
                else if (pollutionResistance >= totalPollution * 0.5f)
                {
                    return PollutionType.Sludge;
                }
                else
                {
                    return PollutionType.Toxic;
                }
            }
        }

        public PollutionTile(Vector2Int pos)
        {
            position = pos;
        }

        /// <summary>
        /// Returns the sum of all pollution stats.
        /// </summary>
        public float GetTotalPollution()
        {
            return pollutionSpreadRate + pollutionStrength + pollutionResistance;
        }

        /// <summary>
        /// Returns resource extraction multiplier based on pollution type.
        /// Plants gain less resources from Sludge and Acidic pollution.
        /// </summary>
        public float GetExtractionMultiplier()
        {
            switch (dominantType)
            {
                case PollutionType.Acidic:
                    return 0.8f; // Aggressive pollution is harder to extract from
                case PollutionType.Sludge:
                    return 0.5f; // Persistent pollution is very resistant to extraction
                case PollutionType.Toxic:
                default:
                    return 1.0f; // Balanced pollution has normal extraction
            }
        }

        /// <summary>
        /// Reduces all pollution stats proportionally by the specified damage amount.
        /// Used when plants extract resources from pollution.
        /// </summary>
        public void TakeDamage(float damage)
        {
            float totalPollution = GetTotalPollution();
            if (totalPollution <= 0) return;

            // Calculate reduction ratio (clamped to 100%)
            float ratio = damage / totalPollution;
            float reduction = Mathf.Min(ratio, 1.0f);

            // Reduce all stats proportionally
            pollutionSpreadRate *= (1f - reduction);
            pollutionStrength *= (1f - reduction);
            pollutionResistance *= (1f - reduction);
        }

        /// <summary>
        /// Checks if pollution is too weak to persist (total < 0.1).
        /// </summary>
        public bool ShouldBeRemoved()
        {
            return GetTotalPollution() < 0.1f;
        }

        /// <summary>
        /// Spreads pollution to adjacent tiles (4-directional).
        /// 
        /// Spread Rules:
        /// 1. Emits 10% of each stat to neighbors
        /// 2. Can kill plants weaker than this tile's attack damage
        /// 3. Cannot spread to: frozen tiles, pollution sources, the Heart, or stronger plants
        /// 4. Caps neighbors at 90% of this tile's total pollution
        /// 5. Newly created tiles inherit all connected sources
        /// </summary>
        public void Spread()
        {
            // Frozen tiles cannot spread (used for thread safety during damage application)
            if (isFrozen)
            {
                return;
            }
            
            // Get all 4-directional neighbors
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            // Calculate spread cap (neighbors can't exceed 90% of this tile's pollution)
            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f;

            // Calculate 10% of each stat to spread
            float spreadAmount_SpreadRate = pollutionSpreadRate * 0.1f;
            float spreadAmount_Strength = pollutionStrength * 0.1f;
            float spreadAmount_Resistance = pollutionResistance * 0.1f;
            float totalSpreadAmount = spreadAmount_SpreadRate + spreadAmount_Strength + spreadAmount_Resistance;

            // Pre-calculate if this tile is acidic (used for multiple plant checks)
            float tileTotalPollution = GetTotalPollution();
            bool tileIsAcidic = (tileTotalPollution > 0 && pollutionStrength >= tileTotalPollution * 0.5f);

            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                // Query GridSystem for accurate tile state
                GridSystem gridSystem = PollutionManager.Instance.gridSystem;
                TileState tileState = gridSystem.GetTileState(neighborPos);
                
                // === Handle Plants and Heart ===
                if (tileState == TileState.Plant || tileState == TileState.Heart)
                {
                    Plant plant = gridSystem.GetEntity<Plant>(neighborPos);
                    if (plant != null)
                    {
                        // --- Heart: Trigger loss if overwhelmed, but never kill ---
                        if (plant.isHeart)
                        {
                            float heartATD = plant.attackDamage;
                            
                            // Acidic pollution reduces plant defense by 33%
                            if (tileIsAcidic)
                            {
                                heartATD *= 0.67f;
                            }
                            
                            // If pollution overwhelms the Heart (tie goes to pollution), game over
                            if (heartATD <= attackDamage)
                            {
                                GameManager gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
                                if (gameManager != null)
                                {
                                    gameManager.TriggerLoss();
                                }
                            }
                            
                            // Heart can never be killed or replaced - pollution stops here
                            continue;
                        }
                        
                        // --- Regular Plants: Calculate effective defense ---
                        float effectiveATD;
                        
                        if (plant.phase == PlantPhase.Bud)
                        {
                            // Buds are protected by their parent's attack damage
                            if (plant.parentPlant == null)
                            {
                                continue; // Orphaned bud (shouldn't happen) - skip
                            }
                            effectiveATD = plant.parentPlant.attackDamage;
                        }
                        else
                        {
                            // Grown plants use their own attack damage
                            effectiveATD = plant.attackDamage;
                        }
                        
                        // Acidic pollution reduces plant defense by 33%
                        if (tileIsAcidic)
                        {
                            effectiveATD *= 0.67f;
                        }
                        
                        // --- Combat Resolution: Kill weak plants and spread ---
                        if (effectiveATD <= attackDamage)
                        {
                            // Pollution wins - delete the plant and all its children
                            PollutionManager.Instance.plantManager.DeletePlant(plant, fromPollution: true);
                            
                            // Create new pollution tile where the plant was
                            PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                            
                            // Propagate source connections to the new tile
                            foreach (PollutionSource source in connectedSources)
                            {
                                if (!newTile.connectedSources.Contains(source))
                                {
                                    newTile.connectedSources.Add(source);
                                }
                                if (!source.connectedTiles.Contains(newTile))
                                {
                                    source.connectedTiles.Add(newTile);
                                }
                            }
                        }
                        
                        // Plant is stronger - pollution cannot spread here
                        continue;
                    }
                }
                
                // === Skip Pollution Sources (pollution can't spread to sources) ===
                if (tileState == TileState.PollutionSource)
                {
                    continue;
                }
                
                // === Handle Empty Tiles and Existing Pollution ===
                object neighborObject = PollutionManager.Instance.grid[neighborPos.x, neighborPos.y];

                // --- Empty Tile: Create new pollution tile ---
                if (neighborObject == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                    
                    // Propagate all source connections to the new tile
                    foreach (PollutionSource source in connectedSources)
                    {
                        if (!newTile.connectedSources.Contains(source))
                        {
                            newTile.connectedSources.Add(source);
                        }
                        if (!source.connectedTiles.Contains(newTile))
                        {
                            source.connectedTiles.Add(newTile);
                        }
                    }
                }
                // --- Existing Pollution Tile: Add to it (respecting cap) ---
                else if (neighborObject is PollutionTile neighborTile)
                {
                    // Skip frozen neighbors (they're being modified elsewhere)
                    if (neighborTile.isFrozen)
                    {
                        continue;
                    }
                    
                    float neighborCurrent = neighborTile.GetTotalPollution();
                    
                    // Only spread if neighbor is below the 90% cap
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        // Calculate how much pollution can be added without exceeding cap
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        
                        // If full spread would exceed cap, scale it down proportionally
                        if (totalSpreadAmount > roomLeft)
                        {
                            float scaleFactor = roomLeft / totalSpreadAmount;
                            float clampedSpreadRate = spreadAmount_SpreadRate * scaleFactor;
                            float clampedStrength = spreadAmount_Strength * scaleFactor;
                            float clampedResistance = spreadAmount_Resistance * scaleFactor;
                            
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, clampedSpreadRate, clampedStrength, clampedResistance);
                        }
                        else
                        {
                            // Room for full spread amount
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                        }
                        
                        // Merge source networks (neighbor now connects to all of this tile's sources)
                        foreach (PollutionSource source in connectedSources)
                        {
                            if (!neighborTile.connectedSources.Contains(source))
                            {
                                neighborTile.connectedSources.Add(source);
                            }
                            if (!source.connectedTiles.Contains(neighborTile))
                            {
                                source.connectedTiles.Add(neighborTile);
                            }
                        }
                    }
                }
            }
        }
    }
}
