using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Option 1: Fully face the camera (like UI)
        transform.forward = cam.transform.forward;

        // Option 2 (alternative): Look directly at camera
        // transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
        //                  cam.transform.rotation * Vector3.up);
    }
}
