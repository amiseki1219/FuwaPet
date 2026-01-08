using System;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public int Coin { get; private set; }
    public int Trust { get; private set; }
    public string selectedCharacterId;

    // ★【修正】名前を2種類用意したよ
    public string PetName { get; private set; } = "なまえ";
    public string PlayerName { get; private set; } = "あみまる";

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

    // ★【修正】ペットの名前を保存する
    public void SetPetName(string newName)
    {
        PetName = newName;
        Save();
    }

    // ★【追加】ユーザーの名前を保存する
    public void SetPlayerName(string newName)
    {
        PlayerName = newName;
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("Coin", Coin);
        PlayerPrefs.SetInt("Trust", Trust);
        PlayerPrefs.SetString("PetName", PetName);
        PlayerPrefs.SetString("PlayerName", PlayerName); // 保存！
        PlayerPrefs.SetString("nextPet", nextPet.ToString());
        PlayerPrefs.SetString("nextEat", nextEat.ToString());
        PlayerPrefs.SetString("nextPlay", nextPlay.ToString());
        PlayerPrefs.SetString("nextBath", nextBath.ToString());
        PlayerPrefs.Save();
    }

    void Load()
    {
        // --- コインの初期設定 (500コイン) ---
        // 一旦 -1 を入れて、データがあるかないかチェックするよ
        int savedCoin = PlayerPrefs.GetInt("Coin", -1);

        if (savedCoin == -1)
        {
            // まだ一度も保存されてない（初めての人）なら500コインあげる！
            Coin = 500;
            Save(); // 忘れないうちに「500持ってるよ」って保存しちゃう
        }
        else
        {
            // すでに遊んだことがある人は、保存されてる数字を使う
            Coin = savedCoin;
        }

        // --- その他の読み込み ---
        Trust = PlayerPrefs.GetInt("Trust", 0);
        PetName = PlayerPrefs.GetString("PetName", "なまえ");
        PlayerName = PlayerPrefs.GetString("PlayerName", "あみまる");

        // 時間関係の読み込み
        long.TryParse(PlayerPrefs.GetString("nextPet", "0"), out nextPet);
        long.TryParse(PlayerPrefs.GetString("nextEat", "0"), out nextEat);
        long.TryParse(PlayerPrefs.GetString("nextPlay", "0"), out nextPlay);
        long.TryParse(PlayerPrefs.GetString("nextBath", "0"), out nextBath);
        //選んだキャラクターのIDを読み込む（デフォルト値０）
        selectedCharacterId = PlayerPrefs.GetString("SelectedCharacterID", "Dog");
    }

    // 他のメソッド（AddCoinとか）は変えなくてOK！
    public void AddCoin(int amount) { Coin += Mathf.Max(0, amount); }
    public void AddTrust(int amount) { Trust += Mathf.Max(0, amount); }
}