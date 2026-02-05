using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text textMeshPro;

    void Awake()
    {
        textMeshPro = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. クリックされた位置のリンクIDを取得する
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1) // リンクが押されていたら
        {
            TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID();

            // 2. IDによって処理を分ける
            if (linkID == "terms")
            {
                Debug.Log("利用規約を開くお！");
                Application.OpenURL("https://example.com/terms"); // Webを開く場合
                // または、規約パネルを表示する処理を書くお！
            }
            else if (linkID == "privacy")
            {
                Debug.Log("プライバシーポリシーを開くお！");
                Application.OpenURL("https://example.com/privacy");
            }
        }
    }
}