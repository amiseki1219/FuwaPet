using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

[System.Serializable]
public class OyatuData
{
    public string id;
    public string displayName;
    public string imageName;          // Resources/FoodUI/ 以下のファイル名
    public int coinCost;              // 無償コイン消費（0なら有償）
    public int lunaCost;              // 有償ルナストーン消費（0なら無償）
    public int hungerAmount;          // おなか回復量
    public int energyAmount;          // 元気回復量
    public bool fullRecovery;         // 全パラ回復フラグ
    public int trustAmount;           // 信頼度加算
    // ★内部名が正（CLAUDE.md §22）。括弧内は画面に出す表示名（ParamNames.cs）。
    public int personalityActivity;   // 活動性（表示名：おてんば）
    public int personalityDependency; // 甘えん坊度（表示名：甘えん坊）
    public int personalityHonesty;    // 素直さ（表示名：素直）
    public int personalityDiligence;  // 勤勉さ（表示名：しっかりもの）
    public int personalitySensitivity;// 感受性（表示名：優しさ）
    public bool isFree;               // 無償おやつフラグ
}

public class OyatuManager : MonoBehaviour
{
    private const int FreeOyatuDailyLimit = 6;

    private static readonly List<OyatuData> AllOyatu = new List<OyatuData>
    {
        // ── 無償おやつ ──────────────────────────────────────────────────────────────
        // ★価格は requirements.md §5。3種とも 10🪙 に統一（2026/5/17 決定 → 2026/8/30 にコードへ反映）。
        //   §7 の「無償おやつ 10🪙×6回＝60🪙」もこの値が前提。
        new OyatuData { id = "niboshi",         displayName = "にぼし",            imageName = "にぼし",        coinCost = 10,  hungerAmount = 10, energyAmount = 5, trustAmount = 3,                                                   isFree = true  },
        new OyatuData { id = "biscuit",         displayName = "ビスケット",        imageName = "ビスケット",    coinCost = 10,  hungerAmount = 15, trustAmount = 3,                                                   isFree = true  },
        new OyatuData { id = "carrot",          displayName = "にんじんスティック", imageName = "にんじん",      coinCost = 10,  energyAmount = 15, trustAmount = 3,                                                   isFree = true  },

        // ── 有償おやつ ──────────────────────────────────────────────────────────────
        // ★価格は requirements.md §5。2026/6/17 の「有償コイン単価10倍」を 2026/8/30 にコードへ反映した。
        //   シャンプー（BathSceneManager）は先に10倍後の値になっていて、おやつだけ取り残されていた。
        new OyatuData { id = "strawberry_cake", displayName = "いちごケーキ",      imageName = "いちごケーキ",  lunaCost = 800, hungerAmount = 25,  trustAmount = 5,  personalityDependency = 5,                       isFree = false },
        new OyatuData { id = "pudding",         displayName = "プリン",            imageName = "プリン",        lunaCost = 800, hungerAmount = 20,  trustAmount = 5,  personalityHonesty = 5,                          isFree = false },
        new OyatuData { id = "fruit_tart",      displayName = "フルーツタルト",    imageName = "フルーツタルト", lunaCost = 1200, hungerAmount = 20,  energyAmount = 20, trustAmount = 5, personalityDiligence = 5,     isFree = false },
        new OyatuData { id = "macaron",         displayName = "ハートマカロン",    imageName = "マカロン",      lunaCost = 1500, hungerAmount = 15,  energyAmount = 15, trustAmount = 5, personalitySensitivity = 5,   isFree = false },
        new OyatuData { id = "hamburg",         displayName = "特製ハンバーグ",    imageName = "ハンバーグ",    lunaCost = 2000, hungerAmount = 100, trustAmount = 35,  personalityActivity = 5,                        isFree = false },
        new OyatuData { id = "parfait",         displayName = "スペシャルパフェ",  imageName = "パフェ",        lunaCost = 3000, fullRecovery = true, trustAmount = 55,
                        personalityActivity = 3, personalityDependency = 3, personalityDiligence = 3, personalityHonesty = 3, personalitySensitivity = 3,                                                                isFree = false },
    };

