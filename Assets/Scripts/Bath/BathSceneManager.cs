using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;   // FormerlySerializedAs（ichigoButton → ohisamaButton の結線引き継ぎ）

public class BathSceneManager : MonoBehaviour
{
    [System.Serializable]
    private class ShampooData
    {
        public string id;
        public string displayName;
        public string imageName;
        public string description;
        public string effectText;
        public int costCoin;
        public int costLuna;
    }

    private static readonly List<ShampooData> AllShampoo = new List<ShampooData>
    {
        new ShampooData
        {
            id = "normal",
            displayName = "せっけん",
            imageName = "NomalImage",
            description = "さっぱりやさしい泡立ち。\n毎日使えるシンプルなせっけん",
            effectText = $"{ParamNames.Clean} {ParamNames.Pt(40)}",
            costCoin = 0, costLuna = 0
        },
        new ShampooData
        {
            // ★2026/8/28：内部IDを "ichigo" から "ohisama" へ改名した。
            //   Bath.unity の BathTouchEffect.shampooSets[].shampooId が先に "ohisama" になっていて
            //   ID が食い違い、飾りパーティクルが せっけん の設定へ黙って落ちていたため、コード側をそろえた。
            //   シャンプーIDはセーブデータに保存していないので、既存セーブへの影響はない。
            id = "ohisama",
            displayName = "おひさまシャンプー",
            imageName = "OhisamaImage",
            description = "おひさまにあたったようないい香り。\n使うたびに甘えん坊になっちゃう？",
            effectText = $"{ParamNames.Clean} {ParamNames.Pt(60)}\n{ParamNames.Dependency} {ParamNames.Pt(2)}",
            costCoin = 0, costLuna = 500
        },
        new ShampooData
        {
            id = "hoshizora",
            displayName = "ほしぞらシャンプー",
            imageName = "HoshiImage",
            description = "星空みたいな神秘的な香り。\nコツコツがんばる気持ちが芽生えるかも",
            effectText = $"{ParamNames.Clean} {ParamNames.Pt(60)}\n{ParamNames.Diligence} {ParamNames.Pt(2)}",
            costCoin = 0, costLuna = 500
        },
        new ShampooData
        {
            id = "rainbow",
            displayName = "レインボーせっけん",
            imageName = "RainbowImage",
            description = "7色の泡があふれだす！\nどんな変化が起きるかはおたのしみ♪",
            effectText = $"{ParamNames.Clean} {ParamNames.Pt(60)}\n性格のどれかが少しアップ",
            costCoin = 100, costLuna = 0
        },
    };

    private static readonly string[] SpeechBubbleTexts =
    {
        "今日はどの香りにする〜？",
        "ふわふわ泡で洗ってほしいな",
        "やさしくごしごししてね",
        "いいにおいにしてくれる？",
        "今日もいっぱい遊んだ！きれいにして〜",
        "ぴかぴかになる準備できてるよ",
        "今日は甘い香りがいい気分…♡",
        "おふろでさっぱりしたいな！",
        "いい香りのおふろ、たのしみ♪",
        "どれ使うの？わくわくする〜！",
    };

    [Header("パネル切り替え")]
    [SerializeField] private GameObject selectShampooPanel;
    [SerializeField] private GameObject washPanel;
    [SerializeField] private BathWashManager bathWashManager;
    [SerializeField] private Button goNextButton;

