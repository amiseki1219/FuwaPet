using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yurufu.EditorTools
{
    /// <summary>
    /// Bath.unity に「キャラクターだけを照らす」Directional Light 3灯をセットアップする Editor スクリプト。
    ///
    /// なぜ必要か:
    ///   お風呂の部屋（3Dモデル）は暗めのトーンで意図どおりに見えている。
    ///   一方でキャラクターだけが真っ黒なシルエットになってしまう。
    ///   そこで Culling Mask を Character レイヤー（レイヤー8）だけに絞ったライトを足す。
    ///   Culling Mask で絞れば「部屋には一切当たらず、キャラだけが明るくなる」ので、
    ///   既存の部屋用 Directional Light や環境光（Ambient）を一切触らずに済む。
    ///
    /// 値の出典:
    ///   Care.unity に保存済みの CharacterFillLight / CharacterRimLight / PokoKeyLight の
    ///   実データ（Color / Intensity / Position / Rotation）をそのまま移植している。
    ///   Care と Bath でキャラの見え方を揃えるため、sRGB 変換などの加工はしていない。
    /// </summary>
    public static class BathLightSetup
    {
        // ===================================================================
        // ここを書き換えて再実行すれば調整できます（メニューをもう一度クリックするだけ）
        // 同じ名前のライトが既にあれば「上書き」されるので、何度実行しても増えません。
        // ===================================================================

        /// <summary>キャラクターだけを照らすためのレイヤー番号。ProjectSettings/TagManager.asset で確認済み。</summary>
        private const int CharacterLayer = 8;

        /// <summary>3灯をぶら下げる親 GameObject の名前。Care.unity と同じ構成にしている。</summary>
        private const string LightsRootName = "Lights";

        /// <summary>このスクリプトを実行してよいシーン名。誤爆防止のためここで固定する。</summary>
        private const string TargetSceneName = "Bath";

        // --- キーライト（主役の光。顔の明るさはほぼこれで決まる）---
        private const string KeyLightName = "CharacterKeyLight";
        private static readonly Color KeyLightColor = new Color(1f, 0.91f, 0.8f, 1f); // ほんのり暖色
        private const float KeyLightIntensity = 0.5f;
        private static readonly Vector3 KeyLightPosition = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 KeyLightRotation = new Vector3(31.3f, 141.912f, 355.74f);

        // --- フィルライト（キーライトの反対側から当てて、影側が黒く潰れるのを防ぐ）---
        private const string FillLightName = "CharacterFillLight";
        private static readonly Color FillLightColor = new Color(0.6f, 0.7f, 1f, 1f); // 影側なので寒色
        private const float FillLightIntensity = 0.12f;
        private static readonly Vector3 FillLightPosition = new Vector3(-2.11f, 1.9f, 5.58f);
        private static readonly Vector3 FillLightRotation = new Vector3(22.184f, 152.412f, 0f);

        // --- リムライト（後ろ上から当てて輪郭を光らせ、暗い背景からキャラを浮き上がらせる）---
        private const string RimLightName = "CharacterRimLight";
        private static readonly Color RimLightColor = new Color(0.55f, 0.68f, 1f, 1f);
        private const float RimLightIntensity = 0.6f;
        private static readonly Vector3 RimLightPosition = new Vector3(2.2f, 2.8f, -2f);
        private static readonly Vector3 RimLightRotation = new Vector3(22.118f, 329.057f, 0f);

        // ===================================================================
        // ここから下は通常さわらなくて大丈夫です
        // ===================================================================

        [MenuItem("Tools/YURUFU/Setup Bath Character Lights")]
        public static void SetupBathCharacterLights()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // 他のシーンで誤って実行されるのを防ぐ。Bath 以外なら何もせず終了する。
            if (scene.name != TargetSceneName)
            {
                Debug.LogWarning(
                    $"[Bath] いま開いているシーンは「{scene.name}」です。" +
                    $"このメニューは「{TargetSceneName}」シーンでのみ実行できます。何もせず終了しました。");
                return;
            }

            // Undo をひとまとめにする。⌘Z 一回で「3灯ぶん全部」戻せるようにするため。
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Bath Character Lights");

            // 親となる Lights を探す。無ければ作る（Care.unity と同じ階層構成に揃える）。
            var lightsRoot = FindRootObject(LightsRootName);
            if (lightsRoot == null)
            {
                lightsRoot = new GameObject(LightsRootName);
                Undo.RegisterCreatedObjectUndo(lightsRoot, "Create Lights Root");
                Debug.Log($"[Bath] 親 GameObject「{LightsRootName}」をルート直下に新規作成しました。");
            }
            else
            {
                Debug.Log($"[Bath] 既存の「{LightsRootName}」を親として使います。");
            }

            // 3灯を作成 or 上書き。
            CreateOrUpdateLight(lightsRoot.transform, KeyLightName, KeyLightColor, KeyLightIntensity, KeyLightPosition, KeyLightRotation);
            CreateOrUpdateLight(lightsRoot.transform, FillLightName, FillLightColor, FillLightIntensity, FillLightPosition, FillLightRotation);
            CreateOrUpdateLight(lightsRoot.transform, RimLightName, RimLightColor, RimLightIntensity, RimLightPosition, RimLightRotation);

            Undo.CollapseUndoOperations(undoGroup);

            // シーンを「未保存」状態にする。保存はユーザーが ⌘S で行うので、ここでは保存しない。
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                "<color=#00E5FF>[決定]</color> [Bath] キャラ専用ライト3灯のセットアップが完了しました。" +
                $"Culling Mask はレイヤー {CharacterLayer}（Character）のみ。" +
                "シーンは未保存です。⌘S で保存してください。");
        }

        /// <summary>
        /// 指定名の子を探し、あれば値を上書き、無ければ新規作成する。
        /// 「上書き」にしているのは、メニューを何度クリックしてもライトが増殖しないようにするため。
        /// </summary>
        private static void CreateOrUpdateLight(
            Transform parent, string name, Color color, float intensity, Vector3 localPosition, Vector3 localEulerAngles)
        {
            bool isNew = false;

            // 子を名前で直接探す（Find は非アクティブな子も見つけられる）。
            var target = parent.Find(name);
            if (target == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                Undo.SetTransformParent(go.transform, parent, $"Parent {name}");
                target = go.transform;
                isNew = true;
            }

            // 既存オブジェクトを書き換える場合も Undo が効くように記録する。
            Undo.RecordObject(target, $"Update {name} Transform");
            target.localPosition = localPosition;
            target.localEulerAngles = localEulerAngles; // 依頼どおり localEulerAngles で指定する
            target.localScale = Vector3.one;

            // Light コンポーネントが無ければ追加（手作業で消されていた場合の保険）。
            var light = target.GetComponent<Light>();
            if (light == null)
            {
                light = Undo.AddComponent<Light>(target.gameObject);
            }

            Undo.RecordObject(light, $"Update {name} Light");
            light.type = LightType.Directional;
            light.color = color;      // Linear 色空間だが、Care に保存されている値をそのまま入れる（変換しない）
            light.intensity = intensity;
            light.shadows = LightShadows.None;               // 影は部屋側のライトに任せるので落とさない
            light.cullingMask = 1 << CharacterLayer;         // Character レイヤーだけを照らす = 部屋には当たらない
            light.renderMode = LightRenderMode.Auto;
            light.lightmapBakeType = LightmapBakeType.Realtime;

            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(target.gameObject);

            string verb = isNew ? "新規作成" : "上書き更新";
            Debug.Log(
                $"[Bath] {name} を{verb}しました / Intensity={intensity} / " +
                $"Color=({color.r}, {color.g}, {color.b}) / Pos={localPosition} / Rot={localEulerAngles}");
        }

        /// <summary>アクティブシーンのルート直下から、指定名の GameObject を探す。</summary>
        private static GameObject FindRootObject(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }
    }
}
