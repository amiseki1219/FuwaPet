using UnityEngine;
using Game.Core;

public class ProtectionUI : MonoBehaviour
{
    [SerializeField] GameObject protectedOverlay;

    void Update()
    {
        protectedOverlay.SetActive(GameContext.Instance.PetStatus.IsProtected);
    }

    public void OnRestart()
    {
        // ここは「データ初期化」処理を呼ぶ
        // SaveManagerがあるなら SaveManager.Instance.ResetAll() みたいなの作るのが理想
        PlayerPrefs.DeleteAll(); // まだJSON未統合なら暫定
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }
}
