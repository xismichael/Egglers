using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public class PollutionSource
    {
        public Vector2Int position;

        // Pollution stats (what it emits)
        public float pollutionSpreadRate;
        public float pollutionStrength;
        public float pollutionResistance;

        // Timing
        public float pulseRate; // how often it spreads (in seconds)
        public float dormantDuration; // delay before it starts (in seconds)

        // Internal tracking
        public float timeSinceCreation;
        public bool IsActive => timeSinceCreation >= dormantDuration;

        // Network tracking
        public HashSet<PollutionTile> connectedTiles = new HashSet<PollutionTile>(); // all tiles connected to this source

        public PollutionSource(Vector2Int pos, float spreadRate, float strength, float resistance, float pulse, float dormant)
        {
            position = pos;
            pollutionSpreadRate = spreadRate;
            pollutionStrength = strength;
            pollutionResistance = resistance;
            pulseRate = pulse;
            dormantDuration = dormant;
            timeSinceCreation = 0f;
        }

        public float GetTotalPollution()
        {
            return pollutionSpreadRate + pollutionStrength + pollutionResistance;
        }

        public float GetInfectionRate()
        {
            //between 30 and 10 seconds, scaled by pollution spread rate
            float percentage = Mathf.Clamp01(pollutionSpreadRate / 15f);
            return 30f - (percentage * 20f);
        }

        public void Pulse()
        {
            // Only pulse if active
            if (!IsActive)
            {
                return;
            }



            // Take snapshot of tiles before the pulse, so new tiles dont spread yet
            List<PollutionTile> tilesBeforePulse = new List<PollutionTile>(connectedTiles);

            // Get adjacent positions
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f; // neighbor can't exceed 90% of my pollution

            // Calculate 90% to emit (like tiles)
            float emitSpreadRate = pollutionSpreadRate * 0.9f;
            float emitStrength = pollutionStrength * 0.9f;
            float emitResistance = pollutionResistance * 0.9f;
            float totalEmit = emitSpreadRate + emitStrength + emitResistance;

            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                PollutionSource neighborSource = PollutionManager.Instance.gameGrid.GetEntity<PollutionSource>(neighborPos);
                if (neighborSource != null) continue;

                PollutionTile neighborTile = PollutionManager.Instance.gameGrid.GetEntity<PollutionTile>(neighborPos);
                PlantBit neighborPlantBit = PollutionManager.Instance.gameGrid.GetEntity<PlantBit>(neighborPos);

                if (neighborPlantBit != null)
                {
                    if (!infectPlant(neighborPlantBit)) continue;

                }

                if (neighborTile == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                    newTile.isFrozen = (neighborPlantBit != null);
                    ConnectToTile(newTile);
                }
                else
                {
                    float neighborCurrent = neighborTile.GetTotalPollution();

                    if (neighborCurrent < maxNeighborPollution)
                    {
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        float scaleFactor = totalEmit > roomLeft ? roomLeft / totalEmit : 1f;

                        PollutionManager.Instance.AddPollutionToTile(
                            neighborTile,
                            emitSpreadRate * scaleFactor,
                            emitStrength * scaleFactor,
                            emitResistance * scaleFactor
                        );

                        ConnectToTile(neighborTile);
                    }
                }
            }

            // Now spread tiles that existed BEFORE this pulse (not newly created ones)
            foreach (PollutionTile tile in tilesBeforePulse)
            {
                tile.Spread();
            }
        }

        private void ConnectToTile(PollutionTile tile)
        {
            tile.connectedSources.Add(this);
            connectedTiles.Add(tile);
        }

        public float TakeDamage(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float currentTotal = GetTotalPollution();
            if (currentTotal <= 0f)
            {
                return 0f;
            }

            //damag gets reduced by pollution resistance
            float resistanceScale = Mathf.Max(0f, 1 - (pollutionResistance/15f + 0.5f));

            float damage = Mathf.Min(amount * resistanceScale, currentTotal);
            float ratio = damage / currentTotal;

            pollutionSpreadRate = Mathf.Max(0f, pollutionSpreadRate - (pollutionSpreadRate * ratio));
            pollutionStrength = Mathf.Max(0f, pollutionStrength - (pollutionStrength * ratio));
            pollutionResistance = Mathf.Max(0f, pollutionResistance - (pollutionResistance * ratio));

            if (GetTotalPollution() <= 0.1f)
            {
                GridEvents.PollutionKilledByPlant(position);
                PollutionManager.Instance.RemovePollutionAt(position.x, position.y);
            }
            else
            {
                GridEvents.PollutionUpdated(position);
            }
            return damage;
        }

        public bool infectPlant(PlantBit plant)
        {
            if (pollutionStrength * 0.9f < plant.attackDamage)
            {
                plant.isInfected = false;
                GridEvents.PlantUpdated(plant.position);
                return false;
            }
            plant.isInfected = true;
            GridEvents.PlantUpdated(plant.position);
            float maxThreshold = plant.attackDamage * (1 + (0.5f - 0.5f * pollutionResistance * 0.9f / 15f));
            if (pollutionStrength * 0.9f > maxThreshold)
            {
                plant.phase = PlantBitPhase.FullyInfected;
                GridEvents.PlantKilledByPollution(plant.position);
                plant.InfectionSpread(GetInfectionRate());
            }
            return true;
        }
    }
}
