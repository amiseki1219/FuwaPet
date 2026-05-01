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
        DontDestroyOnLoad(loadingPanelInstance);

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
        Debug.Log($"<color=cyan>[LoadingManager] LoadSceneWithLoading called for scene: {sceneName}</color>");
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        Debug.Log($"<color=cyan>[LoadingManager] Coroutine started for scene: {sceneName}</color>");
        EnsurePanel();
        SetProgress(0f);
        loadingPanelInstance.SetActive(true);
        Debug.Log("<color=cyan>[LoadingManager] Panel activated, starting progress animation</color>");

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
        Debug.Log("<color=cyan>[LoadingManager] Progress reached 90%, starting scene load</color>");

        // シーンを非同期読み込み
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        Debug.Log($"<color=cyan>[LoadingManager] LoadSceneAsync started, initial progress: {op.progress}</color>");
        while (op.progress < 0.9f)
        {
            yield return null;
        }
        Debug.Log($"<color=cyan>[LoadingManager] Scene load progress complete: {op.progress}, activating scene</color>");
        op.allowSceneActivation = true;
        yield return null;

        // 0.9 → 1.0 フィル (0.2秒)
        float start = currentProgress;
        elapsed = 0f;
        const float fillDuration = 0.2f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillDuration);
            SetProgress(Mathf.Lerp(start, 1f, t));
            yield return null;
        }
        SetProgress(1f);
        Debug.Log("<color=cyan>[LoadingManager] Progress reached 100%, waiting before hiding</color>");

        yield return new WaitForSeconds(0.15f);

        if (loadingPanelInstance != null)
            loadingPanelInstance.SetActive(false);
        Debug.Log("<color=cyan>[LoadingManager] Loading complete, panel hidden</color>");
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
