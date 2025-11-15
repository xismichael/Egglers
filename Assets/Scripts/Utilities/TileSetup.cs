using UnityEngine;

public class TileSetup : MonoBehaviour
{
    public GameObject[] allTiles; // assign all tiles in inspector or generate dynamically

    void Start()
    {
        foreach (var tileObj in allTiles)
        {
            TileData tile = tileObj.GetComponent<TileData>();
            if (tile != null)
                SetupTile(tile);
        }
    }

    void SetupTile(TileData tile)
    {
        tile.actions = new TileData.TileAction[]
        {
            new TileData.TileAction
            {
                actionName = "Move",
                callback = t => Debug.Log("Move clicked on " + t.name)
            },
            new TileData.TileAction
            {
                actionName = "Attack",
                callback = t => Debug.Log("Attack clicked on " + t.name)
            }
        };
    }
}
