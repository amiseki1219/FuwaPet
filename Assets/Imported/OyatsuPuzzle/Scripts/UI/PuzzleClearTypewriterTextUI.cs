using System.Collections;
using UnityEngine;
using TMPro;

namespace OyatsuPuzzle
{
    // クリア結果画面（PuzzleStageClearOverlayPanel）の吹き出しコメントをタイプライター表示する。
    // 失敗側（PuzzleFailTypewriterTextUI）とは分離した独立スクリプト（共通化はしない）。
    // PlayRandom() でランダムに1つ、PlayMessage(msg) で固定文（全クリア時など）を1文字ずつ表示する。
    //
    // ・多重起動しない（再生開始時に走行中コルーチンを停止）。
    // ・OnDisable（オーバーレイを閉じた瞬間）でコルーチンを停止し、表示も止める。
    public class PuzzleClearTypewriterTextUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("文字を表示する TMP。未設定なら同じ GameObject の TMP_Text を使用。")]
        [SerializeField] private TMP_Text targetText;

        [Header("Messages (ランダムに1つ選んで表示)")]
        [TextArea]
        [SerializeField] private string[] messages =
        {
            "やったね！クリアだよ〜♪",
            "すごいすごい！さすがだね",
            "おみごと！次もいけるよ",
            "ナイスプレイ〜！",
            "コツつかんできたね♪",
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

        // オーバーレイを閉じた（非アクティブ化した）瞬間に走行中の表示を止める。
        private void OnDisable()
        {
            StopRunning();
        }

        // ランダムに1メッセージを選んでタイプライター表示する。
        public void PlayRandom()
        {
            if (targetText == null || messages == null || messages.Length == 0) return;
            int idx = Random.Range(0, messages.Length);
            PlayMessage(messages[idx]);
        }

        // 指定メッセージ（全クリア時の固定文など）をタイプライター表示する。
        public void PlayMessage(string message)
        {
            if (targetText == null) return;
            StopRunning();
            // 非アクティブな GameObject ではコルーチンを開始できないため、即時に全文表示する。
            if (!isActiveAndEnabled)
            {
                targetText.text = message ?? "";
                return;
            }
            _running = StartCoroutine(TypeRoutine(message ?? ""));
        }

        private void StopRunning()
        {
            if (_running != null) { StopCoroutine(_running); _running = null; }
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
