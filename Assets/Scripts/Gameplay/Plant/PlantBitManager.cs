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

        public GraftBuffer(int leaf, int root, int fruit)
        {
            leafCount = leaf;
            rootCount = root;
            fruitCount = fruit;
            hasContent = (leaf + root + fruit) > 0;
        }

        public void Update(int leaf, int root, int fruit)
        {
            Debug.Log($"[PlantBitManager] GraftBuffer updated -> L:{leaf} R:{root} F:{fruit}");
            leafCount += leaf;
            rootCount += root;
            fruitCount += fruit;
            hasContent = (leafCount + rootCount + fruitCount) > 0;
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

        //a list of all the plants in the game
        private List<PlantBit> plantsAlive = new List<PlantBit>();

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
            Debug.Log("[PlantBitManager] Initializing manager with shared grid");
            gameGrid = sharedGrid;
            graftBuffer = new GraftBuffer(0, 0, 0);
        }

        public void InitializeHeart(Vector2Int pos)
        {
            Debug.Log($"[PlantBitManager] Initializing Heart at {pos}");

            heart = new PlantBit(pos, heartBitData, this, true,
                heartStartingLeaf, heartStartingRoot, heartStartingFruit);

            gameGrid.SetEntity(heart);
            plantsAlive.Add(heart); // Add to tracking list

            maxEnergy = startingMaxEnergy;
            currentEnergy = startingEnergy;

            Debug.Log($"[PlantBitManager] Heart placed at {pos} | Energy: {currentEnergy}/{maxEnergy}");

            GridEvents.PlantUpdated(pos);
            heart.AttemptSprout(false);
        }

        public void UpdatePlants(PlantBit plantBit)
        {
            // === PHASE 1: Categorize all plants ===
            List<PlantBit> healthyGrownPlants = new List<PlantBit>();
            List<PlantBit> infectedGrownPlants = new List<PlantBit>();
            List<PlantBit> healthyBuds = new List<PlantBit>();
            List<PlantBit> infectedBuds = new List<PlantBit>();
            List<PlantBit> fullyInfectedPlants = new List<PlantBit>();
            
            foreach (var plant in plantsAlive)
            {
                if (plant.phase == PlantBitPhase.FullyInfected)
                {
                    fullyInfectedPlants.Add(plant);
                }
                else if (plant.phase == PlantBitPhase.Grown)
                {
                    if (plant.isInfected)
                        infectedGrownPlants.Add(plant);
                    else
                        healthyGrownPlants.Add(plant);
                }
                else if (plant.phase == PlantBitPhase.Bud)
                {
                    if (plant.isInfected)
                        infectedBuds.Add(plant);
                    else
                        healthyBuds.Add(plant);
                }
            }

            // === PHASE 2: Passive energy generation (ALL plants) ===
            foreach (var plant in healthyGrownPlants)
                plant.PassiveEnergyGain();
            
            foreach (var plant in infectedGrownPlants)
                plant.PassiveEnergyGain(); // 50% rate

            // === PHASE 3: Check infection status (infected plants only) ===
            foreach (var plant in infectedGrownPlants)
            {
                float infectionPercent = pollutionManager.CheckPlantInfection(plant);
                // Could store this percent on plant for UI display later
            }
            
            foreach (var plant in infectedBuds)
            {
                float infectionPercent = pollutionManager.CheckPlantInfection(plant);
                // Could store this percent on plant for UI display later
            }

            // === PHASE 4: Calculate & pay maintenance (healthy plants only) ===
            float totalMaintenance = 0f;
            foreach (var plant in healthyGrownPlants)
            {
                totalMaintenance += plant.maintenanceCost;
            }

            if (!RemoveEnergy(totalMaintenance)) return;

            // === PHASE 5: Extraction (healthy plants only) ===
            foreach (var plant in healthyGrownPlants)
            {
                plant.ExtractEnergy();
            }

            // === PHASE 6: Calculate & pay bud growth (healthy buds only) ===
            float totalGrowthCost = 0f;
            foreach (var bud in healthyBuds)
            {
                totalGrowthCost += bud.maintenanceCost * 2f;
            }

            if (totalGrowthCost > 0 && RemoveEnergy(totalGrowthCost))
            {
                Debug.Log($"[PlantBitManager] Growing {healthyBuds.Count} buds, cost: {totalGrowthCost:F2}");
                
                foreach (var bud in healthyBuds)
                {
                    bud.growthProgress++;
                    if (bud.growthProgress >= bud.data.fullGrowthTicks)
                    {
                        bud.TransitionToGrownPhase();
                    }
                }
            }

            // === PHASE 7: Handle fully infected plants ===
            // (Currently they just exist, waiting to spread via coroutines)
            // Could add logic here if needed
        }

        public void CreateSprout(PlantBit parent, Vector2Int targetPos)
        {
            if (parent == null) return;

            Debug.Log($"[PlantBitManager] Creating sprout at {targetPos} (parent: {parent.position})");

            PlantBit child = new PlantBit(targetPos, plantBitData, parent);
            gameGrid.SetEntity(child);
            plantsAlive.Add(child); // Add to tracking list

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
            plantsAlive.Remove(plantBit); // Remove from tracking list

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

            plantBit.ApplyGraft(graftBuffer.leafCount, graftBuffer.rootCount, graftBuffer.fruitCount);
            GridEvents.PlantUpdated(pos);
        }

        public void RemoveGraftAtPosition(Vector2Int pos, int leaf, int root, int fruit)
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

            plantBit.RemoveGraft(leaf, root, fruit);
            GridEvents.PlantUpdated(pos);
        }

        public void AddPlant(PlantBit plant)
        {
            plantsAlive.Add(plant);
        }

        public void RemovePlant(PlantBit plant)
        {
            plantsAlive.Remove(plant);
        }

        // === INFECTION SYSTEM ===
        
        public Coroutine StartInfectionSpread(PlantBit infectedPlant, float spreadTimer)
        {
            // Start coroutine to spread infection after timer and return reference
            return StartCoroutine(InfectionSpreadCoroutine(infectedPlant, spreadTimer));
        }

        private System.Collections.IEnumerator InfectionSpreadCoroutine(PlantBit infectedPlant, float spreadTimer)
        {
            //Debug.Log($"[PlantBitManager] Infection spreading from {infectedPlant.position} in {spreadTimer} seconds");
            
            // Wait for the timer
            yield return new WaitForSeconds(spreadTimer);

            // Check if plant still exists and is still infected
            if (infectedPlant == null || !plantsAlive.Contains(infectedPlant))
            {
                //Debug.Log($"[PlantBitManager] Infection spread cancelled - plant no longer exists");
                yield break;
            }

            if (!infectedPlant.isInfected)
            {
                //Debug.Log($"[PlantBitManager] Infection spread cancelled - plant is no longer infected");
                yield break;
            }

            // Spread to parent (virus goes UP the tree)
            if (infectedPlant.parent != null && !infectedPlant.parent.isInfected)
            {
                //Debug.Log($"[PlantBitManager] Virus spreading from {infectedPlant.position} to parent {infectedPlant.parent.position}");
                infectedPlant.parent.isInfected = true;
                infectedPlant.parent.InfectionSpread(spreadTimer);
                GridEvents.PlantUpdated(infectedPlant.parent.position);

                // Check if parent is heart - GAME OVER
                if (infectedPlant.parent.isHeart)
                {
                    //Debug.Log("[PlantBitManager] GAME OVER: HEART IS INFECTED!");
                    GameManager.Instance.TriggerLoss();
                }
            }

            // Spread to children (virus goes DOWN the tree)
            foreach (PlantBit child in infectedPlant.children)
            {
                if (child != null && !child.isInfected)
                {
                    //Debug.Log($"[PlantBitManager] Virus spreading from {infectedPlant.position} to child {child.position}");
                    child.isInfected = true;
                    child.InfectionSpread(spreadTimer);
                    GridEvents.PlantUpdated(child.position);
                }
            }

            // Clear coroutine reference since it's done
            infectedPlant.infectionCoroutine = null;
        }
    }
}
