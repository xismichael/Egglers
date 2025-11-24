using System;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Unified grid system for plants, pollution tiles, and pollution sources.
    /// Supports multiple entities per tile and type-safe access by entity instance or position.
    /// </summary>
    public class GridSystem
    {
        private int width;
        private int height;

        private TileState[,] tileStates;

        // Dictionary of position → list of entities at that tile
        private Dictionary<Vector2Int, List<object>> entities;

        public int Width => width;
        public int Height => height;

        #region Initialization
        public void Initialize(int gridWidth, int gridHeight)
        {
            width = gridWidth;
            height = gridHeight;

            tileStates = new TileState[width, height];
            entities = new Dictionary<Vector2Int, List<object>>();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tileStates[x, y] = TileState.Empty;
        }
        #endregion

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
            List<Vector2Int> neighbors = new();

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

        private void UpdateTileState(Vector2Int pos)
        {
            if (!entities.TryGetValue(pos, out var list) || list.Count == 0)
            {
                tileStates[pos.x, pos.y] = TileState.Empty;
                return;
            }

            // Priority for display: Plant > Pollution > PollutionSource
            tileStates[pos.x, pos.y] = list[0] switch
            {
                PlantBit => TileState.Plant,
                PollutionTile => TileState.Pollution,
                PollutionSource => TileState.PollutionSource,
                _ => TileState.Empty
            };
        }
        #endregion

        #region Entity Management

        /// <summary>
        /// Add or replace an entity on the grid using the entity's own position.
        /// </summary>
        public void SetEntity<T>(T entity) where T : class
        {
            if (entity == null) return;
            Vector2Int pos = GetPositionFromEntity(entity);
            if (!IsInBounds(pos)) return;

            if (!entities.TryGetValue(pos, out var list))
            {
                list = new List<object>();
                entities[pos] = list;
            }

            // Remove any existing entity of the same type
            list.RemoveAll(e => e.GetType() == typeof(T));
            list.Add(entity);

            UpdateTileState(pos);
        }

        /// <summary>
        /// Remove an entity from the grid using the entity's own position.
        /// </summary>
        public void RemoveEntity<T>(T entity) where T : class
        {
            if (entity == null) return;
            Vector2Int pos = GetPositionFromEntity(entity);
            if (!IsInBounds(pos) || !entities.TryGetValue(pos, out var list)) return;

            list.RemoveAll(e => e is T);
            if (list.Count == 0) entities.Remove(pos);
            UpdateTileState(pos);

        }

        /// <summary>
        /// Get an entity of type T at the entity's position.
        /// </summary>
        public T GetEntity<T>(T entity) where T : class
        {
            if (entity == null) return null;
            Vector2Int pos = GetPositionFromEntity(entity);
            return GetEntity<T>(pos);
        }

        /// <summary>
        /// Get an entity of type T at a given position.
        /// </summary>
        public T GetEntity<T>(Vector2Int pos) where T : class
        {
            if (!IsInBounds(pos)) return null;
            if (!entities.TryGetValue(pos, out var list)) return null;

            foreach (var e in list)
                if (e is T t) return t;

            return null;
        }

        private Vector2Int GetPositionFromEntity(object entity)
        {
            return entity switch
            {
                PlantBit p => p.position,
                PollutionTile t => t.position,
                PollutionSource s => s.position,
                _ => throw new ArgumentException("Unknown entity type")
            };
        }

        public bool HasEntity<T>(T entity) where T : class
        {
            if (entity == null) return false;
            Vector2Int pos = GetPositionFromEntity(entity);
            return entities.TryGetValue(pos, out var list) && list.Exists(e => e is T);
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

            SetEntity(tile);
            return tile;
        }

        /// <summary>
        /// Get a PlantBit at a position if any.
        /// </summary>
        public PlantBit GetPlantBit(Vector2Int pos)
        {
            return GetEntity<PlantBit>(pos);
        }

        /// <summary>
        /// Get a PollutionSource at a position if any.
        /// </summary>
        public PollutionSource GetPollutionSource(Vector2Int pos)
        {
            return GetEntity<PollutionSource>(pos);
        }

        #endregion
    }
}
