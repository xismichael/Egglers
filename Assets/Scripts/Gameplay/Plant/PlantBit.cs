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
        public float graftingCooldown;

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

            // Inherit all components as natural
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
            // plantManager.gameGrid.SetTileState(position, TileState.Plant);
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

            AttemptAutoSprout();

            GridEvents.PlantUpdated(position);
        }

        private void AttemptAutoSprout()
        {
            Debug.Log($"[PlantBit] AttemptAutoSprout at {position}");

            List<Vector2Int> neighbors = plantManager.gameGrid.GetNeighbors(position);
            foreach (Vector2Int neighborPos in neighbors)
            {
                // Allow sprouting as long as no PlantBit is already present
                bool canSprout = plantManager.gameGrid.GetEntity<PlantBit>(neighborPos) == null;

                Debug.Log($"[PlantBit] Checking sprout spot {neighborPos} | canSprout={canSprout}");

                if (canSprout && plantManager.RemoveEnergy(sproutCost))
                {
                    Debug.Log($"[PlantBit] Sprouting at {neighborPos} (cost: {sproutCost})");
                    plantManager.CreateSprout(this, neighborPos);
                }
            }
        }

        public void TickUpdate()
        {
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
                // Debug.Log($"[PlantBit] TickUpdate GROWN at {position}");
                ExtractEnergy();

                if (graftingCooldown > 0)
                {
                    graftingCooldown--;
                    if (graftingCooldown < 0) graftingCooldown = 0;
                    Debug.Log($"[PlantBit] GraftingCooldown at {position}: {graftingCooldown}");
                }
            }
        }

        public void ExtractEnergy()
        {
            GridSystem grid = plantManager.gameGrid;

            // Check for PollutionTile
            PollutionTile pollutionTile = grid.GetEntity<PollutionTile>(position);
            float totalPollution = 0f;

            if (pollutionTile != null)
                totalPollution += pollutionTile.GetTotalPollution();

            // Check for PollutionSource
            PollutionSource pollutionSource = grid.GetEntity<PollutionSource>(position);
            if (pollutionSource != null)
                totalPollution += pollutionSource.GetTotalPollution();

            if (totalPollution <= 0f)
            {
                // Debug.Log($"[PlantBit] Pollution already depleted at {position}");
                return;
            }

            // Determine energy to extract
            float energyGain = Mathf.Min(extractionRate, totalPollution);
            Debug.Log($"[PlantBit] Extracting {energyGain} energy at {position} (total pollution: {totalPollution})");

            // Apply damage proportionally
            if (pollutionTile != null)
                pollutionTile.TakeDamage(energyGain);

            if (pollutionSource != null)
                pollutionSource.TakeDamage(energyGain);

            // Give energy to plant manager
            plantManager.AddEnergy(energyGain);
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

            if (graftingCooldown > 0) return;

            if (leaf > graftedLeafCount || root > graftedRootCount || fruit > graftedFruitCount) return;

            float removalCost = (leaf + root + fruit) * data.removalCostPerComponent;
            if (!plantManager.RemoveEnergy(removalCost)) return;

            plantManager.graftBuffer.Update(leaf, root, fruit);

            graftedLeafCount -= leaf;
            graftedRootCount -= root;
            graftedFruitCount -= fruit;
            UpdateStats();

            graftingCooldown = data.graftCooldownDuration;
        }

        public void ApplyGraft(GraftBuffer graft)
        {
            Debug.Log($"[PlantBit] ApplyGraft at {position} | Graft: L{graft.leafCount} R{graft.rootCount} F{graft.fruitCount}");

            if (graftingCooldown > 0) return;

            if (TotalComponents + graft.TotalComponents > maxComponentCount) return;

            float cost = data.baseGraftCost * (1 + TotalComponents * data.graftCostScaling);
            if (!plantManager.RemoveEnergy(cost)) return;

            graftedLeafCount += graft.leafCount;
            graftedRootCount += graft.rootCount;
            graftedFruitCount += graft.fruitCount;

            UpdateStats();
            plantManager.graftBuffer.Clear();

            graftingCooldown = data.graftCooldownDuration;
        }
    }
}
