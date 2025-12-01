using UnityEngine;
using System;

namespace Egglers
{
    public enum TileActionType
    {
        Plant1,
        Plant2,
        Plant3,
        Billboard,
        PlaceHeart,
        Debug,
        NipBud
    }

    public class TileActions : MonoBehaviour
    {

        [Serializable]
        public class TileAction
        {
            public TileActionType actionType;
            public Action<GameObject> callback;
            public bool enabled = true;
        }

        public TileAction[] actions;

        public void InvokeAction(TileActionType type)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action.actionType == type)
                {
                    action.callback?.Invoke(gameObject);
                    return;
                }
            }

            Debug.LogWarning($"Tile {name} has no action of type '{type}'");
        }
    }
}