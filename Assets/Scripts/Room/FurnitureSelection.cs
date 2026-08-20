using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「どのスロットに、どの家具を置いているか」を1件ぶん表す。
///
/// ★スロットもアイテムも「文字列」で保存する。
///   数値（enumの番号や配列の添字）で保存すると、あとで家具の並び順を変えたり
///   カテゴリを増やしたりした瞬間に、全ユーザーの部屋が別物に化ける。
///
/// categoryId の中身は SlotKey.ToString() の結果。
///   スロットが1つのカテゴリ → "RoomShell"
///   かべかざりの かべB      → "Decoration2#1"
/// 0番は今までと同じ文字列なので、この機能を入れる前のセーブもそのまま読める。
/// （フィールド名 categoryId は、既存のセーブデータと互換をとるため変えていない）
/// </summary>
[System.Serializable]
public class FurnitureSelection
{
    public string categoryId;   // 例: "RoomShell" / "Decoration2#1"
    public string itemId;       // 例: "RoomShell_Koko"

    public FurnitureSelection() { }

    public FurnitureSelection(SlotKey key, string itemId)
    {
        this.categoryId = key.ToString();
        this.itemId = itemId;
    }
}

/// <summary>
/// 家具の選択状態をセーブデータへ読み書きするためのヘルパー。
///
/// 【なぜ SaveData に直接メソッドを生やさないか】
///   SaveData は「入れ物」に徹してもらい、家具まわりの知識はここに閉じ込めるため。
///   将来この仕組みを変えても SaveData を触らずに済む。
/// </summary>
public static class RoomFurnitureSave
{
    /// <summary>
    /// セーブデータから全カテゴリの選択状態を読む。
    /// セーブが無い・古い・壊れている場合は空を返す（落とさない）。
    /// </summary>
    public static Dictionary<SlotKey, string> LoadAll()
    {
        var result = new Dictionary<SlotKey, string>();

        var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        if (data == null) return result;

        // ★JsonUtility は、JSON にその項目が無いとリストを null のままにする。
        //   この機能を入れる前のセーブデータを読むと必ずここに来るので、必ず補う。
        if (data.roomFurniture == null)
        {
            data.roomFurniture = new List<FurnitureSelection>();
            return result;
        }

        foreach (var s in data.roomFurniture)
        {
            if (s == null || string.IsNullOrEmpty(s.categoryId)) continue;

            // 知らないカテゴリ名は無視する。
            // （将来カテゴリ名を変えた場合などに、ここで落ちないようにする）
            if (!SlotKey.TryParse(s.categoryId, out SlotKey key))
            {
                Debug.LogWarning($"[RoomFurnitureSave] 未知のスロット '{s.categoryId}' を読み飛ばしました");
                continue;
            }

            result[key] = s.itemId;
        }

        return result;
    }

    /// <summary>
    /// 1スロットぶんの選択を書き込む。ファイルへの保存は行わない。
    /// まとめて変更したあとに Commit() を1回だけ呼ぶこと。
    ///
    /// ★カテゴリをそのまま渡せる（＝0番のスロットの意味）。
    /// </summary>
    public static void Set(SlotKey key, string itemId)
    {
        var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        if (data == null)
        {
            Debug.LogWarning("[RoomFurnitureSave] SaveData が取得できないため保存できません");
            return;
        }

        if (data.roomFurniture == null)
            data.roomFurniture = new List<FurnitureSelection>();

        string keyText = key.ToString();

        // 既にそのスロットの行があれば上書きする（行が増え続けないように）
        foreach (var s in data.roomFurniture)
        {
            if (s != null && s.categoryId == keyText)
            {
                s.itemId = itemId;
                return;
            }
        }

        data.roomFurniture.Add(new FurnitureSelection(key, itemId));
    }

    /// <summary>全スロットをまとめて書き込む。</summary>
    public static void SetAll(IDictionary<SlotKey, string> data)
    {
        if (data == null) return;
        foreach (var kv in data) Set(kv.Key, kv.Value);
    }

    /// <summary>変更をファイルへ書き出す。</summary>
    public static void Commit()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[RoomFurnitureSave] SaveManager が見つからないため保存できません");
            return;
        }
        SaveManager.Instance.Save();
    }
}
