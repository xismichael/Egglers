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
        private PlantBitManager plantManager;
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
        
        // public float resourcePerTick;

        public PlantBit(Vector2Int posInit, PlantBitData newData, PlantBitManager manager, bool isHeartInit = false, int startLeaf = 0, int startRoot = 0, int startFruit = 0, int startMaxComponent = 0)
        {
            position = posInit;
            data = newData;
            plantManager = manager;
            isHeart = isHeartInit;
            phase = PlantBitPhase.Bud;
            
            growthProgress = 0;

            leafCount = startLeaf;
            rootCount = startRoot;
            fruitCount = startFruit;

            graftedLeafCount = 0;
            graftedRootCount = 0;
            graftedFruitCount = 0;

            // Setup Heirarchy
            parent = null;

            maxComponentCount = maxComponentCount <= 0 ? startMaxComponent : CalculateMaxComponentCount();
            sproutCost = CalculateSproutCost();

            attackDamage = CalculateStat(leafCount, graftedLeafCount, data.leafMultiplier);
            extractionRate = CalculateStat(rootCount, graftedRootCount, data.rootMultiplier);
            energyStorage = CalculateStat(fruitCount, graftedFruitCount, data.fruitMultiplier);

            // IM LEAVING THIS LINE OUT ASSUMING YOU WILL MAKE A GROWTH LIMIT LOWER IN THE PLANT BIT DATA FOR THE HEART SO THE SPROUT TRIGGERS AUTOMATICALLY FIRST TICK UPDATE
            // if (isHeartInit) growthProgress = data;
        }

        public PlantBit(Vector2Int posInit, PlantBitData newData, PlantBit parentBit)
        {
            position = posInit;
            data = newData;
            plantManager = parentBit.plantManager;
            isHeart = false;
            phase = PlantBitPhase.Bud;

            growthProgress = 0;

            // Inherit all components as natural
            leafCount = parentBit.leafCount + parentBit.graftedLeafCount;
            rootCount = parentBit.rootCount + parentBit.graftedRootCount;
            fruitCount = parentBit.fruitCount + parentBit.graftedFruitCount;

            graftedLeafCount = 0;
            graftedRootCount = 0;
            graftedFruitCount = 0;

            // Setup Heirarchy
            parent = parentBit;
            parentBit.children.Add(this);

            maxComponentCount = CalculateMaxComponentCount();
            sproutCost = CalculateSproutCost();

            attackDamage = CalculateStat(leafCount, graftedLeafCount, data.leafMultiplier);
            extractionRate = CalculateStat(rootCount, graftedRootCount, data.rootMultiplier);
            energyStorage = CalculateStat(fruitCount, graftedFruitCount, data.fruitMultiplier);
        }

        private int CalculateMaxComponentCount()
        {
            int bonus = Mathf.Max(data.maxComponentIncrease, Mathf.CeilToInt(parent.TotalComponents * data.maxComponentBonus));
            return parent.TotalComponents + bonus;
        }

        private float CalculateSproutCost()
        {
            // float naturalScalar = 1 + TotalNaturalComponents * data.naturalSproutCostScaling;
            // float graftedScalar = 1 + TotalGraftedComponents * data.graftedSproutCostScaling;
            // float cost = data.baseSproutCost * naturalScalar * graftedScalar;

            float naturalTax = TotalNaturalComponents * data.naturalSproutCostScaling;
            float graftedTax = TotalGraftedComponents * data.graftedSproutCostScaling;
            float cost = data.baseSproutCost + naturalTax + graftedTax;

            return cost;
        }

        private float CalculateStat(int natural, int grafted, float baseMultiplier)
        {
            // Exponential for natural components (power of 1.3)
            float naturalPower = Mathf.Pow(natural, data.naturalComponentPower) * baseMultiplier;

            // Linear for grafted components
            float graftedPower = grafted * 1.0f; // Why don't we multiply this by the base too?

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

            Debug.Log($"Sprout Grown | added storage: {energyStorage}");

            AttemptAutoSprout();
        }

        private void AttemptAutoSprout()
        {
            void sprout(GameTile tile)
            {
                if (tile.GetPlantBit() == null && plantManager.RemoveEnergy(sproutCost))
                {
                    Debug.Log($"Making Sprout | cost: {sproutCost}");
                    plantManager.CreateSprout(this, tile.position);
                }
            }

            plantManager.gameGrid.ForAllNeighbors(sprout, position);
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

                // Update cooldowns
                if (graftingCooldown > 0)
                {
                    graftingCooldown --;
                    if (graftingCooldown < 0) graftingCooldown = 0;
                }
            }
        }

        public void ExtractEnergy()
        {
            float pollution = plantManager.GetPollutionAtTile(position);

            if (pollution >= 0 && attackDamage > pollution)
                {
                    float energyGain = (extractionRate > pollution) ? pollution : extractionRate;

                    plantManager.RemovePollutionAtTile(position, extractionRate);

                    Debug.Log($"Extracting Energy | gain: {energyGain}");
                    plantManager.AddEnergy(energyGain);
                }
        }

        public void Kill()
        {
            // Update resource cap
            plantManager.RemoveMaxEnergy(energyStorage);

            // Remove from parent's children list
            if (parent != null)
            {
                parent.children.Remove(this);
            }

            // Cascade to all children (no orphans)
            foreach (PlantBit child in children)
            {
                plantManager.KillPlantBit(child);
            }
        }

        public void RemoveGraft(int leaf, int root, int fruit)
        {
            if (graftingCooldown > 0)
            {
                Debug.LogWarning("Plant is on grafting cooldown");
                return;
            }

            // Check if plant has enough grafts to remove
            if (leaf > graftedLeafCount || root > graftedRootCount || fruit > graftedFruitCount)
            {
                Debug.LogWarning("Trying to remove more grafts than available");
                return;
            }

            // Cost to remove
            int totalRemoved = leaf + root + fruit;
            float removalCost = totalRemoved * data.removalCostPerComponent;
            if (!plantManager.RemoveEnergy(removalCost))
            {
                Debug.LogWarning("Not enough resources to remove grafts");
                return;
            }

            plantManager.UpdateGraftBuffer(leaf, root, fruit);

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
            if (graftingCooldown > 0)
            {
                Debug.LogWarning("Plant is on grafting cooldown");
                return;
            }

            // Check capacity
            int newTotal = TotalComponents + graft.TotalComponents;
            if (newTotal > maxComponentCount)
            {
                Debug.LogWarning("Would exceed plant's max component capacity");
                return;
            }

            // Calculate cost
            float cost = data.baseGraftCost * (1 + TotalComponents * data.graftCostScaling);
            if (!plantManager.RemoveEnergy(cost))
            {
                Debug.LogWarning("Not enough resources to apply grafts");
                return;
            }

            plantManager.ClearGraftBuffer();

            // Apply to plant
            graftedLeafCount += graft.leafCount;
            graftedRootCount += graft.rootCount;
            graftedFruitCount += graft.fruitCount;
            UpdateStats();

            // Start cooldown
            graftingCooldown = data.graftCooldownDuration;
        }

        // public bool IsValidSproutTarget(Vector2Int target)
        // {
        //     // TileState state = gridSystem.GetTileState(target);

        //     // // Empty tiles always valid
        //     // if (state == TileState.Empty)
        //     // {
        //     //     return true;
        //     // }

        //     // Polluted tiles: need ATD > pollution attackDamage
        //     // if (state == TileState.Pollution)
        //     // {
        //     //     PollutionTile tile = gridSystem.GetEntity<PollutionTile>(target);
        //     //     if (tile != null)
        //     //     {
        //     //         return attackDamage > tile.attackDamage;
        //     //     }
        //     // }

        //     return false;
        // }
    }
}

