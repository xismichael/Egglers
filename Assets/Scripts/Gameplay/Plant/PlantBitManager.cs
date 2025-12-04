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
            Debug.Log($"[PlantBitManager] GraftBuffer updated -> L:{leaf} R:{root} F:{fruit}");
            leafCount = leaf;
            rootCount = root;
            fruitCount = fruit;
            hasContent = true;
        }

        public void Clear()
        {
            Debug.Log("[PlantBitManager] GraftBuffer cleared");
            leafCount = 0;
            rootCount = 0;
            fruitCount = 0;
            hasContent = false;
        }

        public int TotalComponents => leafCount + rootCount + fruitCount;
    }

    public class PlantBitManager : MonoBehaviour
    {
        public static PlantBitManager Instance { get; private set; }
        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Tooltip("Plant bit data scriptable object")]
        [SerializeField] private PlantBitData plantBitData;

        [Tooltip("Plant bit data scriptable object for heart")]
        [SerializeField] private PlantBitData heartBitData;

        // [SerializeField] private Vector2Int startingHeartPos = Vector2Int.zero;
        // [SerializeField] public int gridWidth = 10;
        // [SerializeField] public int gridHeight = 10;
        // [SerializeField] private float tickDurationSeconds = 1.0f;
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
        public GraftBuffer stashedGraftBuffer; // Used by UI manager to bypass tile actions

        public PollutionManager pollutionManager;

        /// <summary>
        /// Initializes the manager with a shared unified grid.
        /// </summary>
        public void Initialize(GridSystem sharedGrid)
        {
            Debug.Log("[PlantBitManager] Initializing manager with shared grid");
            gameGrid = sharedGrid;
            graftBuffer = new GraftBuffer(0, 0, 0, false);
            stashedGraftBuffer = new GraftBuffer(0, 0, 0, false);
        }

        public void InitializeHeart(Vector2Int pos)
        {
            Debug.Log($"[PlantBitManager] Initializing Heart at {pos}");

            heart = new PlantBit(pos, heartBitData, this, true,
                heartStartingLeaf, heartStartingRoot, heartStartingFruit, heartStartingMaxComponent);

            gameGrid.SetEntity(heart);

            maxEnergy = startingMaxEnergy;
            currentEnergy = startingEnergy;

            Debug.Log($"[PlantBitManager] Heart placed at {pos} | Energy: {currentEnergy}/{maxEnergy}");

            GridEvents.PlantUpdated(pos);
        }

        public void UpdatePlants(PlantBit plantBit)
        {
            if (plantBit == null) return;

            // Debug.Log($"[PlantBitManager] TickUpdate root at {plantBit.position}");

            plantBit.TickUpdate();

            // Iterate normally but skip null children
            for (int i = 0; i < plantBit.children.Count; i++)
            {
                PlantBit child = plantBit.children[i];
                if (child == null) continue; // skip if child has been removed
                UpdatePlants(child);
            }
        }

        public void CreateSprout(PlantBit parent, Vector2Int targetPos)
        {
            if (parent == null) return;

            Debug.Log($"[PlantBitManager] Creating sprout at {targetPos} (parent: {parent.position})");

            PlantBit child = new PlantBit(targetPos, plantBitData, parent);
            gameGrid.SetEntity(child);

            GridEvents.PlantUpdated(targetPos);
        }

        public void KillPlantBit(PlantBit plantBit)
        {
            if (plantBit == null) return;

            Debug.Log($"[PlantBitManager] KillPlantBit called at {plantBit.position}");

            if (plantBit == heart)
            {
                Debug.Log("[PlantBitManager] GAME OVER: HEART IS DEAD");
                GameManager.Instance.TriggerLoss();
            }

            plantBit.Kill();

            GridEvents.PlantUpdated(plantBit.position);
        }

        public void NipPlantBit(PlantBit plantBit)
        {
            if (plantBit == null) return;

            Debug.Log($"[PlantBitManager] NipPlantBit called at {plantBit.position}");

            plantBit.Nip();
        }

        public void AddMaxEnergy(float amount)
        {
            maxEnergy += amount;
            // Debug.Log($"[PlantBitManager] MaxEnergy increased by {amount} → {maxEnergy}");
        }

        public void RemoveMaxEnergy(float amount)
        {
            maxEnergy = Mathf.Max(maxEnergy - amount, 0);
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);

            Debug.Log($"[PlantBitManager] MaxEnergy decreased by {amount} → {maxEnergy} | currentEnergy={currentEnergy}");
        }

        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
            // Debug.Log($"[PlantBitManager] Added {amount} energy → {currentEnergy}/{maxEnergy}");
        }

        public bool RemoveEnergy(float amount)
        {
            if (currentEnergy < amount)
            {
                // Debug.Log($"[PlantBitManager] RemoveEnergy FAILED ({currentEnergy}/{amount})");
                return false;
            }

            currentEnergy -= amount;
            // Debug.Log($"[PlantBitManager] RemoveEnergy {amount} → {currentEnergy}/{maxEnergy}");
            return true;
        }

        public void ApplyGraftAtPosition(Vector2Int pos)
        {
            Debug.Log($"[PlantBitManager] ApplyGraftAtPosition {pos}");

            PlantBit plantBit = gameGrid.GetEntity<PlantBit>(pos);
            if (plantBit == null || !graftBuffer.hasContent)
            {
                Debug.Log($"[PlantBitManager] Cannot apply graft at {pos} (missing plant or empty graft buffer)");
                SoundManager.Instance.PlayError();
                return;
            }

            plantBit.ApplyGraft(graftBuffer);
            GridEvents.PlantUpdated(pos);
        }

        public void RemoveGraftAtPosition(Vector2Int pos, int leaf = 0, int root = 0, int fruit = 0)
        {
            Debug.Log($"[PlantBitManager] RemoveGraftAtPosition {pos}");

            PlantBit plantBit = gameGrid.GetEntity<PlantBit>(pos);
            if (plantBit == null)
            {
                Debug.Log($"[PlantBitManager] RemoveGraft failed at {pos}: no plant found");
                return;
            }

            if (plantBit.isHeart)
            {
                Debug.Log($"[PlantBitManager] RemoveGraft failed at {pos}: Can't Graft Heart");
                SoundManager.Instance.PlayError();

                return;
            }

            // The stashed buffer was added so I could call it from the UI manager since I could not pass in the data through tile actions
            if (leaf + root + fruit <= 0)
            {
                plantBit.RemoveGraft(stashedGraftBuffer.leafCount, stashedGraftBuffer.rootCount, stashedGraftBuffer.fruitCount);
                stashedGraftBuffer.Clear();
            }
            else
            {
                plantBit.RemoveGraft(leaf, root, fruit);
            }

            GridEvents.PlantUpdated(pos);
        }

        public float GetPollutionAtTile(Vector2Int pos)
        {
            float lvl = pollutionManager?.GetPollutionLevelAt(pos) ?? 0f;
            Debug.Log($"[PlantBitManager] GetPollutionAtTile {pos} → {lvl}");
            return lvl;
        }

        public void StashGraftData(int leaf, int root, int fruit)
        {
            stashedGraftBuffer.Update(leaf, root, fruit);
        }
    }
}
