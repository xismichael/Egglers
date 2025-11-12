using UnityEngine;

namespace PlantPollutionGame
{
    [CreateAssetMenu(fileName = "PlantConfig", menuName = "Plant Pollution Game/Plant Config")]
    public class PlantConfig : ScriptableObject
    {
        [Header("Component Multipliers")]
        [Tooltip("Base multiplier for leaf power (attack damage)")]
        public float leafMultiplier = 1.0f;

        [Tooltip("Base multiplier for root power (resource extraction)")]
        public float rootMultiplier = 1.0f;

        [Tooltip("Base multiplier for fruit power (resource storage)")]
        public float fruitMultiplier = 1.0f;

        [Header("Cost Scaling")]
        [Tooltip("Base cost to sprout a new plant")]
        public float baseSproutCost = 10f;

        [Tooltip("Scaling factor based on total components")]
        public float componentCostScaling = 0.2f;

        [Tooltip("Additional scaling factor for grafted components")]
        public float graftedCostScaling = 0.3f;

        [Header("Grafting Costs")]
        [Tooltip("Base cost to apply grafts")]
        public float baseGraftCost = 5f;

        [Tooltip("Scaling factor for grafting based on current components")]
        public float graftCostScaling = 0.25f;

        [Tooltip("Cost per component to remove grafts")]
        public float removalCostPerComponent = 1.0f;

        [Tooltip("Cooldown duration after grafting operations")]
        public float graftCooldownDuration = 5.0f;

        [Header("Tick Rates")]
        [Tooltip("How often plants update (in seconds)")]
        public float plantTickRate = 0.5f;

        [Header("Auto-Sprout")]
        [Tooltip("Enable delayed auto-sprout (false = immediate)")]
        public bool delayedAutoSprout = false;

        [Tooltip("Delay before auto-sprouting (if delayed enabled)")]
        public float autoSproutDelay = 1.0f;
    }
}

