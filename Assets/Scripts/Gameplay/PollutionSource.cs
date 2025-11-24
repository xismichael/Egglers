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
        public float pollutionSpreadRate;   // How fast emitted pollution spreads
        public float pollutionStrength;     // Offensive power of emitted pollution
        public float pollutionResistance;   // Defensive power of emitted pollution

        // === Health System ===
        public float maxHp;      // Maximum hit points
        public float currentHp;  // Current hit points (source is destroyed when this reaches 0)

        // === Timing Control ===
        public float pulseRate;        // How often the source emits pollution (in seconds)
        public float dormantDuration;  // Initial delay before the source becomes active (in seconds)
        public float timeSinceCreation; // Tracks time for dormant period
        public bool IsActive => timeSinceCreation >= dormantDuration;

        // === Network Tracking ===
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
        /// </summary>
        public void Pulse()
        {
            if (!IsActive) return;

            List<PollutionTile> tilesBeforePulse = new List<PollutionTile>(connectedTiles);
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            float totalPollution = GetTotalPollution();
            float maxNeighborPollution = totalPollution * 0.9f;

            float emitSpreadRate = pollutionSpreadRate * 0.1f;
            float emitStrength = pollutionStrength * 0.1f;
            float emitResistance = pollutionResistance * 0.1f;
            float totalEmit = emitSpreadRate + emitStrength + emitResistance;

            bool isAcidic = (totalPollution > 0 && pollutionStrength >= totalPollution * 0.5f);
            GridSystem gridSystem = PollutionManager.Instance.gameGrid;

            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                TileState tileState = gridSystem.GetTileState(neighborPos);

                // === Handle Plants and Heart ===
                if (tileState == TileState.Plant || tileState == TileState.Heart)
                {
                    PlantBit plant = gridSystem.GetEntity<PlantBit>(neighborPos);
                    if (plant != null)
                    {
                        if (plant.isHeart)
                        {
                            float heartATD = plant.attackDamage;
                            if (isAcidic) heartATD *= 0.67f;

                            if (heartATD <= pollutionStrength)
                            {
                                GameManager gameManager = Object.FindObjectOfType<GameManager>();
                                gameManager?.TriggerLoss();
                            }

                            continue; // Heart cannot be killed
                        }

                        float effectiveATD = plant.phase == PlantBitPhase.Bud && plant.parent != null
                            ? plant.parent.attackDamage
                            : plant.attackDamage;

                        if (isAcidic) effectiveATD *= 0.67f;

                        if (effectiveATD <= pollutionStrength)
                        {
                            PollutionManager.Instance.plantManager.KillPlantBit(plant);

                            PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                            ConnectToTile(newTile);
                        }

                        continue;
                    }
                }

                if (tileState == TileState.PollutionSource) continue;

                PollutionTile neighborTile = gridSystem.GetEntity<PollutionTile>(neighborPos);

                if (neighborTile == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                    ConnectToTile(newTile);
                }
                else if (!neighborTile.isFrozen)
                {
                    float neighborCurrent = neighborTile.GetTotalPollution();
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        float scale = totalEmit > roomLeft ? roomLeft / totalEmit : 1f;
                        PollutionManager.Instance.AddPollutionToTile(neighborTile, emitSpreadRate * scale, emitStrength * scale, emitResistance * scale);
                        ConnectToTile(neighborTile);
                    }
                }
            }

            // Trigger cascading spread
            foreach (PollutionTile tile in tilesBeforePulse)
            {
                tile.Spread();
            }
        }

        /// <summary>
        /// Establishes bidirectional connection between this source and a tile.
        /// </summary>
        private void ConnectToTile(PollutionTile tile)
        {
            if (!tile.connectedSources.Contains(this))
                tile.connectedSources.Add(this);

            if (!connectedTiles.Contains(tile))
                connectedTiles.Add(tile);
        }

        /// <summary>
        /// Reduces source HP. When HP reaches 0, the source should be destroyed.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (damage <= 0f) return;

            // Deduct HP
            currentHp -= damage;
            if (currentHp < 0f) currentHp = 0f;

            Debug.Log($"[PollutionSource] Took {damage} damage at {position}. Current HP: {currentHp}/{maxHp}");

            // If destroyed, properly remove it
            if (ShouldBeDestroyed())
            {
                Debug.Log($"[PollutionSource] Destroyed at {position}");
                PollutionManager.Instance.RemovePollutionAt(position); // <--- important
            }
        }

        /// <summary>
        /// Returns true when the source has been destroyed (HP <= 0).
        /// </summary>
        public bool ShouldBeDestroyed()
        {
            return currentHp <= 0;
        }
    }
}
