using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void LoadScene(string sceneName)
    {
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void GoToHome() => LoadScene("Home");

    /// <summary>
    /// Home の MainBtn から呼ぶ。初回はチュートリアル、2回目以降は Main へ。
    /// </summary>
    public void GoToStart()
    {
        bool completed = SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted;
        LoadScene(completed ? "Main" : "Tutorial");
    }

    public void GoToTutorial() => LoadScene("Tutorial");

    public void GotoMain()
    {
        Debug.Log("Careボタンが押された！");
        LoadScene("Main");
    }

    public void GotoCare()  => LoadScene("Care");
    public void GotoBath()  => LoadScene("Bath");
    public void GotoPlay()  => LoadScene("Play");
    public void GotoSleep() => LoadScene("Sleep");
    public void GoToSetting() => LoadScene("Setting");
    public void GoToShop() => LoadScene("Shop");
    public void GoToCoinPurchase() => LoadScene("CoinPurchase");
    public void GoToChat() => LoadScene("Chat");
    public void GoToMyCollection() => LoadScene("MyCollection");
    public void GoToRoomEdit() => LoadScene("RoomEdit");

    public static void LoadHome()
    {
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading("Home");
        else
            SceneManager.LoadScene("Home");
    }
}