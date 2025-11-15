using UnityEngine;

public class TileActionBinder : MonoBehaviour
{
    private TileData tileData;

    [Header("Plants")]
    public GameObject plant1;
    public GameObject plant2;
    public GameObject plant3;

    void Awake()
    {
        tileData = GetComponent<TileData>();

        if (tileData == null || tileData.actions == null)
            return;

        foreach (var action in tileData.actions)
        {
            switch (action.actionName)
            {
                case "Plant1":
                    action.callback = Plant1Action;
                    break;
                case "Plant2":
                    action.callback = Plant2Action;
                    break;
                case "Plant3":
                    action.callback = Plant3Action;
                    break;
                default:
                    Debug.LogWarning($"No callback bound for action '{action.actionName}' on tile {name}");
                    break;
            }
        }
    }

    void Plant1Action(GameObject tile)
    {
        SetActivePlant(plant1);
        Debug.Log("Set Plant 1: " + tile.name);
    }

    void Plant2Action(GameObject tile)
    {
        SetActivePlant(plant2);
        Debug.Log("Set Plant 2: " + tile.name);
    }

    void Plant3Action(GameObject tile)
    {
        SetActivePlant(plant3);
        Debug.Log("Set Plant 3: " + tile.name);
    }

    // Enable only the selected plant
    void SetActivePlant(GameObject activePlant)
    {
        if (plant1 != null) plant1.SetActive(plant1 == activePlant);
        if (plant2 != null) plant2.SetActive(plant2 == activePlant);
        if (plant3 != null) plant3.SetActive(plant3 == activePlant);
    }
}
