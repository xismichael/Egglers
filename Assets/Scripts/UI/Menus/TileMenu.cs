using System;
using System.Collections.Generic;
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
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text tooltipText;

        private GameObject focusedTile;
        private TileActions tileActions;

        private enum ButtonActions
        {
            PlaceHeart,
            NipBud,
            RemoveGraft,
            ApplyGraft,
            Debug,
            CloseMenu,
            None,
        }

        private Button[] buttons;

        protected override void InnerAwake()
        {
            placeHeartButton.onClick.AddListener(OnPlaceHeartClicked);
            nipBudButton.onClick.AddListener(OnNipBudClicked);
            removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
            applyGraftButton.onClick.AddListener(OnApplyGraftClicked);
            debugButton.onClick.AddListener(OnDebugClicked);
            closeButton.onClick.AddListener(OnCloseClicked);

            buttons = new Button[]
            {
                placeHeartButton,
                nipBudButton,
                removeGraftButton,
                applyGraftButton,
                debugButton,
                closeButton,
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                // Tooltip hovering events
                EventTrigger trigger = buttons[i].gameObject.AddComponent<EventTrigger>();

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

            placeHeartButton.gameObject.SetActive(GameManager.Instance.gameState == GameState.HeartPlacement);

            highlightManager.SetSelectedTile(focusedTile);
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
                ButtonActions.PlaceHeart => "Place the heart here",
                ButtonActions.NipBud => "Nip a bud to gain energy",
                ButtonActions.ApplyGraft => "Apply stored graft",
                ButtonActions.RemoveGraft => "Remove graft",
                ButtonActions.Debug => "Debug this tile",
                ButtonActions.CloseMenu => "Close menu",
                ButtonActions.None => "",
                _ => ""
            };
        }

        private void OnPlaceHeartClicked()
        {
            tileActions.InvokeAction(TileActionType.PlaceHeart);
            UIManager.Instance.SetMenuDirty(GameMenuID.TileMenu);
        }

        private void OnNipBudClicked()
        {
            tileActions.InvokeAction(TileActionType.NipBud);
        }

        private void OnRemoveGraftClicked()
        {
            Debug.Log("[TileMenu] Going to Graft Menu");
            UIManager.Instance.GoToMenu(GameMenuID.GraftMenu);
        }

        private void OnApplyGraftClicked()
        {
            tileActions.InvokeAction(TileActionType.ApplyGraft);
        }

        private void OnDebugClicked()
        {
            tileActions.InvokeAction(TileActionType.Debug);
        }

        private void OnCloseClicked()
        {
            UIManager.Instance.GoToMenu(GameMenuID.HUD);
        }
        
        private void OnDestroy()
        {
            placeHeartButton.onClick.RemoveAllListeners();
            nipBudButton.onClick.RemoveAllListeners();
            removeGraftButton.onClick.RemoveAllListeners();
            applyGraftButton.onClick.RemoveAllListeners();
            removeGraftButton.onClick.RemoveAllListeners();
            debugButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
        }
    }
}

