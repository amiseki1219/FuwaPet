using UnityEngine;
using System.Collections.Generic;
using System.Linq; // これがないと並び替え(OrderBy)が使えないお！

public class RankingManager : MonoBehaviour
{
    [SerializeField] private List<RankingEntry> allEntries = new List<RankingEntry>();
    [SerializeField] private RankingEntry myStatusEntry;
    [SerializeField] private TMPro.TextMeshProUGUI myRankText;

    void Start()
    {
        // ゲームが始まったらランキングを更新するお！
        RefreshRanking();
    }

    public void RefreshRanking()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) {
            Debug.LogWarning("SaveManagerかDataが見つからないお！");
            return;
        }

        // 1. データの準備
        List<SaveData> allPlayers = new List<SaveData> { SaveManager.Instance.Data };
        
        // 【テスト用】もしライバルがいたらどうなるか試したい時は、ここを増やすお！
        // allPlayers.Add(new SaveData { ownerName = "ライバルくん", playerLevel = 100 });

        // 2. 信頼度（レベル）が高い順に並び替える
        var sortedList = allPlayers.OrderByDescending(x => x.playerLevel).ToList();

        // 3. 自分の順位をリストの中から探し出す！
        // FindIndexは「0」から数えるから、人間に合わせて「+1」するお✨
        int myRankNumber = sortedList.FindIndex(x => x.ownerName == SaveManager.Instance.Data.ownerName) + 1;

        if (sortedList.Count > 0)
        {
            var topPlayer = sortedList[0];

            // 1位パネルの更新
            if (allEntries.Count > 0 && allEntries[0] != null) {
                allEntries[0].Setup(1, topPlayer.ownerName, topPlayer.playerLevel, topPlayer.selectedFrameId, topPlayer.iconId, true);
            }

            // ★マイステータス（自分）の更新
            if (myStatusEntry != null) {
                // ここも計算した「myRankNumber」を渡すお！
                myStatusEntry.Setup(myRankNumber, SaveManager.Instance.Data.ownerName, SaveManager.Instance.Data.playerLevel, SaveManager.Instance.Data.selectedFrameId, SaveManager.Instance.Data.iconId, true);
            }

            // ★【ここがやりたかったところ！】順位テキストの更新
            if (myRankText != null)
            {
                // 計算された順位を「○位」という文字にして入れるお！
                myRankText.text = myRankNumber + "位";
            }
        }
    }
}