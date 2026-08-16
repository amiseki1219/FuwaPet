using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「どのカテゴリに、どの家具を置いているか」を1件ぶん表す。
///
/// ★カテゴリもアイテムも「文字列」で保存する。
///   数値（enumの番号や配列の添字）で保存すると、あとで家具の並び順を変えたり
///   カテゴリを増やしたりした瞬間に、全ユーザーの部屋が別物に化ける。
/// </summary>
[System.Serializable]
public class FurnitureSelection
{
    public string categoryId;   // 例: "RoomShell"
    public string itemId;       // 例: "RoomShell_Koko"

    public FurnitureSelection() { }

    public FurnitureSelection(FurnitureCategory category, string itemId)
    {
        this.categoryId = category.ToString();
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
    public static Dictionary<FurnitureCategory, string> LoadAll()
    {
        var result = new Dictionary<FurnitureCategory, string>();

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
            if (!System.Enum.TryParse(s.categoryId, out FurnitureCategory cat))
            {
                Debug.LogWarning($"[RoomFurnitureSave] 未知のカテゴリ '{s.categoryId}' を読み飛ばしました");
                continue;
            }

            result[cat] = s.itemId;
        }

        return result;
    }

    /// <summary>
    /// 1カテゴリぶんの選択を書き込む。ファイルへの保存は行わない。
    /// まとめて変更したあとに SaveManager.Save() を1回だけ呼ぶこと。
    /// </summary>
    public static void Set(FurnitureCategory category, string itemId)
    {
        var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        if (data == null)
        {
            Debug.LogWarning("[RoomFurnitureSave] SaveData が取得できないため保存できません");
            return;
        }

        if (data.roomFurniture == null)
            data.roomFurniture = new List<FurnitureSelection>();

        string key = category.ToString();

        // 既にその カテゴリの行があれば上書きする（行が増え続けないように）
        foreach (var s in data.roomFurniture)
        {
            if (s != null && s.categoryId == key)
            {
                s.itemId = itemId;
                return;
            }
        }

        data.roomFurniture.Add(new FurnitureSelection(category, itemId));
    }

    /// <summary>全カテゴリをまとめて書き込む。</summary>
    public static void SetAll(IDictionary<FurnitureCategory, string> data)
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
