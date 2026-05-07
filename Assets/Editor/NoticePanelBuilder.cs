#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility: Tools > YURUFU > Build NoticePanel in Main
///
/// Creates the full NoticePanel UI hierarchy inside the Canvas of Main.unity,
/// attaches NoticeManager, wires all SerializeFields, sets the panel inactive,
/// and saves the scene.
///
/// Hierarchy:
///   Canvas
///   └── NoticePanel            (Image, SetActive=false)
///       ├── Background         (Image, semi-transparent black overlay)
///       └── PanelContent       (Image, centered 700×900 panel)
///           ├── Header         (RectTransform)
///           │   ├── TitleText  (TextMeshProUGUI, "お知らせ")
///           │   └── CloseButton (Button + Image)
///           │       └── CloseText (TextMeshProUGUI, "×")
///           └── ScrollView     (ScrollRect + Image)
///               └── Viewport   (Image + Mask)
///                   └── Content (VerticalLayoutGroup)
/// </summary>
public static class NoticePanelBuilder
{
    private const string ScenePath    = "Assets/Scenes/Main.unity";
    private const string PrefabPath   = "Assets/Prefabs/Notice/NoticeItem.prefab";

    [MenuItem("Tools/YURUFU/Build NoticePanel in Main")]
    public static void BuildNoticePanel()
    {
        // ── 1. Load Main.unity ─────────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── 2. Find Canvas ─────────────────────────────────────────────────
        Canvas canvas = null;
        foreach (var rootGO in scene.GetRootGameObjects())
        {
            canvas = rootGO.GetComponentInChildren<Canvas>(true);
            if (canvas != null) break;
        }

        if (canvas == null)
        {
            Debug.LogError("[NoticePanelBuilder] Canvas が Main.unity に見つかりませんでした。");
            return;
        }

        Debug.Log($"[NoticePanelBuilder] Canvas found: {canvas.gameObject.name}");

        // ── 3. Guard: remove stale NoticePanel if it already exists ────────
        var existing = canvas.transform.Find("NoticePanel");
        if (existing != null)
        {
            Debug.Log("[NoticePanelBuilder] 既存の NoticePanel を削除して再生成します。");
            Object.DestroyImmediate(existing.gameObject);
        }

        // ── 4. NoticePanel ─────────────────────────────────────────────────
        var noticePanel = CreateUIElement("NoticePanel", canvas.transform);
        StretchFull(noticePanel.GetComponent<RectTransform>());
        var noticePanelImage = noticePanel.AddComponent<Image>();
        noticePanelImage.color = new Color(0f, 0f, 0f, 0f); // fully transparent root
        noticePanel.SetActive(false);

        // ── 5. Background ──────────────────────────────────────────────────
        var background = CreateUIElement("Background", noticePanel.transform);
        StretchFull(background.GetComponent<RectTransform>());
        var bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);

        // ── 6. PanelContent ────────────────────────────────────────────────
        var panelContent = CreateUIElement("PanelContent", noticePanel.transform);
        var panelContentRect = panelContent.GetComponent<RectTransform>();
        panelContentRect.anchorMin        = new Vector2(0.5f, 0.5f);
        panelContentRect.anchorMax        = new Vector2(0.5f, 0.5f);
        panelContentRect.pivot            = new Vector2(0.5f, 0.5f);
        panelContentRect.anchoredPosition = Vector2.zero;
        panelContentRect.sizeDelta        = new Vector2(700f, 900f);
        var panelContentImage = panelContent.AddComponent<Image>();
        panelContentImage.color = new Color(1f, 1f, 1f, 1f);

        // ── 7. Header ──────────────────────────────────────────────────────
        var header = CreateUIElement("Header", panelContent.transform);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin        = new Vector2(0f, 1f);
        headerRect.anchorMax        = new Vector2(1f, 1f);
        headerRect.pivot            = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta        = new Vector2(0f, 80f);

        // ── 8. TitleText ───────────────────────────────────────────────────
        var titleText = CreateTMPChild(header.transform, "TitleText", "お知らせ");
        var titleTextRect = titleText.GetComponent<RectTransform>();
        titleTextRect.anchorMin        = new Vector2(0f, 0f);
        titleTextRect.anchorMax        = new Vector2(1f, 1f);
        titleTextRect.pivot            = new Vector2(0.5f, 0.5f);
        titleTextRect.anchoredPosition = Vector2.zero;
        titleTextRect.sizeDelta        = new Vector2(0f, 0f);
        titleText.fontSize             = 28f;
        titleText.fontStyle            = FontStyles.Bold;
        titleText.alignment            = TextAlignmentOptions.Center;
        titleText.color                = Color.black;

