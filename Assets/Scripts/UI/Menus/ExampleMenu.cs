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
    public class ExampleMenu : GameMenu
    {
        // Set buttons in editor
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;

        protected override void InnerAwake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            optionsButton.onClick.AddListener(OnOptionsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            // GameManager.Instance.SetGamePaused(true);
            base.OpenMenu();
        }

        private void OnStartClicked()
        {
            Debug.Log("[MainMenu] Going to HUD Menu");
            // UIManager.Instance.GoToMenu(GameMenuID.HUD);
        }

        private void OnOptionsClicked()
        {
            Debug.Log("[MainMenu] Going to Options Menu");
            // UIManager.Instance.GoToMenu(GameMenuID.Options);
        }

        private void OnCreditsClicked()
        {
            Debug.Log("[MainMenu] Going to Credits Menu");
            // UIManager.Instance.GoToMenu(GameMenuID.Credits);
        }
        
        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            optionsButton.onClick.RemoveListener(OnOptionsClicked);
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        }
    }
}

