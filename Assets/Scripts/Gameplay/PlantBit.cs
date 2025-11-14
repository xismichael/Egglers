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

        // public bool hasAutoSprouted;

        // Cooldowns
        // public float graftingCooldown;
        
        // public int growthDuration;
        // public float resourcePerTick;

        public PlantBit(Vector2Int posInit, PlantBitData newData, PlantBitManager manager, bool isHeartInit = false, int startLeaf = 0, int startRoot = 0, int startFruit = 0)
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

            maxComponentCount = CalculateMaxComponentCount();
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
            float naturalScalar = 1 + TotalNaturalComponents * data.naturalSproutCostScaling;
            float graftedScalar = 1 + TotalGraftedComponents * data.graftedSproutCostScaling;
            float cost = data.baseSproutCost * naturalScalar * graftedScalar;
            return cost;
        }

        private float CalculateStat(int natural, int grafted, float baseMultiplier)
        {
            // Exponential for natural components (power of 1.3)
            float naturalPower = Mathf.Pow(natural, 1.3f) * baseMultiplier;

            // Linear for grafted components
            float graftedPower = grafted * 1.0f;

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

            // One-time auto-sprout
            // if (!hasAutoSprouted)
            // {
            //     hasAutoSprouted = true;
            //     AttemptAutoSprout();
            // }

            AttemptAutoSprout();
        }

        private void AttemptAutoSprout()
        {
            // List<Vector2Int> neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);

            // foreach (Vector2Int neighborPos in neighbors)
            // {
            //     if (IsValidSproutTarget(neighborPos))
            //     {
            //         plantManager.CreateSprout(this, neighborPos);
            //     }
            // }

            void sprout(GameTile tile)
            {
                if (tile.GetPlantBit() != null && plantManager.RemoveEnergy(sproutCost))
                {
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
            else
            {
                ExtractEnergy();
            }

            // // Collect source damage (batched)
            // Dictionary<PollutionSource, float> sourceDamage = new Dictionary<PollutionSource, float>();

            // foreach (Plant plant in allPlants.Values)
            // {
            //     if (plant.phase == PlantPhase.Bud)
            //     {
            //         // Bud growth
            //         if (CanAfford(plant.resourcePerTick))
            //         {
            //             SpendResource(plant.resourcePerTick);
            //             plant.growthProgress++;

            //             if (plant.growthProgress >= plant.growthDuration)
            //             {
            //                 plant.TransitionToGrownPhase();
            //                 UpdateMaxStorage(); // Bud became grown, update cap
            //             }
            //         }
            //         // Else: growth pauses

            //         // Check overwhelm (uses parent's dynamic ATD)
            //         plant.CheckOverwhelm();
            //     }
            //     else if (plant.phase == PlantPhase.Grown)
            //     {
            //         // Extract resources and reduce pollution
            //         plant.ExtractFromAdjacentPollution();

            //         // Collect source damage
            //         List<Vector2Int> neighbors = gridSystem.GetNeighbors(plant.position, includeDiagonal: false);
            //         foreach (Vector2Int neighborPos in neighbors)
            //         {
            //             if (gridSystem.GetTileState(neighborPos) == TileState.PollutionSource)
            //             {
            //                 PollutionSource source = gridSystem.GetEntity<PollutionSource>(neighborPos);
            //                 if (source != null)
            //                 {
            //                     float pollutionAtSource = pollutionManager.GetPollutionLevelAt(source.position);
            //                     if (plant.attackDamage > pollutionAtSource)
            //                     {
            //                         float margin = plant.attackDamage - pollutionAtSource;
            //                         float damage = margin * 0.1f;

            //                         if (!sourceDamage.ContainsKey(source))
            //                         {
            //                             sourceDamage[source] = 0;
            //                         }
            //                         sourceDamage[source] += damage;
            //                     }
            //                 }
            //             }
            //         }

            //         // Check overwhelm (except Heart)
            //         if (!plant.isHeart)
            //         {
            //             plant.CheckOverwhelm();
            //         }
            //     }

            //     // Update cooldowns
            //     if (plant.graftingCooldown > 0)
            //     {
            //         plant.graftingCooldown -= plantTickRate;
            //         if (plant.graftingCooldown < 0) plant.graftingCooldown = 0;
            //     }
            // }

            // // Apply batched source damage
            // foreach (var kvp in sourceDamage)
            // {
            //     kvp.Key.TakeDamage(kvp.Value);
            // }
        }

        public void ExtractEnergy()
        {
            void extract(GameTile tile)
            {
                if (tile.pollution >= 0 && attackDamage > tile.pollution)
                {
                    float resourceGain = tile.pollution * 0.1f * extractionRate;

                    // Apply type modifier
                    // resourceGain *= tile.GetExtractionMultiplier();

                    plantManager.AddEnergy(resourceGain);

                    // Reduce pollution
                    // float pollutionReduction = (attackDamage - tile.attackDamage) * 0.05f;
                    // tile.TakeDamage(pollutionReduction);

                    // Check if tile should be removed
                    // if (tile.ShouldBeRemoved())
                    // {
                    //     plantManager.RemovePollutionTile(neighborPos);
                    // }
                }
            }

            plantManager.gameGrid.ForAllNeighbors(extract, position);
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

        // public bool IsValidManualSprout(Vector2Int target)
        // {
        //     // Must be adjacent
        //     if (!gridSystem.AreAdjacent(position, target))
        //     {
        //         return false;
        //     }

        //     TileState state = gridSystem.GetTileState(target);

        //     // Cannot have plant, source, or heart
        //     if (state == TileState.Plant || state == TileState.PollutionSource || state == TileState.Heart)
        //     {
        //         return false;
        //     }

        //     // Empty tiles always valid
        //     if (state == TileState.Empty)
        //     {
        //         return true;
        //     }

        //     // Polluted tiles: need ATD > pollution
        //     if (state == TileState.Pollution)
        //     {
        //         PollutionTile tile = gridSystem.GetEntity<PollutionTile>(target);
        //         if (tile != null)
        //         {
        //             return attackDamage > tile.attackDamage;
        //         }
        //     }

        //     return false;
        // }

        // public void CheckOverwhelm()
        // {
        //     List<Vector2Int> neighbors = gridSystem.GetNeighbors(position, includeDiagonal: false);

        //     foreach (Vector2Int neighborPos in neighbors)
        //     {
        //         if (gridSystem.GetTileState(neighborPos) == TileState.Pollution)
        //         {
        //             PollutionTile tile = gridSystem.GetEntity<PollutionTile>(neighborPos);
        //             if (tile != null)
        //             {
        //                 float effectiveATD;

        //                 if (phase == PlantPhase.Bud)
        //                 {
        //                     // Buds use parent's CURRENT ATD (dynamic)
        //                     if (parentPlant == null)
        //                     {
        //                         continue; // Safety check
        //                     }
        //                     effectiveATD = parentPlant.attackDamage;
        //                 }
        //                 else
        //                 {
        //                     // Grown plants use own ATD
        //                     effectiveATD = attackDamage;
        //                 }

        //                 // Acidic pollution requires more leaves to resist
        //                 if (tile.dominantType == PollutionType.Acidic)
        //                 {
        //                     effectiveATD *= 0.67f;
        //                 }

        //                 // Pollution overwhelms if ATD <= pollution attack
        //                 if (effectiveATD <= tile.attackDamage)
        //                 {
        //                     plantManager.DeletePlant(this, fromPollution: true);
        //                     return;
        //                 }
        //             }
        //         }
        //     }
        // }
    }
}