    [Header("パネル")]
    [SerializeField] private GameObject oyatuSelectPanel;
    [SerializeField] private GameObject freeOyatuPanel;
    [SerializeField] private GameObject paidOyatuPanel;
    [SerializeField] private Image freeTabImage;
    [SerializeField] private Image paidTabImage;
    [SerializeField] private Color selectedTabColor;   // FFD3D1
    [SerializeField] private Color unselectedTabColor; // FCEBD6

    [Tooltip("タブの文字。ノーマルおやつ側（TabBar/FreeOyatu の Text (TMP)）")]
    [SerializeField] private TextMeshProUGUI freeTabLabel;

    [Tooltip("タブの文字。スペシャルおやつ側（TabBar/MyOyatu の Text (TMP)）")]
    [SerializeField] private TextMeshProUGUI paidTabLabel;

    // ★色は必ず初期値を書く。Unity は未設定の Color を (0,0,0,0)＝透明で入れるため、
    //   書かないと「文字が消えた」ように見えて原因が分からなくなる
    //   （BathFinishEffect で実際に踏んだ地雷。CLAUDE.md §23 参照）
    [Tooltip("選択中のタブの文字色。ピンクの上に乗るので白")]
    [SerializeField] private Color selectedTabTextColor = Color.white;

    [Tooltip("選択していないタブの文字色。クリームの上に乗るので茶色")]
    [SerializeField] private Color unselectedTabTextColor = new Color(0.55f, 0.43f, 0.31f, 1f); // 8C6E4F

    [Tooltip("「※ノーマルおやつは1日6回まであげられるよ」の案内文（NaviText）。\n" +
             "★ノーマルおやつのタブでだけ出す。マイおやつのタブでは隠す。\n" +
             "  未結線でも動く（出しっぱなしになるだけ）")]
    [SerializeField] private GameObject naviText;

    [Header("ボタンの自動生成（U-14・2026/8/30）")]
    [Tooltip("Assets/Prefabs/Oyatu/OyatuButton.prefab を結線する。\n" +
             "★これを結線すると、ボタンは AllOyatu から自動で並べられる。\n" +
             "  未結線のときは Scene に手で置いたボタンがそのまま使われる（従来どおり）")]
    [SerializeField] private OyatuButtonView oyatuButtonPrefab;

    [Tooltip("無償おやつのボタンを並べる先。ふつうは Free Oyatu Panel と同じでよい")]
    [SerializeField] private Transform freeButtonRoot;

    [Tooltip("有償おやつのボタンを並べる先。★Scroll View の Content を結線する")]
    [SerializeField] private Transform paidButtonRoot;

    [Header("選択中おやつ表示（SelectOyatuPanel）")]
    [SerializeField] private RawImage selectOyatuImage;
    [SerializeField] private TextMeshProUGUI selectOyatuName;
    [SerializeField] private TextMeshProUGUI selectOyatuEffect;
    [SerializeField] private Button giveButton;
    [SerializeField] private Button closeButton;

    // 各ボタンの SelectBadge は OyatuButtonId コンポーネント経由で取得

    [Header("参照")]
    [SerializeField] private CareSceneManager    careManager;
    [SerializeField] private CarePokoController  carePokoController;

    [Tooltip("ぽこ以外のキャラのアクション。未結線でもよい（アニメが出ないだけ）")]
    [SerializeField] private CareCharacterActionController careCharacterAction;

    // true=hungerPopup / false=energyPopup
    // true=hungerPopup / false=energyPopup
    private static readonly Dictionary<string, (bool useHunger, string text)> PopupMap = new()
    {
        { "niboshi",         (true,  "+10") },
        { "biscuit",         (true,  "+15") },
        { "carrot",          (false, "+10") },
        { "strawberry_cake", (true,  "+25") },
        { "pudding",         (true,  "+20") },
        { "fruit_tart",      (true,  "+20 / +20") },
        { "macaron",         (true,  "+15 / +15") },
        { "hamburg",         (true,  "全回復") },
        { "parfait",         (true,  "全回復") },
    };

    private static readonly Dictionary<string, string> ButtonNameToId = new()
    {
        { "にぼしButton",         "niboshi" },
        { "にんじんButton",       "carrot" },
        { "ビスケットButton",     "biscuit" },
        { "いちごケーキButton",   "strawberry_cake" },
        { "プリンButton",         "pudding" },
        { "フルーツタルトButton", "fruit_tart" },
        { "マカロンButton",       "macaron" },
        { "ハンバーグButton",     "hamburg" },
        { "パフェButton",         "parfait" },
    };

