#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OyatsuPuzzle
{
    // メニューからデフォルトのStageDataSOアセットを一括生成する
    public static class PuzzleStageSOCreator
    {
        [MenuItem("OyatsuPuzzle/Create Default Stage Assets")]
        public static void CreateAll()
        {
            const string dir = "Assets/OyatsuPuzzle/ScriptableObjects";
            Directory.CreateDirectory(dir);

            for (int i = 1; i <= PuzzleStageRegistry.StageCount; i++)
            {
                string path = $"{dir}/Stage{i}.asset";
                if (AssetDatabase.LoadAssetAtPath<StageDataSO>(path) != null)
                    continue; // 既存ならスキップ

                var so = PuzzleStageRegistry.GetStage(i);
                AssetDatabase.CreateAsset(so, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OyatsuPuzzle] StageDataSOアセット生成完了");
        }
    }
}
#endif
