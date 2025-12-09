using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Egglers
{
    public enum PlantBitPhase
    {
        Bud,            // Consuming resources to grow
        Grown,          // Fully functional, can sprout
        FullyInfected   // Virus has fully taken over
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

        public float maintenanceCost; // Fixed typo

        // Derived stats (calculated)
        public int TotalNaturalComponents => leafCount + rootCount + fruitCount;
        public int TotalGraftedComponents => graftedLeafCount + graftedRootCount + graftedFruitCount;
        public int TotalComponents => TotalNaturalComponents + TotalGraftedComponents;

        // Infection tracking
        public bool isInfected = false; // Virus is spreading through this plant (process)
        // Note: phase == Infected means virus has fully taken over (terminal state)
        public Coroutine infectionCoroutine; // Reference to active infection spread coroutine
        public float infectionSpreadStartTime; // When infection spread started
        public float infectionSpreadDuration; // How long until it spreads




        public PlantBit(Vector2Int pos, PlantBitData newData, PlantBitManager manager,
            bool heart = false, int startLeaf = 0, int startRoot = 0, int startFruit = 0)
        {
            //Debug.Log($"[PlantBit] Created heart={heart} at {pos}");

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

            phase = PlantBitPhase.Grown;
            growthProgress = 0;


            UpdateStats();

            // Register self in the grid
            plantManager.gameGrid.SetEntity(this);
            // plantManager.gameGrid.SetTileState(position, TileState.Plant);
            //Debug.Log($"[PlantBit] Registered to grid at {position}");
        }

        public PlantBit(Vector2Int pos, PlantBitData newData, PlantBit parentBit)
        {
            //Debug.Log($"[PlantBit] Created child at {pos} (parent: {parentBit.position})");

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

            UpdateStats();

            // Register self in the grid
            plantManager.gameGrid.SetEntity(this);
            //Debug.Log($"[PlantBit] Registered child to grid at {position}");
        }

        private int CalculateMaxComponentCount()
        {

            //going to be just total natural compoents + 2 for now
            return TotalNaturalComponents + 2;
            // int parentTotal = parent != null ? parent.TotalComponents : 0;
            // int bonus = Mathf.Max(data.maxComponentIncrease, Mathf.CeilToInt(parentTotal * data.maxComponentBonus));
            // return parentTotal + bonus;
        }

        private float CalculateSproutCost()
        {
            //for now just going to be the a value to the power of total components  plus a base value
            return Mathf.Pow(1.5f, TotalComponents) * 1.5f;
            
            // float naturalTax = TotalNaturalComponents * data.naturalSproutCostScaling;
            // float graftedTax = TotalGraftedComponents * data.graftedSproutCostScaling;
            // return data.baseSproutCost + naturalTax + graftedTax;
        }

        public float CalculateMaintenanceCost()
        {
            // Heart has no maintenance cost
            if (isHeart) return 0f;
            
            //for now just going to be the a value to the power of total components  plus a base value
            return Mathf.Pow(1.4f, TotalComponents)/2;
        }
        
        public float CalculateMaintenanceCostForComponents(int totalComponents)
        {
            // Heart has no maintenance cost
            if (isHeart) return 0f;
            
            //for now just going to be the a value to the power of total components  plus a base value
            return Mathf.Pow(1.4f, totalComponents)/2;
        }

        public float CalculateStat(int natural, int grafted, float baseMultiplier)
        {
            float naturalPower = Mathf.Pow(natural, 1.5f) * baseMultiplier;
            float graftedPower = grafted * baseMultiplier; // linear scaling
            return naturalPower + graftedPower;
        }

        public void UpdateStats()
        {
            attackDamage = CalculateStat(rootCount, graftedRootCount, data.rootMultiplier);
            extractionRate = CalculateStat(leafCount, graftedLeafCount, data.leafMultiplier);
            energyStorage = CalculateStat(fruitCount, graftedFruitCount, data.fruitMultiplier);
            sproutCost = CalculateSproutCost();
            maxComponentCount = CalculateMaxComponentCount();
            maintenanceCost = CalculateMaintenanceCost(); 
        }

        public void TransitionToGrownPhase()
        {
            //Debug.Log($"[PlantBit] TransitionToGrownPhase at {position}");

            phase = PlantBitPhase.Grown;
            UpdateStats();
            plantManager.AddMaxEnergy(energyStorage);

            AttemptSprout(false);

            GridEvents.PlantUpdated(position);
        }

        public bool AttemptSprout(bool haveCost = true)
        {
            if (phase != PlantBitPhase.Grown || isInfected) {
                Debug.LogWarning($"[PlantBit] AttemptSprout failed at {position}, plant is not grown or healthy");
                SoundManager.Instance.PlayError();
                return false;
            }
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
                return false;
            }

            // Check if we can afford sprouting
            if (haveCost && !plantManager.RemoveEnergy(sproutCost))
            {
                Debug.Log($"[PlantBit] Not enough energy to sprout (cost: {sproutCost})");
                return false;
            }

            // Only attempt sprout if tile is clean
            List<Vector2Int> neighbors = grid.GetNeighbors(position);
            bool sproutedAny = false;
            
            foreach (Vector2Int neighborPos in neighbors)
            {
                // Allow sprouting as long as no PlantBit is already present
                bool canSprout = (grid.GetEntity<PlantBit>(neighborPos) == null) && (grid.GetEntity<PollutionTile>(neighborPos) == null) && (grid.GetEntity<PollutionSource>(neighborPos) == null);
                //check

                Debug.Log($"[PlantBit] Checking sprout spot {neighborPos} | canSprout={canSprout}");

                if (canSprout)
                {
                    Debug.Log($"[PlantBit] Sprouting at {neighborPos} (cost: {sproutCost})");
                    plantManager.CreateSprout(this, neighborPos);
                    Debug.Log($"[PlantBit] Sprouted at {neighborPos}");
                    sproutedAny = true;
                }
            }

            
            Debug.Log($"[PlantBit] Sprouting completed, sprouted any: {sproutedAny}");
            return sproutedAny;
        }

        //all the update handled in plantManager for now

        
        // public void TickUpdate()
        // {
        //     if (phase == PlantBitPhase.FullyInfected)
        //     {
        //         //passive infection spread (terminal state - fully infected)
        //         //InfectionSpread();
        //         return;
        //     } 

        //     if (phase == PlantBitPhase.Bud)
        //     {
        //         // Debug.Log($"[PlantBit] TickUpdate BUD at {position} | progress: {growthProgress}/{data.fullGrowthTicks}");

        //         if (!isInfected && plantManager.RemoveEnergy(maintenanceCost * 2f))
        //         {
        //             growthProgress++;
        //             if (growthProgress >= data.fullGrowthTicks)
        //             {
        //                 //Debug.Log($"[PlantBit] Bud fully grown! Transitioning at {position}");
        //                 TransitionToGrownPhase();
        //             }
        //         }
        //     }

        //     if (phase == PlantBitPhase.Grown)
        //     {
        //         // Passive energy from leaves
        //         PassiveEnergyGain();
        //         if (!isInfected && plantManager.RemoveEnergy(maintenanceCost))
        //         {
        //             ExtractEnergy();
        //         }
        //     }
        // }

        public void InfectionSpread(float spreadTimer)
        {
            // Only start if not already spreading
            if (infectionCoroutine != null) return;

            // Track timing for UI display
            infectionSpreadStartTime = Time.time;
            infectionSpreadDuration = spreadTimer;

            // Start the infection spread coroutine in the manager
            // Timer determines how long until virus spreads to adjacent plants
            infectionCoroutine = plantManager.StartInfectionSpread(this, spreadTimer);
        }
        public void PassiveEnergyGain()
        {
            float multiplier = 0.3f / (isInfected ? 2f : 1f);
            
            // Heart has doubled extraction rate
            if (isHeart) multiplier *= 2f;
            
            // Passive energy from leaves
            float energyGain = extractionRate * multiplier;
            plantManager.AddEnergy(energyGain);
        }

        public void ExtractEnergy()
        {
            GridSystem grid = plantManager.gameGrid;

            // Scan current tile + neighbors
            List<Vector2Int> tilesToScan = grid.GetNeighbors(position);
            tilesToScan.Add(position);

            foreach (Vector2Int pos in tilesToScan)
            {
                PollutionTile tile = grid.GetEntity<PollutionTile>(pos);
                PollutionSource source = grid.GetEntity<PollutionSource>(pos);
                float realDamage = 0;
                if (tile == null && source == null) continue;
                if (tile != null) realDamage = tile.TakeDamage(attackDamage);
                if (source != null) realDamage = source.TakeDamage(attackDamage);
                if (realDamage > 0f) plantManager.AddEnergy(realDamage / 2);
            }
        }

        public void Kill()
        {
            //Debug.Log($"[PlantBit] KILL at {position}");

            // Stop infection coroutine if running
            if (infectionCoroutine != null)
            {
                plantManager.StopCoroutine(infectionCoroutine);
                infectionCoroutine = null;
                //Debug.Log($"[PlantBit] Stopped infection coroutine for {position}");
            }

            // Unfreeze pollution at this position if it exists
            PollutionTile tile = plantManager.gameGrid.GetEntity<PollutionTile>(position);
            if (tile != null)
            {
                tile.isFrozen = false;
                GridEvents.PollutionUpdated(position);
            }

            // Remove energy contribution (only if plant was grown and contributed)
            if (phase == PlantBitPhase.Grown)
            {
                plantManager.RemoveMaxEnergy(energyStorage);
            }

            // Detach from parent
            parent?.children.Remove(this);

            // Recursively kill children
            foreach (PlantBit child in new List<PlantBit>(children))
            {
                //Debug.Log($"[PlantBit] Killing child {child.position}");
                plantManager.KillPlantBit(child);
            }

            children.Clear();

            // Remove self from grid using type-safe removal
            plantManager.gameGrid.RemoveEntity<PlantBit>(this);

            //Debug.Log($"[PlantBit] Removed from grid at {position}");
        }

        public bool RemoveGraft(int leaf, int root, int fruit)
        {
            Debug.Log($"[PlantBit] RemoveGraft BEFORE at {position} | Requested: L{leaf} R{root} F{fruit} | Current: L{leafCount} R{rootCount} F{fruitCount}");

            if (phase == PlantBitPhase.Bud)
            {
                Debug.LogWarning("Cannot remove graft, plant is still a bud");
                SoundManager.Instance.PlayError();
                return false;
            }

            //cannot graft grafted components
            int realLeafAmount = Mathf.Min(leaf, leafCount);
            int realRootAmount = Mathf.Min(root, rootCount);
            int realFruitAmount = Mathf.Min(fruit, fruitCount);
            
            Debug.Log($"[PlantBit] Clamped amounts: L{realLeafAmount} R{realRootAmount} F{realFruitAmount}");

            // Cost to remove
            int totalRemoved = realLeafAmount + realRootAmount + realFruitAmount;
            float removalCost = totalRemoved * data.removalCostPerComponent;
            if (!plantManager.RemoveEnergy(removalCost))
            {
                Debug.LogWarning($"Cannot remove graft, not enough energy! Cost: {removalCost:F1}, Current: {plantManager.currentEnergy:F1}");
                SoundManager.Instance.PlayError();
                return false;
            }


            plantManager.graftBuffer.Update(realLeafAmount, realRootAmount, realFruitAmount);

            // Remove from plant
            leafCount -= realLeafAmount;
            rootCount -= realRootAmount;
            fruitCount -= realFruitAmount;
            
            Debug.Log($"[PlantBit] RemoveGraft AFTER at {position} | Removed: L{realLeafAmount} R{realRootAmount} F{realFruitAmount} | New counts: L{leafCount} R{rootCount} F{fruitCount}");
            
            UpdateStats();
            return true;
        }

        //assuming applyGraft always gives you the right amount of components
        public bool ApplyGraft(int leaf, int root, int fruit)
        {
            Debug.Log($"[PlantBit] Attempting to ApplyGraft at {position} | Requested: L{leaf} R{root} F{fruit}");

            if (leaf + root + fruit + TotalComponents > maxComponentCount)
            {
                Debug.LogWarning($"Cannot apply graft, would exceed max components ({maxComponentCount})");
                SoundManager.Instance.PlayError();
                return false;
            }

            // Calculate energy cost
            float cost = (leaf + root + fruit) * data.removalCostPerComponent;
            Debug.Log($"[PlantBit] Calculated graft cost: {cost} energy, Current: {plantManager.currentEnergy}");

            // Attempt to pay energy
            if (!plantManager.RemoveEnergy(cost))
            {
                Debug.Log($"[PlantBit] Cannot apply graft: Not enough energy (Current: {plantManager.currentEnergy}, Needed: {cost})");
                SoundManager.Instance.PlayError();
                return false;
            }

            // Apply graft
            graftedLeafCount += leaf;
            graftedRootCount += root;
            graftedFruitCount += fruit;

            // Play grafting sound on success
            SoundManager.Instance.PlayGrafting();

            Debug.Log($"[PlantBit] Graft applied successfully | New grafted counts -> L:{graftedLeafCount} R:{graftedRootCount} F:{graftedFruitCount}");

            // Update derived stats
            UpdateStats();

            // Remove grafted components from the buffer
            plantManager.graftBuffer.Update(-leaf, -root, -fruit);
            Debug.Log("[PlantBit] Graft buffer updated after applying graft");
            
            return true;
        }

        public bool Nip()
        {
            if (isHeart)
            {
                Debug.LogWarning("[PlantBit] Cannot nip the heart!");
                SoundManager.Instance.PlayError();
                return false;
            }

            float refund = sproutCost * 0.25f;

            Debug.Log($"[PlantBit] Killing plant at {position}, refunding {refund} energy");

            if (!isInfected) plantManager.AddEnergy(refund);
            plantManager.KillPlantBit(this);
            return true;
        }
    }
}
