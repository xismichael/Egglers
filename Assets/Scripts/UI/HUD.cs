using UnityEngine;
using UnityEngine.UI;

namespace Egglers
{   
    public class HUD : GameMenu
    {

        protected override void InnerAwake()
        {
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            // GameManager.Instance.SetGamePaused(true);
            base.OpenMenu();
        }


        
        private void OnDestroy()
        {
        }
    }
}

