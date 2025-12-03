using UnityEngine;
using UnityEngine.UI;

namespace Egglers
{
    public class MainMenu : GameMenu
    {
        // Set buttons in editor
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditsButton;

        protected override void InnerAwake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            Debug.Log("Subscribed");
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            // GameManager.Instance.SetGamePaused(true);
            base.OpenMenu();
            UIManager.Instance.GameCanvas.SetActive(false);
        }

        private void OnStartClicked()
        {
            UIManager.Instance.GoToMenu(GameMenuID.HUD);
            Debug.Log("[MainMenu] Going to HUD Menu");
            UIManager.Instance.GameCanvas.SetActive(true);
        }


        private void OnCreditsClicked()
        {
            UIManager.Instance.GoToMenu(GameMenuID.Credits);
            UIManager.Instance.GameCanvas.SetActive(false);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        }
    }
}

