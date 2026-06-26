#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace OyatsuPuzzle
{
    public static class OyatsuPuzzleSceneSetup
    {
        private const string ScenePath     = "Assets/OyatsuPuzzle/Scenes/OyatsuPuzzleTest.unity";
        private const string JaFontOutPath = "Assets/OyatsuPuzzle/Fonts/JaFont_Generated.asset";

        // Japanese font generation is intentionally disabled.
        // All TMP_Text objects use the TMP default font (LiberationSans SDF).
        // Do NOT call TMP_FontAsset.CreateFontAsset here.

        [MenuItem("OyatsuPuzzle/Delete JaFont_Generated Asset")]
        public static void DeleteJaFont()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(JaFontOutPath) != null)
            {
                AssetDatabase.DeleteAsset(JaFontOutPath);
                AssetDatabase.Refresh();
                Debug.Log("[OyatsuPuzzle] JaFont_Generated deleted.");
            }
            else
            {
                Debug.Log("[OyatsuPuzzle] JaFont_Generated not found, nothing to delete.");
            }
        }

        [MenuItem("OyatsuPuzzle/Setup Scene (Editor API)")]
        public static void SetupScene()
        {
            Debug.Log("[OyatsuPuzzle] Japanese font generation disabled.");

            // ─── 新規シーン作成 ───────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ─── PuzzleManagers ───────────────────────────
            var managersGO = new GameObject("PuzzleManagers");

            var pmGO = new GameObject("PuzzleManager");
            pmGO.transform.SetParent(managersGO.transform);
            var pm = pmGO.AddComponent<PuzzleManager>();

            var dpmGO = new GameObject("PuzzleDailyPlayManager");
            dpmGO.transform.SetParent(managersGO.transform);
            var dpm = dpmGO.AddComponent<PuzzleDailyPlayManager>();

            var scGO = new GameObject("PuzzleScreenController");
            scGO.transform.SetParent(managersGO.transform);
            var sc = scGO.AddComponent<PuzzleScreenController>();

            // ─── EventSystem ──────────────────────────────
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                esGO.AddComponent(inputModuleType);
            else
                esGO.AddComponent<StandaloneInputModule>();

            // ─── Canvas ───────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ─── カメラ ───────────────────────────────────
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();

            // ─── 4パネル作成 ──────────────────────────────
            var startPanel = BuildStartPanel(canvasGO.transform, pm, dpm, sc);
            var gamePanel  = BuildGamePanel(canvasGO.transform, pm, sc);
            var clearPanel = BuildClearPanel(canvasGO.transform, pm, sc);
            var failPanel  = BuildFailPanel(canvasGO.transform, pm, dpm, sc);

            // ─── PuzzleScreenController に参照を設定 ──────
            var scSO = new SerializedObject(sc);
            scSO.FindProperty("startPanel").objectReferenceValue = startPanel;
            scSO.FindProperty("gamePanel").objectReferenceValue  = gamePanel;
            scSO.FindProperty("clearPanel").objectReferenceValue = clearPanel;
            scSO.FindProperty("failPanel").objectReferenceValue  = failPanel;

            var startUI = startPanel.GetComponent<PuzzleStartScreenUI>();
            var gameUI  = gamePanel.GetComponent<PuzzleGameScreenUI>();
            var clearUI = clearPanel.GetComponent<PuzzleClearScreenUI>();
            var failUI  = failPanel.GetComponent<PuzzleFailScreenUI>();

            scSO.FindProperty("startScreenUI").objectReferenceValue = startUI;
            scSO.FindProperty("gameScreenUI").objectReferenceValue  = gameUI;
            scSO.FindProperty("clearScreenUI").objectReferenceValue = clearUI;
            scSO.FindProperty("failScreenUI").objectReferenceValue  = failUI;
            scSO.ApplyModifiedProperties();

            // ─── Font Asset 未設定の TMP_Text を補完 ─────────
            FixMissingFontAssets();

            // ─── シーン保存 ───────────────────────────────
            Directory.CreateDirectory("Assets/OyatsuPuzzle/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            AddToBuildSettings(ScenePath);

            AssetDatabase.Refresh();
            Debug.Log("[OyatsuPuzzle] All TMP_Text objects use default TMP font.");
            Debug.Log("[OyatsuPuzzle] Temporary English labels assigned.");
            Debug.Log("[OyatsuPuzzle] Scene built: " + ScenePath);
        }

        // ─────────────────────────────────────────────────────
        // StartPanel  (English labels, default TMP font)
        // ─────────────────────────────────────────────────────
        private static GameObject BuildStartPanel(
            Transform canvasT, PuzzleManager pm, PuzzleDailyPlayManager dpm, PuzzleScreenController sc)
        {
            var panel = CreateFullPanel(canvasT, "PuzzleStartPanel", new Color(1f, 0.96f, 0.9f));
            var C = new Color(0.45f, 0.22f, 0.08f);
            var P = new Color(0.95f, 0.42f, 0.55f);

            // ── [Header] — always visible, lives directly on panel ──────────
            CreateColorBlock(panel.transform, "HeaderBg", 1080f, 140f, 0f, 890f, new Color(1f, 0.88f, 0.82f), fullWidth: true);
            var backBtn  = CreateButton(panel.transform, "BackButton", "Back", -460f, 890f, new Color(1f, 0.82f, 0.78f), w: 110f, h: 110f);
            SetLabelFontSize(backBtn, 28);
            var titleTmp = CreateTMPLabel(panel.transform, "TitleText", "Play Time", 0f, 890f, 46, C, w: 640f, h: 90f);
            var coinTmp  = CreateTMPLabel(panel.transform, "CoinText",  "1,250",    340f, 893f, 30, C, w: 180f, h: 70f);
            var plusBtn  = CreateButton(panel.transform, "PlusButton",  "Plus",  460f, 890f, P, w: 80f, h: 80f);
            SetLabelFontSize(plusBtn, 22);

            // ═══════════════════════════════════════════════════
            // NormalStartRoot — main start screen content
            // ═══════════════════════════════════════════════════
            var normalStartRootGO = new GameObject("NormalStartRoot");
            normalStartRootGO.transform.SetParent(panel.transform, false);
            {
                var rt = normalStartRootGO.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var normalT = normalStartRootGO.transform;

            // [2] STEP badge + subtitle
            CreateColorBlock(normalT, "StepBadgeBg", 1080f, 52f, 0f, 778f, P, fullWidth: true);
            var stepBadgeTmp = CreateTMPLabel(normalT, "StepBadgeText", "STEP 1", 0f, 778f, 30, Color.white, w: 900f, h: 52f);
            CreateColorBlock(normalT, "SubtitleBg", 1080f, 72f, 0f, 720f, new Color(1f, 0.97f, 0.93f), fullWidth: true);
            var subtitleTmp = CreateTMPLabel(normalT, "SubtitleText", "Poko Snack Puzzle", 0f, 720f, 34, C, w: 900f, h: 72f);

            // [3] Support + Remaining plays
            CreateColorBlock(normalT, "SupportBg", 480f, 80f, -230f, 635f, new Color(1f, 1f, 0.95f));
            var supportTmp = CreateTMPLabel(normalT, "SupportMessageText", "Collect snacks!", -230f, 633f, 24, C, w: 440f, h: 74f);

            CreateColorBlock(normalT, "PlayCountCardBg", 460f, 160f, 270f, 610f, new Color(1f, 0.96f, 0.96f));
            CreateTMPLabel(normalT, "RemainingPlaysTitleText", "Plays Remaining", 270f, 650f, 20, new Color(0.6f, 0.2f, 0.3f), w: 420f, h: 44f);
            var remainValueTmp = CreateTMPLabel(normalT, "RemainingPlaysValueText", "Plays 5 / 5", 270f, 600f, 32, P, w: 420f, h: 70f);
            var remainNoteTmp  = CreateTMPLabel(normalT, "RemainingPlaysNoteText",  "5 plays left today", 270f, 555f, 19, new Color(0.6f, 0.2f, 0.3f), w: 420f, h: 44f);

            // [4] Stage progress dots
            CreateColorBlock(normalT, "StageProgressBg", 1080f, 110f, 0f, 475f, new Color(0.96f, 0.88f, 0.8f), fullWidth: true);
            var stageDotImgs = new Image[5];
            for (int i = 1; i <= 5; i++)
            {
                float dotX   = (i - 3) * 170f;
                Color dotCol = i == 1 ? P : new Color(0.88f, 0.80f, 0.73f);
                var dotGO = new GameObject($"StageDot{i}");
                dotGO.transform.SetParent(normalT, false);
                var dotRT = dotGO.AddComponent<RectTransform>();
                dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
                dotRT.sizeDelta        = new Vector2(88f, 88f);
                dotRT.anchoredPosition = new Vector2(dotX, 475f);
                var dotImg = dotGO.AddComponent<Image>();
                dotImg.color = dotCol;
                stageDotImgs[i - 1] = dotImg;
                CreateTMPLabel(normalT, $"StageDotLabel{i}", i.ToString(), dotX, 475f, 34, Color.white, w: 88f, h: 88f);
            }

            // [5] Challenge header
            CreateColorBlock(normalT, "ChallengeTitleBg", 1080f, 80f, 0f, 380f, P, fullWidth: true);
            var challengeTitleTmp = CreateTMPLabel(normalT, "ChallengeTitleText", "Today's Challenge", 0f, 380f, 28, Color.white, w: 900f, h: 80f);

            // [6] Goal card (left) + Reward list (right)
            float cardTop = 295f;
            float cardH   = 440f;
            float cardCy  = cardTop - cardH * 0.5f;

            CreateColorBlock(normalT, "GoalCardBg", 440f, cardH, -295f, cardCy, new Color(1f, 0.98f, 0.93f));
            var goalTitleTmp = CreateTMPLabel(normalT, "GoalTitleText", "Stage 1 Goal", -295f, cardTop - 38f, 20, P, w: 400f, h: 46f);
            CreateTMPLabel(normalT, "GoalIconText", "*", -295f, cardCy + 120f, 52, C, w: 200f, h: 80f);
            var goalItemTmp  = CreateTMPLabel(normalT, "GoalItemText",  "Collect Niboshi", -295f, cardCy - 10f, 24, C, w: 380f, h: 200f);
            var goalCountTmp = CreateTMPLabel(normalT, "GoalCountText", "",                -295f, cardCy - 10f, 24, C, w: 380f, h: 0f);
            var goalNoteTmp  = CreateTMPLabel(normalT, "GoalNoteText",  "",                -295f, cardCy - 10f, 24, C, w: 380f, h: 0f);

            CreateColorBlock(normalT, "RewardCardBg", 590f, cardH, 255f, cardCy, new Color(1f, 0.98f, 0.93f));
            var rewardListTitleTmp = CreateTMPLabel(normalT, "RewardListTitleText", "Stage Rewards", 255f, cardTop - 38f, 20, P, w: 560f, h: 46f);

            float rowBaseY = cardTop - 100f;
            float rowStep  = -74f;
            var row1 = CreateTMPLabel(normalT, "RewardRow1Text", "1  Free Coin +50",       255f, rowBaseY + rowStep * 0, 20, C, w: 540f, h: 62f, align: TextAlignmentOptions.Left);
            var row2 = CreateTMPLabel(normalT, "RewardRow2Text", "2  Niboshi x1",           255f, rowBaseY + rowStep * 1, 20, C, w: 540f, h: 62f, align: TextAlignmentOptions.Left);
            var row3 = CreateTMPLabel(normalT, "RewardRow3Text", "3  Free Coin +50",       255f, rowBaseY + rowStep * 2, 20, C, w: 540f, h: 62f, align: TextAlignmentOptions.Left);
            var row4 = CreateTMPLabel(normalT, "RewardRow4Text", "4  Random Reward",        255f, rowBaseY + rowStep * 3, 18, C, w: 540f, h: 62f, align: TextAlignmentOptions.Left);
            var row5 = CreateTMPLabel(normalT, "RewardRow5Text", "5  Free Coin +150\n   + Trust +50pt", 255f, rowBaseY + rowStep * 4, 18, C, w: 540f, h: 74f, align: TextAlignmentOptions.Left);

            // [7] Start button — inside NormalStartRoot
            var startBtn = CreateButton(normalT, "StartButton", "Start Snack Puzzle", 0f, -195f, P, w: 900f, h: 130f);
            SetLabelFontSize(startBtn, 38);

            // [8] Bottom nav — inside NormalStartRoot
            CreateColorBlock(normalT, "BottomNavBg", 1080f, 150f, 0f, -345f, new Color(1f, 0.88f, 0.82f), fullWidth: true);
            var helpBtn = CreateButton(normalT, "HelpButton", "Help", -440f, -345f, new Color(0.88f, 0.90f, 1f), w: 130f, h: 130f);
            var homeBtn = CreateButton(normalT, "HomeButton", "Home",  440f, -345f, new Color(1f, 0.80f, 0.70f), w: 130f, h: 130f);

            // ═══════════════════════════════════════════════════
            // DebugButtonsRoot — topmost sibling
            // Active in Editor, inactive in release builds (Awake #if UNITY_EDITOR)
            // ═══════════════════════════════════════════════════
            var debugRootGO = new GameObject("DebugButtonsRoot");
            debugRootGO.transform.SetParent(panel.transform, false);
            {
                var rt = debugRootGO.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var dbgT = debugRootGO.transform;

            float dbgX    = 0f;
            float dbgBotY = -480f;
            float dbgStep = -70f;

            var debugResetBtn = CreateButton(dbgT, "DebugResetPlaysButton",
                "DEBUG RESET PLAYS", dbgX, dbgBotY + dbgStep * 0, new Color(0.4f, 0.4f, 0.4f), w: 360f, h: 60f);
            SetLabelFontSize(debugResetBtn, 18);

            var debugResetStageBtn = CreateButton(dbgT, "DebugResetStageButton",
                "DEBUG RESET STAGE", dbgX, dbgBotY + dbgStep * 1, new Color(0.25f, 0.45f, 0.25f), w: 360f, h: 60f);
            SetLabelFontSize(debugResetStageBtn, 18);

            var debugResetAllBtn = CreateButton(dbgT, "DebugResetAllButton",
                "DEBUG RESET ALL", dbgX, dbgBotY + dbgStep * 2, new Color(0.65f, 0.18f, 0.05f), w: 360f, h: 60f);
            SetLabelFontSize(debugResetAllBtn, 18);

            var debugSetStage4Btn = CreateButton(dbgT, "DebugSetStage4Button",
                "DEBUG SET STAGE 4", dbgX, dbgBotY + dbgStep * 3, new Color(0.2f, 0.3f, 0.6f), w: 360f, h: 60f);
            SetLabelFontSize(debugSetStage4Btn, 18);

            var debugSetStage5Btn = CreateButton(dbgT, "DebugSetStage5Button",
                "DEBUG SET STAGE 5", dbgX, dbgBotY + dbgStep * 4, new Color(0.2f, 0.3f, 0.6f), w: 360f, h: 60f);
            SetLabelFontSize(debugSetStage5Btn, 18);

            // DebugButtonsRoot starts active in Editor (Awake switches it off for release)
            debugRootGO.SetActive(true);

            // ── PuzzleStartScreenUI 参照設定 ──────────────────
            var ui   = panel.AddComponent<PuzzleStartScreenUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("titleText").objectReferenceValue               = titleTmp;
            uiSO.FindProperty("coinText").objectReferenceValue                = coinTmp;
            uiSO.FindProperty("backButton").objectReferenceValue              = backBtn.GetComponent<Button>();
            uiSO.FindProperty("plusButton").objectReferenceValue              = plusBtn.GetComponent<Button>();
            uiSO.FindProperty("stepBadgeText").objectReferenceValue           = stepBadgeTmp;
            uiSO.FindProperty("subtitleText").objectReferenceValue            = subtitleTmp;
            uiSO.FindProperty("supportMessageText").objectReferenceValue      = supportTmp;
            uiSO.FindProperty("remainingPlaysValueText").objectReferenceValue = remainValueTmp;
            uiSO.FindProperty("remainingPlaysNoteText").objectReferenceValue  = remainNoteTmp;

            var dotsProp = uiSO.FindProperty("stageDotImages");
            dotsProp.arraySize = stageDotImgs.Length;
            for (int i = 0; i < stageDotImgs.Length; i++)
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = stageDotImgs[i];

            uiSO.FindProperty("challengeTitleText").objectReferenceValue      = challengeTitleTmp;
            uiSO.FindProperty("goalTitleText").objectReferenceValue           = goalTitleTmp;
            uiSO.FindProperty("goalItemText").objectReferenceValue            = goalItemTmp;
            uiSO.FindProperty("goalCountText").objectReferenceValue           = goalCountTmp;
            uiSO.FindProperty("goalNoteText").objectReferenceValue            = goalNoteTmp;
            uiSO.FindProperty("rewardListTitleText").objectReferenceValue     = rewardListTitleTmp;
            uiSO.FindProperty("rewardRow1Text").objectReferenceValue          = row1;
            uiSO.FindProperty("rewardRow2Text").objectReferenceValue          = row2;
            uiSO.FindProperty("rewardRow3Text").objectReferenceValue          = row3;
            uiSO.FindProperty("rewardRow4Text").objectReferenceValue          = row4;
            uiSO.FindProperty("rewardRow5Text").objectReferenceValue          = row5;

            uiSO.FindProperty("normalStartRoot").objectReferenceValue         = normalStartRootGO;

            uiSO.FindProperty("startButton").objectReferenceValue             = startBtn.GetComponent<Button>();
            uiSO.FindProperty("helpButton").objectReferenceValue              = helpBtn.GetComponent<Button>();
            uiSO.FindProperty("homeButton").objectReferenceValue              = homeBtn.GetComponent<Button>();
            uiSO.FindProperty("debugButtonsRoot").objectReferenceValue        = debugRootGO;
            uiSO.FindProperty("debugResetPlaysButton").objectReferenceValue   = debugResetBtn.GetComponent<Button>();
            uiSO.FindProperty("debugResetStageButton").objectReferenceValue   = debugResetStageBtn.GetComponent<Button>();
            uiSO.FindProperty("debugResetAllButton").objectReferenceValue     = debugResetAllBtn.GetComponent<Button>();
            uiSO.FindProperty("debugSetStage4Button").objectReferenceValue    = debugSetStage4Btn.GetComponent<Button>();
            uiSO.FindProperty("debugSetStage5Button").objectReferenceValue    = debugSetStage5Btn.GetComponent<Button>();
            uiSO.FindProperty("puzzleManager").objectReferenceValue           = pm;
            uiSO.FindProperty("dailyPlayManager").objectReferenceValue        = dpm;
            uiSO.FindProperty("screenController").objectReferenceValue        = sc;
            uiSO.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(startBtn.GetComponent<Button>().onClick,      ui.OnClickStart);
            UnityEventTools.AddPersistentListener(backBtn.GetComponent<Button>().onClick,       ui.OnClickBack);
            UnityEventTools.AddPersistentListener(homeBtn.GetComponent<Button>().onClick,       ui.OnClickHome);
            UnityEventTools.AddPersistentListener(helpBtn.GetComponent<Button>().onClick,       ui.OnClickHelp);
            UnityEventTools.AddPersistentListener(plusBtn.GetComponent<Button>().onClick,       ui.OnClickPlus);
            UnityEventTools.AddPersistentListener(debugResetBtn.GetComponent<Button>().onClick,      ui.OnClickDebugResetPlays);
            UnityEventTools.AddPersistentListener(debugResetStageBtn.GetComponent<Button>().onClick, ui.OnClickDebugResetStage);
            UnityEventTools.AddPersistentListener(debugResetAllBtn.GetComponent<Button>().onClick,   ui.OnClickDebugResetAll);
            UnityEventTools.AddPersistentListener(debugSetStage4Btn.GetComponent<Button>().onClick,  ui.OnClickDebugSetStage4);
            UnityEventTools.AddPersistentListener(debugSetStage5Btn.GetComponent<Button>().onClick,  ui.OnClickDebugSetStage5);

            return panel;
        }

        // ─────────────────────────────────────────────────────
        // GamePanel
        // ─────────────────────────────────────────────────────
        private static GameObject BuildGamePanel(
            Transform canvasT, PuzzleManager pm, PuzzleScreenController sc)
        {
            var panel = CreateFullPanel(canvasT, "PuzzleGamePanel", new Color(0.88f, 1f, 0.88f));

            var stageLabel   = CreateTMPLabel(panel.transform, "StageLabelText",     "Stage 1",   -200, 800, 30);
            var movesLabel   = CreateTMPLabel(panel.transform, "MoveCountLabelText", "Moves: 15",  200, 800, 30);
            var goalLabel    = CreateTMPLabel(panel.transform, "GoalLabelText",       "Goal:\n...", 0, 650, 24);
            var supportLabel = CreateTMPLabel(panel.transform, "SupportMessageText", "Go!",        0, -680, 30);

            var boardGO = new GameObject("BoardRoot");
            boardGO.transform.SetParent(panel.transform, false);
            var boardRT = boardGO.AddComponent<RectTransform>();
            boardRT.anchorMin = boardRT.anchorMax = new Vector2(0.5f, 0.5f);
            boardRT.sizeDelta = new Vector2(500, 500);
            boardRT.anchoredPosition = new Vector2(0, 50);
            var grid = boardGO.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(70, 70);
            grid.spacing         = new Vector2(4, 4);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var pauseBtn = CreateButton(panel.transform, "PauseButton", "||", 430, 800, new Color(0.7f, 0.9f, 1f), w: 80, h: 70);

            var ui   = panel.AddComponent<PuzzleGameScreenUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("stageLabelText").objectReferenceValue     = stageLabel;
            uiSO.FindProperty("moveCountLabelText").objectReferenceValue = movesLabel;
            uiSO.FindProperty("goalLabelText").objectReferenceValue      = goalLabel;
            uiSO.FindProperty("supportMessageText").objectReferenceValue = supportLabel;
            uiSO.FindProperty("boardRoot").objectReferenceValue          = boardGO.transform;
            uiSO.FindProperty("pauseButton").objectReferenceValue        = pauseBtn.GetComponent<Button>();
            uiSO.FindProperty("puzzleManager").objectReferenceValue      = pm;
            uiSO.FindProperty("screenController").objectReferenceValue   = sc;
            uiSO.ApplyModifiedProperties();

            panel.SetActive(false);
            return panel;
        }

        // ─────────────────────────────────────────────────────
        // ClearPanel
        // ─────────────────────────────────────────────────────
        private static GameObject BuildClearPanel(
            Transform canvasT, PuzzleManager pm, PuzzleScreenController sc)
        {
            var panel = CreateFullPanel(canvasT, "PuzzleClearPanel", new Color(1f, 1f, 0.78f));

            var titleText  = CreateTMPLabel(panel.transform, "ClearTitleText", "Clear!",      0, 600, 72);
            var rewardText = CreateTMPLabel(panel.transform, "RewardText",      "Reward\n...", 0, 200, 34);
            var nextText   = CreateTMPLabel(panel.transform, "NextInfoText",    "Next Stage",  0, -50, 28);
            var backBtn    = CreateButton(panel.transform, "BackToPuzzleStartButton", "To Start", 0, -250, new Color(1f, 0.6f, 0.3f));

            var ui   = panel.AddComponent<PuzzleClearScreenUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("clearTitleText").objectReferenceValue          = titleText;
            uiSO.FindProperty("rewardText").objectReferenceValue              = rewardText;
            uiSO.FindProperty("nextInfoText").objectReferenceValue            = nextText;
            uiSO.FindProperty("backToPuzzleStartButton").objectReferenceValue = backBtn.GetComponent<Button>();
            uiSO.FindProperty("puzzleManager").objectReferenceValue           = pm;
            uiSO.FindProperty("screenController").objectReferenceValue        = sc;
            uiSO.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(backBtn.GetComponent<Button>().onClick, ui.OnClickBackToStart);

            panel.SetActive(false);
            return panel;
        }

        // ─────────────────────────────────────────────────────
        // FailPanel
        // ─────────────────────────────────────────────────────
        private static GameObject BuildFailPanel(
            Transform canvasT, PuzzleManager pm, PuzzleDailyPlayManager dpm, PuzzleScreenController sc)
        {
            var panel = CreateFullPanel(canvasT, "PuzzleFailPanel", new Color(0.82f, 0.82f, 0.82f));

            var titleText  = CreateTMPLabel(panel.transform, "FailTitleText",        "Fail",          0, 600, 72, Color.white);
            var stageText  = CreateTMPLabel(panel.transform, "CurrentStageText",     "Stage 1",       0, 450, 34);
            var remainText = CreateTMPLabel(panel.transform, "RemainingPlaysText",   "Plays left: 3", 0, 330, 28);
            var retryBtn   = CreateButton(panel.transform, "RetryButton",            "Retry",    0, -100, new Color(1f, 0.5f, 0.5f));
            var backBtn    = CreateButton(panel.transform, "BackToPuzzleStartButton","To Start", 0, -230, new Color(0.75f, 0.75f, 0.75f));

            var ui   = panel.AddComponent<PuzzleFailScreenUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("failTitleText").objectReferenceValue           = titleText;
            uiSO.FindProperty("currentStageText").objectReferenceValue        = stageText;
            uiSO.FindProperty("remainingPlaysText").objectReferenceValue      = remainText;
            uiSO.FindProperty("retryButton").objectReferenceValue             = retryBtn.GetComponent<Button>();
            uiSO.FindProperty("backToPuzzleStartButton").objectReferenceValue = backBtn.GetComponent<Button>();
            uiSO.FindProperty("puzzleManager").objectReferenceValue           = pm;
            uiSO.FindProperty("dailyPlayManager").objectReferenceValue        = dpm;
            uiSO.FindProperty("screenController").objectReferenceValue        = sc;
            uiSO.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(retryBtn.GetComponent<Button>().onClick, ui.OnClickRetry);
            UnityEventTools.AddPersistentListener(backBtn.GetComponent<Button>().onClick,  ui.OnClickBackToStart);

            panel.SetActive(false);
            return panel;
        }

        // ─────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────

        // シーン内の全 TextMeshProUGUI を走査し、Font Asset が null のものに
        // TMP のデフォルトフォントを明示的に割り当てる。
        // AddComponent 直後に他プロパティを設定すると font が null のまま保存されることがある。
        private static void FixMissingFontAssets()
        {
            // デフォルトフォントを取得（TMP_Settings → LiberationSans SDF の順に探す）
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                var guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
                if (guids.Length > 0)
                    defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (defaultFont == null)
            {
                Debug.LogWarning("[OyatsuPuzzle] Default TMP Font Asset not found. Please set it in TMP Settings.");
                return;
            }

            int fixedCount = 0;
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.font == null)
                {
                    Debug.Log($"[OyatsuPuzzle] Fixed missing TMP Font Asset: {tmp.gameObject.name}");
                    tmp.font = defaultFont;
                    fixedCount++;
                }
            }

            if (fixedCount == 0)
                Debug.Log("[OyatsuPuzzle] No missing TMP Font Assets found.");

            Debug.Log("[OyatsuPuzzle] All TMP_Text font assets assigned.");
        }

        private static void SetLabelFontSize(GameObject btn, int size)
        {
            var lbl = btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (lbl != null) lbl.fontSize = size;
        }

        // fullWidth=true: anchor 0-1 on X (band), fullWidth=false: fixed size anchored at center
        private static void CreateColorBlock(Transform parent, string name,
            float w, float h, float ax, float ay, Color color, bool fullWidth = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (fullWidth)
            {
                rt.anchorMin        = new Vector2(0f, 0.5f);
                rt.anchorMax        = new Vector2(1f, 0.5f);
                rt.sizeDelta        = new Vector2(0f, h);
                rt.anchoredPosition = new Vector2(0f, ay);
            }
            else
            {
                rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta        = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(ax, ay);
            }
            go.AddComponent<Image>().color = color;
        }

        private static GameObject CreateFullPanel(Transform parent, string name, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bgColor;
            return go;
        }

        // font parameter removed — always uses TMP default (LiberationSans SDF)
        private static TMP_Text CreateTMPLabel(Transform parent, string name, string text,
            float x, float y, int fontSize, Color? color = null,
            float w = 900, float h = 200, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.color            = color ?? Color.black;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            // font is intentionally NOT set — TMP uses its built-in default
            return tmp;
        }

        // font parameter removed — always uses TMP default
        private static GameObject CreateButton(Transform parent, string name, string label,
            float x, float y, Color bgColor, float w = 500, float h = 100)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 34;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            // font is intentionally NOT set — TMP uses its built-in default
            return go;
        }

        private static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == path))
                list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
