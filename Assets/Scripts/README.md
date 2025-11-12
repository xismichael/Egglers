# Plant vs Pollution Game - Implementation

## Overview
This is a complete implementation of a 2.5D grid-based game where plants battle spreading pollution through strategic growth, grafting, and resource management.

## Setup Instructions

### 1. Create the Game Scene

1. **Create a new scene** in Unity
2. **Add an empty GameObject** and name it "GameManager"
3. **Attach the following components** to it:
   - `GameManager` (Managers/GameManager.cs)
   - `PlantManager` (Managers/PlantManager.cs)
   - `PollutionManager` (Managers/PollutionManager.cs)

4. **Wire up references** in the Inspector:
   - GameManager → PlantManager: Drag PlantManager component
   - GameManager → PollutionManager: Drag PollutionManager component
   - PlantManager ↔ PollutionManager: Cross-reference each other

### 2. Configure Game Settings

#### Create Configs (Right-click in Project → Create → Plant Pollution Game)

**GameConfig:**
- Grid dimensions (e.g., 20x20)
- Heart starting components (3 leaf, 3 root, 3 fruit)
- Pollution tick rate (7 seconds recommended)

**PlantConfig:**
- Component multipliers (leaf/root/fruit power)
- Cost scaling factors
- Grafting costs and cooldowns
- Plant tick rate (0.5 seconds recommended)

**GridConfig:**
- Use context menu "Setup Example - Medium" for a balanced starting configuration
- Or manually configure pollution sources:
  - Weak sources: HP 50, emission 5 (start immediately)
  - Medium sources: HP 150, emission 15 (awaken at 2 min)
  - Strong sources: HP 300, emission 30 (awaken at 5 min)

**PollutionTypeConfig:**
- Configure multipliers for Toxic/Acidic/Sludge pollution types
- Default values are balanced

### 3. Set Up GameManager in Inspector

In the GameManager component:
- Set grid width and height (e.g., 20x20)
- Set heart starting components (3, 3, 3 recommended)
- Set plant tick rate (0.5s)
- Set pollution tick rate (7s)
- Add pollution sources to the list (or load from GridConfig)

### 4. Testing the Core Systems

To test without UI:
1. Run the scene
2. Use the console or create a simple UI to call:
   ```csharp
   gameManager.OnPlayerPlacesHeart(new Vector2Int(10, 10));
   ```
3. Watch pollution spread and plants auto-sprout in the console logs

## Game Mechanics Summary

### Plant System
- **Natural components** (leaf/root/fruit): Exponential scaling (power 1.3)
- **Grafted components**: Linear scaling
- **Strategy**: Concentrate grafts on parent → sprout child → grafts become natural (exponential)

### Growth & Sprouting
- **Bud Phase**: Consumes resources per tick until grown
- **Grown Phase**: One-time auto-sprout to all valid adjacent tiles
- **Manual Sprouting**: Costs more resources, player-controlled

### Resource System
- **Centralized pool**: All plants share resources
- **Max storage**: Sum of all GROWN plants' resourceStorage
- **Extraction**: Adjacent to pollution with ATD > pollution.attackDamage
- **Cleansing**: Reduces pollution while extracting

### Grafting System
- **Remove Grafts**: Costs resources, stores in global buffer, starts cooldown
- **Apply Grafts**: Costs resources (scales with components), clears buffer
- **Buffer**: Persistent until applied, warns if overwritten
- **Cooldown**: 5 seconds per plant, shared for remove/apply

### Pollution System
- **Sources emit** to immediately adjacent tiles only (4-directional)
- **Tiles spread** to neighbors with distance-based decay
- **Fountain rule**: Only spreads to tiles with less pollution
- **Types**:
  - Toxic: Balanced (1.0 spread, 1.0 attack)
  - Acidic: Aggressive (0.7 spread, 1.5 attack, requires 1.5x leaves)
  - Sludge: Persistent (0.4 spread, 0.8 attack, 0.6x extraction)

