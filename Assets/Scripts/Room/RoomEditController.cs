using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// お部屋編集画面の司令塔。
///
/// 【画面の流れ】
///   入室        部屋全体 ＋ キャラ表示 ＋ カテゴリカード
///     ↓ カテゴリをタップ
///   キャラが消える → カメラがそのカテゴリの定位置へ → アイテム一覧が出る
///     ↓ アイテムをタップ
///   その場で差し替わる（＝プレビュー）
///     ↓ もどる
///   カメラが元へ → キャラが戻る → カテゴリカードに戻る
///
/// 【役割分担】
///   差し替えそのものは RoomFurnitureApplier が担当する。
///   このクラスは「いつ・何を差し替えるか」と画面の見せ方だけを持つ。
///
/// 【付ける場所】
///   RoomEdit シーンの空 GameObject（RoomFurnitureApplier と同じところでOK）
/// </summary>
public class RoomEditController : MonoBehaviour
{
    /// <summary>カテゴリ1つぶんの結線。カテゴリ・ボタン・カメラ位置をセットで持つ。</summary>
    [System.Serializable]
    public class CategoryBinding
    {
        public FurnitureCategory category;

        [Tooltip("FurnitureListCard の中の、このカテゴリのボタン")]
        public Button button;

        [Tooltip("このカテゴリを選んだときのカメラ位置（View_〇〇）。\n" +
                 "空にするとカメラを動かさない。お部屋（RoomShell）は空にする")]
        public Transform viewpoint;
    }

    [Header("── 参照 ──")]
    [SerializeField] private RoomFurnitureApplier applier;
    [SerializeField] private FurnitureCatalog catalog;

    [Tooltip("動かすカメラ。空なら Camera.main を使う")]
    [SerializeField] private Camera targetCamera;

    [Header("── UI ──")]
    [Tooltip("カテゴリ選択のカード")]
    [SerializeField] private GameObject categoryCard;

    [Tooltip("アイテム一覧のパネル。初期は非アクティブにしておく")]
    [SerializeField] private GameObject itemListPanel;

    [Tooltip("ItemListPanel > Scroll View > Viewport > Content")]
    [SerializeField] private RectTransform itemListContent;

    [Tooltip("Art/UI/Prefabs/ItemButton.prefab")]
    [SerializeField] private ItemButtonView itemButtonPrefab;

    [Tooltip("アイテム一覧の上に出すカテゴリ名")]
    [SerializeField] private TMP_Text categoryLabel;

    [SerializeField] private Button returnButton;
    [SerializeField] private Button decideButton;

    [Header("── スロット切り替え（かべA / かべB）──")]
    [Tooltip("スロットが2つ以上あるカテゴリのときだけ出す親。\n" +
             "スロットが1つしかないカテゴリでは自動で非表示になる。\n" +
             "使わないなら空のままでよい")]
    [SerializeField] private GameObject slotTabRoot;

    [Tooltip("スロット番号の順に並べる。0番目 = かべA、1番目 = かべB。\n" +
             "実際のスロット数より多く用意しておいてOK（余ったぶんは自動で隠れる）")]
    [SerializeField] private Button[] slotTabButtons;

    [Tooltip("選択中のタブに出す枠。slotTabButtons と同じ数・同じ順に入れる。\n" +
             "枠の演出を使わないなら空のままでよい")]
    [SerializeField] private GameObject[] slotTabSelectedMarks;

    [Tooltip("カテゴリ選択中だけ見せて、家具を編集している間は隠すもの。\n" +
             "「カテゴリーを選んでね」の見出しや、広告ゾーンなどを入れる")]
    [SerializeField] private GameObject[] hideWhileEditing;

    [Header("── プレビュー ──")]
    [Tooltip("SafeArea に付けた CanvasGroup。プレビュー中はここの alpha を 0 にする")]
    [SerializeField] private CanvasGroup uiGroup;

