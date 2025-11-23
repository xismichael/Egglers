using UnityEngine;

namespace Egglers
{
    public enum PollutionType
    {
        Toxic,      // Balanced: 1.0 spread, 1.0 attack
        Acidic,     // Aggressive: 0.7 spread, 1.5 attack (requires high leaf count)
        Sludge      // Persistent: 0.4 spread, 0.8 attack (low extraction rate)
    }

    public enum PlantPhase
    {
        Bud,        // Consuming resources to grow
        Grown       // Fully functional, can sprout
    }

    public enum TileState
    {
        Empty,
        Plant,
        Pollution,
        Heart,
        PollutionSource
    }

    public enum GameState
    {
        HeartPlacement, // Player choosing where to place Heart
        Playing,
        Paused,
        Won,
        Lost
    }

    public enum SourceTier
    {
        Weak,       // HP 50, emission 5
        Medium,     // HP 150, emission 15
        Strong      // HP 300, emission 30
    }

    public enum SourceState
    {
        Dormant,    // Not yet active
        Active,     // Emitting pollution
        Awakened    // Doubled emission, increased HP
    }

    // Struct for grafting buffer
    // [System.Serializable]
    // public struct GraftBuffer
    // {
    //     public int leafAmount;
    //     public int rootAmount;
    //     public int fruitAmount;
    //     public bool hasContent;

    //     public GraftBuffer(int leaf, int root, int fruit, bool hasContent)
    //     {
    //         this.leafAmount = leaf;
    //         this.rootAmount = root;
    //         this.fruitAmount = fruit;
    //         this.hasContent = hasContent;
    //     }

    //     public int TotalComponents => leafAmount + rootAmount + fruitAmount;
    // }

    // Struct for pollution spreading operations
    [System.Serializable]
    public struct SpreadOperation
    {
        public Vector2Int targetPosition;
        public PollutionType pollutionType;
        public float amount;
        public int hops;
    }
}

