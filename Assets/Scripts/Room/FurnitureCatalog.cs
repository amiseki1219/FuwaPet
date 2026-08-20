using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 家具のカテゴリ。Room_Base の FurnitureSlot_* と1対1で対応する。
///
/// ★ここの名前は「セーブデータに文字列として保存される」ので、
///   一度リリースしたら変えないこと。変えると既存ユーザーの部屋が読めなくなる。
///
/// ★★並び順は変えないこと。新しいカテゴリは必ず「末尾」に足す。
///   セーブデータは文字列なので並べ替えても平気だが、
///   .asset（カタログ）と .unity（シーン）の中では category が「数値」で保存されている。
///   途中に挿入すると、既存の行が1つずつズレて全部別カテゴリになる。
/// </summary>
public enum FurnitureCategory
{
    Bed,          // ベッド
    Table,        // テーブル
    Sofa,         // ソファ
    WallShelf,    // 壁掛け棚
    Shelf,        // 本棚
    Window,       // 窓
    Nightstand,   // サイドテーブル
    RoomLight,    // ルームライト
    Decoration,   // 装飾
    Rug,          // ラグマット
    RoomShell,    // お部屋（部屋土台）
    Decoration2,  // かべかざり（スロット2つ：かべA / かべB）

    // ↑ 新しいカテゴリはここに足す（途中に入れない）
}

/// <summary>
/// 「どのスロットか」を表すキー。カテゴリ ＋ スロット番号 のセット。
///
/// 【なぜ番号が要るか】
///   かべかざりのように、同じカテゴリでスロットを複数持ちたいものがあるため。
///     かべA = Decoration2 の 0番
///     かべB = Decoration2 の 1番
///   スロットが1つしかないカテゴリは、今までどおり全部 0番。
///
///   こうしておくと、アイテムの一覧は「Decoration2」ひとつで共有できる。
///   かべA用・かべB用にカタログの行を2重に作らなくていいし、
///   同じ時計をかべAとかべBの両方に飾ることもできる。
///
/// 【セーブデータに書くときの形】
///   0番      → "Decoration2"      ← 今までのセーブと同じ形。だから旧データもそのまま読める
///   1番以降  → "Decoration2#1"
/// </summary>
[System.Serializable]
public struct SlotKey : System.IEquatable<SlotKey>
{
    public FurnitureCategory category;
    public int index;

    public SlotKey(FurnitureCategory category, int index = 0)
    {
        this.category = category;
        this.index = index;
    }

    /// <summary>セーブデータに書く文字列。0番はカテゴリ名だけ（＝旧形式と同じ）。</summary>
    public override string ToString()
    {
        return index == 0 ? category.ToString() : category + "#" + index;
    }

    /// <summary>
    /// セーブデータの文字列を読み戻す。
    /// 知らないカテゴリ名なら false を返す（呼び出し側で読み飛ばす）。
    /// </summary>
    public static bool TryParse(string s, out SlotKey key)
    {
        key = default;
        if (string.IsNullOrEmpty(s)) return false;

        string catPart = s;
        int idx = 0;

        int sharp = s.IndexOf('#');
        if (sharp >= 0)
        {
            catPart = s.Substring(0, sharp);

            // 番号が数字でなければ 0番として扱う。
            // 「読めないから丸ごと捨てる」より、かべAとして復元するほうが被害が小さい
            int.TryParse(s.Substring(sharp + 1), out idx);
            if (idx < 0) idx = 0;
        }

        if (!System.Enum.TryParse(catPart, out FurnitureCategory cat)) return false;

        key = new SlotKey(cat, idx);
        return true;
    }

    // Dictionary のキーに使うので、比較まわりを自分で書いておく。
    // 書かないと比較のたびに箱詰め（ボックス化）が起きて、地味にゴミが増える
    public bool Equals(SlotKey other) => category == other.category && index == other.index;
    public override bool Equals(object obj) => obj is SlotKey o && Equals(o);
    public override int GetHashCode() => ((int)category * 397) ^ index;

    /// <summary>
    /// カテゴリをそのまま SlotKey として書けるようにする（＝0番の意味）。
    /// これのおかげで、スロットが1つだけのカテゴリを扱う既存のコードを書き換えずに済む。
    /// </summary>
    public static implicit operator SlotKey(FurnitureCategory c) => new SlotKey(c, 0);
}

/// <summary>
/// 家具アイテム1件ぶんのデータ。
/// カタログ（ScriptableObject）の中にリストとして並ぶ。
/// </summary>
[System.Serializable]
public class FurnitureEntry
{
    [Tooltip("どのカテゴリの家具か")]
    public FurnitureCategory category;

    [Tooltip("セーブデータに保存されるID。★リリース後は絶対に変えないこと。\n" +
             "命名は {カテゴリ}_{バリエーション} で統一する。例: RoomShell_Koko / Bed_Default")]
    public string id;

    [Tooltip("一覧に表示する名前。これは後から変えてOK（セーブには使われない）")]
    public string displayName;

    [Tooltip("スロットに入れる Prefab。例: Furniture_RoomShell_Koko\n" +
             "「なし」の行では空にする")]
    public GameObject prefab;