    [SerializeField] private Button previewButton;

    [Tooltip("プレビュー中だけ有効になる全画面の透明ボタン。押すとプレビューを抜ける")]
    [SerializeField] private Button previewExitButton;

    [Header("── キャラクター ──")]
    [Tooltip("編集中に隠すキャラのルート。★WalkSystem を指定すること。\n" +
             "ぽこは PokoWalkRoot の中、それ以外の4キャラは CharacterDisplayAnchor の中に\n" +
             "生成されるため、両方の親である WalkSystem でないと全キャラを隠せない")]
    [SerializeField] private GameObject characterRoot;

    [Header("── 強調表示（任意）──")]
    [Tooltip("選択中のスロットの位置に置く光などの演出。未設定なら何もしない")]
    [SerializeField] private GameObject highlightObject;

    [Header("── カテゴリの結線（11個）──")]
    [SerializeField] private List<CategoryBinding> categories = new List<CategoryBinding>();

    [Header("── 演出の速さ ──")]
    [SerializeField] private float cameraMoveDuration = 0.4f;

    // ── 内部状態 ──
    private readonly List<ItemButtonView> _pool = new List<ItemButtonView>();
    private FurnitureCategory? _openedCategory;   // null = カテゴリ未選択（カード表示中）
    private int _openedSlotIndex;                 // いま編集中のスロット番号（かべA=0 / かべB=1）
    private Vector3 _homeCamPos;                  // 入室時のカメラ位置
    private Quaternion _homeCamRot;               // 入室時のカメラ回転
    private Coroutine _camRoutine;
    private bool _inPreview;
    private Vector3 _charHomePos = Vector3.zero;   // キャラの定位置
    private Vector3 _charHomeScale = Vector3.one;  // キャラの元の大きさ

    /// <summary>いま編集しているスロット。カテゴリ＋スロット番号のセット。</summary>
    private SlotKey CurrentKey =>
        new SlotKey(_openedCategory ?? FurnitureCategory.Bed, _openedSlotIndex);

