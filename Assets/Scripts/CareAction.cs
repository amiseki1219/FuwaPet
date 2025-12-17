using UnityEngine;

public class CareActions : MonoBehaviour
{
    [SerializeField] private MessageUI messageUI;

    string FormatRemain(int sec)
    {
        int m = sec / 60;
        int h = m / 60;
        int mm = m % 60;
        if (h > 0) return $"あと {h}時間{mm}分";
        return $"あと {m}分";
    }

    void CooldownMessage(string actionName, int remainSec)
        => messageUI.Show($"もう{actionName}できない！{FormatRemain(remainSec)}");

    void RewardMessage(string actionName, int coin, int trust)
    {
        if (coin > 0 && trust > 0)
            messageUI.Show($"{actionName}！Coin+{coin} / Trust+{trust}");
        else if (coin > 0)
            messageUI.Show($"{actionName}！Coin+{coin}");
        else
            messageUI.Show($"{actionName}！Trust+{trust}");
    }

    public void OnPet()
    {
        var d = GameData.Instance;
        if (!d.CanDo(ref d.nextPet, out int r))
        {
            CooldownMessage("撫で", r);
            return;
        }

        d.AddCoin(CareConfig.PET_COIN);
        d.AddTrust(CareConfig.PET_TRUST);
        d.SetCooldown(ref d.nextPet, CareConfig.PET_CD);

        RewardMessage("撫でた", CareConfig.PET_COIN, CareConfig.PET_TRUST);
    }

    public void OnPlay()
    {
        var d = GameData.Instance;
        if (!d.CanDo(ref d.nextPlay, out int r))
        {
            CooldownMessage("遊", r);
            return;
        }

        d.AddCoin(CareConfig.PLAY_COIN);
        d.AddTrust(CareConfig.PLAY_TRUST);
        d.SetCooldown(ref d.nextPlay, CareConfig.PLAY_CD);

        RewardMessage("遊んだ", CareConfig.PLAY_COIN, CareConfig.PLAY_TRUST);
    }

    public void OnEat()
    {
        var d = GameData.Instance;
        if (!d.CanDo(ref d.nextEat, out int r))
        {
            CooldownMessage("ご飯", r);
            return;
        }

        d.AddCoin(CareConfig.EAT_COIN);
        d.AddTrust(CareConfig.EAT_TRUST);
        d.SetCooldown(ref d.nextEat, CareConfig.EAT_CD);

        RewardMessage("ご飯あげた", CareConfig.EAT_COIN, CareConfig.EAT_TRUST);
    }

    public void OnBath()
    {
        var d = GameData.Instance;
        if (!d.CanDo(ref d.nextBath, out int r))
        {
            CooldownMessage("お風呂", r);
            return;
        }

        d.AddCoin(CareConfig.BATH_COIN);
        d.AddTrust(CareConfig.BATH_TRUST);
        d.SetCooldown(ref d.nextBath, CareConfig.BATH_CD);

        RewardMessage("お風呂入れた", CareConfig.BATH_COIN, CareConfig.BATH_TRUST);
    }
}
