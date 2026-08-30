#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// Editor メニューから試作を出し入れするだけの入口。
    ///
    /// Scene も Prefab も変更しない。Play 中に GameObject を1つ作るだけで、
    /// Play を止めるかシーンを移動すれば消える（hideFlags は付けない。理由は Enable() のコメント）。
    /// このフォルダを削除すれば跡形もなく消える。
    /// </summary>
    public static class FoamProtoBootstrap
    {
        private const string Root   = "YURUFU/泡試作 Phase1/";
        private const string ObjName = "~FoamProtoController";

        [MenuItem(Root + "試作を有効にする（Play中）", false, 1)]
        public static void Enable()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("泡シェル試作 Phase 1",
                    "Play 中に実行してください。\n\nキャラは実行時に生成されるため、\nPlay していないと対象が見つかりません。", "OK");
                return;
            }

            var already = Find();
            if (already != null)
            {
                Debug.Log("[FoamProto] すでに有効です（もう一度出したいときは先に「試作を無効にする」を押してください）");
                return;
            }

            // ★hideFlags は付けない。
            //   HideFlags.DontSave が付いたオブジェクトは、シーンを切り替えても
            //   Play を止めても破棄されず、Editor に残骸として居座る。
            //   （Main / Care / お風呂 に泡が出続けた原因はこれ）
            //   Play 中のシーンは保存できないので、フラグ無しでも汚れる心配はない。
            //   Hierarchy に見えるので、残っていればすぐ気づける利点もある。
            var go = new GameObject(ObjName);
            var c = go.AddComponent<FoamProtoController>();
            if (!c.Setup())
            {
                Object.DestroyImmediate(go);
                Debug.LogError("[FoamProto] 起動に失敗しました。上のエラーを確認してください");
            }
        }

        [MenuItem(Root + "試作を無効にする", false, 2)]
        public static void Disable()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            Object.DestroyImmediate(c.gameObject);
        }

        [MenuItem(Root + "マスクを全消去", false, 20)]
        public static void ClearMasks()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.ClearMasks();
        }

        [MenuItem(Root + "既存の泡 表示 / 非表示", false, 21)]
        public static void ToggleExisting()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.ToggleExistingBubbles();
        }

        [MenuItem(Root + "切り分け1: マスクを全面白にする", false, 30)]
        public static void TestFill()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.TestFillMask();
        }

        [MenuItem(Root + "切り分け2: レイ自己診断", false, 31)]
        public static void SelfTest()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.SelfTestRays();
        }

        [MenuItem(Root + "切り分け3: 上半分(X>=0)だけ全部塗る", false, 32)]
        public static void PaintUpper()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.TestPaintHalf(true);
        }

        [MenuItem(Root + "切り分け3: 下半分(X<0)だけ全部塗る", false, 33)]
        public static void PaintLower()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.TestPaintHalf(false);
        }

        // ── 比較画像用の表示切り替え ──────────────────────────────────────
        // ①→②→③ の順に切り替えてスクリーンショットを撮ると、比較画像がそろう。

        [MenuItem(Root + "表示/① 泡シェルだけ", false, 23)]
        public static void ShowShellOnly()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.SetDisplay(true, false);
        }

        [MenuItem(Root + "表示/② 泡3.png の粒だけ", false, 24)]
        public static void ShowGrainOnly()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.SetDisplay(false, true);
        }

        [MenuItem(Root + "表示/③ 両方（既定）", false, 25)]
        public static void ShowBoth()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.SetDisplay(true, true);
        }

        [MenuItem(Root + "デバッグ表示 ON / OFF", false, 22)]
        public static void ToggleGui()
        {
            var c = Find();
            if (c == null) { Debug.Log("[FoamProto] 有効になっていません"); return; }
            c.ShowDebugGui = !c.ShowDebugGui;
        }

        /// <summary>
        /// 動いている試作を探す。
        ///
        /// ★FindFirstObjectByType / FindObjectsByType は
        ///   HideFlags.HideAndDontSave が付いたオブジェクトを返さない。
        ///   本体はそのフラグで作っているので、まず静的参照を見る。
        ///   念のため Resources.FindObjectsOfTypeAll も試す（こちらは隠しオブジェクトも拾える）。
        /// </summary>
        // ── 開発用: 表示キャラの切り替え ────────────────────────────────
        //
        // ★SaveData の selectedCharacterId を書き換えます。
        //   試作の確認のためだけの機能で、押さなければ何も起きません。
        //   元に戻したいときは同じメニューで別のキャラを選べば戻せます。
        //   切り替えたあとは、お風呂シーンに入り直してください（Care へ戻って再度おふろ）。

        private static void SetCharacter(string id)
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            if (save == null) { Debug.LogWarning("[FoamProto][開発用] SaveData が取得できません。Play 中に実行してください"); return; }

            string before = save.selectedCharacterId;
            save.selectedCharacterId = id;
            SaveManager.Instance.Save();

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto][開発用] 表示キャラを変更しました {before} → {id}\n" +
                      "      反映するには、お風呂シーンに入り直してください（Care へ戻って、もう一度おふろ）");
        }

        [MenuItem(Root + "開発用/表示キャラを piyoko にする", false, 50)]
        private static void SetPiyoko() => SetCharacter("piyoko");

        [MenuItem(Root + "開発用/表示キャラを koko にする", false, 51)]
        private static void SetKoko() => SetCharacter("koko");

        [MenuItem(Root + "開発用/表示キャラを poko にする", false, 52)]
        private static void SetPoko() => SetCharacter("poko");

        [MenuItem(Root + "開発用/表示キャラを eru にする", false, 53)]
        private static void SetEru() => SetCharacter("eru");

        [MenuItem(Root + "開発用/表示キャラを paru にする", false, 54)]
        private static void SetParu() => SetCharacter("paru");

        private static FoamProtoController Find()
        {
            if (FoamProtoController.Instance != null) return FoamProtoController.Instance;

            foreach (var c in Resources.FindObjectsOfTypeAll<FoamProtoController>())
            {
                if (c != null && c.gameObject.scene.IsValid()) return c;   // Prefab 資産ではなくシーン上のもの
            }
            return null;
        }
    }
}
#endif
