using System.ComponentModel;
using UnityEngine;

namespace Egglers
{
    [CreateAssetMenu(fileName = "PlantBitData", menuName = "Scriptable Objects/PlantBitData")]
    public class PlantBitData : ScriptableObject
    {
        [Header("Component Multipliers")]

        [Tooltip("Base multiplier for leaf power (attack damage)")]
        public float leafMultiplier = 1.0f;

        [Tooltip("Base multiplier for root power (energy extraction)")]
        public float rootMultiplier = 1.0f;

        [Tooltip("Base multiplier for fruit power (energy storage)")]
        public float fruitMultiplier = 1.0f;

        [Tooltip("Bonus power multiplier for natural components: strength = numNatComps ^ this")]
        public float naturalComponentPower = 1.3f;


        // baseSproutCost + [total natural components] * naturalSproutScaling + [total grafted components] * graftedSproutCostScaling
        [Header("Sprouting")]

        [Tooltip("Base cost to sprout a new plant")]
        public float baseSproutCost = 10f;

        [Tooltip("Scaling factor based on total components")]
        public float naturalSproutCostScaling = 0.2f;

        [Tooltip("Additional scaling factor for grafted components")]
        public float graftedSproutCostScaling = 0.3f;

        [Tooltip("Chance to sprout per tick")]
        public float sproutingChance = 0.01f;

        [Tooltip("Sprouting cooldown")]
        public int sproutingCooldownDuration = 100;


        [Header("Grafting Costs")]

        [Tooltip("Base cost to apply grafts")]
        public float baseGraftCost = 5.0f;

        [Tooltip("Scaling factor for grafting based on current components")]
        public float graftCostScaling = 0.25f;

        [Tooltip("Cost per component to remove grafts")]
        public float removalCostPerComponent = 1.0f;

        [Tooltip("Cooldown duration after grafting operations")]
        public int graftCooldownDuration = 5;

        [Tooltip("Amount max component increases each generation")]
        public int maxComponentIncrease = 1;

        [Tooltip("Amount max component increases each generation based on max component of parents")]
        public float maxComponentBonus = 1.5f;


        [Header("Extraction Parameters")]
        
        [Tooltip("Multiplication factor for base pollution yield: yield = this * poll * eRate")]
        public float extractionPollutionFactor = 1.0f;

        [Tooltip("Multiplication factor for bud pollution extraction yield: bud yield = this * adult eRate")]
        public float budExtractionFactor = 0.2f;


        [Header("Budding")]

        [Tooltip("Amount of ticks of growth necessary until bud grows")]
        public int fullGrowthTicks = 60;

        [Tooltip("Amount of energy spent for a bud to grow one tick")]
        public int tickGrowthCost = 10;


        [Header("Nipping")]

        [Tooltip("Percent of sprouting cost refunded for nipping a bud")]
        public float nippingRefundFraction = 0.5f;

        // [Header("Tick Rates")]
        // [Tooltip("How often plants update (in seconds)")]
        // public float plantTickRate = 0.5f;

        // [Header("Auto-Sprout")]
        // [Tooltip("Enable delayed auto-sprout (false = immediate)")]
        // public bool delayedAutoSprout = false;

        // [Tooltip("Delay before auto-sprouting (if delayed enabled)")]
        // public float autoSproutDelay = 1.0f;
    }
}

