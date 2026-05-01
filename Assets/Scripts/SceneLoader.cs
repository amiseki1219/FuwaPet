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

    public void GotoMain()
    {
        Debug.Log("Careボタンが押されたお！");
        LoadScene("Main");
    }

    public void GotoCare() => LoadScene("Care");
    public void GoToSetting() => LoadScene("Setting");
    public void GoToShop() => LoadScene("Shop");
    public void GoToCoinPurchase() => LoadScene("CoinPurchase");
    public void GoToChat() => LoadScene("Chat");
    public void GoToRanking() => LoadScene("Ranking");
    public void GoToMyCollection() => LoadScene("MyCollection");

    public static void LoadHome()
    {
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading("Home");
        else
            SceneManager.LoadScene("Home");
    }
}