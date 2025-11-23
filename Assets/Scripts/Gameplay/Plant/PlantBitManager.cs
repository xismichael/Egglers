using System.Collections;
using System.Data;
using Unity.VisualScripting;
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

        // Grid reference
        public GameGrid gameGrid;

        public PlantBit heart;

        // Resources
        public float currentEnergy;
        public float maxEnergy;

        // public PollutionManager pollutionManager;

        // Grafting
        public GraftBuffer graftBuffer;

        private WaitForSeconds waitForTick;

        private void Awake()
        {
            graftBuffer = new GraftBuffer(0, 0, 0, false);
            gameGrid = new GameGrid(gridWidth, gridHeight);
            // waitForTick = new WaitForSeconds(tickDurationSeconds);
        }

        private void Start()
        {
            InitializeHeart();
            maxEnergy = startingMaxEnergy;
            currentEnergy = startingEnergy;
            // StartCoroutine(UpdateTickRoutine());
        }

        public void UpdatePlants(PlantBit plantBit)
        {
            plantBit.TickUpdate();

            foreach (PlantBit child in plantBit.children)
            {
                UpdatePlants(child);
            }
        }

        // private IEnumerator UpdateTickRoutine()
        // {
        //     while (true)
        //     {
        //         UpdatePlants(heart);
        //         yield return waitForTick;
        //     }
        // }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        public void InitializeHeart()
        {
            heart = new PlantBit(startingHeartPos, heartBitData, this, true, heartStartingLeaf, heartStartingRoot, heartStartingFruit, heartStartingMaxComponent);
            gameGrid.GetTileAtPosition(startingHeartPos).SetPlantBit(heart);
        }

        public void CreateSprout(PlantBit parent, Vector2Int targetPos)
        {
            PlantBit child = new(targetPos, plantBitData, parent);
            gameGrid.GetTileAtPosition(targetPos).SetPlantBit(child);

            GridEvents.PlantUpdated(targetPos);
        }

        public void KillPlantBit(PlantBit plantBit)
        {
            if (plantBit == heart)
            {
                Debug.Log("GAME OVER HEART IS DEAD");
            }

            // Remove from grid
            gameGrid.GetTileAtPosition(plantBit.position).SetPlantBit(null);
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

        public void RemoveGraftAtPosition(Vector2Int pos, int leaf, int root, int fruit)
        {
            GameTile tile = gameGrid.GetTileAtPosition(pos);
            PlantBit plantBit = tile.GetPlantBit();

            if (plantBit == null) return;

            plantBit.RemoveGraft(leaf, root, fruit);
        }

        public void ApplyGraftAtPosition(Vector2Int pos)
        {
            GameTile tile = gameGrid.GetTileAtPosition(pos);
            PlantBit plantBit = tile.GetPlantBit();
            if (plantBit == null) return;

            if (!graftBuffer.hasContent)
            {
                Debug.LogWarning("No grafts in buffer to apply");
                return;
            }

            plantBit.ApplyGraft(graftBuffer);
            GridEvents.PlantUpdated(pos);
        }

        public void UpdateGraftBuffer(int leaf, int root, int fruit)
        {
            // Warn if overwriting buffer
            if (graftBuffer.hasContent)
            {
                Debug.Log("Previous grafts lost!");
            }

            // Update buffer
            graftBuffer.Update(leaf, root, fruit);
        }

        public void ClearGraftBuffer()
        {
            graftBuffer.Clear();
        }

        public float GetPollutionAtTile(Vector2Int pos)
        {
            return gameGrid.GetTileAtPosition(pos).pollution;
        }

        public void RemovePollutionAtTile(Vector2Int pos, float amount)
        {
            Debug.Log($"Removing {amount} from pollution on tile {pos}");
        }

        public float GetAttackDamageAtTile(Vector2Int pos)
        {
            GameTile tile = gameGrid.GetTileAtPosition(pos);
            PlantBit plantBit = tile.GetPlantBit();
            if (plantBit == null) return 0.0f;

            return plantBit.attackDamage;
        }
    }
}
