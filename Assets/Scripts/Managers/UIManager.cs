using UnityEngine;

namespace Egglers
{
    public enum GameMenuID
    {
        Main,
        Credits,
        HUD,
        GameWin,
        GameLoose,
    }

    /// <summary>
    /// UI manager class taken from class pastebin: https://pastebin.com/jZ6hzMcJ
    /// Singleton manager of menu instances
    /// </summary>
    public class UIManager : MonoBehaviour
    {

        [SerializeField] private GameMenu[] Menus; // Array of game menus to be set in inspector from scene
        [SerializeField] private GameMenuID startingMenuID;
        [SerializeField] private GameMenuID currentMenuID;

        // Singleton implementation
        private static UIManager _instance;
        public static UIManager Instance { get { return _instance; } }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            // DontDestroyOnLoad(this);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            foreach (GameMenu menu in Menus)
            {
                if (menu == null)
                {
                    Debug.LogError("No menu found on " + menu.name);
                }

                menu.CloseMenu();
            }

            OpenMenu(startingMenuID);
        }

        private bool CloseMenu(GameMenuID menuID)
        {
            Menus[(int) menuID].CloseMenu();
            return true;
        }

        private bool OpenMenu(GameMenuID menuID)
        {
            Menus[(int) menuID].OpenMenu();
            Debug.Log($"[UI Manager] Opened menu ID: {menuID}");
            return true;
        }

        public void GoToMenu(GameMenuID menuID)
        {

            // Debug.Log($"Attempting to go to menu ID ({menuID}) from current menu ID ({currentMenuID})");
            if (currentMenuID == menuID)
            {
                Debug.LogWarning("Cannot move to menu ID (" + menuID + ") as we are currently on it");
            }
            else
            {
                CloseMenu(currentMenuID);
                currentMenuID = menuID;
                OpenMenu(currentMenuID);
            }
        }

        // Sets the menu dirty so that it knows it needs to refresh next update
        public void SetMenuDirty(GameMenuID menuID)
        {
            Menus[(int)menuID].isDirty = true;
        }
        
        public void SetCursorVisible(bool state)
        {
            // Debug.Log($"[UI Manager] Setting cursor to: {state}");
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
