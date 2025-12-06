using UnityEngine;
using UnityEngine.UI;

namespace Egglers
{   
    /// <summary>
    /// Example menu, make sure these scripts are on objects with a canvas and canvas group component
    /// There should be one canvas for the scene and a subcanvas child for each menu (that subcanvas should have this script on it)
    /// Usually I have a panel as the only child of the menu canvas and put all the stuff in the panel
    /// I just drag the buttons in on the editor and use addListener, this works not sure if its best practice
    /// See the base class for all the overrideable functions
    /// ALSO DONT FORGET TO SET THE MENUS IN THE UI MANAGER IN THE EDITOR BASED ON THE ORDER IN THE MANAGER SCRIPT
    /// </summary>
    public class Credit : GameMenu
    {
        // Set buttons in editor
        [SerializeField] private Button exitButton;

        protected override void InnerAwake()
        {
            exitButton.onClick.AddListener(OnExitClicked);
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            // GameManager.Instance.SetGamePaused(true);
            base.OpenMenu();
        }


        private void OnExitClicked()
        {
            UIManager.Instance.GoToMenu(GameMenuID.Main);
            SoundManager.Instance.PlayButtonClick();
        }
        
        private void OnDestroy()
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
}
