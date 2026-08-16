using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム一覧に並ぶボタン1個ぶんの見た目を担当する。
///
/// 【設計の考え方】
///   このスクリプトは「言われた内容を表示する」だけで、
///   どの家具を選んだか・どう反映するかは一切知らない。
///   判断は RoomEditController 側に集約する。
///
/// 【未所持アイテムの扱い】
///   未所持でも「押せる」。暗くして鍵を出すだけで、選択とプレビューは可能。
///   保存できないことは、決定ボタンを押したときにアラートで伝える。
///   ここでボタンを押せなくしてしまうと、試着すらできず購買につながらない。
///
/// 【付ける場所】
///   ItemButton.prefab のルート
/// </summary>
public class ItemButtonView : MonoBehaviour
{
    [Header("結線（すべて ItemButton の子）")]
    [Tooltip("家具のサムネイル画像")]
    [SerializeField] private Image iconImage;

    [Tooltip("家具の名前")]
    [SerializeField] private TMP_Text label;

    [Tooltip("選択中に表示する枠。初期は非アクティブにしておく")]
    [SerializeField] private GameObject selectedFrame;

    [Tooltip("未所持のときにかぶせる覆い（暗幕＋鍵マーク）。初期は非アクティブにしておく")]
    [SerializeField] private GameObject lockedOverlay;

    [Tooltip("ルートに付いている Button。空なら自動で探す")]
    [SerializeField] private Button button;

    [Header("見た目")]
    [Tooltip("未所持のときにサムネをどれくらい暗くするか（1 = そのまま）")]
    [Range(0.3f, 1f)]
    [SerializeField] private float lockedIconBrightness = 0.55f;

    /// <summary>このボタンが担当している家具のID。押されたときに通知する値。</summary>
    public string ItemId { get; private set; }

    /// <summary>所持しているか。決定ボタンの可否判定に使う。</summary>
    public bool IsOwned { get; private set; } = true;

    // 押されたときに呼ぶ相手。Bind() のたびに差し替わる
    private Action<string> _onClick;

    private void Awake()
    {
        // 結線を忘れても動くように保険をかけておく
        if (button == null) button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("[ItemButton] Button が見つかりません", this);
            return;
        }

        // ★ここで1回だけ登録する。
        //   Bind() のたびに AddListener すると、使い回すたびにリスナーが増えて
        //   「1回押したら何回も反応する」バグになる。
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        // Bind 前に押された場合に備えて null チェック
        _onClick?.Invoke(ItemId);
    }

    /// <summary>
    /// このボタンに家具1件ぶんの内容を流し込む。
    /// プールから取り出して使い回すたびに呼ばれる。
    /// </summary>
    /// <param name="entry">表示する家具</param>
    /// <param name="isSelected">いま部屋に置かれているものかどうか</param>
    /// <param name="isOwned">所持しているか。false なら暗くして鍵を出す（押せることは変わらない）</param>
    /// <param name="onClick">押されたときに呼ぶ処理。引数はアイテムID</param>
    public void Bind(FurnitureEntry entry, bool isSelected, bool isOwned, Action<string> onClick)
    {
        if (entry == null)
        {
            Debug.LogError("[ItemButton] entry が null です", this);
            return;
        }

        ItemId = entry.id;
        _onClick = onClick;

        // ── 名前 ──
        if (label != null)
        {
            // 表示名が未入力ならIDをそのまま出す（空欄より気づきやすい）
            label.text = string.IsNullOrEmpty(entry.displayName) ? entry.id : entry.displayName;
        }

        // ── サムネイル ──
        if (iconImage != null)
        {
            iconImage.sprite = entry.thumbnail;

            // サムネ未設定のときは Image ごと消す。
            // 消さないと白い四角が出て「画像が壊れている」ように見える。
            iconImage.enabled = (entry.thumbnail != null);
        }

        SetSelected(isSelected);
        SetOwned(isOwned);
    }

    /// <summary>選択中の枠を出し入れする。他のボタンが押されたときにも呼ばれる。</summary>
    public void SetSelected(bool isSelected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(isSelected);
    }

    /// <summary>
    /// 所持状態を反映する。
    /// ★押せる状態は変えない。未所持でもプレビューはさせる。
    /// </summary>
    public void SetOwned(bool isOwned)
    {
        IsOwned = isOwned;

        // 暗幕＋鍵マーク
        if (lockedOverlay != null) lockedOverlay.SetActive(!isOwned);

        // サムネ自体も少し暗くすると「持っていない感」が伝わりやすい
        if (iconImage != null)
        {
            float v = isOwned ? 1f : lockedIconBrightness;
            iconImage.color = new Color(v, v, v, 1f);
        }
    }

    private void OnDestroy()
    {
        // シーン破棄時に参照が残らないようにしておく
        if (button != null) button.onClick.RemoveListener(HandleClick);
        _onClick = null;
    }
}
