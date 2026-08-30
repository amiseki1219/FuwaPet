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

    // ── 初期化 ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (freeTabImage != null)   freeTabImage.color = selectedTabColor;
        if (paidTabImage != null)   paidTabImage.color = unselectedTabColor;
        if (closeButton  != null)   closeButton.onClick.AddListener(HidePanel);
    }

    // ── パネル開閉 ──────────────────────────────────────────────────────────────

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        OnTabFree();
        OnSelectOyatu("niboshi");
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
    }

    public void OnTabMy()
    {
        if (freeOyatuPanel != null) freeOyatuPanel.SetActive(false);
        if (paidOyatuPanel != null) paidOyatuPanel.SetActive(true);
        if (freeTabImage != null)   freeTabImage.color = unselectedTabColor;
        if (paidTabImage != null)   paidTabImage.color = selectedTabColor;
    }

    // ── おやつ選択 ──────────────────────────────────────────────────────────────

    public void OnSelectOyatu(string oyatuId)
    {
        _selectedOyatu = AllOyatu.Find(o => o.id == oyatuId);
        if (_selectedOyatu == null) return;

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
            ResetFreeCountIfNewDay(save);
            if (save.freeOyatuCountToday >= FreeOyatuDailyLimit)
            {
                careManager?.ShowNotice("今日のおやつは上限に達したよ…！");
                return;
            }
        }

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
        if (_selectedOyatu.isFree)
            save.freeOyatuCountToday++;

        GameContext.Instance?.SavePetStatus();
        QuestManager.Instance?.NotifyFeed();

        if (careManager != null && PopupMap.TryGetValue(_selectedOyatu.id, out var popup))
        {
            if (popup.useHunger) careManager.ShowHungerPopup(popup.text);
            else                 careManager.ShowEnergyPopup(popup.text);
        }

        careManager?.ShowNotice($"{_selectedOyatu.displayName}をあげたよ！");
        careManager?.RefreshAll();
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

    private void ResetFreeCountIfNewDay(SaveData save)
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (save.lastFreeOyatuDate != today)
        {
            save.freeOyatuCountToday = 0;
            save.lastFreeOyatuDate   = today;
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
