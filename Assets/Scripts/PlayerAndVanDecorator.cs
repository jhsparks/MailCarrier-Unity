using UnityEngine;

// Adds a black cross-body mail satchel to the player and a white "POST OFFICE" paint job to the van at scene load.
public static class PlayerAndVanDecorator
{
    // Palette color used for the van body in the Downtown Cars texture.
    static readonly Color32 BodyColor = new Color32(79, 86, 90, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Decorate()
    {
        foreach (PlayerController3D player in Object.FindObjectsByType<PlayerController3D>(FindObjectsSortMode.None))
        {
            AddSatchel(player);
            if (player.GetComponent<PushRigidbodies>() == null) player.gameObject.AddComponent<PushRigidbodies>();
        }

        foreach (DrivableVan van in Object.FindObjectsByType<DrivableVan>(FindObjectsSortMode.None))
            PaintVan(van);

        AddHydrantColliders();
    }

    static void AddHydrantColliders()
    {
        int count = 0;
        foreach (Transform tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!tr.name.StartsWith("sptp_fire_hydrant", System.StringComparison.OrdinalIgnoreCase)) continue;
            Renderer r = tr.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            foreach (Collider old in tr.GetComponents<Collider>()) Object.DestroyImmediate(old);

            Bounds b = r.bounds;
            BoxCollider box = tr.gameObject.AddComponent<BoxCollider>();
            box.center = tr.InverseTransformPoint(b.center);
            Vector3 s = tr.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
            count++;
            Rigidbody rb = tr.gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = tr.gameObject.AddComponent<Rigidbody>();
            rb.mass = 15f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        Debug.Log("[PlayerAndVanDecorator] Made " + count + " fire hydrants knockable.");
    }

    static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
        return m;
    }

