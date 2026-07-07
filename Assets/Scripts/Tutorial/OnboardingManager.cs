using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using Game.Core;
using Game.Onboarding;
using System.Collections;

public class OnboardingManager : MonoBehaviour
{
    [Header("Onboarding Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject termsOfUsePanel;
    [SerializeField] private GameObject disAgreePanel;
    [SerializeField] private GameObject firstmeetPanel;
    [SerializeField] private GameObject characterPanelCard;
    [SerializeField] private GameObject characterNameInputPanel;
    [SerializeField] private GameObject userInfomationPanel;
    [SerializeField] private GameObject confirmPanel;

    // 未使用（動画廃止・新フローでは呼ばれない。StoryPanelManager.cs 削除時に本フィールドも削除予定・2026/7/6）
    [Header("Door Animation")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRawImage;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private OnboardingStep currentStep = OnboardingStep.Home;

    // パネル切替直後のタップ貫通防止用の入力ロック
    private bool inputLocked = false;
    private const float InputLockSeconds = 0.15f; // 切替後この時間だけ Next()/CompleteOnboarding() を無視
    private Coroutine inputLockCoroutine;

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            SceneManager.LoadScene("Main");
            return;
        }
        UpdateView();
    }

    // --- ステップ制御 ---
    public void Next()
    {
        if (inputLocked)
        {
            Debug.Log("[Onboarding] 入力ロック中のため無視（貫通防止）");
            return;
        }

        if (currentStep < OnboardingStep.Confirm)
        {
            var prevStep = currentStep;
            currentStep++;
            Debug.Log($"[Onboarding] Next() 呼び出し: {prevStep}({(int)prevStep}) → {currentStep}({(int)currentStep})");
            UpdateView();
        }
        else
        {
            Debug.Log("[Onboarding] 最終ステップ到達 → CompleteOnboarding() を呼びます");
            CompleteOnboarding();
        }
    }

    public void GoBack()
    {
        if (currentStep > OnboardingStep.Home)
        {
            var prevStep = currentStep;
            currentStep--;
            Debug.Log($"[Onboarding] GoBack() 呼び出し: {prevStep} → {currentStep}");
            UpdateView();
        }
    }

    private void UpdateView()
    {
        Debug.Log($"[Onboarding] UpdateView: currentStep = {currentStep}({(int)currentStep})");

        if (homePanel != null)
        {
            bool active = currentStep == OnboardingStep.Home;
            Debug.Log($"[Onboarding] homePanel.SetActive({active})");
            homePanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] homePanel が null（未結線）です");

        if (termsOfUsePanel != null)
        {
            bool active = currentStep == OnboardingStep.TermsOfUse;
            Debug.Log($"[Onboarding] termsOfUsePanel.SetActive({active})");
            termsOfUsePanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] termsOfUsePanel が null（未結線）です");

        if (firstmeetPanel != null)
        {
            bool active = currentStep == OnboardingStep.Firstmeet;
            Debug.Log($"[Onboarding] firstmeetPanel.SetActive({active})");
            firstmeetPanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] firstmeetPanel が null（未結線）です");

        if (characterPanelCard != null)
        {
            bool active = currentStep == OnboardingStep.CharacterCard;
            Debug.Log($"[Onboarding] characterPanelCard.SetActive({active})");
            characterPanelCard.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] characterPanelCard が null（未結線）です");

        if (characterNameInputPanel != null)
        {
            bool active = currentStep == OnboardingStep.CharacterNameInput;
            Debug.Log($"[Onboarding] characterNameInputPanel.SetActive({active})");
            characterNameInputPanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] characterNameInputPanel が null（未結線）です");

        if (userInfomationPanel != null)
        {
            bool active = currentStep == OnboardingStep.UserInfomation;
            Debug.Log($"[Onboarding] userInfomationPanel.SetActive({active})");
            userInfomationPanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] userInfomationPanel が null（未結線）です");

        if (confirmPanel != null)
        {
            bool active = currentStep == OnboardingStep.Confirm;
            Debug.Log($"[Onboarding] confirmPanel.SetActive({active})");
            confirmPanel.SetActive(active);
        }
        else Debug.LogWarning("[Onboarding] confirmPanel が null（未結線）です");

        // DisAgreePanel は TermsOfUse 以外では必ず非表示
        if (disAgreePanel != null && currentStep != OnboardingStep.TermsOfUse)
            disAgreePanel.SetActive(false);

        // パネル切替直後の貫通タップを弾くため、短時間だけ入力をロック
        LockInputBriefly();
    }

    // 切替直後に InputLockSeconds だけ inputLocked を立て、その間の Next()/CompleteOnboarding() を無視する
    private void LockInputBriefly()
    {
        if (inputLockCoroutine != null) StopCoroutine(inputLockCoroutine);
        inputLockCoroutine = StartCoroutine(InputLockCoroutine());
    }

    private IEnumerator InputLockCoroutine()
    {
        inputLocked = true;
        Debug.Log($"[Onboarding] 入力ロック開始（{InputLockSeconds}秒・貫通防止）");
        yield return new WaitForSeconds(InputLockSeconds);
        inputLocked = false;
        Debug.Log("[Onboarding] 入力ロック解除");
        inputLockCoroutine = null;
    }

    // --- AI同意しない場合 ---
    public void OnDisagreeClicked()
    {
        if (disAgreePanel != null) disAgreePanel.SetActive(true);
    }

    public void OnReadAgainClicked()
    {
        if (disAgreePanel != null) disAgreePanel.SetActive(false);
    }

    public void OnQuitAppClicked()
    {
        Application.Quit();
    }

    // --- 扉アニメーション ---
    // 未使用（動画廃止・新フローでは呼ばれない。StoryPanelManager.cs 削除時に本メソッドも削除予定・2026/7/6）
    public void PlayDoorAnimation()
    {
        Debug.Log($"<color=yellow>【OnboardingManager】PlayDoorAnimation() videoPanel={videoPanel != null}, videoPlayer={videoPlayer != null}, clip={videoPlayer?.clip != null}, rawImage={videoRawImage != null}</color>");
        if (videoPlayer != null && videoPlayer.clip != null && videoRawImage != null && videoPanel != null)
        {
            Debug.Log("<color=cyan>【OnboardingManager】動画再生開始</color>");
            StartCoroutine(PlayVideoCoroutine());
        }
        else
        {
            Debug.Log("<color=cyan>【OnboardingManager】動画再生できない → フェード処理へ</color>");
            StartCoroutine(FadeTransition());
        }
    }

    // 未使用（動画廃止・新フローでは呼ばれない。StoryPanelManager.cs 削除時に本メソッドも削除予定・2026/7/6）
    private IEnumerator PlayVideoCoroutine()
    {
        // 1. VideoPlayer をアクティブにして準備
        videoPlayer.gameObject.SetActive(true);
        videoPanel.SetActive(true);
        yield return null;

        // 2. RenderTexture を動的生成
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 0);
        videoPlayer.targetTexture = rt;
        videoRawImage.texture = rt;

        // 3. 準備
        videoPlayer.Prepare();
        float timeout = 5f;
        while (!videoPlayer.isPrepared && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            videoPanel.SetActive(false);
            rt.Release();
            Destroy(rt);
            StartCoroutine(FadeTransition());
            yield break;
        }

        // 4. フェードイン（白→透明）でVideoPanel を見せる
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 1f;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = 1f - (t / 0.5f);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        // 5. 動画再生
        videoPlayer.Play();
        float waitTimeout = 2f;
        while (!videoPlayer.isPlaying && waitTimeout > 0f)
        {
            waitTimeout -= Time.deltaTime;
            yield return null;
        }
        while (videoPlayer.isPlaying)
        {
            yield return null;
        }

        // 6. フェードアウト（透明→白）
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = t / 0.5f;
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // 7. 後片付け
        videoPlayer.targetTexture = null;
        videoRawImage.texture = null;
        rt.Release();
        Destroy(rt);
        videoPanel.SetActive(false);
        videoPlayer.gameObject.SetActive(false);

        // 8. 次のステップ表示（Loadingを先に表示してからパネル切り替え）
        if (LoadingManager.Instance != null)
            yield return LoadingManager.Instance.ShowAsync();
        Next();

        // 9. フェードイン（白→透明）で CharacterPanelCard を見せる
        // LoadingとフェードInが重なるのでLoadingを先に消す
        if (LoadingManager.Instance != null)
            yield return LoadingManager.Instance.HideAsync();
        if (fadeCanvasGroup != null)
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = 1f - (t / 0.5f);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    // 未使用（動画廃止・新フローでは呼ばれない。StoryPanelManager.cs 削除時に本メソッドも削除予定・2026/7/6）
    private IEnumerator FadeTransition()
    {
        if (fadeCanvasGroup == null) { Next(); yield break; }

        fadeCanvasGroup.gameObject.SetActive(true);
        // 暗転 0.5秒
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = t / 0.5f;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Loading表示・パネル切り替え（白い間に実行）
        if (LoadingManager.Instance != null)
            yield return LoadingManager.Instance.ShowAsync();
        Next();
        // LoadingとフェードInが重なるのでLoadingを先に消す
        if (LoadingManager.Instance != null)
            yield return LoadingManager.Instance.HideAsync();

        // 明転 0.5秒
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - (t / 0.5f);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    // --- 完了 ---
    public void CompleteOnboarding()
    {
        if (inputLocked)
        {
            Debug.Log("[Onboarding] 入力ロック中のため無視（貫通防止）");
            return;
        }

        Debug.Log("[Onboarding] CompleteOnboarding() 開始");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
        }
        Debug.Log("[Onboarding] onboardingCompleted=true 保存完了、Main をロードします");
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading("Main");
        else
            SceneManager.LoadScene("Main");
    }
}
