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
        SaveManager.Instance?.Save();
        Debug.Log("[DevResetHelper] おやつ使用カウントをリセットしました。");
    }

    /// <summary>
    /// お風呂の1日回数をリセットする（開発用）。
    /// 判定は「lastBathDate が今日なら bathCountToday を見る」なので、両方戻す。
    /// </summary>
    public void DevResetBathCount()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null)
        {
            Debug.LogWarning("[DevResetHelper] SaveManager not found.");
            return;
        }

        int before = save.bathCountToday;
        save.bathCountToday = 0;
        save.lastBathDate   = "";
        SaveManager.Instance?.Save();
        Debug.Log($"[DevResetHelper] お風呂の回数をリセットしました。{before}回 → 0回");
    }

    /// <summary>
    /// ねんねの12時間クールダウンをリセットする（開発用）。
    ///
    /// 触るのは lastSleepTicks だけ。
    /// statusLastBathAt などの「ステータスの時間経過用」とは別物なので巻き込まない。
    /// </summary>
    public void DevResetSleepCooldown()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null)
        {
            Debug.LogWarning("[DevResetHelper] SaveManager not found.");
            return;
        }

        save.lastSleepTicks = 0;
        SaveManager.Instance?.Save();
        Debug.Log("[DevResetHelper] ねんねのクールダウンをリセットしました。");
    }

    /// <summary>
    /// おやつ・お風呂・ねんねをまとめてリセットする（開発用）。
    /// ボタン1つで全部戻したいときはこれを結線する。
    /// </summary>
    public void DevResetAll()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null)
        {
            Debug.LogWarning("[DevResetHelper] SaveManager not found.");
            return;
        }

        save.freeOyatuCountToday = 0;
        save.lastFreeOyatuDate   = "";
        save.bathCountToday      = 0;
        save.lastBathDate        = "";
        save.lastSleepTicks      = 0;

        SaveManager.Instance?.Save();
        Debug.Log("<color=#00E5FF>[決定]</color> [DevResetHelper] おやつ・お風呂・ねんねをまとめてリセットしました。");
    }
}
