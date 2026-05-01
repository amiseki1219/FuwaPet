using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // --- オーナー情報 ---
    public string userName;
    public string ownerBirthday;
    public string playerId;

    // --- プロフィール・レベル ---
    public string profileImagePath;
    public string iconId;
    public string selectedFrameId;
    public string selectedPetFrameId;
    public int playerLevel;

    // --- ペット情報 ---
    public string characterId;
    public int petColorSlot;
    public string petName; //デフォルトのペット名
    public string petNickname = "";  // キャラのニックネーム（省略可）
    public string selectedCharacterId;
    public string startDate; // 出会った日
    public string lastDate;  // 最後に遊んだ日

    // --- お財布 ---
    public int coinCount = 300;       // 無償コイン
    public int lunaStoneCount = 50;   // 有償ルナ・ストーン
    public int trust = 0;            // なかよし度

    // --- アイテムのポケット ---
    public int cloudCandyCount = 0;    // くもキャンディ
    public int talkTicketCount = 0;   // お話チケット
    public int sitterTicketCount = 0;  // シッターチケット

    // --- 設定情報（★ここが新しく追加した部分だお！） ---
    public float bgmVolume = 0.5f;          // BGM音量 (0.0 ~ 1.0)
    public float seVolume = 0.5f;           // SE音量 (0.0 ~ 1.0)
    public bool isNotificationOn = true;    // 通知設定
    public bool isSeOn = true;              // 効果音スイッチ

    // --- 購入・課金情報 ---
    public List<string> ownedDecoIds = new List<string>();
    public bool isFirstTimePackBought = false;
    public long lastPackAPurchaseTicks = 0;
    public long lastPackBPurchaseTicks = 0;
    public long totalBillingAmount = 0; // 課金総額
    public string selectedBadgeId = ""; // 称号バッジ

    // --- 初回起動日 ---
    public string firstLoginDate;

    // --- お世話・システム ---
    public long nextPet = 0;
    public long nextEat = 0;
    public long nextPlay = 0;
    public long nextBath = 0;
    public bool onboardingCompleted;

    // --- 持ち物・記録リスト ---
    public List<string> ownedIcons = new();
    public List<string> ownedFrames = new();
    public List<string> ownedBackgrounds = new();
    public List<WordData> words = new();
    public List<DiaryEntry> diaries = new();

    // --- ペットステータス ---
    public float hunger = 50f;
    public float clean = 50f;
    public float energy = 50f;
    public float mood = 50f;

    public SaveData()
    {
        // 新規作成時にGUIDを生成
        playerId = System.Guid.NewGuid().ToString("D").ToUpper();

        // リストの初期化
        ownedIcons = new List<string> { "Icon1", "Icon2" };
        ownedFrames = new List<string> { "DefaultFrame" };
        ownedBackgrounds = new List<string> { "DefaultBG" };
        words = new List<WordData>();
        diaries = new List<DiaryEntry>();

        // 設定の初期値（ここでも念のため設定しておくと安心だお！）
        bgmVolume = 0.5f;
        seVolume = 0.5f;
        isNotificationOn = true;
        isSeOn = true;
    }
}