    private OyatuData _selectedOyatu;

    /// <summary>
    /// 実行時に並べたボタン。id から引けるようにしておく。
    /// ★Prefab が未結線のときは空のまま。その場合は Scene に手で置いたボタンが使われる。
    /// </summary>
    private readonly Dictionary<string, OyatuButtonView> _buttons = new();

    /// <summary>ボタンを1度でも並べたか。二重に並べないための目印。</summary>
    private bool _buttonsBuilt;

    // ── 初期化 ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (freeTabImage != null)   freeTabImage.color = selectedTabColor;
        if (paidTabImage != null)   paidTabImage.color = unselectedTabColor;
        if (closeButton  != null)   closeButton.onClick.AddListener(HidePanel);

        BuildButtons();
    }

    /// <summary>
    /// AllOyatu からおやつボタンを並べる。★U-14（2026/8/30）
    ///
    /// 【なぜコードで並べるのか】
    ///   以前は9個のボタンを Scene に手で置き、名前と価格を手打ちしていた。
    ///   そのため AllOyatu の価格を直しても画面は古いままという食い違いが起きていた。
    ///   おやつを増やすときも「ボタンを作る／対応表に足す／AllOyatu に足す」の3箇所が必要だった。
    ///   → AllOyatu だけを正とし、そこからボタンを作る。増やすときは AllOyatu に1行足すだけ。
    ///
    /// 【未結線でも壊さない】
    ///   Prefab か並べ先が未結線なら、何もせずに戻る。
    ///   Scene に手で置いたボタンがそのまま動く（従来どおり）。
    ///   ★どちらで動いたかは必ず1行ログに出す。黙って挙動が変わらないようにするため。
    /// </summary>
    private void BuildButtons()
    {
        if (_buttonsBuilt) return;

        if (oyatuButtonPrefab == null || freeButtonRoot == null || paidButtonRoot == null)
        {
            Debug.LogWarning("<color=#00E5FF>[決定]</color> [Care] おやつボタンは【Scene に手で置いたもの】を使います" +
                             "（OyatuManager の Oyatu Button Prefab / Free Button Root / Paid Button Root のいずれかが未結線）", this);
            return;
        }

        _buttonsBuilt = true;

        // 並べ先に残っている古いボタンを消す。Scene の手置きぶんと二重に出さないため
        ClearChildren(freeButtonRoot);
        ClearChildren(paidButtonRoot);

        var save = SaveManager.Instance?.Data;

        foreach (var data in AllOyatu)
        {
            var root = data.isFree ? freeButtonRoot : paidButtonRoot;
            var view = Instantiate(oyatuButtonPrefab, root);
            view.Bind(data, OyatuInventory.StockLabel(save, data.id), OnSelectOyatu);
            _buttons[data.id] = view;
        }

        Debug.Log($"<color=#00E5FF>[決定]</color> [Care] おやつボタンを {AllOyatu.Count} 個【自動生成】しました" +
                  $"（無償 {AllOyatu.FindAll(o => o.isFree).Count} / 有償 {AllOyatu.FindAll(o => !o.isFree).Count}）", this);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else                       DestroyImmediate(child);
        }
    }

    // ── パネル開閉 ──────────────────────────────────────────────────────────────

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        OnTabFree();
        OnSelectOyatu("niboshi");
        RefreshStockTexts();   // ★U-9：所持数の表示を最新にする
    }

    public void HidePanel()
    {
        _selectedOyatu = null;
        ClearSelectBadges();
        gameObject.SetActive(false);
    }

    // ── タブ切り替え ────────────────────────────────────────────────────────────

    public void OnTabFree()
    {
        if (freeOyatuPanel != null) freeOyatuPanel.SetActive(true);
        if (paidOyatuPanel != null) paidOyatuPanel.SetActive(false);
        if (freeTabImage != null)   freeTabImage.color = selectedTabColor;
        if (paidTabImage != null)   paidTabImage.color = unselectedTabColor;

        // ★背景の色だけでなく、文字の色も切り替える。
        //   選択中＝ピンクの上に白、非選択＝クリームの上に茶色。
        //   片方だけにすると、選択中の文字が読めなくなる
        if (freeTabLabel != null) freeTabLabel.color = selectedTabTextColor;
        if (paidTabLabel != null) paidTabLabel.color = unselectedTabTextColor;

        // ★1日6回の上限は【無償おやつだけ】の話なので、こちらのタブでだけ案内を出す
        if (naviText != null) naviText.SetActive(true);
    }

    public void OnTabMy()
    {
        if (freeOyatuPanel != null) freeOyatuPanel.SetActive(false);
        if (paidOyatuPanel != null) paidOyatuPanel.SetActive(true);
        if (freeTabImage != null)   freeTabImage.color = unselectedTabColor;
        if (paidTabImage != null)   paidTabImage.color = selectedTabColor;

        // ★OnTabFree と対になっている。片方だけ直すと必ず食い違うので、変えるときは両方を見ること
        if (freeTabLabel != null) freeTabLabel.color = unselectedTabTextColor;
        if (paidTabLabel != null) paidTabLabel.color = selectedTabTextColor;

        // ★マイおやつには1日6回の上限が無いので、案内文は隠す
        if (naviText != null) naviText.SetActive(false);
    }

    // ── おやつ選択 ──────────────────────────────────────────────────────────────

    public void OnSelectOyatu(string oyatuId)
    {
        _selectedOyatu = AllOyatu.Find(o => o.id == oyatuId);
        if (_selectedOyatu == null) return;

        // 自動生成したボタン
        foreach (var kv in _buttons) kv.Value.SetSelected(kv.Key == oyatuId);

        // Scene に手で置いたボタン（Prefab 未結線のときの経路）
        UpdateSelectBadgesInPanel(freeOyatuPanel, oyatuId);
        UpdateSelectBadgesInPanel(paidOyatuPanel, oyatuId);

        if (selectOyatuName != null)
            selectOyatuName.text = _selectedOyatu.displayName;
        if (selectOyatuEffect != null)
            selectOyatuEffect.text = BuildEffectText(_selectedOyatu);
        if (selectOyatuImage != null)
        {
            // FormC → FormD → 正規化なし の順にフォールバック
            var tex = Resources.Load<Texture2D>($"FoodUI/{_selectedOyatu.imageName.Normalize(System.Text.NormalizationForm.FormC)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"FoodUI/{_selectedOyatu.imageName.Normalize(System.Text.NormalizationForm.FormD)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"FoodUI/{_selectedOyatu.imageName}");
            selectOyatuImage.texture = tex;
        }
    }

    // ── あげる ──────────────────────────────────────────────────────────────────

    public void OnGiveOyatu()
    {
        if (_selectedOyatu == null) return;

        var save   = SaveManager.Instance?.Data;
        var status = GameContext.Instance?.PetStatus;
        if (save == null || status == null) return;

        // 無償おやつの1日上限チェック
        if (_selectedOyatu.isFree)
        {
            // ★S-3（2026/8/31）：ここは「読むだけ」。
            //   この下にはコインが足りずに return する経路があるため、
            //   ここで日付を書き換えると「あげていないのに今日あげた扱い」になってしまう。
            if (DailyCounters.FreeOyatuToday(save) >= FreeOyatuDailyLimit)
            {
                careManager?.ShowNotice("今日のおやつは上限に達したよ…！");
                return;
            }
        }

        // ★U-9（2026/8/30）：在庫があれば在庫を優先して使う。コインもルナも減らさない。
        //   在庫はパズル（あそぶ画面）の報酬などで増える。
        //   在庫が無いときだけ、今までどおり その場で買って食べる。
        //   ★どちらの経路で消費したかは必ず1行ログに出す（黙って挙動が変わらないようにするため）。
        bool usedFromStock = OyatuInventory.TryUse(save, _selectedOyatu.id);

        if (usedFromStock)
        {
            Debug.Log($"<color=#00E5FF>[決定]</color> [Care] 在庫から {_selectedOyatu.displayName} を1つ使いました" +
                      $"（残り {OyatuInventory.Get(save, _selectedOyatu.id)}個・コインは消費していません）");
        }
        else
        {
            // コイン / ルナストーン消費
            if (_selectedOyatu.coinCost > 0)
            {
                if (!GameData.Instance.UseCoin(_selectedOyatu.coinCost))
                {
                    careManager?.ShowNotice("コインが足りないよ…！");
                    return;
                }
            }
            else if (_selectedOyatu.lunaCost > 0)
            {
                if (!GameData.Instance.UseLunaStone(_selectedOyatu.lunaCost))
                {
                    careManager?.ShowNotice("ルナストーンが足りないよ…！");
                    return;
                }
            }
        }

        // ステータス更新
        if (_selectedOyatu.fullRecovery)
        {
            status.AddHunger(100f);
            status.AddEnergy(100f);
        }
        else
        {
            if (_selectedOyatu.hungerAmount != 0) status.AddHunger(_selectedOyatu.hungerAmount);
            if (_selectedOyatu.energyAmount != 0) status.AddEnergy(_selectedOyatu.energyAmount);
        }

        // 最後にごはんをあげた時刻を更新（放置日数の判定用）
        status.OnFed();

        // 信頼度加算
        if (_selectedOyatu.trustAmount > 0)
            status.AddTrust(_selectedOyatu.trustAmount);

        // 性格パラメータ即時反映
        ApplyPersonality(save, _selectedOyatu);

        // 無償おやつ使用カウント更新
        // ★S-3（2026/8/31）：実際にあげたときだけ回数と日付が進む
        if (_selectedOyatu.isFree)
            DailyCounters.ConsumeFreeOyatu(save);

        GameContext.Instance?.SavePetStatus();
        QuestManager.Instance?.NotifyFeed();

        if (careManager != null && PopupMap.TryGetValue(_selectedOyatu.id, out var popup))
        {
            if (popup.useHunger) careManager.ShowHungerPopup(popup.text);
            else                 careManager.ShowEnergyPopup(popup.text);
        }

        careManager?.ShowNotice($"{_selectedOyatu.displayName}をあげたよ！");
        careManager?.RefreshAll();
        RefreshStockTexts();   // ★U-9：使ったぶんを表示へ反映する
        HidePanel();
        // ぽこは carePokoController、それ以外は careCharacterAction が担当する。
        // 担当でないほうは中で何もしないので、両方呼んでよい
        carePokoController?.PlayEat();
        careCharacterAction?.PlayEat();
    }

    // ── プライベートヘルパー ────────────────────────────────────────────────────

    private void ApplyPersonality(SaveData save, OyatuData data)
    {
        // ★範囲は -100〜+100（requirements.md §6 / SaveData.cs:132-136 のコメント）。
        //   2026/8/30 まで下限が 0 になっており、マイナス側の性格が有料おやつ1個で 0 へ飛んでいた。
        //   例：える（甘えん坊度 -50）にいちごケーキ(+5) → Clamp(-45, 0, 100) = 0 で実質 +50 になり、
        //       「クールな子」「ツンデレな子」などマイナス側の性格が二度と出せなくなっていた。
        //   ★お風呂側（BathWashManager.ApplyPersonality）は元から -100,100。片方だけ直さないこと。
        if (data.personalityActivity    != 0) save.personalityActivity    = Mathf.Clamp(save.personalityActivity    + data.personalityActivity,    -100, 100);
        if (data.personalityDependency  != 0) save.personalityDependency  = Mathf.Clamp(save.personalityDependency  + data.personalityDependency,  -100, 100);
        if (data.personalityHonesty     != 0) save.personalityHonesty     = Mathf.Clamp(save.personalityHonesty     + data.personalityHonesty,     -100, 100);
        if (data.personalityDiligence   != 0) save.personalityDiligence   = Mathf.Clamp(save.personalityDiligence   + data.personalityDiligence,   -100, 100);
        if (data.personalitySensitivity != 0) save.personalitySensitivity = Mathf.Clamp(save.personalitySensitivity + data.personalitySensitivity, -100, 100);
    }

    /// <summary>
    /// そのおやつの所持数を返す。おやつパネルの在庫表示から呼ぶ想定。
    /// ★実体は OyatuInventory。ここは画面から呼びやすくするための入口。
    /// </summary>
    public int GetStock(string oyatuId)
        => OyatuInventory.Get(SaveManager.Instance?.Data, oyatuId);

    /// <summary>
    /// 各おやつボタンの「あと○こ」表示を更新する。★U-9（2026/8/30）
    ///
    /// 【結線が要らない理由】
    ///   SelectBadge と同じで、ボタンの子から名前で探す方式にしてある。
    ///   おやつが増えても ButtonNameToId に1行足すだけで済み、
    ///   Inspector の結線を増やさなくてよい。
    ///
    /// 【置き場所】各ボタンの下の "Stock/StockText"（TextMeshProUGUI）。
    ///   見つからないボタンは黙って飛ばす（まだ作っていない場合があるため）。
    /// </summary>
    private void RefreshStockTexts()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null) return;

        // 自動生成したボタン
        foreach (var kv in _buttons) kv.Value.SetStockLabel(OyatuInventory.StockLabel(save, kv.Key));

        // Scene に手で置いたボタン（Prefab 未結線のときの経路）
        RefreshStockTextsInPanel(freeOyatuPanel, save);
        RefreshStockTextsInPanel(paidOyatuPanel, save);
    }

    private void RefreshStockTextsInPanel(GameObject panel, SaveData save)
    {
        if (panel == null) return;

        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (!ButtonNameToId.TryGetValue(btn.gameObject.name, out string id)) continue;

            var t = btn.transform.Find("Stock/StockText");
            if (t == null) continue;

            var label = t.GetComponent<TextMeshProUGUI>();
            if (label == null) continue;

            label.text = OyatuInventory.StockLabel(save, id);
        }
    }

    private void UpdateSelectBadgesInPanel(GameObject panel, string selectedId)
    {
        if (panel == null) return;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (!ButtonNameToId.TryGetValue(btn.gameObject.name, out string btnId)) continue;
            var badge = btn.transform.Find("SelectBadge");
            if (badge != null)
                badge.gameObject.SetActive(btnId == selectedId);
        }
    }

    private void ClearSelectBadges()
    {
        UpdateSelectBadgesInPanel(freeOyatuPanel, "");
        UpdateSelectBadgesInPanel(paidOyatuPanel, "");
    }

    private string BuildEffectText(OyatuData data)
    {
        var sb = new StringBuilder();

        if (data.fullRecovery)
        {
            // 数値がない表示には pt を付けない（requirements.md §5「パラメータの内部名と表示名」）
            sb.AppendLine($"{ParamNames.Hunger} 全回復");
            sb.AppendLine($"{ParamNames.Energy} 全回復");
        }
        else
        {
            if (data.hungerAmount != 0) sb.AppendLine($"{ParamNames.Hunger} {ParamNames.Pt(data.hungerAmount)}");
            if (data.energyAmount != 0) sb.AppendLine($"{ParamNames.Energy} {ParamNames.Pt(data.energyAmount)}");
        }

        if (data.trustAmount > 0) sb.AppendLine($"{ParamNames.Trust} {ParamNames.Pt(data.trustAmount)}");

        // 性格パラ表示（全て同値なら「全性格パラ」でまとめる）
        int act = data.personalityActivity;
        int dep = data.personalityDependency;
        int hon = data.personalityHonesty;
        int dil = data.personalityDiligence;
        int sen = data.personalitySensitivity;
        bool allSame = act != 0 && act == dep && act == hon && act == dil && act == sen;
        if (allSame)
        {
            sb.AppendLine($"全性格パラ <color=#FF69B4>{ParamNames.Pt(act)}</color>");
        }
        else
        {
            if (act != 0) sb.AppendLine($"{ParamNames.Activity} <color=#FF69B4>{ParamNames.Pt(act)}</color>");
            if (dep != 0) sb.AppendLine($"{ParamNames.Dependency} <color=#FF69B4>{ParamNames.Pt(dep)}</color>");
            if (hon != 0) sb.AppendLine($"{ParamNames.Honesty} <color=#FF69B4>{ParamNames.Pt(hon)}</color>");
            if (dil != 0) sb.AppendLine($"{ParamNames.Diligence} <color=#FF69B4>{ParamNames.Pt(dil)}</color>");
            if (sen != 0) sb.AppendLine($"{ParamNames.Sensitivity} <color=#FF69B4>{ParamNames.Pt(sen)}</color>");
        }

        return sb.ToString().TrimEnd();
    }
}

// 各おやつボタン GameObject にアタッチして oyatuId を設定する
public class OyatuButtonId : MonoBehaviour
{
    public string oyatuId;
}
