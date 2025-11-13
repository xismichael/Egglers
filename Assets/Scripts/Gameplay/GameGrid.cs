using System;
using UnityEngine;

namespace Egglers
{
    public class GameGrid
    {
        public GameTile[,] grid;
        private int width;
        private int height;

        public GameGrid(int xSize, int ySize)
        {
            width = xSize;
            height = ySize;
            grid = new GameTile[xSize, ySize];
        }

        public void ForAllNeighbors(Action<GameTile> func, Vector2Int pos)
        {
            if ()
        }
    }
}

