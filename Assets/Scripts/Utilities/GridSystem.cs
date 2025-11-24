using System;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Unified grid system for plants, pollution tiles, and sources.
    /// Replaces both old GameGrid and GridSystem.
    /// </summary>
    public class GridSystem
    {
        private int width;
        private int height;

        // Tile state for quick reference
        private TileState[,] tileStates;

        // Entities stored by position
        private Dictionary<Vector2Int, object> entities;

        public int Width => width;
        public int Height => height;

        public void Initialize(int gridWidth, int gridHeight)
        {
            width = gridWidth;
            height = gridHeight;

            tileStates = new TileState[width, height];
            entities = new Dictionary<Vector2Int, object>();

            // Initialize all tiles as empty
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tileStates[x, y] = TileState.Empty;
                }
            }
        }

        #region Bounds & Adjacency
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public int GetDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        public bool AreAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            return GetDistance(pos1, pos2) == 1;
        }

        public List<Vector2Int> GetNeighbors(Vector2Int pos, bool includeDiagonal = false)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            Vector2Int[] orthogonal = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in orthogonal)
            {
                Vector2Int n = pos + dir;
                if (IsInBounds(n)) neighbors.Add(n);
            }

            if (includeDiagonal)
            {
                Vector2Int[] diagonal = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
                foreach (var dir in diagonal)
                {
                    Vector2Int n = pos + dir;
                    if (IsInBounds(n)) neighbors.Add(n);
                }
            }

            return neighbors;
        }
        #endregion

        #region TileState
        public TileState GetTileState(Vector2Int pos)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Position {pos} is out of bounds!");
                return TileState.Empty;
            }
            return tileStates[pos.x, pos.y];
        }

        public void SetTileState(Vector2Int pos, TileState state)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Cannot set tile state at {pos}, out of bounds!");
                return;
            }
            tileStates[pos.x, pos.y] = state;
        }
        #endregion

        #region Entity Management
        public void SetEntity(Vector2Int pos, object entity)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"Cannot set entity at {pos}, out of bounds!");
                return;
            }

            entities[pos] = entity;

            // Automatically update tile state
            if (entity is PlantBit)
                SetTileState(pos, TileState.Plant);
            else if (entity is PollutionTile)
                SetTileState(pos, TileState.Pollution);
            else if (entity is PollutionSource)
                SetTileState(pos, TileState.PollutionSource);
            else if (entity == null)
                SetTileState(pos, TileState.Empty);
        }

        public T GetEntity<T>(Vector2Int pos) where T : class
        {
            if (!IsInBounds(pos)) return null;

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
                SetTileState(pos, TileState.Empty);
            }
        }

        public bool HasEntity(Vector2Int pos)
        {
            return entities.ContainsKey(pos);
        }
        #endregion

        #region Convenience Methods
        /// <summary>
        /// Returns a PollutionTile at a position, creating one if necessary.
        /// </summary>
        public PollutionTile GetOrCreatePollutionTile(Vector2Int pos, float spreadRate = 10f, float strength = 10f, float resistance = 5f)
        {
            PollutionTile tile = GetEntity<PollutionTile>(pos);
            if (tile != null) return tile;

            tile = new PollutionTile(pos)
            {
                pollutionSpreadRate = spreadRate,
                pollutionStrength = strength,
                pollutionResistance = resistance
            };
            SetEntity(pos, tile);
            return tile;
        }

        /// <summary>
        /// Returns a PlantBit at a position, if any.
        /// </summary>
        public PlantBit GetPlantBit(Vector2Int pos)
        {
            return GetEntity<PlantBit>(pos);
        }

        /// <summary>
        /// Returns a PollutionSource at a position, if any.
        /// </summary>
        public PollutionSource GetPollutionSource(Vector2Int pos)
        {
            return GetEntity<PollutionSource>(pos);
        }
        #endregion
    }
}
