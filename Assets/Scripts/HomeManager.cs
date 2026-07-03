using UnityEngine;

public class HomeManager : MonoBehaviour
{
    void Start()
    {
        // 起動振り分け: 初回またはアカウント削除後はTutorialへ（ローディングなし）
        if (SaveManager.Instance == null || !SaveManager.Instance.Data.onboardingCompleted)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
            return;
        }

        // 2回目以降はSceneに配置済みの背景・StartText・MainBtnでHome画面が成立するため、
        // コード側での表示処理は不要。
    }
}
