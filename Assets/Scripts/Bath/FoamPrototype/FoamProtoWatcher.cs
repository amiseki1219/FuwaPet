#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// 「泡を描いているもの」を Play 中ずっと見張って、増えた／減った瞬間だけ Console に出す。
    ///
    /// 【なぜ必要か】
    ///   メニューの診断は「押した瞬間」しか見ない。
    ///   泡が見えているタイミングと診断を押すタイミングがずれると、
    ///   「画面には泡があるのにログは 0 個」という食い違いが起きる（実際に起きた）。
    ///   人が押すタイミングに頼るのをやめて、変化した瞬間を自動で記録する。
    ///
    /// 【うるさくならない工夫】
    ///   0.5秒ごとに数えるが、前回と同じなら何も出さない。
    ///   変わったときだけ1行出す。毎フレームのログは増やさない。
    /// </summary>
    [InitializeOnLoad]
    public static class FoamProtoWatcher
    {
        private const string Root    = "YURUFU/泡試作 Phase1/";
        private const string PrefKey = "Yurufu.FoamProtoWatcher.Enabled";

        private static double _next;
        private static string _lastSignature = "";

        static FoamProtoWatcher()
        {
            EditorApplication.update += Tick;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);   // 既定は ON
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(Root + "泡の監視 ON / OFF", false, 42)]
        private static void ToggleWatch()
        {
            Enabled = !Enabled;
            _lastSignature = "";
            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto監視] {(Enabled ? "ON にしました（Play中、泡を描くものが増減した瞬間にログを出します）" : "OFF にしました")}");
        }

        [MenuItem(Root + "泡の監視 ON / OFF", true)]
        private static bool ToggleWatchValidate()
        {
            Menu.SetChecked(Root + "泡の監視 ON / OFF", Enabled);
            return true;
        }

        private static void Tick()
        {
            if (!Enabled || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.5;

            var lines = new List<string>();
            string sig = Collect(lines);

            if (sig == _lastSignature) return;
            _lastSignature = sig;

            var sb = new StringBuilder("[FoamProto監視] 泡を描くものが変わりました  ");
            sb.Append($"シーン='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");
            if (lines.Count == 0) sb.Append("\n      → いま 0 個です");
            foreach (var l in lines) { sb.AppendLine(); sb.Append("      " + l); }

            Debug.Log($"<color=#00E5FF>[決定]</color> " + sb);
        }

        /// <summary>泡を描いていそうなものを集めて、状態を表す文字列を返す。</summary>
        private static string Collect(List<string> lines)
        {
            var sb = new StringBuilder();

            // ① 試作が作ったもの
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || EditorUtility.IsPersistent(t.gameObject)) continue;
                if (!t.name.StartsWith("~Foam")) continue;
                var ps = t.GetComponent<ParticleSystem>();
                string s = $"[試作] {t.name}  シーン='{t.gameObject.scene.name}'  active={t.gameObject.activeInHierarchy}" +
                           (ps != null ? $"  粒={ps.particleCount}個" : "");
                lines.Add(s); sb.Append(s).Append('|');
            }

            // ② 泡3.png を使っている描画
            foreach (var sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (sr == null || EditorUtility.IsPersistent(sr.gameObject) || !sr.enabled) continue;
                var tex = sr.sprite != null ? sr.sprite.texture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                string s = $"[泡3/Sprite] {Path(sr.transform)}  シーン='{sr.gameObject.scene.name}'";
                lines.Add(s); sb.Append(s).Append('|');
            }
            foreach (var pr in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
            {
                if (pr == null || EditorUtility.IsPersistent(pr.gameObject) || !pr.enabled) continue;
                var tex = pr.sharedMaterial != null ? pr.sharedMaterial.mainTexture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                string s = $"[泡3/Particle] {Path(pr.transform)}  シーン='{pr.gameObject.scene.name}'";
                lines.Add(s); sb.Append(s).Append('|');
            }
            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (img == null || EditorUtility.IsPersistent(img.gameObject) || !img.enabled) continue;
                var tex = img.sprite != null ? img.sprite.texture : null;
                if (tex == null || !tex.name.Contains("泡3")) continue;
                string s = $"[泡3/UI] {Path(img.transform)}  シーン='{img.gameObject.scene.name}'";
                lines.Add(s); sb.Append(s).Append('|');
            }

            // ③ 既存のスプライト泡
            int bubbles = 0;
            foreach (var b in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (b == null || EditorUtility.IsPersistent(b.gameObject)) continue;
                if (b.GetType().Name != "BubbleController") continue;
                bubbles++;
            }
            if (bubbles > 0)
            {
                string s = $"[既存の泡] BubbleController {bubbles} 個";
                lines.Add(s); sb.Append(s).Append('|');
            }

            return sb.ToString();
        }

        // ── 見つからないとき用: いま描かれているものを全部書き出す ──────────────

        [MenuItem(Root + "★いま描画されているものを全部書き出す", false, 43)]
        public static void DumpAllVisible()
        {
            var lines = new List<string>();

            foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (r == null || EditorUtility.IsPersistent(r.gameObject)) continue;
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;

                var mat = r.sharedMaterial;
                var tex = mat != null ? mat.mainTexture : null;
                lines.Add($"{r.GetType().Name,-22} '{Path(r.transform)}'  シーン='{r.gameObject.scene.name}'  " +
                          $"シェーダー={(mat != null && mat.shader != null ? mat.shader.name : "なし")}  " +
                          $"絵={(tex != null ? tex.name : "なし")}");
            }

            foreach (var g in Resources.FindObjectsOfTypeAll<Graphic>())
            {
                if (g == null || EditorUtility.IsPersistent(g.gameObject)) continue;
                if (!g.enabled || !g.gameObject.activeInHierarchy) continue;

                var tex = g.mainTexture;
                lines.Add($"{g.GetType().Name,-22} '{Path(g.transform)}'  シーン='{g.gameObject.scene.name}'  " +
                          $"絵={(tex != null ? tex.name : "なし")}");
            }

            lines.Sort();

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto全描画] いま描画されているもの: {lines.Count} 個" +
                      "（このあと25個ずつログに出します。泡らしい名前・絵を探してください）");

            const int Chunk = 25;
            for (int i = 0; i < lines.Count; i += Chunk)
            {
                var sb = new StringBuilder($"[FoamProto全描画] {i + 1} 〜 {Mathf.Min(i + Chunk, lines.Count)} 個目");
                for (int k = i; k < Mathf.Min(i + Chunk, lines.Count); k++) { sb.AppendLine(); sb.Append("      " + lines[k]); }
                Debug.Log(sb.ToString());
            }
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
