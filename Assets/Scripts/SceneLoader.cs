using UnityEngine;
using UnityEngine.SceneManagement;

//画面切替

public class SceneLoader : MonoBehaviour
{
    public void GoToHome() => SceneManager.LoadScene("Home");
    public void GotoCare()
    {
        Debug.Log("Careボタンが押されたお！");
        SceneManager.LoadScene("Scenes/Care");
    }
    public void GoToSetting() => SceneManager.LoadScene("Setting");
    public void GoToShop() => SceneManager.LoadScene("Shop");
    public void GoToCoinPurchase() => SceneManager.LoadScene("CoinPurchase");
    public void GoToChat() => SceneManager.LoadScene("Chat");

    public void GoToRanking() => SceneManager.LoadScene("Ranking");
    public void GoToMyCollection() => SceneManager.LoadScene("MyCollection");
    public static void LoadHome()
    {
        SceneManager.LoadScene("Home");
    }

}
