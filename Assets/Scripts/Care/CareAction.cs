using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using System.IO;

public class CareActions : MonoBehaviour
{
    [SerializeField] private MessageUI messageUI;
    [SerializeField] private EatManager eatManager;
    [SerializeField] private RawImage profilePreview;

    [Header("プロフィール詳細パネルの参照")]
    [SerializeField] private GameObject profileDetailPanel;

    // ★追加：バッジを表示するためのRawImage
    [SerializeField] private RawImage badgeImage;

    private const float FULL_HUNGER = 100f;
    private const float HAPPY_MOOD = 100f;

    private int eatCount = 0;
    private int petCount = 0;
    private int playCount = 0;

    bool Roll(int percent) => Random.Range(0, 100) < percent;

    void Start()
    {
        // 画面起動時に両方セットするお！
        LoadUserProfile();
        UpdateBadgeDisplay();
    }

    // --- プロフィール画像の読み込み ---
    private void LoadUserProfile()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
        var data = SaveManager.Instance.Data;

        // ProfileDetailPanelと同じく、iconIdがあればそれを優先するお！
        string iconId = !string.IsNullOrEmpty(data.iconId) ? data.iconId : data.profileImagePath;
        if (string.IsNullOrEmpty(iconId)) return;

        Texture loadedIcon = Resources.Load<Texture>("SpecialIcon/" + iconId);
        if (loadedIcon == null) loadedIcon = Resources.Load<Texture>("Icon/" + iconId);

        if (loadedIcon != null && profilePreview != null)
        {
            profilePreview.texture = loadedIcon;
        }
    }

    // ★追加：バッジの読み込み
    public void UpdateBadgeDisplay()
    {
        if (badgeImage == null || BadgeManager.Instance == null) return;

        // 今の最強バッジをもらってくる
        string bestId = BadgeManager.Instance.GetCurrentBestBadgeId();

        if (!string.IsNullOrEmpty(bestId))
        {
            Texture badgeTex = Resources.Load<Texture>("BadgeUI/" + bestId);
            if (badgeTex != null)
            {
                badgeImage.enabled = true; // 表示！
                badgeImage.texture = badgeTex;
            }
            else { badgeImage.enabled = false; }
        }
        else
        {
            badgeImage.enabled = false; // 0円なら消す！
        }
    }

    public void OnClickProfileIcon()
    {
        if (profileDetailPanel != null)
        {
            profileDetailPanel.SetActive(true);
        }
    }

    // --- 以下、お世話系の関数は変更なし ---
    public void OnEat() { if (eatManager != null) eatManager.ToggleEatPanel(); }

    public void GiveSnack(string snackName)
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        if (GameData.Instance == null) return;
        int cost = 10;
        if (GameData.Instance.Coin < cost) { messageUI.Show("コインが足りないよ…！"); return; }
        GameData.Instance.UseCoin(cost);
        var pet = GameContext.Instance.PetStatus;
        pet.AddHunger(25f);
        pet.AddMood(2f);
        messageUI.Show($"{snackName} をあげたよ！\nもぐもぐ");
        if (eatManager != null) eatManager.ToggleEatPanel();
    }

    public void OnPet()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        var pet = GameContext.Instance.PetStatus;
        if (pet.Mood >= HAPPY_MOOD) { messageUI.Show("今はゆっくりしたいみたい。"); return; }
        GameContext.Instance.DailyTracker.OnCareSuccess();
        float addMood = (petCount == 0) ? 5f : (Roll(70) ? 1f : 0f);
        pet.AddMood(addMood);
        GameData.Instance?.AddCoin(3);
        pet.AddTrust(1);
        petCount++;
        messageUI.Show("えへへ、なでなで大好き！\nもっとやって〜！");
    }

    public void OnPlay()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null) return;
        var pet = GameContext.Instance.PetStatus;
        if (pet.Mood >= HAPPY_MOOD) { messageUI.Show("今はゆっくりしたいみたい。"); return; }
        GameContext.Instance.DailyTracker.OnCareSuccess();
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
        GameContext.Instance.PetStatus.AddMood(15f);
        GameContext.Instance.PetStatus.AddTrust(2);
        GameData.Instance?.AddCoin(5);
        messageUI.Show("お風呂できれいさっぱり！\nぽかぽかだよ〜。");
    }
}