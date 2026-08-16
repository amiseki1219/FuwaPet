using UnityEngine;

/// <summary>
/// セーブデータの家具設定を、起動時に部屋へ反映する。
///
/// 【どこに置くか】
///   部屋を表示する全シーンに1つずつ。
///     Main / Care / RoomEdit
///   空の GameObject を作ってアタッチすると、RoomFurnitureApplier も一緒に付く。
///   その Applier に「そのシーンの11スロット」と「カタログ」を結線する。
///
/// 【なぜ画面ごとに要るのか】
///   スロットの Transform はシーンごとに別物なので、共有できないため。
///   RoomEdit で保存した内容が Main にも出るのは、
///   「同じセーブデータを、それぞれの画面が自分のスロットへ適用する」から。
/// </summary>
[RequireComponent(typeof(RoomFurnitureApplier))]
public class RoomFurnitureLoader : MonoBehaviour
{
    [Tooltip("起動時にセーブデータを読み込んで反映する。\n" +
             "RoomEdit では RoomEditController が担当するので、そちらではオフでよい")]
    [SerializeField] private bool applyOnStart = true;

    [SerializeField] private bool verboseLog = true;

    private RoomFurnitureApplier _applier;

    private void Awake()
    {
        _applier = GetComponent<RoomFurnitureApplier>();
    }

    private void Start()
    {
        if (applyOnStart) Apply();
    }

    /// <summary>セーブデータの内容を部屋へ反映する。</summary>
    public void Apply()
    {
        if (_applier == null) _applier = GetComponent<RoomFurnitureApplier>();
        if (_applier == null) return;

        var catalog = _applier.Catalog;
        if (catalog == null)
        {
            Debug.LogError("[RoomFurnitureLoader] カタログが未設定です", this);
            return;
        }

        var saved = RoomFurnitureSave.LoadAll();
        int applied = 0;

        foreach (FurnitureCategory c in System.Enum.GetValues(typeof(FurnitureCategory)))
        {
            // カタログに1件も無いカテゴリは触らない。
            // （まだ登録していない家具のスロットを空にしてしまわないため）
            if (catalog.GetByCategory(c).Count == 0) continue;

            saved.TryGetValue(c, out string id);

            // id が null でも Applier 側が既定へフォールバックしてくれる
            if (_applier.Apply(c, id)) applied++;
        }

        if (verboseLog)
            Debug.Log($"[RoomFurnitureLoader] セーブから {applied} カテゴリを反映しました", this);
    }
}
