using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

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
    public class GameOverMenu : GameMenu
    {
        [SerializeField] private RawImage outcomeImage;
        [SerializeField] private Texture winTexture;
        [SerializeField] private Texture loseTexture;
        [SerializeField] private TMP_Text gameover;
        [SerializeField] private Button resetGameButton;
        [SerializeField] private float gameOverFadeDuration = 2.2f;
        [SerializeField] private float openDelay = 1.0f;


        private void Awake()
        {
            resetGameButton.onClick.AddListener(OnResetGameButtonClicked);
        }

        public override void OpenMenu()
        {
            StartCoroutine(OpenWithDelay());
        }
        private IEnumerator OpenWithDelay()
        {
            yield return new WaitForSecondsRealtime(openDelay);   // works even if paused

            fadeDuration = gameOverFadeDuration;                  // slower fade
            base.OpenMenu();                                      // fade-in happens here

            UIManager.Instance.SetCursorVisible(true);
            UpdateOutcomeTexture();
        }

        private void UpdateOutcomeTexture()
        {

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[GameOverMenu] GameManager instance not available.");
                return;
            }

            Texture targetTexture = loseTexture;

            if (GameManager.Instance.gameState == GameState.Won)
            {
                targetTexture = winTexture != null ? winTexture : targetTexture;
                gameover.text = "You Win! \nAll pollution sources destroyed!";
            }
            else if (GameManager.Instance.gameState == GameState.Lost)
            {
                targetTexture = loseTexture;
                gameover.text = "You Lose! \nPollution killed your plant!";
            }

            outcomeImage.texture = targetTexture;
        }

        private void OnResetGameButtonClicked()
        {
            GameManager.Instance.ResetGame();
        }

        private void OnDestroy()
        {
            resetGameButton.onClick.RemoveListener(OnResetGameButtonClicked);
        }
    }
}

