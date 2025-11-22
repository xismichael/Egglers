using UnityEngine;
using System;

public class TileData : MonoBehaviour
{
    [Serializable]
    public class TileAction
    {
        public string actionName;

        [NonSerialized]
        public Action<GameObject> callback;
    }

    public TileAction[] actions;
}