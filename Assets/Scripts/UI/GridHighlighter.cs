using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridHighlighter : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public bool ignoreUI = true; // if true, hovering is blocked when pointer over UI

    [Header("Tween Settings")]
    [Tooltip("Multiplier applied to the tile's original scale when highlighted.")]
    public float highlightMultiplier = 1.08f;

    [Tooltip("Time (seconds) it takes to tween to target scale.")]
    public float tweenDuration = 0.12f;

    [Tooltip("If true, uses an overshooting ease (back) when enlarging.")]
    public bool useOvershootOnEnlarge = true;

    // internal hover/selection tracking
    private GameObject hoveredTile;
    private GameObject previousHoveredTile;
    private GameObject selectedTile;
    public bool contextMenuOpen { get; private set; } = false;

    // per-tile tween state
    class TweenState
    {
        public Vector3 originalScale;
        public float startScalar;   // scalar applied to originalScale at start of tween
        public float targetScalar;  // target scalar to reach
        public float elapsed;       // time elapsed in tween
        public float duration;      // tween duration
        public bool animating => elapsed < duration;
    }

    private readonly Dictionary<GameObject, TweenState> states = new Dictionary<GameObject, TweenState>(128);

    void Update()
    {
        UpdateHoveredTile();
        UpdateTargets();
        AdvanceTweens(Time.deltaTime);
    }

    // --------------------------
    // input / hover detection
    // --------------------------
    void UpdateHoveredTile()
    {
        previousHoveredTile = hoveredTile;

        var mouse = Mouse.current;
        if (mouse == null)
        {
            hoveredTile = null;
            return;
        }

        if ((ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || contextMenuOpen)
        {
            hoveredTile = null;
            return;
        }

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Tile"))
            hoveredTile = hit.collider.gameObject;
        else
            hoveredTile = null;
    }

    // --------------------------
    // decide per-tile targets
    // --------------------------
    void UpdateTargets()
    {
        // 1) If previous hovered tile lost hover and isn't selected, target -> original
        if (previousHoveredTile != null && previousHoveredTile != hoveredTile && previousHoveredTile != selectedTile)
        {
            EnsureState(previousHoveredTile);
            StartTween(previousHoveredTile, 1f, tweenDuration);
        }

        // 2) Hovered tile (if any) and not selected -> target -> highlight
        if (hoveredTile != null && hoveredTile != selectedTile)
        {
            EnsureState(hoveredTile);
            StartTween(hoveredTile, highlightMultiplier, tweenDuration, enlarge: true);
        }

        // 3) Selected tile should always be highlighted
        if (selectedTile != null)
        {
            EnsureState(selectedTile);
            StartTween(selectedTile, highlightMultiplier, tweenDuration, enlarge: true);
        }
    }

    // --------------------------
    // tween progression
    // --------------------------
    void AdvanceTweens(float dt)
    {
        if (states.Count == 0) return;

        var toRemove = new List<GameObject>(8);

        foreach (var kv in states)
        {
            GameObject tile = kv.Key;
            TweenState s = kv.Value;

            // advance time
            if (s.elapsed < s.duration)
            {
                s.elapsed += dt;
                float t = Mathf.Clamp01(s.elapsed / s.duration);

                // choose easing
                float eased;
                if (s.targetScalar > s.startScalar && useOvershootOnEnlarge) // enlarging
                    eased = EaseOutBack(t);
                else
                    eased = EaseOutCubic(t);

                float currentScalar = Mathf.Lerp(s.startScalar, s.targetScalar, eased);

                tile.transform.localScale = s.originalScale * currentScalar;
            }
            else
            {
                // ensure final exact value
                tile.transform.localScale = s.originalScale * s.targetScalar;

                // if targetScalar is 1 and tile is neither hovered nor selected, we can optionally drop state
                bool shouldKeepState = (tile == hoveredTile) || (tile == selectedTile);
                if (!shouldKeepState && Mathf.Approximately(s.targetScalar, 1f))
                    toRemove.Add(tile);
            }
        }

        // prune finished states that we no longer need
        foreach (var t in toRemove)
            states.Remove(t);
    }

    // --------------------------
    // state helpers
    // --------------------------
    void EnsureState(GameObject tile)
    {
        if (!states.ContainsKey(tile))
        {
            states[tile] = new TweenState
            {
                originalScale = tile.transform.localScale,
                startScalar = 1f,
                targetScalar = 1f,
                elapsed = 0f,
                duration = tweenDuration
            };
        }
    }

    // Start a tween from the tile's *current* scalar to targetScalar
    void StartTween(GameObject tile, float targetScalar, float duration, bool enlarge = false)
    {
        EnsureState(tile);
        TweenState s = states[tile];

        // compute current scalar relative to original (in case it was mid-tween)
        Vector3 currentScale = tile.transform.localScale;
        float currentScalar = (s.originalScale.x != 0f) ? (currentScale.x / s.originalScale.x) : 1f;

        s.startScalar = currentScalar;
        s.targetScalar = targetScalar;
        s.elapsed = 0f;
        s.duration = Mathf.Max(0.001f, duration);

        // If we're enlarging and using overshoot, make overshoot slightly larger than target then lerp
        if (enlarge && useOvershootOnEnlarge)
        {
            // We'll still use EaseOutBack which provides overshoot behaviour based on t; no extra target needed.
        }
    }

    // --------------------------
    // API for context menu
    // --------------------------
    public void SetSelectedTile(GameObject tile)
    {
        // Unselect previous
        if (selectedTile != null && selectedTile != tile)
        {
            StartTween(selectedTile, 1f, tweenDuration);
        }

        selectedTile = tile;

        if (selectedTile != null)
        {
            EnsureState(selectedTile);
            StartTween(selectedTile, highlightMultiplier, tweenDuration, enlarge: true);
        }
    }

    public void ClearSelectedTile()
    {
        if (selectedTile != null)
        {
            StartTween(selectedTile, 1f, tweenDuration);
            selectedTile = null;
        }
    }

    public void SetContextMenuOpen(bool open)
    {
        contextMenuOpen = open;

        // When opening the menu, we want hovered tiles to stop responding (handled in UpdateHoveredTile).
        // When closing, ensure hovered tile (if any) properly gets highlighted again:
        if (!open && hoveredTile != null)
        {
            StartTween(hoveredTile, highlightMultiplier, tweenDuration, enlarge: true);
        }
    }

    // --------------------------
    // Easing functions
    // --------------------------
    // t in [0,1]
    static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t = t - 1f;
        return 1f + t * t * t;
    }

    // Back easing (overshoot)
    static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
