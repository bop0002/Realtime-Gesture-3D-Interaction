using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrabbableSpawner))]
public class GrabbableSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (GrabbableSpawner)target;

        EditorGUILayout.Space();

        // Chỉ cho bấm lúc đang Play — spawn dùng Random + tạo Rigidbody, chạy ở edit mode sẽ rác scene.
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Spawn Object (Entries + AutoFill)", GUILayout.Height(30)))
            {
                spawner.SpawnBatch();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Nút Spawn chỉ hoạt động khi đang Play.", MessageType.Info);
        }
    }
}
