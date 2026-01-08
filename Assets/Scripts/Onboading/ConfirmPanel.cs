using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class ConfirmPanel : MonoBehaviour
{
    [Header("表示用テキスト")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI ownerNameText;
    [SerializeField] private TextMeshProUGUI birthdayText;
    [SerializeField] private TextMeshProUGUI petNameText;

    [Header("表示用画像")]
    [SerializeField] private Image petImage;

    [Header("ボタン")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    // パネルを開く時に呼ばれる関数
    public void Open(string inputPetName)
    {
        this.gameObject.SetActive(true);

        // 子要素を全部スキャンして、無理やり全部オンにする力技！
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        var data = SaveManager.Instance.Data;

        // 1. テキストの流し込み
        headerText.text = inputPetName;
        ownerNameText.text = data.ownerName;
        birthdayText.text = data.ownerBirthday;
        petNameText.text = inputPetName;

        // 2. 画像の読み込み（Resources/Characters/フォルダから）
        Sprite petSprite = Resources.Load<Sprite>($"Characters/{data.selectedCharacterId}");
        if (petImage != null) petImage.sprite = petSprite;

        // 3. YESボタン：データを保存してCare画面へ！
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => OnFinalDecision(inputPetName));

        // 4. NOボタン：自分を閉じるだけ（NamePanelに戻る）
        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(OnClickNo);

        this.gameObject.SetActive(true);
        Transform cardTransform = transform.Find("Card");
        if (cardTransform != null) cardTransform.gameObject.SetActive(true);
    }

    // ★YESボタンが押された時の最終処理
    public void OnFinalDecision(string inputPetName)
    {
        var data = SaveManager.Instance.Data;
        if (string.IsNullOrEmpty(data.playerId))
        {
            data.playerId = GenerateRandomID(8);
        }
        // 名前を保存して「完了」フラグを立てる
        data.petName = inputPetName;
        data.onboardingCompleted = true; // これで次はHOMEに行くようになる
        data.startDate = DateTime.Now.ToString("yyyy-MM-dd"); // 今日が出会った日
        SaveManager.Instance.Save();

        // チュートリアル直後は直接「Care画面」へ！
        UnityEngine.SceneManagement.SceneManager.LoadScene("Care");
    }

    private string GenerateRandomID(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[length];
        System.Random random = new System.Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
    // ConfirmPanel.cs の中にこれを追加
    [SerializeField] private GameObject namePanelCard; // インスペクターでNamePanelCardを紐付けてね

    public void OnClickNo()
    {
        this.gameObject.SetActive(false); // 自分（確認画面）を消す
        namePanelCard.SetActive(true);    // 名前入力画面を出す
    }
}