using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Egglers
{   
    /// <summary>
    /// Tile menu, replaces context menu
    /// </summary>
    public class TileMenu : GameMenu
    {
        [SerializeField] private GridHighlighter highlightManager;

        // Set buttons in editor
        [SerializeField] private Button placeHeartButton;
        [SerializeField] private Button nipBudButton;
        [SerializeField] private Button removeGraftButton;
        [SerializeField] private Button applyGraftButton;
        [SerializeField] private Button debugButton;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private string defaultTooltip;


        protected override void InnerAwake()
        {
            AddEssentialListeners();
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            base.OpenMenu();
        }

        public override void RefreshMenu()
        {
            RemoveListeners();
            AddEssentialListeners();

            highlightManager.SetSelectedTile(GameManager.Instance.focusedTile);
            highlightManager.SetContextMenuOpen(true);

            tooltipText.gameObject.SetActive(true);
            tooltipText.text = defaultTooltip;
            
            TileActions tileActions = GameManager.Instance.focusedTile.GetComponent<TileActions>();

            // Go over the actions and tie them to the right buttons
            foreach (var action in tileActions.actions)
            {
                // Debug.Log($"[Tile menu] Action type: {action.actionType}");
                Button triggerButton = action.actionType switch
                {
                    TileActionType.PlaceHeart => placeHeartButton,
                    TileActionType.NipBud => nipBudButton,
                    TileActionType.ApplyGraft => applyGraftButton,
                    TileActionType.Debug => debugButton,
                    _ => null
                };

                if (triggerButton == null) continue;

                triggerButton.onClick.AddListener(() =>
                {
                    action.callback?.Invoke(GameManager.Instance.focusedTile);
                });

                // Tooltip hover
                EventTrigger trigger = triggerButton.gameObject.AddComponent<EventTrigger>();
                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((_) => ShowTooltip(action));
                trigger.triggers.Add(enterEntry);
            }

            base.RefreshMenu();
        }

        public override void CloseMenu()
        {
            highlightManager.SetContextMenuOpen(false);
            highlightManager.ClearSelectedTile();
            RemoveListeners();
            base.CloseMenu();
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

        private void OnRemoveGraftClicked()
        {
            Debug.Log("[TileMenu] Going to Graft Menu");
            UIManager.Instance.GoToMenu(GameMenuID.GraftMenu);
        }
        
        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            placeHeartButton.onClick.RemoveAllListeners();
            nipBudButton.onClick.RemoveAllListeners();
            removeGraftButton.onClick.RemoveAllListeners();
            applyGraftButton.onClick.RemoveAllListeners();
            removeGraftButton.onClick.RemoveAllListeners();
            debugButton.onClick.RemoveAllListeners();
        }

        private void AddEssentialListeners()
        {
            removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
        }
    }
}

