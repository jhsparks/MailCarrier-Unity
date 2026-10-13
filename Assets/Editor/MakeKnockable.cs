using UnityEditor;
using UnityEngine;

public static class MakeKnockable
{
    [MenuItem("Tools/Make Selected Knockable")]
    static void Apply()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            KnockableProp prop = go.GetComponent<KnockableProp>();
            if (prop == null) prop = Undo.AddComponent<KnockableProp>(go);
            prop.Setup();
            EditorUtility.SetDirty(go);
        }
    }
}