    [Tooltip("この行は「なし」を表す。選ぶとスロットが空になる。\n" +
             "★Prefab を空にするだけだと『設定し忘れ』と区別できないので、\n" +
             "  意図的に空にする場合は必ずここにチェックを入れること。\n" +
             "カタログにこの行を作らなければ、そのカテゴリでは「なし」を選べない")]
    public bool isEmptySlot = false;

    [Tooltip("一覧ボタンに出すサムネイル画像。未設定でも動く（ボタンが無画像になるだけ）")]
    public Sprite thumbnail;

    [Tooltip("初期状態から持っているか。false の場合はショップで購入するまで選べない。\n" +
             "※所持判定はまだ未実装。今は表示用のメモとして使う")]
    public bool ownedByDefault = true;
}

/// <summary>
/// 家具アイテムの一覧を持つデータ資産（ScriptableObject）。
///
/// 【作り方】
///   Project ウィンドウで右クリック → Create → YURUFU → Furniture Catalog
///   置き場所は Assets/Art/3D/Rooms/ 直下あたりが分かりやすい
///
/// 【なぜ ScriptableObject にするか】
///   家具が増えても「このアセットに1行足すだけ」で済むから。
///   シーンやコードを触らないので、追加作業でバグが混入しない。
/// </summary>
[CreateAssetMenu(fileName = "FurnitureCatalog", menuName = "YURUFU/Furniture Catalog")]
public class FurnitureCatalog : ScriptableObject
{
    [Tooltip("全カテゴリぶんの家具をここに並べる。順番がそのまま一覧の並び順になる")]
    [SerializeField] private List<FurnitureEntry> entries = new List<FurnitureEntry>();

    /// <summary>登録されている全アイテム（読み取り専用）</summary>
    public IReadOnlyList<FurnitureEntry> Entries => entries;

    /// <summary>
    /// 指定カテゴリのアイテムを、登録順のまま取り出す。
    /// アイテム一覧を並べるときに使う。
    /// </summary>
    public List<FurnitureEntry> GetByCategory(FurnitureCategory category)
    {
        // ここで new List を作っているのは、呼び出し側に中身をいじられないようにするため。
        // 毎フレーム呼ぶ想定ではないので、この程度の生成は問題にならない。
        var result = new List<FurnitureEntry>();
        foreach (var e in entries)
        {
            if (e == null) continue;
            if (e.category == category) result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// ID からアイテムを引く。見つからなければ null。
    /// ★セーブデータから復元するとき、ここが null を返す可能性がある
    ///   （アイテムを削除した／IDを変えた場合）。呼び出し側で必ず null チェックすること。
    /// </summary>
    public FurnitureEntry FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var e in entries)
        {
            if (e != null && e.id == id) return e;
        }
        return null;
    }

    /// <summary>
    /// そのカテゴリの「既定のアイテム」を返す。
    /// セーブデータが壊れていたときや、未知のIDが来たときの逃げ先として使う。
    ///
    /// 探す順番:
    ///   1. ID が "{カテゴリ名}_Default" のもの（例: Bed_Default）
    ///   2. 見つからなければ、そのカテゴリの先頭のアイテム
    /// </summary>
    public FurnitureEntry GetDefault(FurnitureCategory category)
    {
        string defaultId = category + "_Default";

        FurnitureEntry first = null;
        foreach (var e in entries)
        {
            if (e == null || e.category != category) continue;
            if (e.id == defaultId) return e;
            if (first == null) first = e;
        }
        return first; // 1件も無ければ null
    }

    // ─────────────────────────────────────────────
    // 入力ミスの検出（エディタでのみ動く）
    // Inspector で値を変えたときに自動で走る
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        var seen = new HashSet<string>();

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null) continue;

            // ID 未入力
            if (string.IsNullOrEmpty(e.id))
            {
                Debug.LogWarning($"[FurnitureCatalog] {i}番目のIDが空です（{e.category}）", this);
                continue;
            }

            // ID の重複はセーブ復元時に事故になるので必ず潰す
            if (!seen.Add(e.id))
            {
                Debug.LogError($"[FurnitureCatalog] IDが重複しています: {e.id}", this);
            }

            // Prefab 未設定。「なし」の行は空が正しいので警告しない
            if (e.prefab == null && !e.isEmptySlot)
            {
                Debug.LogWarning($"[FurnitureCatalog] {e.id} の Prefab が未設定です。" +
                                 $"「なし」の行なら isEmptySlot にチェックを入れてください", this);
            }

            // 逆に、Prefab が入っているのに「なし」扱いになっているのも事故
            if (e.prefab != null && e.isEmptySlot)
            {
                Debug.LogWarning($"[FurnitureCatalog] {e.id} は isEmptySlot なのに Prefab が入っています。" +
                                 $"Prefab は無視されます", this);
            }

            // 命名規則から外れている（動作はするが、後で分からなくなるので警告）
            if (!e.id.StartsWith(e.category.ToString() + "_"))
            {
                Debug.LogWarning($"[FurnitureCatalog] {e.id} は命名規則 " +
                                 $"\"{e.category}_〇〇\" から外れています", this);
            }
        }
    }
#endif
}
