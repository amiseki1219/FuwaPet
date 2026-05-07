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
    [SerializeField] private Image conditionIconImage;
    [SerializeField] private TextMeshProUGUI daysTogetherText;

    [Header("信頼度")]
    [SerializeField] private Image trustCircleImage;
    [SerializeField] private TextMeshProUGUI trustLevelText;
    [SerializeField] private TextMeshProUGUI nextLevelText; // あと◯ptで次のLv

    [Header("コンディションゲージ Fill Area")]
    [SerializeField] private RectTransform moodFillArea;

    [Header("所持金")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaStoneText;

    [Header("コンディションアイコン")]
    [SerializeField] private Sprite iconSuperGood;
    [SerializeField] private Sprite iconGood;
    [SerializeField] private Sprite iconNormal;
    [SerializeField] private Sprite iconBad;
    [SerializeField] private Sprite iconSuperBad;

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
        SetConditionBar();
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
                iconTex = Resources.Load<Texture2D>("Icon/Icon1 1");
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
            if (!string.IsNullOrEmpty(_save.petNickname))
                petNameText.text = _save.petNickname;
            else
            {
                string charId = !string.IsNullOrEmpty(_save.selectedCharacterId) ? _save.selectedCharacterId : _save.characterId;
                petNameText.text = charId switch
                {
                    "poko" => "ぽこ",
                    "eru"  => "える",
                    "koko" => "ここ",
                    "paru" => "ぱる",
                    _ => _save.petName ?? ""
                };
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
        float avg = (_status.Hunger + _status.Mood + _status.Energy) / 3f;
        string text;
        Sprite icon;

        if (avg >= 80f)      { text = "絶好調✨";     icon = iconSuperGood; }
        else if (avg >= 60f) { text = "元気いっぱい！"; icon = iconGood; }
        else if (avg >= 40f) { text = "ふつう";       icon = iconNormal; }
        else if (avg >= 20f) { text = "しょんぼり";   icon = iconBad; }
        else                 { text = "元気ない...";  icon = iconSuperBad; }

        if (conditionText != null) conditionText.text = text;
        if (conditionIconImage != null && icon != null)
            conditionIconImage.sprite = icon;
    }

    // ─── 信頼度円形ゲージ ────────────────────────

    private void SetTrustCircle()
    {
        int trust = _save.trust;
        int level = PetStatus.GetTrustLevel(trust);
        float fill = PetStatus.GetTrustFillAmount(trust);

        if (trustCircleImage != null)
            trustCircleImage.fillAmount = fill;

        if (trustLevelText != null)
            trustLevelText.text = $"{level}";

        if (nextLevelText != null)
            nextLevelText.text = $"次のLvまであと{GetTrustPtsToNextLevel(trust)}pt";
    }

    private int GetTrustPtsToNextLevel(int trust)
    {
        if (trust < 100)  return 100 - trust;
        if (trust < 400)  return 400 - trust;
        if (trust < 1400) return 1400 - trust;
        int level = PetStatus.GetTrustLevel(trust);
        int nextThreshold = 1400 + (level - 3) * 2000;
        return nextThreshold - trust;
    }

    // ─── コンディションゲージ ─────────────────────

    private void SetConditionBar()
    {
        if (moodFillArea == null) return;
        float ratio = Mathf.Clamp01((_status.Mood + _status.Hunger + _status.Energy) / 3f / 100f);
        // Fill Area は pivot=(0,0.5)・anchor=left(0,0.5)
        // 最大幅 = 親の幅 - 左マージン(anchoredPosition.x)
        var parentRect = moodFillArea.parent.GetComponent<RectTransform>();
        float fullWidth = parentRect.rect.width - moodFillArea.anchoredPosition.x;
        moodFillArea.sizeDelta = new Vector2(fullWidth * ratio, moodFillArea.sizeDelta.y);
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