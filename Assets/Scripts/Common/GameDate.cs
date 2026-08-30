using System;
using System.Globalization;

/// <summary>
/// アプリ全体で「今日」を決める、唯一の場所。
///
/// 【なぜ1箇所にまとめるのか】2026/8/30（S-7）
///   以前は「今日」の作り方が2種類あった。
///     ・回数の上限（お風呂・なでなで・あそぶ・おやつ・パズル） … 端末ローカル 0:00
///     ・デイリークエスト                                       … JST 3:00
///   そのため毎日 0:00〜3:00 の3時間だけ、上限は「今日の1回目」なのに
///   クエストは「昨日の続き」に加算される、という食い違いが起きていた。
///   前日のクエストを達成済みだと、なでても進捗が一切増えない状態になる。
///
/// 【なぜ JST 3:00 なのか】
///   サーバの夜バッチが JST 3:00 に走ることが確定しているため
///   （CLAUDE.md §9 / requirements.md 付録I.2）。
///   3:00 は「日本人のログイン率が最も低い時間」という理由で選ばれた値。
///
/// 【★勘違いしないこと】
///   UtcNow を使っても「端末の日付を1日進める」不正は塞がらない。
///   UtcNow も端末のシステムクロックから計算されるため、日付を進めれば同じだけ進む。
///   塞がるのは「タイムゾーンだけを変える」経路だけ
///   （DateTime.Now は最大26時間動くが、UtcNow は動かない）。
///
/// 【なぜ TimeZoneInfo を使わないのか】
///   TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo") は iOS/IL2CPP での動作に確証が無い。
///   動かなかった場合は例外が出て日付が取れなくなる。ここは毎日必ず通る経路なので、
///   実機でしか再現しない停止要因を増やさない。
///   日本標準時は 1951年を最後に夏時間を採用しておらず UTC+9 固定なので、
///   AddHours(9) で誤差は出ない。
///
/// 【iOS ビルドに入る本番コード】
///   UnityEngine に依存させない（Assets/Scripts/Common/ParamNames.cs と同じ方針）。
/// </summary>
public static class GameDate
{
    /// <summary>
    /// 1日が切り替わる時刻（JST）。夜バッチと同じ 3:00。
    /// ★v2.0 でタイムゾーン別バッチへ移行するときは、この定数ごと形が変わる想定
    ///   （ユーザーごとのオフセットを引数で受け取る形になる）。
    /// </summary>
    public const int ResetHourJst = 3;

    /// <summary>日本標準時のオフセット。UTC+9 固定（日本は夏時間を採用していない）。</summary>
    private const int JstOffsetHours = 9;

    /// <summary>いまの日本時間。</summary>
    public static DateTime NowJst() => DateTime.UtcNow.AddHours(JstOffsetHours);

    /// <summary>
    /// 「今日」の日付。JST 3:00 で切り替わる。時刻の部分は 00:00。
    /// 例）日本時間で 8/30 の 1:00 は、まだ 8/29 として扱う。
    /// </summary>
    public static DateTime TodayJst()
    {
        DateTime jst = NowJst();
        if (jst.Hour < ResetHourJst) jst = jst.AddDays(-1);
        return jst.Date;
    }

    /// <summary>
    /// 「今日」の日付キー（"yyyy-MM-dd"）。セーブに入れる日付文字列はすべてこれを使う。
    /// ★InvariantCulture を必ず指定する。和暦カレンダーを既定にしている端末では
    ///   年の表記が変わることがあり、保存済みの文字列と比較できなくなるため。
    /// </summary>
    public static string Today()
        => TodayJst().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// 次にリセットされるまでの残り時間。クエスト画面のタイマーが使う。
    /// </summary>
    public static TimeSpan TimeUntilNextReset()
        => TodayJst().AddDays(1).AddHours(ResetHourJst) - NowJst();
}
