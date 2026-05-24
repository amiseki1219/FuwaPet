using UnityEngine;

public class DebugPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject debugPanel;

    public void OpenPanel()
    {
        if (debugPanel != null) debugPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (debugPanel != null) debugPanel.SetActive(false);
    }

    public void AddCoin()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.AddCoin(1000);
            Debug.Log($"[DEV] コイン +1000 → 残高: {GameData.Instance.Coin}");
        }
        else
        {
            var save = SaveManager.Instance?.Data;
            if (save == null) { Debug.LogWarning("[DEV] SaveManager.Instance is null"); return; }
            save.coinCount += 1000;
            SaveManager.Instance.Save();
            Debug.Log($"[DEV] コイン +1000 → 残高: {save.coinCount}（SaveManager直接）");
        }
    }

    public void AddLuna()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.AddLunaStone(1000);
            Debug.Log($"[DEV] ルナストーン +1000 → 残高: {GameData.Instance.LunaStone}");
        }
        else
        {
            var save = SaveManager.Instance?.Data;
            if (save == null) { Debug.LogWarning("[DEV] SaveManager.Instance is null"); return; }
            save.lunaStoneCount += 1000;
            SaveManager.Instance.Save();
            Debug.Log($"[DEV] ルナストーン +1000 → 残高: {save.lunaStoneCount}（SaveManager直接）");
        }
    }

    public void ResetWallet()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null) { Debug.LogWarning("[DEV] SaveManager.Instance is null"); return; }
        save.coinCount = 0;
        save.lunaStoneCount = 0;
        SaveManager.Instance.Save();
        Debug.Log("[DEV] 財布をリセットしました");
    }
}
