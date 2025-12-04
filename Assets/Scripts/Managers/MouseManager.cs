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
            // Debug.Log("[MouseManager] Pointer clicked, before check");

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Debug.Log("[MouseManager] Pointer clicked");

            // Only block clicks if over the context menu itself
            // if (IsPointerOverContextMenu())
            //     return;

            if (!mouse.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (UIManager.Instance.UIRaycast(mousePos)) return;

            GameObject tile = CameraManager.Instance.RaycastCheck(mousePos, "Tile");
            if (tile == null) return;

            Debug.Log($"[MouseManager] New Tile is {tile.GetComponent<GridVisualTile>().coords}");

            CameraManager.Instance.PanToTarget(tile.transform.position);
            CameraManager.Instance.ZoomToTarget(0.3f);

            GameManager.Instance.focusedTile = tile;
            UIManager.Instance.SetMenuDirty(GameMenuID.TileMenu);
            UIManager.Instance.GoToMenu(GameMenuID.TileMenu);
        }
    }
}

