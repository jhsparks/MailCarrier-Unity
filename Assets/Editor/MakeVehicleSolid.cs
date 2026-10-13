using UnityEditor;
using UnityEngine;

public static class MakeVehicleSolid
{
    [MenuItem("Tools/Make Selected Vehicles Solid")]
    static void Apply()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            VehicleCollider vc = go.GetComponent<VehicleCollider>();
            if (vc == null) vc = Undo.AddComponent<VehicleCollider>(go);
            vc.Setup();
            EditorUtility.SetDirty(go);
        }
    }
}
