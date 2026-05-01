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
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject characterPanelCard;
    [SerializeField] private GameObject profileSelectionPanelCard;

    [Header("Door Animation")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRawImage;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private OnboardingStep currentStep = OnboardingStep.HomePanel;

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            SceneManager.LoadScene("Care");
            return;
        }
        UpdateView();
    }

    // --- ステップ制御 ---
    public void Next()
    {
        if (currentStep < OnboardingStep.ProfileSelectionPanelCard)
        {
            currentStep++;
            UpdateView();
        }
        else
        {
            CompleteOnboarding();
        }
    }

    public void GoBack()
    {
        if (currentStep > OnboardingStep.HomePanel)
        {
            currentStep--;
            UpdateView();
        }
    }

    private void UpdateView()
    {
        if (homePanel != null)
            homePanel.SetActive(currentStep == OnboardingStep.HomePanel);
        if (termsOfUsePanel != null)
            termsOfUsePanel.SetActive(currentStep == OnboardingStep.TermsOfUsePanel);
        if (storyPanel != null)
            storyPanel.SetActive(currentStep == OnboardingStep.StoryPanel);
        if (characterPanelCard != null)
            characterPanelCard.SetActive(currentStep == OnboardingStep.CharacterPanelCard);
        if (profileSelectionPanelCard != null)
            profileSelectionPanelCard.SetActive(currentStep == OnboardingStep.ProfileSelectionPanelCard);

        // DisAgreePanel は TermsOfUsePanel 以外では必ず非表示
        if (disAgreePanel != null && currentStep != OnboardingStep.TermsOfUsePanel)
            disAgreePanel.SetActive(false);
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
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
        }
        SceneManager.LoadScene("Care");
    }
}
