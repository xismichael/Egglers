using UnityEngine;
using System.Collections;

namespace Egglers
{
    /// <summary>
    /// Game menu class taken from class pastebin: https://pastebin.com/w67Mj6ia
    /// Should be extended to make different menus
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class GameMenu : MonoBehaviour
    {

        protected bool isActive = false;
        public bool isDirty = false;
        protected Canvas canvas;
        protected CanvasGroup canvasGroup;

        [SerializeField] protected float fadeDuration = 0.3f;

        private Coroutine fadeRoutine;


        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            InnerAwake();
        }

        protected virtual void InnerAwake()
        {
            // For optional awake logic in extended menu class
        }

        // Update is called once per frame
        void Update()
        {
            if (!isActive) return;

            if (isDirty)
            {
                isDirty = false;
                RefreshMenu();
            }

            InnerUpdate();
        }

        protected virtual void InnerUpdate()
        {
            // For optional udpate logic in extended menu class
        }

        public virtual void RefreshMenu()
        {
            // Extended menu logic fore refresh here
            isDirty = false;
        }

        private IEnumerator FadeCanvas(float start, float end)
        {
            float elapsed = 0f;
            canvasGroup.alpha = start;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = end;
        }


        public virtual void CloseMenu()
        {
            // Extended menu logic before closing menu here
            // Debug.Log($"Closing canvas: {canvas}");
            //canvas.enabled = false;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isActive = false;

            fadeRoutine = StartCoroutine(FadeCanvas(1f, 0f));
        }

        public virtual void OpenMenu()
        {
            // Extended menu logic before opening menu here
            // Debug.Log($"Opening canvas: {canvas}");
            //canvas.enabled = true;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            isDirty = true;
            isActive = true;

            fadeRoutine = StartCoroutine(FadeCanvas(0f, 1f));
        }
    }
}
