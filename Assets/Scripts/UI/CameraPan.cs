using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("Pan Limits")]
    public Vector2 panLimitX = new Vector2(-10f, 10f);
    public Vector2 panLimitZ = new Vector2(-10f, 10f);

    public float panSpeed = 1f;

    private Camera cam;
    private bool isPanning = false;
    private Vector3 dragOriginWorld;
    private Vector3 startPos;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) Debug.LogError("CameraPan: No camera found!");

        // Store starting position as center
        startPos = transform.position;
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    void HandlePan()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseScreenPos = mouse.position.ReadValue();

        // Start panning
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isPanning = true;
            dragOriginWorld = MousePositionToXZPlane(mouseScreenPos);
        }

        // Stop panning
        if (mouse.rightButton.wasReleasedThisFrame)
        {
            isPanning = false;
        }

        // Panning
        if (isPanning)
        {
            Vector3 currentMouseWorld = MousePositionToXZPlane(mouseScreenPos);

            // Delta in world space
            Vector3 delta = dragOriginWorld - currentMouseWorld;

            Vector3 newPos = transform.position + delta * panSpeed;

            // Clamp relative to startPos
            float minXWorld = startPos.x + panLimitX.x;
            float maxXWorld = startPos.x + panLimitX.y;
            float minZWorld = startPos.z + panLimitZ.x;
            float maxZWorld = startPos.z + panLimitZ.y;

            newPos.x = Mathf.Clamp(newPos.x, minXWorld, maxXWorld);
            newPos.z = Mathf.Clamp(newPos.z, minZWorld, maxZWorld);

            transform.position = newPos;

            // Update drag origin for next frame
            dragOriginWorld = MousePositionToXZPlane(mouseScreenPos);
        }
    }

    void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// Converts a screen position to the XZ plane (y = 0)
    /// </summary>
    Vector3 MousePositionToXZPlane(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane xzPlane = new Plane(Vector3.up, Vector3.zero);

        if (xzPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}
