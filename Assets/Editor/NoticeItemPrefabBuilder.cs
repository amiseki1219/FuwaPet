#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility: Tools > YURUFU > Build NoticeItem Prefab
/// 参考画像レイアウト:
///   [サムネイル 90x90] | [カテゴリタグ]
///                       | [タイトル（Bold）]
///                       | [本文（省略）]
///                       |              [日付]
/// </summary>
public static class NoticeItemPrefabBuilder
{
    private const string PrefabPath      = "Assets/Prefabs/Notice/NoticeItem.prefab";
    private const string BaseSprPath     = "Assets/UI/MainUI/NoticeUI/NoticeeItem Base.png";
    private const string ThumbBGSprPath  = "Assets/UI/MainUI/NoticeUI/Image.png";
    private const string BadgeSprPath    = "Assets/UI/MainUI/NoticeUI/Newバッジ.png";
    private const string NoticeTagPath   = "Assets/UI/MainUI/NoticeUI/お知らせバッジ.png";
    private const string CampaignTagPath = "Assets/UI/MainUI/NoticeUI/キャンペーンバッジ.png";
    private const string FontGuid        = "46eb132de9f75408eb3685c8137cac7d";

    // サムネイル左端の余白
    private const float ThumbX      = 8f;
    private const float ThumbSize   = 90f;
    // コンテンツ開始 X（サムネイル右端 + ギャップ）
    private const float ContentX    = ThumbX + ThumbSize + 8f; // = 106
    private const float RightMargin = 8f;

    [MenuItem("Tools/YURUFU/Build NoticeItem Prefab")]
    public static void BuildNoticeItemPrefab()
    {
        var baseSprite    = Load<Sprite>(BaseSprPath);
        var thumbBGSprite = Load<Sprite>(ThumbBGSprPath);
        var badgeSprite   = Load<Sprite>(BadgeSprPath);
        var noticeSprite  = Load<Sprite>(NoticeTagPath);
        var campaignSprite = Load<Sprite>(CampaignTagPath);
        var fontAsset     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                                AssetDatabase.GUIDToAssetPath(FontGuid));

        // ── Root ─────────────────────────────────────────────────────────
        var root = new GameObject("NoticeItem");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot     = new Vector2(0.5f, 1f);
        rootRect.sizeDelta = new Vector2(0f, 120f);

        // ── Background（全体ストレッチ）──────────────────────────────────
        var bgGO  = CreateGO("Background", root.transform);
        StretchFull(bgGO.GetComponent<RectTransform>());
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = baseSprite;

