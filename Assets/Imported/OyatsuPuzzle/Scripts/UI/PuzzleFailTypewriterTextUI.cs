using System.Collections;
using UnityEngine;
using TMPro;

namespace OyatsuPuzzle
{
    // 失敗結果画面（PuzzleStageFailOverlayPanel）に出す応援メッセージのタイプライター表示。
    // PlayRandom() を呼ぶと messages からランダムに1つ選び、1文字ずつ表示する。
    // クリア用の演出スクリプトとは独立。表示対象 TMP は同じ GameObject から自動取得 or Inspector 指定。
    public class PuzzleFailTypewriterTextUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("文字を表示する TMP。未設定なら同じ GameObject の TMP_Text を使用。")]
        [SerializeField] private TMP_Text targetText;

        [Header("Messages (ランダムに1つ選んで表示)")]
        [TextArea]
        [SerializeField] private string[] messages =
        {
            "あと少しだったね、もう一回やってみよ〜",
            "だいじょうぶ、次はいけるよ♪",
            "もうちょっとでクリアだったよ〜",
            "いっしょにもう一回がんばろっ",
            "コツつかんできたかも〜！",
        };

        [Header("Typewriter")]
        [Tooltip("1文字あたりの表示間隔(秒)。")]
        [SerializeField] private float charInterval = 0.06f;
        [Tooltip("表示開始までの待ち(秒)。")]
        [SerializeField] private float startDelay = 0.1f;

        private Coroutine _running;

        private void Awake()
        {
            if (targetText == null) targetText = GetComponent<TMP_Text>();
        }

        // ランダムに1メッセージを選び、タイプライター表示を開始する（多重再生はしない）。
        public void PlayRandom()
        {
            if (targetText == null) return;
            if (messages == null || messages.Length == 0) return;

            // 乱数で1つ選ぶ（Math.Random 系は使わず UnityEngine.Random）。
            int idx = Random.Range(0, messages.Length);
            PlayMessage(messages[idx]);
        }

        public void PlayMessage(string message)
        {
            if (targetText == null) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(TypeRoutine(message ?? ""));
        }

        private IEnumerator TypeRoutine(string message)
        {
            targetText.text = "";
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            float interval = Mathf.Max(0f, charInterval);
            for (int i = 0; i < message.Length; i++)
            {
                targetText.text = message.Substring(0, i + 1);
                if (interval > 0f) yield return new WaitForSeconds(interval);
            }
            _running = null;
        }
    }
}
