// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;

// namespace Egglers
// {
//     /// <summary>
//     /// Graft menu, opened from tile menu
//     /// </summary>
//     public class GraftMenu : GameMenu
//     {
//         [SerializeField] private GridHighlighter highlightManager;

//         // Set elements in editor
//         [SerializeField] private Button removeGraftButton;
//         [SerializeField] private Button backButton;
//         [SerializeField] private Slider leafSlider;
//         [SerializeField] private Slider rootSlider;
//         [SerializeField] private Slider fruitSlider;
//         [SerializeField] private TMP_Text leafText;
//         [SerializeField] private TMP_Text rootText;
//         [SerializeField] private TMP_Text fruitText;
//         [SerializeField] private TMP_Text tooltipText;
//         [SerializeField] private string defaultTooltip;


//         protected override void InnerAwake()
//         {
//             AddEssentialListeners();
//             base.InnerAwake();
//         }


//         public override void OpenMenu()
//         {
//             UIManager.Instance.SetCursorVisible(true);
//             base.OpenMenu();
//         }

//         public override void RefreshMenu()
//         {
//             RemoveListeners();
//             AddEssentialListeners();

//             highlightManager.SetSelectedTile(GameManager.Instance.focusedTile);
//             highlightManager.SetContextMenuOpen(true);

//             tooltipText.gameObject.SetActive(true);
//             tooltipText.text = defaultTooltip;

//             // Set up remove graft button to pass slider values directly
//             removeGraftButton.onClick.AddListener(() =>
//             {
//                 int leafVal = (int)leafSlider.value;
//                 int rootVal = (int)rootSlider.value;
//                 int fruitVal = (int)fruitSlider.value;

//                 GridVisualTile visual = GameManager.Instance.focusedTile.GetComponent<GridVisualTile>();
//                 if (visual != null)
//                 {
//                     PlantBitManager.Instance.RemoveGraftAtPosition(visual.coords, leafVal, rootVal, fruitVal);
//                     UIManager.Instance.GoToMenu(GameMenuID.HUD);
//                 }
//             });

//             int currentLeaf = (int)leafSlider.value;
//             int currentRoot = (int)rootSlider.value;
//             int currentFruit = (int)fruitSlider.value;

//             leafText.text = currentLeaf.ToString();
//             rootText.text = currentRoot.ToString();
//             fruitText.text = currentFruit.ToString();

//             base.RefreshMenu();
//         }

//         public override void CloseMenu()
//         {
//             highlightManager.SetContextMenuOpen(false);
//             highlightManager.ClearSelectedTile();

//             RemoveListeners();
//             base.CloseMenu();
//         }

//         void ShowTooltip(TileActions.TileAction action)
//         {
//             tooltipText.gameObject.SetActive(true);
//             tooltipText.text = GetTooltipText(action);
//         }

//         string GetTooltipText(TileActions.TileAction action)
//         {
//             return action.actionType switch
//             {
//                 TileActionType.RemoveGraft => "Remove the graft.",
//                 TileActionType.Plant1 => "Select Plant 1.",
//                 TileActionType.Plant2 => "Select Plant 2.",
//                 TileActionType.Plant3 => "Select Plant 3.",
//                 TileActionType.Billboard => "Show a message.",
//                 _ => "Select an action."
//             };
//         }

//         private void OnBackClicked()
//         {
//             Debug.Log("[GraftMenu] Going back to Tile Menu");
//             UIManager.Instance.GoToMenu(GameMenuID.TileMenu);
//         }

//         private void OnRemoveGraftClicked()
//         {
//             // Handled by RefreshMenu listener now
//         }

//         private void OnSliderValueChanged(float val)
//         {
//             UIManager.Instance.SetMenuDirty(GameMenuID.GraftMenu);
//         }

//         private void OnDestroy()
//         {
//             RemoveListeners();
//         }

//         private void RemoveListeners()
//         {
//             removeGraftButton.onClick.RemoveAllListeners();
//             backButton.onClick.RemoveAllListeners();
//         }

//         private void AddEssentialListeners()
//         {
//             // Set default tooltip
//             backButton.onClick.AddListener(OnBackClicked);
//             removeGraftButton.onClick.AddListener(OnRemoveGraftClicked);
//             leafSlider.onValueChanged.AddListener(OnSliderValueChanged);
//             rootSlider.onValueChanged.AddListener(OnSliderValueChanged);
//             fruitSlider.onValueChanged.AddListener(OnSliderValueChanged);
//         }
//     }
// }

