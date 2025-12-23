using UnityEngine;
using Game.Core;

public class CareActions : MonoBehaviour
{
    [SerializeField] private MessageUI messageUI;

    const float FULL_HUNGER = 80f;
    const float HAPPY_MOOD = 85f;

    int trustGainEatCount = 0;
    int trustGainPetCount = 0;
    int trustGainPlayCount = 0;

    int RollPlus1(int plus1Percent)
        => (Random.Range(0, 100) < plus1Percent) ? 1 : 0;

    public void OnEat()
    {
        var ctx = GameContext.Instance;
        var pet = ctx.PetStatus;

        if (pet.Hunger >= FULL_HUNGER)
        {
            messageUI.Show("今はお腹いっぱい…");
            return;
        }

        // ✅ 今日のお世話カウント（1回だけ）
        ctx.Daily.OnCare();

        // 効果
        pet.AddHunger(+25f);
        pet.AddMood(+2f);

        int addTrust = (trustGainEatCount == 0) ? 1 : RollPlus1(40);
        trustGainEatCount++;
        pet.AddTrust(addTrust);

        GameData.Instance?.AddCoin(5);

        messageUI.Show($"ご飯！Hunger+25 / Mood+2 / Trust+{addTrust} / Coin+5");
    }

    public void OnPet()
    {
        var ctx = GameContext.Instance;
        var pet = ctx.PetStatus;

        if (pet.Mood >= HAPPY_MOOD)
        {
            messageUI.Show("今は満足してるみたい。なでなで不要！");
            return;
        }

        ctx.Daily.OnCare();

        pet.AddMood(+6f);

        int addTrust = (trustGainPetCount == 0) ? 3 : RollPlus1(30);
        trustGainPetCount++;
        pet.AddTrust(addTrust);

        GameData.Instance?.AddCoin(5);

        messageUI.Show($"なでた！Mood+6 / Trust+{addTrust} / Coin+5");
    }

    public void OnPlay()
    {
        var ctx = GameContext.Instance;
        var pet = ctx.PetStatus;

        if (pet.Mood >= HAPPY_MOOD)
        {
            messageUI.Show("今はゆっくりしたいみたい…");
            return;
        }

        ctx.Daily.OnCare();

        pet.AddHunger(-5f);
        pet.AddMood(+12f);

        int addTrust = (trustGainPlayCount == 0) ? 2 : RollPlus1(50);
        trustGainPlayCount++;
        pet.AddTrust(addTrust);

        GameData.Instance?.AddCoin(5);

        messageUI.Show($"遊んだ！Hunger-5 / Mood+12 / Trust+{addTrust} / Coin+5");
    }

    public void OnBath()
    {
        var ctx = GameContext.Instance;
        var pet = ctx.PetStatus;

        // ✅ 日付跨ぎの更新（0時跨ぎ）をここでやっておくと事故りにくい
        ctx.Daily.EnsureTodayAndCheckNewDay();

        if (!ctx.Daily.CanBath())
        {
            messageUI.Show("今日はもうお風呂入れられない！");
            return;
        }

        ctx.Daily.OnCare();
        ctx.Daily.MarkBath();

        pet.AddMood(+15f);
        pet.AddTrust(2);

        GameData.Instance?.AddCoin(15);

        messageUI.Show("お風呂！Mood+15 / Trust+2 / Coin+15");
    }
}
