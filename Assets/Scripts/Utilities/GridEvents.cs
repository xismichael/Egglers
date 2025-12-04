using System;
using UnityEngine;

public static class GridEvents
{
    // Fires whenever a plant bit changes at a specific tile
    public static event Action<Vector2Int> OnPlantUpdated;
    public static event Action<Vector2Int> OnPollutionUpdated;
    public static event Action<Vector2Int> OnTileStateChanged;
    public static event Action<Vector2Int> OnPlantKilledByPollution;
    public static event Action<Vector2Int> OnPollutionKilledByPlant;

    // Helper method to trigger the event safely
    public static void PlantUpdated(Vector2Int pos)
    {
        OnPlantUpdated?.Invoke(pos);
    }

    public static void PollutionUpdated(Vector2Int pos)
    {
        OnPollutionUpdated?.Invoke(pos);
    }

    public static void TileStateChanged(Vector2Int pos)
    {
        OnTileStateChanged?.Invoke(pos);
    }

    public static void PlantKilledByPollution(Vector2Int pos)
    {
        OnPlantKilledByPollution?.Invoke(pos);
    }

    public static void PollutionKilledByPlant(Vector2Int pos)
    {
        OnPollutionKilledByPlant?.Invoke(pos);
    }
}
