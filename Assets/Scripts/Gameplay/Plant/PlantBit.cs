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
            plantManager.gameGrid.SetEntity(position, this);
            plantManager.gameGrid.SetTileState(position, TileState.Plant);
        }

        public PlantBit(Vector2Int pos, PlantBitData newData, PlantBit parentBit)
        {
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
            plantManager.gameGrid.SetEntity(position, this);
            plantManager.gameGrid.SetTileState(position, TileState.Plant);
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
            phase = PlantBitPhase.Grown;
            UpdateStats();
            plantManager.AddMaxEnergy(energyStorage);

            AttemptAutoSprout();
        }

        private void AttemptAutoSprout()
        {
            // Use the new GridSystem's neighbors
            List<Vector2Int> neighbors = plantManager.gameGrid.GetNeighbors(position);
            foreach (Vector2Int neighborPos in neighbors)
            {
                if (plantManager.gameGrid.GetEntity<PlantBit>(neighborPos) == null &&
                    plantManager.gameGrid.GetTileState(neighborPos) == TileState.Empty &&
                    plantManager.RemoveEnergy(sproutCost))
                {
                    plantManager.CreateSprout(this, neighborPos);
                }
            }
        }

        public void TickUpdate()
        {
            if (phase == PlantBitPhase.Bud)
            {
                if (plantManager.RemoveEnergy(data.tickGrowthCost))
                {
                    growthProgress++;
                    if (growthProgress >= data.fullGrowthTicks)
                    {
                        TransitionToGrownPhase();
                    }
                }
            }
            else if (phase == PlantBitPhase.Grown)
            {
                ExtractEnergy();

                if (graftingCooldown > 0)
                {
                    graftingCooldown--;
                    if (graftingCooldown < 0) graftingCooldown = 0;
                }
            }
        }

        public void ExtractEnergy()
        {
            PollutionTile pollutionTile = plantManager.gameGrid.GetEntity<PollutionTile>(position);
            if (pollutionTile == null) return;

            float totalPollution = pollutionTile.GetTotalPollution();
            if (totalPollution <= 0) return;

            float energyGain = Mathf.Min(extractionRate, totalPollution);
            pollutionTile.TakeDamage(energyGain);

            plantManager.AddEnergy(energyGain);
        }


        public void Kill()
        {
            plantManager.RemoveMaxEnergy(energyStorage);
            parent?.children.Remove(this);

            foreach (PlantBit child in new List<PlantBit>(children))
            {
                plantManager.KillPlantBit(child);
            }

            // Remove self from grid
            plantManager.gameGrid.RemoveEntity(position);
            plantManager.gameGrid.SetTileState(position, TileState.Empty);
        }

        public void RemoveGraft(int leaf, int root, int fruit)
        {
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
