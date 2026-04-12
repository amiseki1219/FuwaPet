using UnityEngine;
using UnityEngine.UI;

public class CollectionFrameItem : MonoBehaviour
{
    // ★変数名はフレーム用に変えたけど、役割は全く同じだお！
    public string myFrameId;      // ID (Reference: myIconId)
    public Image characterImage;  // 暗くする対象の画像 (Reference: characterImage)
    public GameObject lockIcon;   // 鍵アイコン
    public GameObject mySelectionFrame; // 黄色い枠

    public bool isDefaultIcon;

    // ★成功している Setup ロジックをそのまま採用！
    public void Setup(bool isOwned)
    {
        // インスペクターのセット忘れ防止ガード
        if (characterImage == null) return;

        bool canUse = isDefaultIcon || isOwned;
        if (canUse)
        {
            // 持っているなら明るく、鍵を消す
            characterImage.color = Color.white;
            if (lockIcon != null) lockIcon.SetActive(false);
        }
        else
        {
            // ★ここ大事！持っていないなら暗く(0.3)、鍵を出す
            characterImage.color = new Color(0.3f, 0.3f, 0.3f);
            if (lockIcon != null) lockIcon.SetActive(true);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (mySelectionFrame != null) mySelectionFrame.SetActive(isSelected);
    }

    public void OnClick()
    {
        Debug.Log($"フレームボタン押したお！ ID: {myFrameId}, ロック状態: {(lockIcon != null ? lockIcon.activeSelf.ToString() : "null")}, デフォルト設定: {isDefaultIcon}");

        // ★成功しているクリック制限ロジックをそのまま採用！
        if (isDefaultIcon || (lockIcon != null && !lockIcon.activeSelf))
        {
            // ★ここだけ変更！フレームのマネージャーを呼ぶお！
            if (FrameCollectionManager.Instance != null)
            {
                FrameCollectionManager.Instance.OnSelectFrame(myFrameId);
            }
        }
        else
        {
            Debug.LogWarning($"{myFrameId} はロックされているお！🗝️");
        }
    }
}