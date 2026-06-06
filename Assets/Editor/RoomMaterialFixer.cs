using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class RoomMaterialFixer : EditorWindow
{
    [MenuItem("Tools/Fix Room Materials")]
    public static void FixRoomMaterials()
    {
        int fixed_count = 0;

        // 天井のみ独自色（壁より少し濃いベージュ）
        fixed_count += SetObjectMaterialColor("Ceiling_Main", new Color(0.88f, 0.82f, 0.74f, 1f));

        // ※ 壁・床・Wainscot はここでは変更しない

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RoomMaterialFixer] Done. Fixed " + fixed_count + " object(s). Ceiling_Main -> (0.88, 0.82, 0.74)");
        EditorUtility.DisplayDialog("Fix Room Materials", "完了\nCeiling_Main の色を (0.88, 0.82, 0.74) に変更しました。", "OK");
    }

    private static int SetObjectMaterialColor(string objectName, Color color)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning("[RoomMaterialFixer] Not found: " + objectName);
            return 0;
        }
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("[RoomMaterialFixer] No Renderer on: " + objectName);
            return 0;
        }
        foreach (var mat in renderer.sharedMaterials)
        {
            if (mat == null) continue;
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
        }
        Debug.Log("[RoomMaterialFixer] " + objectName + " -> (" +
            color.r.ToString("F2") + ", " + color.g.ToString("F2") + ", " + color.b.ToString("F2") + ")");
        return 1;
    }
}
