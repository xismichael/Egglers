using UnityEngine;

namespace PlantPollutionGame
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Plant Pollution Game/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        [Tooltip("Width of the game grid")]
        public int gridWidth = 20;

        [Tooltip("Height of the game grid")]
        public int gridHeight = 20;

        [Header("Heart Starting Components")]
        [Tooltip("Starting leaf amount for Heart")]
        public int heartStartLeaf = 3;

        [Tooltip("Starting root amount for Heart")]
        public int heartStartRoot = 3;

        [Tooltip("Starting fruit amount for Heart")]
        public int heartStartFruit = 3;

        [Header("Tick Rates")]
        [Tooltip("How often pollution spreads (in seconds)")]
        public float pollutionTickRate = 7.0f;

        [Header("Pollution Spread")]
        [Tooltip("Base rate at which pollution spreads")]
        public float baseSpreadRate = 1.0f;
    }
}

