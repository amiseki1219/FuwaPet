using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // オーナー情報
    public string ownerName;
    public string ownerBirthday;
    public string playerId;

    public SaveData()
    {
        // 新規作成時にGUIDを生成（大文字ハイフンあり）
        playerId = System.Guid.NewGuid().ToString("D").ToUpper();

        // 他のリストの初期化もここにあると安心だお
        ownedIcons = new List<string> { "Icon1", "Icon2" };
        ownedFrames = new List<string> { "DefaultFrame" };
        ownedBackgrounds = new List<string> { "DefaultBG" };
        words = new List<WordData>();
        diaries = new List<DiaryEntry>();
    }
    public string profileImagePath;
    public string iconId;
    public string selectedFrameId;
    public string selectedPetFrameId;
    public int playerLevel;

    // ペット情報
    public string characterId;
    public int petColorSlot;
    public string petName;

    // どのキャラを選んだか
    public string selectedCharacterId;
    public string startDate; // 出会った日
    public string lastDate;  // 最後に遊んだ日

    // --- お財布 ---
    public int coinCount = 300;       // 無償コイン（初期値300）
    public int lunaStoneCount = 0;   // 有償ルナ・ストーン
    public int trust = 0;            // なかよし度
    // --- 【新しく追加！】アイテムのポケット ---
    public int cloudCandyCount = 0;    // くもキャンディ
    public int talkTicketCount = 0;   // お話チケット
    public int sitterTicketCount = 0;  // シッターチケット

    // 特別なデコや背景（持っているかどうかのリスト）
    public List<string> ownedDecoIds = new List<string>();
    // ① 初回限定パックを買ったかどうか
    public bool isFirstTimePackBought = false;

    // パックAの購入制限用（Ticksで保存）
    public long lastPackAPurchaseTicks = 0;
    // ② パックBを最後に買った時間（Ticksという数字で保存するのが正確だお！）
    public long lastPackBPurchaseTicks = 0;

    // --- お世話の時間記録 ---
    public long nextPet = 0;
    public long nextEat = 0;
    public long nextPlay = 0;
    public long nextBath = 0;

    // オンボーディング完了判定
    public bool onboardingCompleted;

    // --- ★【ここから追加：持ち物リスト】★ ---
    // 最初から持っているもののIDを初期値に入れておくと、
    // 買ったものと区別がつくから便利だお！
    // 最初から持っているアイコンを2つ（Icon1とIcon2）にしておくお！
    public List<string> ownedIcons = new List<string> { "Icon1", "Icon2" };
    public List<string> ownedFrames = new List<string> { "DefaultFrame" };
    public List<string> ownedBackgrounds = new List<string> { "DefaultBG" };

    public List<WordData> words = new();
    public List<DiaryEntry> diaries = new();

    public long totalBillingAmount = 0; // 課金総額（円）
    public string selectedBadgeId = ""; // 現在つけているバッジID
}