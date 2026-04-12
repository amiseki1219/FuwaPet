using UnityEngine;
using UnityEngine.UI;

public class CollectionIconItem : MonoBehaviour
{
    public string myIconId;
    public Image characterImage;
    public GameObject lockIcon;
    public GameObject mySelectionFrame;

    public bool isDefaultIcon;

    public void Setup(bool isOwned)
    {
        bool canUse = isDefaultIcon || isOwned;
        if (canUse)
        {
            characterImage.color = Color.white;
            if (lockIcon != null) lockIcon.SetActive(false);
        }
        else
        {
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
        Debug.Log($"ボタン押したお！ ID: {myIconId}, ロック状態: {(lockIcon != null ? lockIcon.activeSelf.ToString() : "null")}, デフォルト設定: {isDefaultIcon}");

        // ロックがかかっていない、もしくはデフォルトなら反応させる
        if (isDefaultIcon || (lockIcon != null && !lockIcon.activeSelf))
        {
            MyCollectionManager.Instance.OnSelectIcon(myIconId);
        }
        else
        {
            Debug.LogWarning($"{myIconId} はロックされているお！🗝️");
        }
    }
}