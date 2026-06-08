using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform cam;
    private void Start() { cam = Camera.main != null ? Camera.main.transform : null; }
    private void LateUpdate()
    {
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }
}