    [Header("財布表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaText;

    [Header("吹き出し")]
    [SerializeField] private TextMeshProUGUI speechBubbleText;

    [Header("シャンプーボタン")]
    [SerializeField] private Button nomalButton;
    // ★2026/8/28：ichigoButton から改名。
    //   FormerlySerializedAs を付けてあるので、Bath.unity 側の結線（OhisamaButton）は
    //   Unity が自動で引き継ぐ。Inspector での再結線は不要。
    [FormerlySerializedAs("ichigoButton")]
    [SerializeField] private Button ohisamaButton;
    [SerializeField] private Button hoshizoraButton;
    [SerializeField] private Button rainbowButton;

    [Header("選択中Panel")]
    [SerializeField] private RawImage selectRawImage;
    [SerializeField] private TextMeshProUGUI selectSorpName;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effortText;

    // 1日のお風呂上限。CareSceneManager.MaxBathPerDay と同じ値を持たせている。
    // ※ 定数が2箇所にあるのは暫定。日付リセットの整理とあわせて後で一元化する
    private const int MaxBathPerDay = 2;

    private string _selectedId = "normal";
    private Coroutine _coinCoroutine;
    private Coroutine _lunaStoneCoroutine;

    private void Start()
    {
        SetRandomSpeechBubble();
        SetupButtonListeners();
        ApplyPriceLabels();          // 価格は AllShampoo を正とし、Scene の直書きを上書きする
        OnSelectShampoo("normal");
        if (goNextButton != null) goNextButton.onClick.AddListener(OnGoNext);
        RefreshWallet();

        // ★A2.7：お風呂に入った時点の表情は、状態パラメータに関係なく必ず Normal にする。
        //   （おなかが減っていて Sad が出ている状態でお風呂に来ても、Normal から始める）
        //   実処理は BathWashManager 側に置いている。洗い中の表情変化と同じ場所にまとめ、
        //   表情を触る箇所が2つに分かれないようにするため。
        //   ★WashPanel は非アクティブだが、コンポーネントのメソッド呼び出しは問題なく動く。
        bathWashManager?.SetFaceNormalOnEnter();
    }

    private void OnGoNext()
    {
        var data = AllShampoo.Find(s => s.id == _selectedId);
        if (data == null) return;

        // 1日2回の上限チェック。
        // 通常は Care 画面（CareSceneManager.OnBtnBath）で止まるが、Bath.unity から直接 Play した場合は
        // そこを通らないため、ここでも守る。シャンプー代を払う前に判定するのが重要。
        // ここでは回数のリセットはしない（リセットの責務を増やさないため）。
        // 日付が変わっていれば「今日は0回」とみなすだけにする。
        var saveForLimit = SaveManager.Instance?.Data;
        if (saveForLimit != null)
        {
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            int bathToday = (saveForLimit.lastBathDate == today) ? saveForLimit.bathCountToday : 0;
            if (bathToday >= MaxBathPerDay)
            {
                Debug.LogWarning($"[Bath] 今日のお風呂は {MaxBathPerDay} 回までです（現在 {bathToday} 回）");
                return;
            }
        }

        bool hasCost = data.costCoin > 0 || data.costLuna > 0;
        if (hasCost)
        {
            if (data.costCoin > 0)
            {
                if (GameData.Instance != null)
                {
                    if (!GameData.Instance.UseCoin(data.costCoin)) { Debug.LogWarning("コインが足りません"); return; }
                }
                else
                {
                    var save = SaveManager.Instance?.Data;
                    if (save == null || save.coinCount < data.costCoin) { Debug.LogWarning("コインが足りません"); return; }
                    save.coinCount -= data.costCoin;
                    SaveManager.Instance?.Save();
                }
            }
            if (data.costLuna > 0)
            {
                if (GameData.Instance != null)
                {
                    if (!GameData.Instance.UseLunaStone(data.costLuna)) { Debug.LogWarning("ルナストーンが足りません"); return; }
                }
                else
                {
                    var save = SaveManager.Instance?.Data;
                    if (save == null || save.lunaStoneCount < data.costLuna) { Debug.LogWarning("ルナストーンが足りません"); return; }
                    save.lunaStoneCount -= data.costLuna;
                    SaveManager.Instance?.Save();
                }
            }
            RefreshWallet();
        }

        if (selectShampooPanel != null) selectShampooPanel.SetActive(false);
        if (washPanel          != null) washPanel.SetActive(true);
        bathWashManager?.Initialize(_selectedId);
    }

    private void RefreshWallet()
    {
        int coin = GameData.Instance != null ? GameData.Instance.Coin
                 : SaveManager.Instance?.Data?.coinCount ?? 0;
        int luna = GameData.Instance != null ? GameData.Instance.LunaStone
                 : SaveManager.Instance?.Data?.lunaStoneCount ?? 0;

        if (coinText != null)
        {
            int from = int.TryParse(coinText.text, out int parsedCoin) ? parsedCoin : coin;
            if (_coinCoroutine != null) StopCoroutine(_coinCoroutine);
            _coinCoroutine = StartCoroutine(AnimateCoinText(coinText, from, coin, 0.5f));
        }
        if (lunaText != null)
        {
            int from = int.TryParse(lunaText.text, out int parsedLuna) ? parsedLuna : luna;
            if (_lunaStoneCoroutine != null) StopCoroutine(_lunaStoneCoroutine);
            _lunaStoneCoroutine = StartCoroutine(AnimateCoinText(lunaText, from, luna, 0.5f));
        }
    }

    private IEnumerator AnimateCoinText(TextMeshProUGUI text, int fromValue, int toValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            text.text = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t)).ToString();
            yield return null;
        }
        text.text = toValue.ToString();
    }

    private void SetRandomSpeechBubble()
    {
        if (speechBubbleText == null) return;
        speechBubbleText.text = SpeechBubbleTexts[Random.Range(0, SpeechBubbleTexts.Length)];
    }

    private void SetupButtonListeners()
    {
        if (nomalButton     != null) nomalButton.onClick.AddListener(()     => OnSelectShampoo("normal"));
        if (ohisamaButton   != null) ohisamaButton.onClick.AddListener(()   => OnSelectShampoo("ohisama"));
        if (hoshizoraButton != null) hoshizoraButton.onClick.AddListener(() => OnSelectShampoo("hoshizora"));
        if (rainbowButton   != null) rainbowButton.onClick.AddListener(()   => OnSelectShampoo("rainbow"));
    }

    // 各シャンプーボタンの価格ラベルを AllShampoo の値から書き込む。
    // Scene に数字を直書きしていると、価格改定のたびにコードと Scene の両方を直す必要があり、
    // 実際に「コードは 500 なのに画面は 50」というズレが起きた（2026/8/22）。
    // 価格の出所を AllShampoo の1箇所に集約するのが目的。
    private void ApplyPriceLabels()
    {
        ApplyPriceLabel(nomalButton,     "normal");
        ApplyPriceLabel(ohisamaButton,   "ohisama");
        ApplyPriceLabel(hoshizoraButton, "hoshizora");
        ApplyPriceLabel(rainbowButton,   "rainbow");
    }

    private void ApplyPriceLabel(Button btn, string shampooId)
    {
        if (btn == null) return;

        var data = AllShampoo.Find(s => s.id == shampooId);
        if (data == null) return;

        // ボタンの子 CoinPanel の中にある TextMeshProUGUI を探す。
        // UpdateFrame() が "SelectFrame" を Find しているのと同じやり方に揃えた。
        var coinPanel = btn.transform.Find("CoinPanel");
        if (coinPanel == null)
        {
            Debug.LogWarning($"[Bath] {btn.name} に CoinPanel が見つかりません。価格表示を更新できません");
            return;
        }

        // 非アクティブな子も対象にするため includeInactive = true
        var label = coinPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            Debug.LogWarning($"[Bath] {btn.name}/CoinPanel に TextMeshProUGUI が見つかりません");
            return;
        }

        label.text = GetPriceLabel(data);
    }

    // 無料なら FREE、無償コイン🪙ならその数字、有償コイン♡ならその数字を返す。
    private string GetPriceLabel(ShampooData data)
    {
        if (data.costCoin <= 0 && data.costLuna <= 0) return "FREE";
        return data.costCoin > 0 ? data.costCoin.ToString() : data.costLuna.ToString();
    }

    public void OnSelectShampoo(string shampooId)
    {
        _selectedId = shampooId;
        UpdateSelectFrames(shampooId);
        UpdateInfoPanel(shampooId);
    }

    private void UpdateSelectFrames(string shampooId)
    {
        UpdateFrame(nomalButton,     "normal",     shampooId);
        UpdateFrame(ohisamaButton,   "ohisama",    shampooId);
        UpdateFrame(hoshizoraButton, "hoshizora",  shampooId);
        UpdateFrame(rainbowButton,   "rainbow",    shampooId);
    }

    private void UpdateFrame(Button btn, string btnId, string selectedId)
    {
        if (btn == null) return;
        var frame = btn.transform.Find("SelectFrame");
        if (frame != null)
            frame.gameObject.SetActive(btnId == selectedId);
    }

    private void UpdateInfoPanel(string shampooId)
    {
        var data = AllShampoo.Find(s => s.id == shampooId);
        if (data == null) return;

        if (selectSorpName  != null) selectSorpName.text  = data.displayName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (effortText      != null) effortText.text      = data.effectText;

        if (selectRawImage != null)
        {
            var tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormC)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormD)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName}");
            selectRawImage.texture = tex;
        }
    }
}
