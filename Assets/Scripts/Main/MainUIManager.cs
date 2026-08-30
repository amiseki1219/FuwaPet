using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class MainUIManager : MonoBehaviour
{
    [Header("ユーザー情報")]
    [SerializeField] private TextMeshProUGUI userNameText;

    [Header("ユーザーアイコン")]
    [SerializeField] private RawImage iconRawImage;
    [SerializeField] private RawImage iconFrameRawImage;

    [Header("キャラクター情報")]
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Image charImage;
    [SerializeField] private TextMeshProUGUI daysTogetherText;

    [Header("信頼度")]
    [SerializeField] private Slider trustSlider;
    [SerializeField] private TextMeshProUGUI trustLevelText;
    [SerializeField] private TextMeshProUGUI trustNoticeText; // あと◯ptで次のLv

    [Header("所持金")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaStoneText;

    [Header("やることパネル")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject tutorialQuestPanel;

    [Header("データ引き継ぎ案内")]
    [SerializeField] private GameObject dataTransferPanel;

    [Header("お知らせ")]
    [SerializeField] private NoticeManager noticeManager;

    [Header("コーチマーク")]
    [SerializeField] private CoachMarkController coachMarkController;

    [Header("シーン遷移")]
    [SerializeField] private SceneLoader sceneLoader;

    private PetStatus _status;
    private SaveData _save;

    private void OnEnable()
    {
        GameData.OnWalletChanged += SetWallet;
        GameContext.OnAppResumed += OnAppResumed;
    }

    private void OnDisable()
    {
        GameData.OnWalletChanged -= SetWallet;
        GameContext.OnAppResumed -= OnAppResumed;
    }

    // 復帰時。減衰と保存は GameContext が済ませているので、Main は表示のやり直しと
    // 日付が変わっていた場合のデイリークエストのリセットだけを行う。
    private void OnAppResumed()
    {
        RefreshAll();
        QuestManager.Instance?.CheckDailyReset();
    }

    private void Start()
    {
        if (GameContext.Instance != null)
        {
            _status = GameContext.Instance.PetStatus;
        }
        else
        {
            _status = new PetStatus();
        }

        if (SaveManager.Instance != null)
        {
            _save = SaveManager.Instance.Data;
        }
        else
        {
            _save = new SaveData();
        }

        _status.ApplyTimeDecay();
        RefreshAll();

        QuestManager.Instance?.CheckDailyReset();
        QuestManager.Instance?.CompleteQuest(QuestId.DailyLogin);

        // データ引き継ぎ案内：初回のみ表示
        if (SaveManager.Instance != null && !SaveManager.Instance.Data.accountLinkShown)
        {
            if (dataTransferPanel != null)
                dataTransferPanel.SetActive(true);
        }

        // データ引き継ぎパネルが出ていない場合はここでコーチマークを表示
        if (dataTransferPanel == null || !dataTransferPanel.activeSelf)
            TryShowCoachMark();
    }

    public void RefreshAll()
    {
        SetUserInfo();
        SetPetInfo();
        SetWallet();
        SetTrustCircle();
    }

    // ─── ユーザー情報 ────────────────────────────

    private void SetUserInfo()
    {
        if (userNameText != null)
            userNameText.text = _save.userName;

        SetUserIcon();
    }

    private void SetUserIcon()
    {
        Debug.Log($"iconId: {SaveManager.Instance?.Data?.iconId}");
        Debug.Log($"frameId: {SaveManager.Instance?.Data?.selectedFrameId}");

        // アイコン表示
        if (iconRawImage != null)
        {
            string iconId = SaveManager.Instance?.Data?.iconId;
            Texture2D iconTex = null;
            if (!string.IsNullOrEmpty(iconId))
                iconTex = Resources.Load<Texture2D>("Icon/" + iconId);
            if (iconTex == null)
                iconTex = Resources.Load<Texture2D>("Icon/DefaultIcon");
            Debug.Log($"アイコンTexture: {iconTex}");
            if (iconTex != null)
                iconRawImage.texture = iconTex;
        }

        // フレーム表示
        if (iconFrameRawImage != null)
        {
            string frameId = SaveManager.Instance?.Data?.selectedFrameId;
            Texture2D frameTex = null;
            if (!string.IsNullOrEmpty(frameId))
                frameTex = Resources.Load<Texture2D>("SpecialFrameUI/" + frameId);
            if (frameTex == null)
                frameTex = Resources.Load<Texture2D>("SpecialFrameUI/DefaultFrame");
            Debug.Log($"フレームTexture: {frameTex}");
            if (frameTex != null)
                iconFrameRawImage.texture = frameTex;
        }
    }

    // ─── キャラクター情報 ────────────────────────

    private void SetPetInfo()
    {
        if (petNameText != null)
        {
            // ★2026/8/28：キャラID→日本語名の対応表を CharacterNames へ集約した。
            //   優先順位（ニックネーム → 日本語名 → petName）は今までと同じ。
            petNameText.text = CharacterNames.ResolveDisplayName(_save);
        }

        // キャラアイコンを設定
        string iconCharId = !string.IsNullOrEmpty(_save.selectedCharacterId) ? _save.selectedCharacterId : _save.characterId;
        Sprite charIcon = string.IsNullOrEmpty(iconCharId) ? null : Resources.Load<Sprite>("CharacterIcon/CharIcon_" + iconCharId + "01");
        if (charImage != null)
        {
            if (charIcon != null)
            {
                charImage.sprite = charIcon;
                charImage.enabled = true;
                Debug.Log("[MainUIManager] キャラアイコン設定: " + iconCharId);
            }
            else
            {
                charImage.enabled = false;
            }
        }

        // 出会って〇〇日（数字はピンク、「日」はブラウン）
        if (daysTogetherText != null)
        {
            int days = 1;
            if (!string.IsNullOrEmpty(_save.startDate) &&
                System.DateTime.TryParse(_save.startDate, out System.DateTime start))
            {
                days = (System.DateTime.Now - start).Days + 1;
            }
            daysTogetherText.text = $"<color=#F07BAA>{days}</color><color=#8B4513>日</color>";
        }

        SetCondition();
    }

    private void SetCondition()
    {
        float avg = (_status.Hunger + _status.Clean + _status.Energy) / 3f;
        string text;

        if (avg >= 80f)      text = "絶好調！";
        else if (avg >= 60f) text = "元気いっぱい！";
        else if (avg >= 40f) text = "ふつう";
        else if (avg >= 20f) text = "しょんぼり";
        else                 text = "元気ない...";

        if (conditionText != null) conditionText.text = text;
    }

    // ─── 信頼度円形ゲージ ────────────────────────

    private void SetTrustCircle()
    {
        int trust = _save.trust;
        int level = TrustFormula.GetLevel(trust);
        bool isMax = TrustFormula.IsMaxLevel(trust);
        int remaining = TrustFormula.GetPtsToNextLevel(trust);

        if (trustLevelText != null)
            trustLevelText.text = $"Lv {level}";

        if (isMax || remaining == 0)
        {
            if (trustSlider != null)
                trustSlider.value = 1f;

            if (trustNoticeText != null)
                trustNoticeText.text = "Lv.100 カンスト達成！";
        }
        else
        {
            if (trustSlider != null)
                trustSlider.value = TrustFormula.GetFillAmount(trust);

            if (trustNoticeText != null)
                trustNoticeText.text = $"あと{remaining}ptで信頼度アップ！";
        }
    }

    // ─── 所持金 ──────────────────────────────────

    private void SetWallet()
    {
        if (coinText != null)
            coinText.text = GameData.Instance.Coin.ToString();
        if (lunaStoneText != null)
            lunaStoneText.text = GameData.Instance.LunaStone.ToString();
    }

    // ─── ボタン遷移 ──────────────────────────────

    private SceneLoader GetSceneLoader()
    {
        if (sceneLoader != null) return sceneLoader;
        sceneLoader = FindFirstObjectByType<SceneLoader>();
        if (sceneLoader == null)
            Debug.LogWarning("[MainUIManager] SceneLoaderが見つかりません。SceneLoadingオブジェクトを確認してください。");
        return sceneLoader;
    }

    public void OnBtnCare()       => GetSceneLoader()?.GotoCare();
    public void OnBtnChat()       => GetSceneLoader()?.GoToChat();
    public void OnBtnCollection() => GetSceneLoader()?.GoToMyCollection();
    public void OnBtnShop()       => GetSceneLoader()?.GoToShop();
    public void OnBtnSetting()    => GetSceneLoader()?.GoToSetting();
    public void OnBtnNotice()     => noticeManager?.ShowPanel();
    public void OnBtnFurniture()  => GetSceneLoader()?.GoToRoomEdit();

    // ─── やることパネル ──────────────────────────

    public void OnBtnQuest()
    {
        bool isTutorial = QuestManager.Instance?.IsTutorialPhase ?? false;
        if (isTutorial)
            tutorialQuestPanel?.SetActive(true);
        else
            questPanel?.SetActive(true);
    }

    public void OnCloseQuestPanel()
    {
        questPanel?.SetActive(false);
        tutorialQuestPanel?.SetActive(false);
    }

    // ─── データ引き継ぎ案内 ──────────────────────

    public void OnAccountLinkLater()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.accountLinkShown = true;
            SaveManager.Instance.Save();
        }
        StartCoroutine(CloseDataTransferPanelNextFrame());
    }

    private IEnumerator CloseDataTransferPanelNextFrame()
    {
        yield return null;
        if (dataTransferPanel != null)
            dataTransferPanel.SetActive(false);
        TryShowCoachMark();
    }

    public void OnAppleSignIn()
    {
        Debug.Log("Apple Sign In（Firebase未実装）");
    }

    public void OnGoogleSignIn()
    {
        Debug.Log("Google Sign In（Firebase未実装）");
    }

    public void OnEmailSignUp()
    {
        Debug.Log("メール登録（Firebase未実装）");
    }

    // ─── ショップ・設定：チュートリアルクエスト通知 ──────────────
    // 既存の OnBtnShop / OnBtnSetting を上書きせず、ここで通知専用メソッドを定義。
    // Inspector 側でボタンの onClick に既存メソッドに加えてこちらも登録してください。

    public void NotifyShopOpenedForQuest()
    {
        QuestManager.Instance?.NotifyShopOpened();
    }

    public void NotifySettingOpenedForQuest()
    {
        QuestManager.Instance?.NotifySettingOpened();
    }

    // ─── コーチマーク表示 ────────────────────────────────────────

    private void TryShowCoachMark()
    {
        if (coachMarkController == null) return;
        var save = SaveManager.Instance?.Data;
        if (save == null || save.coachMarkShown) return;
        if (QuestManager.Instance == null || !QuestManager.Instance.IsTutorialPhase) return;
        coachMarkController.gameObject.SetActive(true);
    }

}