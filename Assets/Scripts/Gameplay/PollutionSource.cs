using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Represents a pollution source on the grid.
    /// Sources are the origin points of pollution - they emit pollution infinitely at regular intervals (pulses).
    /// Sources have HP and can be destroyed by adjacent plants that are stronger than the source's total pollution.
    /// </summary>
    public class PollutionSource
    {
        // === Grid Position ===
        public Vector2Int position;
        
        // === Emission Stats ===
        // These stats determine what the source emits to neighbors
        public float pollutionSpreadRate;   // How fast emitted pollution spreads
        public float pollutionStrength;     // Offensive power of emitted pollution
        public float pollutionResistance;   // Defensive power of emitted pollution
        
        // === Health System ===
        public float maxHp;      // Maximum hit points
        public float currentHp;  // Current hit points (source is destroyed when this reaches 0)
        
        // === Timing Control ===
        public float pulseRate;        // How often the source emits pollution (in seconds)
        public float dormantDuration;  // Initial delay before the source becomes active (in seconds)
        
        // === State Tracking ===
        public float timeSinceCreation; // Tracks time for dormant period
        public bool IsActive => timeSinceCreation >= dormantDuration; // True when source has finished its dormant period
        
        // === Network Tracking ===
        // All pollution tiles that are connected to this source (source → tiles relationship)
        public List<PollutionTile> connectedTiles = new List<PollutionTile>();
        
        /// <summary>
        /// Constructor for pollution source.
        /// </summary>
        public PollutionSource(Vector2Int pos, float spreadRate, float strength, float resistance, float pulse, float dormant, float hp = 50f)
        {
            position = pos;
            pollutionSpreadRate = spreadRate;
            pollutionStrength = strength;
            pollutionResistance = resistance;
            pulseRate = pulse;
            dormantDuration = dormant;
            timeSinceCreation = 0f;
            maxHp = hp;
            currentHp = hp;
        }
        
        /// <summary>
        /// Returns the sum of all emission stats.
        /// Used to determine the source's strength when plants try to damage it.
        /// </summary>
        public float GetTotalPollution()
        {
            return pollutionSpreadRate + pollutionStrength + pollutionResistance;
        }
        
        /// <summary>
        /// Main emission logic - called at regular intervals by PollutionManager's coroutine.
        /// 
        /// Pulse Sequence:
        /// 1. Emit 10% of stats to adjacent tiles (or create new tiles)
        /// 2. Check for weak plants and kill them, spreading pollution to their positions
        /// 3. Trigger all connected tiles to spread (cascading effect)
        /// 
        /// Important: Only tiles that existed BEFORE this pulse are allowed to spread,
        /// preventing infinite recursion from newly created tiles.
        /// </summary>
        public void Pulse()
        {
            // Dormant sources don't pulse yet
            if (!IsActive)
            {
                return;
            }
            
            // Snapshot of tiles before emission (prevents newly created tiles from spreading this pulse)
            List<PollutionTile> tilesBeforePulse = new List<PollutionTile>(connectedTiles);
            
            // Get all 4-directional neighbors
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);
            
            // Calculate emission cap (neighbors can't exceed 90% of source's total pollution)
            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f;
            
            // Calculate 10% of each stat to emit
            float emitSpreadRate = pollutionSpreadRate * 0.1f;
            float emitStrength = pollutionStrength * 0.1f;
            float emitResistance = pollutionResistance * 0.1f;
            float totalEmit = emitSpreadRate + emitStrength + emitResistance;
            
            // Pre-calculate if this source is acidic (used for multiple neighbor checks)
            float totalPollution = GetTotalPollution();
            bool isAcidic = (totalPollution > 0 && pollutionStrength >= totalPollution * 0.5f);
            
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
                            if (isAcidic)
                            {
                                heartATD *= 0.67f;
                            }
                            
                            // If source overwhelms the Heart (tie goes to pollution), game over
                            if (heartATD <= pollutionStrength)
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
                        if (isAcidic)
                        {
                            effectiveATD *= 0.67f;
                        }
                        
                        // --- Combat Resolution: Kill weak plants and emit ---
                        if (effectiveATD <= pollutionStrength)
                        {
                            // Source wins - delete the plant and all its children
                            PollutionManager.Instance.plantManager.DeletePlant(plant, fromPollution: true);
                            
                            // Create new pollution tile where the plant was
                            PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                            
                            // Connect this source to the new tile
                            ConnectToTile(newTile);
                        }
                        
                        // Plant is stronger - pollution cannot spread here
                        continue;
                    }
                }
                
                // === Skip Other Pollution Sources (sources can't emit to each other) ===
                if (tileState == TileState.PollutionSource)
                {
                    continue;
                }
                
                // === Handle Empty Tiles and Existing Pollution ===
                object neighborObject = PollutionManager.Instance.grid[neighborPos.x, neighborPos.y];
                
                // --- Empty Tile: Create new pollution tile ---
                if (neighborObject == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                    
                    // Connect this source to the new tile
                    ConnectToTile(newTile);
                }
                // --- Existing Pollution Tile: Add to it (respecting cap) ---
                else if (neighborObject is PollutionTile neighborTile)
                {
                    // Skip frozen neighbors (they're being modified elsewhere)
                    if (neighborTile.isFrozen)
                    {
                        continue;
                    }
                    
                    // Only emit if neighbor is below the 90% cap
                    float neighborCurrent = neighborTile.GetTotalPollution();
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        // Calculate how much pollution can be added without exceeding cap
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        
                        // If full emission would exceed cap, scale it down proportionally
                        if (totalEmit > roomLeft)
                        {
                            float scaleFactor = roomLeft / totalEmit;
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, emitSpreadRate * scaleFactor, emitStrength * scaleFactor, emitResistance * scaleFactor);
                        }
                        else
                        {
                            // Room for full emission amount
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, emitSpreadRate, emitStrength, emitResistance);
                        }
                        
                        // Connect this source to the tile (if not already connected)
                        ConnectToTile(neighborTile);
                    }
                }
            }
            
            // === Cascading Spread: Trigger all pre-existing tiles to spread ===
            // Only tiles that existed before this pulse spread (prevents infinite recursion)
            foreach (PollutionTile tile in tilesBeforePulse)
            {
                tile.Spread();
            }
        }
        
        /// <summary>
        /// Establishes bidirectional connection between this source and a tile.
        /// Ensures both the source and tile know about each other.
        /// </summary>
        private void ConnectToTile(PollutionTile tile)
        {
            // Add this source to the tile's source list
            if (!tile.connectedSources.Contains(this))
            {
                tile.connectedSources.Add(this);
            }
            
            // Add the tile to this source's tile list
            if (!connectedTiles.Contains(tile))
            {
                connectedTiles.Add(tile);
            }
        }

        /// <summary>
        /// Reduces source HP. When HP reaches 0, the source should be destroyed.
        /// Called by PlantManager when adjacent plants are strong enough to damage the source.
        /// </summary>
        public void TakeDamage(float damage)
        {
            currentHp -= damage;
            
            // Clamp at 0 (PollutionManager will handle removal when ShouldBeDestroyed() returns true)
            if (currentHp <= 0)
            {
                currentHp = 0;
            }
        }

        /// <summary>
        /// Returns true when the source has been destroyed (HP <= 0).
        /// PollutionManager checks this to remove dead sources from the game.
        /// </summary>
        public bool ShouldBeDestroyed()
        {
            return currentHp <= 0;
        }
    }
}
