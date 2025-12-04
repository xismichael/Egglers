using System;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public enum PlantBitPhase
    {
        Bud,        // Consuming resources to grow
        Grown       // Fully functional, can sprout
    }

    public class PlantBit
    {
        // Basic data
        public Vector2Int position;
        public PlantBitData data;
        public PlantBitManager plantManager;
        public bool isHeart;
        public PlantBitPhase phase;

        // Bud growth tracking
        public int growthProgress;

        // Natural components (exponential scaling)
        public int leafCount;
        public int rootCount;
        public int fruitCount;

        // Grafted components (linear scaling)
        public int graftedLeafCount;
        public int graftedRootCount;
        public int graftedFruitCount;

        // Hierarchy
        public PlantBit parent;
        public List<PlantBit> children = new();

        public int maxComponentCount;
        public float sproutCost;

        public float attackDamage;
        public float extractionRate;
        public float energyStorage;

        // Derived stats (calculated)
        public int TotalNaturalComponents => leafCount + rootCount + fruitCount;
        public int TotalGraftedComponents => graftedLeafCount + graftedRootCount + graftedFruitCount;
        public int TotalComponents => TotalNaturalComponents + TotalGraftedComponents;

        // Cooldowns
        public int graftingCooldown;
        public int sproutingCooldown;

        public float leafEnergyGainMultiplier = 1f; // energy per leaf per tick
        public float energyPerRootPerTick = 2f; // energy consumed per root to extract pollution


        public PlantBit(Vector2Int pos, PlantBitData newData, PlantBitManager manager,
            bool heart = false, int startLeaf = 0, int startRoot = 0, int startFruit = 0, int startMaxComponent = 0)
        {
            Debug.Log($"[PlantBit] Created heart={heart} at {pos}");

            position = pos;
            data = newData;
            plantManager = manager;
            isHeart = heart;

            leafCount = startLeaf;
            rootCount = startRoot;
            fruitCount = startFruit;

            graftedLeafCount = 0;
            graftedRootCount = 0;
            graftedFruitCount = 0;

            // Setup hierarchy
            parent = null;
            children = new List<PlantBit>();

            phase = PlantBitPhase.Bud;
            growthProgress = 0;

            maxComponentCount = Mathf.Max(startMaxComponent, 1);
            sproutCost = CalculateSproutCost();

            UpdateStats();

            // Register self in the grid
            plantManager.gameGrid.SetEntity(this);
            // plantManager.gameGrid.SetTileState(position, TileState.Plant);
            Debug.Log($"[PlantBit] Registered to grid at {position}");
        }

        public PlantBit(Vector2Int pos, PlantBitData newData, PlantBit parentBit)
        {
            Debug.Log($"[PlantBit] Created child at {pos} (parent: {parentBit.position})");

            position = pos;
            data = newData;
            plantManager = parentBit.plantManager;
            isHeart = false;

            // Inherit all components (natural + grafted) from parent
            leafCount = parentBit.leafCount + parentBit.graftedLeafCount;
            rootCount = parentBit.rootCount + parentBit.graftedRootCount;
            fruitCount = parentBit.fruitCount + parentBit.graftedFruitCount;

            graftedLeafCount = 0;
            graftedRootCount = 0;
            graftedFruitCount = 0;

            // Setup hierarchy
            parent = parentBit;
            parentBit.children.Add(this);
            children = new List<PlantBit>();

            phase = PlantBitPhase.Bud;
            growthProgress = 0;

            maxComponentCount = CalculateMaxComponentCount();
            sproutCost = CalculateSproutCost();

            UpdateStats();

            // Register self in the grid
            plantManager.gameGrid.SetEntity(this);
            Debug.Log($"[PlantBit] Registered child to grid at {position}");
        }

        private int CalculateMaxComponentCount()
        {
            int parentTotal = parent != null ? parent.TotalComponents : 0;
            int bonus = Mathf.Max(data.maxComponentIncrease, Mathf.CeilToInt(parentTotal * data.maxComponentBonus));
            return parentTotal + bonus;
        }

        private float CalculateSproutCost()
        {
            float naturalTax = TotalNaturalComponents * data.naturalSproutCostScaling;
            float graftedTax = TotalGraftedComponents * data.graftedSproutCostScaling;
            return data.baseSproutCost + naturalTax + graftedTax;
        }

        private float CalculateStat(int natural, int grafted, float baseMultiplier)
        {
            float naturalPower = Mathf.Pow(natural, data.naturalComponentPower) * baseMultiplier;
            float graftedPower = grafted * baseMultiplier; // linear scaling
            return naturalPower + graftedPower;
        }

        public void UpdateStats()
        {
            attackDamage = CalculateStat(leafCount, graftedLeafCount, data.leafMultiplier);
            extractionRate = CalculateStat(rootCount, graftedRootCount, data.rootMultiplier);
            energyStorage = CalculateStat(fruitCount, graftedFruitCount, data.fruitMultiplier);
            sproutCost = CalculateSproutCost();
        }

        public void TransitionToGrownPhase()
        {
            Debug.Log($"[PlantBit] TransitionToGrownPhase at {position}");

            phase = PlantBitPhase.Grown;
            UpdateStats();
            plantManager.AddMaxEnergy(energyStorage);

            // AttemptSprout();

            GridEvents.PlantUpdated(position);
        }

        private void AttemptSprout()
        {
            Debug.Log($"[PlantBit] AttemptSprout at {position}");

            GridSystem grid = plantManager.gameGrid;

            // Check if current tile has pollution
            PollutionTile currentTile = grid.GetEntity<PollutionTile>(position);
            PollutionSource currentSource = grid.GetEntity<PollutionSource>(position);
            float totalPollution = 0f;

            if (currentTile != null) totalPollution += currentTile.GetTotalPollution();
            if (currentSource != null) totalPollution += currentSource.GetTotalPollution();

            if (totalPollution > 0f)
            {
                Debug.Log($"[PlantBit] Cannot sprout: current tile at {position} has pollution ({totalPollution})");
                return;
            }

            // Only attempt sprout if tile is clean
            List<Vector2Int> neighbors = grid.GetNeighbors(position);
            foreach (Vector2Int neighborPos in neighbors)
            {
                // Allow sprouting as long as no PlantBit is already present
                bool canSprout = grid.GetEntity<PlantBit>(neighborPos) == null;

                Debug.Log($"[PlantBit] Checking sprout spot {neighborPos} | canSprout={canSprout}");

                if (canSprout && plantManager.RemoveEnergy(sproutCost))
                {
                    Debug.Log($"[PlantBit] Sprouting at {neighborPos} (cost: {sproutCost})");
                    plantManager.CreateSprout(this, neighborPos);
                }
                else if (canSprout)
                {
                    Debug.Log($"[PlantBit] Not enough energy to sprout at {neighborPos} (cost: {sproutCost})");
                    return;
                }
            }

            sproutingCooldown = data.sproutingCooldownDuration;
            Debug.Log($"[PlantBit] Sprouting cooldown set to {sproutingCooldown}");
        }

        public void TickUpdate()
        {
            ExtractEnergy();

            if (phase == PlantBitPhase.Bud)
            {
                // Debug.Log($"[PlantBit] TickUpdate BUD at {position} | progress: {growthProgress}/{data.fullGrowthTicks}");

                if (plantManager.RemoveEnergy(data.tickGrowthCost))
                {
                    growthProgress++;
                    if (growthProgress >= data.fullGrowthTicks)
                    {
                        Debug.Log($"[PlantBit] Bud fully grown! Transitioning at {position}");
                        TransitionToGrownPhase();
                    }
                }
            }
            else if (phase == PlantBitPhase.Grown)
            {
                // Passive energy from leaves
                float leafEnergyGain = leafCount * 1f + graftedLeafCount * 2f; // adjust multiplier as needed
                plantManager.AddEnergy(leafEnergyGain);
                ExtractEnergy();

                // Handle cooldowns
                if (graftingCooldown > 0) graftingCooldown--;
                if (sproutingCooldown > 0) sproutingCooldown--;
                else if (UnityEngine.Random.value < data.sproutingChance)
                {
                    AttemptSprout();
                }
            }
        }

        public void ExtractEnergy()
        {
            GridSystem grid = plantManager.gameGrid;

            // Scan current tile + neighbors
            List<Vector2Int> tilesToScan = grid.GetNeighbors(position);
            tilesToScan.Add(position);

            float baseDamage = (rootCount + graftedRootCount) * data.rootMultiplier;
            float energyCostPerDamage = 0.2f; // customizable, can scale with roots

            foreach (Vector2Int pos in tilesToScan)
            {
                PollutionTile tile = grid.GetEntity<PollutionTile>(pos);
                PollutionSource source = grid.GetEntity<PollutionSource>(pos);

                if (tile != null)
                {
                    float energyCost = baseDamage * energyCostPerDamage;
                    if (!plantManager.RemoveEnergy(energyCost))
                    {
                        // Debug.Log($"[PlantBit] Not enough energy to attack PollutionTile at {pos}");
                        continue; // skip if not enough energy
                    }

                    tile.TakeDamage(baseDamage);
                    // plantManager.AddEnergy(baseDamage); // optional: gain energy from cleaned pollution
                    // Debug.Log($"[PlantBit] Attacked PollutionTile at {pos} | Damage: {baseDamage}, Energy spent: {energyCost}");
                }

                if (source != null)
                {
                    float energyCost = baseDamage * energyCostPerDamage;
                    if (!plantManager.RemoveEnergy(energyCost))
                    {
                        Debug.Log($"[PlantBit] Not enough energy to attack PollutionSource at {pos}");
                        continue;
                    }

                    source.TakeDamage(baseDamage);
                    plantManager.AddEnergy(baseDamage);
                    Debug.Log($"[PlantBit] Attacked PollutionSource at {pos} | Damage: {baseDamage}, Energy spent: {energyCost}");
                }
            }
        }

        public void Kill()
        {
            Debug.Log($"[PlantBit] KILL at {position}");

            // Remove energy contribution
            plantManager.RemoveMaxEnergy(energyStorage);

            // Detach from parent
            parent?.children.Remove(this);

            // Recursively kill children
            foreach (PlantBit child in new List<PlantBit>(children))
            {
                Debug.Log($"[PlantBit] Killing child {child.position}");
                plantManager.KillPlantBit(child);
            }

            children.Clear();

            // Remove self from grid using type-safe removal
            plantManager.gameGrid.RemoveEntity<PlantBit>(this);

            Debug.Log($"[PlantBit] Removed from grid at {position}");
        }

        public void RemoveGraft(int leaf, int root, int fruit)
        {
            Debug.Log($"[PlantBit] RemoveGraft at {position} | L:{leaf} R:{root} F:{fruit}");

            if (phase == PlantBitPhase.Bud)
            {
                Debug.LogWarning("Cannot remove graft, plant is still a bud");
                return;
            }

            if (graftingCooldown > 0)
            {
                Debug.LogWarning("Cannot remove graft, plant is on grafting cooldown");
                return;
            }

            // Check if plant has enough grafts to remove
            if (leaf > (leafCount + graftedLeafCount) || root > (rootCount + graftedRootCount) || fruit > (fruitCount + graftedFruitCount))
            {
                Debug.LogWarning("Cannot remove graft, trying to remove more grafts than available");
                return;
            }

            // Cost to remove
            int totalRemoved = leaf + root + fruit;
            float removalCost = totalRemoved * data.removalCostPerComponent;
            if (!plantManager.RemoveEnergy(0))
            {
                Debug.LogWarning("Cannot remove graft, not enough resources to remove components");
                return;
            }

            plantManager.graftBuffer.Update(leaf, root, fruit);

            // Remove from plant
            graftedLeafCount -= leaf;
            graftedRootCount -= root;
            graftedFruitCount -= fruit;
            UpdateStats();

            // Start cooldown
            graftingCooldown = data.graftCooldownDuration;
        }

        public void ApplyGraft(GraftBuffer graft)
        {
            Debug.Log($"[PlantBit] Attempting to ApplyGraft at {position} | Graft: L{graft.leafCount} R{graft.rootCount} F{graft.fruitCount}");

            // Check grafting cooldown
            if (graftingCooldown > 0)
            {
                Debug.Log($"[PlantBit] Cannot apply graft: Grafting cooldown active ({graftingCooldown} ticks remaining)");
                return;
            }

            // Check max component limit
            if (TotalComponents + graft.TotalComponents > maxComponentCount)
            {
                Debug.Log($"[PlantBit] Cannot apply graft: Total components ({TotalComponents + graft.TotalComponents}) exceed max ({maxComponentCount})");
                return;
            }

            // Calculate energy cost
            float cost = data.baseGraftCost * (1 + TotalComponents * data.graftCostScaling);
            Debug.Log($"[PlantBit] Calculated graft cost: {cost} energy");

            // Attempt to pay energy
            if (!plantManager.RemoveEnergy(cost))
            {
                Debug.Log($"[PlantBit] Cannot apply graft: Not enough energy (Current: {plantManager.currentEnergy}, Needed: {cost})");
                return;
            }

            // Apply graft
            graftedLeafCount += graft.leafCount;
            graftedRootCount += graft.rootCount;
            graftedFruitCount += graft.fruitCount;

            Debug.Log($"[PlantBit] Graft applied successfully | New grafted counts -> L:{graftedLeafCount} R:{graftedRootCount} F:{graftedFruitCount}");

            // Update derived stats
            UpdateStats();
            Debug.Log($"[PlantBit] Stats updated after graft | AttackDamage: {attackDamage}, ExtractionRate: {extractionRate}, EnergyStorage: {energyStorage}");

            // Clear the manager's graft buffer
            plantManager.graftBuffer.Clear();
            Debug.Log("[PlantBit] Graft buffer cleared in PlantBitManager");

            // Set grafting cooldown
            graftingCooldown = data.graftCooldownDuration;
            Debug.Log($"[PlantBit] Grafting cooldown set to {graftingCooldown} ticks");
        }

        public void Nip()
        {
            if (isHeart)
            {
                Debug.LogWarning("[PlantBit] Cannot nip the heart!");
                return;
            }

            float refund = sproutCost * 0.5f;

            Debug.Log($"[PlantBit] Killing plant at {position}, refunding {refund} energy");

            plantManager.AddEnergy(refund);
            plantManager.KillPlantBit(this);
        }
    }
}
