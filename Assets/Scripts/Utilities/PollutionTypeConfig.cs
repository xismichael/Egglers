using UnityEngine;

namespace PlantPollutionGame
{
    [CreateAssetMenu(fileName = "PollutionTypeConfig", menuName = "Plant Pollution Game/Pollution Type Config")]
    public class PollutionTypeConfig : ScriptableObject
    {
        [Header("Toxic Pollution (Balanced)")]
        [Tooltip("Spread multiplier for Toxic pollution")]
        public float toxicSpreadMultiplier = 1.0f;

        [Tooltip("Attack multiplier for Toxic pollution")]
        public float toxicAttackMultiplier = 1.0f;

        [Tooltip("Resource extraction multiplier for Toxic pollution")]
        public float toxicExtractionMultiplier = 1.0f;

        [Header("Acidic Pollution (Aggressive)")]
        [Tooltip("Spread multiplier for Acidic pollution (slower spread)")]
        public float acidicSpreadMultiplier = 0.7f;

        [Tooltip("Attack multiplier for Acidic pollution (high attack)")]
        public float acidicAttackMultiplier = 1.5f;

        [Tooltip("Defense multiplier against Acidic (requires more leaves)")]
        public float acidicDefenseMultiplier = 0.67f;

        [Tooltip("Resource extraction multiplier for Acidic pollution")]
        public float acidicExtractionMultiplier = 1.0f;

        [Header("Sludge Pollution (Persistent)")]
        [Tooltip("Spread multiplier for Sludge pollution (very slow)")]
        public float sludgeSpreadMultiplier = 0.4f;

        [Tooltip("Attack multiplier for Sludge pollution")]
        public float sludgeAttackMultiplier = 0.8f;

        [Tooltip("Resource extraction multiplier for Sludge (hard to extract)")]
        public float sludgeExtractionMultiplier = 0.6f;

        [Header("General Settings")]
        [Tooltip("Base spread rate for all pollution")]
        public float baseSpreadRate = 1.0f;

        [Tooltip("Distance decay factor (higher = faster decay)")]
        public float distanceDecayFactor = 0.2f;

        [Tooltip("Pollution level threshold for removal")]
        public float removalThreshold = 1.0f;

        public float GetSpreadMultiplier(PollutionType type)
        {
            switch (type)
            {
                case PollutionType.Toxic:
                    return toxicSpreadMultiplier;
                case PollutionType.Acidic:
                    return acidicSpreadMultiplier;
                case PollutionType.Sludge:
                    return sludgeSpreadMultiplier;
                default:
                    return 1.0f;
            }
        }

        public float GetAttackMultiplier(PollutionType type)
        {
            switch (type)
            {
                case PollutionType.Toxic:
                    return toxicAttackMultiplier;
                case PollutionType.Acidic:
                    return acidicAttackMultiplier;
                case PollutionType.Sludge:
                    return sludgeAttackMultiplier;
                default:
                    return 1.0f;
            }
        }

        public float GetExtractionMultiplier(PollutionType type)
        {
            switch (type)
            {
                case PollutionType.Toxic:
                    return toxicExtractionMultiplier;
                case PollutionType.Acidic:
                    return acidicExtractionMultiplier;
                case PollutionType.Sludge:
                    return sludgeExtractionMultiplier;
                default:
                    return 1.0f;
            }
        }
    }
}

