using System.Collections.Generic;
using UnityEngine;

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

        public void Spread()
        {
            // Can't spread if frozen
            if (isFrozen)
            {
                return;
            }

            // Get adjacent positions
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f; // neighbor can't exceed 90% of my pollution

            // Calculate 10% to spread
            float spreadAmount_SpreadRate = pollutionSpreadRate * 0.1f;
            float spreadAmount_Strength = pollutionStrength * 0.1f;
            float spreadAmount_Resistance = pollutionResistance * 0.1f;
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

                // Attack plant if pollution is stronger
                if (neighborPlantBit != null)
                {
                    if (pollutionStrength > neighborPlantBit.attackDamage)
                    {
                        PollutionManager.Instance.plantManager.KillPlantBit(neighborPlantBit);
                        Debug.Log($"[PollutionTile] Killed plant at {neighborPos} | PollutionStrength: {pollutionStrength}, PlantAttack: {neighborPlantBit.attackDamage}");
                    }
                }

                // Create new tile if empty
                if (neighborTile == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(
                        neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance
                    );
                    foreach (PollutionSource source in connectedSources)
                    {
                        newTile.connectedSources.Add(source);
                        source.connectedTiles.Add(newTile);
                    }
                }
                else
                {
                    // Skip frozen tiles
                    if (neighborTile.isFrozen) continue;

                    float neighborCurrent = neighborTile.GetTotalPollution();
                    maxNeighborPollution = GetTotalPollution() * 0.9f;

                    if (neighborCurrent < maxNeighborPollution)
                    {
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        totalSpreadAmount = spreadAmount_SpreadRate + spreadAmount_Strength + spreadAmount_Resistance;

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

        public void TakeDamage(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            float currentTotal = GetTotalPollution();
            if (currentTotal <= 0f)
            {
                return;
            }

            float damage = Mathf.Min(amount, currentTotal);
            float ratio = damage / currentTotal;

            pollutionSpreadRate = Mathf.Max(0f, pollutionSpreadRate - (pollutionSpreadRate * ratio));
            pollutionStrength = Mathf.Max(0f, pollutionStrength - (pollutionStrength * ratio));
            pollutionResistance = Mathf.Max(0f, pollutionResistance - (pollutionResistance * ratio));

            if (GetTotalPollution() <= 0.1f)
            {
                PollutionManager.Instance.RemovePollutionAt(position.x, position.y);
            }
            else
            {
                GridEvents.PollutionUpdated(position);
            }
        }
    }
}
