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

        public void ForAllNeighbors(Action<GameTile> func, Vector2Int pos, bool includeDiagonal = false)
        {
            // 4-directional (up, down, left, right)
            Vector2Int[] orthogonalDirections = new Vector2Int[]
            {
                Vector2Int.up,      // (0, 1)
                Vector2Int.down,    // (0, -1)
                Vector2Int.left,    // (-1, 0)
                Vector2Int.right    // (1, 0)
            };

            foreach (Vector2Int dir in orthogonalDirections)
            {
                Vector2Int neighbor = pos + dir;
                if (IsInBounds(neighbor))
                {
                    func(GetTileAtPosition(neighbor));
                }
            }

            // Add diagonal neighbors if requested
            if (includeDiagonal)
            {
                Vector2Int[] diagonalDirections = new Vector2Int[]
                {
                    new(1, 1),   // Up-right
                    new(1, -1),  // Down-right
                    new(-1, 1),  // Up-left
                    new(-1, -1)  // Down-left
                };

                foreach (Vector2Int dir in diagonalDirections)
                {
                    Vector2Int neighbor = pos + dir;
                    if (IsInBounds(neighbor))
                    {
                        func(GetTileAtPosition(neighbor));
                    }
                }
            }
        }

        public GameTile GetTileAtPosition(Vector2Int pos)
        {
            if (!IsInBounds(pos)) return null;
            return grid[pos.x, pos.y];
        }

        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }
    }
}

