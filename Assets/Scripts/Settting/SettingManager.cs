using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class SettingManager : MonoBehaviour
{
    [Header("プロフィール表示用")]
    [SerializeField] private TextMeshProUGUI ownerNameText;
    [SerializeField] private TextMeshProUGUI birthdayText;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI startDayText;

    [Header("パネル管理")]
    [SerializeField] private GameObject profileDetailPanel;
    [SerializeField] private TextMeshProUGUI playerIDText;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject profileMainButton;

    [Header("サウンド設定")]
    [SerializeField] private Slider bgmSlider;

    [Header("通知ボタン")]
    [SerializeField] private Button notificationOnBtn;
    [SerializeField] private Button notificationOffBtn;

    [Header("効果音ボタン")]
    [SerializeField] private Button seOnBtn;
    [SerializeField] private Button seOffBtn;

    [Header("初期化パネルのキャラ用")]
    [SerializeField] private Image sadCharacterDisplay; // パネルの中のImage枠
    [SerializeField] private GameObject deleteConfirmPanel;

    private void Start()
    {
        // 最初にデータを読み込んで表示するよ！
        RefreshProfile();

        // 最初は詳細パネルを閉じておくなら
        if (profileDetailPanel != null) profileDetailPanel.SetActive(false);

        // 初期状態をONにして色をつけておくよ
        UpdateButtonVisuals(notificationOnBtn, notificationOffBtn, true);
        UpdateButtonVisuals(seOnBtn, seOffBtn, true);
    }

    public void RefreshProfile()
    {
        if (SaveManager.Instance == null) return;
        var data = SaveManager.Instance.Data;

        // チュートリアルで保存したデータをテキストに流し込む
        if (ownerNameText != null) ownerNameText.text = data.ownerName;
        if (birthdayText != null) birthdayText.text = data.ownerBirthday;
        if (characterNameText != null) characterNameText.text = data.petName;
        if (startDayText != null) startDayText.text = data.startDate + "〜";
        if (playerIDText != null) playerIDText.text = "ID: " + data.playerId;
    }

    // プロフィールボタンを押した時にパネルを出す用
    public void OpenProfile()
    {
        RefreshProfile();

        if (profileDetailPanel != null)
        {
            // 1. パネルをアクティブにする
            profileDetailPanel.SetActive(true);

            // ★2. これを追加！「自分を親子関係の最後（一番手前）に移動して！」という命令
            profileDetailPanel.transform.SetAsLastSibling();
        }

        // 3. 他のUIを消す
        if (mainUI != null) mainUI.SetActive(false);

    }


    public void CloseProfile()
    {
        if (profileDetailPanel != null) profileDetailPanel.SetActive(false);
        if (mainUI != null) mainUI.SetActive(true);  // ★後ろを復活させる！

    }

    // スライダーを動かした時に呼ばれる関数
    public void OnBgmSliderChanged()
    {
        if (bgmSlider == null) return;

        float volume = bgmSlider.value; // 0.0 〜 1.0 の値

        // 1. 実際に音量を変える（AudioManagerがあるならそこへ、なければとりあえずAudioListenerで全体を変える）
        AudioListener.volume = volume;

        // 2. 設定をセーブデータに保存する（後で実装しよう！）
        Debug.Log("BGM音量を " + volume + " に変えたよ！");
    }

    // 通知ボタン用（ONはTrue, OFFはFalseをイベントから送るよ）
    public void SetNotification(bool isOn)
    {
        UpdateButtonVisuals(notificationOnBtn, notificationOffBtn, isOn);
        Debug.Log("通知を " + (isOn ? "ON" : "OFF") + " にしたよ！");
    }

    // 効果音ボタン用
    public void SetSe(bool isOn)
    {
        UpdateButtonVisuals(seOnBtn, seOffBtn, isOn);
        Debug.Log("効果音を " + (isOn ? "ON" : "OFF") + " にしたよ！");
    }

    // 選ばれた方のボタンを明るく、選ばれてない方を暗くする魔法
    private void UpdateButtonVisuals(Button onBtn, Button offBtn, bool isOn)
    {
        if (onBtn == null || offBtn == null) return;

        // 色だけを変えるよ（1.0fが通常、0.5fが少し暗い状態）
        // isOnがTrueなら：ONボタンを明るく(1.0f)、OFFボタンを少し暗く(0.5f)
        onBtn.image.color = new Color(1f, 1f, 1f, isOn ? 1.0f : 0.5f);
        offBtn.image.color = new Color(1f, 1f, 1f, isOn ? 0.5f : 1.0f);

        // 【重要】ボタンの「反応」は絶対に消さない！
        onBtn.interactable = true;
        offBtn.interactable = true;
    }
    public void OpenDeletePanel()
    {
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
            deleteConfirmPanel.transform.SetAsLastSibling();

            if (SaveManager.Instance != null && sadCharacterDisplay != null)
            {
                // ★ petName ではなく characterId を使うよ！
                string charID = SaveManager.Instance.Data.characterId;

                // ファイル名を作る（例: "Dog_sad"）
                string fileName = charID + "_sad";

                // Resourcesから画像をロード
                Sprite sadSprite = Resources.Load<Sprite>(fileName);

                if (sadSprite != null)
                {
                    sadCharacterDisplay.sprite = sadSprite;
                    Debug.Log("読み込み成功！: " + fileName);
                }
                else
                {
                    // もし出ない時は、このログをコンソールで見てね！
                    Debug.LogError($"画像が見つからないよ。Resources直下に「{fileName}」はあるかな？");
                }
            }
        }
    }
    // パネルの中の「NO（いいえ）」ボタンを押した時
    public void CloseDeletePanel()
    {
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false); // パネルを隠すだけ
            Debug.Log("よかった、初期化はやめたよ！");
        }
    }

    // パネルの中の「YES（はい）」ボタンを押した時
    public void ExecuteDelete()
    {
        if (SaveManager.Instance != null)
        {
            // 1. データを真っ白にする（SaveManagerにある初期化を呼ぶ）
            SaveManager.Instance.DeleteData(); // ※DeleteDataという名前で作ってあるかな？

            Debug.Log("<color=red>全ての思い出を消去しました...</color>");

            // 2. データを消した後は、タイトル画面（最初）に戻してあげよう！
            UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial"); // タイトルシーンの名前に合わせてね
        }
    }

}