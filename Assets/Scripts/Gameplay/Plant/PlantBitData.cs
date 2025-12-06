using UnityEngine;

namespace Egglers
{
    [CreateAssetMenu(fileName = "PlantBitData", menuName = "Scriptable Objects/PlantBitData")]
    public class PlantBitData : ScriptableObject
    {
        [Header("Component Multipliers")]
        [Tooltip("Base multiplier for leaf power (energy generation rate)")]
        public float leafMultiplier = 1.0f;

        [Tooltip("Base multiplier for root power (attack damage)")]
        public float rootMultiplier = 1.0f;

        [Tooltip("Base multiplier for fruit power (energy storage)")]
        public float fruitMultiplier = 1.0f;

        [Header("Growth")]
        [Tooltip("Amount of ticks of growth necessary until bud becomes grown")]
        public int fullGrowthTicks = 60;

        [Header("Grafting")]
        [Tooltip("Cost per component to remove/apply grafts")]
        public float removalCostPerComponent = 1.0f;
    }
}

