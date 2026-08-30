using System.Collections.Generic;
using UnityEngine;   // Mathf のみ使用

/// <summary>
/// おやつの「所持数（在庫）」の出し入れを、ここ1箇所に集めた場所。
///
/// 【なぜ在庫が要るのか】2026/8/30（U-9）
///   パズル（あそぶ画面）のステージ報酬で おやつ が手に入るため。
///     ステージ2 … にぼし ×1
///     ステージ4 … ランダム（にぼし50% / ビスケット20% / にんじん20% /
///                            いちごケーキ or プリン 10%）
///   もらった時点では食べないので、どこかに貯めておく必要がある。
///   requirements.md §5 の「有料おやつ（6種類・所持アイテム制）」とも一致する。
///
/// 【使い方の決まり】2026/8/30 決定
///   ・在庫があれば在庫を優先して使う。コインもルナストーンも減らさない
///   ・在庫が無いときは、今までどおり その場で買って食べる
///   ・★無償おやつの「1日6回」の上限は、在庫から食べても数える
///
/// 【★JsonUtility の落とし穴】
///   JsonUtility はコンストラクタを呼ばない。そのため
///   このフィールドが無い【古いセーブ】を読むと、リストは null のままになる。
///   （CLAUDE.md の地雷集にある実例と同じ）
///   → 触る前に必ず EnsureList() を通すこと。ここ以外で oyatuStocks を直接触らない。
///
/// 【id について】
///   OyatuManager.AllOyatu の id と同じ文字列を使う。
///   "niboshi" / "biscuit" / "carrot" / "strawberry_cake" / "pudding" /
///   "fruit_tart" / "macaron" / "hamburg" / "parfait"
///   ★パズル側の PieceType との対応表は、パズルの報酬付与（G-6）を作るときに
///     本体側へ用意する。Assets/Imported/ は汚さない。
/// </summary>
public static class OyatuInventory
{
    /// <summary>古いセーブ対策。リストが null なら作る。</summary>
    private static List<OyatuStock> EnsureList(SaveData save)
    {
        if (save == null) return null;
        if (save.oyatuStocks == null) save.oyatuStocks = new List<OyatuStock>();
        return save.oyatuStocks;
    }

    /// <summary>そのおやつを何個持っているか。無ければ 0。</summary>
    public static int Get(SaveData save, string id)
    {
        var list = EnsureList(save);
        if (list == null || string.IsNullOrEmpty(id)) return 0;

        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].id == id) return list[i].count;

        return 0;
    }

    /// <summary>
    /// 1種類につき持てる上限。★2026/8/30 決定。
    /// これを超えるぶんは受け取れない（呼び出し側で「寄付されました」等を出す）。
    /// </summary>
    public const int MaxPerOyatu = 10;

    /// <summary>もう上限まで持っているか。</summary>
    public static bool IsFull(SaveData save, string id) => Get(save, id) >= MaxPerOyatu;

    /// <summary>あと何個受け取れるか。上限まで持っていれば 0。</summary>
    public static int Room(SaveData save, string id) => Mathf.Max(0, MaxPerOyatu - Get(save, id));

    /// <summary>
    /// 在庫を増やす。パズルの報酬やショップの購入から呼ぶ。
    ///
    /// ★上限（MaxPerOyatu = 10）を超えるぶんは受け取らない。
    ///   【実際に受け取れた数】を返すので、呼び出し側は
    ///   「0 だった＝満杯だった」を見て、寄付のお知らせなどを出すこと。
    /// </summary>
    public static int Add(SaveData save, string id, int count = 1)
    {
        var list = EnsureList(save);
        if (list == null || string.IsNullOrEmpty(id) || count <= 0) return 0;

        int room = Room(save, id);
        if (room <= 0) return 0;          // すでに満杯。1つも受け取れない

        int accepted = count < room ? count : room;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || list[i].id != id) continue;
            list[i].count += accepted;
            return accepted;
        }

        list.Add(new OyatuStock { id = id, count = accepted });
        return accepted;
    }

    /// <summary>
    /// 在庫を1つ使う。在庫があれば減らして true、無ければ何もせず false。
    /// ★false のときは呼び出し側が今までどおり コイン／ルナ を払う。
    /// </summary>
    public static bool TryUse(SaveData save, string id)
    {
        var list = EnsureList(save);
        if (list == null || string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || list[i].id != id || list[i].count <= 0) continue;

            list[i].count--;
            // 0 になった行は残しておく。消すと Add のたびに行が増減して
            // セーブの中身が読みにくくなるため。0 は「持っていない」と同じ意味。
            return true;
        }
        return false;
    }
}
