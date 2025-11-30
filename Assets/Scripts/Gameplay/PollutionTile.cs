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

        private float minSpreadAmmount = 5.0f;
        
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


                if (neighborPlantBit != null)
                {
                    // Skip if the plant bit's attack damage is greater than or equal to the pollution strength
                    if (neighborPlantBit.attackDamage >= pollutionStrength) continue;
                    PollutionManager.Instance.plantManager.KillPlantBit(neighborPlantBit);
                }

                
                // If empty (null), create new tile
                if (neighborTile == null)
                {
                    PollutionTile newTile = PollutionManager.Instance.AddPollutionToPosition(neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                    // Connect all of this tile's sources to the new tile
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
                // If it's a PollutionTile, check if we can add to it
                else
                {
                    // Skip frozen tiles
                    if (neighborTile.isFrozen)
                    {
                        continue;
                    }
                    
                    float neighborCurrent = neighborTile.GetTotalPollution();
                    
                    // Only spread if neighbor has less than 90% of my pollution
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        // Calculate how much room is left before hitting the 90% cap
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        
                        // If adding the full amount would exceed 90%, clamp it
                        if (totalSpreadAmount > roomLeft)
                        {
                            // Scale down all three stats proportionally
                            float scaleFactor = roomLeft / totalSpreadAmount;
                            float clampedSpreadRate = spreadAmount_SpreadRate * scaleFactor;
                            float clampedStrength = spreadAmount_Strength * scaleFactor;
                            float clampedResistance = spreadAmount_Resistance * scaleFactor;
                            
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, clampedSpreadRate, clampedStrength, clampedResistance);
                        }
                        else
                        {
                            // Safe to add the full amount
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                        }
                        
                        // Connect all of this tile's sources to the neighbor (merging networks)
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
                // If it's a PollutionSource, skip
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
