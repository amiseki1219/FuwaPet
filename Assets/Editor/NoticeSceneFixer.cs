#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility: Tools > YURUFU > Fix Notice Scene
/// Main.unity に対して以下を修正する:
///   1. Viewport の Mask + Image → RectMask2D に置換
///   2. NoticePanel 上の null 参照 NoticeManager を削除
/// </summary>
public static class NoticeSceneFixer
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/YURUFU/Fix Notice Scene")]
    public static void FixNoticeScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── NoticePanel を探す ────────────────────────────────────────────
        GameObject noticePanel = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            noticePanel = FindChild(root.transform, "NoticePanel");
            if (noticePanel != null) break;
        }

        if (noticePanel == null)
        {
            Debug.LogError("[NoticeSceneFixer] NoticePanel が見つかりません");
            return;
        }

        // ── Fix 1: null 参照の NoticeManager を削除 ───────────────────────
        var managers = noticePanel.GetComponents<NoticeManager>();
        int removed = 0;
        foreach (var nm in managers)
        {
            var so = new SerializedObject(nm);
            if (so.FindProperty("noticePanel").objectReferenceValue == null)
            {
                Object.DestroyImmediate(nm);
                removed++;
            }
        }
        Debug.Log($"[NoticeSceneFixer] 削除した null NoticeManager: {removed}");

        // ── Fix 2: Viewport の Mask+Image → RectMask2D ───────────────────
        var viewport = noticePanel.transform.Find("ScrollView/Viewport");
        if (viewport != null)
        {
            var mask = viewport.GetComponent<Mask>();
            if (mask != null) Object.DestroyImmediate(mask);

            var img = viewport.GetComponent<Image>();
            if (img != null) Object.DestroyImmediate(img);

            var cr = viewport.GetComponent<CanvasRenderer>();
            if (cr != null) Object.DestroyImmediate(cr);

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            Debug.Log("[NoticeSceneFixer] Viewport: Mask + Image → RectMask2D に変更");
        }
        else
        {
            Debug.LogWarning("[NoticeSceneFixer] ScrollView/Viewport が見つかりません");
        }

        // ── シーン保存 ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[NoticeSceneFixer] 完了 — null NoticeManager: {removed}件削除, Viewport: Mask→RectMask2D, Main.unity 保存済み");
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        foreach (Transform child in parent)
        {
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
