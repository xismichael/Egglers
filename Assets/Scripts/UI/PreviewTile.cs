using Egglers;
using UnityEngine;

public class PreviewCloneRotator : MonoBehaviour
{
    [Header("Where the clone will appear")]
    public Transform previewContainer;

    [Header("Source")]
    public GameObject focusedTile;   // This can be updated by ANY script

    [Header("Rotation")]
    public float rotationSpeed = 30f;

    private GameObject previewClone;
    private GameObject lastFocusedTile;

    void Update()
    {
        focusedTile = GameManager.Instance.focusedTile;
        // AUTO-DETECT CHANGE
        if (focusedTile != lastFocusedTile)
        {
            lastFocusedTile = focusedTile;
            RefreshPreview();
        }

        // ROTATE PREVIEW
        if (previewClone != null)
            previewClone.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void RefreshPreview()
    {
        // Delete old clone
        if (previewClone != null)
            Destroy(previewClone);

        // Nothing selected
        if (focusedTile == null)
            return;

        // Clone the tile's model
        previewClone = Instantiate(focusedTile, previewContainer);

        // Strip gameplay logic + physics
        StripNonVisualComponents(previewClone);

        // Reset transform inside preview container
        previewClone.transform.localPosition = Vector3.zero;
        previewClone.transform.localRotation = Quaternion.identity;
        previewClone.transform.localScale = Vector3.one;

        SetLayerRecursively(previewClone, LayerMask.NameToLayer("PlantRenderTexture"));
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Removes everything except renderers
    private void StripNonVisualComponents(GameObject obj)
    {
        foreach (var mono in obj.GetComponentsInChildren<MonoBehaviour>())
            Destroy(mono);

        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        foreach (var col in obj.GetComponentsInChildren<Collider>())
            Destroy(col);
    }
}
