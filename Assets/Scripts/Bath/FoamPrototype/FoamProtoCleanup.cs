#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// 「いま画面に出ている泡は、どのシステムのものか」を確定させるための診断と、
    /// 試作が残した残骸の掃除。
    ///
    /// 【なぜ必要か】
    ///   泡の見え方だけでは、次のどれなのか区別できない。
    ///     A. 試作の泡粒（~FoamGrains の ParticleSystem）
    ///     B. 試作の泡シェル（~FoamShell_Head / ~FoamShell_Body）
    ///     C. 既存のスプライト泡（BubbleGroup の下の BubbleSprite）
    ///   どれか分からないまま直すと外す。だから先に数えて名前で出す。
    ///
    /// 【Play していなくても実行できる】
    ///   Resources.FindObjectsOfTypeAll は、HideFlags で隠されたオブジェクトも拾える。
    ///   FindFirstObjectByType では隠しオブジェクトが出てこないので使わない。
    /// </summary>
    public static class FoamProtoCleanup
    {
        private const string Root = "YURUFU/泡試作 Phase1/";

        /// <summary>試作が作るオブジェクトの名前の頭。これで残骸を見分ける。</summary>
        private const string OurPrefix = "~Foam";

        [MenuItem(Root + "★泡の出どころを調べる（Play中でなくても可）", false, 40)]
        public static void Diagnose()
        {
            // ★1本の長い Debug.Log にすると Console の一覧では1行目しか見えない。
            //   セクションごとに分けて出す。

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto診断] ===== 開始 =====  Play中={Application.isPlaying}  " +
                      $"アクティブなシーン='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");

            LogOurObjects();
            LogExistingBubbles();
            LogAwa3Users();
            LogAllParticleSystems();
            LogFoamLikeNames();
            LogDontDestroyOnLoad();

            // ★まとめは実際の数で分岐させる。前回は常に「試作の残骸が原因」と出ていて紛らわしかった
            int ours = FindOurObjects().Count;
            int awa  = CountAwa3Users();
            string verdict;
            if (ours > 0)      verdict = $"→ 試作の残骸が {ours} 個あります。「★試作の残骸を全部消す」で消えます";
            else if (awa > 0)  verdict = $"→ 試作の残骸は 0。泡3.png を使う描画が {awa} 個あります。上の③がその一覧＝犯人です";
            else               verdict = "→ 泡を描いているものは【1つも見つかりませんでした】。\n" +
                                         "      ★この診断は『実行した瞬間』の状態しか見ません。\n" +
                                         "      　画面に泡が見えている、まさにその状態で実行してください。\n" +
                                         "      　見えていないのに泡ゼロなら、それは正常な状態です（もう直っています）";

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto診断] ===== 終了 =====\n      {verdict}");
        }

        // ── ① 試作の残骸 ──
        private static void LogOurObjects()
        {
            var ours = FindOurObjects();
            var sb = new StringBuilder($"[FoamProto診断] ① 試作が作ったオブジェクト（\"{OurPrefix}\" で始まる）: {ours.Count} 個");
            foreach (var go in ours)
            {
                var ps = go.GetComponent<ParticleSystem>();
                var r  = go.GetComponent<Renderer>();
                sb.AppendLine();
                sb.Append($"      ・{go.name}  active={go.activeInHierarchy}  シーン='{go.scene.name}'  hideFlags={go.hideFlags}");
                if (r  != null) sb.Append($"  描画={r.enabled}");
                if (ps != null) sb.Append($"  粒={ps.particleCount}個");
            }
            if (ours.Count == 0) sb.Append("\n      → 画面の泡は試作のものではありません");
            Debug.Log(sb.ToString());
        }

        // ── ② 既存のスプライト泡 ──
        private static void LogExistingBubbles()
        {
            var sb = new StringBuilder("[FoamProto診断] ② 既存のスプライト泡（BathBubblePainter 方式）");
            int groups = 0, bubbles = 0;

            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || EditorUtility.IsPersistent(t.gameObject)) continue;
                if (t.name != "BubbleGroup") continue;
                groups++;
                sb.AppendLine();
                sb.Append($"      ・BubbleGroup  シーン='{t.gameObject.scene.name}'  active={t.gameObject.activeInHierarchy}  " +
                          $"子={t.childCount}個  パス={Path(t)}");
            }

            foreach (var b in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (b == null || EditorUtility.IsPersistent(b.gameObject)) continue;
                if (b.GetType().Name != "BubbleController") continue;
                bubbles++;
                if (bubbles <= 5)
                {
                    sb.AppendLine();
                    sb.Append($"      ・泡 '{Path(b.transform)}'  シーン='{b.gameObject.scene.name}'  active={b.gameObject.activeInHierarchy}");
                }
            }

            sb.AppendLine();
            sb.Append($"      合計: BubbleGroup {groups} 個 / BubbleController の泡 {bubbles} 個");
            Debug.Log(sb.ToString());
        }

        // ── ③ 泡3.png を使っている描画 ──
        private static void LogAwa3Users()
        {
            var sb = new StringBuilder("[FoamProto診断] ③ 泡3.png を使っている描画");
            int n = 0;

            foreach (var sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (sr == null || EditorUtility.IsPersistent(sr.gameObject)) continue;
                var tex = sr.sprite != null ? sr.sprite.texture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                n++;
                if (n <= 6)
                {
                    sb.AppendLine();
                    sb.Append($"      ・SpriteRenderer '{Path(sr.transform)}'  シーン='{sr.gameObject.scene.name}'  描画={sr.enabled}");
                }
            }

            foreach (var pr in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
            {
                if (pr == null || EditorUtility.IsPersistent(pr.gameObject)) continue;
                var tex = pr.sharedMaterial != null ? pr.sharedMaterial.mainTexture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                n++;
                sb.AppendLine();
                sb.Append($"      ・ParticleSystem '{Path(pr.transform)}'  シーン='{pr.gameObject.scene.name}'  描画={pr.enabled}");
            }

            // ★前回はここを見ていなかった。UI の Image / RawImage でも泡は出せる
            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (img == null || EditorUtility.IsPersistent(img.gameObject)) continue;
                var tex = img.sprite != null ? img.sprite.texture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                n++;
                sb.AppendLine();
                sb.Append($"      ・UI Image '{Path(img.transform)}'  シーン='{img.gameObject.scene.name}'  描画={img.enabled}");
            }

            foreach (var raw in Resources.FindObjectsOfTypeAll<RawImage>())
            {
                if (raw == null || EditorUtility.IsPersistent(raw.gameObject)) continue;
                var tex = raw.texture;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                n++;
                sb.AppendLine();
                sb.Append($"      ・UI RawImage '{Path(raw.transform)}'  シーン='{raw.gameObject.scene.name}'  描画={raw.enabled}");
            }

            sb.AppendLine();
            sb.Append(n == 0 ? "      合計 0 個 → 泡3.png は今どこにも描かれていません" : $"      合計 {n} 個");
            Debug.Log(sb.ToString());
        }

        /// <summary>泡3.png を使っている描画の数だけを数える（まとめの判定用）。</summary>
        private static int CountAwa3Users()
        {
            int n = 0;
            foreach (var sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
                if (sr != null && !EditorUtility.IsPersistent(sr.gameObject) && sr.sprite != null &&
                    sr.sprite.texture != null && sr.sprite.texture.name.Contains("泡3")) n++;
            foreach (var pr in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
                if (pr != null && !EditorUtility.IsPersistent(pr.gameObject) && pr.sharedMaterial != null &&
                    pr.sharedMaterial.mainTexture != null && pr.sharedMaterial.mainTexture.name.Contains("泡3")) n++;
            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
                if (img != null && !EditorUtility.IsPersistent(img.gameObject) && img.sprite != null &&
                    img.sprite.texture != null && img.sprite.texture.name.Contains("泡3")) n++;
            foreach (var raw in Resources.FindObjectsOfTypeAll<RawImage>())
                if (raw != null && !EditorUtility.IsPersistent(raw.gameObject) && raw.texture != null &&
                    raw.texture.name.Contains("泡3")) n++;
            return n;
        }

        // ── ④ いま動いている ParticleSystem を全部 ──
        private static void LogAllParticleSystems()
        {
            var sb = new StringBuilder("[FoamProto診断] ④ シーン上の ParticleSystem 一覧（粒が出ているものが怪しい）");
            int n = 0;

            foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
            {
                if (ps == null || EditorUtility.IsPersistent(ps.gameObject)) continue;
                n++;
                if (n > 25) continue;
                var pr  = ps.GetComponent<ParticleSystemRenderer>();
                var mat = pr != null ? pr.sharedMaterial : null;
                var tex = mat != null ? mat.mainTexture : null;
                sb.AppendLine();
                sb.Append($"      ・'{Path(ps.transform)}'  シーン='{ps.gameObject.scene.name}'  " +
                          $"active={ps.gameObject.activeInHierarchy}  粒={ps.particleCount}個  " +
                          $"マテリアル={(mat != null ? mat.name : "なし")}  絵={(tex != null ? tex.name : "なし")}");
            }

            sb.AppendLine();
            sb.Append($"      合計 {n} 個" + (n > 25 ? "（先頭25個だけ表示）" : ""));
            Debug.Log(sb.ToString());
        }

        // ── ⑤ 名前が泡っぽいオブジェクト ──
        private static void LogFoamLikeNames()
        {
            string[] keys = { "泡", "あわ", "アワ", "Bubble", "bubble", "Foam", "foam", "Awa", "Soap", "Suds" };
            var sb = new StringBuilder("[FoamProto診断] ⑤ 名前が泡っぽい GameObject");
            int n = 0;

            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || EditorUtility.IsPersistent(t.gameObject)) continue;
                bool hit = false;
                foreach (var k in keys) { if (t.name.Contains(k)) { hit = true; break; } }
                if (!hit) continue;

                n++;
                if (n > 30) continue;
                var r = t.GetComponent<Renderer>();
                sb.AppendLine();
                sb.Append($"      ・'{Path(t)}'  シーン='{t.gameObject.scene.name}'  active={t.gameObject.activeInHierarchy}" +
                          (r != null ? $"  描画={r.enabled}  型={r.GetType().Name}" : ""));
            }

            sb.AppendLine();
            sb.Append($"      合計 {n} 個" + (n > 30 ? "（先頭30個だけ表示）" : ""));
            Debug.Log(sb.ToString());
        }

        // ── ⑥ シーンをまたいで生き残っているもの ──
        private static void LogDontDestroyOnLoad()
        {
            var sb = new StringBuilder("[FoamProto診断] ⑥ DontDestroyOnLoad で生き残っている root オブジェクト");
            int n = 0;

            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || t.parent != null || EditorUtility.IsPersistent(t.gameObject)) continue;
                if (t.gameObject.scene.name != "DontDestroyOnLoad") continue;
                n++;
                if (n > 20) continue;
                sb.AppendLine();
                sb.Append($"      ・{t.name}（子={t.childCount}個）");
            }

            sb.AppendLine();
            sb.Append(n == 0 ? "      0 個（Play していないときは常にこうなります）" : $"      合計 {n} 個");
            Debug.Log(sb.ToString());
        }

        [MenuItem(Root + "★試作の残骸を全部消す", false, 41)]
        public static void CleanUp()
        {
            // ★消すのは、試作が自分で作った "~Foam" で始まるオブジェクトだけ。
            //   BubbleGroup や既存の泡、キャラ、シーンの中身には一切触らない。
            var ours = FindOurObjects();
            if (ours.Count == 0)
            {
                Debug.Log("[FoamProto] 試作の残骸はありません\n" +
                          "      （探索条件: 名前が \"~Foam\" で始まる / EditorUtility.IsPersistent が false）");
                return;
            }

            // ★削除前に、何を消すのかを必ず全部ログに出す。
            //   EditorUtility.IsPersistent が true のもの（プロジェクトの資産）は絶対に消さない。
            var names = new List<string>();
            foreach (var go in ours)
            {
                if (go == null) continue;   // 親を先に消したときに備える

                bool persistent = EditorUtility.IsPersistent(go);
                Debug.Log($"[FoamProto掃除] 対象: 名前='{go.name}'  InstanceID={go.GetInstanceID()}  " +
                          $"hideFlags={go.hideFlags}  scene.IsValid={go.scene.IsValid()}  " +
                          $"scene='{go.scene.name}'  IsPersistent={persistent}  " +
                          $"→ {(persistent ? "★資産なので消しません" : "削除します")}");

                if (persistent) continue;   // プロジェクトの資産は絶対に消さない

                names.Add($"{go.name}(InstanceID={go.GetInstanceID()})");
                Object.DestroyImmediate(go);
            }

            // 残っているマテリアル・RenderTexture・Bake 用メッシュも掃除する
            int assets = 0;
            foreach (var o in Resources.FindObjectsOfTypeAll<Object>())
            {
                if (o == null) continue;
                if (!(o is Material || o is RenderTexture || o is Mesh)) continue;
                if (string.IsNullOrEmpty(o.name)) continue;
                if (!o.name.StartsWith("~Foam") && !o.name.StartsWith("~foamproto")) continue;
                // ★二重の保険。どちらか一方でも「資産」と判定されたら消さない
                if (EditorUtility.IsPersistent(o)) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))) continue;
                Debug.Log($"[FoamProto掃除] 対象(資産以外のリソース): 型={o.GetType().Name}  名前='{o.name}'  " +
                          $"InstanceID={o.GetInstanceID()}  hideFlags={o.hideFlags}  IsPersistent=False → 削除します");
                Object.DestroyImmediate(o);
                assets++;
            }

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto] 試作の残骸を消しました\n" +
                      $"      オブジェクト {names.Count} 個: {string.Join(" / ", names)}\n" +
                      $"      マテリアル等 {assets} 個");
        }

        /// <summary>
        /// 試作が作ったオブジェクトだけを集める。隠しオブジェクトも拾う。
        ///
        /// ★除外条件は EditorUtility.IsPersistent（＝プロジェクトの資産かどうか）で判定する。
        ///   以前は go.scene.IsValid() で除外していたが、これは間違いだった。
        ///   HideFlags.DontSave が付いたまま元のシーンが閉じられたオブジェクトは
        ///   「どのシーンにも属さない」状態になり、scene.IsValid() が false を返す。
        ///   つまり、探している当の残骸だけを除外していた（実際に何度も 0 個と誤報告した）。
        /// </summary>
        private static List<GameObject> FindOurObjects()
        {
            var list = new List<GameObject>();
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null) continue;
                var go = t.gameObject;
                if (EditorUtility.IsPersistent(go)) continue;                 // Prefab 資産は除外
                if (!go.name.StartsWith(OurPrefix)) continue;
                if (!list.Contains(go)) list.Add(go);
            }
            return list;
        }

        private static string Path(Transform t)
        {
            var st = new Stack<string>();
            while (t != null) { st.Push(t.name); t = t.parent; }
            return string.Join("/", st.ToArray());
        }
    }
}
#endif
