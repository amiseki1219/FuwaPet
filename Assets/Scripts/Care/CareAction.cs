using UnityEngine;
using Game.Core;

public class CareActions : MonoBehaviour
{
    [SerializeField] private MessageUI messageUI;

    // 定数（ラフ画の「MAX 100」などの設定）
    private const float FULL_HUNGER = 100f;
    private const float HAPPY_MOOD = 100f;

    // お世話回数のカウント（初回ボーナス判定用）
    private int eatCount = 0;
    private int petCount = 0;
    private int playCount = 0;

    // 確率判定用の関数
    bool Roll(int percent) => Random.Range(0, 100) < percent;

    public void OnEat()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        var pet = GameContext.Instance.PetStatus;

        if (pet.Hunger >= FULL_HUNGER)
        {
            messageUI.Show("今はお腹いっぱい");
            return;
        }

        GameContext.Instance.DailyTracker.OnCareSuccess();

        // --- ラフ画：空腹度+25 / 機嫌度+2 / コイン+3 ---
        pet.AddHunger(25f);
        pet.AddMood(2f);
        GameData.Instance?.AddCoin(3);

        // --- ラフ画：信頼度 初回+1 / 2回目以降 40%の確率で+1 ---
        int addTrust = (eatCount == 0) ? 1 : (Roll(40) ? 1 : 0);
        eatCount++;
        pet.AddTrust(addTrust);

        messageUI.Show($"わぁ〜い！おいしい！\nもぐもぐ");
    }

    public void OnPet()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        var pet = GameContext.Instance.PetStatus;

        if (pet.Mood >= HAPPY_MOOD)
        {
            messageUI.Show("今はゆっくりしたいみたい。");
            return;
        }

        GameContext.Instance.DailyTracker.OnCareSuccess();

        // --- ラフ画：機嫌度 初回+5 / 2回目以降 70%の確率で+1 / コイン+3 ---
        float addMood = (petCount == 0) ? 5f : (Roll(70) ? 1f : 0f);
        pet.AddMood(addMood);
        GameData.Instance?.AddCoin(3);

        // 信頼度（なでなでも基本+1しておこうか！）
        pet.AddTrust(1);
        petCount++;

        messageUI.Show("えへへ、なでなで大好き！\nもっとやって〜！");
    }

    public void OnPlay()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        var pet = GameContext.Instance.PetStatus;

        if (pet.Mood >= HAPPY_MOOD)
        {
            messageUI.Show("今はゆっくりしたいみたい。");
            return;
        }

        GameContext.Instance.DailyTracker.OnCareSuccess();

        // --- ラフ画：空腹度-5 / 機嫌度+10 / 信頼度+1 / コイン+5 ---
        // 信頼度 2回目以降は 50%で+1
        pet.AddHunger(-5f);
        pet.AddMood(10f);
        GameData.Instance?.AddCoin(5);

        int addTrust = (playCount == 0) ? 1 : (Roll(50) ? 1 : 0);
        playCount++;
        pet.AddTrust(addTrust);

        messageUI.Show("たのしいね！\nいっしょに遊べてうれしいな！");
    }

    public void OnBath()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;

        GameContext.Instance.DailyTracker.OnCareSuccess();

        // --- ラフ画：1日1回 / 機嫌度+15 / 信頼度+2 / コイン+5 ---
        // ※「1日1回」の制限は DailyTracker がやってくれるよ
        GameContext.Instance.PetStatus.AddMood(15f);
        GameContext.Instance.PetStatus.AddTrust(2);
        GameData.Instance?.AddCoin(5);

        messageUI.Show("お風呂できれいさっぱり！\nぽかぽかだよ〜。");
    }
}