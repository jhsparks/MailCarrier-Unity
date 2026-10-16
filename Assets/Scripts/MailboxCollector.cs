using System.Collections.Generic;
using UnityEngine;

// Phase 2: finds every mailbox in the scene (objects named "sptp_mail_box...")
// and lets the player collect from each one by pressing Space nearby.
public class MailboxCollector : MonoBehaviour
{
    public float collectRadius = 4f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 3f;
    public float markerHeight = 2f;

    class Box
    {
        public Transform target;
        public Transform marker;
    }

    readonly List<Box> boxes = new List<Box>();
    DrivableVan van;
    GameObject player;
    Camera cam;

    public int Init()
    {
        van = FindFirstObjectByType<DrivableVan>();
        player = GameObject.FindWithTag("Player");

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!t.name.StartsWith("sptp_mail_box")) continue;
            if (t.parent != null && t.parent.name.StartsWith("sptp_mail_box")) continue;
            boxes.Add(new Box { target = t, marker = CreateMarker(t) });
        }
        return boxes.Count;
    }

    Transform CreateMarker(Transform target)
    {
        GameObject go = new GameObject("MailboxMarker");
        go.transform.SetParent(transform, false);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = "\u25BC";
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 64;
        tm.characterSize = 0.15f;
        tm.color = new Color(1f, 0.85f, 0.1f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        return go.transform;
    }

    Vector3 ActorPosition()
    {
        if (van != null && van.isBeingDriven) return van.transform.position;
        if (player != null) return player.transform.position;
        return van != null ? van.transform.position : Vector3.zero;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.phase != GameManager.Phase.Collect) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        Vector3 pos = ActorPosition();
        int nearest = -1;
        float best = collectRadius;
        for (int i = 0; i < boxes.Count; i++)
        {
            float d = Vector3.Distance(pos, boxes[i].target.position);
            if (d <= best) { best = d; nearest = i; }
        }
        if (nearest < 0) return;

        if (boxes[nearest].marker != null) Destroy(boxes[nearest].marker.gameObject);
        boxes.RemoveAt(nearest);
        gm.CollectMailbox();
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        foreach (Box b in boxes)
        {
            if (b.marker == null || b.target == null) continue;
            b.marker.position = b.target.position + Vector3.up * (markerHeight + bob);
            if (cam != null)
                b.marker.rotation = Quaternion.LookRotation(b.marker.position - cam.transform.position);
        }
    }
}
