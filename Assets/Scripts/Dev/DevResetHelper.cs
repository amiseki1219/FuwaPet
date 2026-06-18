using UnityEngine;
using Game.Core;

/// <summary>
/// 開発用リセットヘルパー。
/// Inspector で showDevTools = true にするとボタンが表示される。
/// リリース前に false に戻すこと。
/// </summary>
public class DevResetHelper : MonoBehaviour
{
    [Header("DEV ONLY — リリース前に false にすること")]
    [SerializeField] private bool showDevTools = false;
    [SerializeField] private GameObject devButtonGO;

    private void Start()
    {
        if (devButtonGO != null)
            devButtonGO.SetActive(showDevTools);
    }

    /// <summary>おやつの1日使用カウントをリセットする（開発用）。</summary>
    public void DevResetFreeOyatuCount()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null)
        {
            Debug.LogWarning("[DevResetHelper] SaveManager not found.");
            return;
        }
        save.freeOyatuCountToday = 0;
        save.lastFreeOyatuDate   = "";
        Debug.Log("[DevResetHelper] おやつ使用カウントをリセットしました。");
    }
}
