#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Yurufu.FoamDiagnostics
{
    /// <summary>
    /// 泡の実装方式を決めるための「計測だけ」を行う診断ツール。
    ///
    /// 【これは本番コードではありません】
    ///   Assets/Scripts/Bath/FoamDiagnostics/ 以下だけで完結しており、
    ///   既存の BathBubblePainter / BathWashManager / Prefab / Scene / Material を
    ///   一切参照していません。フォルダごと削除すれば跡形もなく消えます。
    ///
    /// 【なぜ Editor 専用か】
    ///   ファイル全体を #if UNITY_EDITOR で囲んでいるので、
    ///   iOS ビルドには一切含まれません。実機の挙動には影響しません。
    /// </summary>
    public static class FoamFeasibilityProbe
    {
        private const string MenuPath = "YURUFU/診断/泡 実現可能性プローブ（Play中に実行）";

        [MenuItem(MenuPath, false, 1)]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "泡 実現可能性プローブ",
                    "Play 中に実行してください。\n\n" +
                    "キャラは実行時に生成されるため、Play していないと\n" +
                    "SkinnedMeshRenderer が1つも見つかりません。",
                    "OK");
                return;
            }

            // 使い捨てのランナーを立てて、コルーチンで60フレーム計測させる。
            // HideAndDontSave なので Hierarchy を汚さず、シーン保存にも入らない。
            var go = new GameObject("~FoamProbeRunner")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            go.AddComponent<FoamProbeRunner>().Begin();
        }

        [MenuItem(MenuPath, true)]
        private static bool RunValidate() => true;
    }
}
#endif
