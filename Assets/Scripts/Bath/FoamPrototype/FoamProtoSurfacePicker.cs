#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// 画面のタップ位置から、いま変形しているキャラ表面のどこに当たったかを求める。
    ///
    /// 【なぜ MeshCollider を使わないのか】
    ///   SkinnedMeshRenderer の変形に MeshCollider は追従しない。
    ///   毎フレーム BakeMesh して Collider を作り直すのは重すぎる。
    ///   Head+Body は合わせて約 7,900 三角形しかないので、
    ///   CPU で直接レイと三角形の交差を取ったほうが速くて確実。
    ///
    /// 【GC を出さないための方針】
    ///   ・sharedMesh の triangles / uv / normals は最初に1回だけ取る
    ///   ・BakeMesh の結果は使い回しの List に GetVertices で受ける
    ///   ・ワールド座標の配列も最初に確保して使い回す
    ///   ・1回の Pointer イベントにつき Bake は各 Renderer 最大1回
    /// </summary>
    public class FoamProtoSurfacePicker
    {
        public struct Hit
        {
            public bool    Valid;
            public int     TargetIndex;     // 0=Head, 1=Body ...
            public int     TriangleIndex;
            public Vector3 Barycentric;
            public Vector2 Uv;              // 元メッシュのUV（0〜1）
            public Vector3 LocalPos;        // Bake 済みローカル座標
            public Vector3 WorldPos;
            public float   Distance;

            /// <summary>ヒットした三角形の、バインドポーズにおける重心 X。side はこれで決める。</summary>
            public float   TriangleCenterX;
            /// <summary>1 = 上半分 (X>=0) / 0 = 下半分 (X&lt;0)</summary>
            public int     SelectedSide;
        }

        private class Target
        {
            public SkinnedMeshRenderer Smr;
            public string Name;
            public Mesh   Baked;                      // Bake 結果の受け皿（使い回し）
            public readonly List<Vector3> BakedVerts = new List<Vector3>();
            public Vector3[] WorldVerts;              // ワールド座標（使い回し）
            public readonly List<Vector3> BakedNorms = new List<Vector3>();   // Bake 結果の法線
            public Vector3[] WorldNorms;              // ワールド法線（使い回し）
            public int[]     Triangles;               // 最初に1回だけ取得
            public Vector2[] Uvs;                     // 最初に1回だけ取得
            public bool      Baked1;                  // このイベントで Bake 済みか
            public bool      VerifyLogged;            // 初回の検証ログを出したか
            public Vector3[] RestVerts;               // バインドポーズの頂点。side 判定に使う
            public float     RestMinX, RestMaxX;      // バインドポーズの X 範囲
            public float     UvAreaSum;               // Σ|三角形のUV面積|。UV の重なり具合の目安
        }

        private readonly List<Target> _targets = new List<Target>();

        /// <summary>直近の Bake にかかった合計ミリ秒。デバッグ表示用。</summary>
        public double LastBakeMs { get; private set; }
        /// <summary>直近のレイ判定にかかったミリ秒。</summary>
        public double LastRaycastMs { get; private set; }

        public int TargetCount => _targets.Count;
        public string TargetName(int i) => _targets[i].Name;

        /// <summary>
        /// UV の重なり具合を判定してログに出す。
        /// この方式（UVマスク）が、いま選ばれているキャラで使えるかどうかが分かる。
        /// </summary>
        public void LogUvCompatibility()
        {
            var sb = new System.Text.StringBuilder();
            bool warn = false;

            sb.AppendLine("UV の重なり判定（Σ|三角形のUV面積|。重なりが無ければ 1.0 以下）");
            foreach (var t in _targets)
            {
                string verdict;
                if (t.UvAreaSum <= 1.10f)      verdict = "重なりなし → そのまま使えます";
                else if (t.UvAreaSum <= 2.20f) verdict = "左右ミラーのみ → いまの左右分割で対応できます";
                else { verdict = $"★ {t.UvAreaSum:F1}枚ぶん重なっています → 左右分割では分離できません"; warn = true; }

                sb.AppendLine($"      {t.Name,-8} Σ|UV面積| = {t.UvAreaSum:F3}   {verdict}");
            }

            if (warn)
            {
                sb.AppendLine("      顔と耳のように、別の部位が同じ UV を共有しているキャラです。");
                sb.AppendLine("      このキャラでは「こすった場所だけ塗る」が成立しません（ぴよこで確認してください）。");
                Debug.LogWarning("[FoamProto] " + sb);
            }
            else
            {
                Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto] " + sb);
            }
        }

        public bool Add(SkinnedMeshRenderer smr)
        {
            if (smr == null || smr.sharedMesh == null) return false;

            var mesh = smr.sharedMesh;
            var t = new Target
            {
                Smr   = smr,
                Name  = smr.name,
                Baked = new Mesh { name = "~foamproto_bake_" + smr.name, hideFlags = HideFlags.HideAndDontSave },
            };

            // 診断で triangles / uv が読めることは確認済み。読めない場合はここで弾く
            try
            {
                t.Triangles = mesh.triangles;
                t.Uvs       = mesh.uv;
                // ★side は「スキニング後の補間 X」ではなく「バインドポーズの三角形重心 X」で決める。
                //   補間 X は正中線付近で ±0 を行き来し、side がフレームごとに揺れてしまう。
                t.RestVerts = mesh.vertices;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FoamProto] '{smr.name}' の triangles/uv を取得できません: {e.GetType().Name} {e.Message}");
                Object.DestroyImmediate(t.Baked);
                return false;
            }

            if (t.Uvs == null || t.Uvs.Length == 0)
            {
                Debug.LogError($"[FoamProto] '{smr.name}' に UV がありません");
                Object.DestroyImmediate(t.Baked);
                return false;
            }

            t.WorldVerts = new Vector3[mesh.vertexCount];
            t.WorldNorms = new Vector3[mesh.vertexCount];

            // バインドポーズの X 範囲（side 判定のしきい値に使う）
            t.RestMinX = float.MaxValue; t.RestMaxX = float.MinValue;
            if (t.RestVerts != null)
                foreach (var rv in t.RestVerts)
                {
                    if (rv.x < t.RestMinX) t.RestMinX = rv.x;
                    if (rv.x > t.RestMaxX) t.RestMaxX = rv.x;
                }

            // ── UV の重なり具合を測る ──
            // 重なりのない展開なら Σ|三角形のUV面積| は 1.0 を超えない。
            // 2.0 前後なら左右ミラー（いまの左右分割で対応できる）。
            // それを大きく超えるなら、顔と耳のように別の部位が同じ UV を共有している。
            double area = 0;
            for (int i = 0; i + 2 < t.Triangles.Length; i += 3)
            {
                Vector2 a = t.Uvs[t.Triangles[i]], b = t.Uvs[t.Triangles[i + 1]], c = t.Uvs[t.Triangles[i + 2]];
                area += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
            }
            t.UvAreaSum = (float)area;

            _targets.Add(t);
            return true;
        }

        /// <summary>Pointer イベントの先頭で呼ぶ。この後の Raycast は同じ Bake 結果を使う。</summary>
        public void BeginEvent()
        {
            for (int i = 0; i < _targets.Count; i++) _targets[i].Baked1 = false;
            LastBakeMs = 0;
        }

        private void EnsureBaked(Target t)
        {
            if (t.Baked1) return;
            t.Baked1 = true;

            // ★キャラが破棄された後（シーン切り替え後）に呼ばれても落ちないようにする。
            //   UnityEngine.Object は破棄済みだと == null が true になる。
            //   これが無いと BakeMesh が MissingReferenceException を投げる。
            if (t.Smr == null || t.Baked == null) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ── 座標変換の規約（Unity 6 公式仕様に固定。推測や自動選択はしない）──
            //   BakeMesh(mesh, useScale: true)
            //     → SkinnedMeshRenderer の Transform scale を補正した頂点が得られる。
            //       頂点は SkinnedMeshRenderer Transform 基準。
            //   Renderer.localToWorldMatrix
            //     → 位置・回転・スケールを「1回だけ」適用する。
            //
            //   既定の BakeMesh(mesh) は useScale: false。
            //   それにスケール込みの行列を掛けるとスケールが二重に適用され、
            //   実測でカメラからの距離が 12 のはずが 107 になっていた。
            t.Smr.BakeMesh(t.Baked, true);
            t.Baked.GetVertices(t.BakedVerts);              // List を使い回すので GC が出にくい

            // 行列は初期化時に固定せず、Bake のたびに現在の値を取る
            Matrix4x4 bakeToWorld = t.Smr.localToWorldMatrix;

            int n = Mathf.Min(t.BakedVerts.Count, t.WorldVerts.Length);
            for (int i = 0; i < n; i++) t.WorldVerts[i] = bakeToWorld.MultiplyPoint3x4(t.BakedVerts[i]);

            // 泡粒を表面から浮かせる向きに使うので、法線もワールドへ直しておく。
            // MultiplyVector は平行移動を無視する。等倍スケール前提で十分な精度が出る。
            t.Baked.GetNormals(t.BakedNorms);
            int nn = Mathf.Min(t.BakedNorms.Count, t.WorldNorms.Length);
            for (int i = 0; i < nn; i++) t.WorldNorms[i] = bakeToWorld.MultiplyVector(t.BakedNorms[i]).normalized;

            sw.Stop();

            if (!t.VerifyLogged) LogBakeVerification(t, bakeToWorld, n);
            LastBakeMs += sw.ElapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        /// <summary>
        /// 初回の Bake のときだけ、座標変換が正しいかを確認するためのログを出す。
        ///
        /// ★これは「確認」であって「選択」ではない。
        ///   ここの比較結果によって変換行列を切り替えることは一切しない。
        ///   Renderer.bounds はアニメーション用に余裕を持つことがあり、
        ///   Bake 済み頂点の実境界と完全一致する保証がないため、判定には使えない。
        /// </summary>
        private void LogBakeVerification(Target t, Matrix4x4 bakeToWorld, int n)
        {
            t.VerifyLogged = true;

            Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
            for (int i = 0; i < n; i++) { mn = Vector3.Min(mn, t.WorldVerts[i]); mx = Vector3.Max(mx, t.WorldVerts[i]); }

            Vector3 center = (mn + mx) * 0.5f;
            Vector3 size   = mx - mn;
            Bounds  rb     = t.Smr.bounds;

            float gap = Vector3.Distance(center, rb.center);
            Vector3 ratio = new Vector3(
                size.x / Mathf.Max(rb.size.x, 1e-6f),
                size.y / Mathf.Max(rb.size.y, 1e-6f),
                size.z / Mathf.Max(rb.size.z, 1e-6f));

            var cam = Camera.main;
            float camDist = cam != null ? Vector3.Distance(cam.transform.position, center) : -1f;

            Debug.Log(
                $"<color=#00E5FF>[決定]</color> [FoamProto] '{t.Name}' 座標変換の検証（useScale=true + localToWorldMatrix）\n" +
                $"      lossyScale                = {t.Smr.transform.lossyScale}\n" +
                $"      BakeMesh                  = BakeMesh(mesh, useScale: true)\n" +
                $"      変換後 AABB  中心         = {center}  サイズ = {size}\n" +
                $"      smr.bounds   中心         = {rb.center}  サイズ = {rb.size}\n" +
                $"      中心距離                  = {gap:F4}\n" +
                $"      size 比 (AABB / bounds)   = {ratio}\n" +
                $"      カメラ〜AABB中心の距離     = {camDist:F3}");
        }

        /// <summary>
        /// レイを飛ばして、カメラにいちばん近い表面を返す。
        /// Renderer.bounds で先に絞ってから三角形を回す。
        /// </summary>
        public Hit Raycast(Ray ray)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var best = new Hit { Valid = false, Distance = float.MaxValue };

            for (int ti = 0; ti < _targets.Count; ti++)
            {
                var t = _targets[ti];
                if (t.Smr == null) continue;

                // 先に大まかに絞る。外れていれば Bake すらしない
                if (!t.Smr.bounds.IntersectRay(ray)) continue;

                EnsureBaked(t);

                var verts = t.WorldVerts;
                var tris  = t.Triangles;

                for (int i = 0; i < tris.Length; i += 3)
                {
                    int i0 = tris[i], i1 = tris[i + 1], i2 = tris[i + 2];
                    if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;

                    if (!RayTriangle(ray, verts[i0], verts[i1], verts[i2],
                                     out float dist, out float u, out float v)) continue;
                    if (dist >= best.Distance) continue;

                    float w = 1f - u - v;
                    best.Valid         = true;
                    best.TargetIndex   = ti;
                    best.TriangleIndex = i / 3;
                    best.Barycentric   = new Vector3(w, u, v);
                    best.Distance      = dist;
                    best.WorldPos      = ray.origin + ray.direction * dist;

                    // UV とローカル座標を重心座標で補間する
                    best.Uv = t.Uvs[i0] * w + t.Uvs[i1] * u + t.Uvs[i2] * v;
                    best.LocalPos = t.BakedVerts[i0] * w + t.BakedVerts[i1] * u + t.BakedVerts[i2] * v;

                    // ★side は三角形単位で確定させる（ヒット点の補間 X は使わない）
                    best.TriangleCenterX = ResolveSideX(t, i0, i1, i2);
                    best.SelectedSide    = best.TriangleCenterX >= 0f ? 1 : 0;
                }
            }

            sw.Stop();
            LastRaycastMs = sw.ElapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return best;
        }

        /// <summary>
        /// 三角形の side を決めるための X を返す。
        ///
        /// 基本は3頂点（バインドポーズ）の重心 X。
        /// 重心が正中線に貼りついている三角形（X=0 をまたぐ帯）では符号が不安定になるので、
        /// そのときだけ「0 でない頂点の多数決」で決める。
        /// </summary>
        private static float ResolveSideX(Target t, int i0, int i1, int i2)
        {
            if (t.RestVerts == null || i0 >= t.RestVerts.Length || i1 >= t.RestVerts.Length || i2 >= t.RestVerts.Length)
                return 0f;

            float x0 = t.RestVerts[i0].x, x1 = t.RestVerts[i1].x, x2 = t.RestVerts[i2].x;
            float center = (x0 + x1 + x2) / 3f;

            // メッシュの左右幅に対して十分大きければ、そのまま重心で決めてよい
            float halfWidth = Mathf.Max((t.RestMaxX - t.RestMinX) * 0.5f, 1e-9f);
            if (Mathf.Abs(center) > halfWidth * 0.001f) return center;

            // 正中線上の三角形: 0 でない頂点の多数決
            int plus = 0, minus = 0;
            float eps = halfWidth * 1e-4f;
            if (x0 >  eps) plus++; else if (x0 < -eps) minus++;
            if (x1 >  eps) plus++; else if (x1 < -eps) minus++;
            if (x2 >  eps) plus++; else if (x2 < -eps) minus++;

            if (plus > minus) return  halfWidth * 1e-3f;
            if (minus > plus) return -halfWidth * 1e-3f;
            return center;   // 完全に決まらないときは重心の符号に従う
        }

        /// <summary>Moller-Trumbore。裏表どちらの面でも当たるようにしてある。</summary>
        private static bool RayTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c,
                                        out float dist, out float u, out float v)
        {
            dist = 0; u = 0; v = 0;
            const float EPS = 1e-8f;

            Vector3 e1 = b - a;
            Vector3 e2 = c - a;
            Vector3 p  = Vector3.Cross(ray.direction, e2);
            float det  = Vector3.Dot(e1, p);
            if (det > -EPS && det < EPS) return false;

            float inv = 1f / det;
            Vector3 tv = ray.origin - a;
            u = Vector3.Dot(tv, p) * inv;
            if (u < 0f || u > 1f) return false;

            Vector3 q = Vector3.Cross(tv, e1);
            v = Vector3.Dot(ray.direction, q) * inv;
            if (v < 0f || u + v > 1f) return false;

            dist = Vector3.Dot(e2, q) * inv;
            return dist > EPS;
        }

        /// <summary>
        /// 切り分け用: Bake 結果のワールド座標が、本当にキャラのいる場所に来ているかを調べる。
        /// ここがずれていると、レイは永遠に当たらない。
        /// </summary>
        public string DebugReport()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in _targets)
            {
                if (t.Smr == null) continue;
                t.Baked1 = false;
                EnsureBaked(t);

                var b = new Bounds(t.WorldVerts.Length > 0 ? t.WorldVerts[0] : Vector3.zero, Vector3.zero);
                for (int i = 1; i < t.WorldVerts.Length; i++) b.Encapsulate(t.WorldVerts[i]);

                var rb = t.Smr.bounds;
                float gap = Vector3.Distance(b.center, rb.center);

                sb.AppendLine($"  [{t.Name}] 頂点={t.WorldVerts.Length} 三角形={t.Triangles.Length / 3}");
                sb.AppendLine($"      Bake後のワールド中心 = {b.center}  サイズ = {b.size}");
                sb.AppendLine($"      Renderer.bounds 中心 = {rb.center}  サイズ = {rb.size}");
                sb.AppendLine($"      ★中心のズレ = {gap:F4}（0.1 以上なら Bake の座標空間が違う）");
                sb.AppendLine($"      変換 = BakeMesh(useScale:true) + Renderer.localToWorldMatrix（固定）");
            }
            return sb.ToString();
        }

        // ── 泡粒の追従（Phase 2A で追加。既存の処理は変更していない）──────────

        /// <summary>
        /// 対象の Renderer が1つでも破棄されていたら true。
        /// キャラは Bath シーンと一緒に消えるので、シーン切り替えの検知に使う。
        /// </summary>
        public bool AnyTargetLost()
        {
            for (int i = 0; i < _targets.Count; i++)
                if (_targets[i].Smr == null) return true;
            return false;
        }

        /// <summary>三角形と重心座標から求めた、いまの表面の1点。</summary>
        public struct SurfacePoint
        {
            public bool    Valid;
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 Tangent;     // 表面に沿った向き（粒を散らすのに使う）
            public Vector3 Bitangent;
        }

        /// <summary>
        /// 毎フレームの表示更新用に、全対象を Bake し直す。
        /// Raycast 用の BeginEvent と役割が違うので別名にしてある
        /// （BeginEvent は「このイベント中は1回だけ Bake する」ためのもの）。
        /// </summary>
        public void BakeAllForFrame()
        {
            LastBakeMs = 0;
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t.Smr == null) continue;   // 破棄済みは飛ばす
                t.Baked1 = false;
                EnsureBaked(t);
            }
        }

        /// <summary>
        /// 覚えておいた（三角形番号 + 重心座標）から、現在の表面の位置・法線を返す。
        /// ★呼ぶ前に BakeAllForFrame() を1回呼ぶこと。呼ばないと前のフレームの値になる。
        /// </summary>
        public SurfacePoint GetSurfacePoint(int targetIndex, int triangleIndex, Vector3 bary)
        {
            var r = new SurfacePoint { Valid = false };

            if (targetIndex < 0 || targetIndex >= _targets.Count) return r;
            var t = _targets[targetIndex];
            if (t.Smr == null) return r;                                  // 破棄済み
            if (t.Triangles == null || t.WorldVerts == null) return r;

            int b = triangleIndex * 3;
            if (b < 0 || b + 2 >= t.Triangles.Length) return r;

            int i0 = t.Triangles[b], i1 = t.Triangles[b + 1], i2 = t.Triangles[b + 2];
            var v = t.WorldVerts;
            if (i0 >= v.Length || i1 >= v.Length || i2 >= v.Length) return r;

            Vector3 p0 = v[i0], p1 = v[i1], p2 = v[i2];
            r.Position = p0 * bary.x + p1 * bary.y + p2 * bary.z;

            // 法線はメッシュの法線を補間する。
            // 三角形の外積から作ると、Unity の面の巻き方によって符号が逆になることがあり、
            // 泡粒が体の内側へ潜ってしまう。読めないときだけ外積で代用する。
            Vector3 nrm = Vector3.zero;
            var wn = t.WorldNorms;
            if (wn != null && i0 < wn.Length && i1 < wn.Length && i2 < wn.Length)
                nrm = wn[i0] * bary.x + wn[i1] * bary.y + wn[i2] * bary.z;

            if (nrm.sqrMagnitude < 1e-12f) nrm = Vector3.Cross(p1 - p0, p2 - p0);
            if (nrm.sqrMagnitude < 1e-20f) return r;
            r.Normal = nrm.normalized;

            // 接平面上の軸。三角形の1辺を法線に直交させて作る
            Vector3 tan = p1 - p0;
            tan -= r.Normal * Vector3.Dot(tan, r.Normal);
            if (tan.sqrMagnitude < 1e-20f)
            {
                tan = p2 - p0;
                tan -= r.Normal * Vector3.Dot(tan, r.Normal);
            }
            if (tan.sqrMagnitude < 1e-20f) return r;

            r.Tangent   = tan.normalized;
            r.Bitangent = Vector3.Cross(r.Normal, r.Tangent);
            r.Valid     = true;
            return r;
        }

        public void Dispose()
        {
            foreach (var t in _targets)
                if (t.Baked != null) Object.DestroyImmediate(t.Baked);
            _targets.Clear();
        }
    }
}
#endif
