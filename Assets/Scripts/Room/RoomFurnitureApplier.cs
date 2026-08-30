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

    [Tooltip("同じカテゴリでスロットを複数持つときの番号。\n" +
             "スロットが1つだけのカテゴリは 0 のままにする。\n" +
             "例）かべかざり：かべA = 0 / かべB = 1")]
    public int slotIndex = 0;

    [Tooltip("Room_Base の中の FurnitureSlot_〇〇 をドラッグする")]
    public Transform slot;

    [Tooltip("その家具の下に敷いている接地影（FakeShadows の中の Shadow_〇〇）。\n" +
             "「なし」を選んだときに自動で消すために使う。\n" +
             "★影を用意していないカテゴリは空のままでよい（空なら何もしない）")]
    public GameObject shadow;
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

    [Tooltip("全カテゴリぶんのスロットをここに登録する。\n" +
             "かべかざりのようにスロットが2つあるカテゴリは、行を2つ作って番号を 0 / 1 にする")]
    [SerializeField] private FurnitureSlotBinding[] slots = new FurnitureSlotBinding[0];

    [Tooltip("時間帯でライトを切り替える担当。Room_Base の WindowViewController。\n" +
             "★未結線でもシーン内から自動で探すので、ふつうは触らなくてよい")]
    [SerializeField] private WindowViewController windowView;

    [Header("デバッグ")]
    [SerializeField] private bool verboseLog = true;

    /// <summary>使っているカタログ。Loader などが参照する。</summary>
    public FurnitureCatalog Catalog => catalog;

    // ★どの表も「カテゴリ」ではなく「SlotKey（カテゴリ＋番号）」で引く。
    //   かべかざりのように1カテゴリに2スロットあるものを、同じ仕組みで扱うため。

    // SlotKey からスロットを高速に引くための表。Awake で1回だけ作る
    private Dictionary<SlotKey, Transform> _slotMap;

    // SlotKey から接地影を引く表。影を登録していないスロットはここに入らない
    private Dictionary<SlotKey, GameObject> _shadowMap;

    // カテゴリごとのスロット番号一覧。UI が「かべA/かべB のタブを何個出すか」を決めるのに使う
    private Dictionary<FurnitureCategory, List<int>> _indicesByCategory;

    // いま各スロットに入っているアイテムID。「もどす」やセーブで使う
    private readonly Dictionary<SlotKey, string> _currentIds
        = new Dictionary<SlotKey, string>();

    // 入室した瞬間の状態。「もどす」を押したらここへ戻す
    private Dictionary<SlotKey, string> _snapshot;

    private void Awake()
    {
        BuildSlotMap();
    }

    private void BuildSlotMap()
    {
        _slotMap = new Dictionary<SlotKey, Transform>();
        _shadowMap = new Dictionary<SlotKey, GameObject>();
        _indicesByCategory = new Dictionary<FurnitureCategory, List<int>>();

        if (slots == null) return;

        foreach (var b in slots)
        {
            if (b == null || b.slot == null) continue;

            if (b.slotIndex < 0)
            {
                Debug.LogError($"[RoomEdit] {b.category} のスロット番号が負の値です（{b.slotIndex}）", this);
                continue;
            }

            var key = new SlotKey(b.category, b.slotIndex);

            // 同じ番号を2回登録していたら、後勝ちにせず警告して最初のを使う。
            // ★スロットを増やすとき「番号を 0 のままコピーした」が一番ありがちなミス
            if (_slotMap.ContainsKey(key))
            {
                Debug.LogError($"[RoomEdit] {key} のスロットが複数登録されています。" +
                               $"スロット番号（Slot Index）が重複していないか確認してください", this);
                continue;
            }
            _slotMap[key] = b.slot;

            // 影は任意。登録されているスロットだけ表に入れる
            if (b.shadow != null) _shadowMap[key] = b.shadow;

            if (!_indicesByCategory.TryGetValue(b.category, out var list))
            {
                list = new List<int>();
                _indicesByCategory[b.category] = list;
            }
            list.Add(b.slotIndex);
        }

        // 番号順に並べておく。UI のタブがこの順に出る
        foreach (var kv in _indicesByCategory) kv.Value.Sort();

        // 登録漏れの検出。ここで気づけないと、実機で「そのカテゴリだけ反応しない」になる
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            if (!_indicesByCategory.ContainsKey(c))
                Debug.LogWarning($"[RoomEdit] カテゴリ {c} のスロットが未登録です", this);
        }
    }

    /// <summary>
    /// そのカテゴリに登録されているスロット番号の一覧（小さい順）。
    /// 1つも無ければ空。UI が「タブを何個出すか」を決めるのに使う。
    /// </summary>
    public IReadOnlyList<int> GetSlotIndices(FurnitureCategory category)
    {
        if (_indicesByCategory == null) BuildSlotMap();
        return _indicesByCategory.TryGetValue(category, out var list)
            ? (IReadOnlyList<int>)list
            : System.Array.Empty<int>();
    }

    /// <summary>そのカテゴリのスロット数。1ならタブを出さない、2以上なら出す。</summary>
    public int GetSlotCount(FurnitureCategory category) => GetSlotIndices(category).Count;

    /// <summary>登録されている全スロットのキー。セーブの読み込みなどで全なめするときに使う。</summary>
    public List<SlotKey> GetAllSlotKeys()
    {
        if (_slotMap == null) BuildSlotMap();
        return new List<SlotKey>(_slotMap.Keys);
    }

    // ─────────────────────────────────────────────
    // 差し替え
    // ─────────────────────────────────────────────

    /// <summary>
    /// 指定スロットに、指定IDの家具を入れる。
    /// IDが見つからない場合はそのカテゴリの既定アイテムにフォールバックする（落とさない）。
    ///
    /// ★引数にカテゴリをそのまま渡せる（例: Apply(FurnitureCategory.Bed, id)）。
    ///   その場合は自動で「0番のスロット」になる。スロットが1つしかないカテゴリは常にこれ。
    /// </summary>
    /// <returns>実際に何かを配置できたら true</returns>
    public bool Apply(SlotKey key, string itemId)
    {
        if (catalog == null)
        {
            Debug.LogError("[RoomEdit] カタログが未設定です", this);
            return false;
        }

        if (_slotMap == null) BuildSlotMap();

        if (!_slotMap.TryGetValue(key, out var slot) || slot == null)
        {
            Debug.LogError($"[RoomEdit] {key} のスロットが見つかりません", this);
            return false;
        }

        var category = key.category;

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

        // ★「なし」を選んだ場合。スロットを空にして終わり。
        //   フォールバックさせない（ここでフォールバックすると「外せない」ことになる）
        if (entry.isEmptySlot)
        {
            ClearSlot(slot);
            SetShadowVisible(key, false);   // ★家具が無いのに影だけ残らないようにする
            _currentIds[key] = entry.id;

            // ★U-6：ナイトスタンドを外したら、時間帯ライトの参照も外す
            if (category == FurnitureCategory.Nightstand) HandOverNightstandLight(null);

            if (verboseLog) Debug.Log($"[RoomEdit] {key} ← なし（{entry.id}）", this);
            return true;
        }

        if (entry.prefab == null)
        {
            // ここに来る＝カタログの設定ミス。
            //   ・その家具を出したいのに Prefab を入れ忘れている
            //   ・「なし」の行なのに isEmptySlot にチェックを入れ忘れている
            // どちらにせよ何も置けないので、スロットは今の状態のまま変えない。
            Debug.LogError($"[RoomEdit] カタログの {entry.id}（{category}）に Prefab が入っていないので " +
                           $"切り替えられません。Prefab を設定するか、" +
                           $"「なし」の行なら isEmptySlot にチェックを入れてください", this);
            return false;
        }

        // ★必ず「中を空にしてから」入れる。
        //   これを忘れるとスロットに2つ入って二重表示になる。
        ClearSlot(slot);

        // 第3引数 false = Prefab が持っている位置・回転・スケールをそのまま使う。
        // true にすると「今のワールド座標を維持」してしまい、狙った位置に入らない。
        var instance = Instantiate(entry.prefab, slot, false);
        instance.name = entry.prefab.name; // "(Clone)" を消して Hierarchy を読みやすくする

        // ★U-6（2026/8/30）：ナイトスタンドの中のライトを、時間帯の担当へ渡す。
        //   ライトは家具 Prefab ごとに別実体なので、置き換えるたびに渡し直す必要がある。
        if (category == FurnitureCategory.Nightstand)
            HandOverNightstandLight(instance.GetComponentInChildren<Light>(true));

        SetShadowVisible(key, true);   // ★「なし」から戻したときに影も復活させる

        _currentIds[key] = entry.id;

        if (verboseLog)
            Debug.Log($"[RoomEdit] {key} ← {entry.id}", this);

        return true;
    }

    /// <summary>指定スロットを既定のアイテムに戻す。</summary>
    public void ApplyDefault(SlotKey key)
    {
        var def = catalog != null ? catalog.GetDefault(key.category) : null;
        Apply(key, def != null ? def.id : null);
    }

    /// <summary>
    /// そのスロットの接地影を出し入れする。
    /// 影を登録していないスロットでは何もしない（登録は任意なので警告も出さない）。
    /// </summary>
    private void SetShadowVisible(SlotKey key, bool visible)
    {
        if (_shadowMap == null) return;
        if (!_shadowMap.TryGetValue(key, out var shadow) || shadow == null) return;

        // 既に同じ状態なら触らない（無駄な SetActive で警告が出るのを避ける）
        if (shadow.activeSelf == visible) return;

        shadow.SetActive(visible);
    }

    /// <summary>スロットの子を全部消す。</summary>
    /// <summary>
    /// ナイトスタンドの中のライトを WindowViewController へ渡す。
    ///
    /// 【なぜ Nightstand だけか】2026/8/30 決定
    ///   いまライトを持つ家具 Prefab は Nightstand の6つだけ（Default / Poko / Eru / Koko / Paru / Piyoko）。
    ///   カテゴリを問わず拾う作りにすると、将来 RoomLight などにライトを足したとき、
    ///   後から置いたほうで上書きされて原因が分からなくなる。対象を絞っておく。
    ///
    /// 【どの経路で動いたかを必ず1行ログに出す】
    ///   黙って何もしない状態を作らないため。
    /// </summary>
    private void HandOverNightstandLight(Light light)
    {
        if (windowView == null)
        {
            // Inspector 未結線でも動くように、シーン内から1回だけ探す
            windowView = FindFirstObjectByType<WindowViewController>();
            if (windowView == null)
            {
                Debug.LogWarning("[RoomEdit] WindowViewController がシーンに見つからないため、" +
                                 "ナイトスタンドのライトは時間帯で切り替わりません", this);
                return;
            }
        }

        windowView.SetBookShelfLight(light);

        if (verboseLog)
            Debug.Log($"<color=#00E5FF>[決定]</color> [RoomEdit] ナイトスタンドのライトを時間帯の担当へ渡しました" +
                      $"（{(light != null ? light.name : "なし＝家具を外した")}）", this);
    }

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
    /// そのスロットの Transform を返す。未登録なら null。
    /// 強調表示をスロットの位置に置くときなどに使う。
    /// </summary>
    public Transform GetSlot(SlotKey key)
    {
        if (_slotMap == null) BuildSlotMap();
        return _slotMap.TryGetValue(key, out var t) ? t : null;
    }

    /// <summary>いまそのスロットに入っているアイテムID。未設定なら null。</summary>
    public string GetCurrentId(SlotKey key)
    {
        return _currentIds.TryGetValue(key, out var id) ? id : null;
    }

    /// <summary>いまの全スロットの状態を取り出す。セーブするときに使う。</summary>
    public Dictionary<SlotKey, string> GetCurrentAll()
    {
        return new Dictionary<SlotKey, string>(_currentIds);
    }

    /// <summary>
    /// 登録されている全スロットをまとめて適用する。セーブデータからの復元で使う。
    /// data に無いスロットは既定アイテムになる。
    ///
    /// ★enum を総なめするのではなく「登録済みのスロット」だけを回す。
    ///   スロットを結線していないカテゴリまで触ると、手置きの家具を消してしまうため。
    /// </summary>
    public void ApplyAll(IDictionary<SlotKey, string> data)
    {
        foreach (var key in GetAllSlotKeys())
        {
            string id = null;
            data?.TryGetValue(key, out id);
            Apply(key, id); // id が null でも既定にフォールバックする
        }
    }

    // ─────────────────────────────────────────────
    // 「もどす」用
    // ─────────────────────────────────────────────

    /// <summary>入室時の状態を覚える。編集を始める前に1回呼ぶ。</summary>
    public void TakeSnapshot()
    {
        _snapshot = new Dictionary<SlotKey, string>(_currentIds);
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
    [ContextMenu("テスト: 全スロットを既定に戻す")]
    private void TestApplyAllDefault()
    {
        BuildSlotMap();
        foreach (var key in GetAllSlotKeys())
            ApplyDefault(key);
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
            var indices = GetSlotIndices(c);
            if (indices.Count == 0)
            {
                Debug.LogWarning($"[RoomEdit] {c,-12} → ★未登録", this);
                continue;
            }

            foreach (int i in indices)
            {
                var key = new SlotKey(c, i);
                _slotMap.TryGetValue(key, out var t);
                _shadowMap.TryGetValue(key, out var sh);
                Debug.Log($"[RoomEdit] {key,-14} → {(t != null ? t.name : "★未登録")}" +
                          $"　影: {(sh != null ? sh.name : "なし")}", this);
            }
        }
    }
#endif
}
