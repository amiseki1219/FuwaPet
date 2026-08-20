using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FurnitureCatalog の Inspector をカテゴリ別の折りたたみ表示にする Editor 拡張。
///
/// 【なぜ作ったか】
///   標準の Inspector だと全カテゴリのアイテムが1本のリストにベタ並びになる。
///   42行を超えたあたりから、目的の行を探すだけで一苦労になっていた。
///   さらに「category を選び間違える」「Prefab 欄に FBX を入れてしまう」といった
///   目視では気づけないミスが実際に発生したため、検出も一緒に載せている。
///
/// 【重要】
///   この拡張は「表示のしかた」を変えるだけで、保存されるデータの形は一切変えていない。
///   FurnitureCatalog.cs の entries（List<FurnitureEntry>）をそのまま読み書きしている。
///   なので、このファイルを削除すれば標準の Inspector に戻るだけで、データは無傷。
///
/// 【置き場所】
///   Assets/Editor/ 配下に置くこと。ここに置いたスクリプトはビルドに含まれないため、
///   アプリ本体の容量にも実行時の動作にも一切影響しない。
/// </summary>
[CustomEditor(typeof(FurnitureCatalog))]
public class FurnitureCatalogEditor : Editor
{
    // entries は private [SerializeField] なので、SerializedProperty 経由で触る。
    // こうすると Undo（⌘Z）と「変更あり」マークが自動で効く。
    // 直接 target をキャストして書き換えると、どちらも効かず保存し忘れの事故になる。
    private SerializedProperty _entries;

    /// <summary>カテゴリの日本語表示名。enum の並び順と対応させること。</summary>
    private static readonly string[] CategoryLabels =
    {
        "ベッド",         // Bed
        "テーブル",       // Table
        "ソファ",         // Sofa
        "壁掛け棚",       // WallShelf
        "本棚",           // Shelf
        "窓",             // Window
        "サイドテーブル", // Nightstand
        "ルームライト",   // RoomLight
        "装飾",           // Decoration
        "ラグマット",     // Rug
        "お部屋",         // RoomShell
        "かべかざり",     // Decoration2
    };

    // 折りたたみの開閉状態は EditorPrefs に覚えさせる。
    // 覚えないと、再生ボタンを押したり別アセットを選ぶたびに全部閉じてしまい、
    // 「さっき開いていたカテゴリをまた探す」という無駄が毎回発生する。
    private const string FoldoutKeyPrefix = "YURUFU.FurnitureCatalog.Foldout.";

    // ─────────────────────────────────────────────
    // 「あとで実行する操作」の予約
    //
    // ★描画の途中でリストを増減させてはいけない。
    //   Inspector の描画は1フレームに複数回（Layout と Repaint）走り、
    //   その途中で要素数が変わると
    //     ・すでに集めたインデックスが1つずつズレて、別の行を消してしまう
    //     ・「Layout と Repaint で GUI の数が違う」というエラーが出る
    //   という2つの事故が起きる。実際に起こしやすい典型的な罠。
    //
    //   なので、ボタンが押された時点では「何をするか」を覚えるだけにして、
    //   全部を描き終わったあとでまとめて1回だけ実行する。
    // ─────────────────────────────────────────────
    private enum PendingKind { None, Delete, Move, Add }

    private PendingKind _pendingKind = PendingKind.None;
    private int _pendingFrom;      // Delete / Move の対象。Add では挿入位置
    private int _pendingTo;        // Move の移動先
    private int _pendingCategory;  // Add で設定するカテゴリ

    private void OnEnable()
    {
        _entries = serializedObject.FindProperty("entries");
    }

    /// <summary>
    /// カテゴリの日本語名を返す。
    /// enum に値を足して CategoryLabels の更新を忘れても、
    /// そのカテゴリが Inspector から消えてしまわないよう enum 名で代用する。
    /// </summary>
    private static string CategoryLabel(int cat)
    {
        if (cat >= 0 && cat < CategoryLabels.Length) return CategoryLabels[cat];
        return ((FurnitureCategory)cat).ToString();
    }