    // ─────────────────────────────────────────────
    // 初期化
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera == null) Debug.LogError("[RoomEdit] カメラが見つかりません", this);
        if (applier == null)      Debug.LogError("[RoomEdit] RoomFurnitureApplier が未設定です", this);
        if (catalog == null)      Debug.LogError("[RoomEdit] FurnitureCatalog が未設定です", this);

        // 入室時のカメラ位置を覚えておく。「もどる」でここへ返す
        if (targetCamera != null)
        {
            _homeCamPos = targetCamera.transform.position;
            _homeCamRot = targetCamera.transform.rotation;
        }

        // キャラの定位置と大きさを覚えておく。戻すときの基準になる
        if (characterRoot != null)
        {
            _charHomePos   = characterRoot.transform.localPosition;
            _charHomeScale = characterRoot.transform.localScale;
        }

        WireCategoryButtons();
        WireSlotTabButtons();

        if (returnButton  != null) returnButton.onClick.AddListener(CloseItemList);
        if (decideButton  != null) decideButton.onClick.AddListener(OnDecide);
        if (previewButton != null) previewButton.onClick.AddListener(() => SetPreview(true));
        if (previewExitButton != null)
        {
            previewExitButton.onClick.AddListener(() => SetPreview(false));
            previewExitButton.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // カタログに登録済みのカテゴリだけ、既定の家具を置き直す。
        // 未登録のカテゴリ（まだカタログに無い家具）は手置きのまま残る。
        InitializeRoomFromCatalog();

        // ここまでの状態を「入室時の状態」として記録する
        applier?.TakeSnapshot();

        ShowCategoryCard();
    }

    private void WireCategoryButtons()
    {
        var seen = new HashSet<FurnitureCategory>();

        foreach (var b in categories)
        {
            if (b == null || b.button == null) continue;

            if (!seen.Add(b.category))
            {
                Debug.LogError($"[RoomEdit] カテゴリ {b.category} のボタンが重複しています", this);
                continue;
            }

            // ★ラムダで b をそのまま使うと、ループ変数を捕まえて全部同じカテゴリになる。
            //   ローカル変数にコピーしてから渡すこと（よくあるバグ）
            var cat = b.category;
            b.button.onClick.AddListener(() => OpenItemList(cat));
        }

        // 結線漏れの検出
        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            if (!seen.Contains(c))
                Debug.LogWarning($"[RoomEdit] カテゴリ {c} のボタンが未結線です", this);
        }
    }

    /// <summary>
    /// かべA / かべB のタブを押したときの処理をつなぐ。
    /// タブの数はカテゴリごとに変わるので、ここでは全部つないでおき、
    /// 表示するかどうかは OpenItemList のときに決める。
    /// </summary>
    private void WireSlotTabButtons()
    {
        if (slotTabButtons == null) return;

        for (int i = 0; i < slotTabButtons.Length; i++)
        {
            if (slotTabButtons[i] == null) continue;

            // ★ここもカテゴリボタンと同じ。ループ変数 i をそのままラムダに渡すと
            //   全部のタブが最後の番号になる。ローカルにコピーしてから渡すこと
            int index = i;
            slotTabButtons[i].onClick.AddListener(() => SelectSlot(index));
        }

        if (slotTabRoot != null) slotTabRoot.SetActive(false);
    }

    private void InitializeRoomFromCatalog()
    {
        if (applier == null || catalog == null) return;

        // セーブデータに保存された選択を読み込む。無ければ空
        var saved = RoomFurnitureSave.LoadAll();

        // ★enum を総なめするのではなく「Applier に結線されているスロット」を回す。
        //   かべかざりのように1カテゴリに2スロットあるものも、これで両方復元される。
        foreach (var key in applier.GetAllSlotKeys())
        {
            // まだカタログに1件も無いカテゴリは触らない（手置きの家具を残す）
            if (catalog.GetByCategory(key.category).Count == 0) continue;

            saved.TryGetValue(key, out string id);

            // id が null でも Applier が既定へフォールバックする
            applier.Apply(key, id);
        }
    }

    // ─────────────────────────────────────────────
    // カテゴリ選択 → アイテム一覧
    // ─────────────────────────────────────────────

    /// <summary>カテゴリボタンから呼ばれる。一覧を開いて、カメラを寄せて、キャラを隠す。</summary>
    public void OpenItemList(FurnitureCategory category)
    {
        if (catalog == null) return;

        _openedCategory = category;
        _openedSlotIndex = 0;   // ★開いたときは必ず かべA から

        var entries = catalog.GetByCategory(category);
        if (entries.Count == 0)
        {
            // カタログ未登録のカテゴリを押した場合。落とさずに知らせるだけにする
            Debug.LogWarning($"[RoomEdit] {category} はカタログに1件も登録されていません", this);
        }

        BuildItemButtons(entries);
        RefreshSlotTabs();

        if (categoryLabel != null) categoryLabel.text = ToDisplayName(category);
        SetCardVisible(false);
        if (itemListPanel != null) itemListPanel.SetActive(true);

        MoveCameraTo(FindViewpoint(category));
        SetCharacterVisible(false);
        MoveHighlightTo(CurrentKey);
    }

    /// <summary>
    /// かべA / かべB のタブを押したとき。
    /// アイテム一覧はそのまま（同じプールを共有している）で、
    /// 「いまどっちのスロットを編集しているか」だけを切り替える。
    /// </summary>
    private void SelectSlot(int index)
    {
        if (_openedCategory == null || applier == null) return;

        // 用意されていない番号を押されても無視する（タブを多めに置いていても安全）
        if (index < 0 || index >= applier.GetSlotCount(_openedCategory.Value)) return;

        _openedSlotIndex = index;

        RefreshSlotTabs();
        RefreshItemSelection();          // 選択枠を、そのスロットの中身に合わせ直す
        MoveHighlightTo(CurrentKey);
    }

    /// <summary>タブの出し入れと、選択中の枠を更新する。</summary>
    private void RefreshSlotTabs()
    {
        int count = (_openedCategory != null && applier != null)
            ? applier.GetSlotCount(_openedCategory.Value)
            : 0;

        // ★スロットが1つしかないカテゴリではタブそのものを出さない。
        //   ベッドやソファの画面に「かべA/かべB」が出てしまうのを防ぐ
        if (slotTabRoot != null) slotTabRoot.SetActive(count > 1);

        if (slotTabButtons == null) return;

        for (int i = 0; i < slotTabButtons.Length; i++)
        {
            if (slotTabButtons[i] != null)
                slotTabButtons[i].gameObject.SetActive(count > 1 && i < count);

            if (slotTabSelectedMarks != null
                && i < slotTabSelectedMarks.Length
                && slotTabSelectedMarks[i] != null)
            {
                slotTabSelectedMarks[i].SetActive(i == _openedSlotIndex);
            }
        }
    }

    /// <summary>「もどる」。カメラとキャラを戻して、カテゴリカードに帰る。</summary>
    public void CloseItemList()
    {
        _openedCategory = null;
        _openedSlotIndex = 0;

        if (itemListPanel != null) itemListPanel.SetActive(false);
        if (slotTabRoot != null) slotTabRoot.SetActive(false);
        ShowCategoryCard();

        MoveCameraHome();
        SetCharacterVisible(true);

        if (highlightObject != null) highlightObject.SetActive(false);
    }

    private void ShowCategoryCard()
    {
        SetCardVisible(true);
        if (itemListPanel != null) itemListPanel.SetActive(false);
    }

    /// <summary>
    /// カテゴリカードと、それに付随する表示（見出し・広告ゾーンなど）をまとめて出し入れする。
    /// 隠したいものが増えたら hideWhileEditing に足すだけでよい。
    /// </summary>
    private void SetCardVisible(bool visible)
    {
        if (categoryCard != null) categoryCard.SetActive(visible);

        if (hideWhileEditing == null) return;
        foreach (var o in hideWhileEditing)
            if (o != null) o.SetActive(visible);
    }

    // ─────────────────────────────────────────────
    // アイテムボタンの生成（プールで使い回す）
    // ─────────────────────────────────────────────
    private void BuildItemButtons(List<FurnitureEntry> entries)
    {
        if (itemButtonPrefab == null || itemListContent == null)
        {
            Debug.LogError("[RoomEdit] ItemButton の Prefab か Content が未設定です", this);
            return;
        }

        // ★一覧はカテゴリ単位で1つ。かべAとかべBは同じ一覧を共有する。
        //   選択枠の位置だけが、いま見ているスロットによって変わる。
        string currentId = applier != null ? applier.GetCurrentId(CurrentKey) : null;

        for (int i = 0; i < entries.Count; i++)
        {
            // ★足りなければ作り、足りていれば使い回す。
            //   毎回 Destroy → Instantiate すると、スクロールのたびに GC が走って
            //   カクつきの原因になる。
            if (i >= _pool.Count)
            {
                var created = Instantiate(itemButtonPrefab, itemListContent);
                _pool.Add(created);
            }

            var view = _pool[i];
            view.gameObject.SetActive(true);
            view.transform.SetSiblingIndex(i); // 並び順をカタログの順に揃える

            var e = entries[i];
            bool isSelected = (e.id == currentId);

            // TODO: 所持データができたら、ここを save.ownedFurnitureIds.Contains(e.id) に差し替える
            bool isOwned = e.ownedByDefault;

            view.Bind(e, isSelected, isOwned, OnItemClicked);
        }

        // 余ったボタンは消さずに隠すだけ。次に使うときそのまま復活できる
        for (int i = entries.Count; i < _pool.Count; i++)
            _pool[i].gameObject.SetActive(false);
    }

    /// <summary>アイテムボタンが押されたとき。その場で部屋に反映する（＝プレビュー）。</summary>
    private void OnItemClicked(string itemId)
    {
        if (_openedCategory == null || applier == null) return;

        // ★いま選んでいるスロット（かべA か かべB）にだけ入れる
        if (!applier.Apply(CurrentKey, itemId)) return;

        RefreshItemSelection();
    }

    /// <summary>選択枠を、いま見ているスロットの中身に合わせ直す。</summary>
    private void RefreshItemSelection()
    {
        string currentId = applier != null ? applier.GetCurrentId(CurrentKey) : null;

        foreach (var v in _pool)
        {
            if (v != null && v.gameObject.activeSelf)
                v.SetSelected(v.ItemId == currentId);
        }
    }

    /// <summary>
    /// そのIDを持っているか。一覧のボタンから判定する。
    /// 一覧に無いIDは判定できないので、通す（＝保存を止めない）。
    /// </summary>
    private bool IsOwned(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return true;

        foreach (var v in _pool)
        {
            if (v != null && v.gameObject.activeSelf && v.ItemId == itemId)
                return v.IsOwned;
        }
        return true;
    }

    /// <summary>「けってい」。所持していないものが選ばれていたら保存させない。</summary>
    private void OnDecide()
    {
        if (_openedCategory == null || applier == null) { CloseItemList(); return; }

        var category = _openedCategory.Value;
        var indices = applier.GetSlotIndices(category);

        // ★このカテゴリのスロットを全部チェックする。
        //   かべAだけ見ていると、かべBに未所持のものが入ったまま保存されてしまう
        foreach (int i in indices)
        {
            string id = applier.GetCurrentId(new SlotKey(category, i));
            if (IsOwned(id)) continue;

            // TODO: 所持データを入れたら、ここでアラートを出してショップへ誘導する
            Debug.Log($"<color=#00E5FF>[決定]</color> [RoomEdit] {id} は未所持のため保存できません", this);
            return;
        }

        // ★このカテゴリのスロットを全部まとめて保存する。
        //   かべAを決定したのにかべBが保存されない、という取りこぼしを防ぐ
        foreach (int i in indices)
        {
            var key = new SlotKey(category, i);
            string id = applier.GetCurrentId(key);

            RoomFurnitureSave.Set(key, id);
            Debug.Log($"<color=#00E5FF>[決定]</color> [RoomEdit] {key} を {id} で保存しました", this);
        }

        // セーブデータをファイルに書き出す。
        // これで Main や Care を開いたときにも同じ家具が出る
        RoomFurnitureSave.Commit();

        applier.TakeSnapshot(); // ここまでを新しい「戻り先」にする
        CloseItemList();
    }

    // ─────────────────────────────────────────────
    // カメラ
    // ─────────────────────────────────────────────
    private Transform FindViewpoint(FurnitureCategory category)
    {
        foreach (var b in categories)
            if (b != null && b.category == category) return b.viewpoint;
        return null;
    }

    private void MoveCameraTo(Transform viewpoint)
    {
        // viewpoint が未設定なら動かさない（お部屋カテゴリはこれ）
        if (viewpoint == null) { MoveCameraHome(); return; }
        StartCameraMove(viewpoint.position, viewpoint.rotation);
    }

    private void MoveCameraHome()
    {
        StartCameraMove(_homeCamPos, _homeCamRot);
    }

    private void StartCameraMove(Vector3 pos, Quaternion rot)
    {
        if (targetCamera == null) return;

        // ★移動中に別カテゴリを押されたら、走っている移動を止めて上書きする。
        //   止めないと2つの移動が同時に走ってガタつく。
        if (_camRoutine != null) StopCoroutine(_camRoutine);
        _camRoutine = StartCoroutine(CameraMoveRoutine(pos, rot));
    }

    private IEnumerator CameraMoveRoutine(Vector3 pos, Quaternion rot)
    {
        Transform t = targetCamera.transform;
        Vector3 p0 = t.position;
        Quaternion r0 = t.rotation;
        float time = 0f;

        while (time < cameraMoveDuration)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / cameraMoveDuration);
            k = k * k * (3f - 2f * k);           // なめらかに加減速（SmoothStep）
            t.position = Vector3.Lerp(p0, pos, k);
            t.rotation = Quaternion.Slerp(r0, rot, k);
            yield return null;
        }

        t.position = pos;
        t.rotation = rot;
        _camRoutine = null;
    }

    // ─────────────────────────────────────────────
    // キャラクター
    // ─────────────────────────────────────────────

    /// <summary>
    /// キャラを出し入れする。
    ///
    /// ★アニメーションは付けない。
    ///   縮めても浮かせても「潰れる／飛んでいく」ように見えて不自然だったため、
    ///   その場でぱっと切り替える方が素直で気持ちがいい。
    ///   位置と大きさは念のため定位置に戻してから切り替える。
    /// </summary>
    private void SetCharacterVisible(bool visible)
    {
        if (characterRoot == null) return;

        Transform t = characterRoot.transform;
        t.localPosition = _charHomePos;
        t.localScale    = _charHomeScale;

        characterRoot.SetActive(visible);
    }

    // ─────────────────────────────────────────────
    // 強調表示・プレビュー
    // ─────────────────────────────────────────────
    /// <summary>
    /// 強調表示を、いま編集しているスロットの位置へ移す。
    /// highlightObject が未設定なら何もしない（任意の演出）。
    /// </summary>
    private void MoveHighlightTo(SlotKey key)
    {
        if (highlightObject == null) return;

        Transform slot = applier != null ? applier.GetSlot(key) : null;
        if (slot == null)
        {
            highlightObject.SetActive(false);
            return;
        }

        // ★スロットの子にはしない。
        //   スロットはスケールが (2.5, 3, 2.5) のように歪んでいるものがあり、
        //   子にすると光まで潰れて表示される。位置だけ合わせるのが安全。
        highlightObject.transform.position = slot.position;
        highlightObject.SetActive(true);
    }

    private void SetPreview(bool on)
    {
        _inPreview = on;

        if (uiGroup != null)
        {
            uiGroup.alpha = on ? 0f : 1f;
            uiGroup.blocksRaycasts = !on;
            uiGroup.interactable = !on;
        }

        if (previewExitButton != null)
        {
            previewExitButton.gameObject.SetActive(on);
        }
        else if (on)
        {
            Debug.LogWarning("[RoomEdit] previewExitButton が未設定です。" +
                             "プレビューから戻れないので、全画面の透明ボタンを用意してください", this);
        }

        Debug.Log($"[RoomEdit] プレビュー {(on ? "開始" : "終了")}", this);
    }

    // ─────────────────────────────────────────────
    private static string ToDisplayName(FurnitureCategory c)
    {
        switch (c)
        {
            case FurnitureCategory.Bed:        return "ベッド";
            case FurnitureCategory.Table:      return "テーブル";
            case FurnitureCategory.Sofa:       return "ソファ";
            case FurnitureCategory.WallShelf:  return "壁掛け棚";
            case FurnitureCategory.Shelf:      return "本棚";
            case FurnitureCategory.Window:     return "窓";
            case FurnitureCategory.Nightstand: return "サイドテーブル";
            case FurnitureCategory.RoomLight:  return "ルームライト";
            case FurnitureCategory.Decoration: return "装飾";
            case FurnitureCategory.Rug:        return "ラグマット";
            case FurnitureCategory.RoomShell:  return "おへや";
            case FurnitureCategory.Decoration2: return "かべかざり";
            default: return c.ToString();
        }
    }
}
