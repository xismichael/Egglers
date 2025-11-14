using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridHighlighter : MonoBehaviour
{
    public Camera cam;
    public bool ignoreUI = true;

    private GameObject hoveredTile;
    private GameObject previousHoveredTile;
    private GameObject selectedTile;

    // Tracks if a context menu is open
    public bool contextMenuOpen { get; private set; } = false;

    void Update()
    {
        UpdateHoveredTile();
        UpdateHighlights();
    }

    void UpdateHoveredTile()
    {
        previousHoveredTile = hoveredTile;

        var mouse = Mouse.current;
        if (mouse == null)
        {
            hoveredTile = null;
            return;
        }

        // Block hovering if UI is under the pointer or context menu is open
        if ((ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            || contextMenuOpen)
        {
            hoveredTile = null;
            return;
        }

        // Raycast from the mouse
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Tile"))
        {
            hoveredTile = hit.collider.gameObject;
        }
        else
        {
            hoveredTile = null;
        }
    }

    void UpdateHighlights()
    {
        // Remove previous hover highlight if no longer hovered
        if (previousHoveredTile != null && previousHoveredTile != hoveredTile && previousHoveredTile != selectedTile)
        {
            SetTileBorder(previousHoveredTile, false);
        }

        // Highlight hovered tile if it's not the selected tile
        if (hoveredTile != null && hoveredTile != selectedTile)
        {
            SetTileBorder(hoveredTile, true);
        }

        // Always highlight selected tile
        if (selectedTile != null)
        {
            SetTileBorder(selectedTile, true);
        }
    }

    void SetTileBorder(GameObject tile, bool enabled)
    {
        Transform border = tile.transform.Find("Border");
        if (border != null)
            border.gameObject.SetActive(enabled);
    }

    public void SetSelectedTile(GameObject tile)
    {
        if (selectedTile != null && selectedTile != tile)
            SetTileBorder(selectedTile, false);

        selectedTile = tile;

        if (selectedTile != null)
            SetTileBorder(selectedTile, true);
    }

    public void ClearSelectedTile()
    {
        if (selectedTile != null)
            SetTileBorder(selectedTile, false);

        selectedTile = null;
    }

    /// <summary>
    /// Called by the context menu to block/unblock highlighting
    /// </summary>
    public void SetContextMenuOpen(bool open)
    {
        contextMenuOpen = open;
    }
}
