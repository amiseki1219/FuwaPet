using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // --- オーナー情報 ---
    public string userName;
    public string ownerBirthday;
    public string ownerBirthYear = "";   // 西暦4桁 例:"2000"
    public string playerId;

    // --- プロフィール・レベル ---
    public string profileImagePath;
    public string iconId = "DefaultIcon";  // デフォルトアイコン（未設定でも常に DefaultIcon が入る）
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

    // --- お知らせ既読管理 ---
    public List<string> readNoticeIds = new List<string>();

    // --- バッジバージョン管理 ---
    public string lastShopVersion       = "";
    public string lastGachaVersion      = "";
    public string lastCollectionVersion = "";

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
    public bool accountLinkShown = false; // データ引き継ぎ案内を表示済みか

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

    // --- クエスト進捗 ---
    public bool[] tutorialQuestsDone = new bool[5];
    public bool tutorialBonusClaimed;
    public bool tutorialAllCompleted;
    public string tutorialCompletedDate;

    public bool[] dailyQuestsDone = new bool[4];
    public bool dailyBonusClaimed;
    public int dailyNadeCount;
    public string lastQuestResetDate;

    public bool coachMarkShown;

    public bool[] tutorialQuestsClaimed = new bool[5];
    public bool[] dailyQuestsClaimed    = new bool[4];

    // --- おやつ使用カウント ---
    public int freeOyatuCountToday = 0;
    public string lastFreeOyatuDate = "";

    // --- お世話回数制限 ---
    public int bathCountToday = 0;
    public string lastBathDate = "";
    public int nadeCountToday = 0;
    public string lastNadeDate = "";
    public int playCountToday = 0;
    public string lastPlayDate = "";
    public long lastSleepTicks = 0; // ねんね8時間クールダウン用

    // --- ステータスの時間経過用（ISO 8601 ラウンドトリップ形式。空文字なら未記録） ---
    // ※ 上の lastBathDate / lastPlayDate は1日の回数制限用で別物。混同しないこと。
    public string statusLastFedAt   = "";
    public string statusLastBathAt  = "";
    public string statusLastPlayAt  = "";
    public string statusLastDecayAt = "";

    // --- プロフィール変更日（2週間ロック用） ---
    public string lastNameChangeDate     = "";
    public string lastCharNameChangeDate = "";
    public string lastBirthdayChangeDate = "";

    // --- 性格パラメータ（初期値はキャラ選択時にセット） ---
    public int personalityActivity    = 0; // 活動性（-100〜+100）
    public int personalityDependency  = 0; // 甘えん坊度（-100〜+100）
    public int personalityHonesty     = 0; // 素直さ（-100〜+100）
    public int personalityDiligence   = 0; // 勤勉さ（-100〜+100）
    public int personalitySensitivity = 0; // 感受性（-100〜+100）

    public SaveData()
    {
        // 新規作成時にGUIDを生成
        playerId = System.Guid.NewGuid().ToString("D").ToUpper();

        // リストの初期化
        ownedIcons = new List<string> { "DefaultIcon" };
        ownedFrames = new List<string> { "DefaultFrame" };
        ownedBackgrounds = new List<string> { "DefaultBG" };
        words = new List<WordData>();
        diaries = new List<DiaryEntry>();

        // 設定の初期値（ここでも念のため設定しておくと安心だお！）
        bgmVolume = 0.5f;
        seVolume = 0.5f;
        isNotificationOn = true;
        isSeOn = true;

        // チュートリアル：新規・削除後は必ず未完了にする
        onboardingCompleted = false;

        // クエスト進捗の配列を初期化
        tutorialQuestsDone    = new bool[5];
        dailyQuestsDone       = new bool[4];
        tutorialQuestsClaimed = new bool[5];
        dailyQuestsClaimed    = new bool[4];
    }
}