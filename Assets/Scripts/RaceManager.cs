using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Free-for-all local multiplayer: spawns one player + van + camera viewport per racer,
// tracks each racer's mail progress independently, and ends the race when the first one finishes.
public class RaceManager : MonoBehaviour
{
    enum Stage { Deliver, Collect, Done }

    class Racer
    {
        public int slot;
        public GameObject player;
        public CharacterController controller;
        public DrivableVan van;
        public Camera camera;
        public ThirdPersonCamera follow;
        public bool inVan;
        public int carried;
        public int delivered;
        public Stage stage = Stage.Deliver;
        public readonly HashSet<DoorTrigger3D> doors = new HashSet<DoorTrigger3D>();
        public readonly HashSet<Transform> boxes = new HashSet<Transform>();
        public float finishTime = -1f;
    }

    readonly List<Racer> racers = new List<Racer>();
    readonly List<DoorTrigger3D> targetDoors = new List<DoorTrigger3D>();
    readonly List<Transform> mailboxes = new List<Transform>();
    readonly List<GameObject> markers = new List<GameObject>();
    MailHQTrigger hq;
    bool gathered;
    bool raceOver;
    Racer winner;
    float elapsed;
    int target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
    }

    static void Ensure()
    {
        if (!MultiplayerSession.Active) return;
        if (FindFirstObjectByType<GameManager>() == null) return;
        if (FindFirstObjectByType<PlayerController3D>() == null || FindFirstObjectByType<DrivableVan>() == null) return;
        if (FindFirstObjectByType<RaceManager>() != null) return;
        new GameObject("RaceManager").AddComponent<RaceManager>();
    }

    void Start()
    {
        PlayerController3D p0 = FindFirstObjectByType<PlayerController3D>();
        DrivableVan v0 = FindFirstObjectByType<DrivableVan>();
        Camera mainCam = Camera.main;
        ThirdPersonCamera c0 = mainCam != null ? mainCam.GetComponentInParent<ThirdPersonCamera>() : null;
        if (c0 == null) c0 = FindFirstObjectByType<ThirdPersonCamera>();
        if (p0 == null || v0 == null || c0 == null) { enabled = false; return; }

        VehicleInteraction vi = FindFirstObjectByType<VehicleInteraction>();
        if (vi != null) vi.enabled = false;

        UIManager3D ui = FindFirstObjectByType<UIManager3D>();
        if (ui != null && ui.mailText != null) ui.mailText.gameObject.SetActive(false);

        Vector3 playerOffset = p0.transform.position - v0.transform.position;
        Bounds vanBounds = VanBounds(v0);
        int n = MultiplayerSession.PlayerCount;

        for (int k = 0; k < n; k++)
        {
            Racer r = new Racer { slot = k };

            GameObject pgo = k == 0 ? p0.gameObject : Instantiate(p0.gameObject);
            DrivableVan van = k == 0 ? v0 : Instantiate(v0.gameObject).GetComponent<DrivableVan>();
            GameObject cgo = k == 0 ? c0.gameObject : Instantiate(c0.gameObject);

            if (k > 0)
            {
                Vector3 vanPos = FindFreeSpot(v0.transform, vanBounds, k);
                van.transform.SetPositionAndRotation(vanPos, v0.transform.rotation);
                Rigidbody rb = van.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

                CharacterController cc = pgo.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                pgo.transform.SetPositionAndRotation(vanPos + playerOffset, p0.transform.rotation);
                if (cc != null) cc.enabled = true;

                foreach (AudioListener al in cgo.GetComponentsInChildren<AudioListener>()) Destroy(al);
                cgo.tag = "Untagged";
                cgo.name = "PlayerCamera" + (k + 1);
                pgo.name = "Player" + (k + 1);
                van.name = "Van" + (k + 1);
            }

            r.player = pgo;
            r.controller = pgo.GetComponent<CharacterController>();
            r.van = van;
            r.follow = cgo.GetComponent<ThirdPersonCamera>();
            r.camera = cgo.GetComponentInChildren<Camera>();
            r.camera.rect = ViewportFor(k, n);

            PlayerController3D pc = pgo.GetComponent<PlayerController3D>();
            pc.slot = k;
            pc.cameraTransform = r.camera.transform;
            van.slot = k;

            if (k > 0)
            {
                float yaw = pgo.transform.eulerAngles.y;
                r.camera.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                r.camera.transform.position = pgo.transform.position + Quaternion.Euler(0f, yaw, 0f) * r.follow.offset;
            }
            r.follow.SetTarget(pgo.transform);

            PlayerAndVanDecorator.DecoratePlayer(pc, MultiplayerSession.Colors[k], MultiplayerSession.Avatars[k]);
            PlayerAndVanDecorator.PaintVan(van, MultiplayerSession.Colors[k], Color.white);

            racers.Add(r);
        }
    }

    static Rect ViewportFor(int k, int n)
    {
        if (n == 2) return new Rect(k * 0.5f, 0f, 0.5f, 1f);
        if (n == 3)
        {
            if (k == 0) return new Rect(0f, 0.5f, 0.5f, 0.5f);
            if (k == 1) return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            return new Rect(0f, 0f, 1f, 0.5f);
        }
        return new Rect((k % 2) * 0.5f, k < 2 ? 0.5f : 0f, 0.5f, 0.5f);
    }

    static Bounds VanBounds(DrivableVan van)
    {
        bool first = true;
        Bounds b = new Bounds(van.transform.position, Vector3.one);
        foreach (Collider c in van.GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger || c is WheelCollider) continue;
            if (first) { b = c.bounds; first = false; }
            else b.Encapsulate(c.bounds);
        }
        return b;
    }

    Vector3 FindFreeSpot(Transform van, Bounds b, int k)
    {
        float side = b.size.x + 1.5f;
        float len = b.size.z + 1.5f;
        Vector3[] dirs =
        {
            van.right * side * k, -van.right * side * k,
            van.forward * len * k, -van.forward * len * k,
            van.right * side * k * 2f, -van.right * side * k * 2f,
        };
        Vector3 half = b.extents * 0.8f;
        foreach (Vector3 d in dirs)
        {
            Vector3 center = b.center + d + Vector3.up * 0.2f;
            if (!Physics.CheckBox(center, half, van.rotation, ~0, QueryTriggerInteraction.Ignore))
                return van.position + d;
        }
        return van.position + dirs[0];
    }

    bool Gather()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        target = gm.dailyTargetCount;
        hq = FindFirstObjectByType<MailHQTrigger>();
        foreach (DoorTrigger3D d in FindObjectsByType<DoorTrigger3D>(FindObjectsSortMode.None))
            if (d.IsTarget) targetDoors.Add(d);
        target = Mathf.Min(target, targetDoors.Count);
        gathered = true;
        return true;
    }

    void EnsureMailboxes()
    {
        if (mailboxes.Count > 0) return;
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("sptp_mail_box")) continue;
            if (t.parent != null && t.parent.name.StartsWith("sptp_mail_box")) continue;
            mailboxes.Add(t);

            GameObject go = new GameObject("MailboxMarker");
            go.transform.position = t.position + Vector3.up * 2f;
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
            markers.Add(go);
        }
    }

    Bounds ActorBounds(Racer r)
    {
        if (r.inVan) return VanBounds(r.van);
        if (r.controller != null) return r.controller.bounds;
        return new Bounds(r.player.transform.position, Vector3.one);
    }

    Vector3 ActorPosition(Racer r)
    {
        return r.inVan ? r.van.transform.position : r.player.transform.position;
    }

    static bool Touches(Transform zone, Bounds actor)
    {
        Collider[] cols = zone.GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return actor.SqrDistance(zone.position) < 9f;
        foreach (Collider c in cols)
            if (c.bounds.Intersects(actor)) return true;
        return false;
    }

    void Update()
    {
        if (!gathered && !Gather()) return;
        if (raceOver || Time.timeScale <= 0f) return;

        elapsed += Time.deltaTime;

        foreach (Racer r in racers)
        {
            if (r.stage == Stage.Done) continue;
            if (PlayerInput.InteractDown(r.slot))
            {
                if (r.inVan) ExitVan(r);
                else if (VanBounds(r.van).SqrDistance(r.player.transform.position) <= 9f) EnterVan(r);
            }
            if (PlayerInput.ActionDown(r.slot)) HandleAction(r);
            if (raceOver) break;
        }
    }

    void EnterVan(Racer r)
    {
        r.inVan = true;
        r.player.SetActive(false);
        r.van.isBeingDriven = true;
        r.follow.SetTarget(r.van.transform);
    }

    void ExitVan(Racer r)
    {
        r.inVan = false;
        r.van.isBeingDriven = false;
        Transform v = r.van.transform;
        Vector3 spot = v.position + v.right * 2.5f + Vector3.up * 0.5f;
        Bounds vb = VanBounds(r.van);
        spot = v.position + v.right * (vb.extents.x + 1.5f) + Vector3.up * 0.5f;
        if (Physics.CheckSphere(spot + Vector3.up * 0.5f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
            spot = v.position - v.right * (vb.extents.x + 1.5f) + Vector3.up * 0.5f;

        if (r.controller != null) r.controller.enabled = false;
        r.player.transform.position = spot;
        r.player.SetActive(true);
        if (r.controller != null) r.controller.enabled = true;
        r.follow.SetTarget(r.player.transform);
    }

    void HandleAction(Racer r)
    {
        Bounds ab = ActorBounds(r);

        if (hq != null && Touches(hq.transform, ab))
        {
            if (r.stage == Stage.Deliver)
            {
                int remaining = target - r.delivered;
                if (r.carried < remaining)
                {
                    r.carried = remaining;
                    AudioManager.PlayPickupMail();
                }
            }
            else if (r.stage == Stage.Collect && r.boxes.Count >= mailboxes.Count)
            {
                Finish(r);
            }
            return;
        }

        if (r.stage == Stage.Deliver && r.carried > 0)
        {
            foreach (DoorTrigger3D d in targetDoors)
            {
                if (r.doors.Contains(d) || !Touches(d.transform, ab)) continue;
                r.doors.Add(d);
                r.carried--;
                r.delivered++;
                MarkDoorIfDone(d);
                if (r.delivered >= target)
                {
                    AudioManager.PlayAllMailDelivered();
                    BeginCollect(r);
                }
                else AudioManager.PlayDoorDelivered();
                return;
            }
        }
        else if (r.stage == Stage.Collect)
        {
            Vector3 pos = ActorPosition(r);
            Transform nearest = null;
            float best = 4f;
            foreach (Transform t in mailboxes)
            {
                if (r.boxes.Contains(t)) continue;
                float dist = Vector3.Distance(pos, t.position);
                if (dist <= best) { best = dist; nearest = t; }
            }
            if (nearest == null) return;
            r.boxes.Add(nearest);
            AudioManager.PlayPickupMail();
            MarkBoxIfDone(nearest);
        }
    }

    void MarkDoorIfDone(DoorTrigger3D d)
    {
        foreach (Racer r in racers) if (!r.doors.Contains(d)) return;
        d.MarkDelivered();
    }

    void MarkBoxIfDone(Transform box)
    {
        foreach (Racer r in racers) if (!r.boxes.Contains(box)) return;
        int i = mailboxes.IndexOf(box);
        if (i >= 0 && markers[i] != null) Destroy(markers[i]);
    }

    void BeginCollect(Racer r)
    {
        EnsureMailboxes();
        if (mailboxes.Count == 0) { Finish(r); return; }
        r.stage = Stage.Collect;
    }

    void Finish(Racer r)
    {
        r.stage = Stage.Done;
        r.finishTime = elapsed;
        AudioManager.PlayAllMailDelivered();
        if (raceOver) return;
        raceOver = true;
        winner = r;
        Time.timeScale = 0f;
    }

    static string Format(float t)
    {
        int m = (int)(t / 60f);
        return m.ToString("00") + ":" + (t - m * 60f).ToString("00.0");
    }

    static string Label(int i)
    {
        return "Player " + (i + 1) + " (" + MultiplayerSession.ColorNames[i] + ")";
    }

    string Objective(Racer r)
    {
        if (r.stage == Stage.Done) return "Finished!";
        if (r.stage == Stage.Collect)
        {
            if (r.boxes.Count >= mailboxes.Count) return "Return to the Post Office!";
            return "Collect mailboxes: " + r.boxes.Count + "/" + mailboxes.Count;
        }
        if (r.carried == 0) return "Pick up mail at the Post Office";
        return "Deliver mail: " + r.delivered + "/" + target + " (carrying " + r.carried + ")";
    }

    static void Fill(Rect rect, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (racers.Count == 0) return;

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        small.normal.textColor = Color.white;

        foreach (Racer r in racers)
        {
            Rect vp = r.camera.rect;
            Rect px = new Rect(vp.x * Screen.width, (1f - vp.y - vp.height) * Screen.height, vp.width * Screen.width, vp.height * Screen.height);
            Color col = MultiplayerSession.Colors[r.slot];

            Fill(new Rect(px.x, px.y, px.width, 3f), col);
            Fill(new Rect(px.x, px.yMax - 3f, px.width, 3f), col);
            Fill(new Rect(px.x, px.y, 3f, px.height), col);
            Fill(new Rect(px.xMax - 3f, px.y, 3f, px.height), col);

            Fill(new Rect(px.x + 8f, px.y + 8f, Mathf.Min(430f, px.width - 16f), 70f), new Color(0f, 0f, 0f, 0.5f));
            title.normal.textColor = col;
            GUI.Label(new Rect(px.x + 14f, px.y + 10f, 420f, 26f), Label(r.slot) + "   " + Format(r.finishTime >= 0f ? r.finishTime : elapsed), title);
            GUI.Label(new Rect(px.x + 14f, px.y + 36f, 420f, 20f), Objective(r), small);
            GUI.Label(new Rect(px.x + 14f, px.y + 55f, 420f, 20f), PlayerInput.Describe(r.slot), small);
        }

        if (!raceOver) return;

        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
        float w = 520f, h = 200f + racers.Count * 34f;
        Rect box = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        Fill(box, new Color(0f, 0f, 0f, 0.85f));

        label.normal.textColor = MultiplayerSession.Colors[winner.slot];
        GUI.Label(new Rect(box.x, box.y + 12f, w, 40f), Label(winner.slot) + " wins!  " + Format(winner.finishTime), label);

        List<Racer> order = new List<Racer>(racers);
        order.Sort((a, b) => Progress(b).CompareTo(Progress(a)));
        float y = box.y + 62f;
        for (int i = 0; i < order.Count; i++)
        {
            Racer r = order[i];
            label.normal.textColor = MultiplayerSession.Colors[r.slot];
            string status = r.finishTime >= 0f ? Format(r.finishTime) : "did not finish (" + r.delivered + "/" + target + " delivered)";
            GUI.Label(new Rect(box.x, y, w, 32f), (i + 1) + ". " + Label(r.slot) + "  " + status, label);
            y += 34f;
        }

        if (GUI.Button(new Rect(box.x + 30f, box.yMax - 60f, 220f, 44f), "Play Again", button))
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (GUI.Button(new Rect(box.xMax - 250f, box.yMax - 60f, 220f, 44f), "Main Menu", button))
        {
            MultiplayerSession.Reset();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    float Progress(Racer r)
    {
        if (r.finishTime >= 0f) return 1000000f - r.finishTime;
        return r.delivered + r.boxes.Count;
    }
}
