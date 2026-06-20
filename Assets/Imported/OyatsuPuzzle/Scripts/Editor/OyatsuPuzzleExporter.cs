using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OyatsuPuzzle
{
    public static class OyatsuPuzzleExporter
    {
        private const string ScenePath   = "Assets/OyatsuPuzzle/Scenes/OyatsuPuzzleTest.unity";
        private const string PrefabPath  = "Assets/OyatsuPuzzle/OyatsuPuzzleRoot.prefab";
        private const string PackagePath = "OyatsuPuzzle.unitypackage";
        private const string RootName    = "OyatsuPuzzleRoot";

        // ─────────────────────────────────────────────────────────────────
        // Step 1: Open OyatsuPuzzleTest, build OyatsuPuzzleRoot, save Prefab
        // ─────────────────────────────────────────────────────────────────
        [MenuItem("OyatsuPuzzle/1. Build OyatsuPuzzleRoot Prefab")]
        public static bool BuildRoot()
        {
            // Ensure OyatsuPuzzleTest is open (open it if not)
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.path.EndsWith("OyatsuPuzzleTest.unity"))
            {
                Debug.Log($"[OyatsuPuzzle] Active scene: '{activeScene.name}'. Opening OyatsuPuzzleTest...");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[OyatsuPuzzle] Cancelled by user.");
                    return false;
                }
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var scene = SceneManager.GetActiveScene();
            Debug.Log($"[OyatsuPuzzle] Active scene: {scene.name} ({scene.path})");

            // Find GameObjects (anywhere in hierarchy) by name
            GameObject puzzleManagers = null;
            GameObject canvas         = null;

            foreach (var go in scene.GetRootGameObjects())
            {
                Debug.Log($"[OyatsuPuzzle]   Root GO: '{go.name}'");
            }

            // Search including children (handles case where a previous run moved them under OyatsuPuzzleRoot)
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == "PuzzleManagers" && puzzleManagers == null) puzzleManagers = go;
                if (go.name == "Canvas"          && canvas         == null) canvas         = go;
            }

            if (puzzleManagers == null)
            {
                Debug.LogError("[OyatsuPuzzle] PuzzleManagers not found anywhere in the scene.");
                return false;
            }
            if (canvas == null)
            {
                Debug.LogError("[OyatsuPuzzle] Canvas not found anywhere in the scene.");
                return false;
            }
            Debug.Log($"[OyatsuPuzzle] Found PuzzleManagers at: {GetPath(puzzleManagers)}");
            Debug.Log($"[OyatsuPuzzle] Found Canvas at: {GetPath(canvas)}");

            // Remove existing OyatsuPuzzleRoot if any (unparent children first to preserve them)
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Debug.Log("[OyatsuPuzzle] Removing existing OyatsuPuzzleRoot (unparenting children first).");
                // Unparent PuzzleManagers and Canvas before destroying root
                if (puzzleManagers.transform.parent != null && puzzleManagers.transform.parent.gameObject == existing)
                    puzzleManagers.transform.SetParent(null, false);
                if (canvas.transform.parent != null && canvas.transform.parent.gameObject == existing)
                    canvas.transform.SetParent(null, false);
                Object.DestroyImmediate(existing);
            }

            // Create root GameObject
            var root = new GameObject(RootName);
            var rt   = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Move PuzzleManagers and Canvas under root
            puzzleManagers.transform.SetParent(root.transform, false);
            Debug.Log("[OyatsuPuzzle] PuzzleManagers moved under OyatsuPuzzleRoot.");

            canvas.transform.SetParent(root.transform, false);
            Debug.Log("[OyatsuPuzzle] Canvas moved under OyatsuPuzzleRoot.");

            // Save scene so Prefab has correct references
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Save as Prefab
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                root, PrefabPath, InteractionMode.UserAction);

            if (prefab == null)
            {
                Debug.LogError("[OyatsuPuzzle] Failed to save Prefab.");
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[OyatsuPuzzle] OyatsuPuzzleRoot.prefab saved: {PrefabPath}");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        // Step 2: Export OyatsuPuzzle.unitypackage (with dependencies)
        // ─────────────────────────────────────────────────────────────────
        [MenuItem("OyatsuPuzzle/2. Export OyatsuPuzzle.unitypackage")]
        public static void ExportPackage()
        {
            // Check prefab exists via AssetDatabase (works regardless of OS path format)
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[OyatsuPuzzle] {PrefabPath} not found. Run step 1 first.");
                return;
            }

            // Collect all assets under Assets/OyatsuPuzzle/
            var assetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/OyatsuPuzzle/"))
                .ToArray();

            Debug.Log($"[OyatsuPuzzle] Exporting {assetPaths.Length} assets (+ dependencies)...");

            AssetDatabase.ExportPackage(
                assetPaths,
                PackagePath,
                ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);

            string fullPath = Path.GetFullPath(PackagePath);
            Debug.Log($"[OyatsuPuzzle] Export complete: {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        private static string GetPath(GameObject go)
        {
            var t = go.transform;
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        // ─────────────────────────────────────────────────────────────────
        // One-shot: Build Prefab then Export
        // ─────────────────────────────────────────────────────────────────
        [MenuItem("OyatsuPuzzle/Build Root + Export Package (One Shot)")]
        public static void BuildAndExport()
        {
            bool ok = BuildRoot();
            if (!ok)
            {
                Debug.LogError("[OyatsuPuzzle] BuildRoot failed. Export cancelled.");
                return;
            }
            ExportPackage();
        }
    }
}
