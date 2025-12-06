using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Egglers
{
    public class PollutionTile
    {
        public Vector2Int position;

        public float pollutionSpreadRate; // How pollution gets translated to the tile
        public float pollutionStrength; // the attack damage of the tile
        public float pollutionResistance; // how quick for the plant to consume the pollution

        private float minSpreadAmmount = 1.0f;

        public bool isFrozen; // if true, cannot b added to or spread

        public HashSet<PollutionSource> connectedSources = new HashSet<PollutionSource>(); // tracks which sources this tile is connected to

        public PollutionTile(Vector2Int pos)
        {
            position = pos;
        }

        public float GetTotalPollution()
        {
            return pollutionSpreadRate + pollutionStrength + pollutionResistance;
        }

        public float GetInfectionRate()
        {
            //between 30 and 10seconds, scaled by pollution spread rate
            float percentage = Mathf.Clamp01(pollutionSpreadRate / 75f);
            return 30f - (percentage * 20f);
        }

        public void Spread()
        {
            // Check if there's still a plant at this position
            PlantBit plantAtPosition = PollutionManager.Instance.gameGrid.GetEntity<PlantBit>(position);
            if (plantAtPosition == null)
            {
                isFrozen = false; // Unfreeze if plant is gone
            }

            // Can't spread if frozen
            if (isFrozen)
            {
                return;
            }

            // Get adjacent positions
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f; // neighbor can't exceed 90% of my pollution

            // Calculate 90% to spread
            float spreadAmount_SpreadRate = pollutionSpreadRate * 0.9f;
            float spreadAmount_Strength = pollutionStrength * 0.9f;
            float spreadAmount_Resistance = pollutionResistance * 0.9f;
            float totalSpreadAmount = spreadAmount_SpreadRate + spreadAmount_Strength + spreadAmount_Resistance;

            // If the total spread amount is less than the minimum spread amount, don't spread
            if (totalSpreadAmount < minSpreadAmmount)
            {
                return;
            }

            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                PollutionTile neighborTile = PollutionManager.Instance.gameGrid.GetEntity<PollutionTile>(neighborPos);
                PollutionSource neighborSource = PollutionManager.Instance.gameGrid.GetEntity<PollutionSource>(neighborPos);
                PlantBit neighborPlantBit = PollutionManager.Instance.gameGrid.GetEntity<PlantBit>(neighborPos);

                // Skip if a pollution source already occupies this tile
                if (neighborSource != null) continue;

                // try to infect the plant
                if (neighborPlantBit != null)
                {
                    if (!infectPlant(neighborPlantBit)) continue;

                }

                // Create new tile if empty
                if (neighborTile == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(
                        neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance
                    );
                    newTile.isFrozen = (neighborPlantBit != null);
                    foreach (PollutionSource source in connectedSources)
                    {
                        newTile.connectedSources.Add(source);
                        source.connectedTiles.Add(newTile);
                    }
                }
                else
                {
                    float neighborCurrent = neighborTile.GetTotalPollution();

                    // Only spread if neighbor is below 90% cap
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        float scaleFactor = totalSpreadAmount > roomLeft ? roomLeft / totalSpreadAmount : 1f;

                        PollutionManager.Instance.AddPollutionToTile(
                            neighborTile,
                            spreadAmount_SpreadRate * scaleFactor,
                            spreadAmount_Strength * scaleFactor,
                            spreadAmount_Resistance * scaleFactor
                        );

                        // Merge network connections
                        foreach (PollutionSource source in connectedSources)
                        {
                            neighborTile.connectedSources.Add(source);
                            source.connectedTiles.Add(neighborTile);
                        }
                    }
                }
            }
        }


        //have to change based on pollution resistance too
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
            float resistanceScale = Mathf.Max(0f, 1 - (pollutionResistance/75f + 0.5f));

            float damage = Mathf.Min(amount * resistanceScale, currentTotal);
            float ratio = damage / currentTotal;

            //pollutionSpreadRate = Mathf.Max(0f, pollutionSpreadRate - (pollutionSpreadRate * ratio));
            pollutionStrength = Mathf.Max(0f, pollutionStrength - (pollutionStrength * ratio));
            //pollutionResistance = Mathf.Max(0f, pollutionResistance - (pollutionResistance * ratio));

            if (pollutionStrength <= 0.1f)
            {
                GridEvents.PollutionKilledByPlant(position);
                PollutionManager.Instance.RemovePollutionAt(position.x, position.y);
            }
            else
            GridEvents.PollutionUpdated(position);
            return damage;
        }


        //try infecting the plant:
        // if current pollution strength is greater than plant's attack damage
        // then infect the plant
        // if the current pollution strenght is greater than the maxthreashold
        // then fully infect the plant

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
            float maxThreshold = plant.attackDamage * (1 + (0.5f - 0.5f * pollutionResistance * 0.9f / 75f));
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
