using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// おやつ選択パネルの「ボタン1個ぶん」の見た目を担当する部品。
/// Assets/Prefabs/Oyatu/OyatuButton.prefab に付ける。
///
/// 【なぜ Prefab ＋ コード生成にするのか】2026/8/30（U-14）
///   それまでは9個のボタンを Scene に手で並べ、名前・価格を手打ちしていた。
///   そのため OyatuManager の AllOyatu で価格を直しても、
///   ボタンの表示は古いままという食い違いが起きていた（実際に起きた）。
///   おやつを1種類増やすのに「ボタンを作る／対応表に足す／AllOyatu に足す」の3箇所が必要だった。
///   → データ（AllOyatu）を正として、ボタンはそこから作る形にそろえる。
///
/// 【この部品の役割】
///   受け取ったデータを、中の Text と Image に流し込むだけ。
///   おやつの効果や消費の判断はしない（それは OyatuManager の担当）。
/// </summary>
public class OyatuButtonView : MonoBehaviour
{
    [Header("結線（Prefab 内のオブジェクト）")]
    [Tooltip("このボタン自身の Button。OyatuButton に付いているもの")]
    [SerializeField] private Button button;

    [Tooltip("おやつの絵。★ボタン自身の Image を入れる")]
    [SerializeField] private Image iconImage;

    [Tooltip("おやつの名前。OyatuButton/Text (TMP)")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("通貨アイコン。OyatuButton/Coin/Image")]
    [SerializeField] private Image currencyIcon;

    [Tooltip("価格の数字。OyatuButton/Coin/Text (TMP)")]
    [SerializeField] private TextMeshProUGUI priceText;

    [Tooltip("所持数。OyatuButton/Stock/StockText")]
    [SerializeField] private TextMeshProUGUI stockText;

    [Tooltip("「選択中」の飾り。OyatuButton/SelectBadge")]
    [SerializeField] private GameObject selectBadge;

    [Header("通貨アイコンの絵（2枚とも入れる）")]
    [Tooltip("無償コイン🪙 の絵")]
    [SerializeField] private Sprite coinSprite;

    [Tooltip("有償コイン♡（ルナストーン）の絵")]
    [SerializeField] private Sprite lunaSprite;

    /// <summary>このボタンが担当するおやつの id（"niboshi" など）。</summary>
    public string Id { get; private set; }

    private Action<string> _onClick;

    /// <summary>
    /// データを流し込む。OyatuManager が生成直後に1回呼ぶ。
    /// </summary>
    public void Bind(OyatuData data, string stockLabel, Action<string> onClick)
    {
        if (data == null) return;

        Id       = data.id;
        _onClick = onClick;

        gameObject.name = $"OyatuButton_{data.id}";   // Hierarchy を読みやすくする

        if (nameText != null) nameText.text = data.displayName;

        // 価格と通貨アイコン。coinCost が入っていれば無償コイン、そうでなければルナストーン
        bool isCoin = data.coinCost > 0;
        int  price  = isCoin ? data.coinCost : data.lunaCost;

        if (priceText != null) priceText.text = price.ToString();

        if (currencyIcon != null)
        {
            var sprite = isCoin ? coinSprite : lunaSprite;
            if (sprite != null) currencyIcon.sprite = sprite;
            else Debug.LogWarning($"[Care] OyatuButton の通貨アイコンが未結線です（{(isCoin ? "coinSprite" : "lunaSprite")}）。" +
                                  $"Prefab の OyatuButtonView に2枚とも入れてください", this);
        }

        ApplyIcon(data);
        SetStockLabel(stockLabel);
        SetSelected(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(Id));
        }
    }

    /// <summary>
    /// ボタンの絵を Resources/FoodButtonUI から読む。
    ///
    /// ★見つからなければ今の絵のままにする（差し替えないだけで、ボタンは動く）。
    /// ★macOS は日本語ファイル名を NFD（濁点を分解した形）で保存するため、
    ///   NFC → NFD → そのまま の順に3回試す（CLAUDE.md「Resources 読み込み規約」と同じ手順）。
    /// ★Resources.Load&lt;Sprite&gt; は Sprite Mode = Single でないと null を返す。
    /// </summary>
    private void ApplyIcon(OyatuData data)
    {
        if (iconImage == null || string.IsNullOrEmpty(data.imageName)) return;

        var sprite =
            Resources.Load<Sprite>($"FoodButtonUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormC)}") ??
            Resources.Load<Sprite>($"FoodButtonUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormD)}") ??
            Resources.Load<Sprite>($"FoodButtonUI/{data.imageName}");

        if (sprite != null)
        {
            iconImage.sprite = sprite;
            return;
        }

        Debug.LogWarning($"[Care] Resources/FoodButtonUI/{data.imageName} が読めませんでした。" +
                         $"ボタンの絵は今のままにします。" +
                         $"（ファイルがあるか、Texture Type=Sprite・Sprite Mode=Single か確認してください）", this);
    }

    /// <summary>
    /// 所持数の表示を更新する。
    /// ★文言は OyatuInventory.StockLabel が決める（0〜9=「3こ」/ 10=「いっぱい」）。
    ///   ここで組み立てないこと。2箇所で作ると必ず食い違う。
    /// </summary>
    public void SetStockLabel(string label)
    {
        if (stockText != null) stockText.text = label;
    }

    /// <summary>「選択中」の飾りを出し入れする。</summary>
    public void SetSelected(bool selected)
    {
        if (selectBadge != null) selectBadge.SetActive(selected);
    }
}
