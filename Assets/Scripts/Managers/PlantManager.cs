using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class PlantManager : MonoBehaviour
    {
        // Grid reference
        public GridSystem gridSystem;
        public PollutionManager pollutionManager;

        // Resources
        public float currentResources;
        public float maxResourceStorage;

        // Plant tracking
        public Dictionary<Vector2Int, Plant> allPlants = new Dictionary<Vector2Int, Plant>();
        public Plant heartPlant;

        // Grafting
        public GraftBuffer graftBuffer;

        // Configuration (will be set from ScriptableObject)
        [Header("Plant Configuration")]
        public float leafMultiplier = 1.0f;
        public float rootMultiplier = 1.0f;
        public float fruitMultiplier = 1.0f;
        public float baseSproutCost = 10f;
        public float baseGraftCost = 5f;
        public float componentCostScaling = 0.2f;
        public float graftedCostScaling = 0.3f;
        public float graftCostScaling = 0.25f;
        public float removalCostPerComponent = 1.0f;
        public float graftCooldownDuration = 5.0f;
        public float plantTickRate = 0.5f;

        // UI Event for graft buffer warning
        public System.Action<string> OnWarningMessage;

        private void Awake()
        {
            graftBuffer = new GraftBuffer(0, 0, 0, false);
        }

        public void InitializeHeart(Vector2Int position, int startLeaf, int startRoot, int startFruit)
        {
            heartPlant = new Plant();
            heartPlant.position = position;
            heartPlant.leafAmount = startLeaf;
            heartPlant.rootAmount = startRoot;
            heartPlant.fruitAmount = startFruit;
            heartPlant.graftedLeafAmount = 0;
            heartPlant.graftedRootAmount = 0;
            heartPlant.graftedFruitAmount = 0;

            int total = startLeaf + startRoot + startFruit;
            heartPlant.maxComponentAmount = total * 2;
            heartPlant.isHeart = true;
            heartPlant.phase = PlantPhase.Grown;
            heartPlant.hasAutoSprouted = false; // Will auto-sprout on initialization

            // Set references
            heartPlant.plantManager = this;
            heartPlant.gridSystem = gridSystem;
            heartPlant.leafMultiplier = leafMultiplier;
            heartPlant.rootMultiplier = rootMultiplier;
            heartPlant.fruitMultiplier = fruitMultiplier;

            heartPlant.RecalculateStats();

            // Add to grid
            gridSystem.SetTileState(position, TileState.Heart);
            gridSystem.SetEntity(position, heartPlant);
            allPlants[position] = heartPlant;

            // Initialize resources
            UpdateMaxStorage();
            float firstSproutCost = CalculateSproutCost(heartPlant);
            currentResources = firstSproutCost * 1.5f;
        }

        public void CreateSprout(Plant parent, Vector2Int targetPos)
        {
            Plant child = new Plant();
            child.position = targetPos;

            // Inherit all components as natural
            child.leafAmount = parent.leafAmount + parent.graftedLeafAmount;
            child.rootAmount = parent.rootAmount + parent.graftedRootAmount;
            child.fruitAmount = parent.fruitAmount + parent.graftedFruitAmount;
            child.graftedLeafAmount = 0;
            child.graftedRootAmount = 0;
            child.graftedFruitAmount = 0;

            // Calculate max capacity: parentTotal + max(2, 25% of parent total)
            int parentTotal = child.leafAmount + child.rootAmount + child.fruitAmount;
            int bonus = Mathf.Max(2, Mathf.CeilToInt(parentTotal * 0.25f));
            child.maxComponentAmount = parentTotal + bonus;

            // Set hierarchy
            child.parentPlant = parent;
            parent.children.Add(child);

            // Set phase and growth parameters
            child.phase = PlantPhase.Bud;
            child.growthDuration = 10 + Mathf.RoundToInt(parentTotal * 0.5f);
            child.sproutGrowthCost = CalculateSproutCost(parent);
            child.resourcePerTick = child.sproutGrowthCost / child.growthDuration;
            child.growthProgress = 0;

            // Set references
            child.plantManager = this;
            child.gridSystem = gridSystem;
            child.leafMultiplier = leafMultiplier;
            child.rootMultiplier = rootMultiplier;
            child.fruitMultiplier = fruitMultiplier;

            child.RecalculateStats();

            // Add to grid
            gridSystem.SetTileState(targetPos, TileState.Plant);
            gridSystem.SetEntity(targetPos, child);
            allPlants[targetPos] = child;
        }

        public void ManualSprout(Plant parent, Vector2Int targetPos)
        {
            if (!parent.IsValidManualSprout(targetPos))
            {
                Debug.LogWarning($"Invalid manual sprout target: {targetPos}");
                return;
            }

            float cost = CalculateSproutCost(parent) * 2f; // Manual sprouting costs more
            if (!CanAfford(cost))
            {
                Debug.LogWarning("Not enough resources for manual sprouting");
                return;
            }

            SpendResource(cost);
            CreateSprout(parent, targetPos);
        }

        public float CalculateSproutCost(Plant parent)
        {
            int totalComponents = parent.TotalComponents;
            int graftedComponents = parent.TotalGraftedComponents;

            float cost = baseSproutCost * (1 + totalComponents * componentCostScaling) * (1 + graftedComponents * graftedCostScaling);
            return cost;
        }

        public void DeletePlant(Plant plant, bool fromPollution = false)
        {
            // Cascade to all children (no orphans)
            List<Plant> childrenCopy = new List<Plant>(plant.children);
            foreach (Plant child in childrenCopy)
            {
                DeletePlant(child, fromPollution);
            }

            // Remove from parent's children list
            if (plant.parentPlant != null)
            {
                plant.parentPlant.children.Remove(plant);
            }

            // Remove from grid
            allPlants.Remove(plant.position);
            gridSystem.SetTileState(plant.position, TileState.Empty);
            gridSystem.RemoveEntity(plant.position);

            // Update resource cap and clamp
            UpdateMaxStorage();
            currentResources = Mathf.Min(currentResources, maxResourceStorage);

            // Refund 50% if pruned by player
            if (!fromPollution)
            {
                float refund = plant.sproutGrowthCost * 0.5f;
                AddResource(refund);
            }
        }

        public void UpdateMaxStorage()
        {
            maxResourceStorage = 0;
            foreach (Plant plant in allPlants.Values)
            {
                if (plant.phase == PlantPhase.Grown)
                {
                    maxResourceStorage += plant.resourceStorage;
                }
            }
        }

        public void RemoveGrafts(Plant plant, int leaf, int root, int fruit)
        {
            if (plant.graftingCooldown > 0)
            {
                Debug.LogWarning("Plant is on grafting cooldown");
                return;
            }

            // Check if plant has enough grafts to remove
            if (leaf > plant.graftedLeafAmount || root > plant.graftedRootAmount || fruit > plant.graftedFruitAmount)
            {
                Debug.LogWarning("Trying to remove more grafts than available");
                return;
            }

            // Cost to remove
            int totalRemoved = leaf + root + fruit;
            float removalCost = totalRemoved * removalCostPerComponent;
            if (!CanAfford(removalCost))
            {
                Debug.LogWarning("Not enough resources to remove grafts");
                return;
            }

            SpendResource(removalCost);

            // Warn if overwriting buffer
            if (graftBuffer.hasContent)
            {
                OnWarningMessage?.Invoke("Previous grafts lost!");
            }

            // Update buffer
            graftBuffer = new GraftBuffer(leaf, root, fruit, true);

            // Remove from plant
            plant.graftedLeafAmount -= leaf;
            plant.graftedRootAmount -= root;
            plant.graftedFruitAmount -= fruit;
            plant.RecalculateStats();

            // Start cooldown
            plant.graftingCooldown = graftCooldownDuration;
        }

        public void ApplyGrafts(Plant plant)
        {
            if (!graftBuffer.hasContent)
            {
                Debug.LogWarning("No grafts in buffer to apply");
                return;
            }

            if (plant.graftingCooldown > 0)
            {
                Debug.LogWarning("Plant is on grafting cooldown");
                return;
            }

            // Check capacity
            int newTotal = plant.TotalComponents + graftBuffer.TotalComponents;
            if (newTotal > plant.maxComponentAmount)
            {
                Debug.LogWarning("Would exceed plant's max component capacity");
                return;
            }

            // Calculate cost
            float cost = baseGraftCost * (1 + plant.TotalComponents * graftCostScaling);
            if (!CanAfford(cost))
            {
                Debug.LogWarning("Not enough resources to apply grafts");
                return;
            }

            SpendResource(cost);

            // Apply to plant
            plant.graftedLeafAmount += graftBuffer.leafAmount;
            plant.graftedRootAmount += graftBuffer.rootAmount;
            plant.graftedFruitAmount += graftBuffer.fruitAmount;
            plant.RecalculateStats();

            // Start cooldown
            plant.graftingCooldown = graftCooldownDuration;

            // Clear buffer
            ClearGraftBuffer();
        }

        public void ClearGraftBuffer()
        {
            graftBuffer = new GraftBuffer(0, 0, 0, false);
        }

        public void AddResource(float amount)
        {
            currentResources += amount;
            currentResources = Mathf.Min(currentResources, maxResourceStorage);
        }

        public void SpendResource(float amount)
        {
            currentResources -= amount;
            currentResources = Mathf.Max(currentResources, 0);
        }

        public bool CanAfford(float cost)
        {
            return currentResources >= cost;
        }

        public void RemovePollutionTile(Vector2Int pos)
        {
            // Delegate to PollutionManager
            if (pollutionManager != null)
            {
                pollutionManager.RemovePollutionTile(pos);
            }
        }

        public void UpdatePlants()
        {
            // Collect source damage (batched)
            Dictionary<PollutionSource, float> sourceDamage = new Dictionary<PollutionSource, float>();

            foreach (Plant plant in allPlants.Values)
            {
                if (plant.phase == PlantPhase.Bud)
                {
                    // Bud growth
                    if (CanAfford(plant.resourcePerTick))
                    {
                        SpendResource(plant.resourcePerTick);
                        plant.growthProgress++;

                        if (plant.growthProgress >= plant.growthDuration)
                        {
                            plant.TransitionToGrownPhase();
                            UpdateMaxStorage(); // Bud became grown, update cap
                        }
                    }
                    // Else: growth pauses

                    // Check overwhelm (uses parent's dynamic ATD)
                    plant.CheckOverwhelm();
                }
                else if (plant.phase == PlantPhase.Grown)
                {
                    // Extract resources and reduce pollution
                    plant.ExtractFromAdjacentPollution();

                    // Collect source damage
                    List<Vector2Int> neighbors = gridSystem.GetNeighbors(plant.position, includeDiagonal: false);
                    foreach (Vector2Int neighborPos in neighbors)
                    {
                        if (gridSystem.GetTileState(neighborPos) == TileState.PollutionSource)
                        {
                            PollutionSource source = gridSystem.GetEntity<PollutionSource>(neighborPos);
                            if (source != null)
                            {
                                float pollutionAtSource = pollutionManager.GetPollutionLevelAt(source.position);
                                if (plant.attackDamage > pollutionAtSource)
                                {
                                    float margin = plant.attackDamage - pollutionAtSource;
                                    float damage = margin * 0.1f;

                                    if (!sourceDamage.ContainsKey(source))
                                    {
                                        sourceDamage[source] = 0;
                                    }
                                    sourceDamage[source] += damage;
                                }
                            }
                        }
                    }

                    // Check overwhelm (except Heart)
                    if (!plant.isHeart)
                    {
                        plant.CheckOverwhelm();
                    }
                }

                // Update cooldowns
                if (plant.graftingCooldown > 0)
                {
                    plant.graftingCooldown -= plantTickRate;
                    if (plant.graftingCooldown < 0) plant.graftingCooldown = 0;
                }
            }

            // Apply batched source damage
            foreach (var kvp in sourceDamage)
            {
                kvp.Key.TakeDamage(kvp.Value);
            }
        }
    }
}

