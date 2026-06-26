using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    // PuzzleClearPanel の後に表示する全画面のステージクリア結果画面。
    // 報酬・次ステージ・ステージ進行を表示し、「次のパズルへ」「スタートへ」を提供する。
    // 表示内容は Refresh() で更新（PuzzleScreenController.ShowStageClearResult から呼ばれる）。
    // ボタンの onClick は Awake で実行時にひもづける（シーン側の PersistentListener 配線に依存しない）。
    public class PuzzleStageClearResultUI : MonoBehaviour
    {
        [Header("Labels")]
        [Tooltip("タイトル（ステージクリア！）。タイトル画像を使う場合は未設定でOK。")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("もらったごほうび表示。PuzzleManager.LastRewardText を表示する。")]
        [SerializeField] private TMP_Text rewardText;
        [Tooltip("次のステージ表示（つぎは Stage N にチャレンジ！）。")]
        [SerializeField] private TMP_Text nextStageText;

        [Header("Reward Image (獲得報酬に応じて切替)")]
        [Tooltip("獲得報酬に応じて sprite を切り替える報酬アイコン。RewardImage を割り当て。")]
        [SerializeField] private Image rewardImage;
        [Tooltip("にぼし報酬の画像。")]
        [SerializeField] private Sprite niboshiRewardSprite;
        [Tooltip("無償コインのみ報酬の画像。")]
        [SerializeField] private Sprite coinRewardSprite;
        [Tooltip("無償コイン＋信頼度報酬の画像。")]
        [SerializeField] private Sprite coinTrustRewardSprite;
        [Tooltip("どうぶつビスケット報酬の画像。")]
        [SerializeField] private Sprite animalBiscuitRewardSprite;
        [Tooltip("にんじんスティック報酬の画像。")]
        [SerializeField] private Sprite carrotStickRewardSprite;
        [Tooltip("いちごケーキ報酬の画像。")]
        [SerializeField] private Sprite strawberryCakeRewardSprite;
        [Tooltip("プリン報酬の画像。")]
        [SerializeField] private Sprite puddingRewardSprite;

        [Header("Comment Bubble (タイプライター)")]
        [Tooltip("吹き出し内コメント。表示するたびにランダムな応援メッセージをタイプライター表示する。")]
        [SerializeField] private PuzzleClearTypewriterTextUI commentTypewriter;
        [Tooltip("全クリア時に吹き出しへ出す固定メッセージ。")]
        [SerializeField] private string allClearComment = "ぜんステージクリア！おつかれさま♪";

        [Header("Stage Progress")]
        [Tooltip("StageProgressBg に付けた進行バー。結果画面でステージ状態を更新する。")]
        [SerializeField] private PuzzleStageProgressBarUI stageProgressBarUI;

        [Header("Buttons")]
        [SerializeField] private Button nextPuzzleButton;
        [SerializeField] private Button backToStartButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleDailyPlayManager dailyPlayManager;
        [SerializeField] private PuzzleScreenController  screenController;

        private bool _wired;

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (_wired) return;
            if (nextPuzzleButton  != null) nextPuzzleButton.onClick.AddListener(OnClickNextPuzzle);
            if (backToStartButton != null) backToStartButton.onClick.AddListener(OnClickBackToStart);
            _wired = true;
        }

        // 結果画面を開くたびに呼ぶ。報酬・次ステージ・進行ドットを更新する。
        public void Refresh()
        {
            // CurrentStage は AdvanceStage の Min 頭打ちで Stage5クリア後も 5 のまま。
            // そのため CurrentStage-1 ではなく、FinishClear で記録した LastClearedStage / IsAllClear を使う。
            int maxStage     = PuzzleStageRegistry.StageCount;
            int clearedStage = puzzleManager != null ? puzzleManager.LastClearedStage
                                                      : PuzzleProgressManager.CurrentStage - 1;
            bool isAllClear  = (puzzleManager != null && puzzleManager.IsAllClear) || clearedStage >= maxStage;
            int nextStage    = clearedStage + 1;
            bool hasNext     = !isAllClear && nextStage <= maxStage;

            if (titleText != null) titleText.text = "ステージクリア！";

            // 報酬値は LastRewardText（付与ロジックの出力）をそのまま使い、表示(テキスト/画像)だけ整える。
            string reward = puzzleManager != null ? puzzleManager.LastRewardText : null;

            if (rewardText != null)
            {
                rewardText.richText = true;
                rewardText.text = BuildRewardDisplay(reward);
            }

            if (rewardImage != null)
            {
                // 獲得報酬に応じて RewardImage の sprite を切り替える。該当Sprite未設定時は現在の表示を維持。
                Sprite s = ResolveRewardSprite(reward);
                if (s != null) rewardImage.sprite = s;
            }

            if (nextStageText != null)
            {
                // 「Stage ○」部分だけ濃いピンク＆少し大きく（Rich Text）。全クリア時はタグなし。
                nextStageText.richText = true;
                nextStageText.text = hasNext
                    ? $"<color=#D58A8A>つぎは</color><color=#F05C86><size=115%>ステージ{nextStage}</size></color><color=#D58A8A>にチャレンジ！</color>"
                    : "全ステージクリア！";
            }

            // ステージ進行バー（肉球スタンプ＋吹き出し）を更新。
            // clearedStage=今クリアしたステージ / nextStage=次に挑むステージ。
            // 全クリア時は nextStage > maxStage となり RefreshForResult 側で全ノード Cleared 扱いになる。
            if (stageProgressBarUI != null)
                stageProgressBarUI.RefreshForResult(clearedStage, nextStage, maxStage);

            // 吹き出しコメント：全クリア時は固定文を、それ以外はランダム応援メッセージをタイプライター表示。
            if (commentTypewriter != null)
            {
                if (isAllClear) commentTypewriter.PlayMessage(allClearComment);
                else            commentTypewriter.PlayRandom();
            }

            // 「次のパズルへ」は、次ステージがあり かつ 残りプレイ回数があるときだけ押せる。
            // （次ステージ開始＝1プレイ消費のため、残り0なら開始できない）
            int remainingForNext = dailyPlayManager != null ? dailyPlayManager.RemainingPlays : 0;
            if (nextPuzzleButton != null) nextPuzzleButton.interactable = hasNext && remainingForNext > 0;
        }

        // 次のパズルへ：次ステージを「1回プレイ開始」する。ステージ開始＝プレイ回数を1消費する。
        // 全クリア時 / 残りプレイが無いときは StartCurrentStage() を呼ばずスタートへ戻す。
        public void OnClickNextPuzzle()
        {
            // 全クリア後の安全ガード：Stage5クリア後は StartCurrentStage() を絶対に呼ばずスタートへ戻す。
            bool isAllClear = puzzleManager != null && puzzleManager.IsAllClear;
            int  nextStage  = PuzzleProgressManager.CurrentStage;
            bool hasNext    = !isAllClear && nextStage <= PuzzleStageRegistry.StageCount;

            if (!hasNext)
            {
                if (screenController != null) screenController.ShowStart();
                return;
            }

            // 次ステージ開始＝1プレイ消費。残りが無ければ開始せずスタートへ戻す。
            if (dailyPlayManager == null || !dailyPlayManager.CanPlay())
            {
                Debug.Log("[OyatsuPuzzle] No plays remaining - cannot start next stage.");
                if (screenController != null) screenController.ShowStart();
                return;
            }
            dailyPlayManager.ConsumePlay();

            if (puzzleManager    != null) puzzleManager.StartCurrentStage();
            if (screenController != null) screenController.ShowGame();
        }

        // スタートへ：PuzzleStartPanel に戻る（既存の ShowStart を利用＝game/clear/overlay は非表示）。
        public void OnClickBackToStart()
        {
            if (screenController != null) screenController.ShowStart();
        }

        // ── 報酬表示の整形（表示のみ。報酬値・付与ロジックには一切影響しない） ──
        private const string RewardNameColor   = "#8B5E4A"; // 報酬名カラー(茶)
        private const string RewardAmountColor = "#F05C86"; // 数量カラー(ピンク)
        private const int    RewardNameSize    = 85;        // 報酬名サイズ(%) 単一報酬(ヒーロー表示)
        private const int    RewardAmountSize  = 160;       // 数量サイズ(%)   単一報酬(ヒーロー表示)
        // 複数報酬(例: ステージ5 = コイン＋信頼度)はボックスに収めるため1行ずつのコンパクト表示にする
        private const int    RewardNameMultiSize    = 80;   // 報酬名サイズ(%) 複数報酬
        private const int    RewardAmountMultiSize  = 135;  // 数量サイズ(%)   複数報酬
        private const int    RewardMultiLineHeight  = 90;   // 行間(%)        複数報酬

        // 獲得報酬(LastRewardText)に応じた RewardImage 用 Sprite を返す。未該当/未設定は null（表示維持）。
        private Sprite ResolveRewardSprite(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            bool hasCoin  = raw.Contains("Free Coin");
            bool hasTrust = raw.Contains("Trust");
            if (hasCoin && hasTrust) return coinTrustRewardSprite;
            if (hasCoin)             return coinRewardSprite;
            if (raw.Contains("Niboshi"))         return niboshiRewardSprite;
            if (raw.Contains("Biscuit"))         return animalBiscuitRewardSprite;
            if (raw.Contains("Carrot Stick"))    return carrotStickRewardSprite;
            if (raw.Contains("Strawberry Cake")) return strawberryCakeRewardSprite;
            if (raw.Contains("Pudding"))         return puddingRewardSprite;
            return null;
        }

        // LastRewardText を「報酬名(小・茶) / 数量(大・ピンク)」の2段Rich Textに整形する。
        // 複数報酬(改行区切り)は各ブロックの間に1行空けて並べる。報酬値は元の文字列のまま使う。
        private static string BuildRewardDisplay(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Trim() == "No Reward")
                return $"<color={RewardNameColor}><size={RewardNameSize}%>ごほうびなし</size></color>";

            string[] lines = raw.Split('\n');
            // 各報酬を (名前, 数量) に分解して保持
            var names   = new System.Collections.Generic.List<string>();
            var amounts = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                string entry = line.Trim();
                if (entry.Length == 0) continue;
                // ステージ4ランダム報酬の "Random Reward: ..." 接頭辞を除去
                const string randPrefix = "Random Reward:";
                if (entry.StartsWith(randPrefix)) entry = entry.Substring(randPrefix.Length).Trim();

                string name, amount;
                ParseRewardEntry(entry, out name, out amount);
                if (name.Length == 0) continue;
                names.Add(name);
                amounts.Add(amount);
            }

            if (names.Count == 0)
                return $"<color={RewardNameColor}><size={RewardNameSize}%>ごほうびなし</size></color>";

            // 報酬が1つ：名前(小)＋数量(大)の2段ヒーロー表示
            if (names.Count == 1)
            {
                string block = $"<color={RewardNameColor}><size={RewardNameSize}%>{names[0]}</size></color>";
                if (amounts[0].Length > 0)
                    block += $"\n<color={RewardAmountColor}><size={RewardAmountSize}%>{amounts[0]}</size></color>";
                return block;
            }

            // 報酬が複数(例: ステージ5 = コイン＋信頼度)：1行ずつのコンパクト表示でボックスに収める
            var rows = new System.Collections.Generic.List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                string row = $"<color={RewardNameColor}><size={RewardNameMultiSize}%>{names[i]} </size></color>";
                if (amounts[i].Length > 0)
                    row += $"<color={RewardAmountColor}><size={RewardAmountMultiSize}%>{amounts[i]}</size></color>";
                rows.Add(row);
            }
            return $"<line-height={RewardMultiLineHeight}%>" + string.Join("\n", rows);
        }

        // 1報酬分の文字列を「日本語の報酬名」と「数量」に分解する。
        private static void ParseRewardEntry(string entry, out string name, out string amount)
        {
            name = ""; amount = "";

            if (entry.StartsWith("Free Coin"))
            {
                name = "無償コイン";
                amount = entry.Substring("Free Coin".Length).Trim(); // 例: "+50"
                return;
            }
            if (entry.StartsWith("Trust"))
            {
                name = "信頼度";
                string a = entry.Substring("Trust".Length).Trim();   // 例: "+10pt"
                if (a.EndsWith("pt")) a = a.Substring(0, a.Length - 2).Trim();
                amount = a;                                           // 例: "+10"
                return;
            }

            // "{Piece} xN" 形式（ピース名に空白を含む場合があるので最後の " x" で分割）
            int xi = entry.LastIndexOf(" x");
            if (xi > 0)
            {
                string en  = entry.Substring(0, xi).Trim();
                string num = entry.Substring(xi + 2).Trim();
                name = MapPieceName(en);
                amount = "×" + num; // おやつ報酬は助数詞をやめて「×N」で統一（例: "×1"）
                return;
            }

            // 想定外はそのまま名前として表示（数量なし）
            name = entry;
        }

        // 英語ピース名 → 日本語の報酬名（数量は呼び出し側で「×N」に統一）
        private static string MapPieceName(string english)
        {
            switch (english)
            {
                case "Niboshi":         return "にぼし";
                case "Biscuit":         return "どうぶつビスケット";
                case "Carrot Stick":    return "にんじんスティック";
                case "Strawberry Cake": return "いちごケーキ";
                case "Pudding":         return "プリン";
                case "Star Cookie":     return "ほしクッキー";
                case "Heart Macaron":   return "ハートマカロン";
                case "Coin":            return "コイン";
                default:                return english;
            }
        }
    }
}
