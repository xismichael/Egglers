using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{

    // Struct for grafting buffer
    [System.Serializable]
    public struct GraftBuffer
    {
        public int leafCount;
        public int rootCount;
        public int fruitCount;
        public bool hasContent;

        public GraftBuffer(int leaf, int root, int fruit, bool hasContent)
        {
            leafCount = leaf;
            rootCount = root;
            fruitCount = fruit;
            this.hasContent = hasContent;
        }

        public void Update(int leaf, int root, int fruit)
        {
            leafCount = leaf;
            rootCount = root;
            fruitCount = fruit;
            hasContent = true;
        }

        public void Clear()
        {
            leafCount = 0;
            rootCount = 0;
            fruitCount = 0;
            hasContent = false;
        }

        public int TotalComponents => leafCount + rootCount + fruitCount;
    }

    public class PlantBitManager : MonoBehaviour
    {
        [Tooltip("Plant bit data scriptable object")]
        [SerializeField] private PlantBitData plantBitData;

        [Tooltip("Plant bit data scriptable object for heart")]
        [SerializeField] private PlantBitData heartBitData;

        [SerializeField] private Vector2Int startingHeartPos = Vector2Int.zero;
        [SerializeField] public int gridWidth = 10;
        [SerializeField] public int gridHeight = 10;
        [SerializeField] private float tickDurationSeconds = 1.0f;
        [SerializeField] private float startingEnergy = 10.0f;
        [SerializeField] private float startingMaxEnergy = 100.0f;

        [Header("Heart Data")]
        [SerializeField] private int heartStartingLeaf = 3;
        [SerializeField] private int heartStartingRoot = 3;
        [SerializeField] private int heartStartingFruit = 3;
        [SerializeField] private int heartStartingMaxComponent = 5;

        public GridSystem gameGrid;

        public PlantBit heart;

        // Resources
        public float currentEnergy;
        public float maxEnergy;

        // Grafting
        public GraftBuffer graftBuffer;

        public PollutionManager pollutionManager;

        /// <summary>
        /// Initializes the manager with a shared unified grid.
        /// </summary>
        public void Initialize(GridSystem sharedGrid)
        {
            gameGrid = sharedGrid;
            graftBuffer = new GraftBuffer(0, 0, 0, false);
        }

        public void InitializeHeart(Vector2Int pos)
        {
            heart = new PlantBit(pos, heartBitData, this, true,
                heartStartingLeaf, heartStartingRoot, heartStartingFruit, heartStartingMaxComponent);

            gameGrid.SetEntity(pos, heart); // Set in unified grid

            maxEnergy = 100f;
            currentEnergy = 10f;

            GridEvents.PlantUpdated(pos);
        }

        public void UpdatePlants(PlantBit plantBit)
        {
            if (plantBit == null) return;

            plantBit.TickUpdate();

            foreach (PlantBit child in plantBit.children)
            {
                UpdatePlants(child);
            }
        }

        public void CreateSprout(PlantBit parent, Vector2Int targetPos)
        {
            if (parent == null) return;

            PlantBit child = new PlantBit(targetPos, plantBitData, parent);
            gameGrid.SetEntity(targetPos, child);

            GridEvents.PlantUpdated(targetPos);
        }

        public void KillPlantBit(PlantBit plantBit)
        {
            if (plantBit == null) return;

            if (plantBit == heart)
            {
                Debug.Log("GAME OVER: HEART IS DEAD");
            }

            gameGrid.RemoveEntity(plantBit.position);
            plantBit.Kill();

            GridEvents.PlantUpdated(plantBit.position);
        }

        public void AddMaxEnergy(float amount)
        {
            maxEnergy += amount;
        }

        public void RemoveMaxEnergy(float amount)
        {
            maxEnergy = Mathf.Max(maxEnergy - amount, 0);
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }

        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        }

        public bool RemoveEnergy(float amount)
        {
            if (currentEnergy < amount) return false;
            currentEnergy -= amount;
            return true;
        }

        public void ApplyGraftAtPosition(Vector2Int pos)
        {
            PlantBit plantBit = gameGrid.GetEntity<PlantBit>(pos);
            if (plantBit == null || !graftBuffer.hasContent) return;

            plantBit.ApplyGraft(graftBuffer);
            GridEvents.PlantUpdated(pos);
        }

        public void RemoveGraftAtPosition(Vector2Int pos, int leaf, int root, int fruit)
        {
            PlantBit plantBit = gameGrid.GetEntity<PlantBit>(pos);
            if (plantBit == null) return;

            plantBit.RemoveGraft(leaf, root, fruit);
            GridEvents.PlantUpdated(pos);
        }

        public float GetPollutionAtTile(Vector2Int pos)
        {
            return pollutionManager?.GetPollutionLevelAt(pos) ?? 0f;
        }
    }
}
