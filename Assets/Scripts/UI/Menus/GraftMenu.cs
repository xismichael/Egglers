using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Egglers
{
    /// <summary>
    /// Graft menu, opened from tile menu
    /// </summary>
    public class GraftMenu : GameMenu
    {
        [SerializeField] private GridHighlighter highlightManager;

        // Set elements in editor
        [SerializeField] private Button removeGraftButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Slider leafSlider;
        [SerializeField] private Slider rootSlider;
        [SerializeField] private Slider fruitSlider;
        [SerializeField] private TMP_Text leafText;
        [SerializeField] private TMP_Text rootText;
        [SerializeField] private TMP_Text fruitText;
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
                // Debug.Log($"[Graft menu] Action type: {action.actionType}");
                Button triggerButton = action.actionType switch
                {
                    TileActionType.RemoveGraft => removeGraftButton,
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

            int leafVal = (int)leafSlider.value;
            int rootVal = (int)rootSlider.value;
            int fruitVal = (int)fruitSlider.value;

            leafText.text = leafVal.ToString();
            rootText.text = rootVal.ToString();
            fruitText.text = fruitVal.ToString();

            // Stash the graft data here so it can be used later by the tile action to remove the graft
            PlantBitManager.Instance.StashGraftData(leafVal, rootVal, fruitVal);

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
                TileActionType.RemoveGraft => "Remove the graft.",
                TileActionType.Plant1 => "Select Plant 1.",
                TileActionType.Plant2 => "Select Plant 2.",
                TileActionType.Plant3 => "Select Plant 3.",
                TileActionType.Billboard => "Show a message.",
                _ => "Select an action."
            };
        }

        private void OnBackClicked()
        {
            Debug.Log("[GraftMenu] Going back to Tile Menu");
            UIManager.Instance.GoToMenu(GameMenuID.TileMenu);
        }

        private void OnRemoveGraftClicked()
        {
            UIManager.Instance.GoToMenu(GameMenuID.HUD);
        }

        private void OnSliderValueChanged(float val)
        {
            UIManager.Instance.SetMenuDirty(GameMenuID.GraftMenu);
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            removeGraftButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }

        private void AddEssentialListeners()
        {
            // Set default tooltip
            backButton.onClick.AddListener(OnBackClicked);
            removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
            leafSlider.onValueChanged.AddListener(OnSliderValueChanged);
            rootSlider.onValueChanged.AddListener(OnSliderValueChanged);
            fruitSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }
}

