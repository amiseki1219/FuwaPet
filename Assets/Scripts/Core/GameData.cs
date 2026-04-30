using System;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    // ★【ここが心臓部】SaveManager経由でSaveDataにアクセスする窓口
    private SaveData CurrentSave => SaveManager.Instance.Data;

    // --- お財布（SaveDataの値をそのまま返す） ---
    public int Coin => CurrentSave.coinCount;
    // GameData.cs のお財布セクションに追加
    public int LunaStone => CurrentSave.lunaStoneCount;
    public int Trust => CurrentSave.trust;

    // --- ペットとプレイヤーの情報（SaveDataを窓口にする） ---
    public string PetName => CurrentSave.petName;
    public string PlayerName => CurrentSave.userName;
    public string selectedCharacterId => CurrentSave.selectedCharacterId;

    // --- 時間関係（これもSaveDataから取る） ---
    public long nextPet => CurrentSave.nextPet;
    public long nextEat => CurrentSave.nextEat;
    public long nextPlay => CurrentSave.nextPlay;
    public long nextBath => CurrentSave.nextBath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // SaveManager側でLoadが完了している必要があるお！
    }

    // --- データの更新メソッドたち ---

    public void AddCoin(int amount)
    {
        CurrentSave.coinCount += Mathf.Max(0, amount);
        Save();
    }

    public bool UseCoin(int amount)
    {
    if (CurrentSave.coinCount < amount)
    {
        Debug.LogWarning("コインが足りないよ！");
        return false;
    }
    CurrentSave.coinCount -= amount;
    Save();
    return true;
    }
    

    public void AddLunaStone(int amount)
    {
        CurrentSave.lunaStoneCount += amount;
        if (CurrentSave.lunaStoneCount < 0) CurrentSave.lunaStoneCount = 0;
        Save();
    }

    public bool UseLunaStone(int amount)
    {
        if (CurrentSave.lunaStoneCount < amount)
    {
        Debug.LogWarning("ルナストーンが足りないよ！");
        return false;
    }
    CurrentSave.lunaStoneCount -= amount;
    Save();
    return true;
    }

    public void SetPetName(string newName)
    {
        CurrentSave.petName = newName;
        Save();
    }

    public void SetPlayerName(string newName)
    {
        CurrentSave.userName = newName;
        Save();
    }

    // ★【重要】保存はPlayerPrefsじゃなくSaveManagerにお願いする
    public void Save()
    {
        SaveManager.Instance.Save();
        Debug.Log("SaveDataに保存したお！");
    }
    // --- GameData.cs に追加 ---

    // ①初回限定パックが買えるかチェック
    public bool CanBuyFirstTimePack()
    {
        return !CurrentSave.isFirstTimePackBought;
    }

    // ②パックAが買えるかチェック（7日経過チェック）
    public bool CanBuyPackA()
    {
        // 一度も買ってなければOK
        if (CurrentSave.lastPackAPurchaseTicks == 0) return true;

        DateTime lastDate = new DateTime(CurrentSave.lastPackAPurchaseTicks);
        TimeSpan elapsed = DateTime.Now - lastDate;

        return elapsed.TotalDays >= 7;
    }

    // ③パックBが買えるかチェック（7日経過チェック）
    public bool CanBuyPackB()
    {
        if (CurrentSave.lastPackBPurchaseTicks == 0) return true;

        DateTime lastDate = new DateTime(CurrentSave.lastPackBPurchaseTicks);
        TimeSpan elapsed = DateTime.Now - lastDate;

        return elapsed.TotalDays >= 7;
    }

    // --- 購入確定した時のデータ更新処理 ---

    public void OnBuyFirstTimePack()
    {
        CurrentSave.isFirstTimePackBought = true;
        Save();
    }

    public void OnBuyPackA()
    {
        CurrentSave.lastPackAPurchaseTicks = DateTime.Now.Ticks;
        Save();
    }

    public void OnBuyPackB()
    {
        CurrentSave.lastPackBPurchaseTicks = DateTime.Now.Ticks;
        Save();
    }
    // --- アイテム付与セクション（ここを追加だお！） ---


    public void AddTalkTicket(int amount)
    {
        CurrentSave.talkTicketCount += amount;
        Save();
    }

    public void AddSitterTicket(int amount)
    {
        CurrentSave.sitterTicketCount += amount;
        Save();
    }

    public void AddCloudCandy(int amount)
    {
        CurrentSave.cloudCandyCount += amount;
        Save();
    }

    public void AddDeco(string id)
    {
        if (!CurrentSave.ownedDecoIds.Contains(id))
        {
            CurrentSave.ownedDecoIds.Add(id);
            Save();
        }
    }

}
