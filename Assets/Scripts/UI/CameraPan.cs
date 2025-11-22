using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Tooltip("Optional Boundaries")]
    public float minX, maxX, minZ, maxZ;

    public float panSpeed = 1f;

    private Camera cam;
    private bool isPanning = false;
    private Vector3 dragOriginWorld;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) Debug.LogError("CameraPan1to1: No camera found!");
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

            transform.position += delta * panSpeed;

            // Clamp camera position
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minX, maxX),
                transform.position.y,
                Mathf.Clamp(transform.position.z, minZ, maxZ)
            );

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
