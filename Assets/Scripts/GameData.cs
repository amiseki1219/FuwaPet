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

    // PlayerPrefs Keys（直書き禁止）
    private const string KEY_COIN = "Coin";
    private const string KEY_TRUST = "Trust";
    private const string KEY_NEXT_PET = "nextPet";
    private const string KEY_NEXT_EAT = "nextEat";
    private const string KEY_NEXT_PLAY = "nextPlay";
    private const string KEY_NEXT_BATH = "nextBath";
    private const string KEY_LAST_SAVE = "lastSaveTime";

    // 前回セーブ時刻（Unix秒）
    private long lastSaveTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // =========================
    // 数値操作
    // =========================
    public void AddCoin(int amount)
    {
        Coin = Mathf.Max(0, Coin + amount);
        Save();
    }

    public void AddTrust(int amount)
    {
        Trust = Mathf.Max(0, Trust + amount);
        Save();
    }

    // =========================
    // クールダウン判定
    // =========================
    public bool CanDo(ref long nextTime, out int remainSeconds)
    {
        long now = Now();
        long diff = nextTime - now;

        if (diff <= 0)
        {
            remainSeconds = 0;
            return true;
        }

        remainSeconds = (diff > int.MaxValue) ? int.MaxValue : (int)diff;
        return false;
    }

    public void SetCooldown(ref long nextTime, int cooldownSeconds)
    {
        nextTime = Now() + cooldownSeconds;
        Save();
    }

    // =========================
    // Save / Load
    // =========================
    public void Save()
    {
        PlayerPrefs.SetInt(KEY_COIN, Coin);
        PlayerPrefs.SetInt(KEY_TRUST, Trust);

        PlayerPrefs.SetString(KEY_NEXT_PET, nextPet.ToString());
        PlayerPrefs.SetString(KEY_NEXT_EAT, nextEat.ToString());
        PlayerPrefs.SetString(KEY_NEXT_PLAY, nextPlay.ToString());
        PlayerPrefs.SetString(KEY_NEXT_BATH, nextBath.ToString());

        lastSaveTime = Now();
        PlayerPrefs.SetString(KEY_LAST_SAVE, lastSaveTime.ToString());

        PlayerPrefs.Save();
    }

    private void Load()
    {
        Coin = PlayerPrefs.GetInt(KEY_COIN, 0);
        Trust = PlayerPrefs.GetInt(KEY_TRUST, 0);

        long.TryParse(PlayerPrefs.GetString(KEY_NEXT_PET, "0"), out nextPet);
        long.TryParse(PlayerPrefs.GetString(KEY_NEXT_EAT, "0"), out nextEat);
        long.TryParse(PlayerPrefs.GetString(KEY_NEXT_PLAY, "0"), out nextPlay);
        long.TryParse(PlayerPrefs.GetString(KEY_NEXT_BATH, "0"), out nextBath);

        long.TryParse(PlayerPrefs.GetString(KEY_LAST_SAVE, "0"), out lastSaveTime);

        ApplyTimeProgress();
    }

    // =========================
    // 時間経過による変化
    // =========================
    private void ApplyTimeProgress()
    {
        if (lastSaveTime <= 0) return;

        long now = Now();
        long elapsedSeconds = now - lastSaveTime;
        if (elapsedSeconds <= 0) return;

        // 例：5分（300秒）ごとに Trust -1
        int trustLoss = (int)(elapsedSeconds / 300);

        if (trustLoss > 0)
        {
            Trust = Mathf.Max(0, Trust - trustLoss);
        }
    }

    // =========================
    // iOS対策（保険）
    // =========================
    private void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
