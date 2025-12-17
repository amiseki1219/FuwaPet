using System;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public int Coin { get; private set; }
    public int Trust { get; private set; }

    // 次にできる時刻（Unix秒）
    public long nextPet;
    public long nextEat;
    public long nextPlay;
    public long nextBath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void AddCoin(int amount) { Coin += Mathf.Max(0, amount); }
    public void AddTrust(int amount) { Trust += Mathf.Max(0, amount); }

    public bool CanDo(ref long nextTime, out int remainSeconds)
    {
        long now = Now();
        long diff = nextTime - now;
        if (diff <= 0)
        {
            remainSeconds = 0;
            return true;
        }
        remainSeconds = (int)diff;
        return false;
    }

    public void SetCooldown(ref long nextTime, int cooldownSeconds)
    {
        nextTime = Now() + cooldownSeconds;
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("Coin", Coin);
        PlayerPrefs.SetInt("Trust", Trust);
        PlayerPrefs.SetString("nextPet", nextPet.ToString());
        PlayerPrefs.SetString("nextEat", nextEat.ToString());
        PlayerPrefs.SetString("nextPlay", nextPlay.ToString());
        PlayerPrefs.SetString("nextBath", nextBath.ToString());
        PlayerPrefs.Save();
    }

    void Load()
    {
        Coin = PlayerPrefs.GetInt("Coin", 0);
        Trust = PlayerPrefs.GetInt("Trust", 0);
        long.TryParse(PlayerPrefs.GetString("nextPet", "0"), out nextPet);
        long.TryParse(PlayerPrefs.GetString("nextEat", "0"), out nextEat);
        long.TryParse(PlayerPrefs.GetString("nextPlay", "0"), out nextPlay);
        long.TryParse(PlayerPrefs.GetString("nextBath", "0"), out nextBath);
    }
}
