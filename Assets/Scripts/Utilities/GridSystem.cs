using System.Collections.Generic;
using UnityEngine;

namespace PlantPollutionGame
{
    public class GridSystem
    {
        private int width;
        private int height;
        private TileState[,] grid;
        private Dictionary<Vector2Int, object> entities; // Stores Plant, PollutionTile, or PollutionSource

        public int Width => width;
        public int Height => height;

        public void Initialize(int gridWidth, int gridHeight)
        {
            width = gridWidth;
            height = gridHeight;
            grid = new TileState[width, height];
            entities = new Dictionary<Vector2Int, object>();

            // Initialize all tiles as Empty
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = TileState.Empty;
                }
            }
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public TileState GetTileState(Vector2Int pos)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Position {pos} is out of bounds!");
                return TileState.Empty;
            }
            return grid[pos.x, pos.y];
        }

        public void SetTileState(Vector2Int pos, TileState state)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Cannot set tile state at {pos}, out of bounds!");
                return;
            }
            grid[pos.x, pos.y] = state;
        }

        public void SetEntity(Vector2Int pos, object entity)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Cannot set entity at {pos}, out of bounds!");
                return;
            }
            entities[pos] = entity;
        }

        public T GetEntity<T>(Vector2Int pos) where T : class
        {
            if (!IsInBounds(pos))
            {
                return null;
            }

            if (entities.TryGetValue(pos, out object entity))
            {
                return entity as T;
            }
            return null;
        }

        public void RemoveEntity(Vector2Int pos)
        {
            if (entities.ContainsKey(pos))
            {
                entities.Remove(pos);
            }
        }

        public bool HasEntity(Vector2Int pos)
        {
            return entities.ContainsKey(pos);
        }

        // Get 4-directional neighbors (orthogonal)
        public List<Vector2Int> GetNeighbors(Vector2Int pos, bool includeDiagonal = false)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            // 4-directional (up, down, left, right)
            Vector2Int[] orthogonalDirections = new Vector2Int[]
            {
                Vector2Int.up,      // (0, 1)
                Vector2Int.down,    // (0, -1)
                Vector2Int.left,    // (-1, 0)
                Vector2Int.right    // (1, 0)
            };

            foreach (var dir in orthogonalDirections)
            {
                Vector2Int neighbor = pos + dir;
                if (IsInBounds(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            // Add diagonal neighbors if requested
            if (includeDiagonal)
            {
                Vector2Int[] diagonalDirections = new Vector2Int[]
                {
                    new Vector2Int(1, 1),   // Up-right
                    new Vector2Int(1, -1),  // Down-right
                    new Vector2Int(-1, 1),  // Up-left
                    new Vector2Int(-1, -1)  // Down-left
                };

                foreach (var dir in diagonalDirections)
                {
                    Vector2Int neighbor = pos + dir;
                    if (IsInBounds(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        // Manhattan distance between two positions
        public int GetDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        // Check if two positions are adjacent (4-directional)
        public bool AreAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            return GetDistance(pos1, pos2) == 1;
        }
    }
}

