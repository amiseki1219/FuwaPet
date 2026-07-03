using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ProfileDetailPanel : MonoBehaviour
{
    [Header("表示パーツ：画像")]
    [SerializeField] private RawImage profileIcon;
    [SerializeField] private RawImage profileFrame;
    [SerializeField] private RawImage characterIcon;

    [Header("表示パーツ：テキスト")]
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private TextMeshProUGUI birthdayText;
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private TextMeshProUGUI startDateText;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private GameObject copyToast;

    private void OnEnable()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
        var data = SaveManager.Instance.Data;

        // --- 1. ユーザーアイコン（ここを修正だお！） ---
        // data.iconId がなければ data.profileImagePath を使うように戻したっぴ！
        string iconId = !string.IsNullOrEmpty(data.iconId) ? data.iconId : data.profileImagePath;

        if (!string.IsNullOrEmpty(iconId))
        {
            // Spriteとして読み込む（UnityのUI画像はこっちが確実だお）
            Sprite iconSprite = Resources.Load<Sprite>("SpecialIcon/" + iconId);
            if (iconSprite == null) iconSprite = Resources.Load<Sprite>("Icon/" + iconId);

            if (iconSprite != null && profileIcon != null)
            {
                profileIcon.enabled = true;
                profileIcon.texture = iconSprite.texture;
            }
            else
            {
                Debug.LogWarning($"ユーザーアイコンが見つからないお：{iconId}");
            }
        }

        // --- 2. ユーザーフレーム ---
        string frameId = !string.IsNullOrEmpty(data.selectedFrameId) ? data.selectedFrameId : "Frame";
        Sprite frameSprite = Resources.Load<Sprite>("SpecialFrameUI/" + frameId);
        if (frameSprite != null && profileFrame != null)
        {
            profileFrame.enabled = true;
            profileFrame.texture = frameSprite.texture;
        }

        // --- 3. ペットアイコン（成功したコードを維持！） ---
        string characterId = data.selectedCharacterId;
        if (!string.IsNullOrEmpty(characterId) && characterIcon != null)
        {
            Sprite petSprite = Resources.Load<Sprite>("CharacterIcon/CharIcon_" + characterId + "01");
            if (petSprite != null)
            {
                characterIcon.enabled = true;
                characterIcon.texture = petSprite.texture;
            }
            else { characterIcon.enabled = false; }
        }

        // --- 4. テキスト反映 ---
        userNameText.text = data.userName;
        playerIdText.text = "ID：" + (!string.IsNullOrEmpty(data.playerId) ? data.playerId : "--------");
        petNameText.text = !string.IsNullOrEmpty(data.petName) ? data.petName : "なまえなし";
        birthdayText.text = "誕生日：" + (!string.IsNullOrEmpty(data.ownerBirthday) ? data.ownerBirthday : "未設定");
        startDateText.text = "記念日：" + (!string.IsNullOrEmpty(data.startDate) ? data.startDate : "----年--月--日");
    }

    public void OnClickClose() => this.gameObject.SetActive(false);

    public void OnClickCopyID()
    {
        string rawId = playerIdText.text.Replace("ID：", "");
        GUIUtility.systemCopyBuffer = rawId;
        StopAllCoroutines();
        StartCoroutine(ShowToastCoroutine());
    }

    private IEnumerator ShowToastCoroutine()
    {
        copyToast.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        copyToast.SetActive(false);
    }
}