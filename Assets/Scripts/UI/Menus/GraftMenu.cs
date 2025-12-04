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
        
        private GameObject focusedTile;
        private TileActions tileActions;

        private enum ButtonActions
        {
            RemoveGraft,
            Back,
            LeafChange,
            RootChange,
            FruitChange,
            None,
        }

        private GameObject[] interactables;

        protected override void InnerAwake()
        {
            removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
            backButton.onClick.AddListener(OnBackClicked);
            leafSlider.onValueChanged.AddListener(OnSliderValueChanged);
            rootSlider.onValueChanged.AddListener(OnSliderValueChanged);
            fruitSlider.onValueChanged.AddListener(OnSliderValueChanged);

            interactables = new GameObject[]
            {
                removeGraftButton.gameObject,
                backButton.gameObject,
                leafSlider.gameObject,
                rootSlider.gameObject,
                fruitSlider.gameObject,
            };

            for (int i = 0; i < interactables.Length; i++)
            {
                // Tooltip hovering events
                EventTrigger trigger = interactables[i].AddComponent<EventTrigger>();

                ButtonActions action = (ButtonActions) i;
                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((_) => ShowTooltip(action));
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((_) => ShowTooltip(ButtonActions.None));
                trigger.triggers.Add(exitEntry);
            }

            ShowTooltip(ButtonActions.None);

            base.InnerAwake();
        }

        public override void RefreshMenu()
        {
            focusedTile = GameManager.Instance.focusedTile;
            tileActions = GameManager.Instance.focusedTile.GetComponent<TileActions>();

            highlightManager.SetSelectedTile(GameManager.Instance.focusedTile);
            highlightManager.SetContextMenuOpen(true);

            base.RefreshMenu();
        }

        public override void CloseMenu()
        {
            highlightManager.SetContextMenuOpen(false);
            highlightManager.ClearSelectedTile();
            base.CloseMenu();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            base.OpenMenu();
        }

        void ShowTooltip(ButtonActions action)
        {
            tooltipText.text = GetTooltipText(action);
        }

        string GetTooltipText(ButtonActions action)
        {
            return action switch
            {
                ButtonActions.RemoveGraft => "Remove graft",
                ButtonActions.Back => "Go back to plant menu",
                ButtonActions.LeafChange => "Set amount of leaf to take",
                ButtonActions.RootChange => "Set amount of root to take",
                ButtonActions.FruitChange => "Set amount of fruit to take",
                ButtonActions.None => "",
                _ => ""
            };
        }

        private void OnBackClicked()
        {
            Debug.Log("[GraftMenu] Going back to Tile Menu");
            UIManager.Instance.GoToMenu(GameMenuID.TileMenu);
        }

        private void OnRemoveGraftClicked()
        {
            // Stash the graft data here so it can be used later by the tile action to remove the graft
            int leafVal = (int) leafSlider.value;
            int rootVal = (int) rootSlider.value;
            int fruitVal = (int) fruitSlider.value;
            PlantBitManager.Instance.StashGraftData(leafVal, rootVal, fruitVal);

            UIManager.Instance.GoToMenu(GameMenuID.HUD);
        }

        private void OnSliderValueChanged(float val)
        {
            int leafVal = (int) leafSlider.value;
            int rootVal = (int) rootSlider.value;
            int fruitVal = (int) fruitSlider.value;

            leafText.text = leafVal.ToString();
            rootText.text = rootVal.ToString();
            fruitText.text = fruitVal.ToString();
        }
        
        private void OnDestroy()
        {
            removeGraftButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }
    }
}

