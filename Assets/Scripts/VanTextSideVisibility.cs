using UnityEngine;

// Shows the van text only when the camera is on the side the text faces (text faces inward, so outward is -forward).
public class VanTextSideVisibility : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || meshRenderer == null) return;
        Vector3 outward = -transform.forward;
        meshRenderer.enabled = Vector3.Dot(cam.transform.position - transform.position, outward) > 0f;
    }
}
