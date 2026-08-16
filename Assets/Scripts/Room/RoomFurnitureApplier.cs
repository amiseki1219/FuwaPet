using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カテゴリと、対応する Room_Base のスロットを紐づける1件ぶんのデータ。
/// Inspector 上でカテゴリを選び、そこにスロットの Transform をドラッグする。
/// </summary>
[System.Serializable]
public class FurnitureSlotBinding
{
    public FurnitureCategory category;

    [Tooltip("Room_Base の中の FurnitureSlot_〇〇 をドラッグする")]
    public Transform slot;
}

/// <summary>
/// スロットの中身を差し替えるだけの部品。UI もカメラも知らない。
///
/// 【なぜ UI と分けるか】
///   「差し替え」だけを単独でテストできるようにするため。
///   UI ができる前に、Inspector の右クリックメニューから動作確認できる。
///
/// 【付ける場所】
///   RoomEdit シーンに空の GameObject（例: RoomEditSystem）を作ってアタッチする。
///   ★Room_Base.prefab には付けないこと。Main シーンにも影響してしまうため。
/// </summary>
public class RoomFurnitureApplier : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private FurnitureCatalog catalog;

    [Tooltip("11カテゴリぶんのスロットをここに登録する")]
    [SerializeField] private FurnitureSlotBinding[] slots = new FurnitureSlotBinding[0];

    [Header("デバッグ")]
    [SerializeField] private bool verboseLog = true;

    /// <summary>使っているカタログ。Loader などが参照する。</summary>
    public FurnitureCatalog Catalog => catalog;

    // カテゴリからスロットを高速に引くための表。Awake で1回だけ作る
    private Dictionary<FurnitureCategory, Transform> _slotMap;

    // いま各スロットに入っているアイテムID。「もどす」やセーブで使う
    private readonly Dictionary<FurnitureCategory, string> _currentIds
        = new Dictionary<FurnitureCategory, string>();

    // 入室した瞬間の状態。「もどす」を押したらここへ戻す
    private Dictionary<FurnitureCategory, string> _snapshot;

    private void Awake()
    {
        BuildSlotMap();
    }

    private void BuildSlotMap()
    {
        _slotMap = new Dictionary<FurnitureCategory, Transform>();

        if (slots == null) return;

        foreach (var b in slots)
        {
            if (b == null || b.slot == null) continue;

            // 同じカテゴリを2回登録していたら、後勝ちにせず警告して最初のを使う
            if (_slotMap.ContainsKey(b.category))
            {
                Debug.LogError($"[RoomEdit] カテゴリ {b.category} のスロットが複数登録されています", this);
                continue;
            }
            _slotMap[b.category] = b.slot;
        }

        // 登録漏れの検出。ここで気づけないと、実機で「そのカテゴリだけ反応しない」になる
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            if (!_slotMap.ContainsKey(c))
                Debug.LogWarning($"[RoomEdit] カテゴリ {c} のスロットが未登録です", this);
        }
    }

    // ─────────────────────────────────────────────
    // 差し替え
    // ─────────────────────────────────────────────

    /// <summary>
    /// 指定カテゴリのスロットに、指定IDの家具を入れる。
    /// IDが見つからない場合はそのカテゴリの既定アイテムにフォールバックする（落とさない）。
    /// </summary>
    /// <returns>実際に何かを配置できたら true</returns>
    public bool Apply(FurnitureCategory category, string itemId)
    {
        if (catalog == null)
        {
            Debug.LogError("[RoomEdit] カタログが未設定です", this);
            return false;
        }

        if (_slotMap == null) BuildSlotMap();

        if (!_slotMap.TryGetValue(category, out var slot) || slot == null)
        {
            Debug.LogError($"[RoomEdit] {category} のスロットが見つかりません", this);
            return false;
        }

        // ★セーブデータに未知のIDが入っていても落とさない。
        //   アイテムを整理した／IDを変えた場合にここへ来る。
        var entry = catalog.FindById(itemId);
        if (entry == null || entry.category != category)
        {
            var fallback = catalog.GetDefault(category);
            if (entry == null && !string.IsNullOrEmpty(itemId))
                Debug.LogWarning($"[RoomEdit] ID '{itemId}' が見つからないので既定にフォールバックします", this);
            entry = fallback;
        }

        if (entry == null)
        {
            Debug.LogError($"[RoomEdit] {category} に置けるアイテムが1件もありません", this);
            return false;
        }

        if (entry.prefab == null)
        {
            Debug.LogError($"[RoomEdit] {entry.id} の Prefab が未設定です", this);
            return false;
        }

        // ★必ず「中を空にしてから」入れる。
        //   これを忘れるとスロットに2つ入って二重表示になる。
        ClearSlot(slot);

        // 第3引数 false = Prefab が持っている位置・回転・スケールをそのまま使う。
        // true にすると「今のワールド座標を維持」してしまい、狙った位置に入らない。
        var instance = Instantiate(entry.prefab, slot, false);
        instance.name = entry.prefab.name; // "(Clone)" を消して Hierarchy を読みやすくする

        _currentIds[category] = entry.id;

        if (verboseLog)
            Debug.Log($"[RoomEdit] {category} ← {entry.id}", this);

        return true;
    }

    /// <summary>指定カテゴリを既定のアイテムに戻す。</summary>
    public void ApplyDefault(FurnitureCategory category)
    {
        var def = catalog != null ? catalog.GetDefault(category) : null;
        Apply(category, def != null ? def.id : null);
    }

    /// <summary>スロットの子を全部消す。</summary>
    private void ClearSlot(Transform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            var child = slot.GetChild(i).gameObject;

            // 再生中は Destroy（フレーム末に消える）、エディタ上のテストでは即時削除
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    /// <summary>
    /// そのカテゴリのスロットを返す。未登録なら null。
    /// 強調表示をスロットの位置に置くときなどに使う。
    /// </summary>
    public Transform GetSlot(FurnitureCategory category)
    {
        if (_slotMap == null) BuildSlotMap();
        return _slotMap.TryGetValue(category, out var t) ? t : null;
    }

    /// <summary>いまそのカテゴリに入っているアイテムID。未設定なら null。</summary>
    public string GetCurrentId(FurnitureCategory category)
    {
        return _currentIds.TryGetValue(category, out var id) ? id : null;
    }

    /// <summary>いまの全カテゴリの状態を取り出す。セーブするときに使う。</summary>
    public Dictionary<FurnitureCategory, string> GetCurrentAll()
    {
        return new Dictionary<FurnitureCategory, string>(_currentIds);
    }

    /// <summary>
    /// 全カテゴリをまとめて適用する。セーブデータからの復元で使う。
    /// data に無いカテゴリは既定アイテムになる。
    /// </summary>
    public void ApplyAll(IDictionary<FurnitureCategory, string> data)
    {
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            string id = null;
            data?.TryGetValue(c, out id);
            Apply(c, id); // id が null でも既定にフォールバックする
        }
    }

    // ─────────────────────────────────────────────
    // 「もどす」用
    // ─────────────────────────────────────────────

    /// <summary>入室時の状態を覚える。編集を始める前に1回呼ぶ。</summary>
    public void TakeSnapshot()
    {
        _snapshot = new Dictionary<FurnitureCategory, string>(_currentIds);
        if (verboseLog) Debug.Log($"[RoomEdit] 状態を記録しました（{_snapshot.Count}件）", this);
    }

    /// <summary>覚えておいた状態へ戻す。</summary>
    public void RestoreSnapshot()
    {
        if (_snapshot == null)
        {
            Debug.LogWarning("[RoomEdit] 記録された状態がありません", this);
            return;
        }
        ApplyAll(_snapshot);
    }

    /// <summary>入室時から何か変わっているか。「保存しますか？」の判定に使う。</summary>
    public bool HasChanges()
    {
        if (_snapshot == null) return false;

        foreach (var kv in _currentIds)
        {
            _snapshot.TryGetValue(kv.Key, out var before);
            if (before != kv.Value) return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // UI が無い段階での動作確認用
    // Inspector のコンポーネント名を右クリックすると出てくる
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("テスト: 全カテゴリを既定に戻す")]
    private void TestApplyAllDefault()
    {
        BuildSlotMap();
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
            ApplyDefault(c);
    }

    [ContextMenu("テスト: お部屋を ここ に変える")]
    private void TestRoomShellKoko()
    {
        BuildSlotMap();
        Apply(FurnitureCategory.RoomShell, "RoomShell_Koko");
    }

    [ContextMenu("テスト: お部屋を ぽこ に変える")]
    private void TestRoomShellPoko()
    {
        BuildSlotMap();
        Apply(FurnitureCategory.RoomShell, "RoomShell_Poko");
    }

    [ContextMenu("テスト: スロットの登録状況を出力")]
    private void TestDumpSlots()
    {
        BuildSlotMap();
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            _slotMap.TryGetValue(c, out var t);
            Debug.Log($"[RoomEdit] {c,-12} → {(t != null ? t.name : "★未登録")}", this);
        }
    }
#endif
}
