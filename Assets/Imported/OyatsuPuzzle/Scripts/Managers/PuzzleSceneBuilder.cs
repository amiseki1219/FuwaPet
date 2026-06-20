#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace OyatsuPuzzle
{
    // Editor メニューからシーンを自動生成するユーティリティ
    public static class PuzzleSceneBuilder
    {
        private const string SceneDir = "Assets/OyatsuPuzzle/Scenes";

        [MenuItem("OyatsuPuzzle/Build All Scenes")]
        public static void BuildAllScenes()
        {
            Directory.CreateDirectory(SceneDir);

            BuildStartScene();
            BuildGameScene();
            BuildClearScene();
            BuildFailScene();
            UpdateBuildSettings();

            AssetDatabase.Refresh();
            Debug.Log("[OyatsuPuzzle] 全シーン生成完了");
        }

        // ──────────────────────────────────────────
        // PuzzleStartScene
        // ──────────────────────────────────────────
        private static void BuildStartScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // EventSystem
            CreateEventSystem();

            // Canvas
            var canvas = CreateCanvas("Canvas");

            // BG
            CreatePanel(canvas.transform, "BG", new Color(1f, 0.95f, 0.8f));

            // Context
            var ctx = new GameObject("PuzzleSceneContext");
            ctx.AddComponent<PuzzleSceneContext>();

            // UI
            var ui = new GameObject("PuzzleStartScreenUI");
            var comp = ui.AddComponent<PuzzleStartScreenUI>();

            var stageLabel   = CreateLabel(canvas.transform, "StageLabel",    "Stage X",              new Vector2(0, 200), 36);
            var playsLabel   = CreateLabel(canvas.transform, "PlaysLabel",    "今日の残りプレイ回数: X", new Vector2(0, 140), 24);
            var goalLabel    = CreateLabel(canvas.transform, "GoalLabel",     "目標\n...",              new Vector2(0, 40),  22);
            var rewardLabel  = CreateLabel(canvas.transform, "RewardLabel",   "報酬: ...",             new Vector2(0, -80), 22);
            var startBtn     = CreateButton(canvas.transform, "StartButton",  "スタート",              new Vector2(0, -160), new Color(1f,0.6f,0.3f));
            var backBtn      = CreateButton(canvas.transform, "BackButton",   "もどる",                new Vector2(0, -230), new Color(0.8f,0.8f,0.8f));

            SetPrivateField(comp, "stageLabel",          stageLabel);
            SetPrivateField(comp, "remainingPlaysLabel", playsLabel);
            SetPrivateField(comp, "goalLabel",           goalLabel);
            SetPrivateField(comp, "rewardLabel",         rewardLabel);
            SetPrivateField(comp, "startButton",         startBtn);
            SetPrivateField(comp, "backButton",          backBtn);

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/PuzzleStartScene.unity");
        }

        // ──────────────────────────────────────────
        // PuzzleGameScene
        // ──────────────────────────────────────────
        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEventSystem();
            var canvas = CreateCanvas("Canvas");
            CreatePanel(canvas.transform, "BG", new Color(0.9f, 1f, 0.9f));

            // HUD
            var movesLabel  = CreateLabel(canvas.transform, "MovesLabel",    "残り手数: XX", new Vector2(-150, 250), 28);
            var pauseBtn    = CreateButton(canvas.transform, "PauseButton",  "||",           new Vector2(200, 250),  new Color(0.7f,0.9f,1f));
            var goalLabel   = CreateLabel(canvas.transform, "GoalLabel",     "目標\n...",    new Vector2(150, 100),  20);
            var pokoLabel   = CreateLabel(canvas.transform, "PokoLabel",     "がんばれ！",  new Vector2(0, -250),   24);

            // Board
            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(canvas.transform, false);
            var boardRect = boardGo.AddComponent<RectTransform>();
            boardRect.sizeDelta = new Vector2(400, 400);
            boardRect.anchoredPosition = new Vector2(-60, 20);
            var grid = boardGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70, 70);
            grid.spacing  = new Vector2(4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5; // ランタイムで変更可

            // CellPrefab
            var cellPrefab = CreateCellPrefab();

            // UI コンポーネント
            var ui   = new GameObject("PuzzleGameScreenUI");
            var comp = ui.AddComponent<PuzzleGameScreenUI>();

            SetPrivateField(comp, "movesLabel",      movesLabel);
            SetPrivateField(comp, "goalLabel",       goalLabel);
            SetPrivateField(comp, "pokoMessageLabel",pokoLabel);
            SetPrivateField(comp, "pauseButton",     pauseBtn.GetComponent<Button>());
            SetPrivateField(comp, "boardParent",     boardGo.transform);
            SetPrivateField(comp, "cellButtonPrefab",cellPrefab);

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/PuzzleGameScene.unity");
        }

        // ──────────────────────────────────────────
        // PuzzleClearScene
        // ──────────────────────────────────────────
        private static void BuildClearScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEventSystem();
            var canvas = CreateCanvas("Canvas");
            CreatePanel(canvas.transform, "BG", new Color(1f, 1f, 0.8f));

            var clearImg    = CreateImagePanel(canvas.transform, "ClearImage", new Vector2(0, 150), new Vector2(300,120), new Color(1f,0.8f,0.2f));
            var clearLabel  = CreateLabel(canvas.transform, "ClearLabel",  "成功！",    new Vector2(0, 150), 48);
            var rewardLabel = CreateLabel(canvas.transform, "RewardLabel", "獲得報酬\n...", new Vector2(0, 20),  26);
            var homeBtn     = CreateButton(canvas.transform, "HomeButton", "スタートへ", new Vector2(0, -120), new Color(1f,0.6f,0.3f));

            var ui   = new GameObject("PuzzleClearScreenUI");
            var comp = ui.AddComponent<PuzzleClearScreenUI>();

            SetPrivateField(comp, "clearMessageLabel", clearLabel);
            SetPrivateField(comp, "clearImage",        clearImg);
            SetPrivateField(comp, "rewardLabel",       rewardLabel);
            SetPrivateField(comp, "homeButton",        homeBtn.GetComponent<Button>());

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/PuzzleClearScene.unity");
        }

        // ──────────────────────────────────────────
        // PuzzleFailScene
        // ──────────────────────────────────────────
        private static void BuildFailScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEventSystem();
            var canvas = CreateCanvas("Canvas");
            CreatePanel(canvas.transform, "BG", new Color(0.85f, 0.85f, 0.85f));

            var dimOverlay  = CreateImagePanel(canvas.transform, "DimOverlay", Vector2.zero, new Vector2(600,800), new Color(0,0,0,0.55f));
            var failImg     = CreateImagePanel(canvas.transform, "FailImage",  new Vector2(0, 120), new Vector2(300,120), new Color(0.6f,0.6f,0.6f));
            var failLabel   = CreateLabel(canvas.transform, "FailLabel",   "失敗",         new Vector2(0, 120), 48);
            var retryBtn    = CreateButton(canvas.transform, "RetryButton","もう一回あそぶ",new Vector2(0, -60),  new Color(1f,0.5f,0.5f));
            var homeBtn     = CreateButton(canvas.transform, "HomeButton", "スタートへ",   new Vector2(0,-140),  new Color(0.8f,0.8f,0.8f));

            var ui   = new GameObject("PuzzleFailScreenUI");
            var comp = ui.AddComponent<PuzzleFailScreenUI>();

            SetPrivateField(comp, "failMessageLabel", failLabel);
            SetPrivateField(comp, "failImage",        failImg);
            SetPrivateField(comp, "boardDimOverlay",  dimOverlay);
            SetPrivateField(comp, "retryButton",      retryBtn.GetComponent<Button>());
            SetPrivateField(comp, "homeButton",       homeBtn.GetComponent<Button>());

            EditorSceneManager.SaveScene(scene, $"{SceneDir}/PuzzleFailScene.unity");
        }

        // ──────────────────────────────────────────
        // BuildSettings 登録
        // ──────────────────────────────────────────
        private static void UpdateBuildSettings()
        {
            var scenes = new[]
            {
                $"{SceneDir}/PuzzleStartScene.unity",
                $"{SceneDir}/PuzzleGameScene.unity",
                $"{SceneDir}/PuzzleClearScene.unity",
                $"{SceneDir}/PuzzleFailScene.unity",
            };

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var path in scenes)
            {
                if (!existing.Exists(s => s.path == path))
                    existing.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = existing.ToArray();
        }

        // ──────────────────────────────────────────
        // ヘルパー
        // ──────────────────────────────────────────

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Input System Package が有効な場合は InputSystemUIInputModule を使う。
            // なければ StandaloneInputModule にフォールバック。
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                go.AddComponent(inputModuleType);
            else
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            // カメラ
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta  = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
        }

        private const string JaFontAssetPath = "Assets/OyatsuPuzzle/Fonts/HiraginoSans-W4 SDF.asset";
        private const string JaFontTtcPath   = "Assets/OyatsuPuzzle/Fonts/HiraginoSans-W4.ttc";

        // 日本語TMP_FontAssetを返す。未生成なら Dynamic モードで生成する。
        private static TMP_FontAsset FindJapaneseFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JaFontAssetPath);
            if (existing != null) return existing;

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(JaFontTtcPath);
            if (sourceFont == null)
            {
                Debug.LogWarning("[OyatsuPuzzle] ヒラギノフォントが見つかりません: " + JaFontTtcPath);
                return null;
            }

            var fa = TMP_FontAsset.CreateFontAsset(sourceFont);
            AssetDatabase.CreateAsset(fa, JaFontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[OyatsuPuzzle] 日本語フォントアセット生成: " + JaFontAssetPath);
            return fa;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, Vector2 pos, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 200);
            rect.anchoredPosition = pos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var jaFont = FindJapaneseFontAsset();
            if (jaFont != null) tmp.font = jaFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260, 60);
            rect.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            var jaFont = FindJapaneseFontAsset();
            if (jaFont != null) tmp.font = jaFont;
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        private static Image CreateImagePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static GameObject CreateCellPrefab()
        {
            var go = new GameObject("CellPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(70, 70);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.9f, 0.85f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lRect = labelGo.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.sizeDelta = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            var jaFont = FindJapaneseFontAsset();
            if (jaFont != null) tmp.font = jaFont;
            tmp.text = "?";
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Center;

            // Prefab として保存
            Directory.CreateDirectory("Assets/OyatsuPuzzle/Prefabs");
            var prefabPath = "Assets/OyatsuPuzzle/Prefabs/CellButton.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
#endif
