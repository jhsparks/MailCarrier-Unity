using UnityEditor;
using UnityEngine;

public static class AlignSelectedY
{
    [MenuItem("Tools/Align Selected To Active Y")]
    static void Apply()
    {
        GameObject active = Selection.activeGameObject;
        if (active == null)
        {
            Debug.LogWarning("[AlignSelectedY] Select several objects, with the reference one selected last (active).");
            return;
        }

        float y = active.transform.position.y;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == active) continue;
            Undo.RecordObject(go.transform, "Align Y");
            Vector3 p = go.transform.position;
            p.y = y;
            go.transform.position = p;
        }
    }

    [MenuItem("Tools/Round Selected Y To Zero")]
    static void ZeroY()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Zero Y");
            Vector3 p = go.transform.position;
            p.y = 0f;
            go.transform.position = p;
        }
    }
}
