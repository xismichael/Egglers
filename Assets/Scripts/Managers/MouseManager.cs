using UnityEngine;
using UnityEngine.InputSystem;

namespace Egglers
{
    public class MouseManager : MonoBehaviour
    {
        // Singleton implementation
        private static MouseManager _instance;
        public static MouseManager Instance { get { return _instance; } }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            // DontDestroyOnLoad(this);
        }

        // Update is called once per frame
        void Update()
        {
            MouseUpdate();
        }

        private void MouseUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (!mouse.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (UIManager.Instance.UIRaycast(mousePos)) return;

            
            
            GameObject tile = CameraManager.Instance.RaycastCheck(mousePos, "Tile");
            if (tile == null) return;

            //Debug.Log($"[MouseManager] New Tile is {tile.GetComponent<GridVisualTile>().coords}");

    

            GameManager.Instance.focusedTile = tile;

            if (GameManager.Instance.gameState == GameState.HeartPlacement)
            {
                TileActions tileActions = tile.GetComponent<TileActions>();
                if (tileActions != null)
                {
                    tileActions.InvokeAction(TileActionType.PlaceHeart);
                }
            }
            else if (GameManager.Instance.gameState == GameState.Playing)
            {
                CameraManager.Instance.PanToTarget(tile.transform.position);
                CameraManager.Instance.ZoomToTarget(0.3f);
                UIManager.Instance.OpenPlantInfo();

            }
        }
    }
}