        // ── 9. CloseButton ─────────────────────────────────────────────────
        var closeButtonGO = CreateUIElement("CloseButton", header.transform);
        var closeButtonRect = closeButtonGO.GetComponent<RectTransform>();
        closeButtonRect.anchorMin        = new Vector2(1f, 0.5f);
        closeButtonRect.anchorMax        = new Vector2(1f, 0.5f);
        closeButtonRect.pivot            = new Vector2(1f, 0.5f);
        closeButtonRect.anchoredPosition = new Vector2(-16f, 0f);
        closeButtonRect.sizeDelta        = new Vector2(48f, 48f);
        var closeButtonImage  = closeButtonGO.AddComponent<Image>();
        closeButtonImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        var closeButton = closeButtonGO.AddComponent<Button>();

        // ── 10. CloseText ──────────────────────────────────────────────────
        var closeText = CreateTMPChild(closeButtonGO.transform, "CloseText", "×");
        var closeTextRect = closeText.GetComponent<RectTransform>();
        StretchFull(closeTextRect);
        closeText.fontSize  = 28f;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color     = Color.black;

        // ── 11. ScrollView ─────────────────────────────────────────────────
        var scrollViewGO = CreateUIElement("ScrollView", panelContent.transform);
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin        = new Vector2(0f, 0f);
        scrollViewRect.anchorMax        = new Vector2(1f, 1f);
        scrollViewRect.pivot            = new Vector2(0.5f, 0.5f);
        scrollViewRect.offsetMin        = new Vector2(0f, 0f);
        scrollViewRect.offsetMax        = new Vector2(0f, -80f);
        var scrollViewImage = scrollViewGO.AddComponent<Image>();
        scrollViewImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;

        // ── 12. Viewport ───────────────────────────────────────────────────
        var viewport = CreateUIElement("Viewport", scrollViewGO.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewport.AddComponent<Image>(); // Mask needs an Image
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // ── 13. Content ────────────────────────────────────────────────────
        var content = CreateUIElement("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin        = new Vector2(0f, 1f);
        contentRect.anchorMax        = new Vector2(1f, 1f);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta        = new Vector2(0f, 0f);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing              = 8f;
        vlg.padding              = new RectOffset(16, 16, 16, 16);

        // Also add ContentSizeFitter so the list grows with items
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ── 14. Wire ScrollRect references ─────────────────────────────────
        scrollRect.viewport = viewportRect;
        scrollRect.content  = contentRect;

        // ── 15. Attach NoticeManager and wire SerializeFields ───────────────
        var noticeManager = noticePanel.AddComponent<NoticeManager>();
        var noticePanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        var so = new SerializedObject(noticeManager);
        so.FindProperty("noticePanel").objectReferenceValue      = noticePanel;
        so.FindProperty("closeButton").objectReferenceValue      = closeButton;
        so.FindProperty("scrollContent").objectReferenceValue    = content.transform;
        so.FindProperty("noticeItemPrefab").objectReferenceValue = noticePanelPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── 16. Save scene ──────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[NoticePanelBuilder] NoticePanel を Main.unity に作成し、シーンを保存しました。");
        EditorUtility.DisplayDialog(
            "NoticePanelBuilder",
            "NoticePanel を Main.unity の Canvas に作成しました。\n" +
            "パネルは SetActive=false（非表示）の状態です。\n\n" +
            "作成した GameObject:\n" +
            "  Canvas/NoticePanel\n" +
            "  Canvas/NoticePanel/Background\n" +
            "  Canvas/NoticePanel/PanelContent\n" +
            "  Canvas/NoticePanel/PanelContent/Header\n" +
            "  Canvas/NoticePanel/PanelContent/Header/TitleText\n" +
            "  Canvas/NoticePanel/PanelContent/Header/CloseButton\n" +
            "  Canvas/NoticePanel/PanelContent/Header/CloseButton/CloseText\n" +
            "  Canvas/NoticePanel/PanelContent/ScrollView\n" +
            "  Canvas/NoticePanel/PanelContent/ScrollView/Viewport\n" +
            "  Canvas/NoticePanel/PanelContent/ScrollView/Viewport/Content",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static TextMeshProUGUI CreateTMPChild(Transform parent, string name, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
    }
}
#endif
