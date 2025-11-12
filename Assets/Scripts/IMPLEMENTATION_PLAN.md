# Plant vs Pollution Game - Complete Implementation Plan

## Overview
Real-time grid game where plants combat spreading pollution. Pollution sources emit to adjacent tiles only, creating gradual spread patterns. Players concentrate components via grafting, then sprout children inheriting grafted stats as exponentially-scaled natural components.

---

## Critical Implementation Rules

### Pollution Spreading Behavior
- Sources ONLY increase pollution in immediately adjacent (4-directional) tiles
- Those tiles spread to their neighbors with reduced amount
- **Rule:** Tiles ALWAYS have pollution < tiles they spread from
- Distance-based decay: `1.0 / (1 + hops * 0.2f)`
- Sources never directly spread to distant tiles

### Parent-Bud Dynamics
- Buds use **DYNAMIC** `parent.attackDamage` for overwhelm checks
- If parent's ATD changes (grafts), bud's resistance changes immediately
- Buds tracked in `parent.children` list for cascade deletion
- Parent deleted → buds cascade-delete (no orphans)

### Source Damage (Batching)
```csharp
// Collect all damage first
float totalDamage = 0;
foreach (plant adjacent to source) {
    if (plant.ATD > pollution) {
        totalDamage += (plant.ATD - pollution.ATD) * 0.1f;
    }
}
// Apply once (prevents null reference if source dies mid-loop)
source.TakeDamage(totalDamage);
```

### Resource Storage Rules
- `maxResourceStorage` = sum of **GROWN** plants' `resourceStorage` only
- Buds do NOT contribute until fully grown
- Heart's `resourceStorage` counts from game start
- When plants die: `currentResources = min(currentResources, newMaxStorage)`

### Grafting Costs
- **RemoveGrafts:** Costs resources (formula: `removalCost = (removed components) * removalCostPerComponent`)
- **ApplyGrafts:** Costs resources (formula: `graftCost = baseGraftCost * (1 + currentTotalComponents * 0.25f)`)
- Both share same per-plant cooldown (5s)
- Buffer overwrites warn player (UI message: "Previous grafts lost!")

---

## Progression System: Source Tiers + Awakening

**Source Tiers:**
- **Weak Sources**: HP 50, emission 5, attackDamage ~10-20
- **Medium Sources**: HP 150, emission 15, attackDamage ~40-60
- **Strong Sources**: HP 300, emission 30, attackDamage ~100+

**Timeline:**
- **0:00-2:00 (Early)**: Only Weak sources active, learn mechanics
- **2:00 (Mid)**: Medium sources awaken, emission doubles
- **5:00 (Late)**: Strong sources awaken, maximum pressure

**Gameplay Arc:**
- Early: Build basic network, learn grafting, defeat 1-2 weak sources
- Mid: Medium sources activate, need specialized plants, expand territory
- Late: Strong sources activate, require deep-tree exponential plants, final push

---

## Core Mechanics Summary

### Exponential Specialization Strategy
- Natural components: `Mathf.Pow(natural, 1.3f)` scaling
- Grafted components: Linear 1:1 scaling
- Strategy: Graft components onto parent → sprout child → child inherits as natural (exponential)

### One-Time Auto-Sprout
- Plant reaches GrownPhase → immediately sprouts to ALL valid adjacent tiles
- Never auto-sprouts again
- Creates organic spreading waves without infinite explosion

### Grafting Workflow
1. Remove grafts from Plant A → components go to global buffer (costs resources)
2. Apply grafts to Plant B → buffer clears, B gains components (costs resources)
3. Both remove and apply share same per-plant cooldown (5s)
4. Concentrate components on "parent" plants before sprouting children

### Pollution Tile Intelligence
- Each tile calculates own spreadSpeed and attackDamage
- Spreads based on composition (Toxic=fast, Acidic=aggressive, Sludge=slow)
- Distance-based decay: `1.0 / (1 + hops * 0.2)`
- Multiple sources blend naturally

### Plant-Pollution Timing
- **Pollution tick (5-10s)**: Sources emit, tiles spread
- **Plant tick (0.5s)**: Plants extract, check overwhelm, grow buds
- Plants die immediately if overwhelmed (checked every 0.5s)
- Pollution spreads in larger pulses (5-10s)

### Heart Mechanics
- Functions like normal plant (can graft, extract, sprout)
- Immune to pollution consumption
- Loss condition: ANY adjacent pollution with attackDamage > Heart.ATD

---

## File Structure
```
Assets/Scripts/
├── Gameplay/
│   ├── Plant.cs
│   ├── PollutionTile.cs
│   └── PollutionSource.cs
├── Managers/
│   ├── GameManager.cs
│   ├── PlantManager.cs
│   └── PollutionManager.cs
├── Utilities/
│   ├── GridSystem.cs
│   └── Enums.cs
└── IMPLEMENTATION_PLAN.md (this file)

Assets/Resources/Configs/
├── PlantConfig.asset
├── GameConfig.asset
├── GridConfig.asset
└── PollutionTypeConfig.asset
```

---

## Testing Checklist
- [ ] Single source gradient (should decay with distance)
- [ ] Multi-source blending (types mix at boundaries)
- [ ] Bud overwhelm when parent's ATD drops (remove grafts during growth)
- [ ] Source damage batching (multiple plants, source dies correctly)
- [ ] Resource cap clamping (kill plants, resources clamp down)
- [ ] Grafting buffer overwrite warning
- [ ] Manual sprout validation (all edge cases)
- [ ] Heart placement validation
- [ ] One-time auto-sprout dead-end scenario
- [ ] Starting resources with Heart storage
- [ ] Source awakening at correct times
- [ ] Exponential scaling vs pollution progression balance