    /// <summary>enum に定義されているカテゴリの数。ラベル配列ではなく enum を正とする。</summary>
    private static int CategoryCount => System.Enum.GetValues(typeof(FurnitureCategory)).Length;

    public override void OnInspectorGUI()
    {
        // FurnitureCatalog.cs 側のフィールド名が変わると null になる。
        // 黙って何も出ないと原因が分からないので、はっきり知らせて標準表示に落とす
        if (_entries == null)
        {
            EditorGUILayout.HelpBox(
                "entries フィールドが見つかりませんでした。\n" +
                "FurnitureCatalog.cs のフィールド名が変わった可能性があります。\n" +
                "標準の Inspector を表示します。", MessageType.Error);
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        // ── 先に全行を走査して、カテゴリ別のインデックスと問題点を集める ──
        // 描画しながら調べると「ID重複」のような行をまたぐ検査ができないため、
        // 描画の前に1回だけまとめて解析する
        var indicesByCategory = new Dictionary<int, List<int>>();
        var problemsByIndex = new Dictionary<int, List<string>>();
        CollectEntries(indicesByCategory, problemsByIndex);

        DrawHeader(problemsByIndex);

        EditorGUILayout.Space(4);

        // ラベルの更新漏れは表示が分かりにくくなるだけで実害は無いので、注意喚起に留める
        if (CategoryLabels.Length != CategoryCount)
        {
            EditorGUILayout.HelpBox(
                $"FurnitureCategory は {CategoryCount} 個ですが、日本語ラベルは {CategoryLabels.Length} 個です。\n" +
                "FurnitureCatalogEditor.cs の CategoryLabels に追記すると表示が分かりやすくなります。",
                MessageType.Info);
        }

        // ── カテゴリごとに描く ──
        int categoryCount = CategoryCount;
        for (int cat = 0; cat < categoryCount; cat++)
        {
            indicesByCategory.TryGetValue(cat, out var indices);
            indices ??= new List<int>();
            DrawCategory(cat, indices, problemsByIndex);
        }

        EditorGUILayout.Space(8);
        DrawUnknownCategoryWarning(indicesByCategory, categoryCount);

        // 描き終わってから、予約されていた追加・削除・並べ替えを実行する
        ExecutePendingAction();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>予約された操作を1件だけ実行する。描画が全部終わったあとに呼ぶこと。</summary>
    private void ExecutePendingAction()
    {
        if (_pendingKind == PendingKind.None) return;

        var kind = _pendingKind;
        _pendingKind = PendingKind.None; // 二重実行を防ぐため、先に消す

        switch (kind)
        {
            case PendingKind.Delete:
                if (_pendingFrom >= 0 && _pendingFrom < _entries.arraySize)
                    _entries.DeleteArrayElementAtIndex(_pendingFrom);
                break;

            case PendingKind.Move:
                if (_pendingFrom >= 0 && _pendingFrom < _entries.arraySize &&
                    _pendingTo >= 0 && _pendingTo < _entries.arraySize)
                    _entries.MoveArrayElement(_pendingFrom, _pendingTo);
                break;

            case PendingKind.Add:
                InsertNewEntry(_pendingFrom, _pendingCategory);
                break;
        }

        // 要素数が変わったので、次の描画をやり直させる
        Repaint();
    }

    /// <summary>指定位置に新しいアイテムを挿入して初期化する。</summary>
    private void InsertNewEntry(int insertAt, int cat)
    {
        insertAt = Mathf.Clamp(insertAt, 0, _entries.arraySize);
        _entries.InsertArrayElementAtIndex(insertAt);

        var e = _entries.GetArrayElementAtIndex(insertAt);

        // InsertArrayElementAtIndex は直前の要素の値をコピーしてくることがある。
        // そのままだと ID や Prefab が複製されて「IDの重複」を自分で作ってしまうので、
        // 全項目を明示的に初期化する
        e.FindPropertyRelative("category").enumValueIndex = cat;
        e.FindPropertyRelative("id").stringValue = ((FurnitureCategory)cat) + "_";
        e.FindPropertyRelative("displayName").stringValue = "";
        e.FindPropertyRelative("prefab").objectReferenceValue = null;
        e.FindPropertyRelative("isEmptySlot").boolValue = false;
        e.FindPropertyRelative("thumbnail").objectReferenceValue = null;
        e.FindPropertyRelative("ownedByDefault").boolValue = true;
    }

    // ─────────────────────────────────────────────
    // 解析
    // ─────────────────────────────────────────────

    /// <summary>全行を走査して、カテゴリ別のインデックスと、行ごとの問題点を集める。</summary>
    private void CollectEntries(
        Dictionary<int, List<int>> indicesByCategory,
        Dictionary<int, List<string>> problemsByIndex)
    {
        // ID の重複を調べるため、「そのIDが何行目に出てきたか」を覚えておく
        var idFirstSeenAt = new Dictionary<string, int>();

        for (int i = 0; i < _entries.arraySize; i++)
        {
            var e = _entries.GetArrayElementAtIndex(i);
            if (e == null) continue;

            int cat = e.FindPropertyRelative("category").enumValueIndex;

            if (!indicesByCategory.TryGetValue(cat, out var list))
            {
                list = new List<int>();
                indicesByCategory[cat] = list;
            }
            list.Add(i);

            var problems = InspectEntry(e, cat, i, idFirstSeenAt);
            if (problems.Count > 0) problemsByIndex[i] = problems;
        }
    }

    /// <summary>1行ぶんの入力ミスを調べる。見つかった問題を日本語で返す。</summary>
    private List<string> InspectEntry(
        SerializedProperty e, int cat, int index, Dictionary<string, int> idFirstSeenAt)
    {
        var problems = new List<string>();

        var pId = e.FindPropertyRelative("id");
        var pPrefab = e.FindPropertyRelative("prefab");
        var pEmpty = e.FindPropertyRelative("isEmptySlot");
        var pThumb = e.FindPropertyRelative("thumbnail");

        string id = pId.stringValue;
        bool isEmpty = pEmpty.boolValue;

        // ---- ID ----
        if (string.IsNullOrEmpty(id))
        {
            problems.Add("ID が空です。セーブデータの保存に使われるので必ず入れてください");
        }
        else
        {
            if (idFirstSeenAt.TryGetValue(id, out int firstAt))
                problems.Add($"ID が {firstAt + 1} 行目と重複しています。セーブ復元時に事故になります");
            else
                idFirstSeenAt[id] = index;

            // 命名規則。動作はするが、後から見て分からなくなるので警告に留める
            string expectedPrefix = ((FurnitureCategory)cat) + "_";
            if (!id.StartsWith(expectedPrefix))
                problems.Add($"ID が命名規則「{expectedPrefix}〇〇」から外れています");
        }

        // ---- Prefab ----
        // 参照切れ（Missing）の判定。
        // 参照先を消した場合、objectReferenceValue は null になるが
        // instanceID のほうには値が残る。この差で「未設定」と「参照切れ」を区別できる
        bool prefabMissing =
            pPrefab.objectReferenceValue == null && pPrefab.objectReferenceInstanceIDValue != 0;

        if (prefabMissing)
        {
            problems.Add("Prefab の参照が切れています（Missing）。入れ直してください");
        }
        else if (pPrefab.objectReferenceValue == null)
        {
            // 空欄。「なし」の行なら正しいが、チェックが無いなら設定漏れ
            if (!isEmpty)
                problems.Add("Prefab が空です。「なし」の行なら Is Empty Slot にチェックを入れてください");
        }
        else
        {
            // ★ここが今回いちばん効く検査。
            //   Prefab 欄には FBX もドラッグできてしまうが、FBX を直接入れると
            //   Prefab 側に入れた位置・回転・大きさの調整がまるごと効かなくなる。
            //   見た目では気づきにくく、実際に位置ズレの原因になった
            string path = AssetDatabase.GetAssetPath(pPrefab.objectReferenceValue);
            if (!string.IsNullOrEmpty(path) &&
                !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                problems.Add(
                    $"Prefab 欄に .prefab 以外が入っています（{System.IO.Path.GetExtension(path)}）。\n" +
                    "FBX を直接入れると位置・向き・大きさの調整が効きません。" +
                    "Furniture_〇〇.prefab を入れてください");
            }

            if (isEmpty)
                problems.Add("Is Empty Slot にチェックが入っているので、この Prefab は無視されます");
        }

        // ---- サムネイル ----
        if (pThumb.objectReferenceValue == null && pThumb.objectReferenceInstanceIDValue != 0)
            problems.Add("サムネイルの参照が切れています（Missing）。画像を入れ直してください");

        return problems;
    }

    // ─────────────────────────────────────────────
    // 描画
    // ─────────────────────────────────────────────

    /// <summary>件数と問題数のサマリー、全開閉ボタン。</summary>
    private void DrawHeader(Dictionary<int, List<string>> problemsByIndex)
    {
        EditorGUILayout.LabelField("家具カタログ", EditorStyles.boldLabel);

        int problemRows = problemsByIndex.Count;
        string summary = $"全 {_entries.arraySize} 件";

        if (problemRows > 0)
            EditorGUILayout.HelpBox($"{summary} ／ 確認が必要な行が {problemRows} 件あります",
                                    MessageType.Warning);
        else
            EditorGUILayout.HelpBox($"{summary} ／ 問題は見つかりませんでした", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("すべて開く")) SetAllFoldouts(true);
            if (GUILayout.Button("すべて閉じる")) SetAllFoldouts(false);
        }
    }

    /// <summary>カテゴリ1つぶんを折りたたみで描く。</summary>
    private void DrawCategory(int cat, List<int> indices, Dictionary<int, List<string>> problemsByIndex)
    {
        string enumName = ((FurnitureCategory)cat).ToString();
        string label = $"{CategoryLabel(cat)}（{enumName}）  {indices.Count} 件";

        // このカテゴリに問題があるなら、閉じていても分かるように印を付ける
        int problemCount = 0;
        foreach (int i in indices) if (problemsByIndex.ContainsKey(i)) problemCount++;
        if (problemCount > 0) label += $"   ⚠ {problemCount}";

        string key = FoldoutKeyPrefix + cat;
        bool open = EditorPrefs.GetBool(key, false);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            bool newOpen = EditorGUILayout.Foldout(open, label, true, EditorStyles.foldoutHeader);
            if (newOpen != open) EditorPrefs.SetBool(key, newOpen);

            if (!newOpen) return;

            EditorGUILayout.Space(2);

            if (indices.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "まだアイテムがありません。下のボタンから追加してください",
                    EditorStyles.miniLabel);
            }

            for (int n = 0; n < indices.Count; n++)
            {
                DrawEntry(indices, n, problemsByIndex);
            }

            EditorGUILayout.Space(2);

            if (GUILayout.Button($"＋ {CategoryLabel(cat)} にアイテムを追加"))
            {
                // 同じカテゴリの行が固まっていたほうが .asset の差分が読みやすいので、
                // リストの末尾ではなく「そのカテゴリの最後の行の次」に挿入する
                _pendingKind = PendingKind.Add;
                _pendingFrom = indices.Count > 0 ? indices[indices.Count - 1] + 1 : _entries.arraySize;
                _pendingCategory = cat;
            }
        }
    }

    /// <summary>アイテム1件ぶんを描く。</summary>
    private void DrawEntry(List<int> indices, int nth, Dictionary<int, List<string>> problemsByIndex)
    {
        int i = indices[nth];
        var e = _entries.GetArrayElementAtIndex(i);

        var pId = e.FindPropertyRelative("id");
        var pName = e.FindPropertyRelative("displayName");
        var pPrefab = e.FindPropertyRelative("prefab");
        var pEmpty = e.FindPropertyRelative("isEmptySlot");
        var pThumb = e.FindPropertyRelative("thumbnail");
        var pOwned = e.FindPropertyRelative("ownedByDefault");

        bool hasProblem = problemsByIndex.ContainsKey(i);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // ---- 1行目: 並べ替え・削除・ID・表示名 ----
            using (new EditorGUILayout.HorizontalScope())
            {
                // 幅を狭く固定しておかないと、ボタンが間延びして押し間違える
                // 表示上は「そのカテゴリの中で1つ動かす」だが、実データは1本のリストなので、
                // 実際のインデックス同士を入れ替える
                using (new EditorGUI.DisabledScope(nth == 0))
                    if (GUILayout.Button("▲", GUILayout.Width(24)))
                    {
                        _pendingKind = PendingKind.Move;
                        _pendingFrom = i;
                        _pendingTo = indices[nth - 1];
                    }

                using (new EditorGUI.DisabledScope(nth == indices.Count - 1))
                    if (GUILayout.Button("▼", GUILayout.Width(24)))
                    {
                        _pendingKind = PendingKind.Move;
                        _pendingFrom = i;
                        _pendingTo = indices[nth + 1];
                    }

                if (hasProblem) GUILayout.Label("⚠", GUILayout.Width(16));

                EditorGUIUtility.labelWidth = 24;
                EditorGUILayout.PropertyField(pId, new GUIContent("ID"));
                EditorGUILayout.PropertyField(pName, new GUIContent("名"));
                EditorGUIUtility.labelWidth = 0;

                // 削除は取り返しがつかないので必ず確認を挟む
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    if (EditorUtility.DisplayDialog(
                            "アイテムを削除しますか？",
                            $"「{pId.stringValue}」をカタログから削除します。\n\n" +
                            "このIDを保存しているセーブデータは、既定のアイテムに\n" +
                            "フォールバックするようになります。",
                            "削除する", "やめる"))
                    {
                        _pendingKind = PendingKind.Delete;
                        _pendingFrom = i;
                    }
                }
            }

            // ---- 2行目: Prefab と「なし」 ----
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 48;
                EditorGUILayout.PropertyField(pPrefab, new GUIContent("Prefab"));
                EditorGUIUtility.labelWidth = 36;
                EditorGUILayout.PropertyField(pEmpty, new GUIContent("なし"), GUILayout.Width(56));
                EditorGUIUtility.labelWidth = 0;
            }

            // ---- 3行目: サムネイルと所持 ----
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 48;
                EditorGUILayout.PropertyField(pThumb, new GUIContent("サムネ"));
                EditorGUIUtility.labelWidth = 36;
                EditorGUILayout.PropertyField(pOwned, new GUIContent("所持"), GUILayout.Width(56));
                EditorGUIUtility.labelWidth = 0;
            }

            // ---- 問題があれば、その行の直下に出す ----
            // 一覧の下にまとめて出すと「どの行の話か」が分からなくなるため、必ず行の直下に置く
            if (hasProblem)
            {
                foreach (string p in problemsByIndex[i])
                    EditorGUILayout.HelpBox(p, MessageType.Warning);
            }
        }
    }

    /// <summary>enum に無いカテゴリ番号の行が残っていないか調べて知らせる。</summary>
    private void DrawUnknownCategoryWarning(Dictionary<int, List<int>> indicesByCategory, int categoryCount)
    {
        var unknown = new List<int>();
        foreach (var kv in indicesByCategory)
            if (kv.Key < 0 || kv.Key >= categoryCount) unknown.AddRange(kv.Value);

        if (unknown.Count == 0) return;

        EditorGUILayout.HelpBox(
            $"カテゴリ番号が不明な行が {unknown.Count} 件あります。\n" +
            "FurnitureCategory から値を削除したか、途中に挿入した可能性があります。\n" +
            "下の「標準の表示」から中身を確認してください。", MessageType.Error);
    }

    // ─────────────────────────────────────────────
    // 操作
    // ─────────────────────────────────────────────

    private void SetAllFoldouts(bool open)
    {
        for (int cat = 0; cat < CategoryCount; cat++)
            EditorPrefs.SetBool(FoldoutKeyPrefix + cat, open);
    }
}
