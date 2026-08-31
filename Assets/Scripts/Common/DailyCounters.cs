/// <summary>
/// 「1日◯回まで」の回数を管理する、唯一の場所。★S-3（2026/8/31）
///
/// 【なぜ「読む」と「使う」を分けたのか】
///   以前は「今日はもう何回やったか」を確認する処理が、
///   ついでにセーブの日付まで書き換えていた。
///   そのため lastBathDate が「最後にお風呂に入った日」ではなく
///   「最後にお風呂ボタンを押した日」になっていた。
///   お風呂ボタンを押して Bath 画面から何もせずに戻っただけでも、
///   その日に入浴したことになってしまう。
///   同じ理由で、コインが足りずに無償おやつが失敗したときも、日付だけが今日に進んでいた。
///
///   そこで窓口を2種類に分けた。
///     ・XxxToday()   … 今日の回数を返すだけ。セーブは一切書き換えない
///     ・ConsumeXxx() … 実際に1回使ったときだけ呼ぶ。日付が変わっていれば
///                      0に戻してから +1 し、日付を今日にする
///   こうすると「実際に使ったときだけ日付が進む」ので、名前と中身が一致する。
///
/// 【リセットの意味】
///   セーブに入っているのは「回数」と「その回数を数えた日」の2つだけで、
///   日付が変わった瞬間に誰かが0に戻してくれるわけではない。
///   「保存されている日付が今日でなければ、今日は0回」とみなすのが実体。
///   だから読む側（XxxToday）は何も書き換えなくても正しい値を返せる。
///
/// 【呼ぶ順番】
///   上限の判定は XxxToday()、実際に使うのは ConsumeXxx()。
///   コインの支払いなど「失敗して戻る可能性のある処理」より
///   ★あとに ConsumeXxx() を呼ぶこと。先に呼ぶと、失敗したのに回数だけ減る。
///
/// 【保存はしない】
///   ここではセーブファイルへの書き出し（SaveManager.Save）は行わない。
///   呼び出し側が GameContext.SavePetStatus() などでまとめて保存しているため、
///   二重に書き出さないようにしている。
///
/// 【「今日」の基準】
///   GameDate.Today()（JST 3:00 で切り替わる。S-7 で一本化した）。
///
/// 【iOS ビルドに入る本番コード】
///   UnityEngine に依存させない（GameDate.cs / ParamNames.cs と同じ方針）。
///
/// 【新しい「1日◯回まで」を足すとき】
///   4種類とも同じ形をしている。コピーして名前とフィールドを変えるだけでよい。
///   ここ以外の場所にリセット処理を書かないこと。それが S-3 で直した問題そのもの。
/// </summary>
public static class DailyCounters
{
    // ── 読む（セーブを一切書き換えない）─────────────────────────────

    /// <summary>今日すでにお風呂に入った回数。</summary>
    public static int BathToday(SaveData save)
    {
        if (save == null) return 0;
        return (save.lastBathDate == GameDate.Today()) ? save.bathCountToday : 0;
    }

    /// <summary>今日すでになでなでした回数。</summary>
    public static int NadeToday(SaveData save)
    {
        if (save == null) return 0;
        return (save.lastNadeDate == GameDate.Today()) ? save.nadeCountToday : 0;
    }

    /// <summary>今日すでにあそんだ回数。</summary>
    public static int PlayToday(SaveData save)
    {
        if (save == null) return 0;
        return (save.lastPlayDate == GameDate.Today()) ? save.playCountToday : 0;
    }

    /// <summary>今日すでに無償おやつをあげた回数（種類を問わない合計）。</summary>
    public static int FreeOyatuToday(SaveData save)
    {
        if (save == null) return 0;
        return (save.lastFreeOyatuDate == GameDate.Today()) ? save.freeOyatuCountToday : 0;
    }

    // ── 使う（実際に1回使ったときだけ呼ぶ）───────────────────────────

    /// <summary>お風呂に1回入った。</summary>
    public static void ConsumeBath(SaveData save)
    {
        if (save == null) return;
        string today = GameDate.Today();
        if (save.lastBathDate != today) save.bathCountToday = 0;   // 日付が変わっていたら数え直す
        save.bathCountToday++;
        save.lastBathDate = today;                                 // 実際に使った日を記録する
    }

    /// <summary>なでなでを1回した。</summary>
    public static void ConsumeNade(SaveData save)
    {
        if (save == null) return;
        string today = GameDate.Today();
        if (save.lastNadeDate != today) save.nadeCountToday = 0;
        save.nadeCountToday++;
        save.lastNadeDate = today;
    }

    /// <summary>あそぶを1回した。</summary>
    public static void ConsumePlay(SaveData save)
    {
        if (save == null) return;
        string today = GameDate.Today();
        if (save.lastPlayDate != today) save.playCountToday = 0;
        save.playCountToday++;
        save.lastPlayDate = today;
    }

    /// <summary>無償おやつを1つあげた。</summary>
    public static void ConsumeFreeOyatu(SaveData save)
    {
        if (save == null) return;
        string today = GameDate.Today();
        if (save.lastFreeOyatuDate != today) save.freeOyatuCountToday = 0;
        save.freeOyatuCountToday++;
        save.lastFreeOyatuDate = today;
    }
}
