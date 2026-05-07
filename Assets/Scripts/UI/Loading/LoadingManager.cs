using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    private static LoadingManager instance;
    public static LoadingManager Instance => instance;

    [SerializeField] private GameObject loadingPanelPrefab;

    private GameObject loadingPanelInstance;
    private Image progressBarFill;
    private TMP_Text percentText;
    private float currentProgress;
    private bool isLoading;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void EnsurePanel()
    {
        if (loadingPanelInstance != null) return;

        loadingPanelInstance = Instantiate(loadingPanelPrefab);
        DontDestroyOnLoad(loadingPanelInstance.transform.root.gameObject);

        progressBarFill = loadingPanelInstance.transform
            .Find("Background/Container/ProgressBarFrame/ProgressBarFill")
            .GetComponent<Image>();
        percentText = loadingPanelInstance.transform
            .Find("Background/Container/PercentText")
            .GetComponent<TMP_Text>();

        loadingPanelInstance.SetActive(false);
    }

    public void SetProgress(float value)
    {
        currentProgress = Mathf.Clamp01(value);
        if (progressBarFill != null)
            progressBarFill.fillAmount = currentProgress;
        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
    }

    /// <summary>
    /// シーン遷移をLoading画面付きで実行（ボタンコールバック等から呼び出し用）
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[LoadingManager] 既にロード中のため無視: {sceneName}");
            return;
        }
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;
        EnsurePanel();
        SetProgress(0f);
        loadingPanelInstance.SetActive(true);

        // 0 → 0.9 自動進行 (0.5秒)
        float elapsed = 0f;
        const float showDuration = 0.5f;
        while (elapsed < showDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / showDuration);
            SetProgress(Mathf.Lerp(0f, 0.9f, t));
            yield return null;
        }
        SetProgress(0.9f);

        // シーンを非同期読み込み
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[LoadingManager] LoadSceneAsync が null を返しました: '{sceneName}'");
            if (loadingPanelInstance != null) loadingPanelInstance.SetActive(false);
            isLoading = false;
            yield break;
        }

        // sceneLoaded イベントで完了を受け取り、新コルーチンを起動
        // → シーン遷移をまたいでコルーチンを生かし続けない設計
        SceneManager.sceneLoaded += OnSceneLoaded;
        op.allowSceneActivation = true;
        // ここでコルーチン終了（遷移後は OnSceneLoaded → FinishLoadingCoroutine が引き継ぐ）
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FinishLoadingCoroutine());
    }

    private IEnumerator FinishLoadingCoroutine()
    {
        // 0.9 → 1.0 フィル (0.2秒)
        float start = currentProgress;
        float elapsed = 0f;
        const float fillDuration = 0.2f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillDuration);
            SetProgress(Mathf.Lerp(start, 1f, t));
            yield return null;
        }
        SetProgress(1f);

        yield return new WaitForSeconds(0.15f);

        if (loadingPanelInstance != null)
            loadingPanelInstance.SetActive(false);
        isLoading = false;
    }

    /// <summary>
    /// Loading画面を表示（外部コルーチンから yield return で呼び出し可）
    /// </summary>
    public IEnumerator ShowAsync()
    {
        EnsurePanel();
        SetProgress(0f);
        loadingPanelInstance.SetActive(true);
        yield return ShowCoroutine(0.5f);
    }

    /// <summary>
    /// Loading画面を非表示（外部コルーチンから yield return で呼び出し可）
    /// </summary>
    public IEnumerator HideAsync()
    {
        yield return HideCoroutine();
    }

    /// <summary>
    /// 任意の処理をLoading画面付きで実行
    /// </summary>
    public void RunWithLoading(Func<IEnumerator> task)
    {
        StartCoroutine(RunWithLoadingCoroutine(task));
    }

    private IEnumerator RunWithLoadingCoroutine(Func<IEnumerator> task)
    {
        EnsurePanel();
        SetProgress(0f);
        loadingPanelInstance.SetActive(true);

        // Show と task を並行実行
        bool showDone = false;
        StartCoroutine(ShowCoroutine(0.5f, () => showDone = true));
        yield return task();
        while (!showDone)
            yield return null;

        // Hide
        yield return HideCoroutine();
    }

    private IEnumerator ShowCoroutine(float minDuration, Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < minDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / minDuration);
            SetProgress(Mathf.Lerp(0f, 0.9f, t));
            yield return null;
        }
        SetProgress(0.9f);
        onComplete?.Invoke();
    }

    private IEnumerator HideCoroutine()
    {
        float start = currentProgress;
        float elapsed = 0f;
        const float fillDuration = 0.2f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillDuration);
            SetProgress(Mathf.Lerp(start, 1f, t));
            yield return null;
        }
        SetProgress(1f);

        yield return new WaitForSeconds(0.15f);

        if (loadingPanelInstance != null)
            loadingPanelInstance.SetActive(false);
    }
}
