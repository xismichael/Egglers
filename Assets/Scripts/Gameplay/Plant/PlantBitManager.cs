using System.Collections;
using UnityEngine;

namespace Egglers
{
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

        // Plant tracking
        // public Dictionary<Vector2Int, Plant> allPlants = new Dictionary<Vector2Int, Plant>();
        
        // public GridSystem gridSystem;
        // public PollutionManager pollutionManager;

        // Grafting
        // public GraftBuffer graftBuffer;

        // UI Event for graft buffer warning
        // public System.Action<string> OnWarningMessage;

        private WaitForSeconds waitForTick;

        private void Awake()
        {
            // graftBuffer = new GraftBuffer(0, 0, 0, false);
            gameGrid = new GameGrid(gridWidth, gridHeight); // THIS IS THE PROBLEM LINE, NOT SURE WHAT IS WRONG
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

            // Initialize resources
            // UpdateMaxStorage();
            // float firstSproutCost = CalculateSproutCost(heart);
            // currentEnergy = firstSproutCost * 1.5f;
        }

        public void CreateSprout(PlantBit parent, Vector2Int targetPos)
        {
            PlantBit child = new PlantBit(targetPos, plantBitData, parent);
            child.position = targetPos;

            gameGrid.GetTileAtPosition(targetPos).SetPlantBit(child);
        }

        // public float CalculateSproutCost(PlantBit parent)
        // {
        //     int totalComponents = parent.TotalComponents;
        //     int graftedComponents = parent.TotalGraftedComponents;

        //     float cost = baseSproutCost * (1 + totalComponents * componentCostScaling) * (1 + graftedComponents * graftedCostScaling);
        //     return cost;
        // }

        // public void ManualSprout(Plant parent, Vector2Int targetPos)
        // {
        //     if (!parent.IsValidManualSprout(targetPos))
        //     {
        //         Debug.LogWarning($"Invalid manual sprout target: {targetPos}");
        //         return;
        //     }

        //     float cost = CalculateSproutCost(parent) * 2f; // Manual sprouting costs more
        //     if (!CanAfford(cost))
        //     {
        //         Debug.LogWarning("Not enough resources for manual sprouting");
        //         return;
        //     }

        //     SpendResource(cost);
        //     CreateSprout(parent, targetPos);
        // }

        // public void DeletePlant(Plant plant, bool fromPollution = false)
        // {
        //     // Cascade to all children (no orphans)
        //     List<Plant> childrenCopy = new List<Plant>(plant.children);
        //     foreach (Plant child in childrenCopy)
        //     {
        //         DeletePlant(child, fromPollution);
        //     }

        //     // Remove from parent's children list
        //     if (plant.parentPlant != null)
        //     {
        //         plant.parentPlant.children.Remove(plant);
        //     }

        //     // Remove from grid
        //     allPlants.Remove(plant.position);
        //     gridSystem.SetTileState(plant.position, TileState.Empty);
        //     gridSystem.RemoveEntity(plant.position);

        //     // Update resource cap and clamp
        //     UpdateMaxStorage();
        //     currentResources = Mathf.Min(currentResources, maxResourceStorage);

        //     // Refund 50% if pruned by player
        //     if (!fromPollution)
        //     {
        //         float refund = plant.sproutGrowthCost * 0.5f;
        //         AddResource(refund);
        //     }
        // }

        public void AddMaxEnergy(float amount)
        {
            maxEnergy += amount;
        }

        public void RemoveMaxEnergy(float amount)
        {
            maxEnergy = Mathf.Max(maxEnergy - amount, 0);
        }

        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        }

        public bool RemoveEnergy(float amount)
        {
            float newEnergy = Mathf.Max(currentEnergy - amount, 0);
            if (newEnergy == 0) return false;

            currentEnergy = newEnergy;
            return true;
        }

        // public void RemoveGrafts(Plant plant, int leaf, int root, int fruit)
        // {
        //     if (plant.graftingCooldown > 0)
        //     {
        //         Debug.LogWarning("Plant is on grafting cooldown");
        //         return;
        //     }

        //     // Check if plant has enough grafts to remove
        //     if (leaf > plant.graftedLeafAmount || root > plant.graftedRootAmount || fruit > plant.graftedFruitAmount)
        //     {
        //         Debug.LogWarning("Trying to remove more grafts than available");
        //         return;
        //     }

        //     // Cost to remove
        //     int totalRemoved = leaf + root + fruit;
        //     float removalCost = totalRemoved * removalCostPerComponent;
        //     if (!CanAfford(removalCost))
        //     {
        //         Debug.LogWarning("Not enough resources to remove grafts");
        //         return;
        //     }

        //     SpendResource(removalCost);

        //     // Warn if overwriting buffer
        //     if (graftBuffer.hasContent)
        //     {
        //         OnWarningMessage?.Invoke("Previous grafts lost!");
        //     }

        //     // Update buffer
        //     graftBuffer = new GraftBuffer(leaf, root, fruit, true);

        //     // Remove from plant
        //     plant.graftedLeafAmount -= leaf;
        //     plant.graftedRootAmount -= root;
        //     plant.graftedFruitAmount -= fruit;
        //     plant.RecalculateStats();

        //     // Start cooldown
        //     plant.graftingCooldown = graftCooldownDuration;
        // }

        // public void ApplyGrafts(Plant plant)
        // {
        //     if (!graftBuffer.hasContent)
        //     {
        //         Debug.LogWarning("No grafts in buffer to apply");
        //         return;
        //     }

        //     if (plant.graftingCooldown > 0)
        //     {
        //         Debug.LogWarning("Plant is on grafting cooldown");
        //         return;
        //     }

        //     // Check capacity
        //     int newTotal = plant.TotalComponents + graftBuffer.TotalComponents;
        //     if (newTotal > plant.maxComponentAmount)
        //     {
        //         Debug.LogWarning("Would exceed plant's max component capacity");
        //         return;
        //     }

        //     // Calculate cost
        //     float cost = baseGraftCost * (1 + plant.TotalComponents * graftCostScaling);
        //     if (!CanAfford(cost))
        //     {
        //         Debug.LogWarning("Not enough resources to apply grafts");
        //         return;
        //     }

        //     SpendResource(cost);

        //     // Apply to plant
        //     plant.graftedLeafAmount += graftBuffer.leafAmount;
        //     plant.graftedRootAmount += graftBuffer.rootAmount;
        //     plant.graftedFruitAmount += graftBuffer.fruitAmount;
        //     plant.RecalculateStats();

        //     // Start cooldown
        //     plant.graftingCooldown = graftCooldownDuration;

        //     // Clear buffer
        //     ClearGraftBuffer();
        // }

        // public void ClearGraftBuffer()
        // {
        //     graftBuffer = new GraftBuffer(0, 0, 0, false);
        // }

        

        // public void RemovePollutionTile(Vector2Int pos)
        // {
        //     // Delegate to PollutionManager
        //     if (pollutionManager != null)
        //     {
        //         pollutionManager.RemovePollutionTile(pos);
        //     }
        // }

        
    }
}
