#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Yurufu.FoamDiagnostics
{
    /// <summary>
    /// 実際の計測を行う使い捨て MonoBehaviour。FoamFeasibilityProbe から生成される。
    /// 計測が終わったら自分を Destroy する。
    /// </summary>
    public class FoamProbeRunner : MonoBehaviour
    {
        /// <summary>連続計測するフレーム数。</summary>
        private const int MeasureFrames = 60;

        /// <summary>
        /// 泡を付けない候補を機械的に拾うためのキーワード。
        /// ★これは「提案」であって決め打ちではない。実際の Renderer 名は全件ログに出すので、
        ///   分類が妥当かどうかはログを見て人間が判断すること。
        /// </summary>
        private static readonly string[] FaceKeywords =
        {
            "eye", "mouth", "mouht", "cheek", "beak", "face", "tooth", "tongue", "pupil", "brow"
        };

        private class Entry
        {
            public SkinnedMeshRenderer Smr;
            public string Path;
            public Mesh Shared;
            public Mesh BakeTarget;
            public int VertexCount;
            public int TriangleCount;
            public int BoneCount;
            public int SubMeshCount;
            public int BlendShapeCount;
            public bool IsReadable;
            public bool BakeOk;
            public string BakeError = "";
            public double FirstBakeMs;
            public bool IsFaceCandidate;
            public string AccessReport = "";
            public readonly List<double> FrameMs = new List<double>();
        }

        public void Begin() => StartCoroutine(Co());

        private IEnumerator Co()
        {
            var sb = new StringBuilder();
            L(sb, "==================================================================");
            L(sb, " FoamFeasibilityProbe  泡の実装方式を決めるための計測");
            L(sb, "==================================================================");
            L(sb, $" Unity        : {Application.unityVersion}");
            L(sb, $" Platform     : {Application.platform}");
            L(sb, $" RenderPipe   : {(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in")}");
            L(sb, $" ColorSpace   : {QualitySettings.activeColorSpace}");
            L(sb, $" GraphicsAPI  : {SystemInfo.graphicsDeviceType}");
            L(sb, $" 計測フレーム : {MeasureFrames}");
            L(sb, "");

            // ── 1. シーン内の SkinnedMeshRenderer を集めて、ルートごとにまとめる ──
            var all = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (all.Length == 0)
            {
                L(sb, "!! SkinnedMeshRenderer が1つも見つかりませんでした。");
                L(sb, "   Bath シーンを Play して、キャラが表示された状態で実行してください。");
                Dump(sb);
                Destroy(gameObject);
                yield break;
            }

            var groups = new Dictionary<Transform, List<SkinnedMeshRenderer>>();
            foreach (var s in all)
            {
                var root = s.transform.root;
                if (!groups.TryGetValue(root, out var list))
                {
                    list = new List<SkinnedMeshRenderer>();
                    groups[root] = list;
                }
                list.Add(s);
            }

            L(sb, "── シーン内の SkinnedMeshRenderer のルート一覧 ──");
            Transform best = null;
            int bestCount = -1;
            foreach (var kv in groups)
            {
                L(sb, $"  root='{kv.Key.name}'  Renderer数={kv.Value.Count}");
                if (kv.Value.Count > bestCount) { bestCount = kv.Value.Count; best = kv.Key; }
            }
            L(sb, $"  → 最も Renderer が多い '{best.name}' をキャラとみなして計測します");
            L(sb, "");

            var targets = groups[best];

            // ── 2. Renderer ごとの基本情報とアクセス可否 ──
            var entries = new List<Entry>();
            foreach (var smr in targets)
            {
                var e = new Entry
                {
                    Smr = smr,
                    Path = FullPath(smr.transform),
                    Shared = smr.sharedMesh,
                    BoneCount = smr.bones != null ? smr.bones.Length : 0,
                };

                if (e.Shared == null)
                {
                    e.AccessReport = "sharedMesh=null";
                    entries.Add(e);
                    continue;
                }

                // vertexCount / subMeshCount は Read/Write OFF でも読める（既知）
                e.VertexCount = e.Shared.vertexCount;
                e.SubMeshCount = e.Shared.subMeshCount;
                // ブレンドシェイプの有無は第4案の可否に直結する。
                // ボーンウェイト補間はスキニングだけを再現するので、
                // ブレンドシェイプで動くメッシュには泡が追従しない。
                e.BlendShapeCount = e.Shared.blendShapeCount;
                e.IsReadable = e.Shared.isReadable;
                e.IsFaceCandidate = IsFaceCandidate(smr.name);

                // 三角形数は index count から推定（triangles を読まずに済む）
                int idx = 0;
                for (int i = 0; i < e.SubMeshCount; i++) idx += (int)e.Shared.GetIndexCount(i);
                e.TriangleCount = idx / 3;

                // ── 7〜10 のアクセス可否を個別に記録する ──
                var ar = new StringBuilder();
                ar.Append(TryAccess("vertices",   () => { var v = e.Shared.vertices;    return v?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("triangles",  () => { var t = e.Shared.triangles;   return t?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("GetIndices", () => { var l = new List<int>(); e.Shared.GetIndices(l, 0); return l.Count; }));
                ar.Append(" | ").Append(TryAccess("uv",         () => { var u = e.Shared.uv;         return u?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("normals",    () => { var n = e.Shared.normals;    return n?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("boneWeights",() => { var b = e.Shared.boneWeights;return b?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("GetAllBoneWeights", () =>
                {
                    var bw = e.Shared.GetAllBoneWeights();   // Unity 2019.3+ / Unity 6 の全ボーンウェイト API
                    return bw.Length;
                }));
                ar.Append(" | ").Append(TryAccess("GetBonesPerVertex", () =>
                {
                    var bp = e.Shared.GetBonesPerVertex();
                    return bp.Length;
                }));
                // bindposes は頂点データではないので、Read/Write OFF でも読める可能性がある。
                // 第4案（ボーンウェイト補間）に必須なので必ず確認する。
                ar.Append(" | ").Append(TryAccess("bindposes", () => { var bp = e.Shared.bindposes; return bp?.Length ?? 0; }));
                ar.Append(" | ").Append(TryAccess("AcquireReadOnlyMeshData", () =>
                {
                    var d = Mesh.AcquireReadOnlyMeshData(e.Shared);
                    int n = d[0].vertexCount;
                    d.Dispose();
                    return n;
                }));
                e.AccessReport = ar.ToString();

                // ── 3・4. BakeMesh が Read/Write OFF のまま成功するか、その所要時間 ──
                e.BakeTarget = new Mesh { name = "~probe_bake_" + smr.name };
                var sw = Stopwatch.StartNew();
                try
                {
                    smr.BakeMesh(e.BakeTarget);
                    sw.Stop();
                    e.BakeOk = true;
                    e.FirstBakeMs = Ms(sw);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    e.BakeOk = false;
                    e.BakeError = ex.GetType().Name + ": " + Short(ex.Message);
                }

                entries.Add(e);
            }

            // ── 出力: Renderer 一覧 ──
            L(sb, "── 1. Renderer 一覧（全件。除外は名前で決め打ちせず、ここを見て判断すること）──");
            L(sb, $"  {"Renderer名",-24} {"mesh名",-22} {"頂点",7} {"三角形",7} {"骨",4} {"Sub",4} {"BS",4} {"Readable",9} {"Bake",6} {"Bake(ms)",9}  顔候補");
            foreach (var e in entries)
            {
                L(sb, $"  {Cut(e.Smr.name,24),-24} {Cut(e.Shared != null ? e.Shared.name : "-",22),-22} " +
                      $"{e.VertexCount,7} {e.TriangleCount,7} {e.BoneCount,4} {e.SubMeshCount,4} {e.BlendShapeCount,4} " +
                      $"{e.IsReadable,9} {(e.BakeOk ? "OK" : "NG"),6} {e.FirstBakeMs,9:F3}  {(e.IsFaceCandidate ? "★" : "")}");
            }
            L(sb, "");
            int bsTotal = 0; foreach (var e in entries) bsTotal += e.BlendShapeCount;
            L(sb, bsTotal > 0
                ? $"  ★ ブレンドシェイプを持つ Renderer があります（合計 {bsTotal} 個）。第4案では追従しない点に注意"
                : "  ★ ブレンドシェイプは0個。第4案（ボーンウェイト補間）でスキニングを完全に再現できます");
            L(sb, "");
            L(sb, "── Renderer の階層パス ──");
            foreach (var e in entries) L(sb, $"  {e.Path}");
            L(sb, "");

            // ── 出力: BakeMesh の失敗内容 ──
            bool anyBakeFail = false;
            foreach (var e in entries)
            {
                if (!e.BakeOk) { anyBakeFail = true; L(sb, $"  !! BakeMesh 失敗 {e.Smr.name} : {e.BakeError}"); }
            }
            L(sb, anyBakeFail
                ? "  → ★ Read/Write OFF のままでは BakeMesh が使えません"
                : "  → ★ Read/Write OFF のままでも BakeMesh は全 Renderer で成功しました");
            L(sb, "");

            // ── 出力: 7〜10 のアクセス可否 ──
            L(sb, "── 7〜10. Mesh データへのアクセス可否（OK=読めた / NG=例外）──");
            foreach (var e in entries)
            {
                L(sb, $"  [{Cut(e.Smr.name,20),-20}] {e.AccessReport}");
            }
            L(sb, "");

            // ── 5. 全対象を1回 Bake した合計 ──
            var okAll = entries.FindAll(x => x.BakeOk);
            var okFoam = okAll.FindAll(x => !x.IsFaceCandidate);
            L(sb, "── 5. 1回 Bake の合計 ──");
            L(sb, $"  全 Renderer   : {okAll.Count} 個 / 頂点 {Sum(okAll)} / 合計 {SumMs(okAll):F3} ms");
            L(sb, $"  顔を除いた対象: {okFoam.Count} 個 / 頂点 {Sum(okFoam)} / 合計 {SumMs(okFoam):F3} ms");
            L(sb, "");

            // ── 6. 60フレーム連続 Bake ──
            L(sb, $"── 6. {MeasureFrames} フレーム連続 Bake を計測中… ──");
            Dump(sb);   // ここまでを先に出す（途中で止まっても情報が残るように）
            sb.Clear();

            var frameTotalAll = new List<double>(MeasureFrames);
            var frameTotalFoam = new List<double>(MeasureFrames);

            for (int f = 0; f < MeasureFrames; f++)
            {
                yield return null;   // 1フレーム待ってから計測（アニメが進んだ状態で測るため）

                double totAll = 0, totFoam = 0;
                foreach (var e in okAll)
                {
                    var sw = Stopwatch.StartNew();
                    try { e.Smr.BakeMesh(e.BakeTarget); } catch { }
                    sw.Stop();
                    double ms = Ms(sw);
                    e.FrameMs.Add(ms);
                    totAll += ms;
                    if (!e.IsFaceCandidate) totFoam += ms;
                }
                frameTotalAll.Add(totAll);
                frameTotalFoam.Add(totFoam);
            }

            L(sb, "── 6. 結果 ──");
            Report(sb, "全 Renderer   ", frameTotalAll);
            Report(sb, "顔を除いた対象", frameTotalFoam);
            L(sb, "");
            L(sb, "  Renderer ごとの平均 Bake 時間:");
            foreach (var e in okAll)
            {
                double avg = 0; foreach (var m in e.FrameMs) avg += m;
                avg /= Math.Max(e.FrameMs.Count, 1);
                L(sb, $"    {Cut(e.Smr.name,24),-24} 平均 {avg,8:F3} ms  頂点 {e.VertexCount,6}  {(e.IsFaceCandidate ? "（顔候補・除外案）" : "")}");
            }
            L(sb, "");

            // ── 顔候補の分類結果 ──
            L(sb, "── 顔候補の分類（★は下のキーワードに名前が一致したもの。妥当かは人間が判断）──");
            L(sb, $"  キーワード: {string.Join(", ", FaceKeywords)}");
            var face = entries.FindAll(x => x.IsFaceCandidate);
            var body = entries.FindAll(x => !x.IsFaceCandidate);
            L(sb, $"  泡を付けない候補 ({face.Count}個): {string.Join(", ", face.ConvertAll(x => x.Smr.name))}");
            L(sb, $"  泡の対象         ({body.Count}個): {string.Join(", ", body.ConvertAll(x => x.Smr.name))}");
            L(sb, "");
            L(sb, "==================== FoamFeasibilityProbe 終了 ====================");
            Dump(sb);

            // 後片付け
            foreach (var e in entries) if (e.BakeTarget != null) Destroy(e.BakeTarget);
            Destroy(gameObject);
        }

        // ── 補助 ────────────────────────────────────────────────────────────

        private static void Report(StringBuilder sb, string label, List<double> v)
        {
            if (v.Count == 0) { L(sb, $"  {label}: データなし"); return; }
            var s = new List<double>(v); s.Sort();
            double sum = 0; foreach (var x in v) sum += x;
            double avg = sum / v.Count;
            double max = s[s.Count - 1];
            double p95 = s[Mathf.Clamp(Mathf.RoundToInt(0.95f * (s.Count - 1)), 0, s.Count - 1)];
            double min = s[0];
            L(sb, $"  {label}: 平均 {avg,8:F3} ms / 最大 {max,8:F3} ms / p95 {p95,8:F3} ms / 最小 {min,8:F3} ms");
        }

        private static string TryAccess(string label, Func<int> f)
        {
            try { return $"{label}=OK({f()})"; }
            catch (Exception e) { return $"{label}=NG[{e.GetType().Name}: {Short(e.Message)}]"; }
        }

        private static bool IsFaceCandidate(string n)
        {
            string s = n.ToLowerInvariant();
            foreach (var k in FaceKeywords) if (s.Contains(k)) return true;
            return false;
        }

        private static int Sum(List<Entry> l) { int t = 0; foreach (var e in l) t += e.VertexCount; return t; }
        private static double SumMs(List<Entry> l) { double t = 0; foreach (var e in l) t += e.FirstBakeMs; return t; }
        private static double Ms(Stopwatch sw) => sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
        private static string Cut(string s, int n) => string.IsNullOrEmpty(s) ? "-" : (s.Length <= n ? s : s.Substring(0, n - 1) + "…");
        private static string Short(string s) => s == null ? "" : (s.Length <= 90 ? s.Replace("\n", " ") : s.Substring(0, 90).Replace("\n", " ") + "…");

        private static string FullPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack.ToArray());
        }

        private static void L(StringBuilder sb, string s) => sb.AppendLine(s);
        private static void Dump(StringBuilder sb) => Debug.Log("<color=#00E5FF>[FoamProbe]</color>\n" + sb);
    }
}
#endif
