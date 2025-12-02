using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera cam;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float autoZoomDuration = 1.0f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private float autoPanDuration = 1.0f;
    [SerializeField] private Vector2 panLimitX = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 panLimitZ = new Vector2(-10f, 10f);
    
    private bool isPanning = false;
    private bool isAutoPanning = false;
    private bool isAutoZooming = false;

    private Vector3 dragOriginWorld;
    private Vector3 camStartPos;

    private float minXWorld;
    private float maxXWorld;
    private float minZWorld;
    private float maxZWorld;

    // Singleton implementation
    private static CameraManager _instance;
    public static CameraManager Instance { get { return _instance; } }

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

        if (cam == null) Debug.LogError("CameraPan: No camera found!");

        // Store starting position as center
        camStartPos = cam.transform.position;

        // Clamp relative to startPos
        minXWorld = camStartPos.x + panLimitX.x;
        maxXWorld = camStartPos.x + panLimitX.y;
        minZWorld = camStartPos.z + panLimitZ.x;
        maxZWorld = camStartPos.z + panLimitZ.y;
    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        if (isAutoPanning) return;

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
            PanToPosition(cam.transform.position + delta * panSpeed);

            // Update drag origin for next frame
            dragOriginWorld = MousePositionToXZPlane(mouseScreenPos);
        }
    }

    private void HandleZoom()
    {
        if (isAutoZooming) return;

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
    private Vector3 MousePositionToXZPlane(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane xzPlane = new Plane(Vector3.up, Vector3.zero);

        if (xzPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }

    public void PanToTarget(Vector3 target)
    {
        if (isAutoPanning) return;
        StartCoroutine(AutoPan(target));
    }

    public void ZoomToTarget(float target)
    {
        if (isAutoZooming) return;
        StartCoroutine(AutoZoom(target));
    }

    private void PanToPosition(Vector3 target)
    {
        target.x = Mathf.Clamp(target.x, minXWorld, maxXWorld);
        target.z = Mathf.Clamp(target.z, minZWorld, maxZWorld);

        cam.transform.position = target;
    }

    private IEnumerator AutoPan(Vector3 targetPoint)
    {
        isPanning = false;
        isAutoPanning = true;

        Vector3 startPos = cam.transform.position;
        Vector3 toPoint = targetPoint - startPos;

        // Project that vector onto the camera's plane
        Vector3 projected = Vector3.ProjectOnPlane(toPoint, cam.transform.forward);

        // Move camera by that projected direction
        Vector3 endPos = startPos + projected;

        float t = 0;
        while (t < autoPanDuration)
        {
            t += Time.deltaTime;
            cam.transform.position = Vector3.Lerp(startPos, endPos, t / autoPanDuration);
            yield return null;
        }

        isAutoPanning = false;
    }

    private IEnumerator AutoZoom(float targetZoom)
    {
        isAutoZooming = true;

        float startZoom = cam.orthographicSize;
        float endZoom = minZoom + (targetZoom * (maxZoom - minZoom)); // Uses target as percentage

        float t = 0;
        while (t < autoZoomDuration)
        {
            t += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(startZoom, endZoom, t / autoZoomDuration);
            yield return null;
        }

        isAutoZooming = false;
    }

    public GameObject RaycastCheck(Vector2 mousePos, string tag)
    {
        Ray ray = cam.ScreenPointToRay(mousePos);

        // Draw the ray in the Scene view (red for 1 second)
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Debug.Log($"Ray hit: {hit.collider.name} at {hit.point}");

            if (hit.collider.CompareTag(tag))
            {
                return hit.collider.gameObject;
            }
        }
        return null;
    }
}
