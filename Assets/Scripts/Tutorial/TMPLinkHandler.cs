using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text textMeshPro;
    private Canvas canvas;

    void Awake()
    {
        textMeshPro = GetComponent<TMP_Text>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (textMeshPro == null) return;

        // Canvas の RenderMode に応じてカメラを取得
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, cam);

        if (linkIndex == -1)
        {
            Debug.Log("[TMPLinkHandler] リンク検出なし");
            return;
        }

        string linkID = textMeshPro.textInfo.linkInfo[linkIndex].GetLinkID();
        Debug.Log($"[TMPLinkHandler] リンク押下: {linkID}");

        if (linkID == "terms")
            Application.OpenURL("https://jagged-wombat-9c5.notion.site/YURUFU-35184120f12f80cba92bd4f91f2bdeae");
        else if (linkID == "privacy")
            Application.OpenURL("https://jagged-wombat-9c5.notion.site/YURUFUWorld-35184120f12f80b4b2b7f16a179c5785");
    }
}