using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
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
        
        public void Pulse()
        {
            // Only pulse if active
            if (!IsActive)
            {
                return;
            }
            
            // Get adjacent positions
            List<Vector2Int> adjacentPositions = PollutionManager.Instance.GetAdjacentPositions(position);
            
            float myTotalPollution = GetTotalPollution();
            float maxNeighborPollution = myTotalPollution * 0.9f;
            
            // Calculate 10% to emit (like tiles)
            float emitSpreadRate = pollutionSpreadRate * 0.1f;
            float emitStrength = pollutionStrength * 0.1f;
            float emitResistance = pollutionResistance * 0.1f;
            float totalEmit = emitSpreadRate + emitStrength + emitResistance;
            
            foreach (Vector2Int neighborPos in adjacentPositions)
            {
                object neighborObject = PollutionManager.Instance.grid[neighborPos.x, neighborPos.y];
                
                // If empty (null), create new tile
                if (neighborObject == null)
                {
                    PollutionManager.Instance.AddPollutionToPosition(neighborPos, emitSpreadRate, emitStrength, emitResistance);
                    PollutionManager.Instance.MarkTileDirty(neighborPos);
                }
                // If it's a PollutionTile
                else if (neighborObject is PollutionTile neighborTile)
                {
                    // Always mark as dirty (keeps spreading active)
                    PollutionManager.Instance.MarkTileDirty(neighborPos);
                    
                    // Add pollution only if under 90% cap
                    float neighborCurrent = neighborTile.GetTotalPollution();
                    if (neighborCurrent < maxNeighborPollution)
                    {
                        // Calculate how much room is left
                        float roomLeft = maxNeighborPollution - neighborCurrent;
                        
                        // If adding full amount would exceed cap, scale it down
                        if (totalEmit > roomLeft)
                        {
                            float scaleFactor = roomLeft / totalEmit;
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, emitSpreadRate * scaleFactor, emitStrength * scaleFactor, emitResistance * scaleFactor);
                        }
                        else
                        {
                            PollutionManager.Instance.AddPollutionToTile(neighborTile, emitSpreadRate, emitStrength, emitResistance);
                        }
                    }
                }
                // If it's another PollutionSource, skip
            }
        }
    }
}
