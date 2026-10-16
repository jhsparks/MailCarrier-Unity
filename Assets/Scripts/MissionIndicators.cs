using UnityEngine;

// Spawns bobbing floating signs: one over the post office entrance (while mail
// can be picked up) and one over the van (while carrying mail, before driving).
public class MissionIndicators : MonoBehaviour
{
    class Sign
    {
        public Transform root;
        public Transform target;
        public float height;
    }

    public float bobHeight = 0.5f;
    public float bobSpeed = 3f;
    public float postOfficeHeight = 2f;
    public float vanHeight = 2.5f;
    bool vanHintDone;

    Sign postOfficeSign;
    Sign vanSign;
    Sign returnSign;
    DrivableVan van;
    Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        AutoCreate();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AutoCreate();
    }

    static void AutoCreate()
    {
        if (FindFirstObjectByType<MailHQTrigger>() == null) return;
        if (FindFirstObjectByType<MissionIndicators>() != null) return;
        new GameObject("MissionIndicators").AddComponent<MissionIndicators>();
    }

    void Start()
    {
        MailHQTrigger hq = FindFirstObjectByType<MailHQTrigger>();
        van = FindFirstObjectByType<DrivableVan>();

        if (hq != null)
        {
            postOfficeSign = CreateSign("PostOfficeSign", "PICK UP MAIL", hq.transform, new Color(1f, 0.85f, 0.1f), postOfficeHeight);
            returnSign = CreateSign("ReturnSign", "RETURN TO POST OFFICE", hq.transform, new Color(1f, 0.85f, 0.1f), postOfficeHeight);
        }
        if (van != null)
            vanSign = CreateSign("VanSign", "GET IN VAN", van.transform, new Color(0.3f, 1f, 0.4f), vanHeight);
    }

    Sign CreateSign(string name, string label, Transform target, Color color, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = label + "\n\u25BC";
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.color = color;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        return new Sign { root = go.transform, target = target, height = height };
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        bool canPickUp = gm.mailCarried < gm.dailyTargetCount - gm.mailDelivered;
        bool driving = van != null && van.isBeingDriven;

        if (gm.mailCarried == 0 && gm.mailDelivered == 0) vanHintDone = false;
        if (driving) vanHintDone = true;

        UpdateSign(postOfficeSign, canPickUp && gm.phase == GameManager.Phase.Deliver);
        UpdateSign(returnSign, gm.phase == GameManager.Phase.Collect && gm.AllMailboxesCollected);
        UpdateSign(vanSign, gm.mailCarried > 0 && !vanHintDone);
    }

    void UpdateSign(Sign sign, bool visible)
    {
        if (sign == null || sign.root == null) return;
        if (sign.target == null) { sign.root.gameObject.SetActive(false); return; }

        sign.root.gameObject.SetActive(visible);
        if (!visible) return;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        sign.root.position = sign.target.position + Vector3.up * (sign.height + bob);

        if (cam != null)
            sign.root.rotation = Quaternion.LookRotation(sign.root.position - cam.transform.position);
    }
}
