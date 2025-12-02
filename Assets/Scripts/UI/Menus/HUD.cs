using UnityEngine;

namespace Egglers
{   
    /// <summary>
    /// HUD with stats should go here eventually
    /// </summary>
    public class HUD : GameMenu
    {
        protected override void InnerAwake()
        {
            base.InnerAwake();
        }

        public override void OpenMenu()
        {
            UIManager.Instance.SetCursorVisible(true);
            base.OpenMenu();
        }
    }
}
