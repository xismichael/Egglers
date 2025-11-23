using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
namespace Egglers
{
    public class TileContextMenu : MonoBehaviour
    {
        public Camera cam;
        public RectTransform contextMenuPanel;
        public GridHighlighter highlightManager;
        public Button buttonPrefab; // assign a simple UI button prefab

        public Vector2 offset = new Vector2(10f, -10f);

        private GameObject selectedTile;
        private Canvas canvas;

        void Awake()
        {
            canvas = contextMenuPanel.GetComponentInParent<Canvas>();
            contextMenuPanel.gameObject.SetActive(false);

            // Center pivot — correct for clamping
            contextMenuPanel.pivot = new Vector2(0.5f, 0.5f);
            contextMenuPanel.anchorMin = new Vector2(0.5f, 0.5f);
            contextMenuPanel.anchorMax = new Vector2(0.5f, 0.5f);
        }

        void Update()
        {
            HandleClick();
            HandleMouseExit();
        }

        bool IsPointerOverContextMenu()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            return RectTransformUtility.RectangleContainsScreenPoint(
                contextMenuPanel, mousePos, canvas.worldCamera);
        }

        void HandleClick()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Only block clicks if over the context menu itself
            if (EventSystem.current != null && IsPointerOverContextMenu())
                return;

            // Left click?
            if (!mouse.leftButton.wasPressedThisFrame)
                return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Tile"))
            {
                selectedTile = hit.collider.gameObject;
                ShowContextMenu(mouse.position.ReadValue(), selectedTile);
            }
            else
            {
                HideContextMenu();
            }
        }

        void HandleMouseExit()
        {
            if (!contextMenuPanel.gameObject.activeSelf)
                return;

            // Only consider closing if pointer is NOT over the menu panel
            if (!IsPointerOverContextMenu())
            {
                HideContextMenu();
            }
        }

        void ShowContextMenu(Vector2 screenPos, GameObject tile)
        {
            selectedTile = tile;
            contextMenuPanel.gameObject.SetActive(true);
            highlightManager.SetSelectedTile(tile);
            highlightManager.SetContextMenuOpen(true);

            // Clear old buttons
            foreach (Transform child in contextMenuPanel)
                Destroy(child.gameObject);

            // Populate buttons dynamically
            TileActions tileActions = tile.GetComponent<TileActions>();
            if (tileActions != null && tileActions.actions != null)
            {
                foreach (var action in tileActions.actions)
                {
                    Button b = Instantiate(buttonPrefab, contextMenuPanel);
                    b.GetComponentInChildren<TMP_Text>().text = action.actionType.ToString();

                    b.onClick.AddListener(() =>
                    {
                        action.callback?.Invoke(tile);
                        HideContextMenu();
                    });
                }
            }

            // Position menu at mouse
            contextMenuPanel.position = screenPos;
        }

        void HideContextMenu()
        {
            contextMenuPanel.gameObject.SetActive(false);
            highlightManager.SetContextMenuOpen(false);
            highlightManager.ClearSelectedTile();
            selectedTile = null;
        }

        public void OnDoAction()
        {
            if (selectedTile != null)
            {
                Debug.Log("Action on " + selectedTile.name);
            }
            HideContextMenu();
        }
    }
}