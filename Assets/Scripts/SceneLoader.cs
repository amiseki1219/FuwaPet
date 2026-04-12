using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void GoToHome() => SceneManager.LoadScene("Home");

    public void GotoMain()
    {
        Debug.Log("Careボタンが押されたお！");
        // Build Settingsに "Care" という名前で登録されている前提だっぴ
        SceneManager.LoadScene("Main");
    }
    
    public void GotoCare() => SceneManager.LoadScene("Care");
    public void GoToSetting() => SceneManager.LoadScene("Setting");
    public void GoToShop() => SceneManager.LoadScene("Shop");
    public void GoToCoinPurchase() => SceneManager.LoadScene("CoinPurchase");
    public void GoToChat() => SceneManager.LoadScene("Chat");
    public void GoToRanking() => SceneManager.LoadScene("Ranking");
    public void GoToMyCollection() => SceneManager.LoadScene("MyCollection");

    public static void LoadHome() => SceneManager.LoadScene("Home");
}