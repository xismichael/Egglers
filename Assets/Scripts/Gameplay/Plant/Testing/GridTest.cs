using Egglers;
using UnityEngine;

namespace Egglers
{
    public class GridTest : MonoBehaviour
    {
        private GameGrid grid;
        
        void Awake()
        {
            grid = new GameGrid(10, 10);
        }
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("PRINTING OUT ALL TILES:");
            
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Debug.Log($"Looking for tile at col {i} and row {j}");
                    GameTile tile = grid.GetTileAtPosition(new Vector2Int(i, j));
                    Debug.Log(tile.position);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}

