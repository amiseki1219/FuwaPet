using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void GoToHome() => SceneManager.LoadScene("Home");
    public void GoToCare() => SceneManager.LoadScene("Care");
    public void GoToSetting() => SceneManager.LoadScene("Setting");
    public void GoToShop() => SceneManager.LoadScene("Shop");
    public void GoToCoinPurchase() => SceneManager.LoadScene("CoinPurchase");
}
