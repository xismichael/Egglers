using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class PollutionTile
    {
        public Vector2Int position;

        public float pollutionSpreadRate; // how often the tile will spread pollution
        public float pollutionStrength; // the attack damage of the tile
        public float pollutionResistance; // how quick for the plant to consume the pollution

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
            // Get adjacent positions
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);

            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f; // neighbor can't exceed 90% of my pollution

            // Calculate 10% to spread
            float spreadAmount_SpreadRate = pollutionSpreadRate * 0.1f;
            float spreadAmount_Strength = pollutionStrength * 0.1f;
            float spreadAmount_Resistance = pollutionResistance * 0.1f;
            float totalSpreadAmount = spreadAmount_SpreadRate + spreadAmount_Strength + spreadAmount_Resistance;

            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                object neighborObject = PollutionManager.Instance.grid[neighborPos.x, neighborPos.y];

                // If empty (null), create new tile
                if (neighborObject == null)
                {
                    PollutionManager.Instance.AddPollutionToPosition(neighborPos, spreadAmount_SpreadRate, spreadAmount_Strength, spreadAmount_Resistance);
                    PollutionManager.Instance.MarkTileDirty(neighborPos);
                }
                // If it's a PollutionTile, check if we can add to it
                else if (neighborObject is PollutionTile neighborTile)
                {
                    // Always mark as dirty (keeps spreading active)
                    PollutionManager.Instance.MarkTileDirty(neighborPos);
                    
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
                    }
                }
                // If it's a PollutionSource, skip
            }
        }
    }
}