    static GameObject MakeCube(string name, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void AddSatchel(PlayerController3D player)
    {
        Animator animator = player.animator != null ? player.animator : player.GetComponentInChildren<Animator>();
        Transform chest = null, hips = null;
        if (animator != null && animator.isHuman)
        {
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        }
        if (chest == null) chest = player.transform;
        if (hips == null) hips = chest;

        Transform root = player.transform;
        Material black = MakeMaterial(new Color(0.04f, 0.04f, 0.04f));

        // Bag rests on the right hip; strap runs from the left shoulder across the chest to the bag.
        Vector3 bagPos = hips.position - root.up * 0.22f + root.right * 0.22f;
        Vector3 shoulder = chest.position + root.up * 0.2f - root.right * 0.12f + root.forward * 0.12f;
        Vector3 strapEnd = bagPos + root.up * 0.12f + root.forward * 0.04f;

        GameObject bag = MakeCube("MailSatchel", black);
        bag.transform.SetPositionAndRotation(bagPos, root.rotation);
        bag.transform.localScale = new Vector3(0.1f, 0.22f, 0.28f);
        bag.transform.SetParent(hips, true);

        GameObject flap = MakeCube("SatchelFlap", black);
        flap.transform.SetPositionAndRotation(bagPos + root.up * 0.105f + root.right * 0.01f, root.rotation);
        flap.transform.localScale = new Vector3(0.11f, 0.04f, 0.3f);
        flap.transform.SetParent(hips, true);

        Vector3 dir = strapEnd - shoulder;
        GameObject strap = MakeCube("SatchelStrap", black);
        strap.transform.position = (shoulder + strapEnd) * 0.5f;
        strap.transform.rotation = Quaternion.LookRotation(dir.normalized, root.forward);
        strap.transform.localScale = new Vector3(0.06f, 0.06f, dir.magnitude);
        strap.transform.SetParent(chest, true);
    }

    // Copies a (possibly non-readable) texture and turns the van body color white.
    static Texture2D MakeWhiteBodyTexture(Texture source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] px = copy.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            if (Mathf.Abs(px[i].r - BodyColor.r) < 8 && Mathf.Abs(px[i].g - BodyColor.g) < 8 && Mathf.Abs(px[i].b - BodyColor.b) < 8)
                px[i] = new Color32(255, 255, 255, 255);
        }
        copy.SetPixels32(px);
        copy.filterMode = FilterMode.Point;
        copy.wrapMode = TextureWrapMode.Clamp;
        copy.Apply();
        return copy;
    }

    static void PaintVan(DrivableVan van)
    {
        Renderer[] renderers = van.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Transform t = van.transform;
        bool first = true;
        Bounds local = new Bounds();
        Texture2D whiteTex = null;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 lp = t.InverseTransformPoint(c);
                if (first) { local = new Bounds(lp, Vector3.zero); first = false; }
                else local.Encapsulate(lp);
            }

            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                string prop = mats[i].HasProperty("_BaseMap") ? "_BaseMap" : (mats[i].HasProperty("_MainTex") ? "_MainTex" : null);
                if (prop == null) continue;
                Texture tex = mats[i].GetTexture(prop);
                if (tex == null) continue;
                if (whiteTex == null) whiteTex = MakeWhiteBodyTexture(tex);
                mats[i].SetTexture(prop, whiteTex);
            }
            r.materials = mats;
        }

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 64);
        Color blue = new Color(0.05f, 0.2f, 0.85f);

        bool lengthIsZ = local.size.z >= local.size.x;
        float lengthWorld = lengthIsZ ? local.size.z * Mathf.Abs(t.lossyScale.z) : local.size.x * Mathf.Abs(t.lossyScale.x);

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject go = new GameObject(side < 0 ? "PostOfficeTextLeft" : "PostOfficeTextRight");
            go.transform.SetParent(t, false);
            Vector3 pos = local.center;
            pos.y = local.center.y + local.size.y * 0.35f;
            // Shift toward the rear so the text clears the door handle.
            if (lengthIsZ) pos.z = local.center.z - local.size.z * 0.18f;
            else pos.x = local.center.x - local.size.x * 0.18f;
            Quaternion rot;
            // TextMesh reads correctly when viewed looking along its forward axis, so it faces inward.
            if (lengthIsZ)
            {
                pos.x = local.center.x + side * (local.extents.x * 0.95f);
                rot = Quaternion.Euler(0f, -side * 90f, 0f);
            }
            else
            {
                pos.z = local.center.z + side * (local.extents.z * 0.95f);
                rot = Quaternion.Euler(0f, side < 0 ? 0f : 180f, 0f);
            }
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;

            // Snap the text onto the actual van surface using the van's colliders.
            Vector3 outward = t.TransformDirection(lengthIsZ ? Vector3.right * side : Vector3.forward * side).normalized;
            Vector3 origin = t.TransformPoint(pos) + outward * 3f;
            Ray ray = new Ray(origin, -outward);
            float best = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;
            foreach (Collider col in van.GetComponentsInChildren<Collider>())
            {
                if (col.isTrigger || col is WheelCollider) continue;
                if (col.Raycast(ray, out RaycastHit hit, 10f) && hit.distance < best)
                {
                    best = hit.distance;
                    bestPoint = hit.point;
                }
            }
            if (best < float.MaxValue)
                go.transform.localPosition = t.InverseTransformPoint(bestPoint + outward * 0.01f);
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, t.lossyScale.x), 1f / Mathf.Max(0.0001f, t.lossyScale.y), 1f / Mathf.Max(0.0001f, t.lossyScale.z));

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = "POST OFFICE";
            tm.font = font;
            tm.color = blue;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.characterSize = 0.1f;
            tm.fontSize = 64;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            // Depth-tested material so the text is hidden by buildings and other objects.
            Material textMat = new Material(Shader.Find("Sprites/Default"));
            textMat.mainTexture = font.material.mainTexture;
            mr.sharedMaterial = textMat;
            go.AddComponent<VanTextSideVisibility>();

            // Fit the text to about 35% of the van length.
            Bounds tb = mr.bounds;
            float width = Mathf.Max(tb.size.x, tb.size.z);
            if (width > 0.0001f) go.transform.localScale *= (lengthWorld * 0.35f) / width;
        }
    }
}