        // ── ThumbnailImageBG（サムネイル背景）────────────────────────────
        var thumbBGGO   = CreateGO("ThumbnailImageBG", root.transform);
        var thumbBGRect = thumbBGGO.GetComponent<RectTransform>();
        SetRect(thumbBGRect, anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f), pos: new Vector2(ThumbX, 0f),
                size: new Vector2(ThumbSize, ThumbSize));
        var thumbBGImg = thumbBGGO.AddComponent<Image>();
        thumbBGImg.sprite = thumbBGSprite;

        // ── ThumbnailImage（実際のサムネイル）────────────────────────────
        var thumbGO   = CreateGO("ThumbnailImage", root.transform);
        var thumbRect = thumbGO.GetComponent<RectTransform>();
        SetRect(thumbRect, anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f), pos: new Vector2(ThumbX + 5f, 0f),
                size: new Vector2(ThumbSize - 10f, ThumbSize - 10f));
        var thumbImg = thumbGO.AddComponent<Image>();

        // ── NewBadge（左上角）────────────────────────────────────────────
        var badgeGO   = CreateGO("NewBadge", root.transform);
        var badgeRect = badgeGO.GetComponent<RectTransform>();
        SetRect(badgeRect, anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), pos: new Vector2(4f, -4f),
                size: new Vector2(44f, 20f));

        var badgeImgGO = CreateGO("BadgeImage", badgeGO.transform);
        StretchFull(badgeImgGO.GetComponent<RectTransform>());
        var badgeImg = badgeImgGO.AddComponent<Image>();
        badgeImg.sprite = badgeSprite;

        // ── NoticeTag（お知らせ）─────────────────────────────────────────
        var noticeTagGO   = CreateGO("NoticeTag", root.transform);
        var noticeTagRect = noticeTagGO.GetComponent<RectTransform>();
        SetRect(noticeTagRect, anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), pos: new Vector2(ContentX, -8f),
                size: new Vector2(80f, 24f));
        var noticeTagImg = noticeTagGO.AddComponent<Image>();
        noticeTagImg.sprite = noticeSprite;

        // ── CampaignTag（キャンペーン）────────────────────────────────────
        var campaignTagGO   = CreateGO("CampaignTag", root.transform);
        var campaignTagRect = campaignTagGO.GetComponent<RectTransform>();
        SetRect(campaignTagRect, anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), pos: new Vector2(ContentX, -8f),
                size: new Vector2(80f, 24f));
        var campaignTagImg = campaignTagGO.AddComponent<Image>();
        campaignTagImg.sprite = campaignSprite;
        campaignTagGO.SetActive(false);

        // ── TitleText（上半分・Bold）──────────────────────────────────────
        var titleGO   = CreateGO("TitleText", root.transform);
        var titleRect = titleGO.GetComponent<RectTransform>();
        SetRect(titleRect, anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(1f, 0.5f),
                pivot: new Vector2(0f, 1f), pos: new Vector2(ContentX, 4f),
                size: new Vector2(-(ContentX + RightMargin), 38f));
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text         = "タイトル";
        titleTMP.fontSize     = 14f;
        titleTMP.fontStyle    = FontStyles.Bold;
        titleTMP.color        = new Color(0.545f, 0.271f, 0.075f);
        titleTMP.overflowMode = TextOverflowModes.Ellipsis;

        // ── BodyText（下半分）────────────────────────────────────────────
        var bodyGO   = CreateGO("BodyText", root.transform);
        var bodyRect = bodyGO.GetComponent<RectTransform>();
        SetRect(bodyRect, anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0.5f),
                pivot: new Vector2(0f, 1f), pos: new Vector2(ContentX, -4f),
                size: new Vector2(-(ContentX + RightMargin), 0f));
        var bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text         = "本文";
        bodyTMP.fontSize     = 12f;
        bodyTMP.color        = new Color(0.545f, 0.271f, 0.075f);
        bodyTMP.overflowMode = TextOverflowModes.Ellipsis;

        // ── DateText（右下）──────────────────────────────────────────────
        var dateGO   = CreateGO("DateText", root.transform);
        var dateRect = dateGO.GetComponent<RectTransform>();
        SetRect(dateRect, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f), pos: new Vector2(-RightMargin, 8f),
                size: new Vector2(120f, 20f));
        var dateTMP = dateGO.AddComponent<TextMeshProUGUI>();
        dateTMP.text      = "2026/05/01";
        dateTMP.fontSize  = 11f;
        dateTMP.color     = new Color(0.5f, 0.5f, 0.5f);
        dateTMP.alignment = TextAlignmentOptions.Right;

        // ── NoticeItem コンポーネント + フィールド紐付け ──────────────────
        var noticeItem = root.AddComponent<NoticeItem>();
        var so = new SerializedObject(noticeItem);
        so.FindProperty("thumbnailImage").objectReferenceValue   = thumbImg;
        so.FindProperty("newBadge").objectReferenceValue         = badgeGO;
        so.FindProperty("categoryNotice").objectReferenceValue   = noticeTagGO;
        so.FindProperty("categoryCampaign").objectReferenceValue = campaignTagGO;
        so.FindProperty("titleText").objectReferenceValue        = titleTMP;
        so.FindProperty("bodyText").objectReferenceValue         = bodyTMP;
        so.FindProperty("dateText").objectReferenceValue         = dateTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        // フォントを SerializedObject 経由で設定（プロパティセッタはエディタ内で例外を出す場合がある）
        if (fontAsset != null)
        {
            void SetFont(TextMeshProUGUI tmp)
            {
                var tmpSo = new SerializedObject(tmp);
                tmpSo.FindProperty("m_fontAsset").objectReferenceValue = fontAsset;
                tmpSo.ApplyModifiedPropertiesWithoutUndo();
            }
            SetFont(titleTMP);
            SetFont(bodyTMP);
            SetFont(dateTMP);
        }

        // ── プレハブ保存 ──────────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);

        if (success)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[NoticeItemPrefabBuilder] Saved: {PrefabPath}");
            EditorUtility.DisplayDialog("NoticeItem Prefab", $"プレハブを保存しました:\n{PrefabPath}", "OK");
        }
        else
        {
            Debug.LogError($"[NoticeItemPrefabBuilder] Failed to save: {PrefabPath}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);

    private static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetRect(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
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
