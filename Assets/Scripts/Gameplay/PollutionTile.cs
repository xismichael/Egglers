using UnityEngine;

namespace PlantPollutionGame
{
    public class PollutionTile
    {
        // Position on grid
        public Vector2Int position;

        // Pollution composition
        public float toxicAmount;
        public float acidicAmount;
        public float sludgeAmount;

        // Calculated values
        public float totalPollutionLevel;
        public PollutionType dominantType;
        public float spreadSpeed;
        public float attackDamage;

        // Distance tracking for decay
        public int hopsFromSource;

        // Configuration (set by PollutionManager)
        public float baseSpreadRate = 1.0f;

        public PollutionTile(Vector2Int pos)
        {
            position = pos;
            toxicAmount = 0f;
            acidicAmount = 0f;
            sludgeAmount = 0f;
            hopsFromSource = int.MaxValue; // Init to max, will be set by spreading logic
            RecalculateStats();
        }

        public void AddPollution(PollutionType type, float amount)
        {
            switch (type)
            {
                case PollutionType.Toxic:
                    toxicAmount += amount;
                    break;
                case PollutionType.Acidic:
                    acidicAmount += amount;
                    break;
                case PollutionType.Sludge:
                    sludgeAmount += amount;
                    break;
            }
            RecalculateStats();
        }

        public void RecalculateStats()
        {
            // Calculate total
            totalPollutionLevel = toxicAmount + acidicAmount + sludgeAmount;

            // Determine dominant type
            dominantType = GetHighestType();

            // Calculate spread speed based on composition
            // Toxic: 1.0 spread, Acidic: 0.7 spread, Sludge: 0.4 spread
            spreadSpeed = (toxicAmount * 1.0f + acidicAmount * 0.7f + sludgeAmount * 0.4f) * baseSpreadRate;

            // Calculate attack damage based on composition
            // Toxic: 1.0 attack, Acidic: 1.5 attack, Sludge: 0.8 attack
            attackDamage = (toxicAmount * 1.0f + acidicAmount * 1.5f + sludgeAmount * 0.8f);
        }

        private PollutionType GetHighestType()
        {
            if (toxicAmount >= acidicAmount && toxicAmount >= sludgeAmount)
            {
                return PollutionType.Toxic;
            }
            else if (acidicAmount >= sludgeAmount)
            {
                return PollutionType.Acidic;
            }
            else
            {
                return PollutionType.Sludge;
            }
        }

        public void TakeDamage(float amount)
        {
            if (totalPollutionLevel <= 0) return;

            // Proportional reduction across all types
            float ratio = Mathf.Clamp01(amount / totalPollutionLevel);
            toxicAmount *= (1 - ratio);
            acidicAmount *= (1 - ratio);
            sludgeAmount *= (1 - ratio);

            RecalculateStats();
        }

        public bool ShouldBeRemoved()
        {
            // Residue threshold: below 1.0 = cleared
            return totalPollutionLevel < 1.0f;
        }

        public float GetExtractionMultiplier()
        {
            // Sludge reduces extraction rate to 0.6x
            if (dominantType == PollutionType.Sludge)
            {
                return 0.6f;
            }
            return 1.0f;
        }
    }
}

