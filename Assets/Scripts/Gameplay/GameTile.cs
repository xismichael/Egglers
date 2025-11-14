using UnityEngine;

namespace Egglers
{
    public class GameTile
    {
        private PlantBit plantBit;
        public Vector2Int position;
        public int pollution;

        public PlantBit GetPlantBit() { return plantBit; }
        public void SetPlantBit(PlantBit newBit) { plantBit = newBit; }
    }
}
