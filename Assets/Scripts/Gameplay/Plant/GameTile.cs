using UnityEngine;

namespace Egglers
{
    public class GameTile
    {
        private PlantBit plantBit;
        public Vector2Int position;
        public int pollution;
        public PollutionTile pollutionTile;

        public GameTile(Vector2Int initPos)
        {
            plantBit = null;
            position = initPos;
            pollution = 25;
            pollutionTile = null;
        }

        public PlantBit GetPlantBit() { return plantBit; }
        public void SetPlantBit(PlantBit newBit) { plantBit = newBit; }
    }
}
