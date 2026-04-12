using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RankingEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ownerName;

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private RawImage iconRawImage;
    [SerializeField] private RawImage frameRawImage;
    [SerializeField] private GameObject badgeImage;

    public void Setup(int rank, string name, int level, string frameId, string iconId, bool hasBadge)
    {
        // 1. 名前とレベルをセット
        ownerName.text = name;
        levelText.text = "Lv." + level;

        // 2. アイコンの読み込み (Resources/Icons/フォルダの中を探す)
        if (iconRawImage != null && !string.IsNullOrEmpty(iconId))
        {
            // あみまるのフォルダ名が「Icon」ならここを "Icon/" にしてね！
            Texture loadedIcon = Resources.Load<Texture>("Icon/" + iconId);

            if (loadedIcon != null)
            {
                iconRawImage.texture = loadedIcon;
            }
            else
            {
                Debug.LogError($"<color=red>【警告】Icon/{iconId} が見つからないお！</color>");
            }
        }

        // 3. フレーム（枠）の読み込み (Resources/Frames/フォルダ)
        if (frameRawImage != null && !string.IsNullOrEmpty(frameId))
        {
            Texture loadedFrame = Resources.Load<Texture>("Frames/" + frameId);
            if (loadedFrame != null)
            {
                frameRawImage.texture = loadedFrame;
            }
        }

        // 4. バッジと順位表示の切り替え
        if (badgeImage != null) badgeImage.SetActive(hasBadge);


    }
}