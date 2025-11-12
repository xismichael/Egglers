using UnityEngine;

namespace PlantPollutionGame
{
    public class PollutionSource
    {
        // Position on grid
        public Vector2Int position;

        // Source properties
        public PollutionType pollutionType;
        public SourceTier tier;
        public SourceState state;

        // HP
        public float maxHP;
        public float currentHP;

        // Emission
        public float baseEmissionRate;
        public float currentEmissionRate;
        public float tickInterval;

        // Awakening
        public float dormantDuration; // How long before source activates

        // References (set externally)
        public PollutionManager pollutionManager;
        public GridSystem gridSystem;

        public PollutionSource(Vector2Int pos, PollutionType type, SourceTier sourceTier, float hp, float emission, float interval, float dormantTime = 0f)
        {
            position = pos;
            pollutionType = type;
            tier = sourceTier;
            maxHP = hp;
            currentHP = hp;
            baseEmissionRate = emission;
            currentEmissionRate = emission;
            tickInterval = interval;
            dormantDuration = dormantTime;

            // Set initial state
            state = dormantTime > 0 ? SourceState.Dormant : SourceState.Active;
        }

        public void CheckAwakening(float gameTime)
        {
            if (state == SourceState.Dormant && gameTime >= dormantDuration)
            {
                // Activate the source
                state = SourceState.Active;
                Debug.Log($"Pollution source at {position} has activated!");
            }
            else if (state == SourceState.Active)
            {
                // Check if should awaken (specific timing based on tier)
                bool shouldAwaken = false;

                switch (tier)
                {
                    case SourceTier.Weak:
                        // Weak sources don't awaken
                        break;
                    case SourceTier.Medium:
                        // Awaken at 2 minutes (120 seconds)
                        if (gameTime >= 120f)
                        {
                            shouldAwaken = true;
                        }
                        break;
                    case SourceTier.Strong:
                        // Awaken at 5 minutes (300 seconds)
                        if (gameTime >= 300f)
                        {
                            shouldAwaken = true;
                        }
                        break;
                }

                if (shouldAwaken && state != SourceState.Awakened)
                {
                    state = SourceState.Awakened;
                    currentEmissionRate = baseEmissionRate * 2f; // Double emission
                    maxHP *= 1.5f; // 50% more HP
                    currentHP = maxHP; // Restore to new max
                    Debug.Log($"Pollution source at {position} has awakened! Emission doubled, HP increased!");
                }
            }
        }

        public void OnTick()
        {
            // Only emit if active or awakened
            if (state == SourceState.Dormant)
            {
                return;
            }

            // Emit to immediately adjacent tiles only (4-directional)
            var neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);

            foreach (Vector2Int neighborPos in neighbors)
            {
                TileState tileState = gridSystem.GetTileState(neighborPos);

                // Can only emit to empty or pollution tiles
                if (tileState == TileState.Empty || tileState == TileState.Pollution)
                {
                    PollutionTile tile = pollutionManager.GetOrCreateTile(neighborPos);
                    tile.AddPollution(pollutionType, currentEmissionRate);
                    tile.hopsFromSource = 1; // Adjacent to source = 1 hop
                }
            }
        }

        public void TakeDamage(float amount)
        {
            currentHP -= amount;

            if (currentHP <= 0)
            {
                currentHP = 0;
                OnDestroyed();
            }
        }

        private void OnDestroyed()
        {
            Debug.Log($"Pollution source at {position} has been destroyed!");
            // PollutionManager will handle removal
            pollutionManager.RemoveSource(this);
        }

        public bool IsAlive()
        {
            return currentHP > 0;
        }

        public bool IsActive()
        {
            return state == SourceState.Active || state == SourceState.Awakened;
        }
    }
}