### Progression System (Source Tiers + Awakening)
- **Weak sources**: Active immediately (HP 50, emission 5)
- **Medium sources**: Awaken at 2 minutes → emission ×2, HP ×1.5
- **Strong sources**: Awaken at 5 minutes → emission ×2, HP ×1.5
- Creates clear early/mid/late game phases

### Win/Loss Conditions
- **Win**: All pollution sources destroyed (HP = 0)
- **Loss**: Heart overwhelmed (adjacent pollution attackDamage > Heart ATD)

## Player Actions API

```csharp
// Place Heart to start game
gameManager.OnPlayerPlacesHeart(Vector2Int position);

// Manual sprouting
gameManager.PlayerManualSprout(Vector2Int plantPos, Vector2Int targetPos);

// Pruning
gameManager.PlayerPrune(Vector2Int plantPos);

// Grafting
gameManager.PlayerRemoveGrafts(Vector2Int plantPos, int leaf, int root, int fruit);
gameManager.PlayerApplyGrafts(Vector2Int plantPos);

// Pause/Resume
gameManager.PauseGame();
gameManager.ResumeGame();

// Restart
gameManager.RestartGame();
```

## Accessing Game State

```csharp
// Current resources
float resources = plantManager.currentResources;
float maxResources = plantManager.maxResourceStorage;

// Get plant at position
Plant plant = gridSystem.GetEntity<Plant>(position);

// Get pollution at position
PollutionTile pollution = gridSystem.GetEntity<PollutionTile>(position);

// Get tile state
TileState state = gridSystem.GetTileState(position);

// Check game state
GameState state = gameManager.gameState;

// Active pollution sources
int sourceCount = pollutionManager.activeSources.Count;
```

## Events for UI Binding

Wire these up in the Inspector or code:

```csharp
// GameManager
gameManager.OnGameWon += HandleGameWon;
gameManager.OnGameLost += HandleGameLost;
gameManager.OnErrorMessage += HandleError;

// PlantManager
plantManager.OnWarningMessage += HandleWarning;
```

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
│   ├── Enums.cs
│   ├── PlantConfig.cs
│   ├── GameConfig.cs
│   ├── GridConfig.cs
│   └── PollutionTypeConfig.cs
├── IMPLEMENTATION_PLAN.md
└── README.md (this file)
```

## Balancing Tips

### Making the Game Easier:
- Increase heart starting components
- Decrease pollution emission rates
- Increase plant tick rate (faster resource generation)
- Decrease pollution spread rate
- Lower grafting costs

### Making the Game Harder:
- Add more pollution sources
- Decrease delay before medium/strong sources awaken
- Increase pollution emission rates
- Decrease base resource extraction rate
- Increase sprouting costs

### Encouraging Specialization:
- Increase natural component power (currently 1.3 exponent)
- Increase grafting costs
- Add specific pollution types that require specialized plants

## Next Steps for Full Game

1. **Visualization**:
   - Create visual representations for grid tiles
   - Animate pollution spreading
   - Show plant components visually
   - Display resource UI

2. **Player Input**:
   - Click to place Heart
   - Click plants to select
   - Click adjacent tiles to sprout
   - UI for grafting operations

3. **Audio & Feedback**:
   - Sound effects for plant growth, pollution spread
   - Visual feedback for resource extraction
   - Particle effects for pollution cleansing

4. **Polish**:
   - Tutorial system
   - Save/Load game state
   - Multiple difficulty levels
   - Achievements/statistics

## Debug & Testing

Enable debug logs to see:
- Pollution source activation/awakening
- Plant growth transitions
- Source destruction
- Win/Loss triggers

Check console for warnings about:
- Invalid sprouting attempts
- Insufficient resources
- Grafting errors
- Out of bounds positions

## Performance Notes

- Grid operations are O(1) with dictionary lookups
- Pollution spreading is O(n×m) where n = polluted tiles, m = neighbors
- Plant updates are O(p) where p = number of plants
- Recommended max grid size: 50×50 for smooth performance
- Consider object pooling for large grids (100×100+)

## Credits

Implementation follows the complete design specification in `IMPLEMENTATION_PLAN.md`.
All systems are fully functional and ready for visual/UI integration.

