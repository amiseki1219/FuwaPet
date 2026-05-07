using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public event Action OnQuestProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── フェーズ判定 ────────────────────────────────────────

    public bool IsTutorialPhase
    {
        get
        {
            var save = SaveManager.Instance?.Data;
            if (save == null) return true;
            if (!save.tutorialAllCompleted) return true;
            // 完了当日はまだチュートリアル表示（翌クエスト日からデイリーへ）
            return save.tutorialCompletedDate == GetCurrentQuestDay();
        }
    }

    // ─── デイリーリセット ────────────────────────────────────

    public void CheckDailyReset()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null || !save.tutorialAllCompleted) return;

        string today = GetCurrentQuestDay();
        if (save.lastQuestResetDate == today) return;

        save.lastQuestResetDate = today;
        save.dailyQuestsDone = new bool[4];
        save.dailyBonusClaimed = false;
        save.dailyNadeCount = 0;
        SaveManager.Instance.Save();
        OnQuestProgressChanged?.Invoke();

        NotifyLogin();
    }

    // ─── 通知メソッド（各シーンから呼ぶ） ────────────────────

    public void NotifyLogin()
    {
        if (!IsTutorialPhase) CompleteQuest(QuestId.DailyLogin);
    }

    public void NotifyConversation()
    {
        if (IsTutorialPhase) CompleteQuest(QuestId.TutorialChat);
        else CompleteQuest(QuestId.DailyChat);
    }

    public void NotifyFeed()
    {
        if (IsTutorialPhase) CompleteQuest(QuestId.TutorialFeed);
        else CompleteQuest(QuestId.DailyFeed);
    }

    public void NotifyNade()
    {
        if (IsTutorialPhase) CompleteQuest(QuestId.TutorialNade);
        else AddDailyNadeCount();
    }

    public void NotifyShopOpened()
    {
        if (IsTutorialPhase) CompleteQuest(QuestId.TutorialShop);
    }

    public void NotifySettingOpened()
    {
        if (IsTutorialPhase) CompleteQuest(QuestId.TutorialSetting);
    }

    // ─── なでなでカウント（デイリー④専用） ───────────────────

    private void AddDailyNadeCount()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null || save.dailyQuestsDone[3]) return;

        save.dailyNadeCount++;
        if (save.dailyNadeCount >= 3)
        {
            CompleteQuest(QuestId.DailyNade);
        }
        else
        {
            SaveManager.Instance.Save();
            OnQuestProgressChanged?.Invoke();
        }
    }

    // ─── コア：クエスト完了処理 ──────────────────────────────

    public void CompleteQuest(QuestId id)
    {
        var save = SaveManager.Instance?.Data;
        if (save == null) return;

        if (IsTutorialPhase)
        {
            int idx = TutorialIndex(id);
            if (idx < 0 || save.tutorialQuestsDone[idx]) return;

            save.tutorialQuestsDone[idx] = true;
            AwardTutorialReward(id);

            if (!save.tutorialBonusClaimed && AllTutorialDone(save))
            {
                save.tutorialBonusClaimed = true;
                save.tutorialAllCompleted = true;
                save.tutorialCompletedDate = GetCurrentQuestDay();
                GameData.Instance?.AddCoin(100);
                GameData.Instance?.AddTalkTicket(5);
                Debug.Log("[Quest] チュートリアル完了ボーナス支給！");
            }
        }
        else
        {
            int idx = DailyIndex(id);
            if (idx < 0 || save.dailyQuestsDone[idx]) return;

            save.dailyQuestsDone[idx] = true;
            AwardDailyReward(id);

            if (!save.dailyBonusClaimed && AllDailyDone(save))
            {
                save.dailyBonusClaimed = true;
                GameData.Instance?.AddCoin(30);
                GameData.Instance?.AddTalkTicket(1);
                Debug.Log("[Quest] デイリー全クリアボーナス支給！");
            }
        }

        SaveManager.Instance?.Save();
        OnQuestProgressChanged?.Invoke();
    }

    // ─── 報酬付与 ────────────────────────────────────────────

    private void AwardTutorialReward(QuestId id)
    {
        switch (id)
        {
            case QuestId.TutorialChat:    GameData.Instance?.AddTalkTicket(3); break;
            case QuestId.TutorialFeed:    GameData.Instance?.AddCoin(50);       break;
            case QuestId.TutorialNade:    GameData.Instance?.AddCoin(30);       break;
            case QuestId.TutorialShop:    GameData.Instance?.AddCoin(20);       break;
            case QuestId.TutorialSetting: GameData.Instance?.AddCoin(20);       break;
        }
    }

    private void AwardDailyReward(QuestId id)
    {
        switch (id)
        {
            case QuestId.DailyLogin: GameData.Instance?.AddCoin(10);       break;
            case QuestId.DailyChat:  GameData.Instance?.AddTalkTicket(1);  break;
            case QuestId.DailyFeed:  GameData.Instance?.AddCoin(20);       break;
            case QuestId.DailyNade:  GameData.Instance?.AddCoin(15);       break;
        }
    }

    // ─── ヘルパー ────────────────────────────────────────────

    private int TutorialIndex(QuestId id) => id switch
    {
        QuestId.TutorialChat    => 0,
        QuestId.TutorialFeed    => 1,
        QuestId.TutorialNade    => 2,
        QuestId.TutorialShop    => 3,
        QuestId.TutorialSetting => 4,
        _ => -1
    };

    private int DailyIndex(QuestId id) => id switch
    {
        QuestId.DailyLogin => 0,
        QuestId.DailyChat  => 1,
        QuestId.DailyFeed  => 2,
        QuestId.DailyNade  => 3,
        _ => -1
    };

    private bool AllTutorialDone(SaveData save)
    {
        foreach (var done in save.tutorialQuestsDone)
            if (!done) return false;
        return true;
    }

    private bool AllDailyDone(SaveData save)
    {
        foreach (var done in save.dailyQuestsDone)
            if (!done) return false;
        return true;
    }

    // JST = UTC+9、3:00 AM リセット（バックエンド実装まではローカル判定）
    public string GetCurrentQuestDay()
    {
        DateTime jst = DateTime.UtcNow.AddHours(9);
        if (jst.Hour < 3) jst = jst.AddDays(-1);
        return jst.ToString("yyyy-MM-dd");
    }

    // ─── UI向け状態取得 ──────────────────────────────────────

    public bool[] GetTutorialQuestsDone() =>
        SaveManager.Instance?.Data?.tutorialQuestsDone ?? new bool[5];

    public bool[] GetDailyQuestsDone() =>
        SaveManager.Instance?.Data?.dailyQuestsDone ?? new bool[4];

    public int GetDailyNadeCount() =>
        SaveManager.Instance?.Data?.dailyNadeCount ?? 0;

    public bool IsTutorialBonusClaimed() =>
        SaveManager.Instance?.Data?.tutorialBonusClaimed ?? false;

    public bool IsDailyBonusClaimed() =>
        SaveManager.Instance?.Data?.dailyBonusClaimed ?? false;

    public bool[] GetTutorialQuestsClaimed() =>
        SaveManager.Instance?.Data?.tutorialQuestsClaimed ?? new bool[5];

    public bool[] GetDailyQuestsClaimed() =>
        SaveManager.Instance?.Data?.dailyQuestsClaimed ?? new bool[4];

    // ─── 報酬受取処理（UIの「受け取る」ボタンから呼ぶ） ──────────
    // ⚠️ CompleteQuest が既に報酬を自動付与するため、現状は二重付与になる。
    // 手動クリック方式に統一する場合は CompleteQuest 内の報酬付与を削除すること。

    public void ClaimReward(QuestId id)
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) { Debug.LogWarning($"[QuestManager] ClaimReward({id}): data null"); return; }

        if (IsTutorialPhase)
        {
            int idx = TutorialIndex(id);
            if (idx < 0 || idx >= 5) { Debug.LogWarning($"[QuestManager] ClaimReward({id}): invalid index {idx}"); return; }
            if (!data.tutorialQuestsDone[idx])    { Debug.LogWarning($"[QuestManager] ClaimReward({id}): 未完了のため受取不可"); return; }
            if (data.tutorialQuestsClaimed[idx])  { Debug.Log($"[QuestManager] ClaimReward({id}): 受取済みのためスキップ"); return; }

            data.tutorialQuestsClaimed[idx] = true;
            GiveQuestReward(id);
            Debug.Log($"[QuestManager] ClaimReward({id}): 報酬付与完了");
        }
        else
        {
            int idx = DailyIndex(id);
            if (idx < 0 || idx >= 4) { Debug.LogWarning($"[QuestManager] ClaimReward({id}): invalid index {idx}"); return; }
            if (!data.dailyQuestsDone[idx])    { Debug.LogWarning($"[QuestManager] ClaimReward({id}): 未完了のため受取不可"); return; }
            if (data.dailyQuestsClaimed[idx])  { Debug.Log($"[QuestManager] ClaimReward({id}): 受取済みのためスキップ"); return; }

            data.dailyQuestsClaimed[idx] = true;
            GiveQuestReward(id);
            Debug.Log($"[QuestManager] ClaimReward({id}): 報酬付与完了");
        }

        SaveManager.Instance?.Save();
        OnQuestProgressChanged?.Invoke();
    }

    public void ClaimBonusReward()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) return;

        if (!data.tutorialAllCompleted)
        {
            if (!data.tutorialBonusClaimed && AllTutorialDone(data))
            {
                data.tutorialBonusClaimed  = true;
                data.tutorialAllCompleted  = true;
                data.tutorialCompletedDate = GetCurrentQuestDay();
                GameData.Instance?.AddCoin(100);
                GameData.Instance?.AddTalkTicket(5);
                SaveManager.Instance?.Save();
                OnQuestProgressChanged?.Invoke();
            }
        }
        else
        {
            if (!data.dailyBonusClaimed && AllDailyDone(data))
            {
                data.dailyBonusClaimed = true;
                GameData.Instance?.AddCoin(30);
                GameData.Instance?.AddTalkTicket(1);
                SaveManager.Instance?.Save();
                OnQuestProgressChanged?.Invoke();
            }
        }
    }

    // 既存の Award メソッドに委譲して報酬を付与する
    private void GiveQuestReward(QuestId id)
    {
        if (TutorialIndex(id) >= 0) AwardTutorialReward(id);
        else                        AwardDailyReward(id);
    }
}
