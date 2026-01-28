using UnityEngine;
using UnityEngine.UI;

// どこでも好きなRawImageにくっつけるだけで、バッジを表示する便利スクリプト！
public class AnywhereBadge : MonoBehaviour
{
    private RawImage myImage;

    void Awake()
    {
        // 自分のついているRawImageコンポーネントを自動で確保！
        myImage = GetComponent<RawImage>();
    }

    void OnEnable()
    {
        // 画面に表示されるたびに更新！
        UpdateMyBadge();
    }

    public void UpdateMyBadge()
    {
        // マネージャーがいなければ何もしない（エラー防止）
        if (BadgeManager.Instance == null || myImage == null) return;

        // 1. 今の最強バッジIDを聞く
        string bestId = BadgeManager.Instance.GetCurrentBestBadgeId();

        // 2. IDがなければ画像を消す
        if (string.IsNullOrEmpty(bestId))
        {
            myImage.enabled = false;
            return;
        }

        // 3. 画像を読み込んで表示！
        Texture tex = Resources.Load<Texture>("BadgeUI/" + bestId);
        if (tex != null)
        {
            myImage.enabled = true;
            myImage.texture = tex;
        }
        else
        {
            myImage.enabled = false;
        }
    }
}