using System.Collections.Generic;
using UnityEngine;

namespace Yurufu.Bath.Foam
{
    /// <summary>
    /// 画面のタップ位置から、いま変形しているキャラ表面のどこに当たったかを求める。
    /// 試作 FoamProtoSurfacePicker の検証済みコードを本番へ移したもの。
    /// ★座標変換の規約は1文字も変えていない（BakeMesh(mesh, true) + Renderer.localToWorldMatrix）。
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
    public class BathFoamSurfacePicker
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
            public Vector3[] RestVerts;               // バインドポーズの頂点。side 判定に使う
            public float     RestMinX, RestMaxX;      // バインドポーズの X 範囲
        }

        private readonly List<Target> _targets = new List<Target>();

        /// <summary>直近の Bake にかかった合計ミリ秒。デバッグ表示用。</summary>
        public double LastBakeMs { get; private set; }
        /// <summary>直近のレイ判定にかかったミリ秒。</summary>
        public double LastRaycastMs { get; private set; }

        public int TargetCount => _targets.Count;
        public string TargetName(int i) => _targets[i].Name;

        public bool Add(SkinnedMeshRenderer smr)
        {
            if (smr == null || smr.sharedMesh == null) return false;

            var mesh = smr.sharedMesh;
            var t = new Target
            {
                Smr   = smr,
                Name  = smr.name,
                Baked = new Mesh { name = "foam_bake_" + smr.name, },   // ★hideFlags は付けない。Dispose() で明示的に Destroy する
            };

            // ★triangles / uv / vertices は sharedMesh から読んではいけない。
            //
            //   FBX の Read/Write Enabled が OFF（isReadable = false）だと、Unity は
            //     Not allowed to access uv on mesh 'Head' (isReadable is false; ...)
            //   というエラーを出して空の配列を返す。
            //   （実測：2026/8/27 に BathWashManager 側で発生し、新方式が起動できなかった）
            //
            //   一方 BakeMesh は isReadable = false でも動く（実測で確認済み）。
            //   そして BakeMesh の書き込み先は「実行時に自分で作った Mesh」なので、
            //   そこからなら triangles / uv / vertices を普通に読める。
            //   → 最初に1回 Bake して、そのコピーから読む。FBX の設定は一切変更しなくてよい。
            try
            {
                smr.BakeMesh(t.Baked, true);
                t.Triangles = t.Baked.triangles;
                t.Uvs       = t.Baked.uv;
                // ★side は「毎フレームの補間 X」ではなく「基準ポーズの三角形重心 X」で決める。
                //   補間 X は正中線付近で ±0 を行き来し、side がフレームごとに揺れてしまう。
                //   ここで取った1回ぶんの姿勢を基準として固定する（以後ずっと同じ振り分けになる）。
                t.RestVerts = t.Baked.vertices;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BathFoam] '{smr.name}' の Bake に失敗しました: {e.GetType().Name} {e.Message}");
                Object.Destroy(t.Baked);
                return false;
            }

            if (t.Uvs == null || t.Uvs.Length == 0)
            {
                Debug.LogError($"[BathFoam] '{smr.name}' に UV がありません（Bake 結果にも UV がありませんでした）");
                Object.Destroy(t.Baked);
                return false;
            }
            if (t.Triangles == null || t.Triangles.Length == 0)
            {
                Debug.LogError($"[BathFoam] '{smr.name}' に三角形がありません（Bake 結果にも三角形がありませんでした）");
                Object.Destroy(t.Baked);
                return false;
            }

            int vcount = Mathf.Max(mesh.vertexCount, t.Baked.vertexCount);
            t.WorldVerts = new Vector3[vcount];
            t.WorldNorms = new Vector3[vcount];

            // バインドポーズの X 範囲（side 判定のしきい値に使う）
            t.RestMinX = float.MaxValue; t.RestMaxX = float.MinValue;
            if (t.RestVerts != null)
                foreach (var rv in t.RestVerts)
                {
                    if (rv.x < t.RestMinX) t.RestMinX = rv.x;
                    if (rv.x > t.RestMaxX) t.RestMaxX = rv.x;
                }

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

            LastBakeMs += sw.ElapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
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

        // ── 泡粒の追従に使う ──────────────────────────────────────────────────

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
                if (t.Baked != null) Object.Destroy(t.Baked);
            _targets.Clear();
        }
    }
}
