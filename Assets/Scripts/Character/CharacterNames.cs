using System.Collections.Generic;

/// <summary>
/// キャラクターの「表示名」を決める処理を1箇所にまとめたもの。
///
/// 【なぜ作ったか】
///   キャラID → 日本語名の対応表が、2026/8/28 時点で 4箇所に散っていた。
///     ・Tutorial/CharaNicknamePanel.cs   （Dictionary）
///     ・Tutorial/ConfirmPanel.cs         （Dictionary・メソッド内で毎回生成）
///     ・Care/CareSceneManager.cs         （switch式）
///     ・Main/MainUIManager.cs            （switch式）
///   キャラを追加したときに直し漏れると、画面によって名前が出たり出なかったりする。
///   お風呂の吹き出し（A2.6）で5箇所目が増えるため、先に集約した。
///
/// 【移行のときに気をつけたこと】
///   「見つからなかったとき」の扱いが呼び出し側で3通りに分かれていた。
///     CharaNicknamePanel … "ぽこ"
///     ConfirmPanel       … ID をそのまま
///     Care / Main        … petName（無ければ空文字）
///   挙動を変えないため、fallback を引数で受け取る形にしてある。
///   ★勝手に統一していない。統一するなら別の作業として決めること。
///
/// 【ID の正規化について】
///   ここでは Trim / ToLower をしない。Care と Main の既存挙動に合わせるため。
///   ★泡システムなど「機能の分岐」に使う ID は
///     CharacterStaticDisplayController.ResolveCharacterId() 側（Trim + ToLower あり）を使うこと。
///     用途が違うので、あえて別のままにしてある。
/// </summary>
public static class CharacterNames
{
    /// <summary>正規のキャラクターID（すべて小文字）。AGENTS.md の一覧と一致させること。</summary>
    public static readonly string[] AllIds = { "poko", "eru", "koko", "paru", "piyoko" };

    /// <summary>キャラID → 日本語名。★ここが対応表の正本。</summary>
    private static readonly Dictionary<string, string> DefaultNames = new Dictionary<string, string>
    {
        { "poko",   "ぽこ" },
        { "eru",    "える" },
        { "koko",   "ここ" },
        { "paru",   "ぱる" },
        { "piyoko", "ぴよこ" },
    };

    /// <summary>
    /// キャラID から日本語名を返す。見つからなければ fallback をそのまま返す。
    /// ニックネームは見ない。「そのキャラの既定の名前」がほしいときに使う。
    /// </summary>
    public static string GetDefaultName(string characterId, string fallback = "")
    {
        if (string.IsNullOrEmpty(characterId)) return fallback;
        return DefaultNames.TryGetValue(characterId, out var name) ? name : fallback;
    }

    /// <summary>
    /// セーブデータから、いま使うキャラIDを返す。
    /// selectedCharacterId を優先し、空なら旧 characterId へフォールバックする。
    /// ★どちらも空なら空文字を返す（"poko" を勝手に補わない）。
    ///   既存の Care / Main / ConfirmPanel がそういう挙動だったため。
    /// </summary>
    public static string GetCharacterId(SaveData data)
    {
        if (data == null) return "";
        return !string.IsNullOrEmpty(data.selectedCharacterId)
            ? data.selectedCharacterId
            : data.characterId;
    }

    /// <summary>
    /// 画面に出すキャラクターの表示名を返す。
    ///
    /// 優先順位
    ///   ① petNickname（ユーザーが付けた名前）
    ///   ② キャラIDに対応する日本語名
    ///   ③ petName（無ければ空文字）
    ///
    /// ★Care/CareSceneManager.ResolveCharName() と Main/MainUIManager.SetPetInfo() の
    ///   挙動をそのまま移したもの。動きは変わっていない。
    /// </summary>
    public static string ResolveDisplayName(SaveData data)
    {
        if (data == null) return "";

        if (!string.IsNullOrEmpty(data.petNickname))
            return data.petNickname;

        return GetDefaultName(GetCharacterId(data), data.petName ?? "");
    }
}
