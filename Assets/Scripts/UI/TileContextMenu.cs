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
        public RectTransform buttonsParent; // assign the "Buttons" parent here
        public GridHighlighter highlightManager;
        public Button buttonPrefab; // assign a simple UI button prefab
        public TMP_Text tooltipText; // assign your inline TMP_Text
        public Vector2 offset = new Vector2(10f, -10f);
        public string defaultTooltip = "Select an action."; // can also be set in the Inspector

        private GameObject selectedTile;
        private Canvas canvas;
        void Awake()
        {
            canvas = contextMenuPanel.GetComponentInParent<Canvas>();
            contextMenuPanel.gameObject.SetActive(false);
            tooltipText.gameObject.SetActive(false);

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
            if (!contextMenuPanel.gameObject.activeInHierarchy)
                return false;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            Canvas rootCanvas = contextMenuPanel.GetComponentInParent<Canvas>();
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            return RectTransformUtility.RectangleContainsScreenPoint(
                contextMenuPanel, mousePos, cam
            );
        }

        void HandleClick()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Only block clicks if over the context menu itself
            if (IsPointerOverContextMenu())
                return;

            if (!mouse.leftButton.wasPressedThisFrame)
                return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);

            // Draw the ray in the Scene view (red for 1 second)
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Debug.Log($"Ray hit: {hit.collider.name} at {hit.point}");

                if (hit.collider.CompareTag("Tile"))
                {
                    selectedTile = hit.collider.gameObject;
                    ShowContextMenu(mousePos, selectedTile);
                }
                else
                {
                    // Debug.Log($"Hit object is not a tile: {hit.collider.name}");
                    HideContextMenu();
                }
            }
            else
            {
                // Debug.Log("Raycast did not hit anything.");
                HideContextMenu();
            }
        }

        void HandleMouseExit()
        {
            if (!contextMenuPanel.gameObject.activeSelf)
                return;

            if (!IsPointerOverContextMenu())
            {
                HideContextMenu();
            }
        }

        void ShowContextMenu(Vector2 screenPos, GameObject tile)
        {
            selectedTile = tile;
            CameraManager.Instance.PanToTarget(selectedTile.transform.position);
            CameraManager.Instance.ZoomToTarget(0.2f);
            contextMenuPanel.gameObject.SetActive(true);
            highlightManager.SetSelectedTile(tile);
            highlightManager.SetContextMenuOpen(true);

            UpdateActionButtons(tile);

            // Position context menu
            contextMenuPanel.position = screenPos;

            // Set default tooltip
            tooltipText.gameObject.SetActive(true);
            tooltipText.text = defaultTooltip;
        }

        void UpdateActionButtons(GameObject tile)
        {
            // Clear previous buttons
            foreach (Transform child in buttonsParent)
            {
                Destroy(child.gameObject);
            }

            TileActions tileActions = tile.GetComponent<TileActions>();
            if (tileActions != null && tileActions.actions != null)
            {
                foreach (var action in tileActions.actions)
                {
                    Button b = Instantiate(buttonPrefab, buttonsParent);
                    b.GetComponentInChildren<TMP_Text>().text = action.actionType.ToString();
                    b.onClick.AddListener(() =>
                    {
                        action.callback?.Invoke(tile);
                        HideContextMenu();
                    });

                    // Tooltip hover
                    EventTrigger trigger = b.gameObject.AddComponent<EventTrigger>();
                    var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    enterEntry.callback.AddListener((_) => ShowTooltip(action));
                    trigger.triggers.Add(enterEntry);
                }
            }
        }



        void ShowTooltip(TileActions.TileAction action)
        {
            tooltipText.gameObject.SetActive(true);
            tooltipText.text = GetTooltipText(action);
        }

        string GetTooltipText(TileActions.TileAction action)
        {
            return action.actionType switch
            {
                TileActionType.PlaceHeart => "Place the heart here.",
                TileActionType.NipBud => "Nip a bud to gain energy.",
                TileActionType.ApplyGraft => "Apply stored graft.",
                TileActionType.Plant1 => "Select Plant 1.",
                TileActionType.Plant2 => "Select Plant 2.",
                TileActionType.Plant3 => "Select Plant 3.",
                TileActionType.Billboard => "Show a message.",
                TileActionType.Debug => "Debug this tile.",
                _ => "Select an action."
            };
        }

        void HideContextMenu()
        {
            contextMenuPanel.gameObject.SetActive(false);
            highlightManager.SetContextMenuOpen(false);
            highlightManager.ClearSelectedTile();
            selectedTile = null;
        }
    }
}
