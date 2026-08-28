/// <summary>
/// 状態パラメータ・性格パラメータの「画面表示名」を1箇所にまとめたもの。
///
/// 【なぜ作ったか】
///   2026/8/28 時点で、同じパラメータが画面ごとに違う名前で出ていた。
///     清潔 … Care のトースト「清潔」／お風呂リザルト「きれい度」／Care のバー「キレイ」
///   性格パラ5個の名前も .cs 4ファイル・計12箇所に散っていて、直し漏れが起きる状態だった。
///   キャラID→日本語名を集約した CharacterNames.cs と同じ発想。
///
/// 【内部名と表示名を分けている理由】
///   requirements.md §6 の性格テキスト判定条件（活動性≥50 など）や SaveData のフィールド名は
///   内部名のままでないと、personalityActivity との対応が読めなくなる。
///   ★コードの中（変数名・コメント・Debug.Log）は内部名が正。ここは「画面に出す文字」専用。
///   対応表の正本は requirements.md §5「パラメータの内部名と表示名」。
///
/// 【pt について】
///   画面に出す増減は、表示上の単位として全部 pt に統一している。
///   信頼度pt（累積・レベル計算に使う）と性格パラ（-100〜+100の内部値）は内部的には別物だが、
///   その違いはユーザーには見えないため、表記のばらつきを優先して消した。
///   「おなか 全回復」のように数値がない表示には pt を付けない。
///
/// 【依存について】
///   文字列を返すだけのクラスなので UnityEngine には依存させない。
///   絶対値は System.Math.Abs を使うこと（Mathf を使わない）。
/// </summary>
public static class ParamNames
{
    // ── 状態パラメータ ──
    public const string Clean  = "キレイ";   // 内部名: 清潔       / SaveData: clean
    public const string Hunger = "おなか";   // 内部名: 空腹・おなか / SaveData: hunger
    public const string Energy = "元気";     // 内部名: 元気       / SaveData: energy
    public const string Mood   = "気分";     // 内部名: 気分       / SaveData: mood　★「機嫌」は廃止
    public const string Trust  = "信頼度";   // 内部名: 信頼度     / SaveData: trust　★例外・表示名も変更なし

    // ── 性格パラメータ ──
    public const string Activity    = "おてんば";     // 内部名: 活動性     / SaveData: personalityActivity
    public const string Dependency  = "甘えん坊";     // 内部名: 甘えん坊度 / SaveData: personalityDependency
    public const string Diligence   = "しっかりもの"; // 内部名: 勤勉さ     / SaveData: personalityDiligence
    public const string Honesty     = "素直";         // 内部名: 素直さ     / SaveData: personalityHonesty
    public const string Sensitivity = "優しさ";       // 内部名: 感受性     / SaveData: personalitySensitivity

    /// <summary>
    /// 性格パラメータの表示名5個。
    /// ★並び順は BathWashManager.ApplyPersonality() のレインボー抽選番号と一致させること。
    ///   0=活動性(おてんば) 1=甘えん坊度(甘えん坊) 2=勤勉さ(しっかりもの)
    ///   3=素直さ(素直) 4=感受性(優しさ)
    /// </summary>
    public static readonly string[] Personality =
        { Activity, Dependency, Diligence, Honesty, Sensitivity };

    /// <summary>
    /// 数値に単位を付ける（半角プラス版）。
    /// 3 → "+3pt" ／ 0 → "0pt"（符号なし） ／ -2 → "-2pt"
    /// </summary>
    public static string Pt(int value)
    {
        if (value > 0) return "+" + value + "pt";
        return value + "pt";   // 0 は "0pt"、負数は "-2pt"（符号は数値側が持つ）
    }

    /// <summary>
    /// 数値に単位を付ける（全角プラス版）。お風呂のリザルトなど桁をそろえたい場所用。
    /// 3 → "＋3pt" ／ 0 → "0pt"（符号なし） ／ -2 → "－2pt"
    /// ★Pt() と0の扱いをそろえてある。片方だけ直さないこと。
    /// </summary>
    public static string PtWide(int value)
    {
        if (value > 0) return "＋" + value + "pt";
        if (value < 0) return "－" + System.Math.Abs(value) + "pt";
        return "0pt";
    }
}
